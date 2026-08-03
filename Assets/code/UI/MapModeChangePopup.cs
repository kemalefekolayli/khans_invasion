using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays a brief centre-screen confirmation whenever the map mode changes.</summary>
public class MapModeChangePopup : MonoBehaviour
{
    private const float Lifetime = 1f;
    private const float RiseDistance = 45f;
    private TextMeshProUGUI popupText;
    private RectTransform popupRect;
    private Coroutine animationRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        new GameObject("MapModeChangePopupBootstrap").AddComponent<MapModeChangePopupBootstrap>();
    }

    private void Awake() => CreatePopup();

    private void OnEnable()
    {
        if (MapModeController.Instance != null)
            MapModeController.Instance.OnMapModeChanged += Show;
    }

    private void OnDisable()
    {
        if (MapModeController.Instance != null)
            MapModeController.Instance.OnMapModeChanged -= Show;
    }

    private void CreatePopup()
    {
        GameObject canvasObject = new GameObject("MapModeChangeCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20001;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("MapModeChangeText");
        textObject.transform.SetParent(canvasObject.transform, false);
        popupRect = textObject.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(1200f, 120f);

        popupText = textObject.AddComponent<TextMeshProUGUI>();
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.fontSize = 44f;
        popupText.fontStyle = FontStyles.Bold;
        popupText.color = new Color(1f, 0.78f, 0.16f, 1f);
        popupText.outlineColor = Color.black;
        popupText.outlineWidth = 0.22f;
        popupText.raycastTarget = false;
        popupText.enableWordWrapping = false;
        GameFontManager.Apply(popupText);
        popupText.gameObject.SetActive(false);
    }

    private void Show(ProvinceMapMode mode)
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        popupText.text = $"Map Mode: {GetModeLabel(mode)}";
        popupText.gameObject.SetActive(true);
        animationRoutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Color color = popupText.color;
        float elapsed = 0f;
        while (elapsed < Lifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / Lifetime);
            popupRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-RiseDistance * 0.5f, RiseDistance * 0.5f, progress));
            color.a = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0.65f, 1f, progress));
            popupText.color = color;
            yield return null;
        }

        popupText.gameObject.SetActive(false);
        animationRoutine = null;
    }

    private static string GetModeLabel(ProvinceMapMode mode)
    {
        return mode == ProvinceMapMode.PopulationDensity ? "Population" : mode.ToString();
    }
}

internal class MapModeChangePopupBootstrap : MonoBehaviour
{
    private void Update()
    {
        MapModeController controller = FindFirstObjectByType<MapModeController>();
        if (controller == null) return;

        if (controller.GetComponent<MapModeChangePopup>() == null)
            controller.gameObject.AddComponent<MapModeChangePopup>();

        Destroy(gameObject);
    }
}