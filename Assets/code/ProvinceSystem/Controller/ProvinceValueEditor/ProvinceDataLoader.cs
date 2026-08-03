using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Loads persistent province economic data after ownership assignment.</summary>
public class ProvinceDataLoader : MonoBehaviour
{
    [Header("Settings")]
    public string dataFileName = "province_data.json";

    [Header("Debug")]
    public bool logEachProvince = false;

    private bool dataLoaded;

    private void OnEnable()
    {
        GameEvents.OnProvincesAssigned += LoadProvinceData;
    }

    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= LoadProvinceData;
    }

    private void Start()
    {
        Invoke(nameof(LoadProvinceData), 0.75f);
    }

    private void LoadProvinceData()
    {
        if (dataLoaded) return;

        string path = Path.Combine(Application.streamingAssetsPath, dataFileName);
        if (!File.Exists(path))
        {
            dataLoaded = true;
            GameLog.Warning(GameLogCategory.Core, $"[ProvinceDataLoader] {dataFileName} not found at {path}");
            GameEvents.ProvinceDataLoaded();
            return;
        }

        ProvinceDataWrapper wrapper = JsonUtility.FromJson<ProvinceDataWrapper>(File.ReadAllText(path));
        if (wrapper?.provinces == null)
        {
            dataLoaded = true;
            GameLog.Error(GameLogCategory.Core, "[ProvinceDataLoader] Failed to parse province data.");
            GameEvents.ProvinceDataLoaded();
            return;
        }

        Dictionary<int, ProvinceData> dataById = new Dictionary<int, ProvinceData>();
        foreach (ProvinceData data in wrapper.provinces)
        {
            dataById[data.provinceId] = data;
        }

        foreach (ProvinceModel province in FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None))
        {
            if (province.CompareTag("River") || !dataById.TryGetValue((int)province.provinceId, out ProvinceData data))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(data.provinceName) && !data.provinceName.StartsWith("Province_"))
            {
                province.SetProvinceName(data.provinceName);
            }

            province.provinceTaxIncome = data.taxIncome;
            province.provinceTradePower = data.tradePower;
            province.provinceCurrentPop = data.currentPop;
            province.provinceMaxPop = data.maxPop;
            province.availableLoot = data.availableLoot;

            if (logEachProvince)
            {
                GameLog.Log(GameLogCategory.Core, $"[Loader] {province.provinceName}: Tax={data.taxIncome}, Trade={data.tradePower}, Pop={data.currentPop}");
            }
        }

        dataLoaded = true;
        GameEvents.ProvinceDataLoaded();
    }
}
