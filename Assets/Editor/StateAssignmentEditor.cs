using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class StateAssignmentEditor : EditorWindow
{
    private const string StatesContainerName = "States";

    private readonly List<StateModel> states = new List<StateModel>();
    private StateModel selectedState;
    private string newStateName = "New State";
    private Vector2 scrollPosition;

    [MenuItem("Tools/State Assignment Editor")]
    private static void OpenWindow()
    {
        GetWindow<StateAssignmentEditor>("State Assignment");
    }

    private void OnFocus()
    {
        RefreshStates();
    }

    private void OnGUI()
    {
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage == null || prefabStage.prefabContentsRoot == null)
        {
            EditorGUILayout.HelpBox(
                "Open MapParentPrefab in Prefab Mode to assign provinces to states. This tool only edits the active prefab contents.",
                MessageType.Info);
            return;
        }

        GameObject prefabRoot = prefabStage.prefabContentsRoot;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Prefab Root", prefabRoot.name, EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(90)))
            {
                RefreshStates();
            }
        }

        DrawCreateState(prefabRoot);
        EditorGUILayout.Space(8);
        DrawStateList(prefabRoot);

        if (selectedState != null)
        {
            EditorGUILayout.Space(8);
            DrawAssignmentActions(prefabRoot);
        }
    }

    private void DrawCreateState(GameObject prefabRoot)
    {
        EditorGUILayout.LabelField("Create State", EditorStyles.boldLabel);
        newStateName = EditorGUILayout.TextField("Name", newStateName);
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newStateName)))
        {
            if (GUILayout.Button("Create State"))
            {
                CreateState(prefabRoot);
            }
        }
    }

    private void DrawStateList(GameObject prefabRoot)
    {
        EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
        if (states.Count == 0)
        {
            EditorGUILayout.HelpBox("No states exist below the prefab root yet.", MessageType.None);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(120));
        foreach (StateModel state in states)
        {
            if (state == null)
            {
                continue;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{state.stateName}  (ID: {state.stateId})", GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField($"{state.provinceList?.Count ?? 0} provinces", GUILayout.Width(100));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    selectedState = state;
                    Selection.activeGameObject = state.gameObject;
                    Repaint();
                }
            }
        }
        EditorGUILayout.EndScrollView();

        if (selectedState == null || !states.Contains(selectedState))
        {
            selectedState = null;
            return;
        }

        EditorGUILayout.LabelField("Selected", selectedState.stateName, EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Delete Selected State"))
        {
            DeleteSelectedState(prefabRoot);
        }
    }

    private void DrawAssignmentActions(GameObject prefabRoot)
    {
        EditorGUILayout.LabelField($"Province Assignment — {selectedState.stateName}", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use Unity's normal hierarchy or scene multi-selection, then assign the selected provinces. River-tagged provinces are skipped.", MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Assign Selected Provinces"))
            {
                AssignSelectedProvinces(prefabRoot);
            }
            if (GUILayout.Button("Unassign Selected Provinces"))
            {
                UnassignSelectedProvinces(prefabRoot);
            }
        }

        if (GUILayout.Button("Rebuild Selected State List"))
        {
            RebuildSelectedStateList(prefabRoot);
        }
    }

    private void RefreshStates()
    {
        states.Clear();
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage == null || prefabStage.prefabContentsRoot == null)
        {
            selectedState = null;
            Repaint();
            return;
        }

        Transform statesContainer = prefabStage.prefabContentsRoot.transform.Find(StatesContainerName);
        if (statesContainer != null)
        {
            states.AddRange(statesContainer.GetComponentsInChildren<StateModel>(true));
        }

        if (selectedState != null && !states.Contains(selectedState))
        {
            selectedState = null;
        }
        Repaint();
    }

    private void CreateState(GameObject prefabRoot)
    {
        Transform statesContainer = GetOrCreateStatesContainer(prefabRoot);
        long maximumId = 0;
        foreach (StateModel state in states)
        {
            if (state != null)
            {
                maximumId = System.Math.Max(maximumId, state.stateId);
            }
        }

        GameObject stateObject = new GameObject(newStateName.Trim());
        Undo.RegisterCreatedObjectUndo(stateObject, "Create State");
        stateObject.transform.SetParent(statesContainer, false);
        StateModel stateModel = Undo.AddComponent<StateModel>(stateObject);
        stateModel.stateName = newStateName.Trim();
        stateModel.stateId = maximumId + 1;
        stateModel.provinceList = new List<ProvinceModel>();

        EditorUtility.SetDirty(stateModel);
        EditorUtility.SetDirty(statesContainer.gameObject);
        EditorUtility.SetDirty(prefabRoot);
        selectedState = stateModel;
        RefreshStates();
    }

    private Transform GetOrCreateStatesContainer(GameObject prefabRoot)
    {
        Transform existingContainer = prefabRoot.transform.Find(StatesContainerName);
        if (existingContainer != null)
        {
            return existingContainer;
        }

        GameObject container = new GameObject(StatesContainerName);
        Undo.RegisterCreatedObjectUndo(container, "Create States Container");
        container.transform.SetParent(prefabRoot.transform, false);
        EditorUtility.SetDirty(prefabRoot);
        return container.transform;
    }

    private void DeleteSelectedState(GameObject prefabRoot)
    {
        if (selectedState == null || !EditorUtility.DisplayDialog(
                "Delete State",
                $"Delete '{selectedState.stateName}' and clear its province assignments?",
                "Delete",
                "Cancel"))
        {
            return;
        }

        foreach (ProvinceModel province in prefabRoot.GetComponentsInChildren<ProvinceModel>(true))
        {
            if (province != null && province.provinceState == selectedState)
            {
                Undo.RecordObject(province, "Clear Province State");
                province.provinceState = null;
                EditorUtility.SetDirty(province);
            }
        }

        Undo.DestroyObjectImmediate(selectedState.gameObject);
        EditorUtility.SetDirty(prefabRoot);
        selectedState = null;
        RefreshStates();
    }

    private void AssignSelectedProvinces(GameObject prefabRoot)
    {
        foreach (ProvinceModel province in GetSelectedProvinces())
        {
            if (province == null || province.CompareTag("River"))
            {
                continue;
            }

            foreach (StateModel state in states)
            {
                if (state != null && state.provinceList != null && state.provinceList.Contains(province))
                {
                    Undo.RecordObject(state, "Remove Province From State");
                    state.provinceList.Remove(province);
                    EditorUtility.SetDirty(state);
                }
            }

            Undo.RecordObject(province, "Assign Province State");
            Undo.RecordObject(selectedState, "Assign Province State");
            province.provinceState = selectedState;
            if (selectedState.provinceList == null)
            {
                selectedState.provinceList = new List<ProvinceModel>();
            }
            if (!selectedState.provinceList.Contains(province))
            {
                selectedState.provinceList.Add(province);
            }
            EditorUtility.SetDirty(province);
            EditorUtility.SetDirty(selectedState);
        }

        EditorUtility.SetDirty(prefabRoot);
    }

    private void UnassignSelectedProvinces(GameObject prefabRoot)
    {
        foreach (ProvinceModel province in GetSelectedProvinces())
        {
            if (province == null || province.provinceState != selectedState)
            {
                continue;
            }

            Undo.RecordObject(province, "Unassign Province State");
            Undo.RecordObject(selectedState, "Unassign Province State");
            province.provinceState = null;
            selectedState.provinceList?.Remove(province);
            EditorUtility.SetDirty(province);
            EditorUtility.SetDirty(selectedState);
        }

        EditorUtility.SetDirty(prefabRoot);
    }

    private void RebuildSelectedStateList(GameObject prefabRoot)
    {
        Undo.RecordObject(selectedState, "Rebuild State Province List");
        if (selectedState.provinceList == null)
        {
            selectedState.provinceList = new List<ProvinceModel>();
        }
        else
        {
            selectedState.provinceList.Clear();
        }

        foreach (ProvinceModel province in prefabRoot.GetComponentsInChildren<ProvinceModel>(true))
        {
            if (province != null && province.provinceState == selectedState)
            {
                selectedState.provinceList.Add(province);
            }
        }

        EditorUtility.SetDirty(selectedState);
        EditorUtility.SetDirty(prefabRoot);
    }

    private static IEnumerable<ProvinceModel> GetSelectedProvinces()
    {
        HashSet<ProvinceModel> provinces = new HashSet<ProvinceModel>();
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject == null)
            {
                continue;
            }

            foreach (ProvinceModel province in selectedObject.GetComponentsInChildren<ProvinceModel>(true))
            {
                provinces.Add(province);
            }
        }
        return provinces;
    }
}
