using UnityEngine;


[System.Serializable]
public class GeneralData
{
    [Header("Identity")]
    public string generalName = "General";
    public bool isKhan = false;
    
    [Header("Stats")]
    public float commandBonus = 1.0f;    // Army effectiveness multiplier
    public float movementRange = 5f;      // Movement per turn
    
    public GeneralData() { }
    
    public GeneralData(string name, bool isKhan = false)
    {
        this.generalName = name;
        this.isKhan = isKhan;
        this.commandBonus = isKhan ? 1.5f : 1.0f;
    }
}