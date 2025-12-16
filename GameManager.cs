using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Harita Prefab'ı")]
    public GameObject completeMapPrefab; // CompleteMap prefab'ını buraya sürükle

    [Header("GUI Prefab")]
    public GameObject topLeftGUIPrefab; // CompleteMap prefab'ını buraya sürükle
    
    [Header("Ayarlar")]
    public bool loadMapOnStart = true;
    
    private GameObject currentMap;
    private GameObject currentGUI;

    void Start()
    {
        if (loadMapOnStart)
        {
            LoadMap();
        }
    }

    public void LoadMap()
    {
    
        // Yeni haritayı yükle
        currentMap = Instantiate(completeMapPrefab);

        // load player gui
        currentGUI = Instantiate(topLeftGUIPrefab);
    }

}