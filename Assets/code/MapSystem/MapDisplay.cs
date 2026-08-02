using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    [Header("Harita Görseli")]
    public Sprite mapSprite;
    
    [Header("Ayarlar")]
    public float mapScale = 10f;
    public bool showMap = true;
    
    private SpriteRenderer spriteRenderer;

    void Awake() // Start yerine Awake (MapGenerator'dan önce çalışsın)
    {
        SetupMap();
    }

    void SetupMap()
    {
        // SpriteRenderer ekle veya al
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Sprite ata
        if (mapSprite != null)
        {
            spriteRenderer.sprite = mapSprite;
            spriteRenderer.sortingOrder = -1; // Province'lerin arkasında
            spriteRenderer.enabled = showMap;
            
            // Scale ayarla
            transform.localScale = new Vector3(mapScale, mapScale, 1f);
            
            // Pozisyon = merkez
            transform.position = Vector3.zero;
            

            GameLog.Log(GameLogCategory.Core, $"  Sprite: {mapSprite.name}");
            GameLog.Log(GameLogCategory.Core, $"  Scale: {mapScale}");
            GameLog.Log(GameLogCategory.Core, $"  Bounds: {spriteRenderer.bounds.size}");
        }
        else
        {
            GameLog.Error(GameLogCategory.Core, "MapDisplay: mapSprite atanmamış!");
        }
    }
}