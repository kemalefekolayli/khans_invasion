using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls smoke sprites that appear when provinces are raided.
/// Uses object pooling for performance.
/// - Initial burst based on loot amount
/// - Continuous spawning every X seconds for 3 turns
/// </summary>
public class RaidSmokeController : MonoBehaviour
{
    [Header("Smoke Sprites")]
    [Tooltip("Drag your smoke sprite images here - will randomly pick from these")]
    [SerializeField] private Sprite[] smokeSprites;
    
    [Header("Rendering")]
    [Tooltip("Sorting order (higher = renders on top of other sprites)")]
    [SerializeField] private int sortingOrder = 100;
    
    [Header("Object Pooling")]
    [Tooltip("Maximum smoke objects in pool")]
    [SerializeField] private int poolSize = 50;
    
    [Header("Initial Raid (based on loot amount)")]
    [Tooltip("Minimum smoke sprites for a weak raid")]
    [SerializeField] private int initialCountMin = 5;
    
    [Tooltip("Maximum smoke sprites for a strong raid")]
    [SerializeField] private int initialCountMax = 15;
    
    [Header("Continuous Smoke")]
    [Tooltip("Time between smoke spawns (seconds)")]
    [SerializeField] private float spawnInterval = 0.75f;
    
    [Tooltip("How many turns smoke continues after raid")]
    [SerializeField] private int smokeDurationTurns = 3;
    
    [Header("Smoke Animation")]
    [Tooltip("How fast smoke rises")]
    [SerializeField] private float riseSpeed = 0.8f;
    
    [Tooltip("How long each smoke sprite lasts")]
    [SerializeField] private float smokeLifetime = 2.5f;
    
    [Tooltip("Starting size of smoke")]
    [SerializeField] private float startSize = 0.3f;
    
    [Tooltip("Maximum size smoke grows to")]
    [SerializeField] private float maxSize = 0.8f;
    
    [Tooltip("Starting opacity (0-1)")]
    [SerializeField] private float startAlpha = 0.7f;
    
    [Header("Spawn Area")]
    [Tooltip("How spread out the smoke spawns horizontally")]
    [SerializeField] private float spawnRadius = 0.5f;
    
    [Tooltip("Height offset above city center")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.3f, 0);
    
    [Header("Debug")]
    [SerializeField] private bool logEvents = true;
    
    // Object pool
    private List<SmokeSprite> pool = new List<SmokeSprite>();
    private Transform poolParent;
    
    // Active smoke sources (provinces being smoked)
    private Dictionary<long, SmokeSource> activeSources = new Dictionary<long, SmokeSource>();
    
    private class SmokeSource
    {
        public ProvinceModel Province;
        public Vector3 Position;
        public int TurnsRemaining;
        public float NextSpawnTime;
        
        public SmokeSource(ProvinceModel province, Vector3 pos, int turns)
        {
            Province = province;
            Position = pos;
            TurnsRemaining = turns;
            NextSpawnTime = Time.time; // Start spawning immediately
        }
    }
    
    private void Awake()
    {
        // Create pool parent
        poolParent = new GameObject("SmokePool").transform;
        poolParent.SetParent(transform);
        
        // Initialize pool
        InitializePool();
    }
    
    private void OnEnable()
    {
        GameEvents.OnProvinceRaided += OnProvinceRaided;
        GameEvents.OnTurnEnded += OnTurnEnded;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
        GameEvents.OnTurnEnded -= OnTurnEnded;
    }
    
    private void Start()
    {
        if (smokeSprites == null || smokeSprites.Length == 0)
        {
            Debug.LogWarning("[RaidSmokeController] No smoke sprites assigned!");
        }
        else
        {
            Debug.Log($"[RaidSmokeController] Initialized with {smokeSprites.Length} sprites, pool size: {poolSize}");
        }
    }
    
    private void Update()
    {
        // Continuously spawn smoke for active sources
        float currentTime = Time.time;
        
        foreach (var kvp in activeSources)
        {
            SmokeSource source = kvp.Value;
            
            // Check if it's time to spawn
            if (currentTime >= source.NextSpawnTime && source.TurnsRemaining > 0)
            {
                SpawnSingleSmoke(source.Position);
                source.NextSpawnTime = currentTime + spawnInterval;
            }
        }
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            SmokeSprite smoke = CreatePooledSmoke();
            smoke.gameObject.SetActive(false);
            pool.Add(smoke);
        }
        
