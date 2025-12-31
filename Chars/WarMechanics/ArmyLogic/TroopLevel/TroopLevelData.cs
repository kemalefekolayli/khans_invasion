using UnityEngine;

/// <summary>
/// Enum for troop color variants.
/// Matches sprite sheet indices (0-5).
/// </summary>
public enum TroopColorVariant
{
    Red = 0,
    White = 1,
    Tan = 2,
    Green = 3,
    Orange = 4,
    Blue = 5
}

/// <summary>
/// Pure data container for troop level information.
/// Separate from Army quality - this is its own progression system.
/// </summary>
[System.Serializable]
public class TroopLevelData
{
    public const int MIN_LEVEL = 1;
    public const int MAX_LEVEL = 4;
    
    [Header("Level")]
    [Range(1, 4)]
    public int currentLevel = 1;
    
    [Header("Experience")]
    public float currentXP = 0f;
    public float xpToNextLevel = 100f;
    
    [Header("Appearance")]
    public TroopColorVariant colorVariant = TroopColorVariant.Red;
    
    // Properties
    public bool IsMaxLevel => currentLevel >= MAX_LEVEL;
    public float LevelProgress => currentXP / xpToNextLevel;
    
    public TroopLevelData() { }
    
    public TroopLevelData(TroopColorVariant color, int level = 1)
    {
        this.colorVariant = color;
        this.currentLevel = Mathf.Clamp(level, MIN_LEVEL, MAX_LEVEL);
        this.currentXP = 0f;
        this.xpToNextLevel = CalculateXPRequirement(this.currentLevel);
    }
    
    /// <summary>
    /// Add XP and handle level ups. Returns true if leveled up.
    /// </summary>
    public bool AddXP(float amount)
    {
        if (IsMaxLevel) return false;
        
        currentXP += amount;
        
        if (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = CalculateXPRequirement(currentLevel);
            
            Debug.Log($"[TroopLevel] LEVEL UP! Now level {currentLevel}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Force set level (for testing or special events).
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, MIN_LEVEL, MAX_LEVEL);
        currentXP = 0f;
        xpToNextLevel = CalculateXPRequirement(currentLevel);
    }
    
    /// <summary>
    /// Calculate XP needed for next level.
    /// Can be adjusted for game balance.
    /// </summary>
    private float CalculateXPRequirement(int level)
    {
        // Level 1 -> 2: 100 XP
        // Level 2 -> 3: 200 XP
        // Level 3 -> 4: 300 XP
        return level * 100f;
    }
    
    public TroopLevelData Clone()
    {
        return new TroopLevelData
        {
            currentLevel = this.currentLevel,
            currentXP = this.currentXP,
            xpToNextLevel = this.xpToNextLevel,
            colorVariant = this.colorVariant
        };
    }
}