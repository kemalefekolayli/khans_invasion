using UnityEngine;

public class CityCenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ProvinceModel province;
    [SerializeField] private CircleCollider2D cityCollider;

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
    private void Awake()
    {

        
        // Ensure we have a collider for detection
        EnsureCollider();
        
        // Auto-find province if not assigned
        if (province == null)
        {
            province = GetComponentInParent<ProvinceModel>();
        }

        // Set tag for detection
        gameObject.tag = "CityCenter";
        UpdateIcons();

    }

    private void Start()
    {
       
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