using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI Panel for managing player-owned provinces.
/// Handles building construction and province info display.
/// </summary>
public class ProvinceManagementPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;
    
    [Header("Province Info")]
    public TextMeshProUGUI provinceNameText;
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI incomeText;
    public TextMeshProUGUI buildingsListText;
    
    [Header("Building Buttons")]
    public Button buildFortressBtn;
    public Button buildFarmBtn;
    public Button buildBarracksBtn;
    public Button buildTradeBuildingBtn;
    public Button buildHousingBtn;
    
    [Header("Button Cost Labels")]
    public TextMeshProUGUI fortressCostText;
    public TextMeshProUGUI farmCostText;
    public TextMeshProUGUI barracksCostText;
    public TextMeshProUGUI tradeBuildingCostText;
    public TextMeshProUGUI housingCostText;
    
    [Header("Close Button")]
    public Button closeButton;
    
    [Header("Animation")]
    public float fadeSpeed = 8f;
    
    private ProvinceModel currentProvince;
    private Builder builder;
    private PlayerNation playerNation;
    private bool isVisible = false;

    private void Awake()
    {
        builder = new Builder();
        
        if (panelRoot != null)
            panelRoot.SetActive(false);
        
        if (canvasGroup == null && panelRoot != null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        SetupButtons();
        UpdateCostLabels();
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceManagementOpened += OnManagementOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
        GameEvents.OnPlayerStatsChanged += RefreshButtonStates;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceManagementOpened -= OnManagementOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
        GameEvents.OnPlayerStatsChanged -= RefreshButtonStates;
    }

    private void SetupButtons()
    {
        if (buildFortressBtn != null)
            buildFortressBtn.onClick.AddListener(() => TryBuild("Fortress"));
        
        if (buildFarmBtn != null)
            buildFarmBtn.onClick.AddListener(() => TryBuild("Farm"));
        
        if (buildBarracksBtn != null)
            buildBarracksBtn.onClick.AddListener(() => TryBuild("Barracks"));
        
        if (buildTradeBuildingBtn != null)
            buildTradeBuildingBtn.onClick.AddListener(() => TryBuild("Trade_Building"));
        
        if (buildHousingBtn != null)
            buildHousingBtn.onClick.AddListener(() => TryBuild("Housing"));
        
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void UpdateCostLabels()
    {
        var costs = Builder.GetAllBuildingCosts();
        
        if (fortressCostText != null)
            fortressCostText.text = $"{costs["Fortress"]}g";
        
        if (farmCostText != null)
            farmCostText.text = $"{costs["Farm"]}g";
        
        if (barracksCostText != null)
            barracksCostText.text = $"{costs["Barracks"]}g";
        
        if (tradeBuildingCostText != null)
            tradeBuildingCostText.text = $"{costs["Trade_Building"]}g";
        
        if (housingCostText != null)
            housingCostText.text = $"{costs["Housing"]}g";
    }

    private void OnManagementOpened(ProvinceModel province)
    {
        currentProvince = province;
        playerNation = PlayerNation.Instance;
        
        ShowPanel();
        UpdateProvinceInfo();
        RefreshButtonStates();
    }

    private void OnPanelClosed()
    {
        HidePanel();
    }

    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {
        if (province == currentProvince)
        {
            UpdateProvinceInfo();
            RefreshButtonStates();
        }
    }

    private void ShowPanel()
    {
        isVisible = true;
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void HidePanel()
    {
        isVisible = false;
        currentProvince = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (canvasGroup == null) return;
        
        float targetAlpha = isVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    private void UpdateProvinceInfo()
    {
        if (currentProvince == null) return;
        
        if (provinceNameText != null)
            provinceNameText.text = currentProvince.provinceName;
        
        if (populationText != null)
            populationText.text = $"Pop: {currentProvince.provinceCurrentPop:F0}/{currentProvince.provinceMaxPop:F0}";
        
        if (incomeText != null)
        {
            float totalIncome = currentProvince.provinceTaxIncome + currentProvince.provinceTradePower;
            incomeText.text = $"Income: {totalIncome:F0}g (Tax: {currentProvince.provinceTaxIncome:F0} + Trade: {currentProvince.provinceTradePower:F0})";
        }
        
        if (buildingsListText != null)
        {
            if (currentProvince.buildings.Count > 0)
            {
                buildingsListText.text = "Buildings: " + string.Join(", ", currentProvince.buildings);
            }
            else
            {
                buildingsListText.text = "Buildings: None";
            }
        }
    }

    private void RefreshButtonStates()
    {
        if (currentProvince == null || playerNation == null) return;
        
        float gold = playerNation.nationMoney;
        
        SetButtonState(buildFortressBtn, "Fortress", gold);
        SetButtonState(buildFarmBtn, "Farm", gold);
        SetButtonState(buildBarracksBtn, "Barracks", gold);
        SetButtonState(buildTradeBuildingBtn, "Trade_Building", gold);
        SetButtonState(buildHousingBtn, "Housing", gold);
    }

    private void SetButtonState(Button button, string buildingType, float availableGold)
    {
        if (button == null) return;
        
        bool canBuild = builder.CanBuild(currentProvince, buildingType, availableGold);
        button.interactable = canBuild;
        
        // Visual feedback - dim if already built
        if (currentProvince.buildings.Contains(buildingType))
        {
            var colors = button.colors;
            colors.disabledColor = new Color(0.5f, 0.8f, 0.5f, 0.5f); // Green tint for built
            button.colors = colors;
        }
    }

    private void TryBuild(string buildingType)
    {
        if (currentProvince == null || playerNation == null)
        {
            Debug.LogWarning("Cannot build - no province or player");
            return;
        }
        
        float cost = builder.BuildBuilding(currentProvince, buildingType, playerNation.nationMoney);
        
        if (cost > 0)
        {
            playerNation.nationMoney -= cost;
            playerNation.RecalculateStats();
            GameEvents.PlayerStatsChanged();
        }
    }

    private void ClosePanel()
    {
        GameEvents.ProvincePanelClosed();
    }
}