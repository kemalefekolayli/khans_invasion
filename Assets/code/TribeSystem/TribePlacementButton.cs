using UnityEngine;
using UnityEngine.UI;

/// <summary>Temporary UI button for placing a recruited tribe into the open player city.</summary>
public class TribePlacementButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image buttonImage;

    [Header("Display")]
    [SerializeField] private bool useFade = true;
    [SerializeField, Min(0.1f)] private float fadeSpeed = 10f;

    private bool panelOpen;
    private bool subscribed;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (button != null) button.onClick.AddListener(OnClicked);
        SetVisible(false, true);
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceManagementOpened += OnProvinceOpened;
        GameEvents.OnProvinceInteractionOpened += OnProvinceOpened;
        GameEvents.OnProvincePanelClosed += OnProvinceClosed;
        subscribed = true;
    }

    private void OnDisable()
    {
        if (!subscribed) return;
        GameEvents.OnProvinceManagementOpened -= OnProvinceOpened;
        GameEvents.OnProvinceInteractionOpened -= OnProvinceOpened;
        GameEvents.OnProvincePanelClosed -= OnProvinceClosed;
        subscribed = false;
    }

    private void Update()
    {
        bool shouldShow = panelOpen && TribePlacementController.Instance != null && TribePlacementController.Instance.CanPlaceSelectedTribe;
        if (useFade)
        {
            float target = shouldShow ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
            canvasGroup.interactable = shouldShow;
            canvasGroup.blocksRaycasts = shouldShow;
            if (buttonImage != null) buttonImage.raycastTarget = shouldShow;
        }
        else SetVisible(shouldShow, true);
    }

    private void OnProvinceOpened(ProvinceModel province) => panelOpen = true;
    private void OnProvinceClosed() { panelOpen = false; SetVisible(false, true); }
    private void OnClicked() => TribePlacementController.Instance?.PlaceSelectedTribe();

    private void SetVisible(bool visible, bool immediate)
    {
        if (canvasGroup == null) return;
        if (immediate) canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        if (buttonImage != null) buttonImage.raycastTarget = visible;
    }
}
