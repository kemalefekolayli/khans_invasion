using UnityEngine;

/// <summary>
/// Handles visual representation of troop level.
/// - Updates troop sprite based on level (regular -> golden at max)
/// - Updates progress bar indicator with matching color
/// 
/// Uses TroopLevelData.colorVariant to pick matching sprites.
/// </summary>
public class TroopLevelVisuals : MonoBehaviour
{
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer troopRenderer;
    [SerializeField] private SpriteRenderer progressBarRenderer;
    
    [Header("Progress Bar Position")]
    [SerializeField] private Vector3 progressBarOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float progressBarScale = 0.5f;
    
    [Header("Troop Sprites (6 color variants: Red, White, Tan, Green, Orange, Blue)")]
    [Tooltip("Regular soldier sprites - index 0=Red, 1=White, 2=Tan, 3=Green, 4=Orange, 5=Blue")]
    [SerializeField] private Sprite[] regularSprites = new Sprite[6];
    
    [Header("Golden Sprites (6 color variants for max level)")]
    [Tooltip("Golden/elite sprites - same order as regular")]
    [SerializeField] private Sprite[] goldenSprites = new Sprite[6];
    
    [Header("Progress Bar Sprites - Level 1 (0 dots)")]
    [Tooltip("6 color variants: Red, White, Tan, Green, Orange, Blue")]
    [SerializeField] private Sprite[] progressLevel1 = new Sprite[6];
    
    [Header("Progress Bar Sprites - Level 2 (1 dot)")]
    [SerializeField] private Sprite[] progressLevel2 = new Sprite[6];
    
    [Header("Progress Bar Sprites - Level 3 (2 dots)")]
    [SerializeField] private Sprite[] progressLevel3 = new Sprite[6];
    
    [Header("Progress Bar Sprites - Level 4 (3 dots)")]
    [SerializeField] private Sprite[] progressLevel4 = new Sprite[6];
    
    // Cached reference to progress bar GameObject
    private GameObject progressBarObject;
    
    private void Awake()
    {
        if (troopRenderer == null)
            troopRenderer = GetComponent<SpriteRenderer>();
        
        EnsureProgressBarExists();
    }
    
    private void EnsureProgressBarExists()
    {
        if (progressBarRenderer != null) return;
        
        Transform existing = transform.Find("ProgressBar");
        if (existing != null)
        {
            progressBarObject = existing.gameObject;
            progressBarRenderer = existing.GetComponent<SpriteRenderer>();
            return;
        }
        
        progressBarObject = new GameObject("ProgressBar");
        progressBarObject.transform.SetParent(transform);
        progressBarObject.transform.localPosition = progressBarOffset;
        progressBarObject.transform.localScale = Vector3.one * progressBarScale;
        
        progressBarRenderer = progressBarObject.AddComponent<SpriteRenderer>();
        progressBarRenderer.sortingOrder = 10;
        
        Debug.Log("[TroopLevelVisuals] Created progress bar child object");
    }
    
    /// <summary>
    /// Update all visuals based on current level data.
    /// Called by TroopLevel component when level/XP/color changes.
    /// </summary>
    public void UpdateVisuals(TroopLevelData data)
    {
        if (data == null) return;
        
        UpdateTroopSprite(data);
        UpdateProgressBar(data);
    }
    
    private void UpdateTroopSprite(TroopLevelData data)
    {
        if (troopRenderer == null) return;
        
        int colorIndex = (int)data.colorVariant;
        
        if (data.IsMaxLevel)
        {
            if (IsValidIndex(goldenSprites, colorIndex))
            {
                troopRenderer.sprite = goldenSprites[colorIndex];
                Debug.Log($"[TroopLevelVisuals] Set GOLDEN sprite ({data.colorVariant})");
            }
            else
            {
                Debug.LogWarning($"[TroopLevelVisuals] Golden sprite missing for {data.colorVariant}");
            }
        }
        else
        {
            if (IsValidIndex(regularSprites, colorIndex))
            {
                troopRenderer.sprite = regularSprites[colorIndex];
            }
            else
            {
                Debug.LogWarning($"[TroopLevelVisuals] Regular sprite missing for {data.colorVariant}");
            }
        }
    }
    
    private void UpdateProgressBar(TroopLevelData data)
    {
        if (progressBarRenderer == null)
        {
            EnsureProgressBarExists();
            if (progressBarRenderer == null) return;
        }
        
        int colorIndex = (int)data.colorVariant;
        Sprite[] levelSprites = GetProgressArrayForLevel(data.currentLevel);
        
        if (levelSprites != null && IsValidIndex(levelSprites, colorIndex))
        {
            progressBarRenderer.sprite = levelSprites[colorIndex];
        }
        else
        {
            Debug.LogWarning($"[TroopLevelVisuals] Progress sprite missing for level {data.currentLevel}, color {data.colorVariant}");
        }
    }
    
    private Sprite[] GetProgressArrayForLevel(int level)
    {
        switch (level)
        {
            case 1: return progressLevel1;
            case 2: return progressLevel2;
            case 3: return progressLevel3;
            case 4: return progressLevel4;
            default: return progressLevel1;
        }
    }
    
    private bool IsValidIndex(Sprite[] array, int index)
    {
        return array != null && index >= 0 && index < array.Length && array[index] != null;
    }
    
    public void SetProgressBarVisible(bool visible)
    {
        if (progressBarObject != null)
            progressBarObject.SetActive(visible);
    }
    
    public void SetProgressBarOffset(Vector3 offset)
    {
        progressBarOffset = offset;
        if (progressBarObject != null)
            progressBarObject.transform.localPosition = offset;
    }
    
    [ContextMenu("Log Sprite Status")]
    private void LogSpriteStatus()
    {
        Debug.Log($"Regular sprites: {CountAssigned(regularSprites)}/6");
        Debug.Log($"Golden sprites: {CountAssigned(goldenSprites)}/6");
        Debug.Log($"Progress L1: {CountAssigned(progressLevel1)}/6");
        Debug.Log($"Progress L2: {CountAssigned(progressLevel2)}/6");
        Debug.Log($"Progress L3: {CountAssigned(progressLevel3)}/6");
        Debug.Log($"Progress L4: {CountAssigned(progressLevel4)}/6");
    }
    
    private int CountAssigned(Sprite[] sprites)
    {
        if (sprites == null) return 0;
        int count = 0;
        foreach (var s in sprites)
            if (s != null) count++;
        return count;
    }
}