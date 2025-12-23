using UnityEngine;
using UnityEngine.UI;

public class EnemyProvinceButtons : MonoBehaviour
{
    [Header("Buttons")]
    public Button raidButton;
    public Button siegeButton;
    
    private ProvinceModel currentProvince;

    private void Awake()
    {
        // Butonlara listener ekle
        if (raidButton != null)
            raidButton.onClick.AddListener(OnRaidClicked);
        
        if (siegeButton != null)
            siegeButton.onClick.AddListener(OnSiegeClicked);
        
        // Başlangıçta gizle
        HideButtons();
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceInteractionOpened += ShowButtons;
        GameEvents.OnProvincePanelClosed += HideButtons;
        GameEvents.OnCityCenterExit += OnCityCenterExit;  // Yeni eklendi
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceInteractionOpened -= ShowButtons;
        GameEvents.OnProvincePanelClosed -= HideButtons;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;  // Yeni eklendi
    }

    private void ShowButtons(ProvinceModel province)
    {
        currentProvince = province;
        
        if (raidButton != null)
            raidButton.gameObject.SetActive(true);
        
        if (siegeButton != null)
            siegeButton.gameObject.SetActive(true);
        
        Debug.Log($"[EnemyButtons] Showing Raid/Siege for {province.provinceName}");
    }

    private void HideButtons()
    {
        currentProvince = null;
        
        if (raidButton != null)
            raidButton.gameObject.SetActive(false);
        
        if (siegeButton != null)
            siegeButton.gameObject.SetActive(false);
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        // At city center'dan çıkınca butonları kapat
        if (currentProvince != null)
        {
            Debug.Log($"[EnemyButtons] Horse left city center, closing buttons");
            HideButtons();
            GameEvents.ProvincePanelClosed();
        }
    }

    private void OnRaidClicked()
    {
        if (currentProvince == null) return;
        
        Debug.Log($"[EnemyButtons] RAID clicked on {currentProvince.provinceName}");
        // TODO: Raid logic buraya
        
        HideButtons();
        GameEvents.ProvincePanelClosed();
    }

    private void OnSiegeClicked()
    {
        if (currentProvince == null) return;
        
        Debug.Log($"[EnemyButtons] SIEGE clicked on {currentProvince.provinceName}");
        // TODO: Siege logic buraya
        
        HideButtons();
        GameEvents.ProvincePanelClosed();
    }
}