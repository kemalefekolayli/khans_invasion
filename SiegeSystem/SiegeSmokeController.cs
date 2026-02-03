using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls smoke sprites that appear when provinces are being sieged.
/// Uses object pooling for performance.
/// - For fortress sieges: spawns smoke every 0.75s during siege
/// - Smoke intensity based on population
/// - Stops 1 turn after successful siege
/// </summary>
public class SiegeSmokeController : MonoBehaviour
{
    [Header("Smoke Sprites")]
    [Tooltip("Drag your smoke sprite images here - will randomly pick from these")]
    [SerializeField] private Sprite[] smokeSprites;
    
    [Header("Rendering")]
    [Tooltip("Sorting order (higher = renders on top of other sprites)")]
    [SerializeField] private int sortingOrder = 100;
    
    [Header("Object Pooling")]
    [Tooltip("Maximum smoke objects in pool")]
    [SerializeField] private int poolSize = 60;
    
    [Header("Fortress Siege Smoke")]
    [Tooltip("Time between smoke spawns for fortress sieges (seconds)")]
    [SerializeField] private float fortressSpawnInterval = 0.75f;
    
    [Header("Population-Based Intensity")]
    [Tooltip("Minimum smoke sprites per spawn for low population")]
    [SerializeField] private int minSmokesPerSpawn = 1;
    
    [Tooltip("Maximum smoke sprites per spawn for high population")]
    [SerializeField] private int maxSmokesPerSpawn = 4;
    
    [Tooltip("Population threshold for minimum smoke")]
    [SerializeField] private float minPopulationThreshold = 1000f;
    
    [Tooltip("Population threshold for maximum smoke")]
    [SerializeField] private float maxPopulationThreshold = 10000f;
    
    [Header("Conquest Smoke")]
    [Tooltip("Initial burst of smoke on conquest")]
    [SerializeField] private int conquestBurstCount = 10;
    
    [Tooltip("Turns smoke continues after conquest")]
    [SerializeField] private int conquestSmokeTurns = 1;
    
    [Header("Smoke Animation")]
    [Tooltip("How fast smoke rises")]
    [SerializeField] private float riseSpeed = 0.9f;
    
    [Tooltip("How long each smoke sprite lasts")]
    [SerializeField] private float smokeLifetime = 2.8f;
    
    [Tooltip("Starting size of smoke")]
    [SerializeField] private float startSize = 0.35f;
    
    [Tooltip("Maximum size smoke grows to")]
    [SerializeField] private float maxSize = 0.9f;
    
    [Tooltip("Starting opacity (0-1)")]
    [SerializeField] private float startAlpha = 0.75f;
    
    [Header("Spawn Area")]
    [Tooltip("How spread out the smoke spawns horizontally")]
    [SerializeField] private float spawnRadius = 0.6f;
    
    [Tooltip("Height offset above city center")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.3f, 0);
    
    [Header("Debug")]
    [SerializeField] private bool logEvents = true;
    
    // Object pool
    private List<SiegeSmokeSprite> pool = new List<SiegeSmokeSprite>();
    private Transform poolParent;
    
    // Active siege smoke sources (provinces being sieged)
    private Dictionary<long, SiegeSmokeSource> activeSiegeSources = new Dictionary<long, SiegeSmokeSource>();
    
    // Conquest smoke sources (after conquest)
    private Dictionary<long, ConquestSmokeSource> conquestSources = new Dictionary<long, ConquestSmokeSource>();
    
    private class SiegeSmokeSource
    {
        public ProvinceModel Province;
        public Vector3 Position;
        public float NextSpawnTime;
        public int SmokesPerSpawn;
        public bool IsFortress;
        
        public SiegeSmokeSource(ProvinceModel province, Vector3 pos, int smokesPerSpawn, bool isFortress)
        {
            Province = province;
            Position = pos;
            SmokesPerSpawn = smokesPerSpawn;
            IsFortress = isFortress;
            NextSpawnTime = Time.time;
        }
    }
    
    private class ConquestSmokeSource
    {
        public ProvinceModel Province;
        public Vector3 Position;
        public int TurnsRemaining;
        public float NextSpawnTime;
        
