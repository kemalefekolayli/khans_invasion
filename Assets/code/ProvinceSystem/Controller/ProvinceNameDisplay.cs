using UnityEngine;
using TMPro;

public class ProvinceNameDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI provinceNameText;
    
    [Header("Display Settings")]
    public Vector3 offsetFromProvince = new Vector3(0, 2f, 0);
    public bool followMouse = false;
    
    [Header("Animation")]
    public float fadeSpeed = 5f;
    public float slideDistance = 30f;
    
    [Header("Styling")]
    public float fontSize = 36f;
    public Color textColor = Color.white;
    public Color outlineColor = Color.black;
    public float outlineWidth = 0.3f;
    public bool keepFixedScreenSize = true;
    
    private Camera mainCamera;
    private ProvinceModel currentProvince;
    private bool isDisplaying;
    private CanvasGroup canvasGroup;
    private Vector3 basePosition;
    private float currentSlideOffset;
    private int activeBattlePanels;
    private int activeBlockingPanels;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        if (provinceNameText == null)
            provinceNameText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (provinceNameText != null)
        {
            ApplyCityNameTextStyle();
            provinceNameText.outlineColor = outlineColor;
            provinceNameText.outlineWidth = outlineWidth;
            
            canvasGroup = provinceNameText.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = provinceNameText.gameObject.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceEnter += OnProvinceEnter;
        GameEvents.OnProvinceExit += OnProvinceExit;
        GameEvents.OnProvinceDataLoaded += RefreshCurrentProvinceName;
        GameEvents.OnArmyBattleStarted += OnArmyBattleStarted;
        GameEvents.OnArmyBattleEnded += OnArmyBattleEnded;
        GameEvents.OnProvinceManagementOpened += OnBlockingProvincePanelOpened;
        GameEvents.OnProvinceInteractionOpened += OnBlockingProvincePanelOpened;
        GameEvents.OnBarrackMenuOpened += OnBlockingPanelOpened;
        GameEvents.OnQuestPanelOpened += OnBlockingPanelOpened;
        GameEvents.OnProvincePanelClosed += OnBlockingPanelClosed;
        GameEvents.OnQuestPanelClosed += OnBlockingPanelClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceEnter -= OnProvinceEnter;
        GameEvents.OnProvinceExit -= OnProvinceExit;
        GameEvents.OnProvinceDataLoaded -= RefreshCurrentProvinceName;
        GameEvents.OnArmyBattleStarted -= OnArmyBattleStarted;
        GameEvents.OnArmyBattleEnded -= OnArmyBattleEnded;
        GameEvents.OnProvinceManagementOpened -= OnBlockingProvincePanelOpened;
        GameEvents.OnProvinceInteractionOpened -= OnBlockingProvincePanelOpened;
        GameEvents.OnBarrackMenuOpened -= OnBlockingPanelOpened;
        GameEvents.OnQuestPanelOpened -= OnBlockingPanelOpened;
        GameEvents.OnProvincePanelClosed -= OnBlockingPanelClosed;
        GameEvents.OnQuestPanelClosed -= OnBlockingPanelClosed;
    }

    private void OnProvinceEnter(ProvinceModel province)
    {
        if (provinceNameText == null || province == null) return;
        
        currentProvince = province;
        provinceNameText.text = !string.IsNullOrEmpty(province.provinceName) ? province.provinceName : province.gameObject.name;
        ApplyCityNameTextStyle();
        
        Vector3 worldPos = province.transform.position + offsetFromProvince;
        basePosition = mainCamera.WorldToScreenPoint(worldPos);
        currentSlideOffset = -slideDistance;
        
        isDisplaying = true;
    }

    private void OnProvinceExit(ProvinceModel province)
    {
        if (currentProvince == province)
        {
            isDisplaying = false;
            currentProvince = null;
        }
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        if (IsBlockedByForegroundPanel())
        {
            SetProvinceNameVisible(false);
            return;
        }
        
        if (isDisplaying && provinceNameText != null)
        {
            SetProvinceNameVisible(true);
            ApplyCityNameTextStyle();

            if (followMouse)
                basePosition = Input.mousePosition + offsetFromProvince;
            else if (currentProvince != null)
            {
                Vector3 worldPos = currentProvince.transform.position + offsetFromProvince;
                basePosition = mainCamera.WorldToScreenPoint(worldPos);
            }
            
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            currentSlideOffset = Mathf.Lerp(currentSlideOffset, 0, Time.deltaTime * fadeSpeed);
            provinceNameText.transform.position = basePosition + new Vector3(0, currentSlideOffset, 0);
        }
        else if (canvasGroup.alpha > 0.01f)
        {
            SetProvinceNameVisible(true);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            currentSlideOffset = Mathf.Lerp(currentSlideOffset, slideDistance, Time.deltaTime * fadeSpeed);
            provinceNameText.transform.position = basePosition + new Vector3(0, currentSlideOffset, 0);
        }
        else
        {
            SetProvinceNameVisible(false);
        }
    }

    private void ApplyCityNameTextStyle()
    {
        if (provinceNameText == null) return;

        GameFontManager.ApplyCityNameFont(provinceNameText);
        provinceNameText.outlineColor = outlineColor;
        provinceNameText.outlineWidth = outlineWidth;
        provinceNameText.enableWordWrapping = false;
        provinceNameText.overflowMode = TextOverflowModes.Overflow;

        if (GameFontManager.Instance == null)
        {
            provinceNameText.fontSize = fontSize;
            provinceNameText.color = textColor;
        }

        if (keepFixedScreenSize)
        {
            provinceNameText.enableAutoSizing = false;
            provinceNameText.transform.localScale = Vector3.one;
        }
    }

    private void RefreshCurrentProvinceName()
    {
        if (provinceNameText == null || currentProvince == null) return;

        provinceNameText.text = !string.IsNullOrEmpty(currentProvince.provinceName)
            ? currentProvince.provinceName
            : currentProvince.gameObject.name;
    }

    private void OnArmyBattleStarted(Army armyA, Army armyB)
    {
        activeBattlePanels++;
        SetProvinceNameVisible(false);
    }

    private void OnArmyBattleEnded(Army winner, Army loser, ArmyBattleEndReason reason)
    {
        activeBattlePanels = Mathf.Max(0, activeBattlePanels - 1);
    }

    private void OnBlockingProvincePanelOpened(ProvinceModel province)
    {
        OnBlockingPanelOpened();
    }

    private void OnBlockingPanelOpened()
    {
        activeBlockingPanels++;
        isDisplaying = false;
        currentProvince = null;
        SetProvinceNameVisible(false);
    }

    private void OnBlockingPanelClosed()
    {
        activeBlockingPanels = Mathf.Max(0, activeBlockingPanels - 1);
    }

    private bool IsBlockedByForegroundPanel()
    {
        return activeBattlePanels > 0 || activeBlockingPanels > 0 || ArmyBattlePopupSpawner.HasActivePanels;
    }

    private void SetProvinceNameVisible(bool visible)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = visible ? canvasGroup.alpha : 0f;

        if (provinceNameText != null)
            provinceNameText.enabled = visible;
    }
}
