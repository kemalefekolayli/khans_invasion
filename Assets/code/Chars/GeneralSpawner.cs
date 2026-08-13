using UnityEngine;

public class GeneralSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject generalPrefab;
    [SerializeField] private int extraGeneralCount = 0;
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
            GameLog.Error(GameLogCategory.Core, "[GeneralSpawner] General prefab not assigned!");
            return;
        }
        
        SelectableGeneral khan = SelectableGeneral.FindKhan();
        if (khan == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GeneralSpawner] Khan not found!");
            return;
        }
        
        Vector3 khanPosition = khan.transform.position;
        
        for (int i = 0; i < extraGeneralCount; i++)
        {
            Vector3 spawnPos = khanPosition + spawnOffset * (i + 1);
            string generalName = GetRandomName();
            
            if (!TrySpawnGeneral(spawnPos, generalName, startingArmySize))
                break;
        }
    }
    
    /// <summary>
    /// Spawn a quest-reward general (with an army) near the Khan.
    /// Reuses the standard spawn pipeline. Falls back to the default starting army size.
    /// </summary>
    public void SpawnQuestRewardGeneral(string generalName, int armySize)
    {
        if (generalPrefab == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GeneralSpawner] General prefab not assigned!");
            return;
        }
        
        SelectableGeneral khan = SelectableGeneral.FindKhan();
        if (khan == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GeneralSpawner] Khan not found!");
            return;
        }
        
        Vector3 spawnPos = khan.transform.position + spawnOffset;
        float size = armySize > 0 ? armySize : startingArmySize;
        
        TrySpawnGeneral(spawnPos, generalName, size);
    }

    public bool TrySpawnFreeGeneral()
    {
        if (generalPrefab == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GeneralSpawner] General prefab not assigned!");
            return false;
        }

        SelectableGeneral khan = SelectableGeneral.FindKhan();
        if (khan == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GeneralSpawner] Khan not found!");
            return false;
        }

        return TrySpawnGeneral(khan.transform.position + spawnOffset, GetRandomName(), startingArmySize);
    }
    
    private bool TrySpawnGeneral(Vector3 position, string generalName, float requestedArmySize)
    {
        MilitaryEconomy militaryEconomy = MilitaryEconomy.GetOrCreate();
        if (!militaryEconomy.CanCreateGeneral()) return false;

        float armySize = militaryEconomy.ClampNewSoldiers(requestedArmySize, "General recruitment");
        if (armySize <= 0f) return false;

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
        
        if (!SpawnArmyForGeneral(general, generalName, armySize))
        {
            Destroy(generalObj);
            return false;
        }
        
        GameLog.Log(GameLogCategory.Core, $"✓ [GeneralSpawner] Spawned general: {generalName} at {position}");
        return true;
    }
    
    private bool SpawnArmyForGeneral(General general, string generalName, float armySize)
    {
        ArmyFactory factory = ArmyFactory.Instance;
        if (factory == null)
        {
            factory = FindFirstObjectByType<ArmyFactory>();
        }
        
        if (factory == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GeneralSpawner] ArmyFactory not found!");
            return false;
        }
        
        ArmyData armyData = new ArmyData(armySize, startingArmyQuality, true);
        armyData.armyName = $"{generalName}'s Army";
        
        Army army = factory.CreateArmyForGeneral(general, armyData);
        
        if (army != null)
        {
            army.OwnerNation = PlayerNation.Instance?.currentNation;
            GameLog.Log(GameLogCategory.Core, $"✓ [GeneralSpawner] Spawned army for {generalName} (Size: {armySize})");
            GameEvents.ArmySpawned(army, general);
            return true;
        }

        return false;
    }
    
    private string GetRandomName()
    {
        int index = Random.Range(0, GENERAL_NAMES.Length);
        return GENERAL_NAMES[index];
    }
}
