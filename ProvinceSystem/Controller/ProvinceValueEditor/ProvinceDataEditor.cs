#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Editor window for visually editing province data.
/// Access via Tools > Province Data Editor
/// </summary>
public class ProvinceDataEditor : EditorWindow
{
    // Data
    private Dictionary<int, ProvinceData> provinceDataById = new Dictionary<int, ProvinceData>();
    private List<ProvinceModel> sceneProvinces = new List<ProvinceModel>();
    private ProvinceModel selectedProvince;
    private ProvinceData selectedData;
    
    // UI State
    private Vector2 listScrollPosition;
    private Vector2 editorScrollPosition;
    private string searchFilter = "";
    private int filterNationId = -1; // -1 = all nations
    private bool selectModeActive = false;
    private bool showOnlyUnedited = false;
    private bool autoSave = true;  // Default to true now
    
    // Track changes for auto-save
    private bool hasUnsavedChanges = false;
    private double lastChangeTime = 0;
    private const double AUTO_SAVE_DELAY = 0.5; // Save 0.5 seconds after last change
    
    // References
    private NationLoader nationLoader;
    
    // Styling
    private GUIStyle headerStyle;
    private GUIStyle selectedStyle;
    private GUIStyle unsavedStyle;
    private bool stylesInitialized = false;
    
    // File path
    private const string DATA_FILE_NAME = "province_data.json";
    
    [MenuItem("Tools/Province Data Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProvinceDataEditor>("Province Data Editor");
        window.minSize = new Vector2(800, 600);
    }
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += OnEditorUpdate;
        RefreshData();
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= OnEditorUpdate;
        
        // Save on close if there are unsaved changes
        if (hasUnsavedChanges)
        {
            SaveAllToJson(silent: true);
        }
    }
    
    private void OnEditorUpdate()
    {
        // Auto-save after delay
        if (autoSave && hasUnsavedChanges && EditorApplication.timeSinceStartup - lastChangeTime > AUTO_SAVE_DELAY)
        {
            SaveAllToJson(silent: true);
            hasUnsavedChanges = false;
        }
    }
    
    private void MarkDirty()
    {
        hasUnsavedChanges = true;
        lastChangeTime = EditorApplication.timeSinceStartup;
        
        // Also save to dictionary immediately
        if (selectedData != null)
        {
            provinceDataById[selectedData.provinceId] = selectedData;
        }
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            margin = new RectOffset(0, 0, 10, 10)
        };
        
