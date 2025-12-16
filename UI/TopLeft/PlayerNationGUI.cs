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

    private void OnEnable()
    {
        // Subscribe to events
        GameEvents.OnPlayerNationReady += OnPlayerNationReady;
        GameEvents.OnPlayerStatsChanged += OnPlayerStatsChanged;
        GameEvents.OnPlayerNationChanged += OnPlayerNationChanged;
        GameEvents.OnTurnEnded += OnTurnEnded;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
        GameEvents.OnPlayerStatsChanged -= OnPlayerStatsChanged;
        GameEvents.OnPlayerNationChanged -= OnPlayerNationChanged;
        GameEvents.OnTurnEnded -= OnTurnEnded;
    }

    private void Start()
    {
        // Auto-find text components
        FindTextReferences();
    }

    private void OnPlayerNationReady()
    {
        // Player nation is ready, update GUI
        UpdateGUI();
    }

    private void OnPlayerStatsChanged()
    {
        UpdateGUI();
    }

    private void OnPlayerNationChanged(NationModel newNation)
    {
        UpdateGUI();
    }

    private void OnTurnEnded(int newTurn)
    {
        UpdateGUI();
    }

    private void FindTextReferences()
    {
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

    public void UpdateGUI()
    {
        // Try to find PlayerNation if not assigned
        if (playerNation == null)
        {
            playerNation = PlayerNation.Instance ?? FindFirstObjectByType<PlayerNation>();
            
            if (playerNation == null)
                return;
        }
        
        // Check if player has a nation assigned
        if (playerNation.currentNation == null)
            return;
        
        // Update all text fields using the properties
        if (nationNameText != null)
            nationNameText.text = playerNation.NationName;
        
        if (taxText != null)
            taxText.text = FormatNumber(playerNation.TaxIncome);
        
        if (tradeText != null)
            tradeText.text = FormatNumber(playerNation.TradeIncome);
        
        if (armySizeText != null)
            armySizeText.text = FormatNumber(playerNation.ArmySize);
        
        if (armyStrText != null)
            armyStrText.text = FormatNumber(playerNation.ArmyStrength);
        
        if (cityCountText != null)
            cityCountText.text = playerNation.CityCount.ToString();
        
        if (turnCountText != null)
            turnCountText.text = $"Turn {playerNation.currentTurn}";
        
        Debug.Log($"GUI Updated: {playerNation.NationName}, Cities: {playerNation.CityCount}");
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
}