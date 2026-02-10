using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public class AIDebuggerWindow : EditorWindow
{
    private Vector2 scrollPosition;

    [MenuItem("Window/AI Debugger")]
    public static void ShowWindow()
    {
        GetWindow<AIDebuggerWindow>("AI Debugger");
    }

    private void OnGUI()
    {
        GUILayout.Label("AI Nation Status", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to view AI status.", MessageType.Info);
            return;
        }

        if (AIManager.Instance == null)
        {
            EditorGUILayout.HelpBox("AIManager not found in scene.", MessageType.Warning);
            return;
        }

        if (AIManager.Instance.AINations == null || AIManager.Instance.AINations.Count == 0)
        {
            EditorGUILayout.HelpBox("No AI Nations initialized yet.", MessageType.Info);
            return;
        }

        DrawHeader();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var ai in AIManager.Instance.AINations)
        {
            DrawNationRow(ai);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        if (GUILayout.Button("Force AI Turn (Debug Use Only)"))
        {
            // Reflection or public method to force turn? 
            // For now, let's just use the event if we can, but subscribing to it won't trigger it.
            // We'll just print a warning that this button is a placeholder unless we expose a method.
            Debug.LogWarning("Force AI Turn not implemented yet - waiting for next turn naturally.");
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Nation", GUILayout.Width(120), GUILayout.MinWidth(80));
        GUILayout.Label("State", GUILayout.Width(80));
        GUILayout.Label("Gold", GUILayout.Width(60));
        GUILayout.Label("Inc.", GUILayout.Width(50));
        GUILayout.Label("Provs", GUILayout.Width(50));
        GUILayout.Label("Last Action", GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNationRow(AINationController ai)
    {
        EditorGUILayout.BeginHorizontal("box");

        // Nation Name (Colored)
        GUIStyle nameStyle = new GUIStyle(EditorStyles.label);
        if (ai.Nation != null)
        {
            Color nationColor = ai.Nation.nationColor;
            // Make sure it's visible on dark/light skin (add some alpha/brightness if needed)
            nationColor.a = 1f; 
            nameStyle.normal.textColor = nationColor; 
        }
        GUILayout.Label(ai.Nation != null ? ai.Nation.nationName : "Unknown", nameStyle, GUILayout.Width(120), GUILayout.MinWidth(80));

        // State
        Color stateColor = Color.white;
        switch (ai.StateMachine.CurrentState)
        {
            case AIState.Idle: stateColor = Color.gray; break;
            case AIState.Expanding: stateColor = Color.red; break;
            case AIState.Fortifying: stateColor = Color.green; break;
        }
        GUIStyle stateStyle = new GUIStyle(EditorStyles.label);
        stateStyle.normal.textColor = stateColor;
        GUILayout.Label(ai.StateMachine.CurrentState.ToString(), stateStyle, GUILayout.Width(80));

        // Gold
        GUILayout.Label(ai.EconomyData.gold.ToString("F0"), GUILayout.Width(60));
        
        // Income
        GUILayout.Label(ai.EconomyData.totalIncome.ToString("F0"), GUILayout.Width(50));

        // Provinces
        int provCount = ai.Nation != null ? ai.Nation.provinceList.Count : 0;
        GUILayout.Label(provCount.ToString(), GUILayout.Width(50));

        // Last Action
        GUILayout.Label(ai.LastActionDescription, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();
    }

    private void Update()
    {
        // Refresh the window every frame so numbers update live
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
#endif