        public ConquestSmokeSource(ProvinceModel province, Vector3 pos, int turns)
        {
            Province = province;
            Position = pos;
            TurnsRemaining = turns;
            NextSpawnTime = Time.time;
        }
    }
    
    private void Awake()
    {
        // Create pool parent
        poolParent = new GameObject("SiegeSmokePool").transform;
        poolParent.SetParent(transform);
        
        // Initialize pool
        InitializePool();
    }
    
    private void OnEnable()
    {
        GameEvents.OnProvinceSieged += OnProvinceSieged;
        GameEvents.OnProvinceConquered += OnProvinceConquered;
        GameEvents.OnSiegeCancelled += OnSiegeCancelled;
        GameEvents.OnTurnEnded += OnTurnEnded;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvinceSieged -= OnProvinceSieged;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
        GameEvents.OnSiegeCancelled -= OnSiegeCancelled;
        GameEvents.OnTurnEnded -= OnTurnEnded;
    }
    
    private void Start()
    {
        if (smokeSprites == null || smokeSprites.Length == 0)
        {
            Debug.LogWarning("[SiegeSmokeController] No smoke sprites assigned!");
        }
        else
        {
            Debug.Log($"[SiegeSmokeController] Initialized with {smokeSprites.Length} sprites, pool size: {poolSize}");
        }
    }
    
    private void Update()
    {
        float currentTime = Time.time;
        
        // Spawn smoke for active fortress sieges
        foreach (var kvp in activeSiegeSources)
        {
            SiegeSmokeSource source = kvp.Value;
            
            if (source.IsFortress && currentTime >= source.NextSpawnTime)
            {
                for (int i = 0; i < source.SmokesPerSpawn; i++)
                {
                    SpawnSingleSmoke(source.Position);
                }
                source.NextSpawnTime = currentTime + fortressSpawnInterval;
            }
        }
        
        // Spawn smoke for conquered provinces
        foreach (var kvp in conquestSources)
        {
            ConquestSmokeSource source = kvp.Value;
            
            if (source.TurnsRemaining > 0 && currentTime >= source.NextSpawnTime)
            {
                SpawnSingleSmoke(source.Position);
                source.NextSpawnTime = currentTime + fortressSpawnInterval;
            }
        }
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            SiegeSmokeSprite smoke = CreatePooledSmoke();
            smoke.gameObject.SetActive(false);
            pool.Add(smoke);
        }
        
