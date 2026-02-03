using UnityEngine;
using TMPro;

/// <summary>
/// Spawns floating text when siege events occur.
/// Shows "SIEGE STARTED!" when siege begins and "SIEGE SUCCESSFUL!" when conquered.
/// Similar pattern to LootPopupSpawner.
/// </summary>
public class SiegePopupSpawner : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color startedColor = new Color(1f, 0.7f, 0.2f); // Orange - siege started
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f); // Green - siege complete
    [SerializeField] private Color failureColor = new Color(0.9f, 0.2f, 0.2f); // Red - siege failed
    [SerializeField] private Color casualtyColor = new Color(0.8f, 0.3f, 0.3f); // Darker red - casualties
    [SerializeField] private Color cancelledColor = new Color(0.7f, 0.5f, 0.2f); // Brown - siege cancelled
    
    [Header("Appearance")]
    [SerializeField] private float fontSize = 4f;
    [SerializeField] private float casualtyFontSize = 3.5f;
    [SerializeField] private float outlineWidth = 0.2f;
    
    [Header("Animation")]
    [SerializeField] private float riseSpeed = 1.2f;
    [SerializeField] private float lifetime = 3f;
    
    [Header("Position Offset")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 2.5f, 0);
    
    private void OnEnable()
    {
        GameEvents.OnProvinceSieged += OnProvinceSieged;
        GameEvents.OnSiegeFailed += OnSiegeFailed;
        GameEvents.OnProvinceConquered += OnProvinceConquered;
        GameEvents.OnSiegeCancelled += OnSiegeCancelled;
        GameEvents.OnSiegeCasualties += OnSiegeCasualties;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceSieged -= OnProvinceSieged;
        GameEvents.OnSiegeFailed -= OnSiegeFailed;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
        GameEvents.OnSiegeCancelled -= OnSiegeCancelled;
        GameEvents.OnSiegeCasualties -= OnSiegeCasualties;
    }
    
    /// <summary>
    /// Called when siege starts successfully.
    /// </summary>
    private void OnProvinceSieged(ProvinceModel province, General attacker, float defenseStrength)
    {
        if (province == null) return;
        
        Vector3 spawnPosition = GetSpawnPosition(province);
        
        // Check if fortress siege (multi-turn)
        int turnsRemaining = SiegeManager.Instance?.GetSiegeTurnsRemaining(province) ?? 1;
        
        if (turnsRemaining > 1)
        {
            SpawnSiegeText($"SIEGE STARTED!\n({turnsRemaining} turns)", spawnPosition, startedColor);
        }
        else
        {
            SpawnSiegeText("SIEGE STARTED!", spawnPosition, startedColor);
        }
        
        Debug.Log($"[SiegePopupSpawner] Spawned 'SIEGE STARTED!' at {province.provinceName}");
    }
    
    /// <summary>
    /// Called when siege attempt fails.
    /// </summary>
    private void OnSiegeFailed(ProvinceModel province, General attacker, SiegeManager.SiegeResult result)
    {
        if (province == null) return;
        
        // Get failure message from SiegeManager
        string message = SiegeManager.Instance != null 
            ? SiegeManager.Instance.GetSiegeFailureMessage(result, province)
            : "Siege failed!";
        
        Vector3 spawnPosition = GetSpawnPosition(province);
        SpawnSiegeText(message, spawnPosition, failureColor);
        
        Debug.Log($"[SiegePopupSpawner] Spawned failure popup at {province.provinceName}: {message}");
    }
    
    /// <summary>
    /// Called when province is successfully conquered (next turn).
    /// </summary>
    private void OnProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (province == null) return;
        
        Vector3 spawnPosition = GetSpawnPosition(province);
        SpawnSiegeText("SIEGE SUCCESSFUL!", spawnPosition, successColor);
        
        Debug.Log($"[SiegePopupSpawner] Spawned 'SIEGE SUCCESSFUL!' at {province.provinceName}");
    }
    
    /// <summary>
    /// Called when siege is cancelled (army left province).
    /// </summary>
    private void OnSiegeCancelled(ProvinceModel province)
    {
        if (province == null) return;
        
        Vector3 spawnPosition = GetSpawnPosition(province);
        SpawnSiegeText("SIEGE ABANDONED!", spawnPosition, cancelledColor);
        
        Debug.Log($"[SiegePopupSpawner] Spawned 'SIEGE ABANDONED!' at {province.provinceName}");
    }
    
    /// <summary>
    /// Called when army takes casualties during ongoing siege.
    /// </summary>
    private void OnSiegeCasualties(ProvinceModel province, General general, int casualties, int turnsRemaining)
    {
        if (province == null || general == null) return;
        
        Vector3 spawnPosition = GetSpawnPosition(province);
        string message = $"-{casualties} troops ({turnsRemaining} turns left)";
        SpawnSiegeText(message, spawnPosition, casualtyColor, casualtyFontSize);
        
        Debug.Log($"[SiegePopupSpawner] Spawned casualty popup at {province.provinceName}: {message}");
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
    
    private void SpawnSiegeText(string message, Vector3 worldPosition, Color color, float fontSizeOverride = -1f)
    {
        // Create text object
        GameObject textObj = new GameObject($"SiegeText_{message}");
        textObj.transform.position = worldPosition;
        
        // Add TextMeshPro component (3D World Space)
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = fontSizeOverride > 0 ? fontSizeOverride : fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        // Outline for visibility
        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = Color.black;
        
        // Sorting order to appear above other sprites
        tmp.sortingOrder = 100;
        
        // Add floating behavior
        FloatingSiegeText floatScript = textObj.AddComponent<FloatingSiegeText>();
        floatScript.Initialize(worldPosition, riseSpeed, lifetime);
        
        // Billboard (face camera)
        textObj.AddComponent<SiegeBillboard>();
    }
}

/// <summary>
/// Floating text behavior for siege popups.
/// </summary>
public class FloatingSiegeText : MonoBehaviour
{
    private Vector3 startPosition;
    private float riseSpeed;
    private float lifetime;
    private float elapsedTime;
    private TextMeshPro textMesh;
    
    public void Initialize(Vector3 start, float speed, float life)
    {
        startPosition = start;
        riseSpeed = speed;
        lifetime = life;
        elapsedTime = 0f;
        textMesh = GetComponent<TextMeshPro>();
    }
    
    private void Update()
    {
        elapsedTime += Time.deltaTime;
        
        // Rise upward
        transform.position = startPosition + Vector3.up * (elapsedTime * riseSpeed);
        
        // Fade out in last portion of lifetime
        if (textMesh != null && elapsedTime > lifetime * 0.6f)
        {
            float fadeProgress = (elapsedTime - lifetime * 0.6f) / (lifetime * 0.4f);
            Color c = textMesh.color;
            c.a = 1f - fadeProgress;
            textMesh.color = c;
        }
        
        // Destroy when lifetime expires
        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Billboard for siege text to face camera.
/// </summary>
public class SiegeBillboard : MonoBehaviour
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
