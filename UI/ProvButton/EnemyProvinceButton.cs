using UnityEngine;
using UnityEngine.UI;

public class EnemyProvinceButtons : MonoBehaviour
{
    [Header("Buttons")]
    public Button raidButton;
    public Button siegeButton;
    
    [Header("Button Colors")]
    [SerializeField] private Color unavailableColor = new Color(0.4f, 0.4f, 0.4f, 0.6f); // Grey/unavailable
    
    private ProvinceModel currentProvince;
    
    // Raid button tracking
    private Image raidButtonImage;
    private Color originalRaidButtonColor;
    private ColorBlock originalRaidColors;
    private bool hasStoredRaidOriginalColor = false;
    
    // Siege button tracking
    private Image siegeButtonImage;
    private Color originalSiegeButtonColor;
    private ColorBlock originalSiegeColors;
    private bool hasStoredSiegeOriginalColor = false;

    private void Awake()
    {
        // Butonlara listener ekle
        if (raidButton != null)
        {
            raidButton.onClick.AddListener(OnRaidClicked);
            raidButtonImage = raidButton.GetComponent<Image>();
            
            // Store original colors
            if (raidButtonImage != null)
            {
                originalRaidButtonColor = raidButtonImage.color;
                hasStoredRaidOriginalColor = true;
            }
            originalRaidColors = raidButton.colors;
        }
        
        if (siegeButton != null)
        {
            siegeButton.onClick.AddListener(OnSiegeClicked);
            siegeButtonImage = siegeButton.GetComponent<Image>();
            
            // Store original colors
            if (siegeButtonImage != null)
            {
                originalSiegeButtonColor = siegeButtonImage.color;
                hasStoredSiegeOriginalColor = true;
            }
            originalSiegeColors = siegeButton.colors;
        }
        
        // Başlangıçta gizle
        HideButtons();
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceInteractionOpened += ShowButtons;
        GameEvents.OnProvincePanelClosed += HideButtons;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
        GameEvents.OnProvinceRaided += OnProvinceRaided;
        GameEvents.OnProvinceSieged += OnProvinceSieged;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceInteractionOpened -= ShowButtons;
        GameEvents.OnProvincePanelClosed -= HideButtons;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
        GameEvents.OnProvinceSieged -= OnProvinceSieged;
    }
    
