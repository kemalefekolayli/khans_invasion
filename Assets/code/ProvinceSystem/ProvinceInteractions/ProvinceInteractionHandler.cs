using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles closing province panels with Escape key.
/// Opening is now handled by InteractionButtonController.
/// </summary>
public class ProvinceInteractionHandler : MonoBehaviour
{
    [Header("Input Settings")]
    public Key closeKey = Key.Escape;
    
    private bool panelOpen = false;

    private void OnEnable()
    {
        GameEvents.OnProvinceManagementOpened += OnPanelOpened;
        GameEvents.OnProvinceInteractionOpened += OnPanelOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceManagementOpened -= OnPanelOpened;
        GameEvents.OnProvinceInteractionOpened -= OnPanelOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
    }

    private void OnPanelOpened(ProvinceModel province)
    {
        panelOpen = true;
    }

    private void OnPanelClosed()
    {
        panelOpen = false;
    }

    private void Update()
    {
        if (!panelOpen) return;
        if (Keyboard.current == null) return;
        
        // Close with Escape or C
        if (Keyboard.current[closeKey].wasPressedThisFrame ||
            Keyboard.current[Key.C].wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    private void ClosePanel()
    {
        panelOpen = false;
        GameEvents.ProvincePanelClosed();
    }

    // Public method for UI close buttons
    public void RequestClosePanel()
    {
        ClosePanel();
    }
}