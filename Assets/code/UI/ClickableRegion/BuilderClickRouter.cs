using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Owns Builder panel click context on the always-active prefab root and routes one
/// background click to one polygon action.
/// </summary>
public class BuilderClickRouter : MonoBehaviour, IPointerClickHandler
{
    private ProvinceModel currentProvince;
    private UIPolygonHotspot[] hotspots;

    private void Awake()
    {
        hotspots = GetComponentsInChildren<UIPolygonHotspot>(true);
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceManagementOpened += OnProvinceManagementOpened;
        GameEvents.OnProvincePanelClosed += OnProvincePanelClosed;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceManagementOpened -= OnProvinceManagementOpened;
        GameEvents.OnProvincePanelClosed -= OnProvincePanelClosed;
        GameEvents.OnCityCenterExit -= OnCityCenterExit;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (currentProvince == null)
        {
            GameLog.Warning(GameLogCategory.UI, "[BuilderClickRouter] No managed province is open.");
            return;
        }

        if (hotspots == null || hotspots.Length == 0)
            hotspots = GetComponentsInChildren<UIPolygonHotspot>(true);

        foreach (UIPolygonHotspot hotspot in hotspots)
        {
            if (hotspot == null || !hotspot.ContainsScreenPoint(eventData.position, eventData.pressEventCamera))
                continue;

            RouteBuild(hotspot.BuildingType);
            return;
        }
    }

    private void RouteBuild(string buildingType)
    {
        if (string.IsNullOrEmpty(buildingType))
        {
            GameLog.Warning(GameLogCategory.UI, "[BuilderClickRouter] Hotspot has an invalid building mapping.");
            return;
        }

        Builder builder = Builder.Instance != null ? Builder.Instance : FindFirstObjectByType<Builder>();
        PlayerNation player = PlayerNation.Instance != null ? PlayerNation.Instance : FindFirstObjectByType<PlayerNation>();
        if (builder == null || player == null || player.currentNation == null)
        {
            GameLog.Warning(GameLogCategory.UI, "[BuilderClickRouter] Builder or PlayerNation is unavailable.");
            return;
        }

        if (currentProvince.buildings.Contains(buildingType))
        {
            string displayName = GetBuildingDisplayName(buildingType);
            CenterWarningPopupSpawner.Show($"{displayName} already built");
            GameLog.Warning(GameLogCategory.UI,
                $"[BuilderClickRouter] {buildingType} already exists in {currentProvince.provinceName}.");
            return;
        }

        float requiredGold = builder.GetPlayerBuildingCost(buildingType);
        if (player.nationMoney < requiredGold)
        {
            string displayName = GetBuildingDisplayName(buildingType);
            float shortage = Mathf.Max(0f, requiredGold - player.nationMoney);
            CenterWarningPopupSpawner.Show(
                $"Not enough gold - {displayName} costs {requiredGold:F0} (missing {shortage:F0})");
            GameLog.Warning(GameLogCategory.UI,
                $"[BuilderClickRouter] {displayName} costs {requiredGold:F0}; missing {shortage:F0} gold.");
            return;
        }

        float chargedGold = builder.BuildBuilding(currentProvince, buildingType, player.nationMoney);
        if (chargedGold <= 0f)
        {
            GameLog.Warning(GameLogCategory.UI, $"[BuilderClickRouter] Build rejected for {buildingType}.");
            return;
        }

        player.nationMoney = Mathf.Max(0f, player.nationMoney - chargedGold);
        player.RecalculateStats();
        GameEvents.PlayerStatsChanged();
        GameLog.Log(GameLogCategory.UI,
            $"[BuilderClickRouter] Built {buildingType} in {currentProvince.provinceName} for {chargedGold:F0} gold.");
    }

    private void OnProvinceManagementOpened(ProvinceModel province)
    {
        currentProvince = province;
    }

    private void OnProvincePanelClosed()
    {
        currentProvince = null;
    }

    private void OnCityCenterExit(CityCenter cityCenter)
    {
        currentProvince = null;
    }

    private static string GetBuildingDisplayName(string buildingType)
    {
        return buildingType == "Trade_Building"
            ? "Trade Building"
            : buildingType.Replace("_", " ");
    }
}
