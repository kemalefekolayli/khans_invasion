using System.Collections.Generic;
using UnityEngine;

public class QuestPanelController : MonoBehaviour
{
    public static QuestPanelController Instance { get; private set; }
    
    [Header("Panel")]
    public GameObject questPanel;
    
    [Header("Quest Items")]
    public List<QuestItemUI> questItems = new List<QuestItemUI>();
    
    private bool isOpen = false;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (questPanel != null)
            questPanel.SetActive(false);
    }
    
    private void Start()
    {
        InitializeQuestItems();
    }
    
    private void OnEnable()
    {
        GameEvents.OnProvincePanelClosed += OnOtherPanelClosed;
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvincePanelClosed -= OnOtherPanelClosed;
    }
    
    private void InitializeQuestItems()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null) return;
        
        for (int i = 0; i < questItems.Count && i < manager.allQuests.Count; i++)
        {
            questItems[i].Initialize(manager.allQuests[i]);
        }
    }
    
    private void OnOtherPanelClosed()
    {
        // Other panels might need to trigger refresh
    }
    
    public void OpenPanel()
    {
        if (isOpen) return;
        
        // Close other panels first
        GameEvents.ProvincePanelClosed();
        
        if (questPanel != null)
        {
            questPanel.SetActive(true);
            isOpen = true;
            
            // Refresh all quest states
            foreach (var item in questItems)
            {
                item.UpdateVisualState();
            }
            
            GameEvents.QuestPanelOpened();
            Debug.Log("[QuestPanelController] Panel opened");
        }
    }
    
    public void ClosePanel()
    {
        if (!isOpen) return;
        
        if (questPanel != null)
        {
            questPanel.SetActive(false);
            isOpen = false;
            
            GameEvents.QuestPanelClosed();
            Debug.Log("[QuestPanelController] Panel closed");
        }
    }
    
    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }
    
    public bool IsOpen => isOpen;
}
