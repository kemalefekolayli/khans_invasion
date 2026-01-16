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
    [Header("Barrack Menu")]
    [SerializeField] private GameObject barrackButton;


    private ProvinceModel currentProvince;

    private void OnEnable()
    {

        GameEvents.OnProvinceManagementOpened += OnProvinceOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
        GameEvents.OnBarrackMenuOpened += OnBarrackMenuOpened;
    }

    private void OnDisable()
    {

        GameEvents.OnProvinceManagementOpened -= OnProvinceOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
        GameEvents.OnBarrackMenuOpened -= OnBarrackMenuOpened;
    }

    private void Start()
    {
        HideAllOverlays();
    }

    private void OnProvinceOpened(ProvinceModel province)
    {
        currentProvince = province;
        UpdateOverlays();
    }

    private void OnPanelClosed()
    {
        currentProvince = null;
        HideAllOverlays();
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        currentProvince = null;
        HideAllOverlays();
    }

    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {

        if (province == currentProvince)
            UpdateOverlays();
    }

    private void UpdateOverlays()
    {
        HideAllOverlays();
        
        if (currentProvince == null) return;

        
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
                SetActive(barrackButton, active);
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
        SetActive(barrackButton, false);
    }

    private void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);

        }
        else
        {
            Debug.LogWarning("[BuilderOverlay] GameObject reference is NULL!");
        }
    }

    private void OnBarrackMenuOpened()
    {
        // When Barrack Menu is opened, hide all overlays
        HideAllOverlays();
    }
}