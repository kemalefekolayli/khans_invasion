using UnityEngine;


public class General : MonoBehaviour
{
    [SerializeField] private GeneralData data = new GeneralData();
    
    // Currently commanded army
    private Army commandedArmy;
    
    // Properties
    public GeneralData Data => data;
    public string GeneralName => data.generalName;
    public bool IsKhan => data.isKhan;
    public float CommandBonus => data.commandBonus;
    public Army CommandedArmy => commandedArmy;
    public bool HasArmy => commandedArmy != null;
    

    public void Initialize(GeneralData generalData)
    {
        data = generalData;
    }
    

    public void Initialize(string name, bool isKhan = false)
    {
        data = new GeneralData(name, isKhan);
    }
    

    public void AssignArmy(Army army)
    {
        // Release current army first
        if (commandedArmy != null && commandedArmy != army)
        {
            commandedArmy.SetCommander(null);
        }
        
        commandedArmy = army;
        
        if (army != null)
        {
            army.SetCommander(this);
            Debug.Log($"[General] {data.generalName} now commands {army.Data.armyName}");
            GameEvents.ArmyAssigned(army, this);
        }
    }
    public void ReleaseArmy()
    {
        if (commandedArmy != null)
        {
            commandedArmy.SetCommander(null);
            commandedArmy = null;
        }
    }
    

    public void OnArmyLost()
    {
        commandedArmy = null;
        Debug.Log($"[General] {data.generalName} lost their army!");
    }
    

    public float GetTotalStrength()
    {
        if (commandedArmy == null) return 0f;
        return commandedArmy.GetEffectiveStrength() * data.commandBonus;
    }
}