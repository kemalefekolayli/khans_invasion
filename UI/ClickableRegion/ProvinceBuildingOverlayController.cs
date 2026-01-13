using UnityEngine;

public class ProvinceBuildingOverlayController : MonoBehaviour
{
    private ProvinceModel currentProvince;

    [SerializeField] private GameObject overlay_trade;
    [SerializeField] private GameObject overlay_farm;
    [SerializeField] private GameObject overlay_barrack;
    [SerializeField] private GameObject overlay_house;
    [SerializeField] private GameObject overlay_fort;

    private void OnEnable()
    {
        GameEvents.OnProvinceEnter += OnProvinceEnter;
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceEnter -= OnProvinceEnter;
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
    }

    private void OnProvinceEnter(ProvinceModel province)
    {
        if (province == null) return;
        
        currentProvince = province;
        UpdateOverlays();

    }


    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {

        if (province == currentProvince)
            UpdateOverlays();
    }

    private void HideAllOverlays()
    {
        SetActive(overlay_farm, true);
        SetActive(overlay_barrack, true);
        SetActive(overlay_fort, true);
        SetActive(overlay_house, true);
        SetActive(overlay_trade, true);
    }
    private void UpdateOverlays()
    {
        HideAllOverlays();
        
        if (currentProvince == null) return;

        foreach (string building in currentProvince.buildings)
        {
            SetBuildingOverlay(building);
        }
    }
    private void SetBuildingOverlay(string buildingType)
    {
        switch (buildingType)
        {
            case "Farm":
                SetActive(overlay_farm, false);
                break;
            case "Barracks":
                SetActive(overlay_barrack, false);
                break;
            case "Fortress":
                SetActive(overlay_fort, false);
                break;
            case "Housing":
                SetActive(overlay_house, false);
                break;
            case "Trade_Building":
                SetActive(overlay_trade, false);
                break;
        }
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
}