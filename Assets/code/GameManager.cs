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
    private Transform capitalTransform;

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
        
        // Resolve the capital once - reused by SetPlayerCapital later
        if (currentMap != null)
        {
            capitalTransform = currentMap.transform.Find(capitalProvinceObjectName);
            if (capitalTransform == null)
            {
                GameLog.Error(GameLogCategory.Core, $"[GameManager] Capital province '{capitalProvinceObjectName}' not found in map prefab!");
            }
        }

        // Spawn horse AFTER map
        if (horsePrefab == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GameManager] horsePrefab not assigned!");
        }
        else if (capitalTransform != null)
        {
            // Use world position, not local
            Vector3 horseStartPosition = capitalTransform.position;

            // Spawn with identity rotation, NOT as child of map
            horse = Instantiate(horsePrefab, horseStartPosition, Quaternion.identity);

            // camera location setting
            if (cameraController != null)
            {
                cameraController.SetCameraPosition(horse.transform.position);
            }
            else
            {
                GameLog.Warning(GameLogCategory.Core, "[GameManager] cameraController not assigned - skipping camera setup");
            }

            if (interactionButtonPrefab != null)
            {
                interactionGUI = Instantiate(interactionButtonPrefab);
            }
            else
            {
                GameLog.Warning(GameLogCategory.Core, "[GameManager] interactionButtonPrefab not assigned!");
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

        if (capitalTransform == null)
        {
            return;
        }

        ProvinceModel capitalProvince = capitalTransform.GetComponent<ProvinceModel>();
        if (capitalProvince == null)
        {
            GameLog.Error(GameLogCategory.Core, $"[GameManager] '{capitalProvinceObjectName}' has no ProvinceModel component!");
            return;
        }

        if (nationController == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GameManager] nationController not assigned - cannot set player capital!");
            return;
        }

        // Now PlayerNation.currentNation is guaranteed to be set
        nationController.SetNationCapital(PlayerNation.Instance.currentNation, capitalProvince);

    }
}