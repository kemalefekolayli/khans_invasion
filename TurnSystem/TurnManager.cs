using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central manager for the turn-based system.
/// Handles phase transitions: Player Turn → AI Turns → End Turn Calculations → New Turn
/// 
/// SETUP: Add to a persistent GameObject in the scene.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    [Header("Turn State")]
    [SerializeField] private int currentTurn = 1;
    [SerializeField] private TurnPhase currentPhase = TurnPhase.PlayerTurn;
    
    [Header("Settings")]
    [Tooltip("If true, AI nations will be processed during their turn (future feature)")]
    [SerializeField] private bool enableAITurns = false;
    
    [Header("Debug")]
    [SerializeField] private bool logTurnEvents = true;
    
    // Turn phases
    public enum TurnPhase
    {
        PlayerTurn,         // Player can take actions
        AIProcessing,       // AI nations take their turns (future)
        EndTurnCalculations,// Income, upkeep, growth, etc.
        TurnTransition      // Brief transition state
    }
    
    // Events for turn system
    public static event Action OnPlayerTurnStart;
    public static event Action OnPlayerTurnEnd;
    public static event Action<int> OnTurnStart;        // int = turn number
    public static event Action<int> OnTurnEnd;          // int = turn number  
    public static event Action OnAITurnsStart;
    public static event Action OnAITurnsComplete;
    public static event Action OnEndTurnCalculationsStart;
    public static event Action OnEndTurnCalculationsComplete;
    
    // Properties
    public int CurrentTurn => currentTurn;
    public TurnPhase CurrentPhase => currentPhase;
    public bool IsPlayerTurn => currentPhase == TurnPhase.PlayerTurn;
    public bool CanPlayerAct => currentPhase == TurnPhase.PlayerTurn;
    
    // Registered processors
    private List<ITurnProcessor> turnProcessors = new List<ITurnProcessor>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✓ TurnManager initialized");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Start the first turn
        StartPlayerTurn();
    }
    
    #region Turn Flow
    
    /// <summary>
    /// Called when the player clicks the End Turn button.
    /// Initiates the turn transition sequence.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (currentPhase != TurnPhase.PlayerTurn)
        {
            Debug.LogWarning($"[TurnManager] Cannot end turn - not player's turn! (Phase: {currentPhase})");
            return;
        }
        
        if (logTurnEvents)
        {
            Debug.Log($"[TurnManager] ═══ PLAYER TURN {currentTurn} ENDED ═══");
        }
        
        OnPlayerTurnEnd?.Invoke();
        
        // Process AI turns if enabled
        if (enableAITurns)
        {
            ProcessAITurns();
        }
        else
        {
            // Skip AI, go directly to end turn calculations
            ProcessEndTurnCalculations();
        }
    }
    
    /// <summary>
    /// Process AI nation turns (placeholder for future implementation).
    /// </summary>
    private void ProcessAITurns()
    {
        currentPhase = TurnPhase.AIProcessing;
        
        if (logTurnEvents)
        {
            Debug.Log("[TurnManager] Processing AI turns...");
        }
        
        OnAITurnsStart?.Invoke();
        
        // TODO: Future AI implementation
        // For each AI nation:
        //   - Process AI decisions
        //   - Move armies
        //   - Build/manage provinces
        //   - etc.
        
        OnAITurnsComplete?.Invoke();
        
        // After AI, do end turn calculations
        ProcessEndTurnCalculations();
    }
    
    /// <summary>
    /// Process end-of-turn calculations (income, upkeep, population growth, etc.).
    /// </summary>
    private void ProcessEndTurnCalculations()
    {
        currentPhase = TurnPhase.EndTurnCalculations;
        
        if (logTurnEvents)
        {
            Debug.Log("[TurnManager] Processing end-of-turn calculations...");
        }
        
        OnEndTurnCalculationsStart?.Invoke();
        
        // Process all registered turn processors
        foreach (var processor in turnProcessors)
        {
            try
            {
                processor.ProcessTurnEnd(currentTurn);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TurnManager] Error in turn processor: {e.Message}");
            }
        }
        
        // Fire legacy event for backwards compatibility
        GameEvents.TurnEnded(currentTurn);
        
        OnEndTurnCalculationsComplete?.Invoke();
        
        // Advance to next turn
        AdvanceToNextTurn();
    }
    
    /// <summary>
    /// Advance to the next turn and start player phase.
    /// </summary>
    private void AdvanceToNextTurn()
    {
        currentPhase = TurnPhase.TurnTransition;
        
        OnTurnEnd?.Invoke(currentTurn);
        
        currentTurn++;
        
        if (logTurnEvents)
        {
            Debug.Log($"[TurnManager] ═══════════════════════════════════");
            Debug.Log($"[TurnManager] ═══     TURN {currentTurn} BEGINS     ═══");
            Debug.Log($"[TurnManager] ═══════════════════════════════════");
        }
        
        OnTurnStart?.Invoke(currentTurn);
        
        StartPlayerTurn();
    }
    
    /// <summary>
    /// Start the player's turn.
    /// </summary>
    private void StartPlayerTurn()
    {
        currentPhase = TurnPhase.PlayerTurn;
        
        if (logTurnEvents)
        {
            Debug.Log($"[TurnManager] Player turn {currentTurn} started - awaiting actions...");
        }
        
        OnPlayerTurnStart?.Invoke();
    }
    
    #endregion
    
    #region Turn Processor Registration
    
    /// <summary>
    /// Register a turn processor to be called at end of each turn.
    /// </summary>
    public void RegisterTurnProcessor(ITurnProcessor processor)
    {
        if (!turnProcessors.Contains(processor))
        {
            turnProcessors.Add(processor);
            
            if (logTurnEvents)
            {
                Debug.Log($"[TurnManager] Registered turn processor: {processor.GetType().Name}");
            }
        }
    }
    
    /// <summary>
    /// Unregister a turn processor.
    /// </summary>
    public void UnregisterTurnProcessor(ITurnProcessor processor)
    {
        turnProcessors.Remove(processor);
    }
    
    #endregion
    
    #region External API
    
    /// <summary>
    /// Enable or disable AI turn processing at runtime.
    /// </summary>
    public void SetAIEnabled(bool enabled)
    {
        enableAITurns = enabled;
        Debug.Log($"[TurnManager] AI turns {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>
    /// Get the current turn number.
    /// </summary>
    public int GetCurrentTurn()
    {
        return currentTurn;
    }
    
    /// <summary>
    /// Check if the player can currently perform actions.
    /// </summary>
    public bool CanPlayerTakeAction()
    {
        return currentPhase == TurnPhase.PlayerTurn;
    }
    
    #endregion
    
    #region Debug
    
    [ContextMenu("End Player Turn")]
    private void DebugEndTurn()
    {
        EndPlayerTurn();
    }
    
    [ContextMenu("Log Turn State")]
    private void LogTurnState()
    {
        Debug.Log($"=== Turn Manager State ===");
        Debug.Log($"Turn: {currentTurn}");
        Debug.Log($"Phase: {currentPhase}");
        Debug.Log($"Registered Processors: {turnProcessors.Count}");
    }
    
    #endregion
}
