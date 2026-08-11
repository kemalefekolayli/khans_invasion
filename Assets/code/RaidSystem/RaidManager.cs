using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for the raiding system.
/// Tracks which provinces have been raided, handles loot calculations,
/// and manages loot regeneration at end of turn.
/// </summary>
public class RaidManager : MonoBehaviour, ITurnProcessor
{
    public static RaidManager Instance { get; private set; }
    
    [Header("Loot Calculation Settings")]
    [Tooltip("Minimum base loot any province can have (even with 0 income)")]
    [SerializeField] private float minimumBaseLoot = 10f;
    
    [Tooltip("Multiplier for tax income when calculating max loot")]
    [SerializeField] private float taxLootMultiplier = 5f;
    
    [Tooltip("Multiplier for trade income when calculating max loot")]
    [SerializeField] private float tradeLootMultiplier = 6f;
    
    [Header("Troop Scaling Settings")]
    [Tooltip("Minimum loot percentage at 100 troops")]
    [SerializeField] private float minLootPercent = 0.10f; // 10%
    
    [Tooltip("Maximum loot percentage at the maximum troop anchor")]
    [SerializeField] private float maxLootPercent = 0.725f; // 72.5%
    
    [Tooltip("Troop count for minimum loot percentage")]
    [SerializeField] private float minTroopCount = 100f;
    
    [Tooltip("Troop count for maximum loot percentage")]
    [SerializeField] private float maxTroopCount = 500f;
    [Tooltip("Below 1 makes loot gains rise faster between the troop anchors")]
    [SerializeField, Min(0.01f)] private float lootCurveExponent = 0.8f;
    [Tooltip("Player-only multiplier applied after the troop loot curve")]
    [SerializeField, Min(0f)] private float playerRaidEffectiveness = 1f;

    [Header("Player Raid Casualties")]
    [SerializeField, Min(0)] private int minimumCasualtiesAtMinTroops = 5;
    [SerializeField, Min(0)] private int maximumCasualtiesAtMinTroops = 15;
    [SerializeField, Min(0)] private int minimumCasualtiesAtMaxTroops = 10;
    [SerializeField, Min(0)] private int maximumCasualtiesAtMaxTroops = 50;
    
    [Header("Regeneration Settings")]
    [Tooltip("Regeneration rate per turn (percentage of missing loot - higher = faster recovery)")]
    [SerializeField] private float regenRate = 0.40f; // ~40% of missing loot recovered per turn
    
    [Header("Debug")]
    [SerializeField] private bool logRaidEvents = true;
    
    // Tracks provinces raided this turn (provinceId -> true)
    private HashSet<long> provincesRaidedThisTurn = new HashSet<long>();
    
    // Tracks provinces that need regeneration (not at 100%)
    private List<ProvinceModel> provincesNeedingRegen = new List<ProvinceModel>();
    
    // ITurnProcessor
    public int ProcessingPriority => 10; // After income
    
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
    
