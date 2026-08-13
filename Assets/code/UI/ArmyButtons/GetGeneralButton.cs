using UnityEngine;
using UnityEngine.UI;

/// <summary>Temporary free general spawn button for testing.</summary>
public class GetGeneralButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private GeneralSpawner generalSpawner;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    private void OnClicked()
    {
        if (generalSpawner == null)
            generalSpawner = FindFirstObjectByType<GeneralSpawner>();

        if (generalSpawner == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[GetGeneralButton] GeneralSpawner not found.");
            return;
        }

        generalSpawner.TrySpawnFreeGeneral();
    }
}
