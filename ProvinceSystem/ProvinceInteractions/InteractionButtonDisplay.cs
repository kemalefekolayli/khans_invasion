using UnityEngine;

/// <summary>
/// Attach to the C button prefab root (the Canvas).
/// Controls visibility of the interaction button based on city center presence.
/// </summary>
public class InteractionButtonDisplay : MonoBehaviour
{
    [Header("References")]
    public GameObject buttonContainer;
    public CanvasGroup canvasGroup;
    
    [Header("Animation")]
    public float fadeSpeed = 10f;
    public bool useFade = true;
    
    [Header("Position Settings")]
    public Vector2 screenOffset = new Vector2(0, -200f);
    public bool followCityCenter = false;
    
    private bool shouldShow = false;
    private CityCenter currentCityCenter;
    private Camera mainCamera;
    private RectTransform rectTransform;
    private bool isSubscribed = false;

    private void Awake()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        
        if (buttonContainer == null && transform.childCount > 0)
            buttonContainer = transform.GetChild(0)?.gameObject;
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        SetVisibility(false, true);
        
        Debug.Log($"[ButtonDisplay] Init - Container: {buttonContainer != null}, CanvasGroup: {canvasGroup != null}");
    }

    private void Start()
    {
        // Subscribe in Start to ensure GameEvents is ready
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        // Also try OnEnable in case object was disabled/enabled
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;
        
        GameEvents.OnCityCenterEnter += OnCityCenterEnter;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
        GameEvents.OnProvinceManagementOpened += OnPanelOpened;
        GameEvents.OnProvinceInteractionOpened += OnPanelOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        
        isSubscribed = true;
        Debug.Log("[ButtonDisplay] Subscribed to events");
    }

    private void OnDisable()
    {
        if (!isSubscribed) return;
        
        GameEvents.OnCityCenterEnter -= OnCityCenterEnter;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
        GameEvents.OnProvinceManagementOpened -= OnPanelOpened;
        GameEvents.OnProvinceInteractionOpened -= OnPanelOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
        
        isSubscribed = false;
    }

    private void OnCityCenterEnter(CityCenter cityCenter)
    {
        Debug.Log($"[ButtonDisplay] CityCenter ENTER -> Show button");
        currentCityCenter = cityCenter;
        shouldShow = true;
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        if (currentCityCenter == cityCenter)
        {
            Debug.Log($"[ButtonDisplay] CityCenter EXIT -> Hide button");
            currentCityCenter = null;
            shouldShow = false;
        }
    }

    private void OnPanelOpened(ProvinceModel province)
    {
        shouldShow = false;
    }

    private void OnPanelClosed()
    {
        if (currentCityCenter != null)
            shouldShow = true;
    }

    private void Update()
    {
        UpdateVisibility();
        UpdatePosition();
    }

    private void UpdateVisibility()
    {
        if (canvasGroup == null) return;
        
        if (!useFade)
        {
            SetVisibility(shouldShow, true);
            return;
        }
        
        float targetAlpha = shouldShow ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        canvasGroup.interactable = shouldShow;
        canvasGroup.blocksRaycasts = shouldShow;
        
        if (buttonContainer != null)
            buttonContainer.SetActive(canvasGroup.alpha > 0.01f);
    }

    private void SetVisibility(bool visible, bool immediate)
    {
        if (canvasGroup != null)
        {
            if (immediate)
                canvasGroup.alpha = visible ? 1f : 0f;
            
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        
        if (buttonContainer != null)
            buttonContainer.SetActive(visible);
    }

    private void UpdatePosition()
    {
        if (!followCityCenter || currentCityCenter == null || mainCamera == null)
        {
            if (rectTransform != null)
                rectTransform.anchoredPosition = screenOffset;
            return;
        }
        
        Vector3 worldPos = currentCityCenter.transform.position;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        if (rectTransform != null)
            rectTransform.position = screenPos + (Vector3)screenOffset;
    }
    
    [ContextMenu("Force Show")]
    public void DebugForceShow()
    {
        shouldShow = true;
        SetVisibility(true, true);
        Debug.Log("[ButtonDisplay] FORCED VISIBLE");
    }
}