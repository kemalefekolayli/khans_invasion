using UnityEngine;
using TMPro;

/// <summary>
/// Displays player nation stats with current + pending format.
/// Shows what you have now and what you'll gain next turn.
/// Example: "150 +23" means 150 gold now, +23 coming next turn.
/// </summary>
public class PlayerNationGUI : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI nationNameText;
    public TextMeshProUGUI goldText;        // Current gold + income
    public TextMeshProUGUI taxText;         // Tax income per turn  
    public TextMeshProUGUI tradeText;       // Trade income per turn
    public TextMeshProUGUI populationText;  // Total population
    public TextMeshProUGUI armySizeText;    // Army size
    public TextMeshProUGUI armyStrText;     // Army strength
    public TextMeshProUGUI cityCountText;   // Number of cities
    public TextMeshProUGUI turnCountText;   // Current turn
    public TextMeshProUGUI lootText;        // Khan's carried loot
    
    [Header("Display Settings")]
    public bool showPendingIncome = true;   // Show "+X" next to current values
    public Color pendingIncomeColor = new Color(0.4f, 0.9f, 0.4f); // Green for positive
    public Color pendingLossColor = new Color(0.9f, 0.4f, 0.4f);   // Red for negative
    
    [Header("References")]
    public PlayerNation playerNation;
    
    // Cached values for comparison
    private float lastGold;
    private float lastTax;
    private float lastTrade;

    private void OnEnable()
    {
        GameEvents.OnPlayerNationReady += OnPlayerNationReady;
        GameEvents.OnPlayerStatsChanged += OnPlayerStatsChanged;
        GameEvents.OnPlayerNationChanged += OnPlayerNationChanged;
        GameEvents.OnTurnEnded += OnTurnEnded;
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        GameEvents.OnProvinceRaided += OnProvinceRaided;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
        GameEvents.OnPlayerStatsChanged -= OnPlayerStatsChanged;
        GameEvents.OnPlayerNationChanged -= OnPlayerNationChanged;
        GameEvents.OnTurnEnded -= OnTurnEnded;
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
    }

    private void Start()
    {
        FindTextReferences();
    }

    private void OnPlayerNationReady()
    {
        CacheCurrentValues();
        UpdateGUI();
    }

    private void OnPlayerStatsChanged()
    {
        UpdateGUI();
    }

    private void OnPlayerNationChanged(NationModel newNation)
    {
        CacheCurrentValues();
        UpdateGUI();
    }

    private void OnTurnEnded(int newTurn)
    {
        // After turn ends, cache new values as baseline
        CacheCurrentValues();
        UpdateGUI();
    }
    
    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {
        // Recalculate when buildings change income
        if (playerNation != null)
        {
            playerNation.RecalculateStats();
        }
        UpdateGUI();
    }
    
    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (playerNation != null)
        {
            playerNation.RecalculateStats();
        }
        UpdateGUI();
    }
    
    private void OnProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        // Update loot display when raid happens
        UpdateLootDisplay();
    }

    private void CacheCurrentValues()
    {
        if (playerNation == null) return;
        
        lastGold = playerNation.nationMoney;
        lastTax = playerNation.TaxIncome;
        lastTrade = playerNation.TradeIncome;
    }

    private void FindTextReferences()
    {
        if (nationNameText == null)
            nationNameText = FindTextByName("NationNameText");
        
        if (goldText == null)
            goldText = FindTextByName("GoldText");
        
        if (taxText == null)
            taxText = FindTextByName("TaxText");
        
        if (tradeText == null)
            tradeText = FindTextByName("TradeText");
        
        if (populationText == null)
            populationText = FindTextByName("PopulationText");
        
        if (armySizeText == null)
            armySizeText = FindTextByName("ArmySizeText");
        
        if (armyStrText == null)
            armyStrText = FindTextByName("ArmyStrText");
        
        if (cityCountText == null)
            cityCountText = FindTextByName("CityCountText");
        
        if (turnCountText == null)
            turnCountText = FindTextByName("TurnCountText");
        
        if (lootText == null)
            lootText = FindTextByName("LootText");
    }

    private TextMeshProUGUI FindTextByName(string name)
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            if (text.gameObject.name == name)
                return text;
        }
        
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            return obj.GetComponent<TextMeshProUGUI>();
        }
        
        return null;
    }

    public void UpdateGUI()
    {
        if (playerNation == null)
        {
            playerNation = PlayerNation.Instance ?? FindFirstObjectByType<PlayerNation>();
            
            if (playerNation == null)
                return;
        }
        
        if (playerNation.currentNation == null)
            return;
        
        // Nation name
        if (nationNameText != null)
            nationNameText.text = playerNation.NationName;
        
        // Gold with pending income
        if (goldText != null)
        {
            float totalIncome = playerNation.TotalIncome;
            goldText.text = FormatWithPending(playerNation.nationMoney, totalIncome);
        }
        
        // Tax - show current + change from start of turn
        if (taxText != null)
        {
            float taxChange = playerNation.TaxIncome - lastTax;
            if (showPendingIncome && Mathf.Abs(taxChange) > 0.01f)
            {
                taxText.text = FormatWithChange(playerNation.TaxIncome, taxChange);
            }
            else
            {
                taxText.text = FormatNumber(playerNation.TaxIncome);
            }
        }
        
        // Trade - show current + change from start of turn
        if (tradeText != null)
        {
            float tradeChange = playerNation.TradeIncome - lastTrade;
            if (showPendingIncome && Mathf.Abs(tradeChange) > 0.01f)
            {
                tradeText.text = FormatWithChange(playerNation.TradeIncome, tradeChange);
            }
            else
            {
                tradeText.text = FormatNumber(playerNation.TradeIncome);
            }
        }
        
        // Population
        if (populationText != null)
            populationText.text = FormatNumber(playerNation.PopulationSize);
        
        // Army size
        if (armySizeText != null)
            armySizeText.text = FormatNumber(playerNation.ArmySize);
        
        // Army strength
        if (armyStrText != null)
            armyStrText.text = FormatNumber(playerNation.ArmyStrength);
        
        // City count
        if (cityCountText != null)
            cityCountText.text = playerNation.CityCount.ToString();
        
        // Turn count - use TurnManager for the real turn count
        if (turnCountText != null)
        {
            int turn = TurnManager.Instance?.CurrentTurn ?? playerNation.GetCurrentTurn();
            turnCountText.text = $"Turn {turn}";
        }
        
        // Loot display
        UpdateLootDisplay();
    }
    
    /// <summary>
    /// Update the loot display text with Khan/selected general's carried loot.
    /// </summary>
    private void UpdateLootDisplay()
    {
        if (lootText == null) return;
        
        General khan = GetKhanGeneral();
        if (khan == null)
        {
            lootText.text = "0/0";
            return;
        }
        
        float carriedLoot = khan.CarriedLoot;
        float maxCapacity = khan.MaxLootCapacity;
        
        // Format: "150/500" or with color if nearly full
        if (carriedLoot >= maxCapacity * 0.9f)
        {
            // Nearly full - show in warning color
            string colorHex = ColorUtility.ToHtmlStringRGB(pendingLossColor);
            lootText.text = $"<color=#{colorHex}>{FormatNumber(carriedLoot)}</color>/{FormatNumber(maxCapacity)}";
        }
        else if (carriedLoot > 0)
        {
            // Has some loot - show in positive color
            string colorHex = ColorUtility.ToHtmlStringRGB(pendingIncomeColor);
            lootText.text = $"<color=#{colorHex}>{FormatNumber(carriedLoot)}</color>/{FormatNumber(maxCapacity)}";
        }
        else
        {
            lootText.text = $"0/{FormatNumber(maxCapacity)}";
        }
    }
    
    /// <summary>
    /// Get the Khan's General component.
    /// </summary>
    private General GetKhanGeneral()
    {
        // First try from selection manager
        if (GeneralSelectionManager.Instance != null && 
            GeneralSelectionManager.Instance.SelectedGeneral != null)
        {
            SelectableGeneral selected = GeneralSelectionManager.Instance.SelectedGeneral;
            if (selected.IsKhan)
            {
                return selected.GetComponent<General>();
            }
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

    /// <summary>
    /// Format: "150 +23" where 150 is current and +23 is pending
    /// </summary>
    private string FormatWithPending(float current, float pending)
    {
        string currentStr = FormatNumber(current);
        
        if (!showPendingIncome || Mathf.Abs(pending) < 0.01f)
            return currentStr;
        
        string sign = pending >= 0 ? "+" : "";
        string pendingStr = FormatNumber(pending);
        string colorHex = pending >= 0 ? ColorUtility.ToHtmlStringRGB(pendingIncomeColor) 
                                       : ColorUtility.ToHtmlStringRGB(pendingLossColor);
        
        return $"{currentStr} <color=#{colorHex}>{sign}{pendingStr}</color>";
    }
    
    /// <summary>
    /// Format: "23 (+5)" where 23 is current value and +5 is change since turn start
    /// </summary>
    private string FormatWithChange(float current, float change)
    {
        string currentStr = FormatNumber(current);
        
        if (Mathf.Abs(change) < 0.01f)
            return currentStr;
        
        string sign = change >= 0 ? "+" : "";
        string changeStr = FormatNumber(Mathf.Abs(change));
        string colorHex = change >= 0 ? ColorUtility.ToHtmlStringRGB(pendingIncomeColor) 
                                      : ColorUtility.ToHtmlStringRGB(pendingLossColor);
        
        return $"{currentStr} <color=#{colorHex}>({sign}{changeStr})</color>";
    }

    /// <summary>
    /// Format large numbers nicely (e.g., 1500 -> "1.5K")
    /// </summary>
    private string FormatNumber(float value)
    {
        if (Mathf.Abs(value) >= 1000000)
            return $"{value / 1000000f:F1}M";
        else if (Mathf.Abs(value) >= 1000)
            return $"{value / 1000f:F1}K";
        else
            return $"{value:F0}";
    }
    
    /// <summary>
    /// Call this to refresh the baseline values (e.g., after loading a save)
    /// </summary>
    public void ResetBaseline()
    {
        CacheCurrentValues();
        UpdateGUI();
    }
}