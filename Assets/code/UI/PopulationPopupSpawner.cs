using UnityEngine;
using TMPro;

/// <summary>
/// Spawns floating text popups for population-related events.
/// - Building housing: "+1000 max pop" 
/// - Turn start population growth: "+X pop" (smaller text)
/// Popups are pooled and reused to avoid GC churn.
/// </summary>
public class PopulationPopupSpawner : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color buildingBonusColor = new Color(0.3f, 0.8f, 0.3f); // Green
    [SerializeField] private Color growthColor = new Color(0.5f, 0.9f, 0.5f); // Light green
    
    [Header("Appearance")]
    [SerializeField] private float buildingFontSize = 3.5f;
    [SerializeField] private float growthFontSize = 2.5f;
    [SerializeField] private float outlineWidth = 0.2f;
    
    [Header("Animation")]
    [SerializeField] private float riseSpeed = 1f;
    [SerializeField] private float lifetime = 2.5f;
    
    [Header("Position")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.5f, 0);
    
    [Header("Object Pooling")]
    [SerializeField] private int poolSize = 40;
    
    [Header("Debug")]
    [SerializeField] private bool logPopups = false;
    
    private ComponentPool<FloatingPopupText> pool;
    
    private void Awake()
    {
        pool = new ComponentPool<FloatingPopupText>("PopupTextPool", transform, poolSize, CreatePopup);
    }
    
    private void OnEnable()
    {
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
        GameEvents.OnPopulationGrowth += OnPopulationGrowth;
    }
    
    private void OnDisable()
    {
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
        GameEvents.OnPopulationGrowth -= OnPopulationGrowth;
    }
    
    /// <summary>
    /// Called when a building is constructed. Shows benefit popup for all buildings.
    /// </summary>
    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {
        if (province == null) return;
        
        string message = "";
        float fontSize = buildingFontSize;

        switch (buildingType)
        {
            case "Housing":
                float bonus = 1000f; // Default
                if (Builder.Instance != null)
                {
                    // Logic to get exact bonus could go here if exposed
                }
                message = $"+{bonus:F0} max pop";
                break;

            case "Barracks":
                message = "Barracks built: Can hire troops here now.";
                fontSize = buildingFontSize * 0.6f; // Smaller font for long text
                break;

            case "Trade_Building":
                message = "+25 trade";
                break;

            case "Farm":
                message = "+10 tax income";
                break;

            case "Fortress":
                message = "Fortress: Defences significantly improved.";
                fontSize = buildingFontSize * 0.6f; // Smaller font for long text
                break;
        }
        
        if (!string.IsNullOrEmpty(message))
        {
            Vector3 spawnPos = GetCityCenterPosition(province) + spawnOffset;
            SpawnPopupText(message, spawnPos, buildingBonusColor, fontSize);
            
            if (logPopups)
            {
                GameLog.Log(GameLogCategory.Province, $"[PopulationPopup] {buildingType} built in {province.provinceName}: {message}");
            }
        }
    }
    
    /// <summary>
    /// Called when population grows at turn start.
    /// </summary>
    private void OnPopulationGrowth(ProvinceModel province, float growthAmount)
    {
        if (province == null || growthAmount <= 0) return;
        
        Vector3 spawnPos = GetCityCenterPosition(province) + spawnOffset;
        SpawnPopupText($"+{growthAmount:F0} pop", spawnPos, growthColor, growthFontSize);
        
        if (logPopups)
        {
            GameLog.Log(GameLogCategory.Province, $"[PopulationPopup] {province.provinceName} grew by {growthAmount:F0}");
        }
    }
    
    /// <summary>
    /// Get the city center position for spawning.
    /// </summary>
    private Vector3 GetCityCenterPosition(ProvinceModel province)
    {
        CityCenter cityCenter = province.GetComponentInChildren<CityCenter>();
        if (cityCenter != null)
        {
            return cityCenter.transform.position;
        }
        return province.transform.position;
    }
    
    /// <summary>
    /// Spawn floating text at position.
    /// </summary>
    private void SpawnPopupText(string message, Vector3 worldPosition, Color color, float fontSize)
    {
        FloatingPopupText popup = GetFromPool();
        if (popup == null) return;
        
        // Configure text for this popup
        TextMeshPro tmp = popup.GetComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = fontSize;
        tmp.color = color;
        
        popup.Initialize(worldPosition, riseSpeed, lifetime);
    }
    
    private FloatingPopupText GetFromPool()
    {
        FloatingPopupText popup = pool.Get();
        if (popup != null)
        {
            popup.BindPool(pool);
        }
        return popup;
    }
    
    private FloatingPopupText CreatePopup(Transform parent)
    {
        // Create text object
        GameObject textObj = new GameObject("PopupText");
        textObj.transform.SetParent(parent);
        
        // Add TextMeshPro component
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        GameFontManager.Apply(tmp);
        
        // Outline for visibility
        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = Color.black;
        
        // Sorting order to appear above other sprites
        tmp.sortingOrder = 100;
        
        // Add floating behavior
        FloatingPopupText floatScript = textObj.AddComponent<FloatingPopupText>();
        
        // Billboard (face camera)
        textObj.AddComponent<PopupBillboard>();
        
        return floatScript;
    }
}

/// <summary>
/// Floating text animation component.
/// Returns itself to its pool when the animation finishes.
/// </summary>
public class FloatingPopupText : MonoBehaviour
{
    private ComponentPool<FloatingPopupText> pool;
    private Vector3 startPosition;
    private float riseSpeed;
    private float lifetime;
    private float spawnTime;
    private TextMeshPro textMesh;
    private float startAlpha;
    
    /// <summary>
    /// Associates this popup with a pool so it returns instead of being destroyed.
    /// </summary>
    public void BindPool(ComponentPool<FloatingPopupText> popupPool)
    {
        pool = popupPool;
    }
    
    public void Initialize(Vector3 startPos, float rise, float life)
    {
        startPosition = startPos;
        riseSpeed = rise;
        lifetime = life;
        spawnTime = Time.time;
        
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            // Reset alpha that may have faded out during the previous use
            Color c = textMesh.color;
            c.a = 1f;
            textMesh.color = c;
            startAlpha = textMesh.color.a;
        }
        
        transform.position = startPos;
        
        // Activate (pooled popups start inactive)
        gameObject.SetActive(true);
    }
    
    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        float progress = elapsed / lifetime;
        
        if (progress >= 1f)
        {
            // Return to pool when finished (destroy if not pooled)
            if (pool != null)
            {
                pool.Return(this);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }
        
        // Rise upward
        transform.position = startPosition + new Vector3(0, elapsed * riseSpeed, 0);
        
        // Fade out in last 30%
        if (progress > 0.7f && textMesh != null)
        {
            float fadeProgress = (progress - 0.7f) / 0.3f;
            Color c = textMesh.color;
            c.a = Mathf.Lerp(startAlpha, 0f, fadeProgress);
            textMesh.color = c;
        }
    }
}

/// <summary>
/// Makes the popup always face the camera.
/// </summary>
public class PopupBillboard : MonoBehaviour
{
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}
