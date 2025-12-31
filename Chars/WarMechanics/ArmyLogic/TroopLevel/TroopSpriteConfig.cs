using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject to hold all troop sprite configurations.
/// Create via: Assets > Create > Khan's Invasion > Troop Sprites Config
/// </summary>
[CreateAssetMenu(fileName = "TroopSpritesConfig", menuName = "Khan's Invasion/Troop Sprites Config")]
public class TroopSpritesConfig : ScriptableObject
{
    [Header("Regular Troop Sprites")]
    [Tooltip("6 color variants: Red, White, Tan, Green, Orange, Blue")]
    public List<Sprite> regularSprites = new List<Sprite>(6);
    
    [Header("Golden/Elite Troop Sprites")]
    [Tooltip("6 color variants for max level troops")]
    public List<Sprite> goldenSprites = new List<Sprite>(6);
    
    [Header("Progress Bar Sprites - Level 1 (0 dots)")]
    [Tooltip("6 color variants")]
    public List<Sprite> progressLevel1 = new List<Sprite>(6);
    
    [Header("Progress Bar Sprites - Level 2 (1 dot)")]
    [Tooltip("6 color variants")]
    public List<Sprite> progressLevel2 = new List<Sprite>(6);
    
    [Header("Progress Bar Sprites - Level 3 (2 dots)")]
    [Tooltip("6 color variants")]
    public List<Sprite> progressLevel3 = new List<Sprite>(6);
    
    [Header("Progress Bar Sprites - Level 4 (3 dots)")]
    [Tooltip("6 color variants")]
    public List<Sprite> progressLevel4 = new List<Sprite>(6);
    
    /// <summary>
    /// Get regular sprite for color variant.
    /// </summary>
    public Sprite GetRegularSprite(TroopColorVariant color)
    {
        int index = (int)color;
        if (index >= 0 && index < regularSprites.Count)
            return regularSprites[index];
        return null;
    }
    
    /// <summary>
    /// Get golden sprite for color variant.
    /// </summary>
    public Sprite GetGoldenSprite(TroopColorVariant color)
    {
        int index = (int)color;
        if (index >= 0 && index < goldenSprites.Count)
            return goldenSprites[index];
        return null;
    }
    
    /// <summary>
    /// Get progress bar sprite for specific level and color.
    /// </summary>
    public Sprite GetProgressSprite(int level, TroopColorVariant color)
    {
        int colorIndex = (int)color;
        List<Sprite> sprites = GetProgressListForLevel(level);
        
        if (sprites != null && colorIndex >= 0 && colorIndex < sprites.Count)
            return sprites[colorIndex];
        return null;
    }
    
    private List<Sprite> GetProgressListForLevel(int level)
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
    
    /// <summary>
    /// Validate that all required sprites are assigned.
    /// </summary>
    public bool Validate(out string errorMessage)
    {
        errorMessage = "";
        
        if (regularSprites.Count < 6)
        {
            errorMessage = "Regular sprites needs 6 color variants";
            return false;
        }
        
        if (goldenSprites.Count < 6)
        {
            errorMessage = "Golden sprites needs 6 color variants";
            return false;
        }
        
        if (progressLevel1.Count < 6 || progressLevel2.Count < 6 || 
            progressLevel3.Count < 6 || progressLevel4.Count < 6)
        {
            errorMessage = "Each progress level needs 6 color variants";
            return false;
        }
        
        return true;
    }
}