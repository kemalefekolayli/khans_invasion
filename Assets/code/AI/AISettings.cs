using UnityEngine;

[CreateAssetMenu(fileName = "AISettings", menuName = "AI/AISettings")]
public class AISettings : ScriptableObject
{
    [Header("AI Decision Telemetry")]
    [Tooltip("Record one bounded decision snapshot per AI nation after its turn action completes.")]
    public bool EnableDecisionTelemetry = false;
    [Tooltip("Emit each compact telemetry JSON record through GameLog in addition to retaining it in memory.")]
    public bool EmitDecisionTelemetryToGameLog = true;
    [Tooltip("Maximum number of decision records retained in memory.")]
    [Min(1)] public int DecisionTelemetryBufferCapacity = 512;

    [Header("Aggression Scale")]
    [Tooltip("Lowest AI aggression level.")]
    public int AggressionMin = 1;
    [Tooltip("Highest AI aggression level.")]
    public int AggressionMax = 6;
    [Tooltip("Mean used when generating aggression procedurally.")]
    public float AggressionMean = 3.5f;
    [Tooltip("Standard deviation used when generating aggression procedurally.")]
    public float AggressionStdDev = 1.1f;

    [Header("Military Readiness")]
    [Tooltip("Weighted strength ratio required before AI considers real attacks.")]
    public float AttackReadinessRatio = 1.2f;
    [Tooltip("Ratio considered strong enough to raise effective aggression.")]
    public float StrongReadinessRatio = 1.5f;
    [Tooltip("Ratio considered weak enough to lower effective aggression.")]
    public float WeakReadinessRatio = 0.8f;
    [Tooltip("How much the player's strength counts in neighbor comparisons.")]
    public float PlayerNeighborStrengthWeight = 0.35f;
    [Tooltip("How attractive the player is as a target compared to AI nations.")]
    public float PlayerTargetScoreMultiplier = 0.35f;
    [Tooltip("Minimum total troops before an AI can attack.")]
    public float MinTroopsBeforeAttack = 500f;
    [Tooltip("Minimum total troops before an AI can raid.")]
    public float MinTroopsBeforeRaid = 100f;
    [Tooltip("Minimum army count before an AI can attack.")]
    public int MinArmiesBeforeAttack = 1;
    [Tooltip("Weighted strength ratio required before AI considers raids.")]
    public float RaidReadinessRatio = 0.75f;

    [Header("Dynamic Aggression")]
    [Tooltip("Temporary aggression bonus when the nation is strong.")]
    public int StrongAggressionBonus = 1;
    [Tooltip("Temporary aggression penalty when the nation is weak.")]
    public int WeakAggressionPenalty = 1;

    [Header("War Behavior Thresholds")]
    public int ScoutAggressionLevel = 2;
    public int AttackAggressionLevel = 3;
    public int RaidAggressionLevel = 3;
    public int SiegeAggressionLevel = 6;

    [Header("AI Raiding")]
    [Tooltip("Allow AI armies to raid enemy provinces without requiring a General component.")]
    public bool EnableAIRaids = true;
    [Tooltip("Global raid loot multiplier. 0.4 means raids take 40% of the normal raid amount.")]
    [Range(0f, 1f)]
    public float AIRaidEffectiveness = 0.4f;
    [Tooltip("If false, provinces with a Fortress cannot be raided.")]
    public bool AllowRaidingFortressProvince = false;
    [Tooltip("How many successful raids against the same defender before conquest pressure starts.")]
    public int RaidsBeforeSiegeConsideration = 2;
    [Tooltip("How many turns raid pressure remains relevant.")]
    public int RaidPressureMemoryTurns = 6;
    [Tooltip("Minimum aggression required before raid pressure can escalate into conquest.")]
    public int RaidToSiegeAggressionMin = 4;
    [Tooltip("Minimum attacker/defender province ratio before AI starts taking land.")]
    [Range(0f, 1f)]
    public float ConquestProvinceRatioThreshold = 0.3f;
    [Tooltip("Minimum number of provinces attacker must have before conquest escalation can start.")]
    public int ConquestMinimumProvinceCount = 5;
    [Tooltip("Minimum military readiness ratio for AI conquest escalation.")]
    public float ConquestReadinessRatio = 0.9f;
    [Tooltip("If true, AI can recruit from owned provinces even before building barracks. Useful while testing AI tempo.")]
    public bool AllowAIFieldRecruitment = true;
    [Tooltip("Population fraction drafted from each recruitment province per AI turn.")]
    [Range(0.01f, 0.5f)]
    public float AIRecruitPopulationFraction = 0.18f;
    [Tooltip("Maximum troops recruited from one province per AI turn.")]
    public float AIRecruitMaxPerProvince = 350f;

    [Header("War Target Scoring")]
    public float EnemyWeaknessTargetWeight = 50f;
    public float EnemyRichnessTargetWeight = 1f;
    public float CapitalProvinceAttackBonus = 100f;
    public float BorderProvinceAttackBonus = 25f;

    [Header("AI Order Budgets")]
    [Tooltip("Maximum armies this nation can move during one AI turn.")]
    public int MaxArmiesMovedPerNationPerTurn = 3;
    [Tooltip("Maximum attack orders this nation can issue during one AI turn.")]
    public int MaxAttackOrdersPerNationPerTurn = 2;
    [Tooltip("Log aggression and war intent decisions.")]
    public bool LogWarIntent = true;

    [Header("AI Tempo")]
    [Tooltip("Starting army size created for each AI nation.")]
    public float AIStartingArmySize = 140f;
    [Tooltip("Maximum buildings an AI nation can construct during one turn.")]
    public int MaxBuildingsPerNationPerTurn = 2;
    [Tooltip("Gold AI keeps available before starting a development or fortification build loop.")]
    public float DevelopmentGoldReserve = 80f;

    [Header("Recruitment")]
    [Tooltip("AI nations below this treasury are topped up when AI initializes.")]
    public float AIStartingTreasury = 300f;
    [Tooltip("Desired max troops per army before creating a new one.")]
    public float MaxArmySize = 1000f;
    [Tooltip("Hard limit for active armies per AI nation. Recruitment reinforces existing armies before creating another.")]
    public int MaxArmiesPerNation = 4;
    [Tooltip("Minimum gold to keep in reserve when recruiting.")]
    public float RecruitmentGoldReserve = 50f;

    [Header("Building Priorities")]
    [Tooltip("Base score for building a Barracks.")]
    public float BaseBarracksScore = 15f;
    [Tooltip("Base score for building Housing/Farms.")]
    public float BaseHousingScore = 10f;
    [Tooltip("Base score for building a Fortress.")]
    public float BaseFortressScore = 5f;
    [Tooltip("Base score for building a Farm.")]
    public float BaseFarmScore = 12f;
    [Tooltip("Base score for building Trade/Market.")]
    public float BaseTradeScore = 14f;

    [Header("Logic Thresholds")]
    [Tooltip("If current pop is > this % of max pop, prioritize Housing.")]
    [Range(0f, 1f)]
    public float PopulationSaturationThreshold = 0.9f;
    
    [Tooltip("If total armies is 0, boost Barracks score by this multiplier.")]
    public float NoArmyBarracksMultiplier = 5.0f;

    [Tooltip("Score penalty per unit distance from Capital (for Developing state).")]
    public float DistanceToCapitalPenalty = 2.0f;

    [Tooltip("Weight for province importance (Pop + Income) in Fortifying state.")]
    public float ProvinceImportanceWeight = 0.5f;
}
