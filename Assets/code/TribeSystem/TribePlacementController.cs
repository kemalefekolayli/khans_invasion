using UnityEngine;

/// <summary>Places a recruited civilian tribe into the selected player's city.</summary>
public class TribePlacementController : MonoBehaviour
{
    public static TribePlacementController Instance { get; private set; }

    private ProvinceModel currentProvince;

    public ProvinceModel CurrentProvince => currentProvince;
    public string PlaceholderButtonLabel => CanPlaceSelectedTribe ? "Place Tribe" : "Place Tribe (Unavailable)";
    public bool CanPlaceSelectedTribe => GetPlaceableTribe() != null && IsValidTarget(currentProvince, PlayerNation.Instance?.currentNation);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<TribePlacementController>() != null) return;
        GameObject host = new(nameof(TribePlacementController));
        DontDestroyOnLoad(host);
        host.AddComponent<TribePlacementController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceManagementOpened += OnProvinceOpened;
        GameEvents.OnProvinceInteractionOpened += OnProvinceOpened;
        GameEvents.OnProvincePanelClosed += OnProvinceClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceManagementOpened -= OnProvinceOpened;
        GameEvents.OnProvinceInteractionOpened -= OnProvinceOpened;
        GameEvents.OnProvincePanelClosed -= OnProvinceClosed;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void OnProvinceOpened(ProvinceModel province) => currentProvince = province;
    private void OnProvinceClosed() => currentProvince = null;

    /// <summary>Placeholder button handler for UI hookup/testing.</summary>
    public bool TryPlaceSelectedTribe()
    {
        SelectableGeneral selected = GeneralSelectionManager.Instance?.SelectedGeneral;
        NationModel player = PlayerNation.Instance?.currentNation;
        General general = selected?.GetComponent<General>();
        TribeGroup tribe = GetPlaceableTribe();

        if (player == null || general == null || general.IsCaptured || currentProvince == null || tribe == null)
        {
            Warn("Select a recruited tribe and a player-owned city first.");
            return false;
        }
        if (!IsValidTarget(currentProvince, player))
        {
            Warn($"{currentProvince.provinceName} is not a valid player-owned city.");
            return false;
        }

        float available = Mathf.Max(0f, currentProvince.provinceMaxPop - currentProvince.provinceCurrentPop);
        if (available <= 0f)
        {
            Warn($"{currentProvince.provinceName} has no population capacity.");
            return false;
        }

        float moved = Mathf.Min(tribe.Population, available);
        currentProvince.provinceCurrentPop += moved;
        tribe.SetPopulation(tribe.Population - moved);
        PlayerNation.Instance.RecalculateStats();
        GameEvents.PopulationGrowth(currentProvince, moved);
        GameLog.Log(GameLogCategory.Province, $"[TribePlacement] Placed {moved:F0} civilians in {currentProvince.provinceName}; remainder={tribe.Population:F0}");
        return true;
    }

    public void PlaceSelectedTribe() => TryPlaceSelectedTribe();

    private TribeGroup GetPlaceableTribe()
    {
        General general = GeneralSelectionManager.Instance?.SelectedGeneral?.GetComponent<General>();
        if (general == null) return null;
        TribeGroup best = null;
        foreach (TribeGroup tribe in FindObjectsByType<TribeGroup>(FindObjectsSortMode.None))
        {
            if (tribe != null && tribe.RecruitingGeneral == general && tribe.Population > 0f) best = tribe;
        }
        return best;
    }

    private static void Warn(string message)
    {
        GameLog.Warning(GameLogCategory.Province, $"[TribePlacement] {message}");
        CenterWarningPopupSpawner.Show(message);
    }

    private static bool IsValidTarget(ProvinceModel province, NationModel player)
    {
        return province != null && player != null && province.provinceOwner == player && SupplyRouteTracker.IsCity(province);
    }
}
