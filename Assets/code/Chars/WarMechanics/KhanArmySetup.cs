using UnityEngine;


public class KhanArmySetup : MonoBehaviour
{
    [Header("Initial Army Settings")]
    [SerializeField] private float startingArmySize = 100f;
    [SerializeField] private float startingArmyQuality = 1.0f;
    
    [Header("Khan Stats")]
    [SerializeField] private float khanCommandBonus = 1.5f;
    
    // Reference to Khan's general component
    private General khanGeneral;
    
    public General KhanGeneral => khanGeneral;
    
    private bool hasSetup = false;
    
    private void OnEnable()
    {
        GameEvents.OnPlayerNationReady += OnPlayerReady;
    }
    
    private void OnDisable()
    {
        GameEvents.OnPlayerNationReady -= OnPlayerReady;
    }
    
    private void OnPlayerReady()
    {
        if (hasSetup)
        {
            GameLog.Warning(GameLogCategory.Core, "[KhanArmySetup] Already setup! Skipping duplicate call.");
            return;
        }
        hasSetup = true;
        SetupKhan();
    }

    private void SetupKhan()
    {
        GameLog.Log(GameLogCategory.Core, "[KhanArmySetup] SetupKhan called");
        
        // Find the Khan
        SelectableGeneral khan = SelectableGeneral.FindKhan();
        if (khan == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[KhanArmySetup] Khan (SelectableGeneral with isKhan) not found!");
            return;
        }

        // Check if already has General
        khanGeneral = khan.GetComponent<General>();
        if (khanGeneral != null)
        {
            GameLog.Warning(GameLogCategory.Core, "[KhanArmySetup] Khan already has General component!");
            if (khanGeneral.HasArmy)
            {
                GameLog.Warning(GameLogCategory.Core, "[KhanArmySetup] Khan already has army, skipping spawn");
                return;
            }
        }
        else
        {
            khanGeneral = khan.gameObject.AddComponent<General>();
        }
        
        // Initialize as Khan
        GeneralData khanData = new GeneralData("Khan", true);
        khanData.commandBonus = khanCommandBonus;
        khanGeneral.Initialize(khanData);
        

        
        // Spawn initial army
        SpawnInitialArmy();
    }
    
    private void SpawnInitialArmy()
    {
        GameLog.Log(GameLogCategory.Core, "[KhanArmySetup] SpawnInitialArmy called");
        
        if (khanGeneral == null) 
        {
            GameLog.Error(GameLogCategory.Core, "[KhanArmySetup] khanGeneral is null!");
            return;
        }
        
        ArmyFactory factory = ArmyFactory.Instance;
        if (factory == null)
        {
            factory = FindFirstObjectByType<ArmyFactory>();
        }
        
        if (factory == null)
        {
            GameLog.Error(GameLogCategory.Core, "[KhanArmySetup] ArmyFactory not found!");
            return;
        }
        
        // Create army data
        ArmyData armyData = new ArmyData(startingArmySize, startingArmyQuality, true);
        armyData.armyName = "Khan's Horde";
        
        // Create army
        Army army = factory.CreateArmyForGeneral(khanGeneral, armyData);
        
        if (army != null)
        {
            army.OwnerNation = PlayerNation.Instance?.currentNation;
            GameLog.Log(GameLogCategory.Core, $"✓ Spawned Khan's army (Size: {startingArmySize}) - Object: {army.gameObject.name}");
            GameEvents.ArmySpawned(army, khanGeneral);
        }
        else
        {
            GameLog.Error(GameLogCategory.Core, "[KhanArmySetup] Failed to spawn army!");
        }
    }
    

    [ContextMenu("Setup Khan")]
    public void ManualSetup()
    {
        SetupKhan();
    }
}