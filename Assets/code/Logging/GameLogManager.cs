using UnityEngine;

public class GameLogManager : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private GameLogProfile profile = GameLogProfile.AIWarOnly;

    [Header("Custom Profile")]
    [SerializeField] private GameLogCategory customCategories = GameLogCategory.AIWar;

    [Header("Severity")]
    [SerializeField] private bool showWarnings = true;
    [SerializeField] private bool showErrors = true;

    [Header("File Sink")]
    [SerializeField] private bool fileLoggingEnabled = true;
    [SerializeField] private string logFilePath = "Logs/game_log.txt";

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Apply Log Profile")]
    public void Apply()
    {
        GameLog.Configure(profile, customCategories, showWarnings, showErrors);
        GameLogFileSink.Configure(fileLoggingEnabled ? logFilePath : null);
    }
}
