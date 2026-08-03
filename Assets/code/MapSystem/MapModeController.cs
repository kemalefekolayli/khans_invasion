using System;
using UnityEngine;

public enum ProvinceMapMode
{
    Ownership,
    State,
    Loot,
    PopulationDensity
}

public class MapModeController : MonoBehaviour
{
    public static MapModeController Instance { get; private set; }

    [SerializeField] private ProvinceMapMode currentMapMode = ProvinceMapMode.Ownership;
    public ProvinceMapMode CurrentMapMode => currentMapMode;
    public event Action<ProvinceMapMode> OnMapModeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<MapModeController>() != null) return;

        FogOfWarManager fogManager = FindFirstObjectByType<FogOfWarManager>();
        if (fogManager != null)
        {
            fogManager.gameObject.AddComponent<MapModeController>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ApplyCurrentMode();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyCurrentMode();
        }
    }

    private void OnEnable()
    {
        GameEvents.OnProvincesAssigned += ApplyCurrentMode;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        GameEvents.OnProvinceRaided += OnProvinceRaided;
        GameEvents.OnPopulationGrowth += OnPopulationGrowth;
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
    }

    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= ApplyCurrentMode;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
        GameEvents.OnPopulationGrowth -= OnPopulationGrowth;
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetOwnershipMapMode() => SetMapMode(ProvinceMapMode.Ownership);
    public void SetStateMapMode() => SetMapMode(ProvinceMapMode.State);
    public void SetLootMapMode() => SetMapMode(ProvinceMapMode.Loot);
    public void SetPopulationDensityMapMode() => SetMapMode(ProvinceMapMode.PopulationDensity);

    public void ToggleMapMode()
    {
        int modeCount = Enum.GetValues(typeof(ProvinceMapMode)).Length;
        SetMapMode((ProvinceMapMode)(((int)currentMapMode + 1) % modeCount));
    }

    public void SetMapMode(ProvinceMapMode mode)
    {
        if (currentMapMode == mode) return;

        currentMapMode = mode;
        ApplyCurrentMode();
        OnMapModeChanged?.Invoke(currentMapMode);
    }

    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (currentMapMode == ProvinceMapMode.Ownership)
        {
            ApplyColor(province);
        }
    }

    private void OnProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        if (currentMapMode == ProvinceMapMode.Loot)
        {
            ApplyCurrentMode();
        }
    }

    private void OnPopulationGrowth(ProvinceModel province, float growthAmount)
    {
        if (currentMapMode == ProvinceMapMode.PopulationDensity)
        {
            ApplyColor(province);
        }
    }

    private void OnBuildingConstructed(ProvinceModel province, string buildingType)
    {
        if (currentMapMode == ProvinceMapMode.PopulationDensity)
        {
            ApplyColor(province);
        }
    }

    private void ApplyCurrentMode()
    {
        ProvinceModel[] provinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        float maximumLoot = GetMaximumLoot(provinces);

        foreach (ProvinceModel province in provinces)
        {
            ApplyColor(province, maximumLoot);
        }
    }

    private void ApplyColor(ProvinceModel province)
    {
        ApplyColor(province, GetMaximumLoot(FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None)));
    }

    private void ApplyColor(ProvinceModel province, float maximumLoot)
    {
        if (province == null || province.CompareTag("River") || province.spriteRenderer == null) return;

        Color color = GetMapColor(province, maximumLoot);
        FogOfWarManager fogManager = FogOfWarManager.Instance;
        if (fogManager != null)
        {
            fogManager.SetProvinceBaseColor(province, color);
        }
        else
        {
            province.spriteRenderer.color = color;
        }
    }

    private Color GetMapColor(ProvinceModel province, float maximumLoot)
    {
        switch (currentMapMode)
        {
            case ProvinceMapMode.State:
                return GetStateColor(province.provinceState);
            case ProvinceMapMode.Loot:
                return GetLootColor(province, maximumLoot);
            case ProvinceMapMode.PopulationDensity:
                return GetPopulationDensityColor(province);
            default:
                return GetOwnershipColor(province);
        }
    }

    private static float GetMaximumLoot(ProvinceModel[] provinces)
    {
        float maximumLoot = 0f;
        foreach (ProvinceModel province in provinces)
        {
            if (province != null && !province.CompareTag("River"))
            {
                maximumLoot = Mathf.Max(maximumLoot, province.availableLoot);
            }
        }

        return maximumLoot;
    }

    private static Color GetOwnershipColor(ProvinceModel province)
    {
        if (province.provinceOwner != null && !string.IsNullOrEmpty(province.provinceOwner.nationColor))
        {
            return NationLoader.HexToColor(province.provinceOwner.nationColor);
        }

        return province.provinceColor;
    }

    private static Color GetStateColor(StateModel state)
    {
        if (state == null) return new Color(0.45f, 0.45f, 0.45f, 1f);

        float hue = Mathf.Repeat(state.stateId * 0.61803398875f, 1f);
        return Color.HSVToRGB(hue, 0.45f, 0.9f);
    }

    private static Color GetLootColor(ProvinceModel province, float maximumLoot)
    {
        float normalizedLoot = maximumLoot > 0f ? Mathf.Clamp01(province.availableLoot / maximumLoot) : 0f;
        return Color.Lerp(Color.black, new Color(1f, 0.72f, 0.12f, 1f), normalizedLoot);
    }

    private static Color GetPopulationDensityColor(ProvinceModel province)
    {
        float density = province.provinceMaxPop > 0f
            ? Mathf.Clamp01(province.provinceCurrentPop / province.provinceMaxPop)
            : 0f;

        Color lightGreen = new Color(0.68f, 0.9f, 0.4f, 1f);
        Color darkGreen = new Color(0.1f, 0.42f, 0.16f, 1f);
        Color brown = new Color(0.38f, 0.2f, 0.08f, 1f);

        return density <= 0.5f
            ? Color.Lerp(lightGreen, darkGreen, density * 2f)
            : Color.Lerp(darkGreen, brown, (density - 0.5f) * 2f);
    }
}