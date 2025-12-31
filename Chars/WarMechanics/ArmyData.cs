using UnityEngine;

/// <summary>
/// Pure data container for army statistics.
/// No logic, just holds the numbers.
/// </summary>
[System.Serializable]
public class ArmyData
{
    [Header("Identity")]
    public string armyName = "Army";
    public bool isPlayerOwned;
    
    [Header("Combat Stats")]
    public float size = 100f;           // Number of soldiers
    public float quality = 1.0f;        // Experience multiplier (1.0 - 3.0)
    public float maxSize = 1000f;       // Max capacity
    
    [Header("Calculated")]
    public float EffectiveStrength => size * quality;
    
    public ArmyData() { }
    
    public ArmyData(float size, float quality, bool isPlayer)
    {
        this.size = size;
        this.quality = quality;
        this.isPlayerOwned = isPlayer;
    }
    
    public ArmyData Clone()
    {
        return new ArmyData
        {
            armyName = this.armyName,
            isPlayerOwned = this.isPlayerOwned,
            size = this.size,
            quality = this.quality,
            maxSize = this.maxSize
        };
    }
}