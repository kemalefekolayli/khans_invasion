using System.Collections.Generic;
using UnityEngine;

/// <summary>Serialized polygon and building action metadata for the Builder click router.</summary>
public class UIPolygonHotspot : MonoBehaviour
{
    public string regionId;
    public List<Vector2> points = new List<Vector2>();

    public string BuildingType => regionId switch
    {
        "1" => "Farm",
        "2" => "Barracks",
        "3" => "Fortress",
        "4" => "Housing",
        "5" => "Trade_Building",
        _ => null
    };

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
    {
        if (points == null || points.Count < 3) return false;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        return PointInPolygon(localPoint, points);
    }

    private static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            bool intersects = ((a.y > point.y) != (b.y > point.y))
                && (point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y + 1e-6f) + a.x);

            if (intersects) inside = !inside;
        }

        return inside;
    }
}
