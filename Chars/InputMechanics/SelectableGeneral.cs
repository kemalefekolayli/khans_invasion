using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Component that makes a general selectable and controllable.
/// Only the selected general receives keyboard input.
/// 
/// REPLACES: Horse.cs movement logic
/// ADD TO: Each general prefab (Khan, other generals)
/// 
/// This component handles:
/// - Registration with GeneralSelectionManager
/// - Input processing (only when selected)
/// - Visual feedback for selection state
/// - Province detection (delegated from Horse)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SelectableGeneral : MonoBehaviour, IProvinceDetector
{
    [Header("Identity")]
    [SerializeField] private string displayName = "General";
    [SerializeField] private bool isKhan = false;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Selection Visuals")]
    [SerializeField] private GameObject selectionIndicator; // Optional: child object that shows when selected
    [SerializeField] private Color selectedTint = Color.white;
    [SerializeField] private Color deselectedTint = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private bool dimWhenNotSelected = true;
    
    [Header("Click Selection")]
    [SerializeField] private Collider2D clickCollider; // For click-to-select (optional)
    
    [Header("Directional Sprites")]
    [Tooltip("Optional: DirectionalSprite component for 8-way facing")]
    [SerializeField] private DirectionalSprite directionalSprite;
    
    // State
    private bool _isSelected = false;
    private Vector2 _moveDirection;
    private HashSet<ProvinceModel> _currentProvinces = new HashSet<ProvinceModel>();
    private ProvinceModel _currentProvince;
    private CityCenter _currentCityCenter;
    
    // Properties
    public string DisplayName => displayName;
    public bool IsKhan => isKhan;
    public bool IsSelected => _isSelected;
    public float MoveSpeed => moveSpeed;
    
    // IProvinceDetector implementation
    public ProvinceModel CurrentProvince => _currentProvince;
    public CityCenter CurrentCityCenter => _currentCityCenter;
    public Vector3 Position => transform.position;
    public bool IsOnCityCenter => _currentCityCenter != null;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (clickCollider == null)
            clickCollider = GetComponent<Collider2D>();
        
        // Set default display name if not set
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;
    }
    
    private void OnEnable()
    {
        // Register with selection manager
        if (GeneralSelectionManager.Instance != null)
        {
            GeneralSelectionManager.Instance.RegisterGeneral(this);
        }
        else
        {
            // Manager might not exist yet, try again in Start
            Debug.LogWarning($"[SelectableGeneral] {displayName}: Manager not found in OnEnable, will retry");
        }
    }
    
    private void Start()
    {
        // Retry registration if failed in OnEnable
        if (GeneralSelectionManager.Instance != null)
        {
            GeneralSelectionManager.Instance.RegisterGeneral(this);
        }
        
        // Apply initial visual state
        UpdateVisuals();
    }
    
    private void OnDisable()
    {
        // Unregister from selection manager
        if (GeneralSelectionManager.Instance != null)
        {
            GeneralSelectionManager.Instance.UnregisterGeneral(this);
        }
    }
    
    private void Update()
    {
        // ONLY process input if selected
        if (!_isSelected) return;
        
        HandleInput();
        CheckCurrentProvince();
        CheckCityCenter();
    }
    
    private void FixedUpdate()
    {
        // ONLY move if selected
        if (!_isSelected) return;
        
        if (_moveDirection.sqrMagnitude < 0.0001f) return;
        
        Vector2 targetPos = rb.position + _moveDirection * moveSpeed * Time.fixedDeltaTime;
        
        if (!IsPositionBlocked(targetPos))
        {
            rb.MovePosition(targetPos);
        }
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleInput()
    {
        if (Keyboard.current == null) return;
        
        Vector2 input = Vector2.zero;
        
        // WASD movement
        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;
        
        _moveDirection = input.normalized;
        
        // Update sprite facing - prefer DirectionalSprite if available
        if (directionalSprite != null && _moveDirection.sqrMagnitude > 0.01f)
        {
            directionalSprite.SetDirectionFromVector(_moveDirection);
        }
        else if (spriteRenderer != null)
        {
            // Fallback: simple flip for left/right
            if (_moveDirection.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (_moveDirection.x < -0.01f)
                spriteRenderer.flipX = true;
        }
    }
    
    #endregion
    
    #region Selection Callbacks
    
    /// <summary>
    /// Called by GeneralSelectionManager when this general is selected.
    /// </summary>
    public void OnSelected()
    {
        _isSelected = true;
        UpdateVisuals();
        
        Debug.Log($"[SelectableGeneral] {displayName} SELECTED - now receiving input");
    }
    
    /// <summary>
    /// Called by GeneralSelectionManager when this general is deselected.
    /// </summary>
    public void OnDeselected()
    {
        _isSelected = false;
        _moveDirection = Vector2.zero; // Stop movement immediately
        UpdateVisuals();
        
        Debug.Log($"[SelectableGeneral] {displayName} DESELECTED - input disabled");
    }
    
    private void UpdateVisuals()
    {
        // Show/hide selection indicator
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(_isSelected);
        }
        
        // Tint sprite based on selection
        if (spriteRenderer != null && dimWhenNotSelected)
        {
            spriteRenderer.color = _isSelected ? selectedTint : deselectedTint;
        }
    }
    
    #endregion
    
    #region Province Detection (from Horse.cs)
    
    private void CheckCurrentProvince()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        
        _currentProvinces.Clear();
        ProvinceModel topProvince = null;
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Province"))
            {
                ProvinceModel province = hit.GetComponent<ProvinceModel>();
                if (province != null)
                {
                    _currentProvinces.Add(province);
                    if (topProvince == null)
                        topProvince = province;
                }
            }
        }
        
        if (_currentProvince != topProvince)
        {
            if (_currentProvince != null)
                GameEvents.ProvinceExit(_currentProvince);
            
            if (topProvince != null)
                GameEvents.ProvinceEnter(topProvince);
            
            _currentProvince = topProvince;
        }
    }
    
    private void CheckCityCenter()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        
        CityCenter detectedCityCenter = null;
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("CityCenter"))
            {
                CityCenter center = hit.GetComponent<CityCenter>();
                if (center != null)
                {
                    detectedCityCenter = center;
                    break;
                }
            }
        }
        
        if (_currentCityCenter != detectedCityCenter)
        {
            if (_currentCityCenter != null)
            {
                _currentCityCenter.SetHighlight(false);
                GameEvents.CityCenterExit(_currentCityCenter);
            }
            
            if (detectedCityCenter != null)
            {
                detectedCityCenter.SetHighlight(true);
                GameEvents.CityCenterEnter(detectedCityCenter);
            }
            
            _currentCityCenter = detectedCityCenter;
        }
    }
    
    private bool IsPositionBlocked(Vector2 position)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(position);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("River"))
                return true;
        }
        return false;
    }
    
    #endregion
    
    #region Click Selection
    
    /// <summary>
    /// Call this when the general is clicked (from GeneralClickSelector or UI).
    /// </summary>
    public void HandleClick()
    {
        if (GeneralSelectionManager.Instance != null)
        {
            GeneralSelectionManager.Instance.Select(this);
        }
    }
    
    // Optional: Direct mouse click detection
    private void OnMouseDown()
    {
        if (GeneralSelectionManager.Instance != null && 
            GeneralSelectionManager.Instance.enableClickSelection)
        {
            HandleClick();
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Set the display name at runtime.
    /// </summary>
    public void SetDisplayName(string name)
    {
        displayName = name;
    }
    
    /// <summary>
    /// Set movement speed at runtime.
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    /// <summary>
    /// Teleport the general to a position.
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        
        // Force province check after teleport
        CheckCurrentProvince();
        CheckCityCenter();
    }
    
    #endregion
    
    #region Debug
    
    [ContextMenu("Select This General")]
    private void DebugSelect()
    {
        HandleClick();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw selection indicator in editor
        Gizmos.color = _isSelected ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
    
    #endregion
}
