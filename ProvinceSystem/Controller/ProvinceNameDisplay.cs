using UnityEngine;
using TMPro;

public class ProvinceNameDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the TextMeshPro UI object here")]
    public TextMeshProUGUI provinceNameText;
    
    [Header("Display Settings")]
    public Vector3 offsetFromProvince = new Vector3(0, 2f, 0);
    public bool followMouse = false;
    public float followSpeed = 10f;
    
    [Header("Styling")]
    public Color textColor = Color.white;
    public Color outlineColor = Color.black;
    public float outlineWidth = 0.2f;
    
    private Camera mainCamera;
    private ProvinceModel currentProvince;
    private bool isDisplaying = false;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Auto-find TextMeshPro if not assigned
        if (provinceNameText == null)
        {
            provinceNameText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (provinceNameText == null)
        {
            Debug.LogError("ProvinceNameDisplay: TextMeshProUGUI not found! Please assign it in the inspector.");
        }
        else
        {
            // Apply styling
            provinceNameText.color = textColor;
            provinceNameText.outlineColor = outlineColor;
            provinceNameText.outlineWidth = outlineWidth;
            HideProvinceName();
        }
    }

    private void Update()
    {
        if (isDisplaying && provinceNameText != null)
        {
            if (followMouse)
            {
                UpdatePositionToMouse();
            }
            else if (currentProvince != null)
            {
                UpdatePositionToProvince();
            }
        }
    }

    public void ShowProvinceName(ProvinceModel province)
    {
        if (provinceNameText == null || province == null) return;
        
        currentProvince = province;
        provinceNameText.text = province.provinceName;
        provinceNameText.gameObject.SetActive(true);
        isDisplaying = true;
        
        // Position immediately
        if (followMouse)
        {
            UpdatePositionToMouse();
        }
        else
        {
            UpdatePositionToProvince();
        }
    }

    public void HideProvinceName()
    {
        if (provinceNameText == null) return;
        
        provinceNameText.gameObject.SetActive(false);
        isDisplaying = false;
        currentProvince = null;
    }


    private void UpdatePositionToProvince()
    {
        if (currentProvince == null || mainCamera == null) return;
        
        // Get province world position
        Vector3 worldPos = currentProvince.transform.position + offsetFromProvince;
        
        // Convert to screen position
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        // Update text position
        provinceNameText.transform.position = screenPos;
    }

    private void UpdatePositionToMouse()
    {
        if (mainCamera == null) return;
        
        Vector3 mousePos = Input.mousePosition + offsetFromProvince;
        
        // Smooth follow
        provinceNameText.transform.position = Vector3.Lerp(
            provinceNameText.transform.position,
            mousePos,
            Time.deltaTime * followSpeed
        );
    }
}