    private void OnProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        // Update button state if this is our current province
        if (province == currentProvince)
        {
            UpdateRaidButtonState();
        }
    }
    
    private void OnProvinceSieged(ProvinceModel province, General attacker, float defenseStrength)
    {
        // Update button state if this is our current province
        if (province == currentProvince)
        {
            UpdateSiegeButtonState();
        }
    }

    private void ShowButtons(ProvinceModel province)
    {
        currentProvince = province;
        
        if (raidButton != null)
        {
            raidButton.gameObject.SetActive(true);
            UpdateRaidButtonState();
        }
        
        if (siegeButton != null)
        {
            siegeButton.gameObject.SetActive(true);
            UpdateSiegeButtonState();
        }
        
        Debug.Log($"[EnemyButtons] Showing Raid/Siege for {province.provinceName}");
    }
    
    /// <summary>
    /// Update raid button visual state based on whether province can be raided.
    /// </summary>
    private void UpdateRaidButtonState()
    {
        if (raidButton == null) return;
        
        // If no province, restore to original state
        if (currentProvince == null)
        {
            RestoreOriginalRaidButtonState();
            return;
        }
        
        bool canRaid = CanRaidCurrentProvince();
        
        // Enable/disable button
        raidButton.interactable = canRaid;
        
        if (canRaid)
        {
            // Restore original appearance
            RestoreOriginalRaidButtonState();
        }
        else
        {
            // Apply grey/unavailable appearance
            if (raidButtonImage != null && hasStoredRaidOriginalColor)
            {
                raidButtonImage.color = unavailableColor;
            }
            
            ColorBlock colors = raidButton.colors;
            colors.normalColor = unavailableColor;
            colors.highlightedColor = unavailableColor;
            colors.pressedColor = unavailableColor;
            colors.disabledColor = unavailableColor;
            raidButton.colors = colors;
        }
        
        Debug.Log($"[EnemyButtons] Raid button for {currentProvince.provinceName}: canRaid={canRaid}, loot={currentProvince.availableLoot}");
    }
    
    private void RestoreOriginalRaidButtonState()
    {
        raidButton.interactable = true;
        
        if (raidButtonImage != null && hasStoredRaidOriginalColor)
        {
            raidButtonImage.color = originalRaidButtonColor;
        }
        
        raidButton.colors = originalRaidColors;
    }
    
    private bool CanRaidCurrentProvince()
    {
        if (currentProvince == null) return false;
        if (RaidManager.Instance == null) return false;
        
        return RaidManager.Instance.CanRaidProvince(currentProvince);
    }
    
    /// <summary>
    /// Update siege button visual state based on whether province can be sieged.
    /// </summary>
    private void UpdateSiegeButtonState()
    {
        if (siegeButton == null) return;
        
        // If no province, restore to original state
        if (currentProvince == null)
        {
            RestoreOriginalSiegeButtonState();
            return;
        }
        
        bool canSiege = CanSiegeCurrentProvince();
        
        // Enable/disable button
        siegeButton.interactable = canSiege;
        
        if (canSiege)
        {
            // Restore original appearance
            RestoreOriginalSiegeButtonState();
        }
        else
        {
            // Apply grey/unavailable appearance
            if (siegeButtonImage != null && hasStoredSiegeOriginalColor)
            {
                siegeButtonImage.color = unavailableColor;
            }
            
            ColorBlock colors = siegeButton.colors;
            colors.normalColor = unavailableColor;
            colors.highlightedColor = unavailableColor;
            colors.pressedColor = unavailableColor;
            colors.disabledColor = unavailableColor;
            siegeButton.colors = colors;
        }
        
        float defenseStrength = SiegeManager.Instance != null ? SiegeManager.Instance.CalculateDefenseStrength(currentProvince) : 0;
        Debug.Log($"[EnemyButtons] Siege button for {currentProvince.provinceName}: canSiege={canSiege}, defense={defenseStrength:F0}");
    }
    
    private void RestoreOriginalSiegeButtonState()
    {
        siegeButton.interactable = true;
        
        if (siegeButtonImage != null && hasStoredSiegeOriginalColor)
        {
            siegeButtonImage.color = originalSiegeButtonColor;
        }
        
        siegeButton.colors = originalSiegeColors;
    }
    
    private bool CanSiegeCurrentProvince()
    {
        if (currentProvince == null) return false;
        if (SiegeManager.Instance == null) return false;
        
        // Get attacker
        General attacker = GetCurrentRaider();
        if (attacker == null) return false;
        
        return SiegeManager.Instance.CanSiegeProvince(currentProvince, attacker) == SiegeManager.SiegeResult.Success;
    }

    private void HideButtons()
    {
        currentProvince = null;
        
        if (raidButton != null)
            raidButton.gameObject.SetActive(false);
        
        if (siegeButton != null)
            siegeButton.gameObject.SetActive(false);
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        // At city center'dan çıkınca butonları kapat
        if (currentProvince != null)
        {
            Debug.Log($"[EnemyButtons] Horse left city center, closing buttons");
            HideButtons();
            GameEvents.ProvincePanelClosed();
        }
    }

    private void OnRaidClicked()
    {
        if (currentProvince == null)
        {
            Debug.LogWarning("[EnemyButtons] No province selected for raid!");
            return;
        }
        
        Debug.Log($"[EnemyButtons] RAID clicked on {currentProvince.provinceName}");
        
        // Execute raid through RaidManager
        ExecuteRaid();
        
        // Update button state after raid (grey out)
        UpdateRaidButtonState();
        
        // Close panel AFTER raid is complete
        HideButtons();
        GameEvents.ProvincePanelClosed();
    }
    
    private void ExecuteRaid()
    {
        if (RaidManager.Instance == null)
        {
            Debug.LogError("[EnemyButtons] RaidManager not found!");
            return;
        }
        
        // Get raider (Khan or selected general)
        General raider = GetCurrentRaider();
        if (raider == null)
        {
            Debug.LogWarning("[EnemyButtons] No raider found!");
            return;
        }
        
        if (!raider.HasArmy)
        {
            Debug.LogWarning("[EnemyButtons] Raider has no army!");
            return;
        }
        
        // Check if can raid
        if (!RaidManager.Instance.CanRaidProvince(currentProvince))
        {
            Debug.LogWarning($"[EnemyButtons] Cannot raid {currentProvince.provinceName} (already raided or no loot)");
            return;
        }
        
        // Execute the raid
        float lootGained = RaidManager.Instance.ExecuteRaid(currentProvince, raider);
        
        if (lootGained > 0)
        {
            Debug.Log($"[EnemyButtons] Raid successful! {raider.GeneralName} gained {lootGained:F0} loot from {currentProvince.provinceName}");
        }
        else
        {
            Debug.Log($"[EnemyButtons] Raid failed - no loot gained");
        }
    }
    
    private General GetCurrentRaider()
    {
        // Try to get from GeneralSelectionManager
        if (GeneralSelectionManager.Instance != null && 
            GeneralSelectionManager.Instance.SelectedGeneral != null)
        {
            return GeneralSelectionManager.Instance.SelectedGeneral.GetComponent<General>();
        }
        
        // Fallback: find Khan in scene
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

    private void OnSiegeClicked()
    {
        if (currentProvince == null)
        {
            Debug.LogWarning("[EnemyButtons] No province selected for siege!");
            return;
        }
        
        Debug.Log($"[EnemyButtons] SIEGE clicked on {currentProvince.provinceName}");
        
        // Execute siege through SiegeManager
        ExecuteSiege();
        
        HideButtons();
        GameEvents.ProvincePanelClosed();
    }
    
    private void ExecuteSiege()
    {
        if (SiegeManager.Instance == null)
        {
            Debug.LogError("[EnemyButtons] SiegeManager not found!");
            return;
        }
        
        // Get attacker (selected general or Khan)
        General attacker = GetCurrentRaider(); // Same logic as raid
        if (attacker == null)
        {
            Debug.LogWarning("[EnemyButtons] No attacker found!");
            return;
        }
        
        if (!attacker.HasArmy)
        {
            Debug.LogWarning("[EnemyButtons] Attacker has no army!");
            return;
        }
        
        // Execute the siege
        SiegeManager.SiegeResult result = SiegeManager.Instance.ExecuteSiege(currentProvince, attacker);
        
        if (result == SiegeManager.SiegeResult.Success)
        {
            Debug.Log($"[EnemyButtons] Siege successful! {currentProvince.provinceName} will be conquered next turn.");
        }
        else
        {
            Debug.Log($"[EnemyButtons] Siege failed: {result}");
        }
    }
}

