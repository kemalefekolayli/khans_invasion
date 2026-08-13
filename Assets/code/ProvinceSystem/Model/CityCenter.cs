using System.Collections.Generic;
using UnityEngine;

public class CityCenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ProvinceModel province;
    [SerializeField] private CircleCollider2D cityCollider;

    [Header("Sprites")]
    [SerializeField] private Sprite otagSprite;
    [SerializeField] private Sprite starSprite;

    [Header("Byzantine Visuals")]
    [SerializeField] private long byzantineNationId = 1;
    [SerializeField] private Sprite byzantineDefaultSprite;
    [SerializeField] private Sprite byzantineFortressSprite;
    [SerializeField, Range(0.05f, 1f)] private float byzantineCityScale = 0.2f;

    [Header("Byzantine Building Overlays")]
    [SerializeField] private Sprite byzantineBarracksSprite;
    [SerializeField] private Sprite byzantineFarmSprite;
    [SerializeField] private Sprite byzantineHousingSprite;
    [SerializeField] private Sprite byzantineTradeSprite;
    [SerializeField, Range(0.02f, 1f)] private float byzantineBuildingScale = 0.08f;
    [SerializeField] private Vector2 byzantineBarracksOffset = new(-0.95f, 0.65f);
    [SerializeField] private Vector2 byzantineFarmOffset = new(0.95f, 0.65f);
    [SerializeField] private Vector2 byzantineHousingOffset = new(-0.95f, -0.65f);
    [SerializeField] private Vector2 byzantineTradeOffset = new(0.95f, -0.65f);

    [Header("Settings")]
    public float detectionRadius = 0.3f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    [Header("Icons")]
    [SerializeField] private GameObject barrackIcon;
    [SerializeField] private GameObject farmIcon;
    [SerializeField] private GameObject houseIcon;
    [SerializeField] private GameObject tradeIcon;
    [SerializeField] private GameObject fortIcon;

    private readonly Dictionary<GameObject, IconVisualState> originalIconStates = new();

    public ProvinceModel Province => province;

    private enum SpriteState { Star, Otag }

    private struct IconVisualState
    {
        public Sprite Sprite;
        public Vector3 Position;
        public Vector3 Scale;
    }

    private SpriteState currentState;

    private void OnEnable()
    {
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        GameEvents.OnProvincesAssigned += OnProvincesAssigned;
    }

    private void OnDisable()
    {
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
        GameEvents.OnProvincesAssigned -= OnProvincesAssigned;
    }

    private void Awake()
    {
        EnsureCollider();
        if (province == null) province = GetComponentInParent<ProvinceModel>();
        gameObject.tag = "CityCenter";
        CacheOriginalIconStates();
        RefreshVisuals();
    }

    private void OnBuildingConstructed(ProvinceModel prov, string buildingType)
    {
        if (prov != province) return;
        UpdateIcons();
        RefreshVisuals();
    }

    private void OnProvinceOwnerChanged(ProvinceModel changedProvince, NationModel previousOwner, NationModel newOwner)
    {
        if (changedProvince != province) return;
        UpdateIcons();
        RefreshVisuals();
    }

    private void OnProvincesAssigned()
    {
        UpdateIcons();
        RefreshVisuals();
    }

    public void SwitchSprites()
    {
        SetCapitalVisual(currentState != SpriteState.Otag);
    }

    public void SetCapitalVisual(bool isCapital)
    {
        if (spriteRenderer == null) return;
        if (UsesByzantineVisuals())
        {
            RefreshVisuals();
            return;
        }

        if (isCapital)
        {
            currentState = SpriteState.Otag;
            spriteRenderer.sprite = otagSprite;
            transform.localScale = new Vector3(0.02f, 0.02f, 1f);
        }
        else if (currentState == SpriteState.Otag)
        {
            currentState = SpriteState.Star;
            spriteRenderer.sprite = starSprite;
            transform.localScale = new Vector3(0.05f, 0.05f, 1f);
        }

        spriteRenderer.color = Color.white;
    }

    private void EnsureCollider()
    {
        cityCollider = GetComponent<CircleCollider2D>();
        if (cityCollider == null) cityCollider = gameObject.AddComponent<CircleCollider2D>();
        cityCollider.radius = detectionRadius;
        cityCollider.isTrigger = true;
    }

    public void SetProvince(ProvinceModel targetProvince)
    {
        province = targetProvince;
        UpdateIcons();
        RefreshVisuals();
    }

    public NationModel GetOwner() => province?.provinceOwner;

    public bool IsOwnedByPlayer() => province?.provinceOwner != null && province.provinceOwner.isPlayer;

    public void SetHighlight(bool highlighted)
    {
        if (spriteRenderer != null) spriteRenderer.color = highlighted ? Color.yellow : Color.white;
    }

    private bool UsesByzantineVisuals()
    {
        return province?.provinceOwner != null
            && province.provinceOwner.nationId == byzantineNationId
            && byzantineDefaultSprite != null;
    }

    private bool HasBuilding(string buildingType) => province?.buildings != null && province.buildings.Contains(buildingType);

    private void RefreshVisuals()
    {
        if (UsesByzantineVisuals())
        {
            bool hasFortress = HasBuilding("Fortress") && byzantineFortressSprite != null;
            spriteRenderer.sprite = hasFortress ? byzantineFortressSprite : byzantineDefaultSprite;
            spriteRenderer.color = Color.white;
            transform.localScale = Vector3.one * byzantineCityScale;

            ApplyByzantineIcon(barrackIcon, byzantineBarracksSprite, byzantineBarracksOffset);
            ApplyByzantineIcon(farmIcon, byzantineFarmSprite, byzantineFarmOffset);
            ApplyByzantineIcon(houseIcon, byzantineHousingSprite, byzantineHousingOffset);
            ApplyByzantineIcon(tradeIcon, byzantineTradeSprite, byzantineTradeOffset);
            SetActive(fortIcon, false);
            return;
        }

        RestoreDefaultIcon(barrackIcon);
        RestoreDefaultIcon(farmIcon);
        RestoreDefaultIcon(houseIcon);
        RestoreDefaultIcon(tradeIcon);
    }

    private void CacheOriginalIconStates()
    {
        CacheOriginalIconState(barrackIcon);
        CacheOriginalIconState(farmIcon);
        CacheOriginalIconState(houseIcon);
        CacheOriginalIconState(tradeIcon);
    }

    private void CacheOriginalIconState(GameObject icon)
    {
        if (icon == null || originalIconStates.ContainsKey(icon)) return;

        SpriteRenderer iconRenderer = icon.GetComponent<SpriteRenderer>();
        originalIconStates[icon] = new IconVisualState
        {
            Sprite = iconRenderer != null ? iconRenderer.sprite : null,
            Position = icon.transform.localPosition,
            Scale = icon.transform.localScale
        };
    }

    private void ApplyByzantineIcon(GameObject icon, Sprite sprite, Vector2 offset)
    {
        if (icon == null || sprite == null) return;

        SpriteRenderer iconRenderer = icon.GetComponent<SpriteRenderer>();
        if (iconRenderer != null) iconRenderer.sprite = sprite;
        icon.transform.localPosition = offset;
        icon.transform.localScale = Vector3.one * byzantineBuildingScale;
    }

    private void RestoreDefaultIcon(GameObject icon)
    {
        if (icon == null || !originalIconStates.TryGetValue(icon, out IconVisualState state)) return;

        SpriteRenderer iconRenderer = icon.GetComponent<SpriteRenderer>();
        if (iconRenderer != null) iconRenderer.sprite = state.Sprite;
        icon.transform.localPosition = state.Position;
        icon.transform.localScale = state.Scale;
    }

    private void UpdateIcons()
    {
        SetActive(barrackIcon, false);
        SetActive(farmIcon, false);
        SetActive(houseIcon, false);
        SetActive(tradeIcon, false);
        SetActive(fortIcon, false);

        if (province?.buildings == null) return;

        foreach (string building in province.buildings) SetBuildingOverlay(building, true);

        if (UsesByzantineVisuals()) SetActive(fortIcon, false);
    }

    private void SetBuildingOverlay(string buildingType, bool active)
    {
        switch (buildingType)
        {
            case "Farm": SetActive(farmIcon, active); break;
            case "Barracks": SetActive(barrackIcon, active); break;
            case "Fortress": SetActive(fortIcon, active && !UsesByzantineVisuals()); break;
            case "Housing": SetActive(houseIcon, active); break;
            case "Trade_Building": SetActive(tradeIcon, active); break;
        }
    }

    private static void SetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}
