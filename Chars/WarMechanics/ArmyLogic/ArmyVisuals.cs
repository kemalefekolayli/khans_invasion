using UnityEngine;

/// <summary>
/// Handles army visual display.
/// On Start, picks a random TroopColorVariant and syncs with TroopLevel.
/// TroopLevelVisuals then uses that color to show correct sprites.
/// </summary>
[RequireComponent(typeof(Army))]
public class ArmyVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Color Settings")]
    [SerializeField] private bool randomizeColorOnStart = true;
    [SerializeField] private TroopColorVariant defaultColor = TroopColorVariant.Red;
    
    private TroopLevel troopLevel;
    private Vector3 lastPosition;
    
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        troopLevel = GetComponent<TroopLevel>();
        lastPosition = transform.position;
    }
    
    private void Start()
    {
        if (randomizeColorOnStart)
        {
            RandomizeColor();
        }
        else
        {
            SetColor(defaultColor);
        }
    }
    
    private void LateUpdate()
    {
        UpdateFacing();
        lastPosition = transform.position;
    }
    
    private void UpdateFacing()
    {
        if (spriteRenderer == null) return;
        
        Vector3 delta = transform.position - lastPosition;
        
        if (delta.x > 0.001f)
            spriteRenderer.flipX = false;
        else if (delta.x < -0.001f)
            spriteRenderer.flipX = true;
    }
    
    /// <summary>
    /// Pick a random color and apply to TroopLevel.
    /// TroopLevelVisuals will then show the matching sprites.
    /// </summary>
    public void RandomizeColor()
    {
        int colorCount = System.Enum.GetValues(typeof(TroopColorVariant)).Length;
        TroopColorVariant randomColor = (TroopColorVariant)Random.Range(0, colorCount);
        
        SetColor(randomColor);
        Debug.Log($"[ArmyVisuals] Randomized color to {randomColor}");
    }
    
    /// <summary>
    /// Set specific color variant.
    /// </summary>
    public void SetColor(TroopColorVariant color)
    {
        if (troopLevel != null)
        {
            troopLevel.SetColorVariant(color);
        }
        else
        {
            Debug.LogWarning("[ArmyVisuals] No TroopLevel component - color not synced");
        }
    }
    
    /// <summary>
    /// Get current color from TroopLevel.
    /// </summary>
    public TroopColorVariant GetColor()
    {
        if (troopLevel != null)
            return troopLevel.ColorVariant;
        return defaultColor;
    }
    
    /// <summary>
    /// Face a specific direction.
    /// </summary>
    public void FaceDirection(bool faceRight)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = !faceRight;
    }
}