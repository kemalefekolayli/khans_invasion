using UnityEngine;
using UnityEngine.EventSystems;

public class MapModeHotspot : MonoBehaviour, IPointerClickHandler, ICanvasRaycastFilter
{
    [SerializeField]
    private Rect normalizedClickRegion = new Rect(0.84f, 0.04f, 0.15f, 0.78f);

    private RectTransform rectTransform;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateBootstrap()
    {
        if (FindFirstObjectByType<MapModeHotspotBootstrap>() != null) return;

        GameObject bootstrapObject = new GameObject("MapModeHotspotBootstrap");
        DontDestroyOnLoad(bootstrapObject);
        bootstrapObject.AddComponent<MapModeHotspotBootstrap>();
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
        if (rectTransform == null) return false;

        Rect localRect = rectTransform.rect;
        if (localRect.width <= 0f || localRect.height <= 0f) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out Vector2 localPoint)) return false;

        Vector2 normalizedPoint = new Vector2(
            (localPoint.x - localRect.xMin) / localRect.width,
            (localPoint.y - localRect.yMin) / localRect.height);

        return normalizedClickRegion.Contains(normalizedPoint);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MapModeController controller = MapModeController.Instance;
        if (controller == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[MapModeHotspot] MapModeController not found.");
            return;
        }

        controller.ToggleMapMode();
        GameLog.Log(GameLogCategory.Core, $"[MapModeHotspot] Active map mode: {controller.CurrentMapMode}");
    }

    private sealed class MapModeHotspotBootstrap : MonoBehaviour
    {
        private void OnEnable()
        {
            GameEvents.OnMapLoaded += AttachHotspot;
        }

        private void OnDisable()
        {
            GameEvents.OnMapLoaded -= AttachHotspot;
        }

        private static void AttachHotspot()
        {
            GameObject topLeft = GameObject.Find("TopLeft");
            if (topLeft == null || topLeft.GetComponent<MapModeHotspot>() != null) return;

            topLeft.AddComponent<MapModeHotspot>();
        }
    }
}
