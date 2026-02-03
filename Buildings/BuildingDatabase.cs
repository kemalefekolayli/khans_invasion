using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that defines all building types and their costs/benefits.
/// Create via: Right-click in Project > Create > Game Data > Building Database
/// This allows you to balance buildings directly in Unity Inspector!
/// </summary>
[CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Game Data/Building Database")]
public class BuildingDatabase : ScriptableObject
{
    [System.Serializable]
    public class BuildingData
    {
        [Header("Identity")]
        public string buildingName = "New Building";
        public string displayName = "New Building";
        [TextArea(2, 4)]
        public string description = "A building.";
        public Sprite icon;
        
        [Header("Cost")]
        public float goldCost = 100f;
        public int turnsToComplete = 1;
        
        [Header("Province Benefits")]
        [Tooltip("Bonus to province max population")]
        public float maxPopulationBonus = 0f;
        
        [Tooltip("Bonus to province tax income")]
        public float taxIncomeBonus = 0f;
        
        [Tooltip("Bonus to province trade power")]
        public float tradePowerBonus = 0f;
        
        [Tooltip("Bonus to province defense force size")]
        public float defenseForceBonus = 0f;
        
        [Tooltip("Multiplier to province defense strength")]
        public float defenseStrengthMultiplier = 1f;
        
        [Header("Special Flags")]
        [Tooltip("Is this a fortress? (affects siege mechanics)")]
        public bool isFortress = false;
        
        [Tooltip("Enables troop recruitment in this province")]
        public bool enablesRecruitment = false;
        
        [Tooltip("Maximum instances per province (0 = unlimited)")]
        public int maxPerProvince = 1;
    }
    
    [Header("All Buildings")]
    [SerializeField]
    private BuildingData[] buildings;
    
    // Quick lookup dictionary (built at runtime)
    private Dictionary<string, BuildingData> buildingLookup;
    
    private void OnEnable()
    {
        RebuildLookup();
    }
    
    private void RebuildLookup()
    {
        buildingLookup = new Dictionary<string, BuildingData>();
        if (buildings != null)
        {
            foreach (var building in buildings)
            {
                if (!string.IsNullOrEmpty(building.buildingName))
                {
                    buildingLookup[building.buildingName] = building;
                }
            }
        }
    }
    
    /// <summary>
    /// Get building data by name.
    /// </summary>
    public BuildingData GetBuilding(string buildingName)
    {
        if (buildingLookup == null || buildingLookup.Count == 0)
        {
            RebuildLookup();
        }
        
        if (buildingLookup.TryGetValue(buildingName, out BuildingData data))
        {
            return data;
        }
        
        Debug.LogWarning($"[BuildingDatabase] Building '{buildingName}' not found!");
        return null;
    }
    
    /// <summary>
    /// Get cost of a building.
    /// </summary>
    public float GetCost(string buildingName)
    {
        BuildingData data = GetBuilding(buildingName);
        return data?.goldCost ?? 9999f;
    }
    
    /// <summary>
    /// Get all available building names.
    /// </summary>
    public List<string> GetAllBuildingNames()
    {
        List<string> names = new List<string>();
        if (buildings != null)
        {
            foreach (var b in buildings)
            {
                names.Add(b.buildingName);
            }
        }
        return names;
    }
    
    /// <summary>
    /// Get all building data entries.
    /// </summary>
    public BuildingData[] GetAllBuildings()
    {
        return buildings;
    }
    
    /// <summary>
    /// Apply building benefits to a province.
    /// </summary>
    public void ApplyBuildingBenefits(ProvinceModel province, string buildingName)
    {
        BuildingData data = GetBuilding(buildingName);
        if (data == null || province == null) return;
        
        province.provinceMaxPop += data.maxPopulationBonus;
        province.provinceTaxIncome += data.taxIncomeBonus;
        province.provinceTradePower += data.tradePowerBonus;
        province.defenceForceSize += data.defenseForceBonus;
        province.defenceForceStr *= data.defenseStrengthMultiplier;
        
        Debug.Log($"[BuildingDatabase] Applied {buildingName} benefits to {province.provinceName}");
    }
}
