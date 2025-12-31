using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPolygonHotspot : MonoBehaviour, IPointerClickHandler, ICanvasRaycastFilter
{
    public string regionId;
    public List<Vector2> points = new List<Vector2>();

    public static event Action<string> AnyRegionClicked;

    RectTransform rt;
    
    // Static references for building
    private static Builder builder;
    private static ProvinceModel currentProvince;
    private static PlayerNation playerNation;
    private static bool isSubscribed = false;

    void Awake()
    {
        rt = (RectTransform)transform;
        
        if (builder == null)
            builder = new Builder();
        
        // Subscribe only once (static)
        SubscribeToEvents();
    }

    private static void SubscribeToEvents()
    {
        if (isSubscribed) return;
        
        GameEvents.OnProvinceManagementOpened += OnProvinceManagementOpened;
        GameEvents.OnProvincePanelClosed += OnPanelClosed;
        GameEvents.OnCityCenterExit += OnCityCenterExit;
        
        isSubscribed = true;
        Debug.Log("[UIPolygonHotspot] Subscribed to events (static)");
    }

    private static void OnProvinceManagementOpened(ProvinceModel province)
    {
        currentProvince = province;
        playerNation = PlayerNation.Instance;
        Debug.Log($"[UIPolygonHotspot] Province set: {province.provinceName}");
    }

    private static void OnPanelClosed()
    {
        currentProvince = null;
        Debug.Log("[UIPolygonHotspot] Province cleared (panel closed)");
    }

    private static void OnCityCenterExit(CityCenter cityCenter)
    {
        currentProvince = null;
        Debug.Log("[UIPolygonHotspot] Province cleared (city center exit)");
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (points == null || points.Count < 3) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, eventCamera, out var local))
            return false;

        return PointInPolygon(local, points);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AnyRegionClicked?.Invoke(regionId);
        Debug.Log($"Clicked region: {regionId}, currentProvince: {(currentProvince != null ? currentProvince.provinceName : "NULL")}");
        
        // Build based on region ID
        string buildingType = regionId switch
        {
            "1" => "Farm",
            "2" => "Barracks",
            "3" => "Fortress",
            "4" => "Housing",
            "5" => "Trade_Building",
            _ => null
        };
        
        if (buildingType != null)
        {
            TryBuild(buildingType);
        }
    }

    private void TryBuild(string buildingType)
    {
        if (currentProvince == null)
        {
            Debug.LogWarning("[UIPolygonHotspot] No province selected!");
            return;
        }
        
        if (playerNation == null)
        {
            playerNation = PlayerNation.Instance;
            if (playerNation == null)
            {
                Debug.LogWarning("[UIPolygonHotspot] PlayerNation not found!");
                return;
            }
        }
        
        float cost = builder.BuildBuilding(currentProvince, buildingType, playerNation.nationMoney);
        
        if (cost > 0)
        {
            playerNation.nationMoney -= cost;
            playerNation.RecalculateStats();
            GameEvents.PlayerStatsChanged();
            Debug.Log($"[UIPolygonHotspot] Built {buildingType} in {currentProvince.provinceName}");
        }
    }

    static bool PointInPolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[j];

            bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                             (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + 1e-6f) + a.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }
}