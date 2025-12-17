// ProvinceInfoDisplay.cs
using UnityEngine;
using TMPro;

public class ProvinceInfoDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI provinceNameText;
    public TextMeshProUGUI ownerText;
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI taxIncomeText;
    public TextMeshProUGUI tradePowerText;
    
    [Header("Container")]
    public CanvasGroup canvasGroup;
    
    [Header("Animation")]
    public float fadeSpeed = 8f;
    
    private ProvinceModel currentProvince;
    private bool isVisible;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0;
    }

    private void Update()
    {
        if (canvasGroup == null) return;
        
        float targetAlpha = isVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    public void ShowProvinceInfo(ProvinceModel province)
    {
        if (province == null) return;
        
        currentProvince = province;
        UpdateDisplay();
        isVisible = true;
    }

    public void HideProvinceInfo()
    {
        isVisible = false;
        currentProvince = null;
    }

    private void UpdateDisplay()
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
        
        if (populationText != null)
            populationText.text = $"Population: {currentProvince.provinceCurrentPop:F0}/{currentProvince.provinceMaxPop:F0}";
        
        if (taxIncomeText != null)
            taxIncomeText.text = $"Tax: {currentProvince.provinceTaxIncome:F0}";
        
        if (tradePowerText != null)
            tradePowerText.text = $"Trade: {currentProvince.provinceTradePower:F0}";
    }
}