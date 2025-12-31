using UnityEngine;

/// <summary>
/// Alternative visuals component that uses TroopSpritesConfig ScriptableObject.
/// Easier to manage sprites in one place.
/// </summary>
[RequireComponent(typeof(Army))]
public class TroopLevelVisualsConfig : MonoBehaviour
{
    [Header("Sprites Configuration")]
    [SerializeField] private TroopSpritesConfig spritesConfig;
    
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer troopRenderer;
    [SerializeField] private SpriteRenderer progressBarRenderer;
    
    [Header("Progress Bar Settings")]
    [SerializeField] private Vector3 progressBarOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float progressBarScale = 0.3f;
    [SerializeField] private int progressBarSortingOrder = 10;
    
    private GameObject progressBarObject;
    
    private void Awake()
    {
        if (troopRenderer == null)
            troopRenderer = GetComponent<SpriteRenderer>();
        
        EnsureProgressBarExists();
    }
    
    private void EnsureProgressBarExists()
    {
        if (progressBarRenderer != null) return;
        
        Transform existing = transform.Find("ProgressBar");
        if (existing != null)
        {
            progressBarObject = existing.gameObject;
            progressBarRenderer = existing.GetComponent<SpriteRenderer>();
            return;
        }
        
        progressBarObject = new GameObject("ProgressBar");
        progressBarObject.transform.SetParent(transform);
        progressBarObject.transform.localPosition = progressBarOffset;
        progressBarObject.transform.localScale = Vector3.one * progressBarScale;
        
        progressBarRenderer = progressBarObject.AddComponent<SpriteRenderer>();
        progressBarRenderer.sortingOrder = progressBarSortingOrder;
    }
    
    /// <summary>
    /// Update visuals from level data.
    /// </summary>
    public void UpdateVisuals(TroopLevelData data)
    {
        if (spritesConfig == null)
        {
            Debug.LogWarning("[TroopLevelVisualsConfig] No sprites config assigned!");
            return;
        }
        
        UpdateTroopSprite(data);
        UpdateProgressBar(data);
    }
    
    private void UpdateTroopSprite(TroopLevelData data)
    {
        if (troopRenderer == null) return;
        
        Sprite sprite;
        if (data.IsMaxLevel)
        {
            sprite = spritesConfig.GetGoldenSprite(data.colorVariant);
            if (sprite != null)
                Debug.Log($"[Visuals] Applied GOLDEN sprite for {data.colorVariant}");
        }
        else
        {
            sprite = spritesConfig.GetRegularSprite(data.colorVariant);
        }
        
        if (sprite != null)
            troopRenderer.sprite = sprite;
    }
    
    private void UpdateProgressBar(TroopLevelData data)
    {
        if (progressBarRenderer == null)
        {
            EnsureProgressBarExists();
            if (progressBarRenderer == null) return;
        }
        
        Sprite sprite = spritesConfig.GetProgressSprite(data.currentLevel, data.colorVariant);
        if (sprite != null)
            progressBarRenderer.sprite = sprite;
    }
    
    public void SetProgressBarVisible(bool visible)
    {
        if (progressBarObject != null)
            progressBarObject.SetActive(visible);
    }
    
    public void SetSpritesConfig(TroopSpritesConfig config)
    {
        spritesConfig = config;
    }
}