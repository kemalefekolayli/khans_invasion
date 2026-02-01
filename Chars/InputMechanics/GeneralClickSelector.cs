using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles click-to-select functionality for generals in the game world.
/// Uses raycasting to detect clicks on SelectableGeneral components.
/// 
/// SETUP: Add to a persistent GameObject (e.g., with GeneralSelectionManager).
/// </summary>
public class GeneralClickSelector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Mouse button for selection (0=Left, 1=Right, 2=Middle)")]
    public int mouseButton = 0;
    
    [Tooltip("Layer mask for general detection")]
    public LayerMask generalLayerMask = ~0;
    
    [Tooltip("Require double-click to select")]
    public bool requireDoubleClick = false;
    
    [Tooltip("Time window for double-click detection")]
    public float doubleClickTime = 0.3f;
    
    [Header("Visual Feedback")]
    [Tooltip("Show highlight when hovering over a general")]
    public bool showHoverHighlight = true;
    
    private Camera mainCamera;
    private float lastClickTime;
    private SelectableGeneral hoveredGeneral;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }
        
        // Check for hover
        if (showHoverHighlight)
        {
            UpdateHover();
        }
        
        // Check for click
        if (Mouse.current == null) return;
        
        bool clicked = mouseButton switch
        {
            0 => Mouse.current.leftButton.wasPressedThisFrame,
            1 => Mouse.current.rightButton.wasPressedThisFrame,
            2 => Mouse.current.middleButton.wasPressedThisFrame,
            _ => false
        };
        
        if (clicked)
        {
            HandleClick();
        }
    }
    
    private void HandleClick()
    {
        // Double-click check
        if (requireDoubleClick)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;
            
            if (timeSinceLastClick > doubleClickTime)
            {
                return; // Wait for second click
            }
        }
        
        // Raycast to find general
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, generalLayerMask);
        
        if (hit.collider != null)
        {
            SelectableGeneral general = hit.collider.GetComponent<SelectableGeneral>();
            
            if (general != null)
            {
                general.HandleClick();
            }
        }
    }
    
    private void UpdateHover()
    {
        if (Mouse.current == null || mainCamera == null) return;
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, generalLayerMask);
        
        SelectableGeneral newHovered = null;
        
        if (hit.collider != null)
        {
            newHovered = hit.collider.GetComponent<SelectableGeneral>();
        }
        
        // Hover state changed
        if (newHovered != hoveredGeneral)
        {
            // Clear old hover
            if (hoveredGeneral != null)
            {
                // Could add hover-off visual here
            }
            
            // Set new hover
            if (newHovered != null)
            {
                // Could add hover-on visual here (cursor change, outline, etc.)
            }
            
            hoveredGeneral = newHovered;
        }
    }
}
