using UnityEngine;

/// <summary>
/// Initializes siege and raid system components on provinces when the game loads.
/// Dynamically adds required overlay components to all provinces.
/// Attach to a persistent object in the scene (e.g., GameManager).
/// 
/// This solves the issue where province instances in the scene
/// don't have the overlay components from the prefab.
/// </summary>
public class ProvinceOverlayInitializer : MonoBehaviour
{
    [Header("Components to Add")]
    [Tooltip("Add SiegeOverlayController to provinces")]
    [SerializeField] private bool addSiegeOverlay = true;
    
    [Tooltip("Add ProvinceRaidOverlay to provinces")]
    [SerializeField] private bool addRaidOverlay = true;
    
    [Header("Debug")]
    [SerializeField] private bool logInitialization = true;
    
    private bool hasInitialized = false;
    
    private void OnEnable()
    {
        GameEvents.OnProvincesAssigned += OnProvincesAssigned;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= OnProvincesAssigned;
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
            Debug.LogWarning("[ProvinceOverlayInitializer] Event didn't fire, using fallback initialization");
            InitializeAllProvinces();
        }
    }
    
    private void OnProvincesAssigned()
    {
        // Small delay to ensure all province data is fully set
        Invoke(nameof(InitializeAllProvinces), 0.1f);
    }
    
    /// <summary>
    /// Add overlay components to all provinces that don't have them.
    /// </summary>
    private void InitializeAllProvinces()
    {
        if (hasInitialized)
        {
            Debug.Log("[ProvinceOverlayInitializer] Already initialized, skipping");
            return;
        }
        
        ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        
        if (allProvinces.Length == 0)
        {
            Debug.LogWarning("[ProvinceOverlayInitializer] No provinces found!");
            return;
        }
        
        int siegeAdded = 0;
        int raidAdded = 0;
        
        foreach (ProvinceModel province in allProvinces)
        {
            if (province == null) continue;
            
            // Add SiegeOverlayController if enabled and not present
            if (addSiegeOverlay && province.GetComponent<SiegeOverlayController>() == null)
            {
                province.gameObject.AddComponent<SiegeOverlayController>();
                siegeAdded++;
            }
            
            // Add ProvinceRaidOverlay if enabled and not present
            if (addRaidOverlay && province.GetComponent<ProvinceRaidOverlay>() == null)
            {
                province.gameObject.AddComponent<ProvinceRaidOverlay>();
                raidAdded++;
            }
        }
        
        hasInitialized = true;
        
        if (logInitialization)
        {
            Debug.Log($"[ProvinceOverlayInitializer] ✓ Initialized {allProvinces.Length} provinces");
            if (addSiegeOverlay)
                Debug.Log($"  - Added SiegeOverlayController to {siegeAdded} provinces");
            if (addRaidOverlay)
                Debug.Log($"  - Added ProvinceRaidOverlay to {raidAdded} provinces");
        }
    }
}
