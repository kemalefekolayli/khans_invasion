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
    private bool isPanelOpen = false;
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
        

    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;
        
        // Only need panel open/close events - Update handles city center logic
        GameEvents.OnProvinceManagementOpened += OnPanelOpened;
        GameEvents.OnProvinceInteractionOpened += OnPanelOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        
        isSubscribed = true;

    }

    private void OnDisable()
    {
        if (!isSubscribed) return;
        
        GameEvents.OnProvinceManagementOpened -= OnPanelOpened;
        GameEvents.OnProvinceInteractionOpened -= OnPanelOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
        
        isSubscribed = false;
    }

    private void OnPanelOpened(ProvinceModel province)
    {
        Debug.Log($"[ButtonDisplay] OnPanelOpened -> isPanelOpen=true (was {isPanelOpen})");
        isPanelOpen = true;
    }

    private void OnPanelClosed()
    {
        Debug.Log($"[ButtonDisplay] OnPanelClosed -> isPanelOpen=false (was {isPanelOpen})");
        isPanelOpen = false;
    }

    private bool _lastShouldShow = false;
    
    private void Update()
    {
        // Simple logic: show button if selected general is on a city center AND no panel is open
        var selectionManager = GeneralSelectionManager.Instance;
        if (selectionManager != null && selectionManager.SelectedGeneral != null)
        {
            currentCityCenter = selectionManager.SelectedGeneral.CurrentCityCenter;
            
            // Safety: if general left city center, any open panel should be considered closed
            if (currentCityCenter == null && isPanelOpen)
            {
                Debug.Log("[ButtonDisplay] Safety reset: general left city center, clearing isPanelOpen");
                isPanelOpen = false;
            }
            
            shouldShow = currentCityCenter != null && !isPanelOpen;
        }
        else
        {
            currentCityCenter = null;
            shouldShow = false;
        }
        
        // Log only on state change
        if (shouldShow != _lastShouldShow)
        {
            Debug.Log($"[ButtonDisplay] shouldShow changed: {_lastShouldShow} -> {shouldShow} (CC={currentCityCenter?.Province?.provinceName}, isPanelOpen={isPanelOpen})");
            _lastShouldShow = shouldShow;
        }
        
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