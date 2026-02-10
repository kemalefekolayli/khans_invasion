using UnityEngine;
using UnityEngine.UI;

public class QuestPanelButton : MonoBehaviour
{
    [Header("References")]
    public Button button;
    
    [Header("Prefab (Optional)")]
    [Tooltip("If not set, will try to load from Resources/QuestPanelCanvas")]
    public GameObject questPanelPrefab;
    
    private QuestPanelController cachedController;
    
    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }
    
    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClicked);
    }
    
    private void OnButtonClicked()
    {
        // Try Instance first
        if (QuestPanelController.Instance != null)
        {
            QuestPanelController.Instance.TogglePanel();
            return;
        }
        
        // Try cached controller
        if (cachedController == null)
        {
            cachedController = FindFirstObjectByType<QuestPanelController>();
        }
        
        if (cachedController != null)
        {
            cachedController.TogglePanel();
            return;
        }
        
        // Not found - instantiate prefab
        InstantiatePrefab();
        
        // Now try again
        if (cachedController != null)
        {
            cachedController.OpenPanel();
        }
    }
    
    private void InstantiatePrefab()
    {
        GameObject prefab = questPanelPrefab;
        
        // Try loading from Resources if not assigned
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("QuestPanelCanvas");
        }
        
        if (prefab == null)
        {
            Debug.LogError("[QuestPanelButton] Cannot find QuestPanelCanvas prefab! Either assign it in Inspector or put it in Resources folder.");
            return;
        }
        
        GameObject instance = Instantiate(prefab);
        instance.name = "QuestPanelCanvas";
        
        cachedController = instance.GetComponent<QuestPanelController>();
        
        if (cachedController == null)
        {
            Debug.LogError("[QuestPanelButton] QuestPanelController component not found on prefab!");
        }
        else
        {
            Debug.Log("[QuestPanelButton] QuestPanelCanvas instantiated successfully");
        }
    }
}
