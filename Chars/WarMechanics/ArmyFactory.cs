using UnityEngine;


public class ArmyFactory : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject armyPrefab;
    
    [Header("Default Values")]
    [SerializeField] private float defaultSize = 100f;
    [SerializeField] private float defaultQuality = 1.0f;
    
    public static ArmyFactory Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public Army CreateArmy(Vector3 position, ArmyData data)
    {
        if (armyPrefab == null)
        {
            Debug.LogError("[ArmyFactory] Army prefab not assigned!");
            return null;
        }
        
        GameObject armyObj = Instantiate(armyPrefab, position, Quaternion.identity);
        Army army = armyObj.GetComponent<Army>();
        
        if (army == null)
        {
            Debug.LogError("[ArmyFactory] Prefab missing Army component!");
            Destroy(armyObj);
            return null;
        }
        
        army.Initialize(data);
        
        return army;
    }
    

    public Army CreateArmy(Vector3 position, float size, float quality, bool isPlayer)
    {
        ArmyData data = new ArmyData(size, quality, isPlayer);
        return CreateArmy(position, data);
    }
    

    public Army CreateArmyForGeneral(General general, ArmyData data)
    {
        if (general == null)
        {
            Debug.LogError("[ArmyFactory] Cannot create army - no general!");
            return null;
        }
        
        // Spawn behind the general
        Vector3 spawnPos = general.transform.position + new Vector3(-0.3f, -0.2f, 0);
        
        Army army = CreateArmy(spawnPos, data);
        
        if (army != null)
        {
            general.AssignArmy(army);
            
            // Snap follower to position
            ArmyFollower follower = army.GetComponent<ArmyFollower>();
            if (follower != null)
            {
                follower.SnapToTarget();
            }
        }
        
        return army;
    }
    

    public Army CreateArmyForGeneral(General general, float size, float quality, bool isPlayer)
    {
        ArmyData data = new ArmyData(size, quality, isPlayer);
        return CreateArmyForGeneral(general, data);
    }
    

    public Army CreateDefaultArmyForGeneral(General general, bool isPlayer)
    {
        return CreateArmyForGeneral(general, defaultSize, defaultQuality, isPlayer);
    }
    
    // Setters for prefab (useful for runtime assignment)
    public void SetArmyPrefab(GameObject prefab)
    {
        armyPrefab = prefab;
    }
}