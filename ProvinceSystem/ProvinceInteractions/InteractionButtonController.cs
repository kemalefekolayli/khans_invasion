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

    }
    
    private void TryGetCurrentCityCenter()
    {
        // First try the currently selected general
        GeneralSelectionManager selectionManager = GeneralSelectionManager.Instance;
        if (selectionManager != null && selectionManager.SelectedGeneral != null)
        {
            SelectableGeneral selectedGeneral = selectionManager.SelectedGeneral;
            if (selectedGeneral.CurrentCityCenter != null)
            {
                currentCityCenter = selectedGeneral.CurrentCityCenter;
                isActive = true;

                return;
            }
        }
        
        // Fallback to Horse if no selected general
        Horse horse = FindFirstObjectByType<Horse>();
        if (horse != null && horse.CurrentCityCenter != null)
        {
            currentCityCenter = horse.CurrentCityCenter;
            isActive = true;
            Debug.Log($"[ButtonController] Found existing city center from Horse: {currentCityCenter.Province?.provinceName}");
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
        Debug.Log($"[ButtonController] OnCityCenterEnter called. cityCenter={cityCenter?.Province?.provinceName}, isActive={isActive}");
        
        // Only respond if this is the selected general's city center
        GeneralSelectionManager selectionManager = GeneralSelectionManager.Instance;
        if (selectionManager != null && selectionManager.SelectedGeneral != null)
        {
            var selectedCC = selectionManager.SelectedGeneral.CurrentCityCenter;
            Debug.Log($"[ButtonController] Filter check: SelectedGeneral.CurrentCityCenter={selectedCC?.Province?.provinceName}, incoming={cityCenter?.Province?.provinceName}, match={selectedCC == cityCenter}");
            if (selectedCC != cityCenter)
            {
                Debug.Log($"[ButtonController] REJECTED - not from selected general");
                return;
            }
        }
        else
        {
            Debug.Log($"[ButtonController] No selection manager or no selected general - accepting event");
        }
        
        Debug.Log($"[ButtonController] CityCenter ENTER -> isActive=true");
        currentCityCenter = cityCenter;
        isActive = true;
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        Debug.Log($"[ButtonController] OnCityCenterExit called. exitingCC={cityCenter?.Province?.provinceName}, currentCC={currentCityCenter?.Province?.provinceName}, isActive={isActive}");
        
        // Only respond if this affects the selected general
        GeneralSelectionManager selectionManager = GeneralSelectionManager.Instance;
        if (selectionManager != null && selectionManager.SelectedGeneral != null)
        {
            var selectedCC = selectionManager.SelectedGeneral.CurrentCityCenter;
            Debug.Log($"[ButtonController] EXIT filter: SelectedGeneral.CurrentCityCenter={selectedCC?.Province?.provinceName}");
            if (selectedCC != null)
            {
                Debug.Log($"[ButtonController] EXIT SKIPPED - selected general still on a city center");
                return;
            }
        }
        
        if (currentCityCenter == cityCenter)
        {
            Debug.Log($"[ButtonController] CityCenter EXIT -> isActive=false");
            currentCityCenter = null;
            isActive = false;
        }
        else
        {
            Debug.Log($"[ButtonController] EXIT ignored - different city center");
        }
    }

    private void OnPanelClosed()
    {
        Debug.Log($"[ButtonController] OnPanelClosed called. isActive={isActive}, currentCC={currentCityCenter?.Province?.provinceName}");
        TryGetCurrentCityCenter();
        Debug.Log($"[ButtonController] After TryGetCurrentCityCenter: isActive={isActive}, currentCC={currentCityCenter?.Province?.provinceName}");
    }

    private void OnButtonClicked()
    {
        TriggerInteraction();
    }

    private void TriggerInteraction()
    {
        Debug.Log($"[ButtonController] TriggerInteraction called. isActive={isActive}, currentCC={currentCityCenter?.Province?.provinceName}");
        if (currentCityCenter == null || currentCityCenter.Province == null)
        {
            Debug.LogWarning("[ButtonController] No valid city center!");
            return;
        }
        
        isActive = false;
        Debug.Log($"[ButtonController] TriggerInteraction -> isActive=false");
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