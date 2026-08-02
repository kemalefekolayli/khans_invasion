using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanelController : MonoBehaviour
{
    public static QuestPanelController Instance { get; private set; }
    
    [Header("Panel")]
    public GameObject questPanel;
    public ScrollRect questScrollRect;
    
    [Header("Quest Tree")]
    public GameObject questItemTemplate;
    public RectTransform treeContent;
    
    [Header("Layout")]
    public float nodeWidth = 130f;
    public float nodeHeight = 90f;
    public float columnSpacing = 160f;
    public float rowSpacing = 140f;
    public float treePadding = 15f;
    
    [Header("Connectors")]
    public Color lockedConnectorColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color unlockedConnectorColor = new Color(0.95f, 0.85f, 0.4f, 1f);
    public float connectorThickness = 6f;
    
    private bool isOpen = false;
    private Sprite whiteSprite;
    private readonly List<QuestItemUI> questItems = new List<QuestItemUI>();
    private readonly List<ConnectorData> connectors = new List<ConnectorData>();
    
    private struct ConnectorData
    {
        public Image image;
        public int childQuestId;
    }
    
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
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgressUpdated += OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestUpdated;
            QuestManager.Instance.OnQuestClaimed += OnQuestUpdated;
        }
    }
    
    private void OnDisable()
    {
        GameEvents.OnProvincePanelClosed -= OnOtherPanelClosed;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgressUpdated -= OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestUpdated;
            QuestManager.Instance.OnQuestClaimed -= OnQuestUpdated;
        }
    }
    
    private void OnQuestUpdated(int questId)
    {
        RecolorConnectors();
    }
    
    private void InitializeQuestItems()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null) return;
        
        ClearQuestItems();
        
        List<QuestData> quests = manager.allQuests;
        if (quests == null || quests.Count == 0) return;
        
        BuildTree(quests);
    }
    
    private void BuildTree(List<QuestData> quests)
    {
        Dictionary<int, List<QuestData>> childrenByParent = new Dictionary<int, List<QuestData>>();
        List<QuestData> roots = new List<QuestData>();
        
        foreach (QuestData quest in quests)
        {
            // Missing prerequisite quest is treated as a root so every quest is placed.
            bool isRoot = quest.prerequisiteQuestId < 0 || FindQuest(quests, quest.prerequisiteQuestId) == null;
            
            if (isRoot)
            {
                roots.Add(quest);
            }
            else
            {
                if (!childrenByParent.TryGetValue(quest.prerequisiteQuestId, out List<QuestData> children))
                {
                    children = new List<QuestData>();
                    childrenByParent[quest.prerequisiteQuestId] = children;
                }
                children.Add(quest);
            }
        }
        
        roots.Sort((a, b) => a.questId.CompareTo(b.questId));
        foreach (List<QuestData> children in childrenByParent.Values)
        {
            children.Sort((a, b) => a.questId.CompareTo(b.questId));
        }
        
        Dictionary<int, int> depth = new Dictionary<int, int>();
        Dictionary<int, float> nodeX = new Dictionary<int, float>();
        HashSet<int> assigned = new HashSet<int>();
        HashSet<int> path = new HashSet<int>();
        int leafCount = 0;
        int maxDepth = 0;
        
        foreach (QuestData root in roots)
        {
            AssignTreePositions(root, childrenByParent, depth, nodeX, ref leafCount, ref maxDepth, 0, assigned, path);
        }
        
        // Safety net: place anything not reachable from the roots (broken/cyclic data).
        foreach (QuestData quest in quests)
        {
            if (!assigned.Contains(quest.questId))
            {
                AssignTreePositions(quest, childrenByParent, depth, nodeX, ref leafCount, ref maxDepth, 0, assigned, path);
            }
        }
        
        float contentWidth = nodeWidth + Mathf.Max(0, leafCount - 1) * columnSpacing + treePadding * 2f;
        float contentHeight = nodeHeight + maxDepth * rowSpacing + treePadding * 2f;
        treeContent.sizeDelta = new Vector2(contentWidth, contentHeight);
        
        foreach (QuestData quest in quests)
        {
            int questDepth = depth.TryGetValue(quest.questId, out int d) ? d : 0;
            float x = nodeX.TryGetValue(quest.questId, out float xPos) ? xPos : 0f;
            CreateQuestItem(quest, ColumnX(x), RowY(questDepth));
        }
        
        CreateConnectors(quests, childrenByParent, nodeX, depth);
        RecolorConnectors();
    }
    
    /// <summary>
    /// Draws vertical connector segments between each parent and its children,
    /// following the same columns used to place the quest nodes.
    /// </summary>
    private void CreateConnectors(List<QuestData> quests, Dictionary<int, List<QuestData>> childrenByParent,
        Dictionary<int, float> nodeX, Dictionary<int, int> depth)
    {
        foreach (KeyValuePair<int, List<QuestData>> pair in childrenByParent)
        {
            QuestData parent = FindQuest(quests, pair.Key);
            if (parent == null) continue;
            if (!nodeX.TryGetValue(parent.questId, out float parentCol)) continue;
            int parentDepth = depth.TryGetValue(parent.questId, out int pd) ? pd : 0;
            float parentCenterX = ColumnX(parentCol);
            float parentCenterY = RowY(parentDepth);
            
            foreach (QuestData child in pair.Value)
            {
                if (!nodeX.TryGetValue(child.questId, out float childCol)) continue;
                int childDepth = depth.TryGetValue(child.questId, out int cd) ? cd : 0;
                float childCenterX = ColumnX(childCol);
                float childCenterY = RowY(childDepth);
                
                CreateConnectorSegment(parentCenterX, parentCenterY, childCenterX, childCenterY, child.questId);
            }
        }
    }
    
    private float ColumnX(float column)
    {
        return treePadding + nodeWidth * 0.5f + column * columnSpacing;
    }
    
    private float RowY(int depth)
    {
        return -(treePadding + nodeHeight * 0.5f + depth * rowSpacing);
    }
    
    private void CreateConnectorSegment(float parentCenterX, float parentCenterY, float childCenterX, float childCenterY, int childQuestId)
    {
        if (treeContent == null) return;
        
        float parentBottom = parentCenterY - nodeHeight * 0.5f;
        float childTop = childCenterY + nodeHeight * 0.5f;
        float midY = (parentBottom + childTop) * 0.5f;
        
        if (Mathf.Abs(childCenterX - parentCenterX) < 0.01f)
        {
            // Vertical drop directly below the parent.
            AddConnectorRect(parentCenterX, midY, connectorThickness, parentBottom - childTop, childQuestId);
        }
        else
        {
            // Vertical from parent bottom to the mid row.
            AddConnectorRect(parentCenterX, (parentBottom + midY) * 0.5f, connectorThickness, parentBottom - midY, childQuestId);
            // Horizontal run at the mid row.
            AddConnectorRect((parentCenterX + childCenterX) * 0.5f, midY, Mathf.Abs(parentCenterX - childCenterX), connectorThickness, childQuestId);
            // Vertical from the mid row down to the child top.
            AddConnectorRect(childCenterX, (midY + childTop) * 0.5f, connectorThickness, midY - childTop, childQuestId);
        }
    }
    
    private void AddConnectorRect(float centerX, float centerY, float width, float height, int childQuestId)
    {
        if (treeContent == null) return;
        if (width <= 0f || height <= 0f) return;
        
        GameObject lineObject = new GameObject("Connector");
        lineObject.transform.SetParent(treeContent, false);
        lineObject.layer = treeContent.gameObject.layer;
        
        RectTransform rect = lineObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(centerX, centerY);
        rect.sizeDelta = new Vector2(width, height);
        
        Image image = lineObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = GetWhiteSprite();
        image.color = lockedConnectorColor;
        
        connectors.Add(new ConnectorData { image = image, childQuestId = childQuestId });
    }
    
    private void RecolorConnectors()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null) return;
        
        foreach (ConnectorData connector in connectors)
        {
            if (connector.image == null) continue;
            connector.image.color = manager.IsQuestUnlocked(connector.childQuestId)
                ? unlockedConnectorColor
                : lockedConnectorColor;
        }
    }
    
    private Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
    
    /// <summary>
    /// Recursive tree walk. Each leaf occupies one column; internal nodes are centered
    /// over their children's columns so parents and children line up visually.
    /// Returns the node's column index.
    /// </summary>
    private float AssignTreePositions(QuestData node, Dictionary<int, List<QuestData>> childrenByParent,
        Dictionary<int, int> depth, Dictionary<int, float> nodeX, ref int leafCount, ref int maxDepth,
        int currentDepth, HashSet<int> assigned, HashSet<int> path)
    {
        if (path.Contains(node.questId))
        {
            // Cycle guard: a node already being placed on this path cannot have children placed.
            if (!nodeX.ContainsKey(node.questId))
            {
                nodeX[node.questId] = leafCount;
                leafCount++;
            }
            return nodeX[node.questId];
        }
        
        path.Add(node.questId);
        depth[node.questId] = currentDepth;
        if (currentDepth > maxDepth) maxDepth = currentDepth;
        assigned.Add(node.questId);
        
        if (childrenByParent.TryGetValue(node.questId, out List<QuestData> children) && children.Count > 0)
        {
            float firstChildX = 0f;
            float lastChildX = 0f;
            for (int i = 0; i < children.Count; i++)
            {
                float childX = AssignTreePositions(children[i], childrenByParent, depth, nodeX,
                    ref leafCount, ref maxDepth, currentDepth + 1, assigned, path);
                if (i == 0) firstChildX = childX;
                lastChildX = childX;
            }
            nodeX[node.questId] = (firstChildX + lastChildX) / 2f;
        }
        else
        {
            nodeX[node.questId] = leafCount;
            leafCount++;
        }
        
        path.Remove(node.questId);
        return nodeX[node.questId];
    }
    
    private void CreateQuestItem(QuestData quest, float x, float y)
    {
        if (questItemTemplate == null || treeContent == null) return;
        
        GameObject itemObject = Instantiate(questItemTemplate, treeContent);
        itemObject.SetActive(true);
        
        RectTransform rect = itemObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(nodeWidth, nodeHeight);
        }
        
        QuestItemUI item = itemObject.GetComponent<QuestItemUI>();
        if (item != null)
        {
            item.Initialize(quest);
            questItems.Add(item);
        }
    }
    
    private void ClearQuestItems()
    {
        if (treeContent != null)
        {
            for (int i = treeContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = treeContent.GetChild(i).gameObject;
                if (child == questItemTemplate) continue;
                Destroy(child);
            }
        }
        questItems.Clear();
        connectors.Clear();
    }
    
    private QuestData FindQuest(List<QuestData> quests, int questId)
    {
        foreach (QuestData quest in quests)
        {
            if (quest.questId == questId)
                return quest;
        }
        return null;
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
            
            if (questItems.Count == 0)
                InitializeQuestItems();
            
            // Refresh all quest states
            foreach (var item in questItems)
            {
                item.UpdateVisualState();
            }
            RecolorConnectors();
            
            ResetScrollPosition();
            
            GameEvents.QuestPanelOpened();
            GameLog.Log(GameLogCategory.Core, "[QuestPanelController] Panel opened");
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
            GameLog.Log(GameLogCategory.Core, "[QuestPanelController] Panel closed");
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
    
    private void ResetScrollPosition()
    {
        if (questScrollRect != null)
        {
            questScrollRect.verticalNormalizedPosition = 1f;
            questScrollRect.horizontalNormalizedPosition = 0f;
        }
    }
}
