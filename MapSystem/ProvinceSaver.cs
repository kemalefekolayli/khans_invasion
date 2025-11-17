#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ProvinceSaver : EditorWindow
{
    [MenuItem("Tools/Province Saver")]
    public static void ShowWindow()
    {
        GetWindow<ProvinceSaver>("Province Saver");
    }

    void OnGUI()
    {
        GUILayout.Label("Province Kaydetme Sistemi", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Tüm Province'leri Kaydet (Sprite + Prefab)"))
        {
            SaveAllProvincesWithSprites();
        }
        
        if (GUILayout.Button("Province'leri Scene'e Yükle"))
        {
            LoadAllProvinces();
        }
        
        if (GUILayout.Button("Tüm Province'leri Sil"))
        {
            DeleteAllProvinces();
        }
    }
            
    void SaveAllProvincesWithSprites()
    {
        MapGenerator mapGen = FindFirstObjectByType<MapGenerator>();
        if (mapGen == null)
        {
            Debug.LogError("MapGenerator bulunamadı!");
            return;
        }
        
        string texturesFolder = "Assets/Textures/Provinces";
        string prefabsFolder = "Assets/Prefabs/Provinces";
        
        if (!Directory.Exists(texturesFolder)) Directory.CreateDirectory(texturesFolder);
        if (!Directory.Exists(prefabsFolder)) Directory.CreateDirectory(prefabsFolder);
        
        ProvinceModel[] provinces = mapGen.GetComponentsInChildren<ProvinceModel>();
        int savedCount = 0;
        
        EditorUtility.DisplayProgressBar("Province Kaydediliyor", "Başlangıç...", 0f);
        
        for (int i = 0; i < provinces.Length; i++)
        {
            ProvinceModel province = provinces[i];
            
            EditorUtility.DisplayProgressBar(
                "Province Kaydediliyor", 
                $"{province.provinceName} ({i+1}/{provinces.Length})", 
                (float)i / provinces.Length
            );
            
            SpriteRenderer sr = province.spriteRenderer;
            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning($"{province.provinceName} sprite'ı yok, atlanıyor...");
                continue;
            }
            
            // TEXTURE'I KAYDET
            Texture2D originalTexture = sr.sprite.texture;
            string texturePath = $"{texturesFolder}/{province.provinceName}.png";
            
            byte[] pngData = originalTexture.EncodeToPNG();
            File.WriteAllBytes(texturePath, pngData);
            AssetDatabase.ImportAsset(texturePath);
            
            // TEXTURE AYARLARI
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.spritePixelsPerUnit = sr.sprite.pixelsPerUnit;
                textureImporter.spritePivot = new Vector2(0, 0);
                textureImporter.isReadable = true;
                
                EditorUtility.SetDirty(textureImporter);
                textureImporter.SaveAndReimport();
            }
            
            // YENİ SPRITE'I YÜK
            Sprite savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            sr.sprite = savedSprite;
            
            // ESKİ COLLIDER'I SİL
            PolygonCollider2D oldCollider = province.GetComponent<PolygonCollider2D>();
            if (oldCollider != null)
            {
                DestroyImmediate(oldCollider);
            }
            
            // PREFAB OLARAK KAYDET
            string prefabPath = $"{prefabsFolder}/{province.provinceName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(province.gameObject, prefabPath);
            
            savedCount++;
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        Debug.Log($"✓ {savedCount} province (texture + sprite + prefab) kaydedildi!");
        EditorUtility.DisplayDialog("Başarılı", $"{savedCount} province kaydedildi!", "Tamam");
    }

    void LoadAllProvinces()
    {
        string prefabsFolder = "Assets/Prefabs/Provinces";
        if (!Directory.Exists(prefabsFolder))
        {
            Debug.LogError("Province prefab'ları bulunamadı!");
            return;
        }
        
        MapGenerator mapGen = FindFirstObjectByType<MapGenerator>();
        if (mapGen == null)
        {
            GameObject mapGenObj = new GameObject("MapGenerator");
            mapGen = mapGenObj.AddComponent<MapGenerator>();
        }
        
        // Eski province'leri temizle
        ProvinceModel[] oldProvinces = mapGen.GetComponentsInChildren<ProvinceModel>();
        foreach (var old in oldProvinces)
        {
            DestroyImmediate(old.gameObject);
        }
        
        // Prefab'ları yükle
        string[] prefabFiles = Directory.GetFiles(prefabsFolder, "*.prefab");
        int loadedCount = 0;
        
        EditorUtility.DisplayProgressBar("Province Yükleniyor", "Başlangıç...", 0f);
        
        for (int i = 0; i < prefabFiles.Length; i++)
        {
            string prefabPath = prefabFiles[i];
            
            EditorUtility.DisplayProgressBar(
                "Province Yükleniyor", 
                $"{Path.GetFileName(prefabPath)} ({i+1}/{prefabFiles.Length})", 
                (float)i / prefabFiles.Length
            );
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(mapGen.transform);
                
                // COLLIDER'I OLUŞTUR
                ProvinceModel province = instance.GetComponent<ProvinceModel>();
                if (province != null)
                {
                    province.EnsureCollider();
                }
                
                loadedCount++;
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        Debug.Log($"✓ {loadedCount} province yüklendi (collider'lar oluşturuldu)!");
        EditorUtility.DisplayDialog("Başarılı", $"{loadedCount} province yüklendi!", "Tamam");
    }

    void DeleteAllProvinces()
    {
        if (!EditorUtility.DisplayDialog("Uyarı", "Tüm province'ler silinecek. Emin misin?", "Evet", "Hayır"))
        {
            return;
        }
        
        MapGenerator mapGen = FindFirstObjectByType<MapGenerator>();
        if (mapGen != null)
        {
            ProvinceModel[] provinces = mapGen.GetComponentsInChildren<ProvinceModel>();
            foreach (var province in provinces)
            {
                DestroyImmediate(province.gameObject);
            }
            Debug.Log($"✓ {provinces.Length} province silindi!");
        }
    }
} // CLASS KAPANIŞ PARANTEZİ - ÇOK ÖNEMLİ!
#endif