using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Harita Prefab'ı")]
    public GameObject completeMapPrefab; // CompleteMap prefab'ını buraya sürükle
    
    [Header("Ayarlar")]
    public bool loadMapOnStart = true;
    
    private GameObject currentMap;

    void Start()
    {
        if (loadMapOnStart)
        {
            LoadMap();
        }
    }

    public void LoadMap()
    {
        if (completeMapPrefab == null)
        {
            Debug.LogError("CompleteMap prefab atanmamış!");
            return;
        }
    
        // Yeni haritayı yükle
        currentMap = Instantiate(completeMapPrefab);
    }

}