        if (logEvents)
        {
            Debug.Log($"[RaidSmokeController] Created pool of {poolSize} smoke objects");
        }
    }
    
    private SmokeSprite CreatePooledSmoke()
    {
        GameObject obj = new GameObject("PooledSmoke");
        obj.transform.SetParent(poolParent);
        
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        
        SmokeSprite smoke = obj.AddComponent<SmokeSprite>();
        smoke.Initialize(this, sr);
        
        return smoke;
    }
    
    private void OnProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        if (province == null) return;
        
        Vector3 spawnPosition = GetCityCenterPosition(province);
        
        // Calculate initial burst count based on loot
        float maxLoot = RaidManager.Instance != null ? RaidManager.Instance.CalculateMaxLoot(province) : 100f;
        float lootPercent = maxLoot > 0 ? lootAmount / maxLoot : 0.5f;
        int burstCount = Mathf.RoundToInt(Mathf.Lerp(initialCountMin, initialCountMax, lootPercent));
        
        // Spawn initial burst
        for (int i = 0; i < burstCount; i++)
        {
            SpawnSingleSmoke(spawnPosition);
        }
        
        // Track for continuous smoke
        if (!activeSources.ContainsKey(province.provinceId))
        {
            activeSources[province.provinceId] = new SmokeSource(province, spawnPosition, smokeDurationTurns);
        }
        else
        {
            // Reset turns if already exists
            activeSources[province.provinceId].TurnsRemaining = smokeDurationTurns;
        }
        
        if (logEvents)
        {
            Debug.Log($"[RaidSmokeController] 💨 Raid at {province.provinceName}: {burstCount} burst, then continuous for {smokeDurationTurns} turns");
        }
    }
    
    private void OnTurnEnded(int turnNumber)
    {
        List<long> toRemove = new List<long>();
        
        foreach (var kvp in activeSources)
        {
            SmokeSource source = kvp.Value;
            source.TurnsRemaining--;
            
            if (source.TurnsRemaining <= 0)
            {
                toRemove.Add(kvp.Key);
                if (logEvents)
                {
                    Debug.Log($"[RaidSmokeController] Smoke ended for {source.Province.provinceName}");
                }
            }
            else if (logEvents)
            {
                Debug.Log($"[RaidSmokeController] {source.Province.provinceName}: {source.TurnsRemaining} turns of smoke remaining");
            }
        }
        
        foreach (long id in toRemove)
        {
            activeSources.Remove(id);
        }
    }
    
    private void SpawnSingleSmoke(Vector3 basePosition)
    {
        if (smokeSprites == null || smokeSprites.Length == 0) return;
        
        // Get from pool
        SmokeSprite smoke = GetFromPool();
        if (smoke == null) return;
        
        // Random sprite
        int spriteIndex = UnityEngine.Random.Range(0, smokeSprites.Length);
        Sprite sprite = smokeSprites[spriteIndex];
        if (sprite == null) return;
        
        // Random position
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-spawnRadius, spawnRadius),
            UnityEngine.Random.Range(0f, spawnRadius * 0.5f),
            0
        );
        Vector3 spawnPos = basePosition + spawnOffset + randomOffset;
        
        // Activate smoke
        smoke.Activate(sprite, spawnPos, riseSpeed, smokeLifetime, startSize, maxSize, startAlpha);
    }
    
    private SmokeSprite GetFromPool()
    {
        // Find inactive smoke
        foreach (SmokeSprite smoke in pool)
        {
            if (!smoke.gameObject.activeInHierarchy)
            {
                return smoke;
            }
        }
        
        // Pool exhausted - create new one (expand pool)
        if (pool.Count < poolSize * 2) // Allow pool to double if needed
        {
            SmokeSprite smoke = CreatePooledSmoke();
            pool.Add(smoke);
            return smoke;
        }
        
        // Pool at max capacity, reuse oldest active
        return null;
    }
    
    /// <summary>
    /// Called by SmokeSprite when it finishes - returns to pool.
    /// </summary>
    public void ReturnToPool(SmokeSprite smoke)
    {
        smoke.gameObject.SetActive(false);
    }
    
    private Vector3 GetCityCenterPosition(ProvinceModel province)
    {
        CityCenter cityCenter = province.GetComponentInChildren<CityCenter>();
        if (cityCenter != null)
        {
            return cityCenter.transform.position;
        }
        return province.transform.position;
    }
}

/// <summary>
/// Pooled smoke sprite with animation.
/// </summary>
public class SmokeSprite : MonoBehaviour
{
    private RaidSmokeController controller;
    private SpriteRenderer spriteRenderer;
    
    private float riseSpeed;
    private float lifetime;
    private float startSize;
    private float maxSize;
    private float startAlpha;
    
    private float spawnTime;
    private Vector3 startPosition;
    
    public void Initialize(RaidSmokeController ctrl, SpriteRenderer sr)
    {
        controller = ctrl;
        spriteRenderer = sr;
    }
    
    public void Activate(Sprite sprite, Vector3 position, float rise, float life, float sSize, float mSize, float alpha)
    {
        // Set sprite
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
        
        // Set position
        transform.position = position;
        startPosition = position;
        
        // Set animation params
        riseSpeed = rise;
        lifetime = life;
        startSize = sSize;
        maxSize = mSize;
        startAlpha = alpha;
        
        // Random rotation
        transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
        transform.localScale = Vector3.one * startSize;
        
        // Start
        spawnTime = Time.time;
        gameObject.SetActive(true);
    }
    
    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        float progress = elapsed / lifetime;
        
        if (progress >= 1f)
        {
            // Return to pool
            controller.ReturnToPool(this);
            return;
        }
        
        // Rise upward with sway
        float yOffset = elapsed * riseSpeed;
        float xDrift = Mathf.Sin(elapsed * 2f) * 0.1f;
        transform.position = startPosition + new Vector3(xDrift, yOffset, 0);
        
        // Size: grow then shrink
        float sizeCurve;
        if (progress < 0.3f)
        {
            sizeCurve = Mathf.Lerp(startSize, maxSize, progress / 0.3f);
        }
        else
        {
            sizeCurve = Mathf.Lerp(maxSize, maxSize * 0.7f, (progress - 0.3f) / 0.7f);
        }
        transform.localScale = Vector3.one * sizeCurve;
        
        // Fade out
        float alpha = Mathf.Lerp(startAlpha, 0f, progress);
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
        
        // Slow rotation
        transform.Rotate(0, 0, 15f * Time.deltaTime);
    }
}
