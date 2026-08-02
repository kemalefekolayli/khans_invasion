using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    
    [Header("Quest Definitions")]
    public List<QuestData> allQuests = new List<QuestData>();
    
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
        GameLog.Log(GameLogCategory.Core, "[QuestManager] Initialized");
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
        GameEvents.OnArmySpawned += OnArmySpawned;
        GameEvents.OnPlayerStatsChanged += OnPlayerStatsChanged;
        GameEvents.OnPopulationGrowth += OnPopulationGrowth;
        GameEvents.OnTurnEnded += OnTurnEnded;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
        GameEvents.OnBuildingBuilt -= OnBuildingBuilt;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
        GameEvents.OnArmyDefeated -= OnArmyDefeated;
        GameEvents.OnNationDestroyed -= OnNationDestroyed;
        GameEvents.OnArmySpawned -= OnArmySpawned;
        GameEvents.OnPlayerStatsChanged -= OnPlayerStatsChanged;
        GameEvents.OnPopulationGrowth -= OnPopulationGrowth;
        GameEvents.OnTurnEnded -= OnTurnEnded;
    }
    
    private void OnProvinceRaided(ProvinceModel province, General raider, float loot)
    {
        // Only count if the raider is player's general
        if (raider?.OwnerNation != PlayerNation.Instance?.Nation) return;
        AddProgress(QuestType.RaidProvinces, 1);
    }
    
    private void OnBuildingBuilt(string buildingType, ProvinceModel province)
    {
        AddProgress(QuestType.BuildBuildings, 1);
    }
    
    private void OnProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        // Only count if we are the new owner
        if (newOwner != PlayerNation.Instance?.Nation) return;
        
        AddProgress(QuestType.ConquerProvinces, 1);
        
        // Check if province has fortress building
        if (province.buildings != null && province.buildings.Contains("Fortress"))
        {
            AddProgress(QuestType.TakeoverFortressProvinces, 1);
        }
    }
    
    private void OnArmyDefeated(Army army)
    {
        // Only count defeated armies NOT owned by the player nation
        if (army?.OwnerNation == PlayerNation.Instance?.Nation) return;
        
        AddProgress(QuestType.DefeatArmies, 1);
    }
    
    private void OnNationDestroyed(NationModel nation)
    {
        AddProgress(QuestType.DestroyNation, 1);
    }
    
    private void OnArmySpawned(Army army, General general)
    {
        // Count newly recruited troops for the player
        if (army?.OwnerNation != PlayerNation.Instance?.Nation) return;
        AddProgress(QuestType.RecruitTroops, (int)army.ArmySize);
    }
    
    private void OnPlayerStatsChanged()
    {
        CheckValueQuests();
    }
    
    private void OnPopulationGrowth(ProvinceModel province, float growthAmount)
    {
        CheckValueQuests();
    }
    
    private void OnTurnEnded(int turnNumber)
    {
        CheckValueQuests();
    }
    
    private void AddProgress(QuestType questType, int amount)
    {
        foreach (var quest in allQuests)
        {
            if (quest.questType != questType) continue;
            if (completedQuests.Contains(quest.questId)) continue;
            
            questProgress[quest.questId] += amount;
            GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} progress: {questProgress[quest.questId]}/{quest.targetCount}");
            
            OnQuestProgressUpdated?.Invoke(quest.questId);
            
            if (questProgress[quest.questId] >= quest.targetCount)
            {
                completedQuests.Add(quest.questId);
                GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} COMPLETED!");
                OnQuestCompleted?.Invoke(quest.questId);
            }
        }
    }
    
    /// <summary>
    /// Value-based quest check. Reads the current live value for each value-type
    /// quest (gold, income, population, army size) and completes it once the
    /// value reaches the target. Already-completed quests are skipped.
    /// </summary>
    public void CheckValueQuests()
    {
        PlayerNation player = PlayerNation.Instance;
        if (player?.Nation == null) return;
        
        foreach (var quest in allQuests)
        {
            if (!IsValueQuest(quest.questType)) continue;
            if (completedQuests.Contains(quest.questId)) continue;
            
            int currentValue = GetQuestCurrentValue(quest);
            if (currentValue == questProgress[quest.questId]) continue;
            
            questProgress[quest.questId] = currentValue;
            OnQuestProgressUpdated?.Invoke(quest.questId);
            
            if (currentValue >= quest.targetCount)
            {
                completedQuests.Add(quest.questId);
                GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} COMPLETED! (value {currentValue}/{quest.targetCount})");
                OnQuestCompleted?.Invoke(quest.questId);
            }
        }
    }
    
    private bool IsValueQuest(QuestType questType)
    {
        switch (questType)
        {
            case QuestType.AccumulateGold:
            case QuestType.ReachIncome:
            case QuestType.ReachTotalPopulation:
            case QuestType.ReachArmySize:
                return true;
            default:
                return false;
        }
    }
    
    private int GetQuestCurrentValue(QuestData quest)
    {
        PlayerNation player = PlayerNation.Instance;
        switch (quest.questType)
        {
            case QuestType.AccumulateGold:
                return (int)player.nationMoney;
            case QuestType.ReachIncome:
            {
                float income = 0f;
                foreach (var province in player.OwnedProvinces)
                {
                    if (province == null) continue;
                    income += province.provinceTaxIncome + province.provinceTradePower;
                }
                return (int)(income + player.bonusTradeIncome);
            }
            case QuestType.ReachTotalPopulation:
            {
                int population = 0;
                foreach (var province in player.OwnedProvinces)
                {
                    if (province == null) continue;
                    population += (int)province.provinceCurrentPop;
                }
                return population;
            }
            case QuestType.ReachArmySize:
                return ArmyManager.Instance != null ? (int)ArmyManager.Instance.TotalPlayerSoldiers : 0;
            default:
                return questProgress.TryGetValue(quest.questId, out int progress) ? progress : 0;
        }
    }
    
    public bool IsQuestUnlocked(int questId)
    {
        QuestData quest = GetQuestById(questId);
        if (quest == null) return false;
        
        if (quest.prerequisiteQuestId < 0) return true;
        
        return completedQuests.Contains(quest.prerequisiteQuestId);
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
        
        GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {questId} CLAIMED! Reward: {quest.rewardDescription}");
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
                GameLog.Log(GameLogCategory.Core, $"[QuestManager] Reward: +{quest.rewardAmount} gold");
                break;
                
            case RewardType.PopulationCapacity:
                foreach (var province in player.OwnedProvinces)
                {
                    province.provinceMaxPop += quest.rewardAmount;
                }
                GameLog.Log(GameLogCategory.Core, $"[QuestManager] Reward: +{quest.rewardAmount} pop capacity to all provinces");
                break;
                
            case RewardType.TradeIncome:
                player.bonusTradeIncome += quest.rewardAmount;
                player.RecalculateStats();
                GameEvents.PlayerStatsChanged();
                GameLog.Log(GameLogCategory.Core, $"[QuestManager] Reward: +{quest.rewardAmount} trade income");
                break;
                
            case RewardType.NewGeneral:
                SpawnQuestRewardGeneral(quest.rewardAmount);
                GameLog.Log(GameLogCategory.Core, "[QuestManager] Reward: New General spawned!");
                break;
                
            case RewardType.MoveCapitalAbility:
                player.canMoveCapital = true;
                GameLog.Log(GameLogCategory.Core, "[QuestManager] Reward: Move Capital ability unlocked!");
                break;
        }
    }
    
    private void SpawnQuestRewardGeneral(int armySize)
    {
        GeneralSpawner spawner = FindFirstObjectByType<GeneralSpawner>();
        if (spawner == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[QuestManager] GeneralSpawner not found - cannot spawn quest reward general");
            return;
        }
        
        spawner.SpawnQuestRewardGeneral("Quest General", armySize);
        
        GameLog.Log(GameLogCategory.Core, "[QuestManager] New general spawned as quest reward!");
    }
}