    private void Start()
    {
        // Register with TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterTurnProcessor(this);
        }
    }
    
    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterTurnProcessor(this);
        }
    }
    
    #region Loot Calculation
    
    /// <summary>
    /// Calculate the maximum loot a province can hold based on its income.
    /// Formula: max(minimumBaseLoot, taxIncome * 5 + tradeIncome * 6)
    /// </summary>
    public float CalculateMaxLoot(ProvinceModel province)
    {
        if (province == null) return 0f;
        
        float incomeLoot = (province.provinceTaxIncome * taxLootMultiplier) + 
                          (province.provinceTradePower * tradeLootMultiplier);
        
        // Ensure minimum base loot so all provinces have something to raid
        return Mathf.Max(minimumBaseLoot, incomeLoot);
    }
    
    /// <summary>
    /// Calculate what percentage of available loot can be taken based on troop count.
    /// 100 troops = 10%, 1000 troops = 60%, linear interpolation between.
    /// </summary>
    public float CalculateLootPercentage(float troopCount)
    {
        // Clamp troop count to valid range
        float clampedTroops = Mathf.Clamp(troopCount, minTroopCount, maxTroopCount);
        
        float range = Mathf.Max(0.001f, maxTroopCount - minTroopCount);
        float t = (clampedTroops - minTroopCount) / range;
        return Mathf.Lerp(minLootPercent, maxLootPercent, Mathf.Pow(t, lootCurveExponent));
    }
    
    /// <summary>
    /// Calculate actual loot amount for a raid.
    /// </summary>
    public float CalculateLootAmount(ProvinceModel province, float troopCount)
    {
        if (province == null) return 0f;
        
        float lootPercent = CalculateLootPercentage(troopCount);
        float actualLoot = province.availableLoot * lootPercent;
        
        return actualLoot;
    }
    
    #endregion
    
    #region Raid Execution
    
    /// <summary>
    /// Check if a province can be raided this turn.
    /// </summary>
    public bool CanRaidProvince(ProvinceModel province)
    {
        if (province == null) return false;

        bool allowFortressRaids = AIManager.Instance != null
            && AIManager.Instance.Settings != null
            && AIManager.Instance.Settings.AllowRaidingFortressProvince;
        if (!allowFortressRaids && province.buildings != null && province.buildings.Contains("Fortress"))
        {
            if (logRaidEvents)
                GameLog.Log(GameLogCategory.AIWar, $"[RaidManager] {province.provinceName} has a Fortress and cannot be raided.");
            return false;
        }
        
        // Check if already raided this turn
        if (provincesRaidedThisTurn.Contains(province.provinceId))
        {
            if (logRaidEvents)
                GameLog.Log(GameLogCategory.Core, $"[RaidManager] Province {province.provinceName} already raided this turn!");
            return false;
        }
        
        // Check if there's loot available
        if (province.availableLoot <= 0)
        {
            if (logRaidEvents)
                GameLog.Log(GameLogCategory.Core, $"[RaidManager] Province {province.provinceName} has no loot available!");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Execute a raid on a province.
    /// Returns the amount of loot gained.
    /// </summary>
    public float ExecuteRaid(ProvinceModel province, General raider)
    {
        if (province == null || raider == null)
        {
            GameLog.Error(GameLogCategory.Core, "[RaidManager] ExecuteRaid called with null province or raider!");
            return 0f;
        }
        
        if (!CanRaidProvince(province))
        {
            return 0f;
        }
        
        // Get troop count from raider's army
        float troopCount = 0f;
        if (raider.HasArmy)
        {
            troopCount = raider.CommandedArmy.ArmySize;
        }
        else
        {
            GameLog.Warning(GameLogCategory.Core, $"[RaidManager] {raider.GeneralName} has no army to raid with!");
            return 0f;
        }
        
        // Calculate loot
        float lootAmount = CalculatePlayerLootAmount(province, troopCount);
        
        // Check raider's carrying capacity
        float availableCapacity = raider.MaxLootCapacity - raider.CarriedLoot;
        float actualLoot = Mathf.Min(lootAmount, availableCapacity);
        
        if (actualLoot <= 0)
        {
            if (logRaidEvents)
                GameLog.Log(GameLogCategory.Core, $"[RaidManager] {raider.GeneralName} cannot carry any more loot!");
            return 0f;
        }
        
        // Execute the raid
        province.availableLoot -= actualLoot;
        raider.AddLoot(actualLoot);
        ApplyPlayerRaidCasualties(raider, troopCount);
        
        // Mark province as raided this turn
        provincesRaidedThisTurn.Add(province.provinceId);
        
        // Add to regeneration list if not already there
        if (!provincesNeedingRegen.Contains(province))
        {
            provincesNeedingRegen.Add(province);
        }
        
        if (logRaidEvents)
        {
            float maxLoot = CalculateMaxLoot(province);
            float lootPercent = (province.availableLoot / maxLoot) * 100f;
            GameLog.Log(GameLogCategory.Core, $"[RaidManager] ═══ RAID SUCCESSFUL ═══");
            GameLog.Log(GameLogCategory.Core, $"  Raider: {raider.GeneralName} ({troopCount:F0} troops)");
            GameLog.Log(GameLogCategory.Core, $"  Province: {province.provinceName}");
            GameLog.Log(GameLogCategory.Core, $"  Loot Taken: {actualLoot:F0}");
            GameLog.Log(GameLogCategory.Core, $"  Province Loot Remaining: {province.availableLoot:F0}/{maxLoot:F0} ({lootPercent:F0}%)");
            GameLog.Log(GameLogCategory.Core, $"  {raider.GeneralName} now carries: {raider.CarriedLoot:F0}/{raider.MaxLootCapacity:F0}");
        }
        
        // Fire raid event
        GameEvents.ProvinceRaided(province, raider, actualLoot);
        NationModel playerNation = PlayerNation.Instance?.currentNation;
        if (playerNation != null && raider.CommandedArmy != null && raider.CommandedArmy.IsPlayerArmy)
            GameEvents.RecordCityOperation(playerNation, province, CityOperationType.Raid, raider);
        
        return actualLoot;
    }

    public float CalculatePlayerLootAmount(ProvinceModel province, float troopCount)
    {
        return CalculateLootAmount(province, troopCount) * playerRaidEffectiveness;
    }

    public float ExecuteRaid(ProvinceModel province, Army raiderArmy)
    {
        if (province == null || raiderArmy == null)
        {
            GameLog.Error(GameLogCategory.Raid, "[RaidManager] AI ExecuteRaid called with null province or army!");
            return 0f;
        }

        if (!CanRaidProvince(province))
        {
            return 0f;
        }

        float actualLoot = CalculateLootAmount(province, raiderArmy.ArmySize) * GetRaidEffectiveness();
        if (actualLoot <= 0f)
        {
            return 0f;
        }

        province.availableLoot = Mathf.Max(0f, province.availableLoot - actualLoot);

        NationModel raiderNation = raiderArmy.OwnerNation;
        if (raiderNation != null)
        {
            raiderNation.treasury += actualLoot;
        }

        provincesRaidedThisTurn.Add(province.provinceId);

        if (!provincesNeedingRegen.Contains(province))
        {
            provincesNeedingRegen.Add(province);
        }

        if (logRaidEvents)
        {
            string nationName = raiderNation != null ? raiderNation.nationName : "Unknown";
            GameLog.Log(GameLogCategory.AIWar, $"[AI Raid] {nationName} raided {province.provinceName} for {actualLoot:F0} gold with {raiderArmy.ArmySize:F0} troops.");
        }

        if (AIManager.Instance != null)
        {
            AIManager.Instance.RecordAIRaid(raiderNation, province.provinceOwner, province, actualLoot);
        }

        GameEvents.ProvinceRaided(province, null, actualLoot);
        return actualLoot;
    }
    
    #endregion
    
    #region Turn Processing
    
    /// <summary>
    /// Called at end of each turn. Handles loot regeneration.
    /// </summary>
    public void ProcessTurnEnd(int turnNumber)
    {
        // Clear raid cooldowns for new turn
        provincesRaidedThisTurn.Clear();
        
        // Regenerate loot for damaged provinces
        RegenerateProvinceLoot();
        
        if (logRaidEvents)
        {
            GameLog.Log(GameLogCategory.Core, $"[RaidManager] Turn {turnNumber} end - {provincesNeedingRegen.Count} provinces regenerating loot");
        }
    }
    
    /// <summary>
    /// Regenerate loot for provinces that have been raided.
    /// Uses exponential approach: recovers ~25% of missing loot per turn.
    /// At this rate, 4-5 turns to reach ~80-90% recovery.
    /// </summary>
    private void RegenerateProvinceLoot()
    {
        List<ProvinceModel> fullyRecovered = new List<ProvinceModel>();
        
        foreach (ProvinceModel province in provincesNeedingRegen)
        {
            if (province == null) continue;
            
            float maxLoot = CalculateMaxLoot(province);
            float missingLoot = maxLoot - province.availableLoot;
            
            if (missingLoot <= 0.1f) // Essentially full
            {
                province.availableLoot = maxLoot;
                fullyRecovered.Add(province);
                continue;
            }
            
            // Exponential regeneration: recover percentage of missing loot
            float regenAmount = missingLoot * regenRate;
            province.availableLoot += regenAmount;
            
            // Clamp to max
            province.availableLoot = Mathf.Min(province.availableLoot, maxLoot);
            
            // Check if now full
            if (province.availableLoot >= maxLoot - 0.1f)
            {
                province.availableLoot = maxLoot;
                fullyRecovered.Add(province);
            }
            
            if (logRaidEvents)
            {
                float lootPercent = (province.availableLoot / maxLoot) * 100f;
                GameLog.Log(GameLogCategory.Core, $"  {province.provinceName}: +{regenAmount:F0} loot → {lootPercent:F0}%");
            }
        }
        
        // Remove fully recovered provinces from tracking
        foreach (ProvinceModel province in fullyRecovered)
        {
            provincesNeedingRegen.Remove(province);
            
            if (logRaidEvents)
                GameLog.Log(GameLogCategory.Core, $"  {province.provinceName}: Fully recovered!");
        }
    }
    
    #endregion
    
    #region Province Initialization
    
    /// <summary>
    /// Initialize a province's loot to max based on its income.
    /// Call this when provinces are first loaded.
    /// </summary>
    public void InitializeProvinceLoot(ProvinceModel province)
    {
        if (province == null) return;
        
        float maxLoot = CalculateMaxLoot(province);
        province.availableLoot = maxLoot;
    }
    
    /// <summary>
    /// Get the loot percentage of a province (0-1).
    /// If province hasn't been raided (not in provincesNeedingRegen), ensure 100%.
    /// </summary>
    public float GetLootPercentage(ProvinceModel province)
    {
        if (province == null) return 0f;
        
        float maxLoot = CalculateMaxLoot(province);
        if (maxLoot <= 0) return 0f;
        
        // If province hasn't been raided recently, ensure it's at full loot
        // This handles the case where income increases after initialization
        if (!provincesNeedingRegen.Contains(province))
        {
            // Province hasn't been raided - should be at 100%
            if (province.availableLoot < maxLoot)
            {
                province.availableLoot = maxLoot;
            }
            return 1f;
        }
        
        // Province is recovering from raid - show actual percentage
        float percent = province.availableLoot / maxLoot;
        return Mathf.Clamp01(percent);
    }
    
    #endregion

    private float GetRaidEffectiveness()
    {
        return AIManager.Instance != null && AIManager.Instance.Settings != null
            ? Mathf.Clamp01(AIManager.Instance.Settings.AIRaidEffectiveness)
            : 0.4f;
    }

    private void ApplyPlayerRaidCasualties(General raider, float troopCount)
    {
        Army army = raider?.CommandedArmy;
        if (army == null || army.ArmySize <= 1f) return;

        float range = Mathf.Max(0.001f, maxTroopCount - minTroopCount);
        float t = Mathf.Clamp01((troopCount - minTroopCount) / range);
        int minimum = Mathf.RoundToInt(Mathf.Lerp(minimumCasualtiesAtMinTroops, minimumCasualtiesAtMaxTroops, t));
        int maximum = Mathf.RoundToInt(Mathf.Lerp(maximumCasualtiesAtMinTroops, maximumCasualtiesAtMaxTroops, t));
        maximum = Mathf.Max(minimum, maximum);
        float mean = (minimum + maximum) * 0.5f;
        float standardDeviation = Mathf.Max(0.1f, (maximum - minimum) / 6f);
        int casualties = Mathf.RoundToInt(Mathf.Clamp(SampleNormal(mean, standardDeviation), minimum, maximum));
        casualties = Mathf.Clamp(casualties, 0, Mathf.Max(0, Mathf.FloorToInt(army.ArmySize) - 1));
        if (casualties <= 0) return;

        army.RemoveSoldiers(casualties);
        if (logRaidEvents)
            GameLog.Log(GameLogCategory.Raid, $"[RaidManager] {raider.GeneralName} lost {casualties} troops while raiding.");
    }

    private static float SampleNormal(float mean, float standardDeviation)
    {
        float u1 = Mathf.Max(0.0001f, Random.value);
        float u2 = Random.value;
        float standardNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        return mean + standardDeviation * standardNormal;
    }
}
