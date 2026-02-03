using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the raid button UI and executes raids when clicked.
/// Shows raid availability, expected loot, and triggers the raid through RaidManager.
/// </summary>
public class RaidButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI lootPreviewText; // Optional: shows expected loot
    
    [Header("Visual Feedback")]
    [SerializeField] private Color canRaidColor = Color.green;
    [SerializeField] private Color cannotRaidColor = Color.gray;
    [SerializeField] private Image buttonImage;
    
    // Current target province (set by the panel that opens this button)
    private ProvinceModel currentProvince;
    private CityCenter currentCityCenter;
    
    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
            
        if (buttonImage == null && button != null)
            buttonImage = button.GetComponent<Image>();
    }
    
    private void Start()
    {
        // NOTE: onClick is now handled by EnemyProvinceButtons
        // This script only handles visual state updates
        // if (button != null)
        //     button.onClick.AddListener(OnRaidButtonClicked);
    }
    
    private void OnEnable()
    {
        // Subscribe to events
        GameEvents.OnProvinceInteractionOpened += OnProvinceInteractionOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        GameEvents.OnCityCenterEnter += OnCityCenterEnter;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceInteractionOpened -= OnProvinceInteractionOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
        GameEvents.OnCityCenterEnter -= OnCityCenterEnter;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
    }
    
    #region Event Handlers
    
    private void OnProvinceInteractionOpened(ProvinceModel province)
    {
        currentProvince = province;
        UpdateButtonState();
    }
    
    private void OnPanelClosed()
    {
        currentProvince = null;
        currentCityCenter = null;
    }
    
    private void OnCityCenterEnter(CityCenter cityCenter)
    {
        currentCityCenter = cityCenter;
        if (cityCenter != null && cityCenter.Province != null)
        {
            currentProvince = cityCenter.Province;
        }
        UpdateButtonState();
    }
    
    private void OnCityCenterExit(CityCenter cityCenter)
    {
        if (currentCityCenter == cityCenter)
        {
            currentCityCenter = null;
        }
    }
    
    #endregion
    
    /// <summary>
    /// Update button state based on whether raid is possible.
    /// </summary>
    private void UpdateButtonState()
    {
        if (currentProvince == null || RaidManager.Instance == null)
        {
            SetButtonEnabled(false, "No Target");
            return;
        }
        
        // Check if can raid
        bool canRaid = RaidManager.Instance.CanRaidProvince(currentProvince);
        
        // Get raider (currently selected general)
        General raider = GetCurrentRaider();
        if (raider == null || !raider.HasArmy)
        {
            SetButtonEnabled(false, "No Army");
            return;
        }
        
        if (!canRaid)
        {
            SetButtonEnabled(false, "Already Raided");
            return;
        }
        
        // Calculate expected loot for preview
        float expectedLoot = RaidManager.Instance.CalculateLootAmount(currentProvince, raider.CommandedArmy.ArmySize);
        float availableCapacity = raider.AvailableLootCapacity;
        float actualLoot = Mathf.Min(expectedLoot, availableCapacity);
        
        if (actualLoot <= 0)
        {
            SetButtonEnabled(false, "Bags Full");
            return;
        }
        
        // Can raid!
        SetButtonEnabled(true, $"Raid (+{actualLoot:F0})");
        
        if (lootPreviewText != null)
        {
            float lootPercent = RaidManager.Instance.GetLootPercentage(currentProvince) * 100f;
            lootPreviewText.text = $"Loot: {lootPercent:F0}%";
        }
    }
    
    /// <summary>
    /// Set button enabled/disabled state with text.
    /// </summary>
    private void SetButtonEnabled(bool enabled, string text)
    {
        if (button != null)
            button.interactable = enabled;
            
        if (buttonText != null)
            buttonText.text = text;
            
        if (buttonImage != null)
            buttonImage.color = enabled ? canRaidColor : cannotRaidColor;
    }
    
    /// <summary>
    /// Called when raid button is clicked.
    /// </summary>
    private void OnRaidButtonClicked()
    {
        if (currentProvince == null)
        {
            Debug.LogWarning("[RaidButton] No province selected!");
            return;
        }
        
        if (RaidManager.Instance == null)
        {
            Debug.LogError("[RaidButton] RaidManager not found!");
            return;
        }
        
        General raider = GetCurrentRaider();
        if (raider == null)
        {
            Debug.LogWarning("[RaidButton] No raider found!");
            return;
        }
        
        // Execute the raid
        float lootGained = RaidManager.Instance.ExecuteRaid(currentProvince, raider);
        
        if (lootGained > 0)
        {
            Debug.Log($"[RaidButton] Raid successful! Gained {lootGained:F0} loot.");
            
            // Visual feedback
            PlayRaidAnimation();
            
            // Update button state
            UpdateButtonState();
        }
    }
    
    /// <summary>
    /// Get the current raider (Khan or selected general).
    /// </summary>
    private General GetCurrentRaider()
    {
        // Try to get from GeneralSelectionManager
        if (GeneralSelectionManager.Instance != null && 
            GeneralSelectionManager.Instance.SelectedGeneral != null)
        {
            return GeneralSelectionManager.Instance.SelectedGeneral.GetComponent<General>();
        }
        
        // Fallback: find Khan
        SelectableGeneral[] generals = FindObjectsByType<SelectableGeneral>(FindObjectsSortMode.None);
        foreach (var selectable in generals)
        {
            if (selectable.IsKhan)
            {
                return selectable.GetComponent<General>();
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Play raid animation (to be expanded with DOTween).
    /// </summary>
    private void PlayRaidAnimation()
    {
        // TODO: Add DOTween animation
        // - Province color tint (red/fire overlay)
        // - Particle effects (smoke, fire)
        // - Loot text popup
        
        Debug.Log("[RaidButton] Playing raid animation (placeholder)");
    }
    
    /// <summary>
    /// Set the current province to raid (called by province panel).
    /// </summary>
    public void SetTargetProvince(ProvinceModel province)
    {
        currentProvince = province;
        UpdateButtonState();
    }
}