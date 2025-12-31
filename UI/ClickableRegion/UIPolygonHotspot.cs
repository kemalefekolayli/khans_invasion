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
    private Builder builder;

    void Awake()
    {
        rt = (RectTransform)transform;

    }

    // 🔥 IMPORTANT: This makes UI raycasts respect the polygon shape
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (points == null || points.Count < 3) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, eventCamera, out var local))
            return false;

        return PointInPolygon(local, points);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // At this point, we already KNOW it's inside polygon (because of the filter),
        // but keeping it safe is fine.
        AnyRegionClicked?.Invoke(regionId);
        Debug.Log("Clicked region: " + regionId);
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
