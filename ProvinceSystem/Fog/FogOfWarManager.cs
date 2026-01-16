// FogOfWar/FogOfWarManager.cs
using UnityEngine;
using System.Collections.Generic;

public class FogOfWarManager : MonoBehaviour
{
    [Header("Fog Settings")]
    public Color fogColor = new Color(0.08f, 0.08f, 0.1f, 1f);
    public float revealSpeed = 2f;
    
    [Header("Border Peek Effect")]
    [Range(0f, 0.3f)]
    public float borderPeekBrightness = 0.15f; // How much lighter than full fog
    
    private Dictionary<ProvinceModel, FogState> provinceFogStates = new Dictionary<ProvinceModel, FogState>();
    private HashSet<ProvinceModel> discoveredProvinces = new HashSet<ProvinceModel>();
    private bool fogInitialized = false;  // NEW: Guard against duplicate initialization

    private class FogState
    {
        public ProvinceModel province;
        public Color targetColor;
        public bool isRevealing;
        public bool isBorderPeek;
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

    private void OnProvincesAssigned()
    {
        if (fogInitialized)  // NEW: Prevent duplicate initialization
        {
            Debug.Log("[FogOfWarManager] Already initialized, skipping duplicate call.");
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
            Color nationColor = GetNationColor(province);
            
            // Check if province is owned by player - auto-discover it
            bool isPlayerOwned = (playerNationModel != null && province.provinceOwner == playerNationModel);
            
            if (isPlayerOwned)
            {
                // Player-owned provinces start revealed
                discoveredProvinces.Add(province);
                playerOwnedCount++;
                
                provinceFogStates[province] = new FogState
                {
                    province = province,
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
                    province = province,
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
        
        Debug.Log($"✓ FogOfWar initialized: {playerOwnedCount} player provinces revealed, {foggedCount} provinces fogged");
    }

    private Color GetNationColor(ProvinceModel province)
    {
        if (province.provinceOwner != null && !string.IsNullOrEmpty(province.provinceOwner.nationColor))
        {
            return NationLoader.HexToColor(province.provinceOwner.nationColor);
        }
        return province.provinceColor;
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
        foreach (var kvp in provinceFogStates)
        {
            FogState state = kvp.Value;
            if (state.province == null || state.province.spriteRenderer == null) continue;
            
            Color currentColor = state.province.spriteRenderer.color;
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
                continue; // Stay at fog color
            }
            
            // Lerp toward target
            if (currentColor != target)
            {
                Color newColor = Color.Lerp(currentColor, target, Time.deltaTime * revealSpeed);
                state.province.spriteRenderer.color = newColor;
            }
        }
    }

    public bool IsDiscovered(ProvinceModel province)
    {
        return discoveredProvinces.Contains(province);
    }
}