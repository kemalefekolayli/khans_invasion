using UnityEngine;
using TMPro;

/// <summary>
/// Spawns floating text popups for population-related events.
/// - Building housing: "+1000 max pop" 
/// - Turn start population growth: "+X pop" (smaller text)
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
    
    [Header("Debug")]
    [SerializeField] private bool logPopups = false;
    
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
    
    private void Start()
    {
        Debug.Log("✓ PopulationPopupSpawner initialized");
    }
    
    /// <summary>
    /// Called when a building is constructed. Shows bonus for relevant buildings.
    /// </summary>
    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {
        if (province == null) return;
        
        // Only show popup for Housing (population bonus)
        if (buildingType == "Housing")
        {
            // Get the housing bonus from Builder
            float bonus = 1000f; // Default
            if (Builder.Instance != null)
            {
                var (_, desc, _) = Builder.Instance.GetBuildingInfo("Housing");
                // Try to parse bonus from description, or use default
            }
            
            Vector3 spawnPos = GetCityCenterPosition(province) + spawnOffset;
            SpawnPopupText($"+{bonus:F0} max pop", spawnPos, buildingBonusColor, buildingFontSize);
            
            if (logPopups)
            {
                Debug.Log($"[PopulationPopup] Housing built in {province.provinceName}: +{bonus} max pop");
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
            Debug.Log($"[PopulationPopup] {province.provinceName} grew by {growthAmount:F0}");
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
        // Create text object
        GameObject textObj = new GameObject($"PopupText_{message}");
        textObj.transform.position = worldPosition;
        
        // Add TextMeshPro component
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        // Outline for visibility
        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = Color.black;
        
        // Sorting order to appear above other sprites
        tmp.sortingOrder = 100;
        
        // Add floating behavior
        FloatingPopupText floatScript = textObj.AddComponent<FloatingPopupText>();
        floatScript.Initialize(worldPosition, riseSpeed, lifetime);
        
        // Billboard (face camera)
        textObj.AddComponent<PopupBillboard>();
    }
}

/// <summary>
/// Floating text animation component.
/// </summary>
public class FloatingPopupText : MonoBehaviour
{
    private Vector3 startPosition;
    private float riseSpeed;
    private float lifetime;
    private float spawnTime;
    private TextMeshPro textMesh;
    private float startAlpha;
    
    public void Initialize(Vector3 startPos, float rise, float life)
    {
        startPosition = startPos;
        riseSpeed = rise;
        lifetime = life;
        spawnTime = Time.time;
        
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            startAlpha = textMesh.color.a;
        }
    }
    
    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        float progress = elapsed / lifetime;
        
        if (progress >= 1f)
        {
            Destroy(gameObject);
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
