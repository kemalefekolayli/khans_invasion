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
    public ProvinceModel Province => province;

    private enum SpriteState
    {
    Star,
    Otag
    }
    private SpriteState currentState;
    void OnEnable()
    {
        GameEvents.OnBuildingConstructed += OnBuildingConstructed;
        
    }
    void OnDisable()
    {
        GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
        
    }
    private void OnBuildingConstructed(ProvinceModel prov, string buildingType)
    {
        if (prov == province)
        {
            SetBuildingOverlay(buildingType, true);
        }
    }

public void SwitchSprites()
{   
    if (currentState == SpriteState.Otag)
    {
        currentState = SpriteState.Star;
        spriteRenderer.sprite = starSprite;
        
        // Star sprite'ı için uygun boyut (Haritaya göre ayarla)
        transform.localScale = new Vector3(0.05f, 0.05f, 1f); 
        
        // Eğer rengi beyaz yapmak istersen (sarılaşmayı önlemek için)
        spriteRenderer.color = Color.white; 
    }
    else
    {
        currentState = SpriteState.Otag;
        spriteRenderer.sprite = otagSprite;
        
        // Otag sprite'ı için uygun boyut
        transform.localScale = new Vector3(0.02f, 0.02f, 1f);
        
        spriteRenderer.color = Color.white;
    }
}
    private void Awake()
    {
        EnsureCollider();
        
        if (province == null)
        {
            province = GetComponentInParent<ProvinceModel>();
        }
        gameObject.tag = "CityCenter";
        UpdateIcons();
    }


    private void EnsureCollider()
    {
        cityCollider = GetComponent<CircleCollider2D>();
        
        if (cityCollider == null)
        {
            cityCollider = gameObject.AddComponent<CircleCollider2D>();

        }
        
        cityCollider.radius = detectionRadius;
        cityCollider.isTrigger = true;

    }

    public void SetProvince(ProvinceModel targetProvince)
    {
        province = targetProvince;
    }

    public NationModel GetOwner()
    {
        return province?.provinceOwner;
    }

    public bool IsOwnedByPlayer()
    {
        if (province?.provinceOwner == null) 
        {

            return false;
        }
        bool isPlayer = province.provinceOwner.isPlayer;

        return isPlayer;
    }

    // Visual feedback when horse is on city center
    public void SetHighlight(bool highlighted)
    {

        
        if (spriteRenderer == null) 
        {

            return;
        }
        
        if (highlighted)
        {
            spriteRenderer.color = Color.yellow;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }
    
    private void UpdateIcons()
    {

        foreach (string building in province.buildings)
        {
            SetBuildingOverlay(building, true);
        }
    }

    private void SetBuildingOverlay(string buildingType, bool active)
    {
        switch (buildingType)
        {
            case "Farm":
                SetActive(farmIcon, active);
                break;
            case "Barracks":
                SetActive(barrackIcon, active);
                break;
            case "Fortress":
                SetActive(fortIcon, active);
                break;
            case "Housing":
                SetActive(houseIcon, active);
                break;
            case "Trade_Building":
                SetActive(tradeIcon, active);
                break;
        }
    }

     private void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);

        }
    }
}