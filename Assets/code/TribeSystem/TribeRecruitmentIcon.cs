using UnityEngine;

/// <summary>Displays the tribe recruitment interaction above a tribe.</summary>
[RequireComponent(typeof(TribeGroup))]
public class TribeRecruitmentIcon : MonoBehaviour
{
    [Header("Handshake Icon")]
    [SerializeField] private Sprite handshakeSprite;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.145f, 0f);
    [SerializeField, Min(0.01f)] private float iconScale = 0.715f;
    [SerializeField, Min(0f)] private float hoverDistance = 0.08f;
    [SerializeField, Min(0f)] private float hoverSpeed = 3f;
    [SerializeField] private int sortingOrder = 10;

    private GameObject iconObject;
    private SpriteRenderer iconRenderer;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        CreateIcon();
        SetVisible(false);
    }

    private void Update()
    {
        if (iconObject == null || !iconObject.activeSelf) return;
        float offset = Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;
        iconObject.transform.localPosition = baseLocalPosition + Vector3.up * offset;
    }

    public void SetVisible(bool visible)
    {
        if (iconObject == null) CreateIcon();
        if (iconObject != null) iconObject.SetActive(visible);
    }

    public bool ContainsScreenPoint(Camera camera, Vector2 screenPoint)
    {
        if (camera == null || iconRenderer == null || !iconRenderer.gameObject.activeInHierarchy) return false;
        Vector3 worldPoint = camera.ScreenToWorldPoint(screenPoint);
        worldPoint.z = iconRenderer.bounds.center.z;
        return iconRenderer.bounds.Contains(worldPoint);
    }

    private void CreateIcon()
    {
        if (iconObject != null || handshakeSprite == null) return;

        iconObject = new GameObject("RecruitmentHandshake");
        iconObject.transform.SetParent(transform, false);
        iconObject.transform.localScale = Vector3.one * iconScale;
        baseLocalPosition = localOffset;
        iconObject.transform.localPosition = baseLocalPosition;

        iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = handshakeSprite;
        iconRenderer.sortingOrder = sortingOrder;
    }
}
