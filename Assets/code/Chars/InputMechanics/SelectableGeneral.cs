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
    [Tooltip("Preferred: DirectionalSpriteAnimator for 8-way walk-cycle animation")]
    [SerializeField] private DirectionalSpriteAnimator spriteAnimator;

    [Tooltip("Fallback: DirectionalSprite component for 8-way static facing")]
    [SerializeField] private DirectionalSprite directionalSprite;

    // State
    private General _general;
    private bool _isSelected = false;
    private Vector2 _moveDirection;
    private bool _controlLocked;

    // Walk animation state (ported from Horse.cs)
    private Vector2 _currentAnimDirection = Vector2.down;
    private float _directionHoldTimer = 0f;
    // Hold diagonal facing briefly when one key is released, so the split-second
    // transition from diagonal to cardinal input doesn't flicker the sprite
    private const float DIRECTION_HOLD_DURATION = 0.15f;
    private HashSet<ProvinceModel> _currentProvinces = new HashSet<ProvinceModel>();
    private ProvinceModel _currentProvince;
    private CityCenter _currentCityCenter;

    // Reused non-alloc physics query state (province / city-center / river detection)
    private Collider2D[] _overlapResults = new Collider2D[64];
    private ContactFilter2D _overlapFilter;
    private Vector2 _lastScanPosition = Vector2.positiveInfinity;
    // Only re-scan when the general has actually moved this far; an idle general's overlap results cannot change.
    private const float SCAN_REPOSITION_THRESHOLD = 0.05f;
    
    // Properties
    public string DisplayName => displayName;
    public bool IsKhan => isKhan;
    public bool IsSelected => _isSelected;
    public bool IsControlLocked => _controlLocked;
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

        if (spriteAnimator == null)
            spriteAnimator = GetComponent<DirectionalSpriteAnimator>();

        if (_general == null)
            _general = GetComponent<General>();

        // IMPORTANT: Make rigidbody kinematic to prevent physical collisions between generals
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = false;
        }
        
        // Make collider a trigger so it doesn't push other objects
        if (clickCollider != null)
        {
            clickCollider.isTrigger = true;
        }
        
        // Set default display name if not set
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;

        // Build the physics query filter once: trigger colliders on the Default layer
        // (all province / river / city-center colliders live on layer 0).
        _overlapFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Default")
        };
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
            GameLog.Warning(GameLogCategory.Core, $"[SelectableGeneral] {displayName}: Manager not found in OnEnable, will retry");
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
        if (_controlLocked)
        {
            _moveDirection = Vector2.zero;
            return;
        }

        // ONLY process input if selected
        if (!_isSelected) return;

        EnsureGeneralReference();
        if (IsRegroupingAndMovementInputPressed())
        {
            _moveDirection = Vector2.zero;
            CenterWarningPopupSpawner.Show($"Army regrouping. Wait {_general.RegroupTurnsRemaining} turns!");
            return;
        }
        
        HandleInput();

        // Only re-scan when the general has actually moved; an idle general's
        // overlap results cannot change between frames.
        Vector2 currentPosition = transform.position;
        if ((currentPosition - _lastScanPosition).sqrMagnitude > SCAN_REPOSITION_THRESHOLD * SCAN_REPOSITION_THRESHOLD)
        {
            _lastScanPosition = currentPosition;
            CheckCurrentProvince();
            CheckCityCenter();
        }
    }
    
    private void FixedUpdate()
    {
        if (_controlLocked) return;
        EnsureGeneralReference();
        if (_general != null && _general.IsRegrouping) return;
        if (!_isSelected || _moveDirection.sqrMagnitude < 0.0001f) return;

        Vector2 targetPos = rb.position + _moveDirection * moveSpeed * Time.fixedDeltaTime;
        ProvinceModel targetProvince = ResolveTopProvince(targetPos, false);
        if (targetProvince != null && targetProvince != _currentProvince)
        {
            Army army = _general != null ? _general.CommandedArmy : null;
            SupplyMovementCoordinator supply = SupplyMovementCoordinator.Instance;
            if (supply != null && !supply.CanEnterProvince(army, _currentProvince, targetProvince, out string reason))
            {
                CenterWarningPopupSpawner.Show(reason);
                return;
            }
        }

        if (!IsPositionBlocked(targetPos))
            rb.MovePosition(targetPos);
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

        // Update sprite facing - prefer walk-cycle animator, then static directional sprite
        if (spriteAnimator != null)
        {
            UpdateWalkAnimation(input);
        }
        else if (directionalSprite != null && _moveDirection.sqrMagnitude > 0.01f)
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

    /// <summary>
    /// Drive the 8-way walk-cycle animator (ported from Horse.cs).
    /// Holds diagonal facing briefly when transitioning to cardinal input
    /// so releasing one key of a diagonal pair doesn't flicker the sprite.
    /// </summary>
    private void UpdateWalkAnimation(Vector2 input)
    {
        bool isDiagonalInput = Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) > 0.1f;

        if (input.sqrMagnitude > 0.5f) // Moving
        {
            if (isDiagonalInput)
            {
                // Definitely diagonal -> update immediately & reset hold timer
                _currentAnimDirection = _moveDirection;
                _directionHoldTimer = DIRECTION_HOLD_DURATION;
                spriteAnimator.SetDirection(_currentAnimDirection);
            }
            else if (_directionHoldTimer > 0)
            {
                // Recently diagonal - keep animating with the old (diagonal) direction
                // to cover the split-second release delay
                _directionHoldTimer -= Time.deltaTime;
                spriteAnimator.SetDirection(_currentAnimDirection);
            }
            else
            {
                // Hold time expired -> user really means to go cardinal now
                _currentAnimDirection = _moveDirection;
                spriteAnimator.SetDirection(_currentAnimDirection);
            }
        }
        else if (input.sqrMagnitude < 0.01f) // Stopped
        {
            // Stop on whatever direction is active - facing is preserved as idle
            spriteAnimator.SetDirection(_currentAnimDirection);
            spriteAnimator.StopMoving();
            _directionHoldTimer = 0f;
        }
    }

    #endregion
    
    #region Selection Callbacks
    
    /// <summary>
    /// Called by GeneralSelectionManager when this general is selected.
    /// </summary>
    public void OnSelected()
    {
        if (_controlLocked)
        {
            _isSelected = false;
            _moveDirection = Vector2.zero;
            UpdateVisuals();
            GameLog.Log(GameLogCategory.Core, $"[SelectableGeneral] {displayName} is locked and cannot be selected");
            return;
        }

        _isSelected = true;
        UpdateVisuals();
        
        GameLog.Log(GameLogCategory.Core, $"[SelectableGeneral] {displayName} SELECTED - now receiving input");
    }
    
    /// <summary>
    /// Called by GeneralSelectionManager when this general is deselected.
    /// </summary>
    public void OnDeselected()
    {
        _isSelected = false;
        _moveDirection = Vector2.zero; // Stop movement immediately

        // Halt walk animation on the current facing
        if (spriteAnimator != null)
        {
            spriteAnimator.StopMoving();
        }

        UpdateVisuals();
        
        GameLog.Log(GameLogCategory.Core, $"[SelectableGeneral] {displayName} DESELECTED - input disabled");
    }
    
    private void UpdateVisuals()
    {
        // Show/hide selection indicator
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(_isSelected);
            
            // Update name on indicator when selected
            if (_isSelected)
            {
                SelectionIndicator indicator = selectionIndicator.GetComponent<SelectionIndicator>();
                if (indicator != null)
                {
                    string nameToShow = isKhan ? "Khan" : displayName;
                    indicator.SetGeneralName(nameToShow);
                }
            }
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
        ProvinceModel topProvince = ResolveTopProvince(transform.position, true);
        
        if (_currentProvince != topProvince)
        {
            ProvinceModel oldProvince = _currentProvince;
            _currentProvince = topProvince;
            
            if (oldProvince != null)
                GameEvents.ProvinceExit(oldProvince);
            
            if (topProvince != null)
                GameEvents.ProvinceEnter(topProvince);
        }
    }
    
    private ProvinceModel ResolveTopProvince(Vector2 position, bool collectProvinces)
    {
        int hitCount = Physics2D.OverlapPoint(position, _overlapFilter, _overlapResults);
        if (collectProvinces)
            _currentProvinces.Clear();

        ProvinceModel topProvince = null;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _overlapResults[i];
            if (!hit.CompareTag("Province")) continue;

            ProvinceModel province = hit.GetComponent<ProvinceModel>();
            if (province == null) continue;
            if (collectProvinces) _currentProvinces.Add(province);
            if (topProvince == null) topProvince = province;
        }

        return topProvince;
    }
    private void CheckCityCenter()
    {
        int hitCount = Physics2D.OverlapPoint(transform.position, _overlapFilter, _overlapResults);
        
        CityCenter detectedCityCenter = null;
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _overlapResults[i];
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
            CityCenter oldCenter = _currentCityCenter;
            _currentCityCenter = detectedCityCenter;
            
            if (oldCenter != null)
            {
                oldCenter.SetHighlight(false);
                GameEvents.CityCenterExit(oldCenter);
            }
            
            if (detectedCityCenter != null)
            {
                detectedCityCenter.SetHighlight(true);
                GameEvents.CityCenterEnter(detectedCityCenter);
            }
        }
    }
    
    private bool IsPositionBlocked(Vector2 position)
    {
        int hitCount = Physics2D.OverlapPoint(position, _overlapFilter, _overlapResults);
        bool overlapsProvince = false;
        for (int i = 0; i < hitCount; i++)
        {
            if (_overlapResults[i].CompareTag("River"))
                return true;

            if (_overlapResults[i].CompareTag("Province"))
                overlapsProvince = true;
        }
        return !overlapsProvince;
    }
    
    #endregion
    
    #region Click Selection
    
    /// <summary>
    /// Call this when the general is clicked (from GeneralClickSelector or UI).
    /// </summary>
    public void HandleClick()
    {
        if (_controlLocked) return;

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
    /// Find the Khan's SelectableGeneral in the scene (replaces FindFirstObjectByType&lt;Horse&gt;).
    /// </summary>
    public static SelectableGeneral FindKhan()
    {
        foreach (var general in FindObjectsByType<SelectableGeneral>(FindObjectsSortMode.None))
        {
            if (general.isKhan) return general;
        }
        return null;
    }

    /// <summary>
    /// Set the display name at runtime.
    /// </summary>
    public void SetDisplayName(string name)
    {
        displayName = name;
    }
    
    /// <summary>
    /// Set whether this general is the Khan.
    /// </summary>
    public void SetIsKhan(bool value)
    {
        isKhan = value;
    }
    
    /// <summary>
    /// Set movement speed at runtime.
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void SetControlLocked(bool locked)
    {
        _controlLocked = locked;
        if (_controlLocked)
        {
            _isSelected = false;
            _moveDirection = Vector2.zero;

            if (spriteAnimator != null)
                spriteAnimator.StopMoving();
        }

        UpdateVisuals();
    }

    private bool IsRegroupingAndMovementInputPressed()
    {
        if (_general == null || !_general.IsRegrouping || Keyboard.current == null)
            return false;

        return Keyboard.current.wKey.isPressed
            || Keyboard.current.aKey.isPressed
            || Keyboard.current.sKey.isPressed
            || Keyboard.current.dKey.isPressed;
    }

    private void EnsureGeneralReference()
    {
        if (_general == null)
            _general = GetComponent<General>();
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
        _lastScanPosition = transform.position;
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
