using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the C button prefab's Button GameObject.
/// Handles both mouse click and keyboard C press to open province interface.
/// </summary>
public class InteractionButtonController : MonoBehaviour
{
    [Header("References")]
    public Button button;
    
    [Header("Input Settings")]
    public Key interactionKey = Key.C;
    
    private CityCenter currentCityCenter;
    private bool isActive = false;
    private bool isSubscribed = false;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
        
        Debug.Log($"[ButtonController] Init - Button: {button != null}");
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        
        // Check if Horse is already on a city center (fixes race condition)
        TryGetCurrentCityCenter();
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;
        
        GameEvents.OnCityCenterEnter += OnCityCenterEnter;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        
        isSubscribed = true;
        Debug.Log("[ButtonController] Subscribed to events");
    }
    
    private void TryGetCurrentCityCenter()
    {
        // Find the Horse and check if it's on a city center
        Horse horse = FindFirstObjectByType<Horse>();
        if (horse != null && horse.CurrentCityCenter != null)
        {
            currentCityCenter = horse.CurrentCityCenter;
            isActive = true;
            Debug.Log($"[ButtonController] Found existing city center: {currentCityCenter.Province?.provinceName}");
        }
    }

    private void OnDisable()
    {
        if (!isSubscribed) return;
        
        GameEvents.OnCityCenterEnter -= OnCityCenterEnter;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
        
        isSubscribed = false;
    }

    private void Update()
    {
        if (!isActive) return;
        if (Keyboard.current == null) return;
        
        if (Keyboard.current[interactionKey].wasPressedThisFrame)
        {
            Debug.Log("[ButtonController] C key pressed");
            TriggerInteraction();
        }
    }

    private void OnCityCenterEnter(CityCenter cityCenter)
    {
        Debug.Log($"[ButtonController] CityCenter ENTER -> Active");
        currentCityCenter = cityCenter;
        isActive = true;
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        if (currentCityCenter == cityCenter)
        {
            Debug.Log($"[ButtonController] CityCenter EXIT -> Inactive");
            currentCityCenter = null;
            isActive = false;
        }
    }

    private void OnPanelClosed()
    {
        if (currentCityCenter != null)
            isActive = true;
    }

    private void OnButtonClicked()
    {
        TriggerInteraction();
    }

    private void TriggerInteraction()
    {
        if (currentCityCenter == null || currentCityCenter.Province == null)
        {
            Debug.LogWarning("[ButtonController] No valid city center!");
            return;
        }
        
        isActive = false;
        ProvinceModel province = currentCityCenter.Province;
        
        if (currentCityCenter.IsOwnedByPlayer())
        {
            Debug.Log($"[ButtonController] Opening MANAGEMENT for {province.provinceName}");
            GameEvents.ProvinceManagementOpened(province);
        }
        else
        {
            Debug.Log($"[ButtonController] Opening INTERACTION for {province.provinceName}");
            GameEvents.ProvinceInteractionOpened(province);
        }
    }
}