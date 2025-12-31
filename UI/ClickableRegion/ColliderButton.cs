using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class ColliderButton : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onClick;
    
    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    
    private Camera mainCam;
    private bool isHovered = false;

    void Start()
    {
        mainCam = Camera.main;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        Debug.Log($"[ColliderButton] {gameObject.name} initialized. Collider: {GetComponent<Collider2D>() != null}");
    }

    void Update()
    {
        if (Mouse.current == null) return;
        
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = mainCam.ScreenToWorldPoint(mouseScreen);
        
        // Her frame logla (test için)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log($"[ColliderButton] Click at screen: {mouseScreen}, world: {mouseWorld}");
            
            // Tüm collider'ları kontrol et
            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);
            Debug.Log($"[ColliderButton] Found {hits.Length} colliders at click point");
            
            foreach (var hit in hits)
            {
                Debug.Log($"  - {hit.gameObject.name}");
            }
        }
        
        // Hover check
        Collider2D hit2 = Physics2D.OverlapPoint(mouseWorld);
        bool nowHovered = (hit2 != null && hit2.gameObject == gameObject);
        
        if (nowHovered != isHovered)
        {
            isHovered = nowHovered;
            Debug.Log($"[ColliderButton] Hover changed: {isHovered}");
            if (spriteRenderer != null)
                spriteRenderer.color = isHovered ? hoverColor : normalColor;
        }
        
        // Click check
        if (isHovered && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log($"[ColliderButton] {gameObject.name} CLICKED!");
            onClick?.Invoke();
        }
    }
}