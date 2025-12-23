using UnityEngine;
using UnityEngine.UI;

public class AllyProvinceButton : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private GameObject panel;
    
    private ProvinceModel currentProvince;

    private void Awake()
    {
        // buttonlara listener ekle
        
        HideButtons();
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceManagementOpened += ShowButtons;
        GameEvents.OnProvincePanelClosed += HideButtons;
        GameEvents.OnCityCenterExit += OnCityCenterExit;  // Yeni eklendi
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceManagementOpened -= ShowButtons;
        GameEvents.OnProvincePanelClosed -= HideButtons;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;  // Yeni eklendi
    }



    private void ShowButtons(ProvinceModel province)
    {
        currentProvince = province;
        
        if (panel != null)
            panel.SetActive(true);
        

    }

    private void HideButtons()
    {
        currentProvince = null;
        
        if (panel != null)
            panel.SetActive(false);

    }
    private void OnCityCenterExit(CityCenter cityCenter)
    {
        // At city center'dan çıkınca butonları kapat
        if (currentProvince != null)
        {
            HideButtons();
            GameEvents.ProvincePanelClosed();
        }
    }
}