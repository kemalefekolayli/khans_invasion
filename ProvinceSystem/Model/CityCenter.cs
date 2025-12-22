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

    public ProvinceModel Province => province;

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
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CityCenter:{gameObject.name}] {message}");
        }
    }
    
    // Draw gizmo in editor to visualize detection radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}