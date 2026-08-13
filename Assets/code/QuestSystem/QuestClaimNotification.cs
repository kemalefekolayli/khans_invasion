using TMPro;
using UnityEngine;

public class QuestClaimNotification : MonoBehaviour
{
    private const string ClaimMessage = "Quest complete - claim your prize!";

    [Header("Notification Text")]
    [SerializeField] private TextMeshProUGUI claimText;
    [SerializeField] private TextMeshProUGUI claimExclamation;

    private QuestManager boundManager;

    private void Awake()
    {
        ConfigureText();
        ConfigureGraphics();
        SetVisible(false);
    }

    private void OnEnable()
    {
        TryBindManager();
        Refresh();
    }

    private void Update()
    {
        if (boundManager != QuestManager.Instance)
        {
            UnbindManager();
            TryBindManager();
            Refresh();
        }
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    private void TryBindManager()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null || manager == boundManager) return;

        boundManager = manager;
        boundManager.OnQuestCompleted += OnQuestStateChanged;
        boundManager.OnQuestClaimed += OnQuestStateChanged;
    }

    private void UnbindManager()
    {
        if (boundManager == null) return;
        boundManager.OnQuestCompleted -= OnQuestStateChanged;
        boundManager.OnQuestClaimed -= OnQuestStateChanged;
        boundManager = null;
    }

    private void OnQuestStateChanged(int questId) => Refresh();

    private void Refresh()
    {
        SetVisible(boundManager != null && boundManager.HasClaimableQuests);
    }

    private void ConfigureGraphics()
    {
        if (claimText != null) claimText.raycastTarget = false;
        if (claimExclamation != null) claimExclamation.raycastTarget = false;
    }

    private void ConfigureText()
    {
        if (claimText != null) claimText.text = ClaimMessage;
        if (claimExclamation != null) claimExclamation.text = "!";
    }

    private void SetVisible(bool visible)
    {
        if (claimText != null) claimText.gameObject.SetActive(visible);
        if (claimExclamation != null) claimExclamation.gameObject.SetActive(visible);
    }
}
