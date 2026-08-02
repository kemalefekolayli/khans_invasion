using UnityEngine;
using TMPro;

/// <summary>
/// Spawns floating text when siege events occur.
/// Shows "SIEGE STARTED!" when siege begins and "SIEGE SUCCESSFUL!" when conquered.
/// Similar pattern to LootPopupSpawner. Popups are pooled and reused to avoid GC churn.
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
    
    [Header("Object Pooling")]
    [SerializeField] private int poolSize = 20;
    
    private ComponentPool<FloatingSiegeText> pool;
    
    private void Awake()
    {
        pool = new ComponentPool<FloatingSiegeText>("SiegePopupPool", transform, poolSize, CreatePopup);
    }
    
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
        
        GameLog.Log(GameLogCategory.Core, $"[SiegePopupSpawner] Spawned 'SIEGE STARTED!' at {province.provinceName}");
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
        
        GameLog.Log(GameLogCategory.Core, $"[SiegePopupSpawner] Spawned failure popup at {province.provinceName}: {message}");
    }
    
    /// <summary>
    /// Called when province is successfully conquered (next turn).
    /// </summary>
    private void OnProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (province == null) return;
        
        Vector3 spawnPosition = GetSpawnPosition(province);
        SpawnSiegeText("SIEGE SUCCESSFUL!", spawnPosition, successColor);
        
        GameLog.Log(GameLogCategory.Core, $"[SiegePopupSpawner] Spawned 'SIEGE SUCCESSFUL!' at {province.provinceName}");
    }
    
    /// <summary>
    /// Called when siege is cancelled (army left province).
    /// </summary>
    private void OnSiegeCancelled(ProvinceModel province)
    {
        if (province == null) return;
        
        Vector3 spawnPosition = GetSpawnPosition(province);
        SpawnSiegeText("SIEGE ABANDONED!", spawnPosition, cancelledColor);
        
        GameLog.Log(GameLogCategory.Core, $"[SiegePopupSpawner] Spawned 'SIEGE ABANDONED!' at {province.provinceName}");
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
        
        GameLog.Log(GameLogCategory.Core, $"[SiegePopupSpawner] Spawned casualty popup at {province.provinceName}: {message}");
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
        FloatingSiegeText popup = GetFromPool();
        if (popup == null) return;
        
        // Configure text for this message
        TextMeshPro tmp = popup.GetComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = fontSizeOverride > 0 ? fontSizeOverride : fontSize;
        tmp.color = color;
        
        popup.Initialize(worldPosition, riseSpeed, lifetime);
    }
    
    private FloatingSiegeText GetFromPool()
    {
        FloatingSiegeText popup = pool.Get();
        if (popup != null)
        {
            popup.BindPool(pool);
        }
        return popup;
    }
    
    private FloatingSiegeText CreatePopup(Transform parent)
    {
        // Create text object
        GameObject textObj = new GameObject("SiegePopup");
        textObj.transform.SetParent(parent);
        
        // Add TextMeshPro component (3D World Space)
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        // Outline for visibility
        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = Color.black;
        
        // Sorting order to appear above other sprites
        tmp.sortingOrder = 100;
        
        // Add floating behavior
        FloatingSiegeText floatScript = textObj.AddComponent<FloatingSiegeText>();
        
        // Billboard (face camera)
        textObj.AddComponent<SiegeBillboard>();
        
        return floatScript;
    }
}

/// <summary>
/// Floating text behavior for siege popups.
/// Returns itself to its pool when the animation finishes.
/// </summary>
public class FloatingSiegeText : MonoBehaviour
{
    private ComponentPool<FloatingSiegeText> pool;
    private Vector3 startPosition;
    private float riseSpeed;
    private float lifetime;
    private float elapsedTime;
    private TextMeshPro textMesh;
    
    /// <summary>
    /// Associates this popup with a pool so it returns instead of being destroyed.
    /// </summary>
    public void BindPool(ComponentPool<FloatingSiegeText> popupPool)
    {
        pool = popupPool;
    }
    
    public void Initialize(Vector3 start, float speed, float life)
    {
        startPosition = start;
        riseSpeed = speed;
        lifetime = life;
        elapsedTime = 0f;
        
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            // Reset alpha that may have faded out during the previous use
            Color c = textMesh.color;
            c.a = 1f;
            textMesh.color = c;
        }
        
        transform.position = start;
        
        // Activate (pooled popups start inactive)
        gameObject.SetActive(true);
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
        
        // Return to pool when lifetime expires (destroy if not pooled)
        if (elapsedTime >= lifetime)
        {
            if (pool != null)
            {
                pool.Return(this);
            }
            else
            {
                Destroy(gameObject);
            }
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
