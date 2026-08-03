using UnityEngine;

/// <summary>
/// Handles visual highlighting of provinces when entered/exited.
/// Subscribes to GameEvents for province detection.
/// </summary>
public class ProvinceHighlighter : MonoBehaviour
{
    [Header("Settings")]
    [Range(0f, 1f)]
    public float darkenAmount = 0.7f;
    
    private ProvinceModel highlightedProvince;

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
        if (province == null || province.spriteRenderer == null) return;
        
        highlightedProvince = province;
        
        Color baseColor = FogOfWarManager.Instance != null
            ? FogOfWarManager.Instance.GetVisibleBaseColor(province)
            : province.provinceColor;
        Color darkened = baseColor * darkenAmount;
        darkened.a = baseColor.a;
        province.spriteRenderer.color = darkened;
    }

    private void OnProvinceExit(ProvinceModel province)
    {
        if (province == null || province.spriteRenderer == null) return;
        
        province.spriteRenderer.color = FogOfWarManager.Instance != null
            ? FogOfWarManager.Instance.GetVisibleBaseColor(province)
            : province.provinceColor;
        
        if (highlightedProvince == province)
            highlightedProvince = null;
    }
}