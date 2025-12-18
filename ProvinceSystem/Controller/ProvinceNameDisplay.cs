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
    public float fontSize = 24f;
    public Color textColor = Color.white;
    public Color outlineColor = Color.black;
    public float outlineWidth = 0.3f;
    
    private Camera mainCamera;
    private ProvinceModel currentProvince;
    private bool isDisplaying;
    private CanvasGroup canvasGroup;
    private Vector3 basePosition;
    private float currentSlideOffset;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        if (provinceNameText == null)
            provinceNameText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (provinceNameText != null)
        {
            provinceNameText.fontSize = fontSize;
            provinceNameText.color = textColor;
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
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceEnter -= OnProvinceEnter;
        GameEvents.OnProvinceExit -= OnProvinceExit;
    }

    private void OnProvinceEnter(ProvinceModel province)
    {
        if (provinceNameText == null || province == null) return;
        
        currentProvince = province;
        provinceNameText.text = province.provinceName;
        
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
        
        if (isDisplaying && provinceNameText != null)
        {
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
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            currentSlideOffset = Mathf.Lerp(currentSlideOffset, slideDistance, Time.deltaTime * fadeSpeed);
            provinceNameText.transform.position = basePosition + new Vector3(0, currentSlideOffset, 0);
        }
    }
}