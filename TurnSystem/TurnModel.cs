using UnityEngine;

/// <summary>
/// DEPRECATED: Use TurnManager instead.
/// This class is kept for backward compatibility only.
/// </summary>
[System.Obsolete("Use TurnManager instead. This class will be removed in a future version.")]
public class TurnModel : MonoBehaviour
{
    public enum TurnPhase
    {
        PlayerTurn,
        EnemyTurn,
    }
    
    // Delegate to TurnManager
    public TurnPhase currentPhase 
    {
        get 
        {
            if (TurnManager.Instance == null) return TurnPhase.PlayerTurn;
            return TurnManager.Instance.IsPlayerTurn ? TurnPhase.PlayerTurn : TurnPhase.EnemyTurn;
        }
    }
    
    public int turnNumber 
    {
        get => TurnManager.Instance?.CurrentTurn ?? 1;
    }

    /// <summary>
    /// DEPRECATED: Use TurnManager.Instance.EndPlayerTurn() instead.
    /// </summary>
    public void AdvanceTurn()
    {
        Debug.LogWarning("[TurnModel] AdvanceTurn() is deprecated. Use TurnManager.Instance.EndPlayerTurn() instead.");
        TurnManager.Instance?.EndPlayerTurn();
    }
}
