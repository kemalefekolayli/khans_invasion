using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIPolygonButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Polygon Points (normalized 0-1)")]
    public Vector2[] normalizedPoints;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onClick;
    
    [Header("Visual")]
    public Image targetImage;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsInsidePolygon(eventData))
        {
            Debug.Log($"[UIPolygonButton] {gameObject.name} CLICKED!");
            onClick?.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage != null)
            targetImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage != null)
            targetImage.color = normalColor;
    }

    private bool IsInsidePolygon(PointerEventData eventData)
    {
        if (normalizedPoints == null || normalizedPoints.Length < 3)
            return true;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        
        // Local point'i normalize et (0-1 aralığına)
        Rect rect = rectTransform.rect;
        Vector2 normalized = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height
        );
        
        return IsPointInPolygon(normalized, normalizedPoints);
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        
        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y < point.y && polygon[j].y >= point.y ||
                 polygon[j].y < point.y && polygon[i].y >= point.y) &&
                (polygon[i].x + (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * 
                (polygon[j].x - polygon[i].x) < point.x))
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
