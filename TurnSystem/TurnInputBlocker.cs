using UnityEngine;

/// <summary>
/// Blocks player input during non-player turns.
/// Attach to any component that should be disabled during AI/processing phases.
/// 
/// Can also be used to prevent movement, building, etc. during turn processing.
/// </summary>
public class TurnInputBlocker : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, this GameObject will be disabled during non-player turns")]
    [SerializeField] private bool disableGameObject = false;
    
    [Tooltip("Components to disable during non-player turn (optional)")]
    [SerializeField] private MonoBehaviour[] componentsToDisable;
    
    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;
    
    private bool wasEnabled = true;
    
    private void OnEnable()
    {
        TurnManager.OnPlayerTurnStart += OnPlayerTurnStart;
        TurnManager.OnPlayerTurnEnd += OnPlayerTurnEnd;
    }
    
    private void OnDisable()
    {
        TurnManager.OnPlayerTurnStart -= OnPlayerTurnStart;
        TurnManager.OnPlayerTurnEnd -= OnPlayerTurnEnd;
    }
    
    private void Start()
    {
        // Check initial state
        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
        {
            BlockInput();
        }
    }
    
    private void OnPlayerTurnStart()
    {
        UnblockInput();
    }
    
    private void OnPlayerTurnEnd()
    {
        BlockInput();
    }
    
    private void BlockInput()
    {
        if (logStateChanges)
        {
            Debug.Log($"[TurnInputBlocker] Blocking input on: {gameObject.name}");
        }
        
        if (disableGameObject)
        {
            wasEnabled = gameObject.activeSelf;
            // Note: We can't actually disable this GameObject or the event won't fire
            // to re-enable it. Use a canvas group or similar instead.
        }
        
        // Disable specified components
        if (componentsToDisable != null)
        {
            foreach (var component in componentsToDisable)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }
        }
    }
    
    private void UnblockInput()
    {
        if (logStateChanges)
        {
            Debug.Log($"[TurnInputBlocker] Unblocking input on: {gameObject.name}");
        }
        
        // Re-enable specified components
        if (componentsToDisable != null)
        {
            foreach (var component in componentsToDisable)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }
        }
    }
}
