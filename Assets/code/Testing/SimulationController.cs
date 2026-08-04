using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Khans.Invasion.Testing
{
    /// <summary>
    /// Autoplay smoke-test harness. Waits for the player nation to be ready,
    /// then advances turns automatically on a fixed cadence and asserts core
    /// invariants each turn (no negative gold/pop, TurnManager not stuck,
    /// no exceptions/null refs). Writes a PASS/FAIL report to Logs/.
    ///
    /// Safe to leave in the scene during normal play: it does nothing unless
    /// autoStart is enabled or SimulationRunner.RequestRun was called.
    /// </summary>
    public class SimulationController : MonoBehaviour
    {
        public const string ReportDirectoryName = "Logs";
        public const string ReportFileName = "simulation_report.txt";

        [Header("Simulation Settings")]
        [Tooltip("If true, the harness auto-starts when the player nation is ready (manual scene use).")]
        [SerializeField] private bool autoStart = false;

        [Tooltip("Number of turns to advance before writing the report.")]
        [SerializeField] private int turnsToRun = 10;

        [Tooltip("Seconds to wait between turn advances so armies can move and battles can play out.")]
        [SerializeField] private float turnIntervalSeconds = 2.0f;

        [Tooltip("If the run does not finish within this many seconds, force-finish as FAIL.")]
        [SerializeField] private float timeoutSeconds = 120f;

        [Tooltip("AIOnly: just advance turns. ScriptedPlayer: also drive player armies/generals via public APIs.")]
        [SerializeField] private SimulationMode mode = SimulationMode.AIOnly;

        // Request state used by the headless SimulationRunner. Statics survive
        // play-mode transitions, so the runner can set this before entering play.
        private static bool runRequested = false;
        private static SimulationMode requestedMode = SimulationMode.AIOnly;
        private static int requestedTurns = 10;
        private static float requestedTurnInterval = 2f;
        private static float requestedTimeout = 120f;
        private static int requestedSeed;
        private static string requestedReportFileName = ReportFileName;
        private const string ExternalRequestFileName = "mcp_simulation.request";

        public static bool LastResult { get; private set; } = true;

        private readonly List<string> violations = new List<string>();
        private readonly StringBuilder snapshotText = new StringBuilder();
        private bool started = false;
        private bool finished = false;
        private bool armed = false;
        private bool failing = false;
        private bool inLogCallback = false;
        private int turnsAdvanced = 0;
        private float runStartTime;
        private Coroutine turnLoop;

        public static void RequestRun(
            SimulationMode mode,
            int turns = 10,
            float turnInterval = 2f,
            float timeout = 120f,
            int seed = 0,
            string reportFileName = null)
        {
            runRequested = true;
            requestedMode = mode;
            requestedTurns = Mathf.Max(1, turns);
            requestedTurnInterval = Mathf.Max(0f, turnInterval);
            requestedTimeout = Mathf.Max(requestedTurnInterval * requestedTurns + 10f, timeout);
            requestedSeed = seed;
            requestedReportFileName = string.IsNullOrWhiteSpace(reportFileName) ? ReportFileName : reportFileName;
        }

        public static void ResetRequest()
        {
            runRequested = false;
        }

        // Boot flag path: spawn the controller at runtime when a headless run
        // was requested, so no scene modification is required.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            TryConsumeExternalRequest();
            if (!runRequested) return;
            if (FindFirstObjectByType<SimulationController>() != null) return;

            GameObject go = new GameObject("SimulationController");
            go.AddComponent<SimulationController>();
        }

        private static void TryConsumeExternalRequest()
        {
            if (runRequested) return;

            string requestPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ReportDirectoryName, ExternalRequestFileName);
            if (!File.Exists(requestPath)) return;

            try
            {
                string[] values = File.ReadAllText(requestPath).Trim().Split(',');
                int turns = values.Length > 0 && int.TryParse(values[0], out int parsedTurns) ? parsedTurns : 50;
                float interval = values.Length > 1 && float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedInterval) ? parsedInterval : 0.1f;
                float timeout = values.Length > 2 && float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedTimeout) ? parsedTimeout : 90f;
                int seed = values.Length > 3 && int.TryParse(values[3], out int parsedSeed) ? parsedSeed : 0;
                string reportName = values.Length > 4 ? values[4].Trim() : ReportFileName;

                RequestRun(SimulationMode.AIOnly, turns, interval, timeout, seed, reportName);
                File.Delete(requestPath);
            }
            catch (Exception exception)
            {
                GameLog.Error(GameLogCategory.Core, $"[SimulationController] Failed to consume external request: {exception.Message}");
            }
        }
        private void Awake()
        {
            if (!runRequested) return;

            mode = requestedMode;
            turnsToRun = requestedTurns;
            turnIntervalSeconds = requestedTurnInterval;
            timeoutSeconds = requestedTimeout;
            if (requestedSeed != 0)
            {
                UnityEngine.Random.InitState(requestedSeed);
            }
        }

        private void Start()
        {
            if (!autoStart && !runRequested)
            {
                return;
            }

            armed = true;
            runStartTime = Time.unscaledTime;
            GameLog.Configure(GameLogProfile.FullDebug, GameLogCategory.All, true, true);
            GameLog.Log(GameLogCategory.Core, $"[SimulationController] Starting ({mode}) - {turnsToRun} turns");

            Application.logMessageReceived += OnLogMessageReceived;
            GameEvents.OnPlayerNationReady += OnPlayerNationReady;

            if (PlayerNation.Instance != null && PlayerNation.Instance.Nation != null)
            {
                OnPlayerNationReady();
            }
        }

        private void OnPlayerNationReady()
        {
            if (started || finished) return;
            started = true;
            turnLoop = StartCoroutine(RunTurns());
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
        }

        private void Update()
        {
            if (!armed || finished) return;

            if (!started)
            {
                if (Time.unscaledTime - runStartTime > timeoutSeconds)
                {
                    RecordViolation($"Timed out waiting for GameEvents.OnPlayerNationReady ({timeoutSeconds:F0}s)");
                    FinishSimulation();
                }
                return;
            }

            if (Time.unscaledTime - runStartTime > timeoutSeconds)
            {
                RecordViolation($"Simulation exceeded {timeoutSeconds:F0}s timeout");
                FinishSimulation();
            }
        }

        private IEnumerator RunTurns()
        {
            GameLog.Log(GameLogCategory.Core, "[SimulationController] Player nation ready - starting turn loop.");

            AssertInvariants();
            RecordSnapshot();

            for (int i = 0; i < turnsToRun && !failing; i++)
            {
                if (turnIntervalSeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(turnIntervalSeconds);
                }
                else
                {
                    yield return null;
                }

                if (mode == SimulationMode.ScriptedPlayer)
                {
                    ExecuteScriptedPlayerActions();
                }

                AdvanceTurn();

                if (!failing)
                {
                    AssertInvariants();
                }
                RecordSnapshot();
            }

            FinishSimulation();
        }

        private void AdvanceTurn()
        {
            TurnManager turnManager = TurnManager.Instance;
            if (turnManager == null)
            {
                RecordViolation("TurnManager.Instance is null - cannot advance turn");
                return;
            }

            if (turnManager.CurrentPhase != TurnManager.TurnPhase.PlayerTurn)
            {
                RecordViolation($"TurnManager stuck in phase {turnManager.CurrentPhase} before advancing turn");
                return;
            }

            int turnBefore = turnManager.CurrentTurn;
            try
            {
                turnManager.EndPlayerTurn();
            }
            catch (Exception e)
            {
                RecordViolation($"Exception during EndPlayerTurn (turn {turnBefore}): {e.GetType().Name}: {e.Message}");
                return;
            }

            if (turnManager.CurrentTurn != turnBefore + 1)
            {
                RecordViolation($"TurnManager did not advance (turn {turnBefore} -> {turnManager.CurrentTurn})");
                return;
            }

            turnsAdvanced++;

            if (turnManager.CurrentPhase != TurnManager.TurnPhase.PlayerTurn)
            {
                RecordViolation($"TurnManager stuck in phase {turnManager.CurrentPhase} after advancing to turn {turnManager.CurrentTurn}");
            }
        }

        private void AssertInvariants()
        {
            PlayerNation player = PlayerNation.Instance;
            if (player == null || player.Nation == null)
            {
                RecordViolation("PlayerNation.Instance or PlayerNation.Nation is null");
            }
            else
            {
                if (player.nationMoney < 0f)
                {
                    RecordViolation($"Negative gold: {player.nationMoney:F0}");
                }

                if (player.OwnedProvinces == null)
                {
                    RecordViolation("Player has no OwnedProvinces list");
                }
                else
                {
                    foreach (ProvinceModel province in player.OwnedProvinces)
                    {
                        if (province == null)
                        {
                            RecordViolation("Null province in player's OwnedProvinces");
                        }
                        else if (province.provinceCurrentPop < 0f)
                        {
                            RecordViolation($"Negative population in {province.provinceName}: {province.provinceCurrentPop:F0}");
                        }
                    }
                }
            }

            if (TurnManager.Instance == null)
            {
                RecordViolation("TurnManager.Instance is null");
            }
        }

        private void RecordSnapshot()
        {
            TurnManager turnManager = TurnManager.Instance;
            int turn = turnManager != null ? turnManager.CurrentTurn : 0;
            PlayerNation player = PlayerNation.Instance;
            float gold = player != null ? player.nationMoney : 0f;
            float pop = GetPlayerPopulation();
            string armyCounts = GetArmyCountsPerNation();
            AIManager.AIActivityMetrics aiActivity = AIManager.Instance != null
                ? AIManager.Instance.GetActivityMetrics()
                : new AIManager.AIActivityMetrics(0, 0, 0f);

            snapshotText.AppendLine($"[Turn {turn}] Gold: {gold:F0} | Pop: {pop:F0} | Armies: {armyCounts} | AI raids: {aiActivity.RaidCount} | AI conquests: {aiActivity.ConquestCount} | AI raid loot: {aiActivity.RaidLoot:F0}");
            GameLog.Log(GameLogCategory.Core, $"[SimulationController] Turn {turn} snapshot - Gold: {gold:F0}, Pop: {pop:F0}, Armies: {armyCounts}, AI raids: {aiActivity.RaidCount}, AI conquests: {aiActivity.ConquestCount}, AI raid loot: {aiActivity.RaidLoot:F0}");
        }

        private float GetPlayerPopulation()
        {
            PlayerNation player = PlayerNation.Instance;
            if (player == null || player.OwnedProvinces == null) return 0f;

            float total = 0f;
            foreach (ProvinceModel province in player.OwnedProvinces)
            {
                if (province != null)
                {
                    total += province.provinceCurrentPop;
                }
            }
            return total;
        }

        private string GetArmyCountsPerNation()
        {
            if (ArmyManager.Instance == null) return "n/a";

            List<Army> armies = ArmyManager.Instance.GetAllArmies();
            if (armies.Count == 0) return "none";

            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (Army army in armies)
            {
                if (army == null) continue;
                string nationName = army.OwnerNation != null ? army.OwnerNation.nationName : "Unknown";
                if (!counts.ContainsKey(nationName)) counts[nationName] = 0;
                counts[nationName]++;
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{pair.Key}={pair.Value}");
            }
            return sb.ToString();
        }

        // ===== SCRIPTED PLAYER MODE =====

        private void ExecuteScriptedPlayerActions()
        {
            if (ArmyManager.Instance == null) return;

            TryRaid();
            TryMovePlayerArmies();
        }

        private void TryRaid()
        {
            if (RaidManager.Instance == null) return;

            foreach (General general in FindObjectsByType<General>(FindObjectsSortMode.None))
            {
                if (general == null || !general.HasArmy) continue;

                ProvinceModel target = FindEnemyProvince();
                if (target == null || !RaidManager.Instance.CanRaidProvince(target)) continue;

                try
                {
                    RaidManager.Instance.ExecuteRaid(target, general);
                }
                catch (Exception e)
                {
                    RecordViolation($"Exception in scripted raid: {e.GetType().Name}: {e.Message}");
                }
                return;
            }
        }

        private void TryMovePlayerArmies()
        {
            List<Army> playerArmies = ArmyManager.Instance.GetPlayerArmies();
            if (playerArmies.Count == 0) return;

            ProvinceModel target = FindEnemyProvince();
            if (target == null) return;

            foreach (Army army in playerArmies)
            {
                if (army == null || !army.CanReceiveMovementOrders) continue;

                try
                {
                    army.MoveToProvince(target);
                }
                catch (Exception e)
                {
                    RecordViolation($"Exception in scripted army move: {e.GetType().Name}: {e.Message}");
                }
                return;
            }
        }

        private ProvinceModel FindEnemyProvince()
        {
            NationModel playerNation = PlayerNation.Instance != null ? PlayerNation.Instance.Nation : null;

            foreach (ProvinceModel province in FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None))
            {
                if (province == null) continue;
                if (province.provinceOwner == null || province.provinceOwner == playerNation) continue;
                return province;
            }
            return null;
        }

        // ===== REPORTING =====

        private void RecordViolation(string message)
        {
            violations.Add(message);
            failing = true;
            GameLog.Error(GameLogCategory.Core, $"[SimulationController] VIOLATION: {message}");
        }

        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (finished || inLogCallback) return;
            if (type != LogType.Error && type != LogType.Exception) return;

            inLogCallback = true;
            RecordViolation($"Unity logged {type}: {logString}");
            inLogCallback = false;
        }

        private void FinishSimulation()
        {
            if (finished) return;
            finished = true;

            if (turnLoop != null)
            {
                StopCoroutine(turnLoop);
            }
            Application.logMessageReceived -= OnLogMessageReceived;
            GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
            runRequested = false;

            WriteReport();
            LastResult = !failing;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void WriteReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("KHAN'S INVASION - SIMULATION REPORT");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Mode: {mode}");
            sb.AppendLine($"Seed: {requestedSeed}");
            sb.AppendLine($"Turns requested: {turnsToRun}");
            sb.AppendLine($"Turns advanced: {turnsAdvanced}");
            if (AIManager.Instance != null)
            {
                AIManager.AIActivityMetrics aiActivity = AIManager.Instance.GetActivityMetrics();
                sb.AppendLine($"AI raids: {aiActivity.RaidCount}");
                sb.AppendLine($"AI conquests: {aiActivity.ConquestCount}");
                sb.AppendLine($"AI raid loot: {aiActivity.RaidLoot:F0}");
            }
            sb.AppendLine();
            sb.AppendLine("=== PER-TURN SNAPSHOTS ===");
            sb.AppendLine(snapshotText.ToString());
            sb.AppendLine("=== VIOLATIONS ===");
            if (violations.Count == 0)
            {
                sb.AppendLine("(none)");
            }
            else
            {
                foreach (string violation in violations)
                {
                    sb.AppendLine($"- {violation}");
                }
            }
            sb.AppendLine();
            sb.AppendLine(failing ? "RESULT: FAIL" : "RESULT: PASS");

            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string reportDirectory = Path.Combine(projectRoot, ReportDirectoryName);
                Directory.CreateDirectory(reportDirectory);
                string reportPath = Path.Combine(reportDirectory, requestedReportFileName);
                File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
                GameLog.Log(GameLogCategory.Core, $"[SimulationController] Report written to {reportPath}");
            }
            catch (Exception e)
            {
                GameLog.Error(GameLogCategory.Core, $"[SimulationController] Failed to write report: {e.Message}");
            }
        }
    }

    public enum SimulationMode
    {
        AIOnly = 0,
        ScriptedPlayer = 1
    }
}
