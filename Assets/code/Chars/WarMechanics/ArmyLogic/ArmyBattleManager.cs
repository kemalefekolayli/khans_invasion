using System.Collections.Generic;
using UnityEngine;

public class ArmyBattleManager : MonoBehaviour
{
    public static ArmyBattleManager Instance { get; private set; }

    [Header("Detection")]
    [SerializeField] private float battleRadius = 0.75f;
    [SerializeField] private float retreatRadiusMultiplier = 1.35f;
    [SerializeField] private float scanIntervalSeconds = 0.4f;
    [SerializeField] private bool logScanDiagnostics = true;
    [SerializeField] private float diagnosticRadius = 3f;

    [Header("Battle Rules")]
    [SerializeField] private float battleTickSeconds = 1f;
    [SerializeField] private float minCombatantSize = 10f;
    [SerializeField] private float winnerLossPercent = 0.1f;
    [SerializeField] private float loserLossPercent = 0.3f;
    [SerializeField] private float drawLossPercent = 0.2f;
    [SerializeField] private float qualityReward = 0.03f;
    [SerializeField] private float troopLevelXpReward = 15f;

    private readonly Dictionary<int, ArmyBattleState> activeBattles = new Dictionary<int, ArmyBattleState>();
    private readonly Dictionary<Army, int> battleByArmy = new Dictionary<Army, int>();
    private readonly List<int> battleIdBuffer = new List<int>();
    private float nextDiagnosticLogTime;
    private float nextScanTime;

    private class ArmyBattleState
    {
        public Army armyA;
        public Army armyB;
        public float elapsed;
        public int turn;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (Instance != null || FindFirstObjectByType<ArmyBattleManager>() != null) return;

        GameObject managerObject = new GameObject("ArmyBattleManager");
        managerObject.AddComponent<ArmyBattleManager>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateBattles();
        ScanForNewBattles();
    }

    private void ScanForNewBattles()
    {
        if (ArmyManager.Instance == null)
        {
            if (ShouldLogDiagnostics())
                GameLog.Warning(GameLogCategory.Core, "[ArmyBattleManager] ArmyManager.Instance is null; cannot scan for battles.");
            return;
        }

        if (Time.time < nextScanTime) return;
        nextScanTime = Time.time + scanIntervalSeconds;

        IReadOnlyList<Army> armies = ArmyManager.Instance.AllArmies;
        if (ShouldLogDiagnostics() && armies.Count < 2)
        {
            GameLog.Log(GameLogCategory.Core, $"[ArmyBattleManager] Need at least 2 armies to fight. Registered armies: {armies.Count}");
        }

        float radiusSqr = battleRadius * battleRadius;

        for (int i = 0; i < armies.Count; i++)
        {
            Army armyA = armies[i];
            if (!CanStartBattle(armyA)) continue;

            for (int j = i + 1; j < armies.Count; j++)
            {
                Army armyB = armies[j];
                if (!CanStartBattle(armyB)) continue;
                if (!AreHostile(armyA, armyB))
                {
                    LogCloseFriendlyOrInvalidPair(armyA, armyB);
                    continue;
                }

                float distanceSqr = (armyA.transform.position - armyB.transform.position).sqrMagnitude;
                if (distanceSqr <= radiusSqr)
                {
                    StartBattle(armyA, armyB);
                    break;
                }
            }
        }
    }

    private bool CanStartBattle(Army army)
    {
        return army != null
            && army.ArmySize > minCombatantSize
            && !army.IsInBattle
            && !army.IsRetreating
            && !army.IsCaptured
            && !battleByArmy.ContainsKey(army);
    }

    private bool AreHostile(Army armyA, Army armyB)
    {
        if (armyA == null || armyB == null) return false;
        if (armyA.OwnerNation != null && armyB.OwnerNation != null)
            return armyA.OwnerNation != armyB.OwnerNation;

        return armyA.IsPlayerArmy != armyB.IsPlayerArmy;
    }

    private void LogCloseFriendlyOrInvalidPair(Army armyA, Army armyB)
    {
        if (!ShouldLogDiagnostics() || armyA == null || armyB == null) return;

        float distance = Vector3.Distance(armyA.transform.position, armyB.transform.position);
        if (distance > diagnosticRadius) return;

        GameLog.Log(GameLogCategory.Core, 
            "[ArmyBattleManager] Close armies are not hostile: " +
            $"{DescribeArmy(armyA)} vs {DescribeArmy(armyB)} | " +
            $"distance={distance:F2}, battleRadius={battleRadius:F2}");
    }

