using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoadingController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField, Min(0f)] private float minimumVisibleSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float readyTimeoutSeconds = 20f;

    [Header("Display")]
    [SerializeField] private CanvasGroup loadingCanvas;

    private bool playerNationReady;
    private bool mapReady;
    private bool loadStarted;
    private bool ownsLoad;
    private static bool loadInProgress;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (loadingCanvas != null)
        {
            loadingCanvas.alpha = 1f;
            loadingCanvas.interactable = true;
            loadingCanvas.blocksRaycasts = true;

            Canvas canvas = loadingCanvas.GetComponent<Canvas>();
            if (canvas == null) canvas = loadingCanvas.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = short.MaxValue;
            }
        }
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerNationReady += OnPlayerNationReady;
        GameEvents.OnMapLoaded += OnMapLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
        GameEvents.OnMapLoaded -= OnMapLoaded;
        if (ownsLoad) loadInProgress = false;
    }

    private void Start()
    {
        if (loadStarted || loadInProgress) return;
        loadStarted = true;
        ownsLoad = true;
        loadInProgress = true;
        StartCoroutine(LoadGameRoutine());
    }

    private void OnPlayerNationReady() => playerNationReady = true;
    private void OnMapLoaded() => mapReady = true;

    private IEnumerator LoadGameRoutine()
    {
        float startedAt = Time.unscaledTime;
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            GameLog.Warning(GameLogCategory.Core, $"[GameLoadingController] Could not begin loading {gameSceneName}.");
            loadInProgress = false;
            ownsLoad = false;
            yield break;
        }

        while (!loadOperation.isDone) yield return null;

        float readyStartedAt = Time.unscaledTime;
        while (!IsGameReady() && Time.unscaledTime - readyStartedAt < readyTimeoutSeconds)
            yield return null;

        if (!IsGameReady())
            GameLog.Warning(GameLogCategory.Core, $"[GameLoadingController] Readiness timed out after {readyTimeoutSeconds:0.#} seconds (map={mapReady}, nation={playerNationReady || PlayerNation.Instance?.currentNation != null}).");

        while (Time.unscaledTime - startedAt < minimumVisibleSeconds) yield return null;

        if (loadingCanvas != null)
        {
            const float fadeSeconds = 0.2f;
            float fadeStartedAt = Time.unscaledTime;
            float startAlpha = loadingCanvas.alpha;
            while (Time.unscaledTime - fadeStartedAt < fadeSeconds)
            {
                loadingCanvas.alpha = Mathf.Lerp(startAlpha, 0f, (Time.unscaledTime - fadeStartedAt) / fadeSeconds);
                yield return null;
            }
            loadingCanvas.alpha = 0f;
            loadingCanvas.interactable = false;
            loadingCanvas.blocksRaycasts = false;
        }

        loadInProgress = false;
        ownsLoad = false;
        Destroy(gameObject);
    }

    private bool IsGameReady()
    {
        bool nationReady = playerNationReady || PlayerNation.Instance?.currentNation != null;
        return mapReady && nationReady && SceneManager.GetActiveScene().name == gameSceneName;
    }
}
