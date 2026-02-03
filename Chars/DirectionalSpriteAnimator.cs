using UnityEngine;

/// <summary>
/// 8-directional sprite animation for characters.
/// Automatically plays correct animation based on movement direction.
/// When stopped, shows the idle sprite for that direction.
/// </summary>
public class DirectionalSpriteAnimator : MonoBehaviour
{
    [Header("Idle Sprites (shown when stopped)")]
    [Tooltip("Idle sprite facing South")]
    public Sprite southIdle;
    
    [Tooltip("Idle sprite facing South-East")]
    public Sprite southEastIdle;
    
    [Tooltip("Idle sprite facing East")]
    public Sprite eastIdle;
    
    [Tooltip("Idle sprite facing North-East")]
    public Sprite northEastIdle;
    
    [Tooltip("Idle sprite facing North")]
    public Sprite northIdle;
    
    [Tooltip("Idle sprite facing North-West")]
    public Sprite northWestIdle;
    
    [Tooltip("Idle sprite facing West")]
    public Sprite westIdle;
    
    [Tooltip("Idle sprite facing South-West")]
    public Sprite southWestIdle;
    
    [Header("Running Animation Frames")]
    [Tooltip("4 frames for South direction")]
    public Sprite[] southFrames;
    
    [Tooltip("4 frames for South-East direction")]
    public Sprite[] southEastFrames;
    
    [Tooltip("4 frames for East direction")]
    public Sprite[] eastFrames;
    
    [Tooltip("4 frames for North-East direction")]
    public Sprite[] northEastFrames;
    
    [Tooltip("4 frames for North direction")]
    public Sprite[] northFrames;
    
    [Tooltip("4 frames for North-West direction")]
    public Sprite[] northWestFrames;
    
    [Tooltip("4 frames for West direction")]
    public Sprite[] westFrames;
    
    [Tooltip("4 frames for South-West direction")]
    public Sprite[] southWestFrames;
    
    [Header("Animation Settings")]
    [Tooltip("Frames per second (how fast animation plays)")]
    [Range(1f, 30f)]
    [SerializeField] private float frameRate = 8f;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Debug")]
    [SerializeField] private bool logAnimationChanges = false;
    
    // Current state
    private Sprite[] currentFrames;
    private Sprite currentIdleSprite;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isMoving = false;
    private Direction currentDirection = Direction.South;
    
    // Direction enum
    public enum Direction
    {
        South,
        SouthEast,
        East,
        NorthEast,
        North,
        NorthWest,
        West,
        SouthWest
    }
    
    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Default to south
        currentFrames = southFrames;
        currentIdleSprite = southIdle;
        currentDirection = Direction.South;
    }
    
    private void Update()
    {
        // Only animate when moving
        if (!isMoving) return;
        
        // Check if we have frames to animate
        if (currentFrames == null || currentFrames.Length == 0) return;
        
        // Update timer
        frameTimer += Time.deltaTime;
        
        // Time for next frame?
        float frameInterval = 1f / frameRate;
        
        if (frameTimer >= frameInterval)
        {
            frameTimer -= frameInterval;
            
            // Next frame
            currentFrame = (currentFrame + 1) % currentFrames.Length;
            
            // Apply sprite
            if (spriteRenderer != null && currentFrames[currentFrame] != null)
            {
                spriteRenderer.sprite = currentFrames[currentFrame];
                
                if (logAnimationChanges)
                {
                    Debug.Log($"[Animator] Frame {currentFrame}: {currentFrames[currentFrame].name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Set movement direction. Call this from your movement script.
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            StopMoving();
            return;
        }
        
        // Only update direction if we're moving with enough magnitude
        // This prevents direction flickering when releasing diagonal keys
        if (direction.sqrMagnitude < 0.5f && isMoving)
        {
            // Low magnitude while moving - don't change direction, just stop
            StopMoving();
            return;
        }
        
        // Start moving
        if (!isMoving)
        {
            isMoving = true;
            frameTimer = 0f;
            currentFrame = 0;
        }
        
        // Determine which direction
        Direction dir = GetDirectionFromVector(direction);
        SetDirectionData(dir);
    }
    
    /// <summary>
    /// Stop movement animation and show idle sprite for current direction.
    /// </summary>
    public void StopMoving()
    {
        if (!isMoving) return;
        
        isMoving = false;
        frameTimer = 0f;
        currentFrame = 0;
        
        // Show idle sprite for current direction (direction is preserved!)
        if (spriteRenderer != null && currentIdleSprite != null)
        {
            spriteRenderer.sprite = currentIdleSprite;
        }
    }
    
    private Direction GetDirectionFromVector(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 8 directions, each 45 degrees
        if (angle >= 337.5f || angle < 22.5f) return Direction.East;
        if (angle >= 22.5f && angle < 67.5f) return Direction.NorthEast;
        if (angle >= 67.5f && angle < 112.5f) return Direction.North;
        if (angle >= 112.5f && angle < 157.5f) return Direction.NorthWest;
        if (angle >= 157.5f && angle < 202.5f) return Direction.West;
        if (angle >= 202.5f && angle < 247.5f) return Direction.SouthWest;
        if (angle >= 247.5f && angle < 292.5f) return Direction.South;
        if (angle >= 292.5f && angle < 337.5f) return Direction.SouthEast;
        
        return Direction.South;
    }
    
    private void SetDirectionData(Direction dir)
    {
        if (dir == currentDirection) return; // No change
        
        currentDirection = dir;
        
        // Set frames and idle sprite for this direction
        switch (dir)
        {
            case Direction.South:
                currentFrames = southFrames;
                currentIdleSprite = southIdle;
                break;
            case Direction.SouthEast:
                currentFrames = southEastFrames;
                currentIdleSprite = southEastIdle;
                break;
            case Direction.East:
                currentFrames = eastFrames;
                currentIdleSprite = eastIdle;
                break;
            case Direction.NorthEast:
                currentFrames = northEastFrames;
                currentIdleSprite = northEastIdle;
                break;
            case Direction.North:
                currentFrames = northFrames;
                currentIdleSprite = northIdle;
                break;
            case Direction.NorthWest:
                currentFrames = northWestFrames;
                currentIdleSprite = northWestIdle;
                break;
            case Direction.West:
                currentFrames = westFrames;
                currentIdleSprite = westIdle;
                break;
            case Direction.SouthWest:
                currentFrames = southWestFrames;
                currentIdleSprite = southWestIdle;
                break;
        }
        
        // Reset animation
        currentFrame = 0;
        frameTimer = 0f;
        
        // Immediately show first frame
        if (isMoving && spriteRenderer != null && currentFrames != null && currentFrames.Length > 0)
        {
            spriteRenderer.sprite = currentFrames[0];
        }
        
        if (logAnimationChanges)
        {
            Debug.Log($"[Animator] Direction: {dir}");
        }
    }
    
    public bool IsAnimating() => isMoving;
    public Direction GetCurrentDirection() => currentDirection;
    
    /// <summary>
    /// Check if current direction is diagonal (NE, NW, SE, SW).
    /// </summary>
    public bool IsDiagonalDirection()
    {
        return currentDirection == Direction.NorthEast ||
               currentDirection == Direction.NorthWest ||
               currentDirection == Direction.SouthEast ||
               currentDirection == Direction.SouthWest;
    }
}
