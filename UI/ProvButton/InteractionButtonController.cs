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

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
        
        Debug.Log($"[ButtonController] Init - Button: {button != null}");
    }

    private void OnEnable()
    {
        GameEvents.OnCityCenterEnter += OnCityCenterEnter;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnCityCenterEnter -= OnCityCenterEnter;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
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
        Debug.Log("[ButtonController] Button CLICKED");
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