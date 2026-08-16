#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Khans.Invasion.Testing
{
    /// <summary>Headless driver for deterministic AI simulation runs. Invoke with -executeMethod Khans.Invasion.Testing.SimulationRunner.Run.</summary>
    public static class SimulationRunner
    {
        private const string LoadingScenePath = "Assets/Scenes/LoadingScene.unity";
        private static bool batchMode;

        [MenuItem("Tools/Simulation/Run Headless")]
        public static void RunFromMenu() => Run();

        public static void Run()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[SimulationRunner] Already in play mode - aborting.");
                return;
            }

            batchMode = Application.isBatchMode;
            SimulationController.RequestRun(
                ParseMode(),
                ReadIntArgument("-khansSimTurns", 50),
                ReadFloatArgument("-khansSimInterval", 0.15f),
                ReadFloatArgument("-khansSimTimeout", 180f),
                ReadIntArgument("-khansSimSeed", 0),
                ReadStringArgument("-khansSimReport", SimulationController.ReportFileName));
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogError("[SimulationRunner] Scene save cancelled - aborting.");
                Abort();
                return;
            }

            EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static SimulationMode ParseMode()
        {
            string mode = ReadStringArgument("-khansSimMode", "AIOnly");
            return Enum.TryParse(mode, true, out SimulationMode parsed) ? parsed : SimulationMode.AIOnly;
        }

        private static string ReadStringArgument(string argumentName, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argumentName, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return fallback;
        }

        private static int ReadIntArgument(string argumentName, int fallback)
        {
            return int.TryParse(ReadStringArgument(argumentName, fallback.ToString()), out int value) ? value : fallback;
        }

        private static float ReadFloatArgument(string argumentName, float fallback)
        {
            return float.TryParse(ReadStringArgument(argumentName, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : fallback;
        }

        private static void Abort()
        {
            SimulationController.ResetRequest();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (!batchMode) return;

            Debug.Log($"[SimulationRunner] Headless simulation finished with result: {(SimulationController.LastResult ? "PASS" : "FAIL")}");
            EditorApplication.update += ExitEditor;
        }

        private static void ExitEditor()
        {
            EditorApplication.update -= ExitEditor;
            EditorApplication.Exit(SimulationController.LastResult ? 0 : 1);
        }
    }
}
#endif
