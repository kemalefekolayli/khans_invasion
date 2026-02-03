using UnityEngine;

/// <summary>
/// Creates a striped (çizgili) overlay effect on provinces that have been sieged.
/// Uses SpriteMask to ensure stripes only render within the province's shape.
/// Attach this directly to the Province prefab.
/// </summary>
[RequireComponent(typeof(ProvinceModel))]
public class SiegeOverlayController : MonoBehaviour
{
    [Header("Stripe Settings")]
    [Tooltip("Color of the stripes")]
    [SerializeField] private Color stripeColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    
    [Tooltip("Number of stripes across the province")]
    [SerializeField] private int stripeCount = 8;
    
    [Tooltip("Stripe width ratio (0-1, portion of stripe+gap that is stripe)")]
    [SerializeField] private float stripeWidthRatio = 0.5f;
    
    [Tooltip("Angle of the stripes in degrees")]
    [SerializeField] private float stripeAngle = 45f;
    
    [Header("Animation")]
    [Tooltip("Enable pulsing animation")]
    [SerializeField] private bool enablePulse = true;
    
    [Tooltip("Pulse speed")]
    [SerializeField] private float pulseSpeed = 2f;
    
    [Tooltip("Pulse intensity (0-1)")]
    [SerializeField] private float pulseIntensity = 0.2f;
    
    private ProvinceModel province;
    private SpriteRenderer provinceRenderer;
    private GameObject overlayObject;
    private SpriteRenderer overlayRenderer;
    private SpriteMask spriteMask;
    private bool isUnderSiege = false;
    
    private void Awake()
    {
        province = GetComponent<ProvinceModel>();
        provinceRenderer = GetComponent<SpriteRenderer>();
        
        if (provinceRenderer == null)
        {
            provinceRenderer = province?.spriteRenderer;
        }
    }
    
    private void OnEnable()
    {
        GameEvents.OnProvinceSieged += OnProvinceSieged;
        GameEvents.OnProvinceConquered += OnProvinceConquered;
        GameEvents.OnSiegeCancelled += OnSiegeCancelled;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceSieged -= OnProvinceSieged;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
        GameEvents.OnSiegeCancelled -= OnSiegeCancelled;
    }
    
    private void Update()
    {
        if (isUnderSiege && enablePulse && overlayRenderer != null)
        {
            // Pulsing effect
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            Color pulsedColor = stripeColor;
            pulsedColor.a = Mathf.Clamp01(stripeColor.a + pulse);
            overlayRenderer.color = pulsedColor;
        }
    }
    
    private void OnProvinceSieged(ProvinceModel siegedProvince, General attacker, float defenseStrength)
    {
        if (siegedProvince == province)
        {
            ShowSiegeOverlay();
        }
    }
    
    private void OnProvinceConquered(ProvinceModel conqueredProvince, NationModel oldOwner, NationModel newOwner)
    {
        if (conqueredProvince == province)
        {
            HideSiegeOverlay();
        }
    }
    
    private void OnSiegeCancelled(ProvinceModel cancelledProvince)
    {
        if (cancelledProvince == province)
        {
            HideSiegeOverlay();
            Debug.Log($"[SiegeOverlay] Siege cancelled - hiding overlay on {province?.provinceName}");
        }
    }
    
    /// <summary>
    /// Show the striped siege overlay on this province.
    /// Uses SpriteMask to constrain stripes to province shape.
    /// </summary>
    public void ShowSiegeOverlay()
    {
        if (provinceRenderer == null || provinceRenderer.sprite == null)
        {
            Debug.LogWarning($"[SiegeOverlay] No sprite renderer found on {province?.provinceName}");
            return;
        }
        
        if (overlayObject != null)
        {
            overlayObject.SetActive(true);
            isUnderSiege = true;
            return;
        }
        
        // Create the SpriteMask on the province (if not exists)
        EnsureSpriteMask();
        
        // Create overlay child object
        CreateOverlayObject();
        
        isUnderSiege = true;
        
        Debug.Log($"[SiegeOverlay] Showing siege overlay on {province?.provinceName}");
    }
    
