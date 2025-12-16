using UnityEngine;
using TMPro;

public class PlayerNationGUI : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI nationNameText;
    public TextMeshProUGUI taxText;
    public TextMeshProUGUI tradeText;
    public TextMeshProUGUI armySizeText;
    public TextMeshProUGUI armyStrText;
    public TextMeshProUGUI cityCountText;
    public TextMeshProUGUI turnCountText;
    
    [Header("References")]
    public PlayerNation playerNation;
    
    [Header("Update Settings")]
    public float updateInterval = 0.5f; // TODO HORRIBLE LOGIC - WE WILL DYNAMICALLY UPDATE AT EVERY TURN Update every 0.5 seconds
    
    private float updateTimer;

    private void Start()
    {
        // Try to find PlayerNation if not assigned
        if (playerNation == null)
        {
            playerNation = PlayerNation.Instance;
            
            if (playerNation == null)
            {
                playerNation = FindFirstObjectByType<PlayerNation>();
            }
        }
        
        // Auto-find text components if not assigned
        FindTextReferences();
        
        // Initial update
        UpdateGUI();
    }

    private void FindTextReferences()
    {
        // Find by name if not assigned
        if (nationNameText == null)
            nationNameText = FindTextByName("NationNameText");
        
        if (taxText == null)
            taxText = FindTextByName("TaxText");
        
        if (tradeText == null)
            tradeText = FindTextByName("TradeText");
        
        if (armySizeText == null)
            armySizeText = FindTextByName("ArmySizeText");
        
        if (armyStrText == null)
            armyStrText = FindTextByName("ArmyStrText");
        
        if (cityCountText == null)
            cityCountText = FindTextByName("CityCountText");
        
        if (turnCountText == null)
            turnCountText = FindTextByName("TurnCountText");
    }

    private TextMeshProUGUI FindTextByName(string name)
    {
        // Search in children first
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            if (text.gameObject.name == name)
                return text;
        }
        
        // Search in entire scene
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            return obj.GetComponent<TextMeshProUGUI>();
        }
        
        return null;
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateGUI();
        }
    }

    public void UpdateGUI()
    {
        if (playerNation == null)
        {
            // Try to find again
            playerNation = PlayerNation.Instance ?? FindFirstObjectByType<PlayerNation>();
            
            if (playerNation == null)
                return;
        }
        
        // Update all text fields
        if (nationNameText != null)
            nationNameText.text = playerNation.nationName;
        
        if (taxText != null)
            taxText.text = FormatNumber(playerNation.taxIncome);
        
        if (tradeText != null)
            tradeText.text = FormatNumber(playerNation.tradeIncome);
        
        if (armySizeText != null)
            armySizeText.text = FormatNumber(playerNation.armySize);
        
        if (armyStrText != null)
            armyStrText.text = FormatNumber(playerNation.armyStrength);
        
        if (cityCountText != null)
            cityCountText.text = playerNation.cityCount.ToString();
        
        if (turnCountText != null)
            turnCountText.text = $"Turn {playerNation.currentTurn}";
    }

    // Format large numbers nicely (e.g., 1500 -> "1.5K")
    private string FormatNumber(float value)
    {
        if (value >= 1000000)
            return $"{value / 1000000f:F1}M";
        else if (value >= 1000)
            return $"{value / 1000f:F1}K";
        else
            return $"{value:F0}";
    }

    // Call this when player nation changes (e.g., after conquering a province)
    public void RefreshStats()
    {
        if (playerNation != null)
        {
            playerNation.RecalculateStats();
            UpdateGUI();
        }
    }
}