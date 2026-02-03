using UnityEngine;


public class Army : MonoBehaviour
{
    [SerializeField] private ArmyData data = new ArmyData();
    
    // The general commanding this army (set by General class)
    private General commandingGeneral;
    
    // Properties
    public ArmyData Data => data;
    public float ArmySize => data.size;
    public float ArmyQuality => data.quality;
    public bool IsPlayerArmy => data.isPlayerOwned;
    public General CommandingGeneral => commandingGeneral;
    public bool HasGeneral => commandingGeneral != null;
    

    public void Initialize(float size, float quality, bool isPlayer)
    {
        data.size = size;
        data.quality = quality;
        data.isPlayerOwned = isPlayer;
    }
    

    public void Initialize(ArmyData armyData)
    {
        data = armyData.Clone();
    }
    
    public void SetCommander(General general)
    {
        commandingGeneral = general;
        
        // Notify follower component if present
        ArmyFollower follower = GetComponent<ArmyFollower>();
        if (follower != null)
        {
            follower.SetFollowTarget(general?.transform);
        }
        
        if (general != null)
            Debug.Log($"[Army] {data.armyName} now commanded by {general.GeneralName}");
        else
            Debug.Log($"[Army] {data.armyName} has no commander");
    }
    

    public void AddSoldiers(float count)
    {
        data.size = Mathf.Min(data.size + count, data.maxSize);
        RefreshArmyText();
    }
    
 
    public void RemoveSoldiers(float count)
    {
        data.size = Mathf.Max(data.size - count, 0);
        RefreshArmyText();
        
        if (data.size <= 0)
        {
            OnArmyDestroyed();
        }
    }
    
    /// <summary>
    /// Set army size directly (used for siege casualties).
    /// </summary>
    public void SetArmySize(float newSize)
    {
        float oldSize = data.size;
        data.size = Mathf.Clamp(newSize, 0, data.maxSize);
        RefreshArmyText();
        
        if (data.size <= 0 && oldSize > 0)
        {
            OnArmyDestroyed();
        }
    }
    
    /// <summary>
    /// Refresh the army text display to show current size.
    /// </summary>
    private void RefreshArmyText()
    {
        ArmyText armyText = GetComponentInChildren<ArmyText>();
        if (armyText != null)
        {
            armyText.RefreshDisplay();
        }
    }

    public void GainExperience(float amount)
    {
        data.quality = Mathf.Min(data.quality + amount, 3.0f);
    }

    public float GetEffectiveStrength()
    {
        return data.EffectiveStrength;
    }
    
    private void OnArmyDestroyed()
    {
        Debug.Log($"[Army] {data.armyName} destroyed!");
        
        // Notify general
        if (commandingGeneral != null)
        {
            commandingGeneral.OnArmyLost();
        }
        
        // Fire event
        GameEvents.ArmyDestroyed(this);
        
        // Unregister from manager
        if (ArmyManager.Instance != null)
        {
            ArmyManager.Instance.UnregisterArmy(this);
        }
        
        Destroy(gameObject);
    }
}