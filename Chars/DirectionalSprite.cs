using UnityEngine;

/// <summary>
/// Handles 8-directional sprite rotation based on movement direction.
/// Swaps sprites instead of rotating the transform for pixel-perfect 2D.
/// 
/// SETUP:
/// 1. Import all 8 directional sprites to Unity
/// 2. Assign them in the Inspector (North, NorthEast, East, etc.)
/// 3. Add this component to your character
/// 4. This will automatically update sprites based on movement
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSprite : MonoBehaviour
{
    [Header("Directional Sprites (8 directions)")]
    [Tooltip("Facing up")]
    [SerializeField] private Sprite spriteNorth;
    
    [Tooltip("Facing up-right")]
    [SerializeField] private Sprite spriteNorthEast;
    
    [Tooltip("Facing right")]
    [SerializeField] private Sprite spriteEast;
    
    [Tooltip("Facing down-right")]
    [SerializeField] private Sprite spriteSouthEast;
    
    [Tooltip("Facing down")]
    [SerializeField] private Sprite spriteSouth;
    
    [Tooltip("Facing down-left")]
    [SerializeField] private Sprite spriteSouthWest;
    
    [Tooltip("Facing left")]
    [SerializeField] private Sprite spriteWest;
    
    [Tooltip("Facing up-left")]
    [SerializeField] private Sprite spriteNorthWest;
    
    [Header("Settings")]
    [Tooltip("Default direction when not moving (0-7, starting from East going counter-clockwise)")]
    [SerializeField] private Direction defaultDirection = Direction.South;
    
    [Tooltip("Minimum velocity to trigger direction change")]
    [SerializeField] private float minVelocity = 0.01f;
    
    [Header("Optional - Auto-detect Movement")]
    [Tooltip("If assigned, will automatically read velocity from Rigidbody2D")]
    [SerializeField] private Rigidbody2D rb;
    
    private SpriteRenderer spriteRenderer;
    private Direction currentDirection;
    private Sprite[] directionSprites;
    
    // Direction enum for clarity (clockwise from East)
    public enum Direction
    {
        East = 0,       // Right
        NorthEast = 1,  // Up-Right
        North = 2,      // Up
        NorthWest = 3,  // Up-Left
        West = 4,       // Left
        SouthWest = 5,  // Down-Left
        South = 6,      // Down
        SouthEast = 7   // Down-Right
    }
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Build sprite array for quick lookup
        directionSprites = new Sprite[8]
        {
            spriteEast,
            spriteNorthEast,
            spriteNorth,
            spriteNorthWest,
            spriteWest,
            spriteSouthWest,
            spriteSouth,
            spriteSouthEast
        };
        
        // Set default direction
        currentDirection = defaultDirection;
        UpdateSprite();
    }
    
    private void Update()
    {
        // Auto-detect from Rigidbody if assigned
        if (rb != null && rb.linearVelocity.sqrMagnitude > minVelocity * minVelocity)
        {
            SetDirectionFromVector(rb.linearVelocity);
        }
    }
    
    /// <summary>
    /// Set the direction based on a movement vector.
    /// Call this from your movement script with the current move direction.
    /// </summary>
    public void SetDirectionFromVector(Vector2 direction)
    {
        if (direction.sqrMagnitude < minVelocity * minVelocity)
            return;
        
        Direction newDirection = VectorToDirection(direction);
        
        if (newDirection != currentDirection)
        {
            currentDirection = newDirection;
            UpdateSprite();
        }
    }
    
    /// <summary>
    /// Set direction directly using the Direction enum.
    /// </summary>
    public void SetDirection(Direction direction)
    {
        if (direction != currentDirection)
        {
            currentDirection = direction;
            UpdateSprite();
        }
    }
    
    /// <summary>
    /// Get the current facing direction.
    /// </summary>
    public Direction GetCurrentDirection()
    {
        return currentDirection;
    }
    
    /// <summary>
    /// Convert a Vector2 direction to one of 8 cardinal/ordinal directions.
    /// </summary>
    private Direction VectorToDirection(Vector2 direction)
    {
        // Normalize for consistent angle calculation
        direction = direction.normalized;
        
        // Get angle in degrees (0 = right, 90 = up, etc.)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Convert to 0-360 range
        if (angle < 0) angle += 360f;
        
        // Each direction covers 45 degrees
        // East is 0°, NorthEast is 45°, etc.
        // Offset by 22.5° so each direction has 22.5° on each side
        int segment = Mathf.RoundToInt(angle / 45f) % 8;
        
        return (Direction)segment;
    }
    
    /// <summary>
    /// Update the sprite based on current direction.
    /// </summary>
    private void UpdateSprite()
    {
        int index = (int)currentDirection;
        
        if (index >= 0 && index < directionSprites.Length)
        {
            Sprite sprite = directionSprites[index];
            
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                // Ensure the sprite isn't flipped by other scripts (like Horse.cs)
                spriteRenderer.flipX = false;
            }
            else
            {
                Debug.LogWarning($"[DirectionalSprite] No sprite assigned for direction: {currentDirection}");
            }
        }
    }
    
    #region Debug
    
    [ContextMenu("Test - Face North")]
    private void TestNorth() => SetDirection(Direction.North);
    
    [ContextMenu("Test - Face South")]
    private void TestSouth() => SetDirection(Direction.South);
    
    [ContextMenu("Test - Face East")]
    private void TestEast() => SetDirection(Direction.East);
    
    [ContextMenu("Test - Face West")]
    private void TestWest() => SetDirection(Direction.West);
    
    [ContextMenu("Cycle All Directions")]
    private void CycleDirections()
    {
        StartCoroutine(CycleDirectionsCoroutine());
    }
    
    private System.Collections.IEnumerator CycleDirectionsCoroutine()
    {
        for (int i = 0; i < 8; i++)
        {
            SetDirection((Direction)i);
            Debug.Log($"Direction: {(Direction)i}");
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    #endregion
}
