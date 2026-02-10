using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    
    [Header("Quest Definitions")]
    public List<QuestData> allQuests = new List<QuestData>();
    
    [Header("General Spawning")]
    public GameObject generalPrefab;
    public Transform generalSpawnPoint;
    
    private Dictionary<int, int> questProgress = new Dictionary<int, int>();
    private HashSet<int> completedQuests = new HashSet<int>();
    private HashSet<int> claimedQuests = new HashSet<int>();
    
    public event Action<int> OnQuestProgressUpdated;
    public event Action<int> OnQuestCompleted;
    public event Action<int> OnQuestClaimed;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        InitializeQuestProgress();
        Debug.Log("[QuestManager] Initialized");
    }
    
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeQuestProgress()
    {
        foreach (var quest in allQuests)
        {
            questProgress[quest.questId] = 0;
        }
    }
    
    private void SubscribeToEvents()
    {
        GameEvents.OnProvinceRaided += OnProvinceRaided;
        GameEvents.OnBuildingBuilt += OnBuildingBuilt;
        GameEvents.OnProvinceConquered += OnProvinceConquered;
        GameEvents.OnArmyDefeated += OnArmyDefeated;
        GameEvents.OnNationDestroyed += OnNationDestroyed;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
        GameEvents.OnBuildingBuilt -= OnBuildingBuilt;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
        GameEvents.OnArmyDefeated -= OnArmyDefeated;
        GameEvents.OnNationDestroyed -= OnNationDestroyed;
    }
    
    private void OnProvinceRaided(ProvinceModel province, General raider, float loot)
    {
        // Only count if the raider is player's general
        if (raider?.OwnerNation != PlayerNation.Instance?.Nation) return;
        IncrementProgress(QuestType.RaidProvinces);
    }
    
    private void OnBuildingBuilt(string buildingType, ProvinceModel province)
    {
        IncrementProgress(QuestType.BuildBuildings);
    }
    
    private void OnProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        // Only count if we are the new owner
        if (newOwner != PlayerNation.Instance?.Nation) return;
        
        IncrementProgress(QuestType.ConquerProvinces);
        
        // Check if province has fortress building
        if (province.buildings != null && province.buildings.Contains("Fortress"))
        {
            IncrementProgress(QuestType.TakeoverFortressProvinces);
        }
    }
    
    private void OnArmyDefeated(Army army)
    {
        IncrementProgress(QuestType.DefeatArmies);
    }
    
    private void OnNationDestroyed(NationModel nation)
    {
        IncrementProgress(QuestType.DestroyNation);
    }
    
    private void IncrementProgress(QuestType questType)
    {
        foreach (var quest in allQuests)
        {
            if (quest.questType != questType) continue;
            if (completedQuests.Contains(quest.questId)) continue;
            
            questProgress[quest.questId]++;
            Debug.Log($"[QuestManager] Quest {quest.questId} progress: {questProgress[quest.questId]}/{quest.targetCount}");
            
            OnQuestProgressUpdated?.Invoke(quest.questId);
            
            if (questProgress[quest.questId] >= quest.targetCount)
            {
                completedQuests.Add(quest.questId);
                Debug.Log($"[QuestManager] Quest {quest.questId} COMPLETED!");
                OnQuestCompleted?.Invoke(quest.questId);
            }
        }
    }
    
    public bool IsQuestUnlocked(int questId)
    {
        QuestData quest = GetQuestById(questId);
        if (quest == null) return false;
        
        if (quest.prerequisiteQuestId < 0) return true;
        
        return claimedQuests.Contains(quest.prerequisiteQuestId);
    }
    
    public bool IsQuestCompleted(int questId)
    {
        return completedQuests.Contains(questId);
    }
    
    public bool IsQuestClaimed(int questId)
    {
        return claimedQuests.Contains(questId);
    }
    
    public int GetQuestProgress(int questId)
    {
        return questProgress.TryGetValue(questId, out int progress) ? progress : 0;
    }
    
    public QuestData GetQuestById(int questId)
    {
        return allQuests.Find(q => q.questId == questId);
    }
    
    public bool TryClaimQuest(int questId)
    {
        if (!IsQuestCompleted(questId)) return false;
        if (IsQuestClaimed(questId)) return false;
        
        QuestData quest = GetQuestById(questId);
        if (quest == null) return false;
        
        ApplyReward(quest);
        claimedQuests.Add(questId);
        
        Debug.Log($"[QuestManager] Quest {questId} CLAIMED! Reward: {quest.rewardDescription}");
        OnQuestClaimed?.Invoke(questId);
        
        return true;
    }
    
    private void ApplyReward(QuestData quest)
    {
        PlayerNation player = PlayerNation.Instance;
        if (player == null) return;
        
        switch (quest.rewardType)
        {
            case RewardType.Gold:
                player.nationMoney += quest.rewardAmount;
                GameEvents.PlayerStatsChanged();
                Debug.Log($"[QuestManager] Reward: +{quest.rewardAmount} gold");
                break;
                
            case RewardType.PopulationCapacity:
                foreach (var province in player.OwnedProvinces)
                {
                    province.provinceMaxPop += quest.rewardAmount;
                }
                Debug.Log($"[QuestManager] Reward: +{quest.rewardAmount} pop capacity to all provinces");
                break;
                
            case RewardType.TradeIncome:
                player.bonusTradeIncome += quest.rewardAmount;
                player.RecalculateStats();
                GameEvents.PlayerStatsChanged();
                Debug.Log($"[QuestManager] Reward: +{quest.rewardAmount} trade income");
                break;
                
            case RewardType.NewGeneral:
                SpawnNewGeneral();
                Debug.Log("[QuestManager] Reward: New General spawned!");
                break;
                
            case RewardType.MoveCapitalAbility:
                player.canMoveCapital = true;
                Debug.Log("[QuestManager] Reward: Move Capital ability unlocked!");
                break;
        }
    }
    
    private void SpawnNewGeneral()
    {
        if (generalPrefab == null || generalSpawnPoint == null)
        {
            Debug.LogWarning("[QuestManager] Cannot spawn general - prefab or spawn point not set");
            return;
        }
        
        // Directly instantiate the general
        GameObject generalObj = Instantiate(generalPrefab, generalSpawnPoint.position, Quaternion.identity);
        generalObj.name = "General_QuestReward";
        
        SelectableGeneral selectable = generalObj.GetComponent<SelectableGeneral>();
        if (selectable != null)
        {
            selectable.SetDisplayName("Quest General");
            selectable.SetIsKhan(false);
        }
        
        General general = generalObj.GetComponent<General>();
        if (general == null)
        {
            general = generalObj.AddComponent<General>();
        }
        general.Initialize("Quest General", false);
        
        Debug.Log("[QuestManager] New general spawned as quest reward!");
    }
}