    private bool ShouldLogDiagnostics()
    {
        if (!logScanDiagnostics) return false;
        if (Time.time < nextDiagnosticLogTime) return false;

        nextDiagnosticLogTime = Time.time + 1f;
        return true;
    }

    [ContextMenu("Log Battle Candidates")]
    public void LogBattleCandidates()
    {
        if (ArmyManager.Instance == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[ArmyBattleManager] ArmyManager.Instance is null.");
            return;
        }

        List<Army> armies = ArmyManager.Instance.GetAllArmies();
        GameLog.Log(GameLogCategory.Core, $"[ArmyBattleManager] Registered armies: {armies.Count}");

        for (int i = 0; i < armies.Count; i++)
        {
            Army army = armies[i];
            GameLog.Log(GameLogCategory.Core, $"  Army {i}: {DescribeArmy(army)}");
        }

        for (int i = 0; i < armies.Count; i++)
        {
            for (int j = i + 1; j < armies.Count; j++)
            {
                Army armyA = armies[i];
                Army armyB = armies[j];
                if (armyA == null || armyB == null) continue;

                float distance = Vector3.Distance(armyA.transform.position, armyB.transform.position);
                if (distance > diagnosticRadius) continue;

                GameLog.Log(GameLogCategory.Core, 
                    $"  Pair: {DescribeArmy(armyA)} <-> {DescribeArmy(armyB)} | " +
                    $"distance={distance:F2}, hostile={AreHostile(armyA, armyB)}, " +
                    $"canA={CanStartBattle(armyA)}, canB={CanStartBattle(armyB)}");
            }
        }
    }

    private string DescribeArmy(Army army)
    {
        if (army == null) return "null";

        string owner = army.OwnerNation != null ? army.OwnerNation.nationName : "NO_OWNER";
        return $"{army.name}/{army.Data.armyName} owner={owner} isPlayer={army.IsPlayerArmy} size={army.ArmySize:F0} inBattle={army.IsInBattle}";
    }

    private void StartBattle(Army armyA, Army armyB)
    {
        int battleId = GetBattleId(armyA, armyB);
        if (activeBattles.ContainsKey(battleId)) return;

        ArmyBattleState state = new ArmyBattleState
        {
            armyA = armyA,
            armyB = armyB
        };

        activeBattles.Add(battleId, state);
        battleByArmy[armyA] = battleId;
        battleByArmy[armyB] = battleId;
        armyA.SetBattleState(true);
        armyB.SetBattleState(true);

        GameEvents.ArmyBattleStarted(armyA, armyB);
    }

    private void UpdateBattles()
    {
        if (activeBattles.Count == 0) return;

        battleIdBuffer.Clear();
        battleIdBuffer.AddRange(activeBattles.Keys);
        foreach (int battleId in battleIdBuffer)
        {
            if (!activeBattles.TryGetValue(battleId, out ArmyBattleState state)) continue;

            ArmyBattleEndReason? endReason = GetEndReason(state);
            if (endReason.HasValue)
            {
                EndBattle(battleId, state, endReason.Value);
                continue;
            }

            state.elapsed += Time.deltaTime;
            if (state.elapsed >= battleTickSeconds)
            {
                state.elapsed = 0f;
                ResolveBattleTick(battleId, state);
            }
        }
    }

    private ArmyBattleEndReason? GetEndReason(ArmyBattleState state)
    {
        if (state == null || state.armyA == null || state.armyB == null)
            return ArmyBattleEndReason.Invalid;

        if (state.armyA.IsCaptured || state.armyB.IsCaptured || state.armyA.IsRetreating || state.armyB.IsRetreating)
            return ArmyBattleEndReason.Invalid;

        if (state.armyA.ArmySize <= minCombatantSize || state.armyB.ArmySize <= minCombatantSize)
            return ArmyBattleEndReason.Defeated;

        float retreatRadius = battleRadius * retreatRadiusMultiplier;
        float retreatRadiusSqr = retreatRadius * retreatRadius;
        float distanceSqr = (state.armyA.transform.position - state.armyB.transform.position).sqrMagnitude;
        if (distanceSqr > retreatRadiusSqr)
            return ArmyBattleEndReason.Retreated;

        return null;
    }

