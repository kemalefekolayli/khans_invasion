using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Player-controlled horse character.
/// Handles movement, province detection, and city center detection.
/// 
/// PERFORMANCE OPTIMIZATIONS:
/// 1. Province/CityCenter checks only run when horse is moving
/// 2. Reuses Collider2D array instead of allocating new one each frame
/// 3. Position caching to avoid redundant checks when stationary
/// </summary>
public class Horse : MonoBehaviour, IProvinceDetector // this is deprecated
{
    [Header("Movement")]
    public Rigidbody2D horseRigidBody;
    public float moveSpeed = 5f;
    
    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    
    [Header("Animation")]
    [Tooltip("8-directional animator component")]
    public DirectionalSpriteAnimator spriteAnimator;

    // Movement state
    private Vector2 moveDir;
    private Vector3 lastCheckedPosition;
    
    // Province tracking
    private HashSet<ProvinceModel> currentProvinces = new HashSet<ProvinceModel>();
    private ProvinceModel currentProvince;
    
    // City center tracking
    private CityCenter currentCityCenter;
    
    // Performance: reusable list for physics queries
    private List<Collider2D> hitBuffer = new List<Collider2D>(20);
    private ContactFilter2D contactFilter = new ContactFilter2D().NoFilter();
    
    // Minimum distance to trigger a new check (avoids micro-movement spam)
    private const float MIN_CHECK_DISTANCE = 0.05f;

    // IProvinceDetector implementation
    public ProvinceModel CurrentProvince => currentProvince;
    public Vector3 Position => transform.position;
    public CityCenter CurrentCityCenter => currentCityCenter;
    public bool IsOnCityCenter => currentCityCenter != null;

    private void Awake()
    {
        if (horseRigidBody == null)
            horseRigidBody = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Auto-find animator
        if (spriteAnimator == null)
            spriteAnimator = GetComponent<DirectionalSpriteAnimator>();
            
        lastCheckedPosition = transform.position;
    }

    private void Update()
    {

        SelectableGeneral selectable = GetComponent<SelectableGeneral>();
        if (selectable != null && !selectable.IsSelected) return;
        HandleInput();
        
        // OPTIMIZATION: Only check collisions if we've moved enough
        if (HasMovedEnough())
        {
            CheckCurrentProvince();
            CheckCityCenter();
            lastCheckedPosition = transform.position;
        }
    }

    private void FixedUpdate()
    {
        if (moveDir.sqrMagnitude < 0.0001f) return;

        Vector2 targetPos = horseRigidBody.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        if (!IsPositionBlocked(targetPos))
            horseRigidBody.MovePosition(targetPos);
    }

    // Track current active direction
    private Vector2 currentDirection = Vector2.down;
    
    // Buffer to hold diagonal direction when transitioning to single key release
    private float directionHoldTimer = 0f;
    private const float DIRECTION_HOLD_DURATION = 0.15f; // Wait 150ms before switching from diagonal to cardinal
    
    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;

        moveDir = input.normalized;
        
        // Is this frame diagonal input?
        bool isDiagonalInput = Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) > 0.1f;

        if (spriteAnimator != null)
        {
            if (input.sqrMagnitude > 0.5f) // Moving
            {
                if (isDiagonalInput)
                {
                    // Definitely diagonal -> update immediately & reset hold timer
                    currentDirection = moveDir;
                    directionHoldTimer = DIRECTION_HOLD_DURATION;
                    spriteAnimator.SetDirection(currentDirection);
                }
                else
                {
                    // Cardinal input (single key)
                    if (directionHoldTimer > 0)
                    {
                        // We recently had diagonal input. 
                        // Don't switch to cardinal yet! Hold previous direction.
                        // This covers the split-second release delay.
                        directionHoldTimer -= Time.deltaTime;
                        
                        // Keep animating, but use OLD direction (diagonal)
                        spriteAnimator.SetDirection(currentDirection); 
                    }
                    else
                    {
                        // Hold time expired -> user really means to go cardinal now
                        currentDirection = moveDir;
                        spriteAnimator.SetDirection(currentDirection);
                    }
                }
            }
            else if (input.sqrMagnitude < 0.01f) // Stopped
            {
                // Just stop using whatever currentDirection is active, no corrections needed.
                // Because we held the diagonal direction during the release, currentDirection is STILL diagonal!
                spriteAnimator.SetDirection(currentDirection);
                spriteAnimator.StopMoving();
                
                // Clear timer
                directionHoldTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Check if horse has moved enough to warrant a new collision check
    /// </summary>
    private bool HasMovedEnough()
    {
        float distance = Vector3.Distance(transform.position, lastCheckedPosition);
        return distance >= MIN_CHECK_DISTANCE;
    }

    private void CheckCurrentProvince()
    {
        // New Unity API - non-allocating with List
        hitBuffer.Clear();
        Physics2D.OverlapPoint(transform.position, contactFilter, hitBuffer);

        currentProvinces.Clear();
        ProvinceModel topProvince = null;

        for (int i = 0; i < hitBuffer.Count; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit.CompareTag("Province"))
            {
                ProvinceModel province = hit.GetComponent<ProvinceModel>();
                if (province != null)
                {
                    currentProvinces.Add(province);
                    if (topProvince == null)
                        topProvince = province;
                }
            }
        }

        // Only fire events on change
        if (currentProvince != topProvince)
        {
            if (currentProvince != null)
                GameEvents.ProvinceExit(currentProvince);

            if (topProvince != null)
                GameEvents.ProvinceEnter(topProvince);

            currentProvince = topProvince;
        }
    }

    private void CheckCityCenter()
    {
        hitBuffer.Clear();
        Physics2D.OverlapPoint(transform.position, contactFilter, hitBuffer);

        CityCenter detectedCityCenter = null;

        for (int i = 0; i < hitBuffer.Count; i++)
        {
            Collider2D hit = hitBuffer[i];
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

        // Only fire events on change (not every frame!)
        if (currentCityCenter != detectedCityCenter)
        {
            if (currentCityCenter != null)
            {
                currentCityCenter.SetHighlight(false);
                GameEvents.CityCenterExit(currentCityCenter);
            }

            if (detectedCityCenter != null)
            {
                detectedCityCenter.SetHighlight(true);
                GameEvents.CityCenterEnter(detectedCityCenter);
            }

            currentCityCenter = detectedCityCenter;
        }
    }

    private bool IsPositionBlocked(Vector2 position)
    {
        hitBuffer.Clear();
        Physics2D.OverlapPoint(position, contactFilter, hitBuffer);
        
        for (int i = 0; i < hitBuffer.Count; i++)
        {
            if (hitBuffer[i].CompareTag("River"))
                return true;
        }
        return false;
    }
}