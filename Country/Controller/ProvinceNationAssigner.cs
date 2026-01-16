using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class ProvinceAssignment
{
    public int provinceId;
    public int nationId;
    public string provinceName;
}

[System.Serializable]
public class ProvinceAssignmentWrapper
{
    public ProvinceAssignment[] assignments;
}

public class ProvinceNationAssigner : MonoBehaviour
{
    public static ProvinceNationAssigner Instance { get; private set; }
    
    [Header("References")]
    public NationLoader nationLoader;

    [Header("Settings")]
    public string assignmentsFileName = "province_assignments.json";

    private Dictionary<int, ProvinceModel> provincesById = new Dictionary<int, ProvinceModel>();
    private bool nationsReady = false;
    private bool mapReady = false;
    private bool assignmentComplete = false;  // Guard against duplicate execution

    void Awake()
    {
        // Singleton pattern - destroy duplicate instances
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ProvinceNationAssigner] Duplicate instance destroyed!");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnNationsLoaded += OnNationsLoaded;
        GameEvents.OnMapLoaded += OnMapLoaded;
    
        // Check if events already fired
        NationLoader loader = FindFirstObjectByType<NationLoader>();
        if (loader != null && loader.allNations.Count > 0)
        {
            OnNationsLoaded();
        }
    }

    void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnNationsLoaded -= OnNationsLoaded;
        GameEvents.OnMapLoaded -= OnMapLoaded;
    }

    void Start()
    {
        // Find references if not assigned
        if (nationLoader == null)
            nationLoader = FindFirstObjectByType<NationLoader>();
    }

    private void OnNationsLoaded()
    {
        nationsReady = true;
        TryAssignProvinces();
    }

    private void OnMapLoaded()
    {
        mapReady = true;
        TryAssignProvinces();
    }

    private void TryAssignProvinces()
    {
        // Only proceed when both nations and map are ready
        Debug.Log($"TryAssignProvinces: nationsReady={nationsReady}, mapReady={mapReady}, assignmentComplete={assignmentComplete}");
        if (!nationsReady || !mapReady) return;
        if (assignmentComplete) return;  // NEW: Prevent duplicate execution
        
        assignmentComplete = true;  // NEW: Mark as complete before running
        AssignProvinces();
    }

    void AssignProvinces()
    {
        // Collect all provinces
        CollectProvinces();

        // Load assignments from JSON
        LoadAssignments();

        Debug.Log("✓ Province-Nation assignment complete!");
        
        // Fire event - provinces are assigned
        GameEvents.ProvincesAssigned();
    }

    void CollectProvinces()
    {
        // Find all provinces in scene (from prefab)
        ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        
        foreach (ProvinceModel province in allProvinces)
        {
            provincesById[(int)province.provinceId] = province;
        }

        Debug.Log($"✓ Collected {provincesById.Count} provinces from scene");
    }

    void LoadAssignments()
    {
        string path = Path.Combine(Application.streamingAssetsPath, assignmentsFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Province assignments file not found: {path}");
            Debug.LogWarning("Creating template file...");
            CreateTemplateAssignments();
            return;
        }

        string json = File.ReadAllText(path);
        ProvinceAssignmentWrapper wrapper = JsonUtility.FromJson<ProvinceAssignmentWrapper>(json);

        if (wrapper == null || wrapper.assignments == null)
        {
            Debug.LogError("Failed to parse province assignments!");
            return;
        }

        int assignedCount = 0;
        foreach (ProvinceAssignment assignment in wrapper.assignments)
        {
            if (provincesById.ContainsKey(assignment.provinceId))
            {
                ProvinceModel province = provincesById[assignment.provinceId];
                NationModel nation = nationLoader.GetNationById(assignment.nationId);

                if (province.CompareTag("River"))
                {
                    // Skip rivers
                }
                else if (nation != null)
                {
                    // Assign nation to province
                    province.provinceOwner = nation;
                    
                    // FIXED: Only use JSON name if prefab name is default/empty
                    // Keep the prefab's custom name if it was set manually
                    if (string.IsNullOrEmpty(province.provinceName) || 
                        province.provinceName.StartsWith("Province_") ||
                        province.provinceName == province.gameObject.name)
                    {
                        // Use JSON name only if province has default naming
                        if (!string.IsNullOrEmpty(assignment.provinceName) && 
                            !assignment.provinceName.StartsWith("Province_"))
                        {
                            province.provinceName = assignment.provinceName;
                        }
                    }
                    // Otherwise keep the prefab's provinceName as-is

                    // Add province to nation's list
                    if (!nation.provinceList.Contains(province))
                    {
                        nation.provinceList.Add(province);
                    }

                    // Color the province with nation color
                    Color nationColor = NationLoader.HexToColor(nation.nationColor);
                    province.SetNationColor(nationColor);

                    assignedCount++;
                }
            }
        }

        Debug.Log($"✓ Assigned {assignedCount} provinces to nations");
    }

    void CreateTemplateAssignments()
    {
        // Create a template file with all provinces unassigned (nation 0)
        List<ProvinceAssignment> assignments = new List<ProvinceAssignment>();

        foreach (var kvp in provincesById)
        {
            assignments.Add(new ProvinceAssignment
            {
                provinceId = kvp.Key,
                nationId = 0, // Unassigned
                provinceName = kvp.Value.provinceName
            });
        }

        ProvinceAssignmentWrapper wrapper = new ProvinceAssignmentWrapper
        {
            assignments = assignments.ToArray()
        };

        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.streamingAssetsPath, assignmentsFileName);

        File.WriteAllText(path, json);
        Debug.Log($"✓ Template assignments file created: {path}");
    }

    // Editor helper to assign multiple provinces at once
    public void AssignProvincesToNation(int[] provinceIds, int nationId)
    {
        NationModel nation = nationLoader.GetNationById(nationId);
        if (nation == null)
        {
            Debug.LogError($"Nation {nationId} not found!");
            return;
        }

        Color nationColor = NationLoader.HexToColor(nation.nationColor);

        foreach (int provinceId in provinceIds)
        {
            if (provincesById.ContainsKey(provinceId))
            {
                ProvinceModel province = provincesById[provinceId];
                
                NationModel oldOwner = province.provinceOwner;
                
                province.provinceOwner = nation;
                nation.provinceList.Add(province);
                province.SetNationColor(nationColor);
                
                // Fire event for province ownership change
                GameEvents.ProvinceOwnerChanged(province, oldOwner, nation);
            }
        }

        Debug.Log($"✓ Assigned {provinceIds.Length} provinces to {nation.nationName}");
    }
}