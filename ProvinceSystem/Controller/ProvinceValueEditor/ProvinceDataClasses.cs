using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JSON-serializable data for a single province.
/// Stored in province_data.json, separate from nation assignments.
/// </summary>
[System.Serializable]
public class ProvinceData
{
    public int provinceId;
    public string provinceName;
    
    [Header("Economy")]
    public float taxIncome = 10f;
    public float tradePower = 5f;
    
    [Header("Population")]
    public float currentPop = 100f;
    public float maxPop = 500f;
    
    [Header("Military")]
    public float defenceForceSize = 50f;
    public float defenceForceStr = 1.0f;
    public float availableLoot = 100f;
    
    [Header("Terrain")]
    public string terrainType = "Plains"; // Plains, Mountain, Forest, Desert, Coastal
    public bool isCapital = false;
    
    public ProvinceData() { }
    
    public ProvinceData(int id, string name)
    {
        provinceId = id;
        provinceName = name;
    }
    
    /// <summary>
    /// Create from existing ProvinceModel (for initial export)
    /// </summary>
    public static ProvinceData FromProvinceModel(ProvinceModel model)
    {
        return new ProvinceData
        {
            provinceId = (int)model.provinceId,
            provinceName = model.provinceName,
            taxIncome = model.provinceTaxIncome,
            tradePower = model.provinceTradePower,
            currentPop = model.provinceCurrentPop,
            maxPop = model.provinceMaxPop,
            defenceForceSize = model.defenceForceSize,
            defenceForceStr = model.defenceForceStr,
            availableLoot = model.availableLoot
        };
    }
    
    /// <summary>
    /// Apply this data to a ProvinceModel
    /// </summary>
    public void ApplyToProvinceModel(ProvinceModel model)
    {
        // Only update name if we have a meaningful one
        if (!string.IsNullOrEmpty(provinceName) && !provinceName.StartsWith("Province_"))
        {
            model.provinceName = provinceName;
        }
        
        model.provinceTaxIncome = taxIncome;
        model.provinceTradePower = tradePower;
        model.provinceCurrentPop = currentPop;
        model.provinceMaxPop = maxPop;
        model.defenceForceSize = defenceForceSize;
        model.defenceForceStr = defenceForceStr;
        model.availableLoot = availableLoot;
    }
}

/// <summary>
/// Wrapper for JSON serialization of province data array.
/// </summary>
[System.Serializable]
public class ProvinceDataWrapper
{
    public ProvinceData[] provinces;
    public string lastModified;
    public int version = 1;
}

/// <summary>
/// Preset templates for quick province setup.
/// </summary>
public static class ProvincePresets
{
    public static ProvinceData SmallVillage => new ProvinceData
    {
        taxIncome = 5f,
        tradePower = 2f,
        currentPop = 50f,
        maxPop = 200f,
        defenceForceSize = 20f,
        defenceForceStr = 0.8f,
        availableLoot = 30f,
        terrainType = "Plains"
    };
    
    public static ProvinceData Town => new ProvinceData
    {
        taxIncome = 15f,
        tradePower = 10f,
        currentPop = 200f,
        maxPop = 600f,
        defenceForceSize = 80f,
        defenceForceStr = 1.0f,
        availableLoot = 100f,
        terrainType = "Plains"
    };
    
    public static ProvinceData City => new ProvinceData
    {
        taxIncome = 30f,
        tradePower = 25f,
        currentPop = 500f,
        maxPop = 1500f,
        defenceForceSize = 200f,
        defenceForceStr = 1.2f,
        availableLoot = 300f,
        terrainType = "Plains"
    };
    
    public static ProvinceData Capital => new ProvinceData
    {
        taxIncome = 50f,
        tradePower = 40f,
        currentPop = 1000f,
        maxPop = 3000f,
        defenceForceSize = 500f,
        defenceForceStr = 1.5f,
        availableLoot = 600f,
        terrainType = "Plains",
        isCapital = true
    };
    
    public static ProvinceData TradeHub => new ProvinceData
    {
        taxIncome = 20f,
        tradePower = 50f,
        currentPop = 300f,
        maxPop = 800f,
        defenceForceSize = 100f,
        defenceForceStr = 1.0f,
        availableLoot = 400f,
        terrainType = "Coastal"
    };
    
    public static ProvinceData Fortress => new ProvinceData
    {
        taxIncome = 10f,
        tradePower = 5f,
        currentPop = 150f,
        maxPop = 400f,
        defenceForceSize = 300f,
        defenceForceStr = 1.8f,
        availableLoot = 80f,
        terrainType = "Mountain"
    };
    
    public static ProvinceData Desert => new ProvinceData
    {
        taxIncome = 5f,
        tradePower = 15f,  // Trade routes
        currentPop = 80f,
        maxPop = 250f,
        defenceForceSize = 40f,
        defenceForceStr = 1.0f,
        availableLoot = 50f,
        terrainType = "Desert"
    };
    
    public static ProvinceData Forest => new ProvinceData
    {
        taxIncome = 8f,
        tradePower = 3f,
        currentPop = 100f,
        maxPop = 300f,
        defenceForceSize = 60f,
        defenceForceStr = 1.3f, // Defensive advantage
        availableLoot = 40f,
        terrainType = "Forest"
    };
}