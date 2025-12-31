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
            Debug.LogWarning("[KhanArmySetup] Already setup! Skipping duplicate call.");
            return;
        }
        hasSetup = true;
        SetupKhan();
    }

    private void SetupKhan()
    {
        Debug.Log("[KhanArmySetup] SetupKhan called");
        
        // Find the Khan (Horse)
        Horse khan = FindFirstObjectByType<Horse>();
        if (khan == null)
        {
            Debug.LogWarning("[KhanArmySetup] Horse not found!");
            return;
        }
        
        // Check if already has General
        khanGeneral = khan.GetComponent<General>();
        if (khanGeneral != null)
        {
            Debug.LogWarning("[KhanArmySetup] Horse already has General component!");
            if (khanGeneral.HasArmy)
            {
                Debug.LogWarning("[KhanArmySetup] Khan already has army, skipping spawn");
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
        
        Debug.Log("✓ Khan set up as General");
        
        // Spawn initial army
        SpawnInitialArmy();
    }
    
    private void SpawnInitialArmy()
    {
        Debug.Log("[KhanArmySetup] SpawnInitialArmy called");
        
        if (khanGeneral == null) 
        {
            Debug.LogError("[KhanArmySetup] khanGeneral is null!");
            return;
        }
        
        ArmyFactory factory = ArmyFactory.Instance;
        if (factory == null)
        {
            factory = FindFirstObjectByType<ArmyFactory>();
        }
        
        if (factory == null)
        {
            Debug.LogError("[KhanArmySetup] ArmyFactory not found!");
            return;
        }
        
        // Create army data
        ArmyData armyData = new ArmyData(startingArmySize, startingArmyQuality, true);
        armyData.armyName = "Khan's Horde";
        
        // Create army
        Army army = factory.CreateArmyForGeneral(khanGeneral, armyData);
        
        if (army != null)
        {
            Debug.Log($"✓ Spawned Khan's army (Size: {startingArmySize}) - Object: {army.gameObject.name}");
            GameEvents.ArmySpawned(army, khanGeneral);
        }
        else
        {
            Debug.LogError("[KhanArmySetup] Failed to spawn army!");
        }
    }
    

    [ContextMenu("Setup Khan")]
    public void ManualSetup()
    {
        SetupKhan();
    }
}