        selectedStyle = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { background = MakeColorTexture(new Color(0.3f, 0.5f, 0.8f, 0.3f)) }
        };
        
        unsavedStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(1f, 0.6f, 0.2f) }
        };
        
        stylesInitialized = true;
    }
    
    private Texture2D MakeColorTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
    
    private void RefreshData()
    {
        // Find all provinces in scene
        sceneProvinces.Clear();
        sceneProvinces.AddRange(FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None)
            .Where(p => !p.CompareTag("River"))
            .OrderBy(p => p.provinceId));
        
        // Find nation loader
        nationLoader = FindFirstObjectByType<NationLoader>();
        
        // Load existing data
        LoadDataFromJson();
        
        Debug.Log($"[ProvinceDataEditor] Refreshed: {sceneProvinces.Count} provinces, {provinceDataById.Count} data entries");
    }
    
    private void OnGUI()
    {
        InitStyles();
        
        // Unsaved changes indicator
        if (hasUnsavedChanges)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("● Unsaved changes", unsavedStyle);
            if (GUILayout.Button("Save Now", GUILayout.Width(80)))
            {
                SaveAllToJson(silent: false);
                hasUnsavedChanges = false;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.BeginHorizontal();
        
        // Left panel - Province list
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        DrawProvinceList();
        EditorGUILayout.EndVertical();
        
        // Separator
        EditorGUILayout.BeginVertical(GUILayout.Width(2));
        GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(2));
        EditorGUILayout.EndVertical();
        
        // Right panel - Province editor
        EditorGUILayout.BeginVertical();
        DrawProvinceEditor();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawProvinceList()
    {
        GUILayout.Label("Provinces", headerStyle);
        
        // Toolbar
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            RefreshData();
        }
        
        selectModeActive = GUILayout.Toggle(selectModeActive, "Scene Select", "Button", GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();
        
        // Search filter
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();
        
        // Nation filter
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Nation:", GUILayout.Width(50));
        if (nationLoader != null && nationLoader.allNations.Count > 0)
        {
            List<string> nationNames = new List<string> { "All Nations" };
            nationNames.AddRange(nationLoader.allNations.Select(n => n.nationName));
            
            int selected = filterNationId == -1 ? 0 : nationLoader.allNations.FindIndex(n => n.nationId == filterNationId) + 1;
            int newSelected = EditorGUILayout.Popup(selected, nationNames.ToArray());
            
            if (newSelected == 0)
                filterNationId = -1;
            else
                filterNationId = (int)nationLoader.allNations[newSelected - 1].nationId;
        }
        EditorGUILayout.EndHorizontal();
        
        // Options
        showOnlyUnedited = EditorGUILayout.Toggle("Show Only Unedited", showOnlyUnedited);
        
        EditorGUILayout.Space(5);
        
        // Province list
        listScrollPosition = EditorGUILayout.BeginScrollView(listScrollPosition);
        
        var filteredProvinces = GetFilteredProvinces();
        
        foreach (var province in filteredProvinces)
        {
            bool hasData = provinceDataById.ContainsKey((int)province.provinceId);
            bool isSelected = selectedProvince == province;
            
            // Get nation color
            Color bgColor = Color.gray;
            if (province.provinceOwner != null && !string.IsNullOrEmpty(province.provinceOwner.nationColor))
            {
                bgColor = NationLoader.HexToColor(province.provinceOwner.nationColor);
            }
            
            // Draw province row
            EditorGUILayout.BeginHorizontal(isSelected ? selectedStyle : EditorStyles.helpBox);
            
            // Color indicator
            GUI.backgroundColor = bgColor;
            GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            
            // Province info
            string displayName = !string.IsNullOrEmpty(province.provinceName) ? province.provinceName : $"Province_{province.provinceId}";
            string status = hasData ? "✓" : "○";
            
            if (GUILayout.Button($"{status} {displayName}", EditorStyles.label))
            {
                SelectProvince(province);
            }
            
            // ID label
            GUILayout.Label($"#{province.provinceId}", GUILayout.Width(40));
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        // Stats
        EditorGUILayout.Space(5);
        int editedCount = sceneProvinces.Count(p => provinceDataById.ContainsKey((int)p.provinceId));
        EditorGUILayout.LabelField($"Edited: {editedCount}/{sceneProvinces.Count}");
    }
    
    private List<ProvinceModel> GetFilteredProvinces()
    {
        return sceneProvinces.Where(p =>
        {
            // Search filter
            if (!string.IsNullOrEmpty(searchFilter))
            {
                bool matchesName = p.provinceName?.ToLower().Contains(searchFilter.ToLower()) ?? false;
                bool matchesId = p.provinceId.ToString().Contains(searchFilter);
                if (!matchesName && !matchesId) return false;
            }
            
            // Nation filter
            if (filterNationId != -1)
            {
                if (p.provinceOwner == null || p.provinceOwner.nationId != filterNationId)
                    return false;
            }
            
            // Unedited filter
            if (showOnlyUnedited && provinceDataById.ContainsKey((int)p.provinceId))
                return false;
            
            return true;
        }).ToList();
    }
    
    private void SelectProvince(ProvinceModel province)
    {
        // Save current before switching
        if (selectedData != null && hasUnsavedChanges)
        {
            provinceDataById[selectedData.provinceId] = selectedData;
        }
        
        selectedProvince = province;
        
        // Get or create data
        int id = (int)province.provinceId;
        if (!provinceDataById.TryGetValue(id, out selectedData))
        {
            selectedData = ProvinceData.FromProvinceModel(province);
            selectedData.provinceId = id;
            selectedData.provinceName = province.provinceName;
            provinceDataById[id] = selectedData;
            MarkDirty();
        }
        
        // Focus in scene
        Selection.activeGameObject = province.gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();
        
        Repaint();
    }
    
    private void DrawProvinceEditor()
    {
        if (selectedProvince == null || selectedData == null)
        {
            GUILayout.Label("Select a province to edit", headerStyle);
            
            EditorGUILayout.Space(20);
            
            // Bulk operations
            GUILayout.Label("Bulk Operations", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate Default Data for All Provinces"))
            {
                GenerateDefaultDataForAll();
            }
            
            if (GUILayout.Button("Export Current Values to JSON"))
            {
                ExportCurrentValuesToJson();
            }
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Load from JSON"))
            {
                LoadDataFromJson();
            }
            
            if (GUILayout.Button("Apply JSON to Scene"))
            {
                ApplyDataToScene();
            }
            
            return;
        }
        
        editorScrollPosition = EditorGUILayout.BeginScrollView(editorScrollPosition);
        
        // Header
        string nationName = selectedProvince.provinceOwner?.nationName ?? "Unowned";
        GUILayout.Label($"Editing: {selectedData.provinceName} (#{selectedData.provinceId})", headerStyle);
        EditorGUILayout.LabelField($"Nation: {nationName}");
        
        EditorGUILayout.Space(10);
        
        // Presets
        GUILayout.Label("Apply Preset:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Village")) ApplyPreset(ProvincePresets.SmallVillage);
        if (GUILayout.Button("Town")) ApplyPreset(ProvincePresets.Town);
        if (GUILayout.Button("City")) ApplyPreset(ProvincePresets.City);
        if (GUILayout.Button("Capital")) ApplyPreset(ProvincePresets.Capital);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Trade Hub")) ApplyPreset(ProvincePresets.TradeHub);
        if (GUILayout.Button("Fortress")) ApplyPreset(ProvincePresets.Fortress);
        if (GUILayout.Button("Desert")) ApplyPreset(ProvincePresets.Desert);
        if (GUILayout.Button("Forest")) ApplyPreset(ProvincePresets.Forest);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Track changes with EditorGUI.BeginChangeCheck
        EditorGUI.BeginChangeCheck();
        
        // Basic Info
        GUILayout.Label("Basic Info", EditorStyles.boldLabel);
        selectedData.provinceName = EditorGUILayout.TextField("Name", selectedData.provinceName);
        selectedData.terrainType = EditorGUILayout.TextField("Terrain Type", selectedData.terrainType);
        selectedData.isCapital = EditorGUILayout.Toggle("Is Capital", selectedData.isCapital);
        
        EditorGUILayout.Space(10);
        
        // Economy
        GUILayout.Label("Economy", EditorStyles.boldLabel);
        selectedData.taxIncome = EditorGUILayout.Slider("Tax Income", selectedData.taxIncome, 0f, 100f);
        selectedData.tradePower = EditorGUILayout.Slider("Trade Power", selectedData.tradePower, 0f, 100f);
        selectedData.availableLoot = EditorGUILayout.Slider("Available Loot", selectedData.availableLoot, 0f, 1000f);
        
        EditorGUILayout.Space(10);
        
        // Population
        GUILayout.Label("Population", EditorStyles.boldLabel);
        selectedData.currentPop = EditorGUILayout.Slider("Current Pop", selectedData.currentPop, 0f, selectedData.maxPop);
        selectedData.maxPop = EditorGUILayout.Slider("Max Pop", selectedData.maxPop, 100f, 5000f);
        
        // Population bar
        Rect popRect = GUILayoutUtility.GetRect(100, 20);
        EditorGUI.ProgressBar(popRect, selectedData.currentPop / selectedData.maxPop, 
            $"{selectedData.currentPop:F0} / {selectedData.maxPop:F0}");
        
        EditorGUILayout.Space(10);
        
        // Military
        GUILayout.Label("Military / Defense", EditorStyles.boldLabel);
        selectedData.defenceForceSize = EditorGUILayout.Slider("Garrison Size", selectedData.defenceForceSize, 0f, 1000f);
        selectedData.defenceForceStr = EditorGUILayout.Slider("Garrison Strength", selectedData.defenceForceStr, 0.5f, 3f);
        
        // Check if anything changed
        if (EditorGUI.EndChangeCheck())
        {
            MarkDirty();
        }
        
        // Effective defense calculation
        float effectiveDefense = selectedData.defenceForceSize * selectedData.defenceForceStr;
        EditorGUILayout.LabelField($"Effective Defense: {effectiveDefense:F0}");
        
        EditorGUILayout.Space(20);
        
        // Actions
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Apply to Scene Object", GUILayout.Height(30)))
        {
            ApplyToSceneObject();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Save All to JSON", GUILayout.Height(35)))
        {
            SaveAllToJson(silent: false);
            hasUnsavedChanges = false;
        }
        
        EditorGUILayout.EndScrollView();
        
        // Auto-save toggle
        EditorGUILayout.Space(5);
        autoSave = EditorGUILayout.Toggle("Auto-save on change", autoSave);
    }
    
    private void ApplyPreset(ProvinceData preset)
    {
        if (selectedData == null) return;
        
        // Keep ID and name
        int id = selectedData.provinceId;
        string name = selectedData.provinceName;
        
        // Copy preset values
        selectedData.taxIncome = preset.taxIncome;
        selectedData.tradePower = preset.tradePower;
        selectedData.currentPop = preset.currentPop;
        selectedData.maxPop = preset.maxPop;
        selectedData.defenceForceSize = preset.defenceForceSize;
        selectedData.defenceForceStr = preset.defenceForceStr;
        selectedData.availableLoot = preset.availableLoot;
        selectedData.terrainType = preset.terrainType;
        selectedData.isCapital = preset.isCapital;
        
        // Restore ID and name
        selectedData.provinceId = id;
        selectedData.provinceName = name;
        
        MarkDirty();
        Repaint();
    }
    
    private void ApplyToSceneObject()
    {
        if (selectedProvince == null || selectedData == null) return;
        
        selectedData.ApplyToProvinceModel(selectedProvince);
        EditorUtility.SetDirty(selectedProvince);
        
        Debug.Log($"[ProvinceDataEditor] Applied data to scene object: {selectedProvince.provinceName}");
    }
    
    private void GenerateDefaultDataForAll()
    {
        int count = 0;
        foreach (var province in sceneProvinces)
        {
            int id = (int)province.provinceId;
            if (!provinceDataById.ContainsKey(id))
            {
                var data = ProvinceData.FromProvinceModel(province);
                data.provinceId = id;
                data.provinceName = province.provinceName;
                
                // Apply default "Town" values if province has zeros
                if (data.taxIncome <= 0) data.taxIncome = 10f;
                if (data.maxPop <= 0) data.maxPop = 500f;
                if (data.currentPop <= 0) data.currentPop = 100f;
                if (data.defenceForceSize <= 0) data.defenceForceSize = 50f;
                if (data.defenceForceStr <= 0) data.defenceForceStr = 1f;
                
                provinceDataById[id] = data;
                count++;
            }
        }
        
        Debug.Log($"[ProvinceDataEditor] Generated default data for {count} provinces");
        SaveAllToJson(silent: false);
    }
    
    private void ExportCurrentValuesToJson()
    {
        foreach (var province in sceneProvinces)
        {
            int id = (int)province.provinceId;
            provinceDataById[id] = ProvinceData.FromProvinceModel(province);
            provinceDataById[id].provinceId = id;
            provinceDataById[id].provinceName = province.provinceName;
        }
        
        SaveAllToJson(silent: false);
        Debug.Log($"[ProvinceDataEditor] Exported {provinceDataById.Count} provinces to JSON");
    }
    
    private void SaveAllToJson(bool silent = false)
    {
        ProvinceDataWrapper wrapper = new ProvinceDataWrapper
        {
            provinces = provinceDataById.Values.OrderBy(p => p.provinceId).ToArray(),
            lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            version = 1
        };
        
        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.streamingAssetsPath, DATA_FILE_NAME);
        
        // Ensure directory exists
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        
        if (!silent)
        {
            Debug.Log($"✓ Saved {wrapper.provinces.Length} provinces to {path}");
            EditorUtility.DisplayDialog("Saved", $"Saved {wrapper.provinces.Length} provinces to province_data.json", "OK");
        }
    }
    
    private void LoadDataFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, DATA_FILE_NAME);
        
        if (!File.Exists(path))
        {
            Debug.Log($"[ProvinceDataEditor] No existing data file at {path}");
            return;
        }
        
        string json = File.ReadAllText(path);
        ProvinceDataWrapper wrapper = JsonUtility.FromJson<ProvinceDataWrapper>(json);
        
        if (wrapper == null || wrapper.provinces == null)
        {
            Debug.LogError("[ProvinceDataEditor] Failed to parse province_data.json");
            return;
        }
        
        provinceDataById.Clear();
        foreach (var data in wrapper.provinces)
        {
            provinceDataById[data.provinceId] = data;
        }
        
        Debug.Log($"✓ Loaded {wrapper.provinces.Length} provinces from JSON (v{wrapper.version}, modified: {wrapper.lastModified})");
    }
    
    private void ApplyDataToScene()
    {
        int appliedCount = 0;
        
        foreach (var province in sceneProvinces)
        {
            int id = (int)province.provinceId;
            if (provinceDataById.TryGetValue(id, out ProvinceData data))
            {
                data.ApplyToProvinceModel(province);
                EditorUtility.SetDirty(province);
                appliedCount++;
            }
        }
        
        Debug.Log($"✓ Applied data to {appliedCount} scene provinces");
        EditorUtility.DisplayDialog("Applied", $"Applied data to {appliedCount} provinces in scene", "OK");
    }
    
    // Scene view click handling
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!selectModeActive) return;
        
        Event e = Event.current;
        
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100f);
            
            if (hit.collider != null)
            {
                ProvinceModel province = hit.collider.GetComponent<ProvinceModel>();
                if (province != null && !province.CompareTag("River"))
                {
                    SelectProvince(province);
                    e.Use();
                }
            }
        }
        
        // Draw highlight on selected
        if (selectedProvince != null)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireCube(selectedProvince.transform.position, Vector3.one * 0.5f);
        }
        
        sceneView.Repaint();
    }
}
#endif