using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles keyboard input for general selection.
/// Provides shortcuts to cycle between generals or select by number.
/// 
/// SETUP: Add to the same GameObject as GeneralSelectionManager.
/// </summary>
public class GeneralSelectionInput : MonoBehaviour
{
    [Header("Cycle Keys")]
    [Tooltip("Key to select next general")]
    public Key nextGeneralKey = Key.Tab;
    
    [Tooltip("Key to select previous general (with Shift)")]
    public bool useShiftForPrevious = true;
    
    [Header("Number Keys")]
    [Tooltip("Enable 1-9 keys to select generals by index")]
    public bool enableNumberKeys = true;
    
    [Header("Deselect")]
    [Tooltip("Key to deselect all (optional)")]
    public Key deselectKey = Key.None;
    
    private GeneralSelectionManager manager;
    
    private void Start()
    {
        manager = GeneralSelectionManager.Instance;
        
        if (manager == null)
        {
            Debug.LogWarning("[GeneralSelectionInput] GeneralSelectionManager not found!");
        }
    }
    
    private void Update()
    {
        if (manager == null || Keyboard.current == null) return;
        
        HandleCycleInput();
        HandleNumberKeys();
        HandleDeselectKey();
    }
    
    private void HandleCycleInput()
    {
        if (Keyboard.current[nextGeneralKey].wasPressedThisFrame)
        {
            bool shiftHeld = Keyboard.current.shiftKey.isPressed;
            
            if (useShiftForPrevious && shiftHeld)
            {
                manager.SelectPrevious();
            }
            else
            {
                manager.SelectNext();
            }
        }
    }
    
    private void HandleNumberKeys()
    {
        if (!enableNumberKeys) return;
        
        // Keys 1-9 select generals by index
        Key[] numberKeys = new Key[]
        {
            Key.Digit1, Key.Digit2, Key.Digit3,
            Key.Digit4, Key.Digit5, Key.Digit6,
            Key.Digit7, Key.Digit8, Key.Digit9
        };
        
        for (int i = 0; i < numberKeys.Length; i++)
        {
            if (Keyboard.current[numberKeys[i]].wasPressedThisFrame)
            {
                manager.SelectByIndex(i);
                break;
            }
        }
    }
    
    private void HandleDeselectKey()
    {
        if (deselectKey == Key.None) return;
        
        if (Keyboard.current[deselectKey].wasPressedThisFrame)
        {
            manager.Deselect();
        }
    }
}
