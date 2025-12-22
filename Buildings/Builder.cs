using UnityEngine;
using System.Collections.Generic;

public class Builder
{
    // Building costs - adjust as needed
    private static readonly Dictionary<string, float> buildingCosts = new Dictionary<string, float>
    {
        { "Fortress", 500f },
        { "Farm", 100f },
        { "Barracks", 300f },
        { "Trade_Building", 250f },
        { "Housing", 150f }
    };

    public bool CanBuild(ProvinceModel province, string buildingType, float availableGold)
    {
        if (province == null) return false;
        
        // Check if building already exists
        if (province.buildings.Contains(buildingType))
        {
            Debug.Log($"Building {buildingType} already exists in {province.provinceName}");
            return false;
        }
        
        // Check cost
        float cost = GetBuildingCost(buildingType);
        if (availableGold < cost)
        {
            Debug.Log($"Not enough gold for {buildingType}. Need {cost}, have {availableGold}");
            return false;
        }
        
        return true;
    }

    public float GetBuildingCost(string buildingType)
    {
        if (buildingCosts.TryGetValue(buildingType, out float cost))
        {
            return cost;
        }
        Debug.LogWarning($"Unknown building type: {buildingType}");
        return 9999f;
    }

    /// <summary>
    /// Attempts to build a building. Returns the cost if successful, -1 if failed.
    /// </summary>
    public float BuildBuilding(ProvinceModel province, string buildingType, float availableGold)
    {
        if (!CanBuild(province, buildingType, availableGold))
        {
            return -1f;
        }
        
        float cost = GetBuildingCost(buildingType);
        
        switch (buildingType)
        {
            case "Barracks":
                BuildBarracks(province);
                break;
            case "Farm":
                BuildFarm(province);
                break;
            case "Housing":
                BuildHousing(province);
                break;
            case "Trade_Building":
                BuildTradeBuilding(province);
                break;
            case "Fortress":
                BuildFortress(province);
                break;
            default:
                Debug.LogWarning($"Unknown building type: {buildingType}");
                return -1f;
        }
        
        Debug.Log($"✓ Built {buildingType} in {province.provinceName} for {cost} gold");
        GameEvents.BuildingConstructed(province, buildingType);
        
        return cost;
    }

    private void BuildFortress(ProvinceModel province)
    {
        province.buildings.Add("Fortress");
        province.defenceForceSize += 100f;
        province.defenceForceStr += 1.2f;
    }

    private void BuildFarm(ProvinceModel province)
    {
        province.buildings.Add("Farm");
        province.provinceTaxIncome += 10f;
    }

    private void BuildBarracks(ProvinceModel province)
    {
        province.buildings.Add("Barracks");
        // Enables troop recruitment - handled elsewhere
    }

    private void BuildTradeBuilding(ProvinceModel province)
    {
        province.buildings.Add("Trade_Building");
        province.provinceTradePower += 25f;
    }

    private void BuildHousing(ProvinceModel province)
    {
        province.buildings.Add("Housing");
        province.provinceMaxPop += 500f;
    }

    /// <summary>
    /// Gets list of buildings that can still be built in this province
    /// </summary>
    public List<string> GetAvailableBuildings(ProvinceModel province)
    {
        List<string> available = new List<string>();
        
        foreach (var buildingType in buildingCosts.Keys)
        {
            if (!province.buildings.Contains(buildingType))
            {
                available.Add(buildingType);
            }
        }
        
        return available;
    }

    public static Dictionary<string, float> GetAllBuildingCosts()
    {
        return new Dictionary<string, float>(buildingCosts);
    }
}