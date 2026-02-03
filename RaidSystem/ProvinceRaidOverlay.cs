using UnityEngine;

/// <summary>
/// Visual overlay that indicates a province is being raided or has been raided.
/// Handles color tinting and raid animation effects.
/// </summary>
public class ProvinceRaidOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer provinceRenderer;
    
    [Header("Raid Tint Settings")]
    [SerializeField] private Color raidedTintColor = new Color(1f, 0.3f, 0.3f, 0.5f); // Red tint
    [SerializeField] private float tintIntensity = 0.3f;
    
    [Header("Recovery Visual")]
    [SerializeField] private bool showRecoveryVisual = true;
    [SerializeField] private Color recoveringTintColor = new Color(1f, 0.6f, 0.3f, 0.3f); // Orange
    
    private ProvinceModel province;
    private Color originalColor;
    
    /// <summary>
    /// Whether this province has been raided and hasn't fully recovered.
    /// </summary>
    public bool IsRaided { get; private set; } = false;
    
    private void Awake()
    {
        province = GetComponent<ProvinceModel>();
        
        if (provinceRenderer == null)
            provinceRenderer = GetComponent<SpriteRenderer>();
            
        if (provinceRenderer != null)
            originalColor = provinceRenderer.color;
    }
    
    private void OnEnable()
    {
        GameEvents.OnProvinceRaided += OnProvinceRaided;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        TurnManager.OnTurnStart += OnTurnStart;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
        TurnManager.OnTurnStart -= OnTurnStart;
    }
    
    private void OnProvinceRaided(ProvinceModel raidedProvince, General raider, float lootAmount)
    {
        if (raidedProvince == province)
        {
            ApplyRaidedTint();
            IsRaided = true;
        }
    }
    
    /// <summary>
    /// When province owner changes, update our stored original color to the new nation's color.
    /// This prevents restoring the old owner's color.
    /// </summary>
    private void OnProvinceOwnerChanged(ProvinceModel changedProvince, NationModel oldOwner, NationModel newOwner)
    {
        if (changedProvince == province && provinceRenderer != null)
        {
            // Update stored original color to the new owner's color
            originalColor = provinceRenderer.color;
            IsRaided = false; // Reset raided state for new owner
            
            Debug.Log($"[ProvinceRaidOverlay] Updated original color for {province?.provinceName} after conquest");
        }
    }
    
    private void OnTurnStart(int turnNumber)
    {
        // Update visual based on loot recovery
        if (province != null && RaidManager.Instance != null)
        {
            float lootPercent = RaidManager.Instance.GetLootPercentage(province);
            
            if (lootPercent >= 0.99f)
            {
                // Fully recovered
                RemoveTint();
                IsRaided = false;
            }
            else if (showRecoveryVisual && lootPercent > 0)
            {
                // Still recovering - show lighter tint
                ApplyRecoveringTint(lootPercent);
            }
        }
    }
    
    /// <summary>
    /// Apply the raided tint to the province.
    /// </summary>
    private void ApplyRaidedTint()
    {
        if (provinceRenderer == null) return;
        
        // Blend original color with raid tint
        Color tintedColor = Color.Lerp(originalColor, raidedTintColor, tintIntensity);
        provinceRenderer.color = tintedColor;
    }
    
    /// <summary>
    /// Apply a lighter recovering tint based on loot percentage.
    /// </summary>
    private void ApplyRecoveringTint(float lootPercent)
    {
        if (provinceRenderer == null) return;
        
        // Lighter tint as loot recovers
        float recoveryIntensity = tintIntensity * (1f - lootPercent);
        Color tintedColor = Color.Lerp(originalColor, recoveringTintColor, recoveryIntensity);
        provinceRenderer.color = tintedColor;
    }
    
    /// <summary>
    /// Remove the raid tint and restore original color.
    /// </summary>
    private void RemoveTint()
    {
        if (provinceRenderer != null)
        {
            provinceRenderer.color = originalColor;
        }
    }
    
    /// <summary>
    /// Trigger raid animation (for DOTween integration).
    /// </summary>
    public void PlayRaidAnimation()
    {
        // TODO: Implement DOTween animation
        // - Flash effect
        // - Particle spawning
        // - Sound effect trigger
        
        Debug.Log($"[ProvinceRaidOverlay] Playing raid animation for {province?.provinceName}");
    }
}
