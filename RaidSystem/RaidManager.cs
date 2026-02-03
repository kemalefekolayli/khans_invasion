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
    
    [Tooltip("Maximum loot percentage at 1000 troops")]
    [SerializeField] private float maxLootPercent = 0.60f; // 60%
    
    [Tooltip("Troop count for minimum loot percentage")]
    [SerializeField] private float minTroopCount = 100f;
    
    [Tooltip("Troop count for maximum loot percentage")]
    [SerializeField] private float maxTroopCount = 1000f;
    
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
            Debug.Log("✓ RaidManager initialized");
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
        
        // Linear interpolation between min and max percentage
        float t = (clampedTroops - minTroopCount) / (maxTroopCount - minTroopCount);
        return Mathf.Lerp(minLootPercent, maxLootPercent, t);
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
        
        // Check if already raided this turn
        if (provincesRaidedThisTurn.Contains(province.provinceId))
        {
            if (logRaidEvents)
                Debug.Log($"[RaidManager] Province {province.provinceName} already raided this turn!");
            return false;
        }
        
        // Check if there's loot available
        if (province.availableLoot <= 0)
        {
            if (logRaidEvents)
                Debug.Log($"[RaidManager] Province {province.provinceName} has no loot available!");
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
            Debug.LogError("[RaidManager] ExecuteRaid called with null province or raider!");
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
            Debug.LogWarning($"[RaidManager] {raider.GeneralName} has no army to raid with!");
            return 0f;
        }
        
        // Calculate loot
        float lootAmount = CalculateLootAmount(province, troopCount);
        
        // Check raider's carrying capacity
        float availableCapacity = raider.MaxLootCapacity - raider.CarriedLoot;
        float actualLoot = Mathf.Min(lootAmount, availableCapacity);
        
        if (actualLoot <= 0)
        {
            if (logRaidEvents)
                Debug.Log($"[RaidManager] {raider.GeneralName} cannot carry any more loot!");
            return 0f;
        }
        
        // Execute the raid
        province.availableLoot -= actualLoot;
        raider.AddLoot(actualLoot);
        
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
            Debug.Log($"[RaidManager] ═══ RAID SUCCESSFUL ═══");
            Debug.Log($"  Raider: {raider.GeneralName} ({troopCount:F0} troops)");
            Debug.Log($"  Province: {province.provinceName}");
            Debug.Log($"  Loot Taken: {actualLoot:F0}");
            Debug.Log($"  Province Loot Remaining: {province.availableLoot:F0}/{maxLoot:F0} ({lootPercent:F0}%)");
            Debug.Log($"  {raider.GeneralName} now carries: {raider.CarriedLoot:F0}/{raider.MaxLootCapacity:F0}");
        }
        
        // Fire raid event
        GameEvents.ProvinceRaided(province, raider, actualLoot);
        
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
            Debug.Log($"[RaidManager] Turn {turnNumber} end - {provincesNeedingRegen.Count} provinces regenerating loot");
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
                Debug.Log($"  {province.provinceName}: +{regenAmount:F0} loot → {lootPercent:F0}%");
            }
        }
        
        // Remove fully recovered provinces from tracking
        foreach (ProvinceModel province in fullyRecovered)
        {
            provincesNeedingRegen.Remove(province);
            
            if (logRaidEvents)
                Debug.Log($"  {province.provinceName}: Fully recovered!");
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
}
