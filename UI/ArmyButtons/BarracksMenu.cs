using UnityEngine;

public class BarracksMenu : MonoBehaviour
{
    [SerializeField] private GameObject barracksMenuUI;

    private ProvinceModel currentProvince;
    void Start()
    {
        SetActive(barracksMenuUI, false);
    }
    void OnEnable()
    {
        GameEvents.OnBarrackMenuOpened += OpenBarracksMenu;
        GameEvents.OnCityCenterExit += CloseBarracksMenu;
        GameEvents.OnProvinceEnter += (ProvinceModel province) => currentProvince = province;
    }

    void OnDisable()
    {
        GameEvents.OnBarrackMenuOpened -= OpenBarracksMenu;
        GameEvents.OnCityCenterExit -= CloseBarracksMenu;
        GameEvents.OnProvinceEnter -= (ProvinceModel province) => currentProvince = province;
    }

    private void OpenBarracksMenu()
    {
        Debug.Log("Barracks Menu Opened 123");
        SetActive(barracksMenuUI, true);
    }

    private void CloseBarracksMenu(CityCenter cityCenter)
    {
        SetActive(barracksMenuUI, false);
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