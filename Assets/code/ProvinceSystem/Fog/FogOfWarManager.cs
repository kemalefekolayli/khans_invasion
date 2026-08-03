// FogOfWar/FogOfWarManager.cs
using UnityEngine;
using System.Collections.Generic;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    [Header("Fog Settings")]
    public Color fogColor = new Color(0.08f, 0.08f, 0.1f, 1f);
    public float revealSpeed = 2f;

    [Header("Border Peek Effect")]
    [Range(0f, 0.3f)]
    public float borderPeekBrightness = 0.15f; // How much lighter than full fog

private Dictionary<ProvinceModel, FogState> provinceFogStates = new Dictionary<ProvinceModel, FogState>();
    private HashSet<ProvinceModel> discoveredProvinces = new HashSet<ProvinceModel>();
    private HashSet<ProvinceModel> activeLerpProvinces = new HashSet<ProvinceModel>();
    private List<ProvinceModel> settledProvinces = new List<ProvinceModel>();
    private const float ColorEpsilon = 0.001f;
    private bool fogInitialized = false;  // NEW: Guard against duplicate initialization
    public bool IsFogActive => isActiveAndEnabled && fogInitialized;

    private class FogState
    {
        public Color targetColor;
        public bool isRevealing;
        public bool isBorderPeek;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnProvincesAssigned += OnProvincesAssigned;
        GameEvents.OnProvinceEnter += OnProvinceEnter;

    }

    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= OnProvincesAssigned;
        GameEvents.OnProvinceEnter -= OnProvinceEnter;

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnProvincesAssigned()
    {
        if (fogInitialized)  // NEW: Prevent duplicate initialization
        {
            GameLog.Log(GameLogCategory.Core, "[FogOfWarManager] Already initialized, skipping duplicate call.");
            return;
        }
        fogInitialized = true;  // NEW: Mark as initialized
        StartCoroutine(InitializeFog());
    }

    private System.Collections.IEnumerator InitializeFog()
    {
        yield return null; // Wait one frame
        
        ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        
        // Get player nation reference
        PlayerNation playerNation = PlayerNation.Instance;
        NationModel playerNationModel = playerNation?.currentNation;
        
        int foggedCount = 0;
        int playerOwnedCount = 0;
        
        foreach (var province in allProvinces)
        {
            if (province.CompareTag("River")) continue;
            if (province.spriteRenderer == null) continue;
            
            // Store original target color
            Color nationColor = province.spriteRenderer.color;
            
            // Check if province is owned by player - auto-discover it
            bool isPlayerOwned = (playerNationModel != null && province.provinceOwner == playerNationModel);
            
            if (isPlayerOwned)
            {
                // Player-owned provinces start revealed
                discoveredProvinces.Add(province);
                playerOwnedCount++;
                
                provinceFogStates[province] = new FogState
                {
                    targetColor = nationColor,
                    isRevealing = true,  // Already revealing
                    isBorderPeek = false
                };
                
                // Keep nation color (no fog)
                province.spriteRenderer.color = nationColor;
            }
            else
            {
                // Non-player provinces start fogged
                foggedCount++;
                
                provinceFogStates[province] = new FogState
                {
                    targetColor = nationColor,
                    isRevealing = false,
                    isBorderPeek = false
                };
                
                // Set initial fog color
                province.spriteRenderer.color = fogColor;
            }
        }
        
        // After setting up player provinces, update adjacent provinces to border peek
        UpdateAdjacentProvinces();
        

    }

    private void OnProvinceEnter(ProvinceModel province)
    {
        if (province == null) return;
        DiscoverProvince(province);
    }

    public void DiscoverProvince(ProvinceModel province)
    {
        if (province == null || discoveredProvinces.Contains(province)) return;
        
        discoveredProvinces.Add(province);
        
        if (provinceFogStates.TryGetValue(province, out FogState state))
        {
            state.isRevealing = true;
            state.isBorderPeek = false;
            activeLerpProvinces.Add(province);
        }
        
        // Update neighbors to border peek mode
        UpdateAdjacentProvinces();
    }
    
    private void UpdateAdjacentProvinces()
    {
        foreach (var kvp in provinceFogStates)
        {
            ProvinceModel province = kvp.Key;
            FogState state = kvp.Value;
            
            if (discoveredProvinces.Contains(province)) continue;
            if (state.isBorderPeek) continue;
            
            if (IsAdjacentToDiscovered(province))
            {
                state.isBorderPeek = true;
                activeLerpProvinces.Add(province);
            }
        }
    }

    private bool IsAdjacentToDiscovered(ProvinceModel province)
    {
        // Check by collider overlap
        Collider2D provinceCollider = province.GetComponent<Collider2D>();
        if (provinceCollider == null) return false;
        
        Bounds bounds = provinceCollider.bounds;
        bounds.Expand(0.3f);
        
        Collider2D[] nearby = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        
        foreach (var col in nearby)
        {
            if (col.gameObject == province.gameObject) continue;
            
            ProvinceModel other = col.GetComponent<ProvinceModel>();
            if (other != null && discoveredProvinces.Contains(other))
            {
                return true;
            }
        }
        
        return false;
    }

    private void Update()
    {
        if (activeLerpProvinces.Count == 0) return;

        foreach (var province in activeLerpProvinces)
        {
            if (province == null || province.spriteRenderer == null) continue;

            if (!provinceFogStates.TryGetValue(province, out FogState state))
            {
                settledProvinces.Add(province);
                continue;
            }

            Color currentColor = province.spriteRenderer.color;
            Color target;

            if (state.isRevealing)
            {
                // Fully reveal to nation color
                target = state.targetColor;
            }
            else if (state.isBorderPeek)
            {
                // Slightly lighter fog for border peek
                target = Color.Lerp(fogColor, state.targetColor, borderPeekBrightness);
            }
            else
            {
                settledProvinces.Add(province);
                continue; // Stay at fog color
            }

            // Lerp toward target
            if (ColorsNearlyEqual(currentColor, target))
            {
                settledProvinces.Add(province);
                continue;
            }

            Color newColor = Color.Lerp(currentColor, target, Time.deltaTime * revealSpeed);
            province.spriteRenderer.color = newColor;
        }

        if (settledProvinces.Count > 0)
        {
            for (int i = 0; i < settledProvinces.Count; i++)
            {
                activeLerpProvinces.Remove(settledProvinces[i]);
            }
            settledProvinces.Clear();
        }
    }

    private static bool ColorsNearlyEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < ColorEpsilon
            && Mathf.Abs(a.g - b.g) < ColorEpsilon
            && Mathf.Abs(a.b - b.b) < ColorEpsilon
            && Mathf.Abs(a.a - b.a) < ColorEpsilon;
    }

    public void SetProvinceBaseColor(ProvinceModel province, Color baseColor)
    {
        if (province == null || province.spriteRenderer == null) return;

        if (!IsFogActive || !provinceFogStates.TryGetValue(province, out FogState state))
        {
            province.spriteRenderer.color = baseColor;
            return;
        }

        state.targetColor = baseColor;
        if (state.isRevealing || state.isBorderPeek)
            activeLerpProvinces.Add(province);
    }

    public Color GetVisibleBaseColor(ProvinceModel province)
    {
        if (province == null) return Color.gray;
        if (!IsFogActive) return province.spriteRenderer != null ? province.spriteRenderer.color : province.provinceColor;
        if (!provinceFogStates.TryGetValue(province, out FogState state)) return fogColor;
        if (state.isRevealing) return state.targetColor;
        if (state.isBorderPeek) return Color.Lerp(fogColor, state.targetColor, borderPeekBrightness);
        return fogColor;
    }
    public bool IsDiscovered(ProvinceModel province)
    {
        if (!IsFogActive) return true;
        return discoveredProvinces.Contains(province);
    }
}
