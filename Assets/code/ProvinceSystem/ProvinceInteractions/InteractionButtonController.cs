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
        
        // Fallback to the Khan if no selected general
        SelectableGeneral khan = SelectableGeneral.FindKhan();
        if (khan != null && khan.CurrentCityCenter != null)
        {
            currentCityCenter = khan.CurrentCityCenter;
            isActive = true;
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] Found existing city center from Khan: {currentCityCenter.Province?.provinceName}");
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
            GameLog.Log(GameLogCategory.Core, "[ButtonController] C key pressed");
            TriggerInteraction();
        }
    }

    private void OnCityCenterEnter(CityCenter cityCenter)
    {
        GameLog.Log(GameLogCategory.Core, $"[ButtonController] OnCityCenterEnter called. cityCenter={cityCenter?.Province?.provinceName}, isActive={isActive}");
        
        // Only respond if this is the selected general's city center
        GeneralSelectionManager selectionManager = GeneralSelectionManager.Instance;
        if (selectionManager != null && selectionManager.SelectedGeneral != null)
        {
            var selectedCC = selectionManager.SelectedGeneral.CurrentCityCenter;
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] Filter check: SelectedGeneral.CurrentCityCenter={selectedCC?.Province?.provinceName}, incoming={cityCenter?.Province?.provinceName}, match={selectedCC == cityCenter}");
            if (selectedCC != cityCenter)
            {
                GameLog.Log(GameLogCategory.Core, $"[ButtonController] REJECTED - not from selected general");
                return;
            }
        }
        else
        {
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] No selection manager or no selected general - accepting event");
        }
        
        GameLog.Log(GameLogCategory.Core, $"[ButtonController] CityCenter ENTER -> isActive=true");
        currentCityCenter = cityCenter;
        isActive = true;
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        GameLog.Log(GameLogCategory.Core, $"[ButtonController] OnCityCenterExit called. exitingCC={cityCenter?.Province?.provinceName}, currentCC={currentCityCenter?.Province?.provinceName}, isActive={isActive}");
        
        // Only respond if this affects the selected general
        GeneralSelectionManager selectionManager = GeneralSelectionManager.Instance;
        if (selectionManager != null && selectionManager.SelectedGeneral != null)
        {
            var selectedCC = selectionManager.SelectedGeneral.CurrentCityCenter;
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] EXIT filter: SelectedGeneral.CurrentCityCenter={selectedCC?.Province?.provinceName}");
            if (selectedCC != null)
            {
                GameLog.Log(GameLogCategory.Core, $"[ButtonController] EXIT SKIPPED - selected general still on a city center");
                return;
            }
        }
        
        if (currentCityCenter == cityCenter)
        {
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] CityCenter EXIT -> isActive=false");
            currentCityCenter = null;
            isActive = false;
        }
        else
        {
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] EXIT ignored - different city center");
        }
    }

    private void OnPanelClosed()
    {
        GameLog.Log(GameLogCategory.Core, $"[ButtonController] OnPanelClosed called. isActive={isActive}, currentCC={currentCityCenter?.Province?.provinceName}");
        TryGetCurrentCityCenter();
        GameLog.Log(GameLogCategory.Core, $"[ButtonController] After TryGetCurrentCityCenter: isActive={isActive}, currentCC={currentCityCenter?.Province?.provinceName}");
    }

    private void OnButtonClicked()
    {
        TriggerInteraction();
    }

    private void TriggerInteraction()
    {
        GameLog.Log(GameLogCategory.Core, $"[ButtonController] TriggerInteraction called. isActive={isActive}, currentCC={currentCityCenter?.Province?.provinceName}");
        if (currentCityCenter == null || currentCityCenter.Province == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[ButtonController] No valid city center!");
            return;
        }
        
        isActive = false;
        GameLog.Log(GameLogCategory.Core, $"[ButtonController] TriggerInteraction -> isActive=false");
        ProvinceModel province = currentCityCenter.Province;
        
        if (currentCityCenter.IsOwnedByPlayer())
        {
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] Opening MANAGEMENT for {province.provinceName}");
            GameEvents.ProvinceManagementOpened(province);
        }
        else
        {
            GameLog.Log(GameLogCategory.Core, $"[ButtonController] Opening INTERACTION for {province.provinceName}");
            GameEvents.ProvinceInteractionOpened(province);
        }
    }
}