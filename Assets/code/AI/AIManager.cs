using UnityEngine;
using System.Collections.Generic;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logAISummary = true;

    [Header("Settings")]
    public AISettings Settings;

    private List<AINationController> aiNations = new List<AINationController>();
    public List<AINationController> AINations => aiNations; // For Debugger
    private bool initialized = false;
    private readonly AIWorldIntelCache worldIntelCache = new AIWorldIntelCache();
    private readonly AIDecisionTelemetryBuffer decisionTelemetry = new AIDecisionTelemetryBuffer();
    private readonly List<AIRaidPressure> raidPressures = new List<AIRaidPressure>();
    private int totalAIRaids;
    private int totalAIConquests;
    private float totalAIRaidLoot;

    public readonly struct AIActivityMetrics
    {
        public readonly int RaidCount;
        public readonly int ConquestCount;
        public readonly float RaidLoot;

        public AIActivityMetrics(int raidCount, int conquestCount, float raidLoot)
        {
            RaidCount = raidCount;
            ConquestCount = conquestCount;
            RaidLoot = raidLoot;
        }
    }

    private class AIRaidPressure
    {
        public NationModel raider;
        public NationModel defender;
        public ProvinceModel province;
        public int raidCount;
        public int lastRaidTurn;
        public float totalLootTaken;
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
            return;
        }
    }

    private void OnEnable()
    {
        TurnManager.OnAITurnsStart += OnAITurnsStart;
        GameEvents.OnPlayerNationReady += OnPlayerNationReady;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        GameEvents.OnProvinceConquered += OnProvinceConquered;
        GameEvents.OnArmySpawned += OnArmySpawned;
        GameEvents.OnArmyDestroyed += OnArmyDestroyed;
        GameEvents.OnArmySizeChanged += OnArmySizeChanged;
    }

    private void OnDisable()
    {
        TurnManager.OnAITurnsStart -= OnAITurnsStart;
        GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
        GameEvents.OnArmySpawned -= OnArmySpawned;
        GameEvents.OnArmyDestroyed -= OnArmyDestroyed;
        GameEvents.OnArmySizeChanged -= OnArmySizeChanged;
    }

    private void OnPlayerNationReady()
    {
        if (initialized) return;
        InitializeAINations();
    }

    private void InitializeAINations()
    {
        NationLoader loader = FindFirstObjectByType<NationLoader>();
        if (loader == null)
        {
            GameLog.Error(GameLogCategory.AI, "[AIManager] NationLoader not found!");
            return;
        }

        aiNations.Clear();

        foreach (NationModel nation in loader.allNations)
        {
            if (nation.isPlayer) continue;
            if (nation.provinceList == null || nation.provinceList.Count == 0) continue;

            float startingTreasury = Settings != null ? Settings.AIStartingTreasury : 300f;
            if (nation.treasury < startingTreasury)
            {
                nation.treasury = startingTreasury;
            }

            // Pass the global settings
            AINationController controller = new AINationController(nation, Settings);
            aiNations.Add(controller);

            // Initialize a configurable starting army.
            if (nation.capitalProvince != null && ArmyFactory.Instance != null)
            {
                Army startingArmy = ArmyFactory.Instance.CreateArmy(nation.capitalProvince.transform.position, Settings != null ? Settings.AIStartingArmySize : 140f, 1.0f, false);
                if (startingArmy != null)
                {
                    startingArmy.OwnerNation = nation;
                    startingArmy.CurrentProvince = nation.capitalProvince;
                    GameLog.Log(GameLogCategory.AI, $"[AIManager] Created starting army for {nation.nationName} at {nation.capitalProvince.provinceName}");
                }
            }
            else if (nation.provinceList.Count > 0 && ArmyFactory.Instance != null)
            {
                 // Fallback if no capital
                 var randomProv = nation.provinceList[0];
                 Army startingArmy = ArmyFactory.Instance.CreateArmy(randomProv.transform.position, Settings != null ? Settings.AIStartingArmySize : 140f, 1.0f, false);
                 if (startingArmy != null)
                 {
                     startingArmy.OwnerNation = nation;
                     startingArmy.CurrentProvince = randomProv;
                     GameLog.Log(GameLogCategory.AI, $"[AIManager] Created starting army for {nation.nationName} at {randomProv.provinceName} (No Capital)");
                 }
            }
        }

        initialized = true;
        worldIntelCache.MarkBordersDirty();
        worldIntelCache.MarkMilitaryDirty();
        GameLog.Log(GameLogCategory.AI, $"[AIManager] Initialized {aiNations.Count} AI nations");
    }

    private void OnAITurnsStart()
    {
        if (!initialized)
        {
            GameLog.Warning(GameLogCategory.AI, "[AIManager] AI turns started but not initialized yet!");
            return;
        }

        int turnNumber = TurnManager.Instance != null ? TurnManager.Instance.CurrentTurn : 0;

        GameLog.Log(GameLogCategory.AI, $"[AIManager] === Processing AI Turns (Turn {turnNumber}) ===");

        NationLoader loader = FindFirstObjectByType<NationLoader>();
        List<NationModel> allNations = loader != null ? loader.allNations : null;
        if (allNations != null)
        {
            worldIntelCache.RebuildIfNeeded(allNations);
        }
        else
        {
            GameLog.Warning(GameLogCategory.AI, "[AIManager] NationLoader not found while rebuilding AI intel.");
        }

        foreach (AINationController controller in aiNations)
        {
            worldIntelCache.RebuildIfNeeded(allNations);
            TrimExpiredRaidPressure(turnNumber);
            controller.ProcessTurn(turnNumber, worldIntelCache);
            decisionTelemetry.Record(controller, turnNumber, Settings);
        }

        if (logAISummary)
        {
            LogAISummary();
        }
    }

    private void LogAISummary()
    {
        GameLog.Log(GameLogCategory.AI, "[AIManager] === AI Summary ===");
        int expanding = 0, fortifying = 0, idle = 0;

        foreach (AINationController controller in aiNations)
        {
            switch (controller.StateMachine.CurrentState)
            {
                case AIState.Recruiting:
                case AIState.Attacking:
                    expanding++; 
                    break;
                case AIState.Developing: 
                    // Count developing as... something else? or just ignore for summary?
                    // Let's just bundle them conceptually for the log or add new counters
                    break;
                case AIState.Fortifying: fortifying++; break;
                case AIState.Idle: idle++; break;
            }
        }

        GameLog.Log(GameLogCategory.AI, $"[AIManager] Active(Rec/Atk): {expanding} | Fortifying: {fortifying} | Idle: {idle}");
    }

    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        worldIntelCache.MarkBordersDirty();
    }

    private void OnProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        worldIntelCache.MarkBordersDirty();
        if (newOwner != null && !newOwner.isPlayer)
        {
            totalAIConquests++;
        }
    }

    private void OnArmySpawned(Army army, General general)
    {
        worldIntelCache.MarkMilitaryDirty();
    }

    private void OnArmyDestroyed(Army army)
    {
        worldIntelCache.MarkMilitaryDirty();
    }

    private void OnArmySizeChanged(Army army)
    {
        worldIntelCache.MarkMilitaryDirty();
    }

    public void RecordAIRaid(NationModel raider, NationModel defender, ProvinceModel province, float lootTaken)
    {
        if (raider == null || defender == null || province == null) return;

        int turnNumber = TurnManager.Instance != null ? TurnManager.Instance.CurrentTurn : 0;
        AIRaidPressure pressure = raidPressures.Find(p => p.raider == raider && p.defender == defender);
        if (pressure == null)
        {
            pressure = new AIRaidPressure
            {
                raider = raider,
                defender = defender,
                province = province
            };
            raidPressures.Add(pressure);
        }

        pressure.province = province;
        pressure.raidCount++;
        pressure.lastRaidTurn = turnNumber;
        pressure.totalLootTaken += lootTaken;
        totalAIRaids++;
        totalAIRaidLoot += lootTaken;

        GameLog.Log(GameLogCategory.AIWar, $"[AIWar] Raid pressure {raider.nationName}->{defender.nationName}: {pressure.raidCount} raids, {pressure.totalLootTaken:F0} loot.");
    }

    public AIActivityMetrics GetActivityMetrics()
    {
        return new AIActivityMetrics(totalAIRaids, totalAIConquests, totalAIRaidLoot);
    }

    public IReadOnlyList<AIDecisionTelemetryRecord> GetDecisionTelemetrySnapshot()
    {
        return decisionTelemetry.GetSnapshot();
    }

    public string ExportDecisionTelemetryJsonLines()
    {
        return decisionTelemetry.ToJsonLines();
    }

    public bool ShouldEscalateToConquest(NationModel raider, NationModel defender, AIWarContext context)
    {
        if (raider == null || defender == null || context == null) return false;

        AISettings settings = Settings;
        int requiredRaids = settings != null ? settings.RaidsBeforeSiegeConsideration : 2;
        int minAggression = settings != null ? settings.RaidToSiegeAggressionMin : 4;
        int minProvinces = settings != null ? settings.ConquestMinimumProvinceCount : 5;
        float provinceRatioThreshold = settings != null ? settings.ConquestProvinceRatioThreshold : 0.3f;
        float readinessThreshold = settings != null ? settings.ConquestReadinessRatio : 0.9f;

        AIRaidPressure pressure = raidPressures.Find(p => p.raider == raider && p.defender == defender);
        if (pressure == null || pressure.raidCount < requiredRaids) return false;
        if (context.EffectiveAggression < minAggression) return false;
        if (defender.provinceList == null || defender.provinceList.Count == 0) return false;
        if (context.ReadinessRatio < readinessThreshold) return false;

        int raiderProvinceCount = raider.provinceList != null ? raider.provinceList.Count : 0;
        float provinceRatio = raiderProvinceCount / Mathf.Max(1f, defender.provinceList.Count);
        bool hasEnoughLand = raider.provinceList != null
            && (raiderProvinceCount >= minProvinces || provinceRatio >= provinceRatioThreshold);
        return hasEnoughLand;
    }

    public void ClearRaidPressure(NationModel raider, NationModel defender)
    {
        raidPressures.RemoveAll(p => p.raider == raider && p.defender == defender);
    }

    private void TrimExpiredRaidPressure(int turnNumber)
    {
        int memoryTurns = Settings != null ? Settings.RaidPressureMemoryTurns : 6;
        raidPressures.RemoveAll(p => turnNumber - p.lastRaidTurn > memoryTurns);
    }
}
