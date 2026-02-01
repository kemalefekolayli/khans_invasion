using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Button to end the player's turn.
/// Shows current turn number and handles click to advance turn.
/// 
/// SETUP: 
/// 1. Attach to a UI Button GameObject
/// 2. Optionally assign turnText to show current turn number
/// </summary>
public class EndTurnButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI turnNumberText;
    
    [Header("Text Settings")]
    [SerializeField] private string buttonLabel = "End Turn";
    [SerializeField] private string processingLabel = "Processing...";
    [SerializeField] private string turnFormat = "Turn {0}";
    
    [Header("Visual Feedback")]
    [SerializeField] private bool disableDuringProcessing = true;
    
    private void Awake()
    {
        if (endTurnButton == null)
        {
            endTurnButton = GetComponent<Button>();
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to turn events
        TurnManager.OnPlayerTurnStart += OnPlayerTurnStart;
        TurnManager.OnPlayerTurnEnd += OnPlayerTurnEnd;
        TurnManager.OnTurnStart += OnTurnStart;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        TurnManager.OnPlayerTurnStart -= OnPlayerTurnStart;
        TurnManager.OnPlayerTurnEnd -= OnPlayerTurnEnd;
        TurnManager.OnTurnStart -= OnTurnStart;
    }
    
    private void Start()
    {
        // Initialize display
        UpdateButtonText(buttonLabel);
        UpdateTurnNumber();
    }
    
    /// <summary>
    /// Called when End Turn button is clicked.
    /// </summary>
    private void OnEndTurnClicked()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogError("[EndTurnButton] TurnManager not found!");
            return;
        }
        
        if (!TurnManager.Instance.CanPlayerTakeAction())
        {
            Debug.LogWarning("[EndTurnButton] Cannot end turn - not player's turn!");
            return;
        }
        
        TurnManager.Instance.EndPlayerTurn();
    }
    
    /// <summary>
    /// Called when player turn starts.
    /// </summary>
    private void OnPlayerTurnStart()
    {
        // Re-enable button
        if (endTurnButton != null)
        {
            endTurnButton.interactable = true;
        }
        
        UpdateButtonText(buttonLabel);
    }
    
    /// <summary>
    /// Called when player turn ends.
    /// </summary>
    private void OnPlayerTurnEnd()
    {
        // Disable button during processing
        if (disableDuringProcessing && endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }
        
        UpdateButtonText(processingLabel);
    }
    
    /// <summary>
    /// Called when a new turn starts.
    /// </summary>
    private void OnTurnStart(int turnNumber)
    {
        UpdateTurnNumber();
    }
    
    /// <summary>
    /// Update the button text.
    /// </summary>
    private void UpdateButtonText(string text)
    {
        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }
    
    /// <summary>
    /// Update the turn number display.
    /// </summary>
    private void UpdateTurnNumber()
    {
        if (turnNumberText != null && TurnManager.Instance != null)
        {
            turnNumberText.text = string.Format(turnFormat, TurnManager.Instance.CurrentTurn);
        }
    }
    
    #region Public API
    
    /// <summary>
    /// Simulate clicking the End Turn button.
    /// Useful for keyboard shortcuts or AI auto-play.
    /// </summary>
    public void TriggerEndTurn()
    {
        OnEndTurnClicked();
    }
    
    /// <summary>
    /// Enable or disable the button manually.
    /// </summary>
    public void SetButtonEnabled(bool enabled)
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = enabled;
        }
    }
    
    #endregion
}
