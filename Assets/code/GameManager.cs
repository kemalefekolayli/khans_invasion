using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Map Prefab")]
    public GameObject completeMapPrefab;
    public NationController nationController;

    [Header("GUI Prefab")]
    public GameObject topLeftGUIPrefab;
    public GameObject interactionButtonPrefab;

    [Header("Horse Prefab")]
    public GameObject horsePrefab;

    [Header("Settings")]
    public bool loadMapOnStart = true;

    [Header("Camera")]
    public CameraController cameraController;

    [Header("Capital Settings")]
    public string capitalProvinceObjectName = "Province_104";

    private GameObject currentMap;
    private GameObject horse;
    private GameObject currentGUI;
    private GameObject interactionGUI;
    private Transform capitalTransform;

    private void OnEnable()
    {
        GameEvents.OnPlayerNationReady += OnPlayerNationReady;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerNationReady -= OnPlayerNationReady;
    }

    private void Start()
    {
        if (loadMapOnStart)
        {
            LoadMap();
        }
    }

    public void LoadMap()
    {
        if (completeMapPrefab != null)
        {
            currentMap = Instantiate(completeMapPrefab);
        }
        else
        {
            GameLog.Error(GameLogCategory.Core, "[GameManager] Complete map prefab is not assigned.");
            return;
        }

        if (topLeftGUIPrefab != null)
        {
            currentGUI = Instantiate(topLeftGUIPrefab);
            RuntimeScreenCanvasPolicy.Apply(currentGUI, new Vector2(1920f, 1080f), 0.5f);
        }

        capitalTransform = currentMap.transform.Find(capitalProvinceObjectName);
        if (capitalTransform == null)
        {
            GameLog.Error(GameLogCategory.Core, $"[GameManager] Fallback capital province '{capitalProvinceObjectName}' was not found in the map prefab.");
        }

        if (horsePrefab == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GameManager] Horse prefab is not assigned.");
        }
        else if (capitalTransform != null)
        {
            horse = Instantiate(horsePrefab, capitalTransform.position, Quaternion.identity);

            if (cameraController != null)
            {
                cameraController.SetCameraPosition(horse.transform.position);
            }

            if (interactionButtonPrefab != null)
            {
                interactionGUI = Instantiate(interactionButtonPrefab);
                RuntimeScreenCanvasPolicy.Apply(interactionGUI, new Vector2(1920f, 1080f), 0.5f);
                CapitalLootDepositController depositController = interactionGUI.GetComponent<CapitalLootDepositController>();
                if (depositController == null)
                    depositController = interactionGUI.AddComponent<CapitalLootDepositController>();
                depositController.Initialize(interactionGUI);
            }
        }

        GetComponent<RuntimeUIInstaller>()?.Install();
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
        if (currentMap == null || PlayerNation.Instance == null || PlayerNation.Instance.currentNation == null)
        {
            return;
        }

        NationModel playerNation = PlayerNation.Instance.currentNation;
        ProvinceModel prefabCapital = playerNation.capitalProvince;
        if (prefabCapital != null)
        {
            // Capitals can be assigned before PlayerNation marks its nation as player-owned.
            // Re-applying the same capital is idempotent and emits the player visual event now
            // that the player nation is fully initialized.
            nationController?.SetNationCapital(playerNation, prefabCapital);
            MovePlayerToCapital(prefabCapital);
            return;
        }

        if (capitalTransform == null)
        {
            return;
        }

        ProvinceModel fallbackCapital = capitalTransform.GetComponent<ProvinceModel>();
        if (fallbackCapital == null)
        {
            GameLog.Error(GameLogCategory.Core, $"[GameManager] '{capitalProvinceObjectName}' has no ProvinceModel component.");
            return;
        }

        if (nationController == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GameManager] NationController is not assigned; cannot set fallback player capital.");
            return;
        }

        nationController.SetNationCapital(playerNation, fallbackCapital);
        MovePlayerToCapital(fallbackCapital);
    }

    private void MovePlayerToCapital(ProvinceModel capitalProvince)
    {
        if (capitalProvince == null) return;

        if (horse != null)
        {
            horse.transform.position = capitalProvince.transform.position;
        }

        if (cameraController != null)
        {
            cameraController.SetCameraPosition(capitalProvince.transform.position);
        }
    }
}
