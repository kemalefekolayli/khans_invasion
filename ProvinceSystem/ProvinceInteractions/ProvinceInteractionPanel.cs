using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Panel for interacting with enemy or neutral provinces.
/// Handles raid, siege, and scouting actions.
/// </summary>
public class ProvinceInteractionPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;
    
    [Header("Province Info")]
    public TextMeshProUGUI provinceNameText;
    public TextMeshProUGUI ownerText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI lootText;
    
    [Header("Action Buttons")]
    public Button raidButton;
    public Button siegeButton;
    public Button scoutButton;
    
    [Header("Close Button")]
    public Button closeButton;
    
    [Header("Animation")]
    public float fadeSpeed = 8f;
    
    private ProvinceModel currentProvince;
    private bool isVisible = false;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        
        if (canvasGroup == null && panelRoot != null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceInteractionOpened += OnInteractionOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceInteractionOpened -= OnInteractionOpened;
        GameEvents.OnProvincePanelClosed -= OnPanelClosed;
    }

    private void SetupButtons()
    {
        if (raidButton != null)
            raidButton.onClick.AddListener(OnRaidClicked);
        
        if (siegeButton != null)
            siegeButton.onClick.AddListener(OnSiegeClicked);
        
        if (scoutButton != null)
            scoutButton.onClick.AddListener(OnScoutClicked);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnInteractionOpened(ProvinceModel province)
    {
        currentProvince = province;
        
        ShowPanel();
        UpdateProvinceInfo();
        RefreshButtonStates();
    }

    private void OnPanelClosed()
    {
        HidePanel();
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
        
        if (ownerText != null)
        {
            string ownerName = currentProvince.provinceOwner != null 
                ? currentProvince.provinceOwner.nationName 
                : "Unowned";
            ownerText.text = $"Owner: {ownerName}";
        }
        
        if (defenseText != null)
        {
            float defenseStrength = currentProvince.defenceForceSize * currentProvince.defenceForceStr;
            defenseText.text = $"Defense: {currentProvince.defenceForceSize:F0} troops ({defenseStrength:F0} strength)";
        }
        
        if (lootText != null)
            lootText.text = $"Available Loot: {currentProvince.availableLoot:F0}g";
    }

    private void RefreshButtonStates()
    {
        if (currentProvince == null) return;
        
        // TODO: Check player army state, war status, etc.
        // For now, all buttons enabled if province has an owner
        
        bool hasOwner = currentProvince.provinceOwner != null;
        
        if (raidButton != null)
            raidButton.interactable = hasOwner;
        
        if (siegeButton != null)
            siegeButton.interactable = hasOwner;
        
        if (scoutButton != null)
            scoutButton.interactable = true; // Can always scout
    }

    private void OnRaidClicked()
    {
        if (currentProvince == null) return;
        
        Debug.Log($"Raiding {currentProvince.provinceName}...");
        
        // TODO: Implement raid logic
        // - Check if player has army attached to khan
        // - Calculate battle outcome
        // - Award loot on success
        // - Apply casualties
        
        ClosePanel();
    }

    private void OnSiegeClicked()
    {
        if (currentProvince == null) return;
        
        Debug.Log($"Starting siege on {currentProvince.provinceName}...");
        
        // TODO: Implement siege logic
        // - Check if player has sufficient army
        // - Start siege timer/turns
        // - Handle assault vs starve options
        
        ClosePanel();
    }

    private void OnScoutClicked()
    {
        if (currentProvince == null) return;
        
        Debug.Log($"Scouting {currentProvince.provinceName}...");
        
        // TODO: Implement scout logic
        // - Reveal province details
        // - Show nearby army positions
        // - Cost some gold or require scout unit
        
        ClosePanel();
    }

    private void ClosePanel()
    {
        GameEvents.ProvincePanelClosed();
    }
}