using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CenterWarningPopupSpawner : MonoBehaviour
{
    public static CenterWarningPopupSpawner Instance { get; private set; }

    [Header("Appearance")]
    [SerializeField] private Color warningColor = new Color(1f, 0.12f, 0.08f, 1f);
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float fontSize = 44f;
    [SerializeField] private float outlineWidth = 0.25f;

    [Header("Timing")]
    [SerializeField] private float lifetime = 1.45f;
    [SerializeField] private float cooldownSeconds = 2f;

    private Canvas canvas;
    private TextMeshProUGUI warningText;
    private Coroutine activeRoutine;
    private float nextAllowedTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (Instance != null || FindFirstObjectByType<CenterWarningPopupSpawner>() != null) return;

        GameObject obj = new GameObject("CenterWarningPopupSpawner");
        obj.AddComponent<CenterWarningPopupSpawner>();
    }

    public static void Show(string message)
    {
        if (Instance == null || string.IsNullOrEmpty(message)) return;
        Instance.ShowInternal(message);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildCanvas();
    }

    private void ShowInternal(string message)
    {
        if (Time.unscaledTime < nextAllowedTime) return;

        nextAllowedTime = Time.unscaledTime + cooldownSeconds;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        warningText.text = message;
        warningText.gameObject.SetActive(true);
        activeRoutine = StartCoroutine(AnimateWarning());
    }

    private void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("CenterWarningCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("CenterWarningText");
        textObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 105f);
        rect.sizeDelta = new Vector2(1100f, 160f);

        warningText = textObj.AddComponent<TextMeshProUGUI>();
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.fontSize = fontSize;
        warningText.fontStyle = FontStyles.Bold;
        warningText.color = warningColor;
        warningText.outlineColor = outlineColor;
        warningText.outlineWidth = outlineWidth;
        warningText.raycastTarget = false;
        warningText.enableWordWrapping = false;
        warningText.overflowMode = TextOverflowModes.Overflow;
        GameFontManager.Apply(warningText);

        warningText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateWarning()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.92f;
        Vector3 endScale = Vector3.one;

        while (elapsed < lifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / lifetime);
            float alpha = progress < 0.72f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.72f) / 0.28f);

            Color color = warningColor;
            color.a = alpha;
            warningText.color = color;
            warningText.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, progress));

            yield return null;
        }

        warningText.gameObject.SetActive(false);
        activeRoutine = null;
    }
}