    /// <summary>
    /// Ensure the province has a SpriteMask component using its own sprite.
    /// </summary>
    private void EnsureSpriteMask()
    {
        spriteMask = GetComponent<SpriteMask>();
        
        if (spriteMask == null)
        {
            spriteMask = gameObject.AddComponent<SpriteMask>();
        }
        
        // Use the province's sprite as the mask
        spriteMask.sprite = provinceRenderer.sprite;
        spriteMask.alphaCutoff = 0.1f; // Only show where sprite alpha > 0.1
    }
    
    /// <summary>
    /// Create the overlay object with striped sprite that respects the mask.
    /// </summary>
    private void CreateOverlayObject()
    {
        overlayObject = new GameObject("SiegeStripeOverlay");
        overlayObject.transform.SetParent(transform);
        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.Euler(0, 0, stripeAngle);
        overlayObject.transform.localScale = Vector3.one;
        
        // Add sprite renderer
        overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        
        // Create stripe texture that's large enough to cover the province
        Texture2D stripeTexture = CreateStripeTexture(256, 256);
        Sprite stripeSprite = Sprite.Create(
            stripeTexture, 
            new Rect(0, 0, stripeTexture.width, stripeTexture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        
        overlayRenderer.sprite = stripeSprite;
        overlayRenderer.color = stripeColor;
        
        // IMPORTANT: This makes the stripes only visible inside the SpriteMask
        overlayRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        
        // Render above the province
        overlayRenderer.sortingOrder = provinceRenderer.sortingOrder + 1;
        overlayRenderer.sortingLayerName = provinceRenderer.sortingLayerName;
        
        // Scale to cover the province bounds
        ScaleOverlayToCoverProvince();
    }
    
    /// <summary>
    /// Scale the overlay to be large enough to cover the entire province.
    /// </summary>
    private void ScaleOverlayToCoverProvince()
    {
        if (provinceRenderer == null || overlayRenderer == null) return;
        
        Bounds provinceBounds = provinceRenderer.bounds;
        
        // Make overlay larger than province to account for rotation
        float maxDimension = Mathf.Max(provinceBounds.size.x, provinceBounds.size.y) * 1.5f;
        
        // Get the overlay sprite size
        Sprite overlaySprite = overlayRenderer.sprite;
        if (overlaySprite != null)
        {
            float spriteWidth = overlaySprite.bounds.size.x;
            float spriteHeight = overlaySprite.bounds.size.y;
            
            float scaleX = maxDimension / spriteWidth;
            float scaleY = maxDimension / spriteHeight;
            
            overlayObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
    
    /// <summary>
    /// Create a striped texture procedurally.
    /// </summary>
    private Texture2D CreateStripeTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        
        Color transparent = new Color(0, 0, 0, 0);
        Color stripe = Color.white; // Will be tinted by renderer color
        
        // Calculate stripe pattern (vertical stripes, rotation handled by transform)
        float stripeWidth = (float)width / stripeCount;
        float solidWidth = stripeWidth * stripeWidthRatio;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Determine if in stripe or gap based on X position
                float posInStripe = x % stripeWidth;
                
                if (posInStripe < solidWidth)
                {
                    texture.SetPixel(x, y, stripe);
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// Hide the siege overlay.
    /// </summary>
    public void HideSiegeOverlay()
    {
        if (overlayObject != null)
        {
            overlayObject.SetActive(false);
        }
        
        // Remove the SpriteMask when not under siege to avoid affecting other rendering
        if (spriteMask != null)
        {
            spriteMask.enabled = false;
        }
        
        isUnderSiege = false;
        
        Debug.Log($"[SiegeOverlay] Hiding siege overlay on {province?.provinceName}");
    }
    
    private void OnDestroy()
    {
        if (overlayObject != null)
        {
            Destroy(overlayObject);
        }
    }
    
    #region Debug
    
    [ContextMenu("Test Show Overlay")]
    private void DebugShowOverlay()
    {
        ShowSiegeOverlay();
    }
    
    [ContextMenu("Test Hide Overlay")]
    private void DebugHideOverlay()
    {
        HideSiegeOverlay();
    }
    
    #endregion
}
