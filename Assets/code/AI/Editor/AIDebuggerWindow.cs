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
            GameLog.Warning(GameLogCategory.Core, "Force AI Turn not implemented yet - waiting for next turn naturally.");
        }
    }

    private void DrawNationRow(AINationController ai)
    {
        EditorGUILayout.BeginHorizontal("box");

        // Nation Name (Colored)
        GUIStyle nameStyle = new GUIStyle(EditorStyles.label);
        if (ai.Nation != null)
        {
            Color nationColor = NationLoader.HexToColor(ai.Nation.nationColor);
            nationColor.a = 1f; 
            nameStyle.normal.textColor = nationColor; 
        }
        GUILayout.Label(ai.Nation != null ? ai.Nation.nationName : "Unknown", nameStyle, GUILayout.Width(120), GUILayout.MinWidth(80));

        // State
        Color stateColor = Color.white;
        switch (ai.StateMachine.CurrentState)
        {
            case AIState.Idle: stateColor = Color.gray; break;
            case AIState.Recruiting: stateColor = Color.cyan; break;
            case AIState.Attacking: stateColor = Color.red; break;
            case AIState.Fortifying: stateColor = Color.green; break;
        }
        GUIStyle stateStyle = new GUIStyle(EditorStyles.label);
        stateStyle.normal.textColor = stateColor;
        GUILayout.Label(ai.StateMachine.CurrentState.ToString(), stateStyle, GUILayout.Width(80));

        // Gold
        GUILayout.Label(ai.EconomyData.gold.ToString("F0"), GUILayout.Width(60));
        
        // Income
        GUILayout.Label(ai.EconomyData.TotalIncome.ToString("F0"), GUILayout.Width(40));

        // Population
        string popStr = $"{ai.EconomyData.totalPopulation/1000f:F1}k / {ai.EconomyData.totalMaxPopulation/1000f:F1}k";
        GUILayout.Label(popStr, GUILayout.Width(90));

        // Troops (Calculate on fly)
        float troopCount = 0;
        if (ArmyManager.Instance != null)
        {
            var armies = ArmyManager.Instance.GetAllArmies();
            foreach (var army in armies)
            {
                if (army != null && army.OwnerNation == ai.Nation)
                {
                    troopCount += army.ArmySize;
                }
            }
        }
        GUILayout.Label(troopCount.ToString("F0"), GUILayout.Width(60));

        // Fortresses
        int fortressCount = 0;
        if (ai.Nation != null && ai.Nation.provinceList != null)
        {
            foreach (var p in ai.Nation.provinceList)
            {
                if (p != null && p.buildings != null && p.buildings.Contains("Fortress"))
                {
                    fortressCount++;
                }
            }
        }
        GUILayout.Label(fortressCount.ToString(), GUILayout.Width(30));

        // Target
        string targetName = ai.TargetNation != null ? ai.TargetNation.nationName : "-";
        GUILayout.Label(targetName, GUILayout.Width(100));

        // Last Action
        GUILayout.Label(ai.LastActionDescription, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Nation", GUILayout.Width(120), GUILayout.MinWidth(80));
        GUILayout.Label("State", GUILayout.Width(80));
        GUILayout.Label("Gold", GUILayout.Width(60));
        GUILayout.Label("Inc.", GUILayout.Width(40));
        GUILayout.Label("Pop (C/M)", GUILayout.Width(90));
        GUILayout.Label("Troops", GUILayout.Width(60));
        GUILayout.Label("Fts", GUILayout.Width(30));
        GUILayout.Label("Target", GUILayout.Width(100));
        GUILayout.Label("Last Action", GUILayout.ExpandWidth(true));
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
