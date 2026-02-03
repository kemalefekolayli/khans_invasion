using UnityEngine;
using TMPro;

/// <summary>
/// Spawns floating loot text when provinces are raided.
/// Attach to a manager object in the scene.
/// Creates TextMeshPro in world space - no prefab needed!
/// </summary>
public class LootPopupSpawner : MonoBehaviour
{
    [Header("Prefab (Optional)")]
    [Tooltip("Optional prefab with FloatingLootText component. If not assigned, text is created dynamically.")]
    [SerializeField] private GameObject lootTextPrefab;
    
    [Header("Appearance")]
    [SerializeField] private Color lootColor = new Color(1f, 0.84f, 0f); // Gold
    [SerializeField] private float fontSize = 5f; // World space - smaller value
    [SerializeField] private float outlineWidth = 0.2f;
    
    [Header("Animation")]
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private float lifetime = 2.5f;
    
    [Header("Position Offset")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 2f, 0);
    
    private void OnEnable()
    {
        GameEvents.OnProvinceRaided += OnProvinceRaided;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
    }
    
    private void OnProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        if (province == null || lootAmount <= 0) return;
        
        // Get spawn position (city center if available, otherwise province center)
        Vector3 spawnPosition = GetSpawnPosition(province);
        
        // Create floating text
        SpawnLootText(lootAmount, spawnPosition);
        
        Debug.Log($"[LootPopupSpawner] Spawned '+{lootAmount:F0} Gold' at {province.provinceName}");
    }
    
    private Vector3 GetSpawnPosition(ProvinceModel province)
    {
        // Try to find city center
        CityCenter cityCenter = province.GetComponentInChildren<CityCenter>();
        if (cityCenter != null)
        {
            return cityCenter.transform.position + spawnOffset;
        }
        
        // Fallback to province center
        return province.transform.position + spawnOffset;
    }
    
    private void SpawnLootText(float lootAmount, Vector3 worldPosition)
    {
        if (lootTextPrefab != null)
        {
            // Use prefab
            GameObject textObj = Instantiate(lootTextPrefab, worldPosition, Quaternion.identity);
            FloatingLootText floatingText = textObj.GetComponent<FloatingLootText>();
            if (floatingText != null)
            {
                floatingText.Initialize(lootAmount, worldPosition, riseSpeed, lifetime);
            }
        }
        else
        {
            // Create dynamically (no prefab needed)
            CreateDynamicLootText(lootAmount, worldPosition);
        }
    }
    
    private void CreateDynamicLootText(float lootAmount, Vector3 worldPosition)
    {
        // Create text object
        GameObject textObj = new GameObject($"LootText_+{lootAmount:F0}");
        textObj.transform.position = worldPosition;
        
        // Add TextMeshPro component (3D World Space - NOT UGUI)
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = $"+{lootAmount:F0} Gold";
        tmp.fontSize = fontSize;
        tmp.color = lootColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        // Outline for visibility
        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = Color.black;
        
        // Sorting order to appear above other sprites
        tmp.sortingOrder = 100;
        
        // Add floating behavior with our settings
        FloatingLootText floatScript = textObj.AddComponent<FloatingLootText>();
        floatScript.Initialize(lootAmount, worldPosition, riseSpeed, lifetime);
        
        // Billboard (face camera)
        textObj.AddComponent<BillboardText>();
    }
}

/// <summary>
/// Simple billboard that makes text face the camera.
/// </summary>
public class BillboardText : MonoBehaviour
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
            // Face the camera
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}

