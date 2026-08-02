using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class QuestItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image questBoxImage;
    public TextMeshProUGUI questTitleText;
    public Image arrowImage;
    public Button button;
    
    [Header("Colors")]
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color activeColor = Color.white;
    public Color completedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color claimedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    
    private QuestData questData;
    private QuestManager questManager;
    
    public QuestData QuestData => questData;
    
    public void Initialize(QuestData data)
    {
        questData = data;
        questManager = QuestManager.Instance;
        
        if (questTitleText != null)
            questTitleText.text = $"QUEST {questData.questId}";
        
        if (button != null)
            button.onClick.AddListener(OnClick);
        
        UpdateVisualState();
    }
    
    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgressUpdated += OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestUpdated;
            QuestManager.Instance.OnQuestClaimed += OnQuestUpdated;
        }
    }
    
    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgressUpdated -= OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestUpdated;
            QuestManager.Instance.OnQuestClaimed -= OnQuestUpdated;
        }
    }
    
    private void OnQuestUpdated(int questId)
    {
        UpdateVisualState();
    }
    
    public void UpdateVisualState()
    {
        if (questData == null || questManager == null) return;
        
        bool isUnlocked = questManager.IsQuestUnlocked(questData.questId);
        bool isCompleted = questManager.IsQuestCompleted(questData.questId);
        bool isClaimed = questManager.IsQuestClaimed(questData.questId);
        
        if (isClaimed)
        {
            SetVisualState(claimedColor, false, "COMPLETED");
        }
        else if (isCompleted && isUnlocked)
        {
            SetVisualState(completedColor, true, "CLAIM!");
        }
        else if (isCompleted && !isUnlocked)
        {
            // Quest is done but prerequisite not claimed yet
            SetVisualState(completedColor, false, "DONE");
        }
        else if (isUnlocked)
        {
            int progress = questManager.GetQuestProgress(questData.questId);
            SetVisualState(activeColor, false, $"{progress}/{questData.targetCount}");
        }
        else
        {
            int progress = questManager.GetQuestProgress(questData.questId);
            if (progress > 0)
                SetVisualState(lockedColor, false, $"{progress}/{questData.targetCount}");
            else
                SetVisualState(lockedColor, false, "LOCKED");
        }
    }
    
    private void SetVisualState(Color color, bool highlight, string statusText)
    {
        if (questBoxImage != null)
            questBoxImage.color = color;
        
        if (arrowImage != null)
            arrowImage.color = color;
        
        if (button != null)
            button.interactable = highlight || questManager.IsQuestCompleted(questData.questId);
    }
    
    private void OnClick()
    {
        if (questManager == null || questData == null) return;
        
        if (questManager.IsQuestCompleted(questData.questId) && !questManager.IsQuestClaimed(questData.questId))
        {
            questManager.TryClaimQuest(questData.questId);
        }
    }
    
    // ===== HOVER EVENTS =====
    

    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (questData == null) return;
        

        
        if (QuestTooltip.Instance != null)
        {
            QuestTooltip.Instance.Show(questData, eventData.position);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {

        
        if (QuestTooltip.Instance != null)
        {
            QuestTooltip.Instance.Hide();
        }
    }
    
}
