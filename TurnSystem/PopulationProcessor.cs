using UnityEngine;

/// <summary>
/// Handles population growth at the end of each turn.
/// Growth rate is based on the ratio of max population to current population.
/// Higher ratio = more room to grow = faster growth.
/// 
/// Growth Formula:
///   ratio = maxPop / (currentPop + 1)  (adding 1 to avoid divide by zero)
///   ratio is clamped to maxGrowthRatio (default 2.5)
///   growthPercent = lerp(minGrowthRate, maxGrowthRate, (ratio - 1) / (maxGrowthRatio - 1))
///   newPop = currentPop * (1 + growthPercent)
///   newPop is capped at maxPop
/// 
/// SETUP: Add to a persistent GameObject (like PlayerNation or TurnManager).
/// </summary>
public class PopulationProcessor : MonoBehaviour, ITurnProcessor
{
    [Header("Growth Settings")]
    [Tooltip("Minimum population growth per turn (when near max capacity)")]
    [SerializeField] private float minGrowthRate = 0.01f; // 1%
    
    [Tooltip("Maximum population growth per turn (when far below max capacity)")]
    [SerializeField] private float maxGrowthRate = 0.10f; // 10%
    
    [Tooltip("Maximum ratio of maxPop/currentPop for growth calculation")]
    [SerializeField] private float maxGrowthRatio = 2.5f;
    
    [Header("Processing")]
    [Tooltip("Only grow population in player-owned provinces")]
    [SerializeField] private bool playerProvincesOnly = true;
    
    [Tooltip("Also grow population in all provinces (for simulation)")]
    [SerializeField] private bool growAllProvinces = false;
    
    [Header("Debug")]
    [SerializeField] private bool logGrowth = false;
    [SerializeField] private bool logSummary = true;
    
    // ITurnProcessor implementation
    public int ProcessingPriority => 10; // After income (0), before other processors
    
    private void Start()
    {
        // Register with TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterTurnProcessor(this);
            Debug.Log("✓ PopulationProcessor registered with TurnManager");
        }
    }
    
    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterTurnProcessor(this);
        }
    }
    
    /// <summary>
    /// Called by TurnManager at end of each turn.
    /// </summary>
    public void ProcessTurnEnd(int turnNumber)
    {
        int provincesGrown = 0;
        float totalGrowth = 0f;
        
        if (playerProvincesOnly || !growAllProvinces)
        {
            // Grow only player provinces
            PlayerNation player = PlayerNation.Instance;
            if (player?.currentNation?.provinceList != null)
            {
                foreach (ProvinceModel province in player.currentNation.provinceList)
                {
                    float growth = GrowPopulation(province);
                    if (growth > 0)
                    {
                        provincesGrown++;
                        totalGrowth += growth;
                    }
                }
            }
        }
        
        if (growAllProvinces)
        {
            // Grow all provinces (for simulation)
            ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
            foreach (ProvinceModel province in allProvinces)
            {
                // Skip if already processed as player province
                if (playerProvincesOnly && province.provinceOwner == PlayerNation.Instance?.currentNation)
                    continue;
                    
                float growth = GrowPopulation(province);
                if (growth > 0)
                {
                    provincesGrown++;
                    totalGrowth += growth;
                }
            }
        }
        
        if (logSummary && provincesGrown > 0)
        {
            Debug.Log($"[PopulationProcessor] Turn {turnNumber}: {provincesGrown} provinces grew by total {totalGrowth:F0} population");
        }
    }
    
    /// <summary>
    /// Grow population in a single province.
    /// Returns the amount of growth (0 if at max capacity).
    /// </summary>
    private float GrowPopulation(ProvinceModel province)
    {
        if (province == null) return 0f;
        
        float currentPop = province.provinceCurrentPop;
        float maxPop = province.provinceMaxPop;
        
        // Already at max capacity
        if (currentPop >= maxPop)
        {
            return 0f;
        }
        
        // Calculate ratio (add 1 to avoid divide by zero)
        float ratio = maxPop / (currentPop + 1f);
        
        // Clamp ratio to max
        ratio = Mathf.Min(ratio, maxGrowthRatio);
        
        // Calculate growth percentage based on ratio
        // ratio of 1 = min growth, ratio of maxGrowthRatio = max growth
        float t = (ratio - 1f) / (maxGrowthRatio - 1f);
        t = Mathf.Clamp01(t);
        float growthPercent = Mathf.Lerp(minGrowthRate, maxGrowthRate, t);
        
        // Calculate actual growth
        float growth = currentPop * growthPercent;
        
        // Apply growth (cap at max)
        float newPop = Mathf.Min(currentPop + growth, maxPop);
        float actualGrowth = newPop - currentPop;
        
        province.provinceCurrentPop = newPop;
        
        // Fire event for popup display
        if (actualGrowth > 0)
        {
            GameEvents.PopulationGrowth(province, actualGrowth);
        }
        
        if (logGrowth && actualGrowth > 0)
        {
            Debug.Log($"[PopulationProcessor] {province.provinceName}: {currentPop:F0} → {newPop:F0} (+{actualGrowth:F0}, {growthPercent:P1})");
        }
        
        return actualGrowth;
    }
    
    #region Debug
    
    [ContextMenu("Test Growth (All Player Provinces)")]
    private void DebugTestGrowth()
    {
        ProcessTurnEnd(0);
    }
    
    #endregion
}
