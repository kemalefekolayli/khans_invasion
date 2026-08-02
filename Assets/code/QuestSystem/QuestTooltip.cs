using UnityEngine;
using TMPro;

public class QuestTooltip : MonoBehaviour
{
    public static QuestTooltip Instance { get; private set; }
    
    [Header("UI References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    
    [Header("Settings")]
    public Vector2 offset = new Vector2(10f, -10f);
    
    private RectTransform tooltipRect;
    private Canvas parentCanvas;
    private QuestData currentQuest;
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (tooltipPanel != null)
        {
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            
            // Add CanvasGroup to prevent tooltip from blocking mouse events
            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            
            Hide();
        }
    }
    
    public void Show(QuestData quest, Vector3 position)
    {
        if (tooltipPanel == null || quest == null) return;
        
        currentQuest = quest;
        UpdateContent();
        tooltipPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        UpdatePosition(position);
    }
    
    public void Hide()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        currentQuest = null;
    }
    
    public void UpdatePosition(Vector3 screenPosition)
    {
        if (tooltipRect == null || parentCanvas == null) return;

        RectTransform parentRect = tooltipRect.parent as RectTransform;
        if (parentRect == null) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            GetCanvasCamera(),
            out localPoint
        );
        
        Vector2 desiredPivotPosition = localPoint + offset;
        Vector2 tooltipSize = tooltipRect.rect.size;
        Rect parentBounds = parentRect.rect;

        // Clamp the tooltip's pivot, rather than its anchored position. This keeps
        // the whole panel inside the canvas even when the pointer is at an edge.
        Vector2 pivotMinimum = parentBounds.min + Vector2.Scale(tooltipSize, tooltipRect.pivot);
        Vector2 pivotMaximum = parentBounds.max - Vector2.Scale(tooltipSize, Vector2.one - tooltipRect.pivot);
        desiredPivotPosition.x = Mathf.Clamp(desiredPivotPosition.x, pivotMinimum.x, pivotMaximum.x);
        desiredPivotPosition.y = Mathf.Clamp(desiredPivotPosition.y, pivotMinimum.y, pivotMaximum.y);

        Vector2 anchorReference = parentBounds.min + Vector2.Scale(parentBounds.size, tooltipRect.anchorMin);
        tooltipRect.anchoredPosition = desiredPivotPosition - anchorReference;
    }

    private Camera GetCanvasCamera()
    {
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return parentCanvas.worldCamera;
    }
    
    private void UpdateContent()
    {
        if (currentQuest == null) return;
        
        if (titleText != null)
        {
            titleText.text = currentQuest.questTitle;
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = currentQuest.questDescription;
        }
        
        if (progressText != null)
        {
            int current = 0;
            if (QuestManager.Instance != null)
            {
                current = QuestManager.Instance.GetQuestProgress(currentQuest.questId);
            }
            progressText.text = $"Progress: {current}/{currentQuest.targetCount}";
        }
        
        if (rewardText != null)
        {
            rewardText.text = $"Reward: {currentQuest.rewardDescription}";
        }
    }
}