    private void ResolveBattleTick(int battleId, ArmyBattleState state)
    {
        if (state.armyA == null || state.armyB == null) return;

        state.turn++;

        int armyARoll = Random.Range(1, 7);
        int armyBRoll = Random.Range(1, 7);
        float armyAPower = state.armyA.GetEffectiveStrength() * armyARoll;
        float armyBPower = state.armyB.GetEffectiveStrength() * armyBRoll;

        float armyALoss;
        float armyBLoss;

        if (armyAPower > armyBPower)
        {
            armyALoss = state.armyA.ArmySize * winnerLossPercent;
            armyBLoss = state.armyB.ArmySize * loserLossPercent;
            RewardWinner(state.armyA);
        }
        else if (armyBPower > armyAPower)
        {
            armyALoss = state.armyA.ArmySize * loserLossPercent;
            armyBLoss = state.armyB.ArmySize * winnerLossPercent;
            RewardWinner(state.armyB);
        }
        else
        {
            armyALoss = state.armyA.ArmySize * drawLossPercent;
            armyBLoss = state.armyB.ArmySize * drawLossPercent;
        }

        state.armyA.SetArmySize(state.armyA.ArmySize - armyALoss);
        state.armyB.SetArmySize(state.armyB.ArmySize - armyBLoss);

        GameEvents.ArmyBattleTick(state.armyA, state.armyB, armyALoss, armyBLoss, armyARoll, armyBRoll, state.turn);

        ArmyBattleEndReason? endReason = GetEndReason(state);
        if (endReason.HasValue && activeBattles.ContainsKey(battleId))
        {
            EndBattle(battleId, state, endReason.Value);
        }
    }

    private void RewardWinner(Army winner)
    {
        if (winner == null) return;

        winner.GainExperience(qualityReward);

        TroopLevel troopLevel = winner.GetComponent<TroopLevel>();
        if (troopLevel != null)
        {
            troopLevel.GainXP(troopLevelXpReward);
        }
    }

    private void EndBattle(int battleId, ArmyBattleState state, ArmyBattleEndReason reason)
    {
        Army winner = GetWinner(state, reason);
        Army loser = GetLoser(state, winner);

        ClearBattle(battleId, state);
        GameEvents.ArmyBattleEnded(winner, loser, reason);

        if (reason == ArmyBattleEndReason.Defeated && loser != null)
        {
            loser.ResolveDefeatAftermath(winner);
            GameEvents.ArmyDefeated(loser);
        }
    }

    private Army GetWinner(ArmyBattleState state, ArmyBattleEndReason reason)
    {
        if (state == null) return null;
        if (state.armyA == null) return state.armyB;
        if (state.armyB == null) return state.armyA;
        if (reason == ArmyBattleEndReason.Retreated) return null;

        return state.armyA.ArmySize >= state.armyB.ArmySize ? state.armyA : state.armyB;
    }

    private Army GetLoser(ArmyBattleState state, Army winner)
    {
        if (state == null) return null;
        if (winner == state.armyA) return state.armyB;
        if (winner == state.armyB) return state.armyA;

        if (state.armyA == null) return state.armyB;
        if (state.armyB == null) return state.armyA;
        return state.armyA.ArmySize <= state.armyB.ArmySize ? state.armyA : state.armyB;
    }

    private void ClearBattle(int battleId, ArmyBattleState state)
    {
        activeBattles.Remove(battleId);

        if (state?.armyA != null)
        {
            state.armyA.SetBattleState(false);
            battleByArmy.Remove(state.armyA);
        }

        if (state?.armyB != null)
        {
            state.armyB.SetBattleState(false);
            battleByArmy.Remove(state.armyB);
        }
    }

    private int GetBattleId(Army armyA, Army armyB)
    {
        int idA = armyA.GetInstanceID();
        int idB = armyB.GetInstanceID();
        if (idA > idB)
        {
            int temp = idA;
            idA = idB;
            idB = temp;
        }

        unchecked
        {
            return (idA * 397) ^ idB;
        }
    }
}
