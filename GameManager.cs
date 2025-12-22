using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Harita Prefab'ı")]
    public GameObject completeMapPrefab;

    [Header("GUI Prefab")]
    public GameObject topLeftGUIPrefab;
    [Header("Horse Prefab")]
    public GameObject horsePrefab;
    
    [Header("Ayarlar")]
    public bool loadMapOnStart = true;

    [Header("Kamera")]
    public CameraController cameraController;
    
    private GameObject currentMap;
    private GameObject horse;
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
    
    // Spawn horse AFTER map
    if (horsePrefab != null && currentMap != null)
    {
        Transform startPosTransform = currentMap.transform.Find("Province_104");
        
        if (startPosTransform != null)
        {
            // Use world position, not local
            Vector3 horseStartPosition = startPosTransform.position;
            
            // Spawn with identity rotation, NOT as child of map
            horse = Instantiate(horsePrefab, horseStartPosition, Quaternion.identity);
            
            // camera location setting 
            cameraController.SetCameraPosition(horse.transform.position);
            Debug.Log($"✓ Horse spawned at {horseStartPosition}");
        }
        else
        {
            Debug.LogError("Province_104 not found in map!");
        }
    }
    
    // Fire event - map is loaded
    Invoke(nameof(FireMapLoadedEvent), 0.5f);
    }

    private void FireMapLoadedEvent()
    {
        GameEvents.MapLoaded();
    }
}