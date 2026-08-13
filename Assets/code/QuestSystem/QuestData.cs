using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Quest Info")]
    public int questId;
    public string questTitle;
    [TextArea(2, 4)]
    public string questDescription;
    
    [Header("Completion")]
    public QuestType questType;
    public int targetCount = 1;
    public bool useDynamicStartingTarget;
    [Tooltip("Optional filter for BuildBuildings quests. Empty counts any building type.")]
    public string requiredBuildingType;
    
    [Header("Reward")]
    public RewardType rewardType;
    public int rewardAmount;
    [Min(0f)] public float goldRewardIncomeTurns;
    [TextArea(1, 2)]
    public string rewardDescription;
    
    [Header("Prerequisites")]
    public int prerequisiteQuestId = -1;
}
