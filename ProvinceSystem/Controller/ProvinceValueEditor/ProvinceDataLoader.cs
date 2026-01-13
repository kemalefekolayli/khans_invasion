using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Loads province economic data from province_data.json at game start.
/// This runs AFTER ProvinceNationAssigner assigns nations to provinces.
/// 
/// SETUP: Add this component to the same GameObject as ProvinceNationAssigner.
/// </summary>
public class ProvinceDataLoader : MonoBehaviour
{
    [Header("Settings")]
    public string dataFileName = "province_data.json";
    
    [Header("Debug")]
    public bool logEachProvince = false;
    
    private void OnEnable()
    {
        // Load AFTER provinces are assigned to nations
        GameEvents.OnProvincesAssigned += LoadProvinceData;
    }

    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= LoadProvinceData;
    }

    /// <summary>
    /// Called automatically after ProvincesAssigned event.
    /// Loads province_data.json and applies values to all provinces.
    /// </summary>
    private void LoadProvinceData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, dataFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[ProvinceDataLoader] {dataFileName} not found at {path}");
            Debug.LogWarning("[ProvinceDataLoader] Use Tools > Province Data Editor to create it");
            GameEvents.ProvinceDataLoaded();
            return;
        }

        // Read and parse JSON
        string json = File.ReadAllText(path);
        ProvinceDataWrapper wrapper = JsonUtility.FromJson<ProvinceDataWrapper>(json);

        if (wrapper == null || wrapper.provinces == null)
        {
            Debug.LogError("[ProvinceDataLoader] Failed to parse JSON!");
            GameEvents.ProvinceDataLoaded();
            return;
        }

        // Build lookup: provinceId -> data
        Dictionary<int, ProvinceData> dataById = new Dictionary<int, ProvinceData>();
        foreach (var data in wrapper.provinces)
        {
            dataById[data.provinceId] = data;
        }

        // Find all provinces and apply data
        ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        int appliedCount = 0;

        foreach (var province in allProvinces)
        {
            if (province.CompareTag("River")) continue;

            int id = (int)province.provinceId;
            
            if (dataById.TryGetValue(id, out ProvinceData data))
            {
                // Apply all values
                if (!string.IsNullOrEmpty(data.provinceName) && !data.provinceName.StartsWith("Province_"))
                {
                    province.provinceName = data.provinceName;
                }
                
                province.provinceTaxIncome = data.taxIncome;
                province.provinceTradePower = data.tradePower;
                province.provinceCurrentPop = data.currentPop;
                province.provinceMaxPop = data.maxPop;
                province.defenceForceSize = data.defenceForceSize;
                province.defenceForceStr = data.defenceForceStr;
                province.availableLoot = data.availableLoot;
                
                appliedCount++;

                if (logEachProvince)
                {
                    Debug.Log($"[Loader] {province.provinceName}: Tax={data.taxIncome}, Trade={data.tradePower}, Pop={data.currentPop}");
                }
            }
        }

        Debug.Log($"✓ ProvinceDataLoader: Applied data to {appliedCount} provinces from {dataFileName}");
        
        // Signal that data is loaded - PlayerNation will recalculate
        GameEvents.ProvinceDataLoaded();
    }
}