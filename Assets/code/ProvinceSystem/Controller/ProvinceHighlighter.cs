using UnityEngine;

/// <summary>
/// Tracks the province currently occupied by the selected general.
/// The highlight is rendered as an overlay so map mode and fog remain the source
/// of truth for each province's base colour.
/// </summary>
public class ProvinceHighlighter : MonoBehaviour
{
    [Header("Settings")]
    [Range(0f, 1f)]
    [Tooltip("How much darker the current province appears.")]
    public float darkenAmount = 0.7f;

    private ProvinceModel highlightedProvince;
    private SpriteRenderer highlightOverlay;

    private void OnEnable()
    {
        GameEvents.OnProvinceEnter += OnProvinceEnter;
        GameEvents.OnProvinceExit += OnProvinceExit;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceEnter -= OnProvinceEnter;
        GameEvents.OnProvinceExit -= OnProvinceExit;
        ClearHighlight();
    }

    private void OnProvinceEnter(ProvinceModel province)
    {
        if (province == null) return;

        if (highlightedProvince == province)
            return;

        ClearHighlight();
        highlightedProvince = province;
        ShowHighlight(province);
    }

    private void OnProvinceExit(ProvinceModel province)
    {
        if (highlightedProvince == province)
            ClearHighlight();
    }

    private void ShowHighlight(ProvinceModel province)
    {
        if (province.spriteRenderer == null) return;

        GameObject overlayObject = new GameObject("Province Highlight Overlay");
        overlayObject.transform.SetParent(province.spriteRenderer.transform, false);
        highlightOverlay = overlayObject.AddComponent<SpriteRenderer>();
        CopyRendererAppearance(province.spriteRenderer, highlightOverlay);
        highlightOverlay.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(darkenAmount));
    }

    private void ClearHighlight()
    {
        highlightedProvince = null;

        if (highlightOverlay != null)
        {
            Destroy(highlightOverlay.gameObject);
            highlightOverlay = null;
        }
    }

    private static void CopyRendererAppearance(SpriteRenderer source, SpriteRenderer target)
    {
        target.sprite = source.sprite;
        target.sharedMaterial = source.sharedMaterial;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder + 1;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.drawMode = source.drawMode;
        target.size = source.size;
        target.maskInteraction = source.maskInteraction;
    }
}
