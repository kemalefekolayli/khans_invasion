using UnityEngine;
using System;

public class PlayerNation : MonoBehaviour
{
    [Header("Current Nation Reference")]
    [NonSerialized] // Don't show in Inspector - will be set at runtime
    public NationModel currentNation;
    
    [Header("Player-Specific Data")]
    public float nationMoney;
    public int currentTurn = 1;
    
    [Header("Initialization")]
    public int startingNationId = 0;
    
    // Singleton for easy access
    public static PlayerNation Instance { get; private set; }

    // Convenience properties - delegate to currentNation
    public long NationId => currentNation?.nationId ?? 0;
    public string NationName => currentNation?.nationName ?? "No Nation";
    public string NationColor => currentNation?.nationColor ?? "#808080";
    public int CityCount => currentNation?.provinceList.Count ?? 0;
    
    // Calculated properties
    public float TaxIncome { get; private set; }
    public float TradeIncome { get; private set; }
    public float TotalIncome => TaxIncome + TradeIncome;
    public float PopulationSize { get; private set; }
    public float ArmySize { get; private set; }
    public float ArmyStrength { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✓ PlayerNation singleton created");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        Debug.Log("PlayerNation.OnEnable - subscribing to events");
        GameEvents.OnProvincesAssigned += OnProvincesAssigned;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= OnProvincesAssigned;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
    }

    private void OnProvincesAssigned()
    {
        Debug.Log("PlayerNation received OnProvincesAssigned event");
        InitializePlayer();
    }

    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (currentNation != null && (oldOwner == currentNation || newOwner == currentNation))
        {
            RecalculateStats();
            GameEvents.PlayerStatsChanged();
        }
    }

    private void InitializePlayer()
    {
        Debug.Log($"InitializePlayer called. startingNationId = {startingNationId}");
        
        if (currentNation == null)
        {
            NationLoader loader = FindFirstObjectByType<NationLoader>();
            
            if (loader == null)
            {
                Debug.LogError("NationLoader not found!");
                return;
            }
            
            Debug.Log($"NationLoader found. Total nations: {loader.allNations.Count}");
            Debug.Log($"Nations in dictionary: {loader.nationsById.Count}");
            
            // Debug: print all nation IDs
            foreach (var kvp in loader.nationsById)
            {
                Debug.Log($"  - Nation ID {kvp.Key}: {kvp.Value.nationName}, provinces: {kvp.Value.provinceList.Count}");
            }
            
            currentNation = loader.GetNationById(startingNationId);
            
            if (currentNation != null)
            {
                currentNation.isPlayer = true;
                Debug.Log($"✓ Player initialized as: {currentNation.nationName} (ID: {currentNation.nationId})");
                Debug.Log($"✓ Player owns {currentNation.provinceList.Count} provinces");
            }
            else
            {
                Debug.LogError($"GetNationById({startingNationId}) returned null!");
                return;
            }
        }
        else
        {
            Debug.Log($"currentNation already set: {currentNation.nationName}");
        }
        
        RecalculateStats();
        GameEvents.PlayerNationReady();
    }

    public void SetNation(NationModel nation)
    {
        if (currentNation != null)
        {
            currentNation.isPlayer = false;
        }
        
        currentNation = nation;
        
        if (currentNation != null)
        {
            currentNation.isPlayer = true;
            Debug.Log($"✓ Player nation changed to: {currentNation.nationName}");
        }
        
        RecalculateStats();
        GameEvents.PlayerNationChanged(currentNation);
        GameEvents.PlayerStatsChanged();
    }

    public void SetNationById(int nationId)
    {
        NationLoader loader = FindFirstObjectByType<NationLoader>();
        if (loader != null)
        {
            NationModel nation = loader.GetNationById(nationId);
            if (nation != null)
            {
                SetNation(nation);
            }
            else
            {
                Debug.LogError($"Nation with ID {nationId} not found!");
            }
        }
    }

    public void RecalculateStats()
    {
        if (currentNation == null)
        {
            Debug.LogWarning("RecalculateStats: currentNation is null!");
            return;
        }
        
        TaxIncome = 0f;
        TradeIncome = 0f;
        PopulationSize = 0f;
        ArmySize = 0f;
        ArmyStrength = 0f;
        
        foreach (ProvinceModel province in currentNation.provinceList)
        {
            if (province != null)
            {
                TaxIncome += province.provinceTaxIncome;
                TradeIncome += province.provinceTradePower;
                PopulationSize += province.provinceCurrentPop;
            }
        }
        
        Debug.Log($"✓ Stats recalculated: {CityCount} cities, Tax: {TaxIncome}, Trade: {TradeIncome}");
    }

    public void EndTurn()
    {
        if (currentNation == null) return;
        
        nationMoney += TotalIncome;
        currentTurn++;
        
        RecalculateStats();
        
        Debug.Log($"Turn {currentTurn}: +{TotalIncome:F0} gold (Total: {nationMoney:F0})");
        
        GameEvents.TurnEnded(currentTurn);
        GameEvents.PlayerStatsChanged();
    }

    public bool OwnsProvince(ProvinceModel province)
    {
        if (currentNation == null || province == null) return false;
        return province.provinceOwner == currentNation;
    }

    public Color GetNationColor()
    {
        if (currentNation == null) return Color.gray;
        return NationLoader.HexToColor(currentNation.nationColor);
    }
}