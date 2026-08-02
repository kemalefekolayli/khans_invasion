#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Khans.Invasion.Testing
{
    /// <summary>
    /// Headless smoke-test driver for the SimulationController harness.
    /// Loads GameScene, enters Play Mode, lets the harness run and write its
    /// report, then exits Play Mode. In batchmode it also quits the editor
    /// with a 0/1 exit code reflecting the report's PASS/FAIL.
    ///
    /// Invoke via: -executeMethod Khans.Invasion.Testing.SimulationRunner.Run
    /// </summary>
    public static class SimulationRunner
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";

        private static bool batchMode;

        [MenuItem("Tools/Simulation/Run Headless")]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void Run()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[SimulationRunner] Already in play mode - aborting.");
                return;
            }

            batchMode = Application.isBatchMode;

            SimulationController.RequestRun(SimulationMode.AIOnly);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogError("[SimulationRunner] Scene save cancelled - aborting.");
                Abort();
                return;
            }

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
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

            if (batchMode)
            {
                Debug.Log($"[SimulationRunner] Headless simulation finished with result: {(SimulationController.LastResult ? "PASS" : "FAIL")}");
                EditorApplication.update += ExitEditor;
            }
        }

        private static void ExitEditor()
        {
            EditorApplication.update -= ExitEditor;
            EditorApplication.Exit(SimulationController.LastResult ? 0 : 1);
        }
    }
}
#endif
