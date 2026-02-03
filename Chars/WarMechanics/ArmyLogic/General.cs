using UnityEngine;


public class General : MonoBehaviour
{
    [SerializeField] private GeneralData data = new GeneralData();
    
    [Header("Loot Carrying")]
    [Tooltip("Maximum loot this general can carry")]
    [SerializeField] private float maxLootCapacity = 500f;
    
    [Tooltip("Current loot being carried")]
    [SerializeField] private float carriedLoot = 0f;
    
    // Currently commanded army
    private Army commandedArmy;
    
    // Properties
    public GeneralData Data => data;
    public string GeneralName => data.generalName;
    public bool IsKhan => data.isKhan;
    public float CommandBonus => data.commandBonus;
    public Army CommandedArmy => commandedArmy;
    public bool HasArmy => commandedArmy != null;
    
    // Nation ownership (currently all generals are player-owned)
    public NationModel OwnerNation => PlayerNation.Instance?.currentNation;
    
    // Loot properties
    public float MaxLootCapacity => maxLootCapacity;
    public float CarriedLoot => carriedLoot;
    public float AvailableLootCapacity => maxLootCapacity - carriedLoot;
    public bool CanCarryMoreLoot => carriedLoot < maxLootCapacity;
    

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
    
    #region Loot Management
    
    /// <summary>
    /// Add loot to this general's carried amount.
    /// Returns the amount actually added (may be less if capacity reached).
    /// </summary>
    public float AddLoot(float amount)
    {
        float actualAmount = Mathf.Min(amount, AvailableLootCapacity);
        carriedLoot += actualAmount;
        
        if (actualAmount < amount)
        {
            Debug.LogWarning($"[General] {data.generalName} could only carry {actualAmount:F0} of {amount:F0} loot (capacity full)");
        }
        
        return actualAmount;
    }
    
    /// <summary>
    /// Remove loot from this general (e.g., when depositing to treasury).
    /// Returns the amount actually removed.
    /// </summary>
    public float RemoveLoot(float amount)
    {
        float actualAmount = Mathf.Min(amount, carriedLoot);
        carriedLoot -= actualAmount;
        return actualAmount;
    }
    
    /// <summary>
    /// Deposit all carried loot to the player's treasury.
    /// </summary>
    public void DepositLootToTreasury()
    {
        if (carriedLoot <= 0) return;
        
        if (PlayerNation.Instance != null)
        {
            PlayerNation.Instance.nationMoney += carriedLoot;
            Debug.Log($"[General] {data.generalName} deposited {carriedLoot:F0} loot to treasury");
            carriedLoot = 0f;
            
            GameEvents.PlayerStatsChanged();
        }
    }
    
    /// <summary>
    /// Set the maximum loot capacity (for upgrades).
    /// </summary>
    public void SetMaxLootCapacity(float capacity)
    {
        maxLootCapacity = capacity;
    }
    
    #endregion
}