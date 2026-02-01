using UnityEngine;

/// <summary>
/// Handles player income calculations at end of turn.
/// Calculates tax and trade income from all provinces and adds to player's gold.
/// 
/// SETUP: Add to the PlayerNation GameObject or any persistent object.
/// </summary>
public class IncomeProcessor : MonoBehaviour, ITurnProcessor
{
    [Header("Income Modifiers")]
    [Tooltip("Global multiplier for tax income (for buffs/debuffs)")]
    [SerializeField] private float taxMultiplier = 1.0f;
    
    [Tooltip("Global multiplier for trade income (for buffs/debuffs)")]
    [SerializeField] private float tradeMultiplier = 1.0f;
    
    [Header("Debug")]
    [SerializeField] private bool logIncome = true;
    
    // ITurnProcessor implementation
    public int ProcessingPriority => 0; // Income calculated first
    
    private PlayerNation playerNation;
    
    private void Start()
    {
        playerNation = PlayerNation.Instance;
        
        // Register with TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterTurnProcessor(this);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister from TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterTurnProcessor(this);
        }
    }
    
    /// <summary>
    /// Called by TurnManager at end of each turn.
    /// </summary>
    public void ProcessTurnEnd(int turnNumber)
    {
        if (playerNation == null)
        {
            playerNation = PlayerNation.Instance;
            if (playerNation == null)
            {
                Debug.LogWarning("[IncomeProcessor] PlayerNation not found!");
                return;
            }
        }
        
        // Recalculate province stats first
        playerNation.RecalculateStats();
        
        // Calculate income with multipliers
        float taxIncome = playerNation.TaxIncome * taxMultiplier;
        float tradeIncome = playerNation.TradeIncome * tradeMultiplier;
        float totalIncome = taxIncome + tradeIncome;
        
        // Add to player's gold
        float previousGold = playerNation.nationMoney;
        playerNation.nationMoney += totalIncome;
        
        if (logIncome)
        {
            Debug.Log($"[IncomeProcessor] Turn {turnNumber} Income:");
            Debug.Log($"  Tax:   +{taxIncome:F0} (×{taxMultiplier})");
            Debug.Log($"  Trade: +{tradeIncome:F0} (×{tradeMultiplier})");
            Debug.Log($"  Total: +{totalIncome:F0}");
            Debug.Log($"  Gold:  {previousGold:F0} → {playerNation.nationMoney:F0}");
        }
        
        // Fire event for UI updates
        GameEvents.PlayerStatsChanged();
    }
    
    #region Modifier API
    
    /// <summary>
    /// Set tax income multiplier (for upgrades/buffs).
    /// </summary>
    public void SetTaxMultiplier(float multiplier)
    {
        taxMultiplier = multiplier;
        Debug.Log($"[IncomeProcessor] Tax multiplier set to: {multiplier}");
    }
    
    /// <summary>
    /// Set trade income multiplier (for upgrades/buffs).
    /// </summary>
    public void SetTradeMultiplier(float multiplier)
    {
        tradeMultiplier = multiplier;
        Debug.Log($"[IncomeProcessor] Trade multiplier set to: {multiplier}");
    }
    
    /// <summary>
    /// Add to tax multiplier (additive buff).
    /// </summary>
    public void AddTaxBonus(float bonus)
    {
        taxMultiplier += bonus;
    }
    
    /// <summary>
    /// Add to trade multiplier (additive buff).
    /// </summary>
    public void AddTradeBonus(float bonus)
    {
        tradeMultiplier += bonus;
    }
    
    #endregion
}
