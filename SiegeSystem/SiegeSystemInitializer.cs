using UnityEngine;

/// <summary>
/// Initializes siege system components on provinces when the game loads.
/// Attach to a persistent object in the scene.
/// Must run AFTER provinces are assigned.
/// </summary>
public class SiegeSystemInitializer : MonoBehaviour
{
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
            Debug.LogWarning("[SiegeSystemInitializer] Event didn't fire, using fallback initialization");
            InitializeAllProvinces();
        }
    }
    
    private void OnProvincesAssigned()
    {
        // Small delay to ensure all province data is fully set
        Invoke(nameof(InitializeAllProvinces), 0.1f);
    }
    
    /// <summary>
    /// Add SiegeOverlayController to all provinces.
    /// </summary>
    private void InitializeAllProvinces()
    {
        if (hasInitialized)
        {
            Debug.Log("[SiegeSystemInitializer] Already initialized, skipping");
            return;
        }
        
        ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        
        if (allProvinces.Length == 0)
        {
            Debug.LogWarning("[SiegeSystemInitializer] No provinces found!");
            return;
        }
        
        int initialized = 0;
        
        foreach (ProvinceModel province in allProvinces)
        {
            if (province == null) continue;
            
            // Add SiegeOverlayController if not already present
            if (province.GetComponent<SiegeOverlayController>() == null)
            {
                province.gameObject.AddComponent<SiegeOverlayController>();
                initialized++;
            }
        }
        
        hasInitialized = true;
        
        if (logInitialization)
        {
            Debug.Log($"[SiegeSystemInitializer] ✓ Added SiegeOverlayController to {initialized} provinces");
        }
    }
}
