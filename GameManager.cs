using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Harita Prefab'ı")]
    public GameObject completeMapPrefab;
    public NationController nationController;

    [Header("GUI Prefab")]
    public GameObject topLeftGUIPrefab;
    public GameObject interactionButtonPrefab;
    [Header("Horse Prefab")]
    public GameObject horsePrefab;
    
    [Header("Ayarlar")]
    public bool loadMapOnStart = true;

    [Header("Kamera")]
    public CameraController cameraController;
    
    [Header("Capital Settings")]
    public string capitalProvinceObjectName = "Province_104";
    
    private GameObject currentMap;
    private GameObject horse;
    private GameObject currentGUI;
    private GameObject interactionGUI;

    void OnEnable()
    {
        // Subscribe to PlayerNationReady to set capital at the right time
        GameEvents.OnPlayerNationReady += OnPlayerNationReady;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
    }

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
        }
        // Load player GUI
        if (topLeftGUIPrefab != null)
        {
            currentGUI = Instantiate(topLeftGUIPrefab);

        }
        
        // Spawn horse AFTER map
        if (horsePrefab != null && currentMap != null)
        {
            Transform startPosTransform = currentMap.transform.Find(capitalProvinceObjectName);
            
            if (startPosTransform != null)
            {
                // Use world position, not local
                Vector3 horseStartPosition = startPosTransform.position;
                
                // Spawn with identity rotation, NOT as child of map
                horse = Instantiate(horsePrefab, horseStartPosition, Quaternion.identity);
                
                // camera location setting 
                cameraController.SetCameraPosition(horse.transform.position);
                interactionGUI = Instantiate(interactionButtonPrefab);
            }
        }
        
        Invoke(nameof(FireMapLoadedEvent), 0.5f);
    }

    private void FireMapLoadedEvent()
    {
        GameEvents.MapLoaded();
    }

    private void OnPlayerNationReady()
    {
        SetPlayerCapital();
    }

    private void SetPlayerCapital()
    {
        if (currentMap == null)
        {
            return;
        }

        if (PlayerNation.Instance == null || PlayerNation.Instance.currentNation == null)
        {
            return;
        }

        Transform capitalTransform = currentMap.transform.Find(capitalProvinceObjectName);
        if (capitalTransform == null)
        {
            return;
        }

        ProvinceModel capitalProvince = capitalTransform.GetComponent<ProvinceModel>();
        if (capitalProvince == null)
        {
            return;
        }
        // Now PlayerNation.currentNation is guaranteed to be set
        nationController.SetNationCapital(PlayerNation.Instance.currentNation, capitalProvince);

    }
}