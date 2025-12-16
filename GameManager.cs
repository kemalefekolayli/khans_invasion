using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Harita Prefab'ı")]
    public GameObject completeMapPrefab;

    [Header("GUI Prefab")]
    public GameObject topLeftGUIPrefab;
    
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
        // Load the map prefab
        if (completeMapPrefab != null)
        {
            currentMap = Instantiate(completeMapPrefab);
            Debug.Log("✓ Map prefab instantiated");
        }
        else
        {
            Debug.LogError("CompleteMap prefab not assigned!");
        }

        // Load player GUI
        if (topLeftGUIPrefab != null)
        {
            currentGUI = Instantiate(topLeftGUIPrefab);
            Debug.Log("✓ GUI prefab instantiated");
        }
        
        // Fire event - map is loaded
        // Small delay to ensure all province components are initialized
        Invoke(nameof(FireMapLoadedEvent), 0.5f);
    }

    private void FireMapLoadedEvent()
    {
        GameEvents.MapLoaded();
    }
}