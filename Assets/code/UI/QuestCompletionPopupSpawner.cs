using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a non-blocking toast banner when a quest completes or its reward is claimed.
/// Builds a self-contained screen-space Canvas in code (no prefab needed), pools banners
/// via ComponentPool (T0.9) to avoid GC churn, and queues messages so multiple quests
/// completing in the same frame are shown sequentially.
/// Hooks QuestManager's existing C# events; no new GameEvents.
/// </summary>
public class QuestCompletionPopupSpawner : MonoBehaviour
{
    public static QuestCompletionPopupSpawner Instance { get; private set; }

    [Header("Appearance")]
    [SerializeField] private Color textColor = new Color(1f, 0.84f, 0.2f, 1f);
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private Color backgroundColor = new Color(0.04f, 0.04f, 0.12f, 0.82f);
    [SerializeField] private float fontSize = 40f;
    [SerializeField] private float outlineWidth = 0.25f;

    [Header("Canvas")]
    [SerializeField] private int sortingOrder = 20000;
    [SerializeField] private Vector2 bannerAnchorPosition = new Vector2(0f, -80f);
    [SerializeField] private Vector2 bannerSize = new Vector2(1700f, 220f);

    [Header("Pool")]
    [SerializeField] private int poolSize = 2;

    private Canvas canvas;
    private ComponentPool<QuestCompletionBanner> pool;
    private readonly Queue<string> pendingMessages = new Queue<string>();
    private bool showing;
    private Sprite whiteSprite;
    private QuestManager boundManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (Instance != null || FindFirstObjectByType<QuestCompletionPopupSpawner>() != null) return;

        GameObject obj = new GameObject("QuestCompletionPopupSpawner");
        obj.AddComponent<QuestCompletionPopupSpawner>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCanvas();
        pool = new ComponentPool<QuestCompletionBanner>("QuestCompletionBannerPool", canvas.transform, poolSize, CreateBanner);
    }

    private void OnEnable()
    {
        TryBindManager();
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    private void Update()
    {
        if (boundManager == QuestManager.Instance) return;
        UnbindManager();
        TryBindManager();
    }

    private void OnDestroy()
    {
        UnbindManager();
        if (Instance == this) Instance = null;
    }

    private void TryBindManager()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null || manager == boundManager) return;
        boundManager = manager;
        boundManager.OnQuestCompleted += OnQuestCompleted;
        boundManager.OnQuestClaimed += OnQuestClaimed;
    }

    private void UnbindManager()
    {
        if (boundManager == null) return;
        boundManager.OnQuestCompleted -= OnQuestCompleted;
        boundManager.OnQuestClaimed -= OnQuestClaimed;
        boundManager = null;
    }

    private void OnQuestCompleted(int questId)
    {
        QuestData quest = GetQuest(questId);
        if (quest == null) return;

        Enqueue($"Quest complete - claim your prize! {quest.questTitle}");
    }

    private void OnQuestClaimed(int questId)
    {
        QuestData quest = GetQuest(questId);
        string rewardDescription = QuestManager.Instance?.GetCurrentRewardDescription(questId) ?? quest?.rewardDescription;
        if (quest == null || string.IsNullOrEmpty(rewardDescription)) return;

        Enqueue($"Reward Claimed! {rewardDescription}");
    }

    private QuestData GetQuest(int questId)
    {
        return QuestManager.Instance != null ? QuestManager.Instance.GetQuestById(questId) : null;
    }

    private void Enqueue(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (showing)
        {
            pendingMessages.Enqueue(message);
        }
        else
        {
            showing = true;
            ShowNext(message);
        }
    }

    private void ShowNext(string message)
    {
        QuestCompletionBanner banner = pool.Get();
        if (banner == null)
        {
            showing = false;
            GameLog.Warning(GameLogCategory.Quest, "[QuestCompletionPopupSpawner] Banner pool exhausted, message dropped");
            return;
        }

        banner.BindPool(pool, this);
        banner.Initialize(message);
        GameLog.Log(GameLogCategory.Quest, $"[QuestCompletionPopupSpawner] Banner: {message}");
    }

    public void OnBannerFinished(QuestCompletionBanner banner)
    {
        if (pendingMessages.Count > 0)
        {
            ShowNext(pendingMessages.Dequeue());
        }
        else
        {
            showing = false;
        }
    }

    private QuestCompletionBanner CreateBanner(Transform parent)
    {
        GameObject bannerObj = new GameObject("QuestCompletionBanner");
        bannerObj.transform.SetParent(parent, false);

        RectTransform rect = bannerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = bannerAnchorPosition;
        rect.sizeDelta = bannerSize;

        CanvasGroup group = bannerObj.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        Image background = bannerObj.AddComponent<Image>();
        background.sprite = GetWhiteSprite();
        background.color = backgroundColor;
        background.raycastTarget = false;

        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(bannerObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(40f, 16f);
        textRect.offsetMax = new Vector2(-40f, -16f);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = textColor;
        text.outlineColor = outlineColor;
        text.outlineWidth = outlineWidth;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        GameFontManager.Apply(text);

        return bannerObj.AddComponent<QuestCompletionBanner>();
    }

    private void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("QuestCompletionCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
}
