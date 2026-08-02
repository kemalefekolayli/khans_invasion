using UnityEngine;

[RequireComponent(typeof(Army))]
public class ArmyFlagVisuals : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite outlineSprite;
    [SerializeField] private Sprite fillSprite;

    [Header("Layout")]
    [SerializeField] private Vector3 localOffset = new Vector3(0.35f, 1.05f, 0f);
    [SerializeField] private Vector3 localScale = new Vector3(0.45f, 0.45f, 1f);
    [SerializeField] private int sortingOrderOffset = 3;

    [Header("Fallback")]
    [SerializeField] private Color fallbackColor = Color.white;

    private Army army;
    private SpriteRenderer outlineRenderer;
    private SpriteRenderer fillRenderer;
    private Sprite fallbackOutlineSprite;
    private Sprite fallbackFillSprite;
    private NationModel lastOwner;

    private void Awake()
    {
        army = GetComponent<Army>();
        EnsureFlagRenderers();
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        GameEvents.OnPlayerNationChanged += OnPlayerNationChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
        GameEvents.OnPlayerNationChanged -= OnPlayerNationChanged;
    }

    private void Start()
    {
        RefreshFlag();
    }

    private void LateUpdate()
    {
        if (army == null) return;

        NationModel owner = army.OwnerNation;
        if (owner != lastOwner)
        {
            RefreshFlag();
        }
    }

    [ContextMenu("Refresh Flag")]
    public void RefreshFlag()
    {
        EnsureFlagRenderers();

        NationModel owner = army != null ? army.OwnerNation : null;
        Color ownerColor = GetOwnerColor(owner);

        if (outlineRenderer != null)
        {
            outlineRenderer.sprite = outlineSprite != null ? outlineSprite : GetFallbackOutlineSprite();
            outlineRenderer.enabled = outlineRenderer.sprite != null;
        }

        if (fillRenderer != null)
        {
            fillRenderer.sprite = fillSprite != null ? fillSprite : GetFallbackFillSprite();
            fillRenderer.color = ownerColor;
            fillRenderer.enabled = fillRenderer.sprite != null;
        }

        ApplySorting();
        lastOwner = owner;
    }

    private void EnsureFlagRenderers()
    {
        Transform root = transform.Find("ArmyFlag");
        if (root == null)
        {
            GameObject rootObject = new GameObject("ArmyFlag");
            root = rootObject.transform;
            root.SetParent(transform);
        }

        root.localPosition = localOffset;
        root.localScale = localScale;
        root.localRotation = Quaternion.identity;

        fillRenderer = EnsureChildRenderer(root, "Fill", fillRenderer);
        outlineRenderer = EnsureChildRenderer(root, "Outline", outlineRenderer);
        ApplySorting();
    }

    private SpriteRenderer EnsureChildRenderer(Transform parent, string childName, SpriteRenderer cachedRenderer)
    {
        if (cachedRenderer != null) return cachedRenderer;

        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(parent);
            child.localPosition = Vector3.zero;
            child.localScale = Vector3.one;
            child.localRotation = Quaternion.identity;
        }

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = child.gameObject.AddComponent<SpriteRenderer>();

        return renderer;
    }

    private void ApplySorting()
    {
        SpriteRenderer armyRenderer = GetArmyRenderer();
        string sortingLayerName = armyRenderer != null ? armyRenderer.sortingLayerName : "Default";
        int baseOrder = armyRenderer != null ? armyRenderer.sortingOrder : 0;

        if (fillRenderer != null)
        {
            fillRenderer.sortingLayerName = sortingLayerName;
            fillRenderer.sortingOrder = baseOrder + sortingOrderOffset;
        }

        if (outlineRenderer != null)
        {
            outlineRenderer.sortingLayerName = sortingLayerName;
            outlineRenderer.sortingOrder = baseOrder + sortingOrderOffset + 1;
        }
    }

    private SpriteRenderer GetArmyRenderer()
    {
        SpriteRenderer ownRenderer = GetComponent<SpriteRenderer>();
        if (ownRenderer != null)
            return ownRenderer;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer == fillRenderer || renderer == outlineRenderer)
                continue;

            return renderer;
        }

        return null;
    }

    private Color GetOwnerColor(NationModel owner)
    {
        if (owner == null || string.IsNullOrEmpty(owner.nationColor))
            return fallbackColor;

        return NationLoader.HexToColor(owner.nationColor);
    }

    private Sprite GetFallbackFillSprite()
    {
        if (fallbackFillSprite != null) return fallbackFillSprite;

        Texture2D texture = new Texture2D(12, 8, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool pole = x <= 1;
                texture.SetPixel(x, y, pole ? clear : Color.white);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();
        fallbackFillSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.15f, 0.5f), texture.height);
        return fallbackFillSprite;
    }

    private Sprite GetFallbackOutlineSprite()
    {
        if (fallbackOutlineSprite != null) return fallbackOutlineSprite;

        Texture2D texture = new Texture2D(12, 8, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = Color.black;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool pole = x <= 1;
                bool border = x == 2 || x == texture.width - 1 || y == 0 || y == texture.height - 1;
                texture.SetPixel(x, y, pole || border ? outline : clear);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();
        fallbackOutlineSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.15f, 0.5f), texture.height);
        return fallbackOutlineSprite;
    }

    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        RefreshFlag();
    }

    private void OnPlayerNationChanged(NationModel newNation)
    {
        RefreshFlag();
    }
}
