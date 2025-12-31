using UnityEngine;

public class BuilderOverlayController : MonoBehaviour 
{
    [Header("Building Overlays")]
    [SerializeField] private GameObject overlay_farm;
    [SerializeField] private GameObject overlay_barrack;
    [SerializeField] private GameObject overlay_barrack2;
    [SerializeField] private GameObject overlay_fort;
    [SerializeField] private GameObject overlay_fort2;
    [SerializeField] private GameObject overlay_house;
    [SerializeField] private GameObject overlay_house2;
    [SerializeField] private GameObject overlay_trade;

    private ProvinceModel currentProvince;

    private void Awake()
    {
        Debug.Log("[BuilderOverlay] Awake called");
    }

    private void OnEnable()
    {
        Debug.Log("[BuilderOverlay] OnEnable - subscribing to events");
        GameEvents.OnProvinceManagementOpened += OnProvinceOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
    }

    private void OnDisable()
    {
        Debug.Log("[BuilderOverlay] OnDisable - unsubscribing");
        GameEvents.OnProvinceManagementOpened -= OnProvinceOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
    }

    private void Start()
    {
        Debug.Log($"[BuilderOverlay] Start - overlay_farm assigned: {overlay_farm != null}");
        HideAllOverlays();
    }

    private void OnProvinceOpened(ProvinceModel province)
    {
        Debug.Log($"[BuilderOverlay] Province opened: {province.provinceName}, buildings: {province.buildings.Count}");
        currentProvince = province;
        UpdateOverlays();
    }

    private void OnPanelClosed()
    {
        Debug.Log("[BuilderOverlay] Panel closed");
        currentProvince = null;
        HideAllOverlays();
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        Debug.Log("[BuilderOverlay] CityCenter exit");
        currentProvince = null;
        HideAllOverlays();
    }

    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {
        Debug.Log($"[BuilderOverlay] Building constructed: {buildingType}, is current: {province == currentProvince}");
        if (province == currentProvince)
            UpdateOverlays();
    }

    private void UpdateOverlays()
    {
        HideAllOverlays();
        
        if (currentProvince == null) return;

        Debug.Log($"[BuilderOverlay] Updating overlays for {currentProvince.buildings.Count} buildings");
        
        foreach (string building in currentProvince.buildings)
        {
            Debug.Log($"[BuilderOverlay] Activating overlay for: {building}");
            SetBuildingOverlay(building, true);
        }
    }

    private void SetBuildingOverlay(string buildingType, bool active)
    {
        switch (buildingType)
        {
            case "Farm":
                SetActive(overlay_farm, active);
                break;
            case "Barracks":
                SetActive(overlay_barrack, active);
                SetActive(overlay_barrack2, active);
                break;
            case "Fortress":
                SetActive(overlay_fort, active);
                SetActive(overlay_fort2, active);
                break;
            case "Housing":
                SetActive(overlay_house, active);
                SetActive(overlay_house2, active);
                break;
            case "Trade_Building":
                SetActive(overlay_trade, active);
                break;
        }
    }

    private void HideAllOverlays()
    {
        SetActive(overlay_farm, false);
        SetActive(overlay_barrack, false);
        SetActive(overlay_barrack2, false);
        SetActive(overlay_fort, false);
        SetActive(overlay_fort2, false);
        SetActive(overlay_house, false);
        SetActive(overlay_house2, false);
        SetActive(overlay_trade, false);
    }

    private void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
            Debug.Log($"[BuilderOverlay] Set {obj.name} active: {active}");
        }
        else
        {
            Debug.LogWarning("[BuilderOverlay] GameObject reference is NULL!");
        }
    }
}