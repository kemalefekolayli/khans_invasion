using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    [Header("Harita Görseli")]
    public Texture2D provinceMapTexture;
    
    [Header("Background Haritası")]
    public Transform mapBackground;
    
    [Header("Manuel Ayarlar")]
    public float manualMapScale = 10f;
    
    [Header("Province Görünümü")]
    public Color defaultProvinceColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public bool showProvinceBorders = true;
    public Color borderColor = Color.black;
    
    [Header("Performans")]
    public int provincesPerFrame = 5;
    
    private Dictionary<Color32, ProvinceModel> colorToProvince = new Dictionary<Color32, ProvinceModel>();
    private Dictionary<Color32, HashSet<Vector2Int>> colorToPixels = new Dictionary<Color32, HashSet<Vector2Int>>();
    private Color32[] allPixels;
    
    private Vector3 mapWorldSize;
    private Vector3 mapWorldCenter;
    private float worldToPixelRatio; // Dünya birimi başına kaç pixel

    void Start()
    {
        if (mapBackground == null)
        {
            GameObject bgObject = GameObject.Find("MapBackground");
            if (bgObject != null)
            {
                mapBackground = bgObject.transform;
            }
        }
        
        CalculateMapScale();
        StartCoroutine(GenerateProvinces());
    }

    void CalculateMapScale()
    {
        if (mapBackground != null)
        {
            SpriteRenderer bgRenderer = mapBackground.GetComponent<SpriteRenderer>();
            if (bgRenderer != null && bgRenderer.sprite != null)
            {
                // Background'ın GERÇEK dünya boyutunu al
                Bounds bounds = bgRenderer.bounds;
                mapWorldSize = bounds.size;
                mapWorldCenter = bounds.center;
                
                // Dünya birimi başına pixel oranını hesapla
                worldToPixelRatio = provinceMapTexture.width / mapWorldSize.x;
                
                Debug.Log($"✓ MapBackground kullanılıyor");
                Debug.Log($"✓ Texture boyutu: {provinceMapTexture.width}x{provinceMapTexture.height}");
                Debug.Log($"✓ Harita dünya boyutu: {mapWorldSize}");
                Debug.Log($"✓ Harita merkezi: {mapWorldCenter}");
                Debug.Log($"✓ World to Pixel Ratio: {worldToPixelRatio}");
                return;
            }
        }
        
        Debug.LogWarning("MapBackground kullanılamıyor - manuel ayarlar");
        
        float textureAspect = (float)provinceMapTexture.width / provinceMapTexture.height;
        mapWorldSize = new Vector3(manualMapScale, manualMapScale / textureAspect, 1);
        mapWorldCenter = Vector3.zero;
        worldToPixelRatio = provinceMapTexture.width / manualMapScale;
        
        Debug.Log($"✓ Manuel ölçek: {manualMapScale}");
        Debug.Log($"✓ World to Pixel Ratio: {worldToPixelRatio}");
    }

    IEnumerator GenerateProvinces()
    {
        Debug.Log("=== HARİTA TARAMASI BAŞLADI ===");
        float startTime = Time.realtimeSinceStartup;
        
        allPixels = provinceMapTexture.GetPixels32();
        int width = provinceMapTexture.width;
        int height = provinceMapTexture.height;
        
        for (int i = 0; i < allPixels.Length; i++)
        {
            Color32 pixelColor = allPixels[i];
            
            if (pixelColor.a < 128 || IsGrayish(pixelColor))
                continue;
            
            if (!colorToPixels.ContainsKey(pixelColor))
            {
                colorToPixels[pixelColor] = new HashSet<Vector2Int>();
            }
            
            int x = i % width;
            int y = i / width;
            colorToPixels[pixelColor].Add(new Vector2Int(x, y));
            
            if (i % 10000 == 0)
            {
                yield return null;
            }
        }
        
        float scanTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"✓ Tarama tamamlandı: {colorToPixels.Count} province bulundu ({scanTime:F2} saniye)");
        
        yield return StartCoroutine(CreateProvincesGradually());
        
        float totalTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"=== TÜM SİSTEM HAZIR === Toplam süre: {totalTime:F2} saniye");
        
        allPixels = null;
    }

    IEnumerator CreateProvincesGradually()
    {
        long provinceIdCounter = 1;
        int processedCount = 0;
        int totalProvinces = colorToPixels.Count;
        
        List<KeyValuePair<Color32, HashSet<Vector2Int>>> provinceList = colorToPixels.ToList();
        
        for (int i = 0; i < provinceList.Count; i += provincesPerFrame)
        {
            int batchEnd = Mathf.Min(i + provincesPerFrame, provinceList.Count);
            
            for (int j = i; j < batchEnd; j++)
            {
                var kvp = provinceList[j];
                Color32 provinceColor = kvp.Key;
                HashSet<Vector2Int> pixels = kvp.Value;
                
                if (pixels.Count < 10)
                {
                    processedCount++;
                    continue;
                }
                
                GameObject provinceObj = new GameObject($"Province_{provinceIdCounter}");
                provinceObj.transform.SetParent(transform);
                
                ProvinceModel province = provinceObj.AddComponent<ProvinceModel>();
                province.provinceId = provinceIdCounter++;
                province.provinceName = provinceObj.name;
                province.provinceColor = new Color(
                    provinceColor.r / 255f,
                    provinceColor.g / 255f,
                    provinceColor.b / 255f,
                    provinceColor.a / 255f
                );
                
                CreateProvinceSprite(province, pixels, provinceColor);
                
                colorToProvince[provinceColor] = province;
                processedCount++;
            }
            
            float progress = (float)processedCount / totalProvinces * 100f;
            if (processedCount % 20 == 0)
            {
                Debug.Log($"İlerleme: {processedCount}/{totalProvinces} ({progress:F1}%)");
            }
            
            yield return null;
        }
        
        Debug.Log("✓ Province oluşturma tamamlandı!");
    }

    void CreateProvinceSprite(ProvinceModel province, HashSet<Vector2Int> pixels, Color32 originalProvinceColor)
{
    int minX = int.MaxValue, maxX = int.MinValue;
    int minY = int.MaxValue, maxY = int.MinValue;
    
    foreach (var pixel in pixels)
    {
        if (pixel.x < minX) minX = pixel.x;
        if (pixel.x > maxX) maxX = pixel.x;
        if (pixel.y < minY) minY = pixel.y;
        if (pixel.y > maxY) maxY = pixel.y;
    }
    
    int width = maxX - minX + 1;
    int height = maxY - minY + 1;
    
    // Texture oluştur
    Texture2D provinceTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    provinceTexture.filterMode = FilterMode.Point;
    
    Color32[] colors = new Color32[width * height];
    Color32 fillColor = new Color32(
        (byte)(defaultProvinceColor.r * 255),
        (byte)(defaultProvinceColor.g * 255),
        (byte)(defaultProvinceColor.b * 255),
        255
    );
    Color32 edgeColor = new Color32(
        (byte)(borderColor.r * 255),
        (byte)(borderColor.g * 255),
        (byte)(borderColor.b * 255),
        255
    );
    
    for (int i = 0; i < colors.Length; i++)
    {
        colors[i] = new Color32(0, 0, 0, 0);
    }
    
    foreach (Vector2Int pixel in pixels)
    {
        int localX = pixel.x - minX;
        int localY = pixel.y - minY;
        int index = localY * width + localX;
        
        if (index >= 0 && index < colors.Length)
        {
            if (showProvinceBorders && IsEdgePixel(pixel, pixels))
            {
                colors[index] = edgeColor;
            }
            else
            {
                colors[index] = fillColor;
            }
        }
    }
    
    provinceTexture.SetPixels32(colors);
    provinceTexture.Apply(false, false);
    
    // SPRITE OLUŞTUR - PIVOT 0,0 KULLAN!
    Sprite provinceSprite = Sprite.Create(
        provinceTexture,
        new Rect(0, 0, width, height),
        Vector2.zero, // PIVOT = SOL ALT KÖŞE!
        100f // Sabit PPU - sonra scale ile ayarla
    );
    
    SpriteRenderer sr = province.gameObject.AddComponent<SpriteRenderer>();
    sr.sprite = provinceSprite;
    sr.sortingOrder = 1;
    
    // POZİSYON - DÜZELTİLMİŞ HESAPLAMA
    // Sol-alt köşenin pixel pozisyonu
    float leftPixel = minX;
    float bottomPixel = minY;
    
    // Background'ın sol-alt köşesini bul
    Vector3 bgBottomLeft = mapWorldCenter - mapWorldSize * 0.5f;
    
    // Pixel'i dünya koordinatına çevir
    float pixelToWorld = mapWorldSize.x / provinceMapTexture.width;
    
    Vector3 worldPos = new Vector3(
        bgBottomLeft.x + leftPixel * pixelToWorld,
        bgBottomLeft.y + bottomPixel * pixelToWorld,
        0
    );
    
    province.transform.position = worldPos;
    
    // SCALE - Sprite'ın gerçek boyutunu background'a uydur
    float spriteScale = pixelToWorld * 100f; // 100f = sprite PPU
    province.transform.localScale = new Vector3(spriteScale, spriteScale, 1);
    
    province.spriteRenderer = sr;
    
    PolygonCollider2D collider = province.gameObject.AddComponent<PolygonCollider2D>();
    
    if (province.provinceId <= 3)
    {
        Debug.Log($"Province {province.provinceId}: Pos={worldPos}, MinXY=({minX},{minY}), Size={width}x{height}, Scale={spriteScale}");
    }
}
    bool IsEdgePixel(Vector2Int pixel, HashSet<Vector2Int> provincePixels)
    {
        return !provincePixels.Contains(new Vector2Int(pixel.x - 1, pixel.y)) ||
               !provincePixels.Contains(new Vector2Int(pixel.x + 1, pixel.y)) ||
               !provincePixels.Contains(new Vector2Int(pixel.x, pixel.y - 1)) ||
               !provincePixels.Contains(new Vector2Int(pixel.x, pixel.y + 1));
    }

bool IsGrayish(Color32 color)
{
    // Daha hassas tespit
    float avg = (color.r + color.g + color.b) / 3f;
    float variance = Mathf.Abs(color.r - avg) + Mathf.Abs(color.g - avg) + Mathf.Abs(color.b - avg);
    return variance < 10f; // 25'ten 10'a düşür - daha az province atlanır
}
}