using UnityEngine;

public class GeneralSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject generalPrefab;
    [SerializeField] private int extraGeneralCount = 1;
    [SerializeField] private Vector3 spawnOffset = new Vector3(-3f, 0, 0);
    
    [Header("Army Settings")]
    [SerializeField] private float startingArmySize = 50f;
    [SerializeField] private float startingArmyQuality = 0.8f;
    
    private static readonly string[] GENERAL_NAMES = {
        "Subotai", "Jebe", "Kublai", "Batu", "Berke",
        "Chagatai", "Tolui", "Ögedei", "Möngke", "Börte"
    };
    
    private bool hasSpawned = false;
    
    private void OnEnable()
    {
        GameEvents.OnPlayerNationReady += OnPlayerNationReady;
    }
    
    private void OnDisable()
    {
        GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
    }
    
    private void OnPlayerNationReady()
    {
        if (hasSpawned) return;
        hasSpawned = true;
        
        Invoke(nameof(SpawnExtraGenerals), 0.2f);
    }
    
    private void SpawnExtraGenerals()
    {
        if (generalPrefab == null)
        {
            Debug.LogError("[GeneralSpawner] General prefab not assigned!");
            return;
        }
        
        Horse khan = FindFirstObjectByType<Horse>();
        if (khan == null)
        {
            Debug.LogError("[GeneralSpawner] Khan (Horse) not found!");
            return;
        }
        
        Vector3 khanPosition = khan.transform.position;
        
        for (int i = 0; i < extraGeneralCount; i++)
        {
            Vector3 spawnPos = khanPosition + spawnOffset * (i + 1);
            string generalName = GetRandomName();
            
            SpawnGeneral(spawnPos, generalName);
        }
    }
    
    private void SpawnGeneral(Vector3 position, string generalName)
    {
        GameObject generalObj = Instantiate(generalPrefab, position, Quaternion.identity);
        generalObj.name = $"General_{generalName}";
        
        SelectableGeneral selectable = generalObj.GetComponent<SelectableGeneral>();
        if (selectable != null)
        {
            selectable.SetDisplayName(generalName);
            selectable.SetIsKhan(false); // Ensure spawned generals are NOT the Khan
        }
        
        General general = generalObj.GetComponent<General>();
        if (general == null)
        {
            general = generalObj.AddComponent<General>();
        }
        general.Initialize(generalName, false);
        
        SpawnArmyForGeneral(general, generalName);
        
        Debug.Log($"✓ [GeneralSpawner] Spawned general: {generalName} at {position}");
    }
    
    private void SpawnArmyForGeneral(General general, string generalName)
    {
        ArmyFactory factory = ArmyFactory.Instance;
        if (factory == null)
        {
            factory = FindFirstObjectByType<ArmyFactory>();
        }
        
        if (factory == null)
        {
            Debug.LogError("[GeneralSpawner] ArmyFactory not found!");
            return;
        }
        
        ArmyData armyData = new ArmyData(startingArmySize, startingArmyQuality, true);
        armyData.armyName = $"{generalName}'s Army";
        
        Army army = factory.CreateArmyForGeneral(general, armyData);
        
        if (army != null)
        {
            Debug.Log($"✓ [GeneralSpawner] Spawned army for {generalName} (Size: {startingArmySize})");
            GameEvents.ArmySpawned(army, general);
        }
    }
    
    private string GetRandomName()
    {
        int index = Random.Range(0, GENERAL_NAMES.Length);
        return GENERAL_NAMES[index];
    }
}
