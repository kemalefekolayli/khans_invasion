using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual component that shows the current loot status of a province.
/// Works as a floating UI that updates when entering different provinces.
/// Attach to the loot bar Image in the screen-space ProvinceCanvas GUI.
/// </summary>
public class ProvinceLootBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The fill image that shows loot percentage (use Image.fillAmount)")]
    [SerializeField] private Image fillImage;
    
    [Header("Colors")]
    [SerializeField] private Color fullLootColor = new Color(1f, 0.84f, 0f); // Gold/Yellow
    [SerializeField] private Color lowLootColor = new Color(0.5f, 0.25f, 0f); // Dark brown
    [SerializeField] private Color emptyLootColor = new Color(0.3f, 0.3f, 0.3f); // Gray
    
    [Header("Settings")]
    [SerializeField] private bool animateFill = true;
    [SerializeField] private float fillSpeed = 5f;
    
    private ProvinceModel currentProvince;
    private float targetFill = 1f;
    private float currentFill = 1f;
    
    private void Awake()
    {
        // Auto-find fillImage if not assigned
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
    }
    
    private void Start()
    {
        // Ensure the image is set to Filled type with Horizontal fill
        if (fillImage != null)
        {
            if (fillImage.type != Image.Type.Filled)
            {
                fillImage.type = Image.Type.Filled;
                Debug.Log($"[ProvinceLootBar] Set Image type to Filled");
            }
            
            // Force Horizontal fill (not Radial!)
            if (fillImage.fillMethod != Image.FillMethod.Horizontal)
            {
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0; // Left
                Debug.Log($"[ProvinceLootBar] Set Fill Method to Horizontal (Left origin)");
            }
        }
        
        Debug.Log($"[ProvinceLootBar] Initialized on {gameObject.name}");
    }
    
    private void Update()
    {
        // Animate fill if enabled
        if (animateFill && Mathf.Abs(currentFill - targetFill) > 0.001f)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * fillSpeed);
            UpdateFillVisual();
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to province change events
        // Use OnProvinceEnter to update loot display when entering ANY province
        GameEvents.OnProvinceEnter += OnProvinceEnter;
        GameEvents.OnProvinceRaided += OnProvinceRaided;
        
        Debug.Log($"[ProvinceLootBar] Subscribed to events");
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceEnter -= OnProvinceEnter;
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
    }
    
    #region Event Handlers
    
    /// <summary>
    /// Called when general enters any province (not just city center).
    /// Updates the loot bar to show this province's loot status.
    /// </summary>
    private void OnProvinceEnter(ProvinceModel province)
    {
        if (province != null)
        {
            SetProvince(province);
            Debug.Log($"[ProvinceLootBar] Entered province {province.provinceName}");
        }
    }
    
    private void OnProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        // Update display if this is the province we're showing
        if (province == currentProvince)
        {
            Debug.Log($"[ProvinceLootBar] {province.provinceName} was raided, updating display");
            RefreshLootDisplay();
        }
    }
    
    #endregion
    
    /// <summary>
    /// Set the province to display loot for.
    /// </summary>
    public void SetProvince(ProvinceModel province)
    {
        currentProvince = province;
        RefreshLootDisplay();
    }
    
    /// <summary>
    /// Refresh the loot display based on current province state.
    /// </summary>
    public void RefreshLootDisplay()
    {
        if (currentProvince == null)
        {
            Debug.LogWarning($"[ProvinceLootBar] No province set!");
            targetFill = 0f;
            if (!animateFill)
            {
                currentFill = targetFill;
                UpdateFillVisual();
            }
            return;
        }
        
        if (RaidManager.Instance == null)
        {
            Debug.LogWarning($"[ProvinceLootBar] RaidManager not found!");
            targetFill = 1f;
            if (!animateFill)
            {
                currentFill = targetFill;
                UpdateFillVisual();
            }
            return;
        }
        
        // Get loot percentage from RaidManager
        float lootPercent = RaidManager.Instance.GetLootPercentage(currentProvince);
        targetFill = lootPercent;
        
        Debug.Log($"[ProvinceLootBar] {currentProvince.provinceName}: Loot = {currentProvince.availableLoot:F1}, " +
                  $"Max = {RaidManager.Instance.CalculateMaxLoot(currentProvince):F1}, " +
                  $"Percent = {lootPercent:P0}");
        
        if (!animateFill)
        {
            currentFill = targetFill;
            UpdateFillVisual();
        }
    }
    
    /// <summary>
    /// Update the visual representation of the fill bar.
    /// </summary>
    private void UpdateFillVisual()
    {
        if (fillImage == null) return;
        
        // Set fill amount
        fillImage.fillAmount = currentFill;
        
        // Interpolate color based on fill
        Color targetColor;
        if (currentFill <= 0.01f)
        {
            targetColor = emptyLootColor;
        }
        else if (currentFill < 0.5f)
        {
            targetColor = Color.Lerp(lowLootColor, fullLootColor, currentFill * 2f);
        }
        else
        {
            targetColor = fullLootColor;
        }
        
        fillImage.color = targetColor;
    }
    
    /// <summary>
    /// Immediately set fill without animation.
    /// </summary>
    public void SetFillImmediate(float percent)
    {
        targetFill = percent;
        currentFill = percent;
        UpdateFillVisual();
    }
}
