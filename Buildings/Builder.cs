using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles building construction in provinces.
/// Can use either hardcoded values or a BuildingDatabase ScriptableObject for balancing.
/// </summary>
public class Builder : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Optional: Use a BuildingDatabase ScriptableObject for balancing.\nIf null, uses hardcoded values below.")]
    [SerializeField] private BuildingDatabase buildingDatabase;
    
    [Header("Fallback Costs (if no database)")]
    [SerializeField] private float fortressCost = 500f;
    [SerializeField] private float farmCost = 100f;
    [SerializeField] private float barracksCost = 300f;
    [SerializeField] private float tradeBuildingCost = 250f;
    [SerializeField] private float housingCost = 100f;
    
    [Header("Fallback Benefits (if no database)")]
    [SerializeField] private float housingMaxPopBonus = 1000f;
    [SerializeField] private float farmTaxBonus = 10f;
    [SerializeField] private float tradeBuildingTradePower = 25f;
    [SerializeField] private float fortressDefenseBonus = 100f;
    [SerializeField] private float fortressDefenseStr = 1.2f;
    
    [Header("Debug")]
    [SerializeField] private bool logBuilding = true;
    
    public static Builder Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CanBuild(ProvinceModel province, string buildingType, float availableGold)
    {
        if (province == null) return false;
        
        // Check if building already exists
        if (province.buildings.Contains(buildingType))
        {
            if (logBuilding)
                Debug.Log($"Building {buildingType} already exists in {province.provinceName}");
            return false;
        }
        
        // Check cost
        float cost = GetBuildingCost(buildingType);
        if (availableGold < cost)
        {
            if (logBuilding)
                Debug.Log($"Not enough gold for {buildingType}. Need {cost}, have {availableGold}");
            return false;
        }
        
        return true;
    }

    public float GetBuildingCost(string buildingType)
    {
        // Use database if available
        if (buildingDatabase != null)
        {
            return buildingDatabase.GetCost(buildingType);
        }
        
        // Fallback to hardcoded values
        switch (buildingType)
        {
            case "Fortress": return fortressCost;
            case "Farm": return farmCost;
            case "Barracks": return barracksCost;
            case "Trade_Building": return tradeBuildingCost;
            case "Housing": return housingCost;
            default:
                Debug.LogWarning($"Unknown building type: {buildingType}");
                return 9999f;
        }
    }

    public float BuildBuilding(ProvinceModel province, string buildingType, float availableGold)
    {
        if (!CanBuild(province, buildingType, availableGold))
        {
            return -1f;
        }
        
        float cost = GetBuildingCost(buildingType);
        
        // Add to building list
        province.buildings.Add(buildingType);
        
        // Apply benefits
        if (buildingDatabase != null)
        {
            // Use database for benefits
            buildingDatabase.ApplyBuildingBenefits(province, buildingType);
        }
        else
        {
            // Use hardcoded benefits
            ApplyBuildingBenefits(province, buildingType);
        }
        
        if (logBuilding)
        {
            Debug.Log($"✓ Built {buildingType} in {province.provinceName} for {cost} gold");
        }
        
        GameEvents.BuildingConstructed(province, buildingType);
        
        return cost;
    }
    
    /// <summary>
    /// Apply hardcoded building benefits (fallback when no database).
    /// </summary>
    private void ApplyBuildingBenefits(ProvinceModel province, string buildingType)
    {
        switch (buildingType)
        {
            case "Fortress":
                province.defenceForceSize += fortressDefenseBonus;
                province.defenceForceStr += fortressDefenseStr;
                break;
                
            case "Farm":
                province.provinceTaxIncome += farmTaxBonus;
                break;
                
            case "Barracks":
                // Enables troop recruitment - handled elsewhere
                break;
                
            case "Trade_Building":
                province.provinceTradePower += tradeBuildingTradePower;
                break;
                
            case "Housing":
                province.provinceMaxPop += housingMaxPopBonus;
                break;
        }
    }

    /// <summary>
    /// Gets list of buildings that can still be built in this province
    /// </summary>
    public List<string> GetAvailableBuildings(ProvinceModel province)
    {
        List<string> available = new List<string>();
        
        List<string> allBuildings;
        if (buildingDatabase != null)
        {
            allBuildings = buildingDatabase.GetAllBuildingNames();
        }
        else
        {
            allBuildings = new List<string> { "Fortress", "Farm", "Barracks", "Trade_Building", "Housing" };
        }
        
        foreach (var buildingType in allBuildings)
        {
            if (!province.buildings.Contains(buildingType))
            {
                available.Add(buildingType);
            }
        }
        
        return available;
    }

    /// <summary>
    /// Get all building costs (for UI display).
    /// </summary>
    public Dictionary<string, float> GetAllBuildingCosts()
    {
        Dictionary<string, float> costs = new Dictionary<string, float>();
        
        if (buildingDatabase != null)
        {
            foreach (string name in buildingDatabase.GetAllBuildingNames())
            {
                costs[name] = buildingDatabase.GetCost(name);
            }
        }
        else
        {
            costs["Fortress"] = fortressCost;
            costs["Farm"] = farmCost;
            costs["Barracks"] = barracksCost;
            costs["Trade_Building"] = tradeBuildingCost;
            costs["Housing"] = housingCost;
        }
        
        return costs;
    }
    
    /// <summary>
    /// Get building info for UI.
    /// </summary>
    public (string displayName, string description, float cost) GetBuildingInfo(string buildingType)
    {
        if (buildingDatabase != null)
        {
            var data = buildingDatabase.GetBuilding(buildingType);
            if (data != null)
            {
                return (data.displayName, data.description, data.goldCost);
            }
        }
        
        // Fallback descriptions
        float cost = GetBuildingCost(buildingType);
        string desc = buildingType switch
        {
            "Fortress" => $"+{fortressDefenseBonus} defense, +{fortressDefenseStr} strength",
            "Farm" => $"+{farmTaxBonus} tax income",
            "Barracks" => "Enables recruitment",
            "Trade_Building" => $"+{tradeBuildingTradePower} trade power",
            "Housing" => $"+{housingMaxPopBonus} max population",
            _ => "Unknown building"
        };
        
        return (buildingType.Replace("_", " "), desc, cost);
    }
}