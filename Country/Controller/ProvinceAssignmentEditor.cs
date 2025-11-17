#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ProvinceAssignmentEditor : EditorWindow
{
    private NationLoader nationLoader;
    private MapGenerator mapGenerator;
    private List<ProvinceModel> allProvinces = new List<ProvinceModel>();
    
    private int selectedNationId = 1;
    private Vector2 scrollPosition;
    private Dictionary<int, List<ProvinceModel>> provincesByNation = new Dictionary<int, List<ProvinceModel>>();
    
    private bool selectMode = false;
    private ProvinceModel hoveredProvince = null;

    [MenuItem("Tools/Province Assignment Editor")]
    public static void ShowWindow()
    {
        GetWindow<ProvinceAssignmentEditor>("Province Assigner");
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        RefreshData();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void RefreshData()
    {
        nationLoader = FindFirstObjectByType<NationLoader>();
        
        // Find all provinces in active scene
        allProvinces.Clear();
        allProvinces.AddRange(FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None));
        
        Debug.Log($"RefreshData found {allProvinces.Count} provinces");
        GroupProvincesByNation();
    }

    void GroupProvincesByNation()
    {
        provincesByNation.Clear();
        foreach (var province in allProvinces)
        {
            int nationId = province.provinceOwner != null ? (int)province.provinceOwner.nationId : 0;
            if (!provincesByNation.ContainsKey(nationId))
                provincesByNation[nationId] = new List<ProvinceModel>();
            provincesByNation[nationId].Add(province);
        }
    }

    void OnGUI()
    {
        // Main scroll view for entire window
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Province Assignment Tool", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Data"))
        {
            RefreshData();
        }

        EditorGUILayout.Space();

        // Select mode toggle
        selectMode = EditorGUILayout.Toggle("Select Mode (Click in Scene)", selectMode);
        
        if (selectMode)
        {
            EditorGUILayout.HelpBox("Click provinces in the Scene view to assign them to the selected nation.", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Nation selector
        if (nationLoader != null && nationLoader.allNations.Count > 0)
        {
            EditorGUILayout.LabelField("Select Nation:");
            
            foreach (var nation in nationLoader.allNations)
            {
                Color nationColor = NationLoader.HexToColor(nation.nationColor);
                GUI.backgroundColor = nationColor;
                
                bool isSelected = selectedNationId == nation.nationId;
                if (GUILayout.Button($"{nation.nationName} (ID: {nation.nationId})", GUILayout.Height(30)))
                {
                    selectedNationId = (int)nation.nationId;
                }
                
                GUI.backgroundColor = Color.white;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("NationLoader not found! Make sure nations are loaded.", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Province Summary:", EditorStyles.boldLabel);
        
        foreach (var kvp in provincesByNation.OrderBy(x => x.Key))
        {
            int nationId = kvp.Key;
            List<ProvinceModel> provinces = kvp.Value;
            
            string nationName = "Unassigned";
            Color nationColor = Color.gray;
            
            if (nationId > 0 && nationLoader != null)
            {
                NationModel nation = nationLoader.GetNationById(nationId);
                if (nation != null)
                {
                    nationName = nation.nationName;
                    nationColor = NationLoader.HexToColor(nation.nationColor);
                }
            }
            
            GUI.backgroundColor = nationColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{nationName}: {provinces.Count} provinces");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Save Assignments to JSON", GUILayout.Height(40)))
        {
            SaveAssignments();
        }
        
        if (GUILayout.Button("Load Assignments from JSON", GUILayout.Height(30)))
        {
            LoadAssignments();
        }
        
        EditorGUILayout.EndScrollView();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!selectMode) return;

        Event e = Event.current;
        
        // Mouse position raycast
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100f);
        
        hoveredProvince = null;
        
        if (hit.collider != null)
        {
            ProvinceModel province = hit.collider.GetComponent<ProvinceModel>();
            if (province != null)
            {
                hoveredProvince = province;
                
                // Highlight
                Handles.color = Color.yellow;
                Handles.DrawWireCube(province.transform.position, Vector3.one * 0.5f);
                
                // Click to assign
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    AssignProvinceToNation(province, selectedNationId);
                    e.Use();
                    Repaint();
                }
            }
        }
        
        sceneView.Repaint();
    }

    void AssignProvinceToNation(ProvinceModel province, int nationId)
    {
        if (nationLoader == null) return;
        
        NationModel nation = nationLoader.GetNationById(nationId);
        if (nation == null)
        {
            Debug.LogError($"Nation {nationId} not found!");
            return;
        }
        
        // Remove from old nation
        if (province.provinceOwner != null)
        {
            province.provinceOwner.provinceList.Remove(province);
        }
        
        // Assign to new nation
        province.provinceOwner = nation;
        
        // Clear and re-add to prevent duplicates
        if (!nation.provinceList.Contains(province))
        {
            nation.provinceList.Add(province);
        }
        
        // Update color
        Color nationColor = NationLoader.HexToColor(nation.nationColor);
        province.SetNationColor(nationColor);
        
        // Mark dirty for prefab changes
        UnityEditor.EditorUtility.SetDirty(province);
        UnityEditor.EditorUtility.SetDirty(province.gameObject);
        
        GroupProvincesByNation();
        
        Debug.Log($"✓ Assigned Province_{province.provinceId} to {nation.nationName} (ID:{nationId})");
    }

    void SaveAssignments()
    {
        // Refresh data first
        RefreshData();
        
        if (allProvinces.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No provinces found! Make sure you're in Play mode.", "OK");
            return;
        }
        
        List<ProvinceAssignment> assignments = new List<ProvinceAssignment>();
        
        foreach (var province in allProvinces)
        {
            int nationId = province.provinceOwner != null ? (int)province.provinceOwner.nationId : 0;
            
            assignments.Add(new ProvinceAssignment
            {
                provinceId = (int)province.provinceId,
                nationId = nationId,
                provinceName = province.provinceName
            });
        }
        
        ProvinceAssignmentWrapper wrapper = new ProvinceAssignmentWrapper
        {
            assignments = assignments.ToArray()
        };
        
        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.streamingAssetsPath, "province_assignments.json");
        
        // Ensure directory exists
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        
        Debug.Log($"✓ Saved {assignments.Count} province assignments to {path}");
        EditorUtility.DisplayDialog("Success", $"Saved {assignments.Count} assignments!", "OK");
    }

    void LoadAssignments()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "province_assignments.json");
        
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Error", "province_assignments.json not found!", "OK");
            return;
        }
        
        string json = File.ReadAllText(path);
        ProvinceAssignmentWrapper wrapper = JsonUtility.FromJson<ProvinceAssignmentWrapper>(json);
        
        if (wrapper == null || wrapper.assignments == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to parse JSON!", "OK");
            return;
        }
        
        Dictionary<int, ProvinceModel> provincesById = allProvinces.ToDictionary(p => (int)p.provinceId);
        
        int loadedCount = 0;
        foreach (var assignment in wrapper.assignments)
        {
            if (provincesById.ContainsKey(assignment.provinceId))
            {
                ProvinceModel province = provincesById[assignment.provinceId];
                
                if (nationLoader != null)
                {
                    NationModel nation = nationLoader.GetNationById(assignment.nationId);
                    if (nation != null)
                    {
                        AssignProvinceToNation(province, assignment.nationId);
                        loadedCount++;
                    }
                }
            }
        }
        
        GroupProvincesByNation();
        
        Debug.Log($"✓ Loaded {loadedCount} province assignments");
        EditorUtility.DisplayDialog("Success", $"Loaded {loadedCount} assignments!", "OK");
    }
}
#endif