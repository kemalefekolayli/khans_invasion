using UnityEngine;

/// <summary>
/// Initializes province loot values when the game loads.
/// Attach to a persistent object in the scene.
/// Must run AFTER ProvinceDataLoader has applied tax/trade values!
/// </summary>
public class ProvinceLootInitializer : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logInitialization = true;
    
    [Header("Loot Settings (Fallback)")]
    [SerializeField] private float taxMultiplier = 5f;
    [SerializeField] private float tradeMultiplier = 6f;
    [SerializeField] private float minimumLoot = 10f;
    
    private bool hasInitialized = false;
    
    private void OnEnable()
    {
        // Subscribe to ProvinceDataLoaded, NOT ProvincesAssigned
        // This ensures tax/trade values are already loaded
        GameEvents.OnProvinceDataLoaded += OnProvinceDataLoaded;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceDataLoaded -= OnProvinceDataLoaded;
    }
    
    private void Start()
    {
        // Fallback: If event already fired before we subscribed, try after a delay
        Invoke(nameof(TryFallbackInitialization), 1f);
    }
    
    private void TryFallbackInitialization()
    {
        if (!hasInitialized)
        {
            Debug.LogWarning("[ProvinceLootInitializer] Event didn't fire, using fallback initialization");
            InitializeAllProvinceLoot();
        }
    }
    
    private void OnProvinceDataLoaded()
    {
        // Small delay to ensure all province data is fully set
        Invoke(nameof(InitializeAllProvinceLoot), 0.1f);
    }
    
    /// <summary>
    /// Initialize loot for all provinces in the game.
    /// </summary>
    private void InitializeAllProvinceLoot()
    {
        if (hasInitialized)
        {
            Debug.Log("[ProvinceLootInitializer] Already initialized, skipping");
            return;
        }
        
        ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        
        if (allProvinces.Length == 0)
        {
            Debug.LogWarning("[ProvinceLootInitializer] No provinces found!");
            return;
        }
        
        int initialized = 0;
        float totalLoot = 0f;
        int zeroIncomeCount = 0;
        
        foreach (ProvinceModel province in allProvinces)
        {
            if (province == null) continue;
            
            // Calculate max loot directly (don't depend on RaidManager)
            float incomeLoot = (province.provinceTaxIncome * taxMultiplier) + 
                              (province.provinceTradePower * tradeMultiplier);
            float maxLoot = Mathf.Max(minimumLoot, incomeLoot);
            
            // Set available loot to max
            province.availableLoot = maxLoot;
            
            if (incomeLoot <= 0)
            {
                zeroIncomeCount++;
                if (logInitialization)
                {
                    Debug.Log($"[ProvinceLootInitializer] {province.provinceName} has 0 income, using minimum loot: {minimumLoot}");
                }
            }
            
            totalLoot += province.availableLoot;
            initialized++;
        }
        
        hasInitialized = true;
        
        if (logInitialization)
        {
            Debug.Log($"[ProvinceLootInitializer] ✓ Initialized loot for {initialized} provinces");
            Debug.Log($"[ProvinceLootInitializer] Total loot in world: {totalLoot:F0}");
            if (zeroIncomeCount > 0)
            {
                Debug.Log($"[ProvinceLootInitializer] {zeroIncomeCount} provinces have 0 income (using minimum loot)");
            }
        }
    }
}



