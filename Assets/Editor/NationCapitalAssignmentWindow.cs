using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NationCapitalAssignmentWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/NationCapitalRegistry.asset";

    private NationCapitalRegistry registry;
    private readonly List<NationJson> nations = new List<NationJson>();
    private readonly Dictionary<int, string> provinceNames = new Dictionary<int, string>();
    private readonly Dictionary<int, List<int>> provinceIdsByNation = new Dictionary<int, List<int>>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Nation Capitals")]
    public static void Open()
    {
        GetWindow<NationCapitalAssignmentWindow>("Nation Capitals");
    }

    private void OnEnable()
    {
        RefreshData();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Nation Capital Assignment", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Choose one initial capital for each country. If that province is conquered, the game automatically selects the best remaining connected city.", MessageType.Info);

        if (GUILayout.Button("Refresh Data"))
        {
            RefreshData();
        }

        if (registry == null || nations.Count == 0)
        {
            EditorGUILayout.HelpBox("Nation or province assignment data could not be loaded.", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (NationJson nation in nations.OrderBy(entry => entry.name))
        {
            DrawNationRow(nation);
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Save Capital Assignments"))
        {
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawNationRow(NationJson nation)
    {
        if (!provinceIdsByNation.TryGetValue(nation.id, out List<int> provinceIds) || provinceIds.Count == 0)
        {
            EditorGUILayout.LabelField(nation.name, "No owned provinces");
            return;
        }

        List<string> choices = new List<string> { "Auto-select at game start" };
        choices.AddRange(provinceIds.Select(id => $"{provinceNames[id]} (Province {id})"));

        int selectedIndex = 0;
        if (registry.TryGetCapitalProvinceId(nation.id, out int selectedProvinceId))
        {
            int optionIndex = provinceIds.IndexOf(selectedProvinceId);
            if (optionIndex >= 0) selectedIndex = optionIndex + 1;
        }

        EditorGUI.BeginChangeCheck();
        int nextIndex = EditorGUILayout.Popup(nation.name, selectedIndex, choices.ToArray());
        if (!EditorGUI.EndChangeCheck()) return;

        Undo.RecordObject(registry, "Assign Nation Capital");
        if (nextIndex == 0)
        {
            registry.assignments.RemoveAll(entry => entry.nationId == nation.id);
        }
        else
        {
            registry.SetCapital(nation.id, provinceIds[nextIndex - 1]);
        }

        EditorUtility.SetDirty(registry);
    }

    private void RefreshData()
    {
        registry = AssetDatabase.LoadAssetAtPath<NationCapitalRegistry>(AssetPath);
        if (registry == null)
        {
            const string resourcesDirectory = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesDirectory))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            registry = CreateInstance<NationCapitalRegistry>();
            AssetDatabase.CreateAsset(registry, AssetPath);
            AssetDatabase.SaveAssets();
        }

        nations.Clear();
        provinceNames.Clear();
        provinceIdsByNation.Clear();

        string nationPath = Path.Combine(Application.streamingAssetsPath, "nations.json");
        string provincePath = Path.Combine(Application.streamingAssetsPath, "province_data.json");
        string assignmentPath = Path.Combine(Application.streamingAssetsPath, "province_assignments.json");

        if (!File.Exists(nationPath) || !File.Exists(provincePath) || !File.Exists(assignmentPath)) return;

        NationListWrapper nationWrapper = JsonUtility.FromJson<NationListWrapper>(File.ReadAllText(nationPath));
        ProvinceDataWrapper provinceWrapper = JsonUtility.FromJson<ProvinceDataWrapper>(File.ReadAllText(provincePath));
        ProvinceAssignmentWrapper assignmentWrapper = JsonUtility.FromJson<ProvinceAssignmentWrapper>(File.ReadAllText(assignmentPath));

        if (nationWrapper?.nations == null || provinceWrapper?.provinces == null || assignmentWrapper?.assignments == null) return;

        nations.AddRange(nationWrapper.nations);
        foreach (ProvinceData province in provinceWrapper.provinces)
        {
            provinceNames[province.provinceId] = province.provinceName;
        }

        foreach (ProvinceAssignment assignment in assignmentWrapper.assignments)
        {
            if (!provinceNames.ContainsKey(assignment.provinceId)) continue;

            if (!provinceIdsByNation.TryGetValue(assignment.nationId, out List<int> provinceIds))
            {
                provinceIds = new List<int>();
                provinceIdsByNation.Add(assignment.nationId, provinceIds);
            }

            provinceIds.Add(assignment.provinceId);
        }

        foreach (List<int> provinceIds in provinceIdsByNation.Values)
        {
            provinceIds.Sort((left, right) => string.Compare(provinceNames[left], provinceNames[right], System.StringComparison.Ordinal));
        }
    }
}