        if (logEvents)
        {
            Debug.Log($"[SiegeSmokeController] Created pool of {poolSize} smoke objects");
        }
    }
    
    private SiegeSmokeSprite CreatePooledSmoke()
    {
        GameObject obj = new GameObject("PooledSiegeSmoke");
        obj.transform.SetParent(poolParent);
        
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        
        SiegeSmokeSprite smoke = obj.AddComponent<SiegeSmokeSprite>();
        smoke.Initialize(this, sr);
        
        return smoke;
    }
    
    private void OnProvinceSieged(ProvinceModel province, General attacker, float defenseStrength)
    {
        if (province == null) return;
        
        // Check if fortress siege
        bool isFortress = SiegeManager.Instance?.HasFortress(province) ?? false;
        
        // Only spawn continuous smoke for fortress sieges
        if (!isFortress) return;
        
        Vector3 spawnPosition = GetCityCenterPosition(province);
        
        // Calculate smoke intensity based on population
        int smokesPerSpawn = CalculateSmokesBasedOnPopulation(province);
        
        // Add to active siege sources
        if (!activeSiegeSources.ContainsKey(province.provinceId))
        {
            activeSiegeSources[province.provinceId] = new SiegeSmokeSource(province, spawnPosition, smokesPerSpawn, isFortress);
            
            if (logEvents)
            {
                Debug.Log($"[SiegeSmokeController] 💨 Fortress siege at {province.provinceName}: {smokesPerSpawn} smokes per spawn");
            }
        }
    }
    
    private void OnProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (province == null) return;
        
        // Remove from siege sources
        activeSiegeSources.Remove(province.provinceId);
        
        Vector3 spawnPosition = GetCityCenterPosition(province);
        
        // Spawn initial burst
        int burstCount = Mathf.RoundToInt(conquestBurstCount * GetPopulationMultiplier(province));
        for (int i = 0; i < burstCount; i++)
        {
            SpawnSingleSmoke(spawnPosition);
        }
        
        // Add to conquest sources for continued smoke
        conquestSources[province.provinceId] = new ConquestSmokeSource(province, spawnPosition, conquestSmokeTurns);
        
        if (logEvents)
        {
            Debug.Log($"[SiegeSmokeController] 💨 Province conquered: {province.provinceName}, burst: {burstCount}, continues for {conquestSmokeTurns} turns");
        }
    }
    
    private void OnSiegeCancelled(ProvinceModel province)
    {
        if (province == null) return;
        
        // Remove from active siege sources
        if (activeSiegeSources.Remove(province.provinceId))
        {
            if (logEvents)
            {
                Debug.Log($"[SiegeSmokeController] Siege cancelled at {province.provinceName}, stopping smoke");
            }
        }
    }
    
    private void OnTurnEnded(int turnNumber)
    {
        List<long> toRemove = new List<long>();
        
        foreach (var kvp in conquestSources)
        {
            ConquestSmokeSource source = kvp.Value;
            source.TurnsRemaining--;
            
            if (source.TurnsRemaining <= 0)
            {
                toRemove.Add(kvp.Key);
                if (logEvents)
                {
                    Debug.Log($"[SiegeSmokeController] Conquest smoke ended for {source.Province.provinceName}");
                }
            }
        }
        
        foreach (long id in toRemove)
        {
            conquestSources.Remove(id);
        }
    }
    
    private int CalculateSmokesBasedOnPopulation(ProvinceModel province)
    {
        float pop = province.provinceCurrentPop;
        float t = Mathf.InverseLerp(minPopulationThreshold, maxPopulationThreshold, pop);
        return Mathf.RoundToInt(Mathf.Lerp(minSmokesPerSpawn, maxSmokesPerSpawn, t));
    }
    
    private float GetPopulationMultiplier(ProvinceModel province)
    {
        float pop = province.provinceCurrentPop;
        return Mathf.Lerp(0.5f, 2f, Mathf.InverseLerp(minPopulationThreshold, maxPopulationThreshold, pop));
    }
    
    private void SpawnSingleSmoke(Vector3 basePosition)
    {
        if (smokeSprites == null || smokeSprites.Length == 0) return;
        
        // Get from pool
        SiegeSmokeSprite smoke = GetFromPool();
        if (smoke == null) return;
        
        // Random sprite
        int spriteIndex = Random.Range(0, smokeSprites.Length);
        Sprite sprite = smokeSprites[spriteIndex];
        if (sprite == null) return;
        
        // Random position
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(0f, spawnRadius * 0.5f),
            0
        );
        Vector3 spawnPos = basePosition + spawnOffset + randomOffset;
        
        // Activate smoke
        smoke.Activate(sprite, spawnPos, riseSpeed, smokeLifetime, startSize, maxSize, startAlpha);
    }
    
    private SiegeSmokeSprite GetFromPool()
    {
        // Find inactive smoke
        foreach (SiegeSmokeSprite smoke in pool)
        {
            if (!smoke.gameObject.activeInHierarchy)
            {
                return smoke;
            }
        }
        
        // Pool exhausted - create new one (expand pool)
        if (pool.Count < poolSize * 2)
        {
            SiegeSmokeSprite smoke = CreatePooledSmoke();
            pool.Add(smoke);
            return smoke;
        }
        
        // Pool at max capacity
        return null;
    }
    
    /// <summary>
    /// Called by SiegeSmokeSprite when it finishes - returns to pool.
    /// </summary>
    public void ReturnToPool(SiegeSmokeSprite smoke)
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
/// Pooled siege smoke sprite with animation.
/// </summary>
public class SiegeSmokeSprite : MonoBehaviour
{
    private SiegeSmokeController controller;
    private SpriteRenderer spriteRenderer;
    
    private float riseSpeed;
    private float lifetime;
    private float startSize;
    private float maxSize;
    private float startAlpha;
    
    private float spawnTime;
    private Vector3 startPosition;
    
    public void Initialize(SiegeSmokeController ctrl, SpriteRenderer sr)
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
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
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
        float xDrift = Mathf.Sin(elapsed * 2f) * 0.12f;
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
        transform.Rotate(0, 0, 18f * Time.deltaTime);
    }
}
