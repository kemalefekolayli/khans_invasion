using System.Collections.Generic;
using UnityEngine;

public class RuntimeUIInstaller : MonoBehaviour
{
    [Header("Runtime UI")]
    [SerializeField] private GameObject[] uiPrefabs;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField, Range(0f, 1f)] private float match = 0.5f;

    private readonly List<GameObject> instances = new List<GameObject>();
    private bool installed;

    private void OnEnable() => GameEvents.OnMapLoaded += Install;
    private void OnDisable() => GameEvents.OnMapLoaded -= Install;

    public void Install()
    {
        if (installed) return;
        installed = true;

        if (uiPrefabs == null) return;
        for (int i = 0; i < uiPrefabs.Length; i++)
        {
            GameObject prefab = uiPrefabs[i];
            if (prefab == null)
            {
                GameLog.Warning(GameLogCategory.Core, $"[RuntimeUIInstaller] UI prefab at index {i} is not assigned.");
                continue;
            }

            GameObject instance = Instantiate(prefab);
            RuntimeScreenCanvasPolicy.Apply(instance, referenceResolution, match);
            instances.Add(instance);
        }
    }
}
