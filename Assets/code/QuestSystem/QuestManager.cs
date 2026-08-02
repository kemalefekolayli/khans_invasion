using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    
    [Header("Quest Definitions")]
    public List<QuestData> allQuests = new List<QuestData>();

    [Header("Dynamic Value Targets")]
    [SerializeField, Min(1f)] private float startingValueTargetMultiplier = 1.25f;
    [SerializeField, Min(1)] private int startingValueTargetMargin = 1;
    [SerializeField, Min(0.05f)] private float startingValuesSettleDelay = 0.3f;
    
    private Dictionary<int, int> questProgress = new Dictionary<int, int>();
    private HashSet<int> completedQuests = new HashSet<int>();
    private HashSet<int> claimedQuests = new HashSet<int>();
    private Dictionary<int, int> effectiveTargets = new Dictionary<int, int>();
    private bool valueTargetsInitialized;
    private Coroutine effectiveTargetInitialization;
    
    public event Action<int> OnQuestProgressUpdated;
    public event Action<int> OnQuestCompleted;
    public event Action<int> OnQuestClaimed;
    public event Action OnQuestTargetsInitialized;
    
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

        if (!valueTargetsInitialized && effectiveTargetInitialization == null)
        {
            effectiveTargetInitialization = StartCoroutine(InitializeEffectiveTargetsWhenSettled());
        }
    }
    
    private void OnDisable()
    {
        UnsubscribeFromEvents();

        if (effectiveTargetInitialization != null)
        {
            StopCoroutine(effectiveTargetInitialization);
            effectiveTargetInitialization = null;
        }
    }
    
    private void InitializeQuestProgress()
    {
        questProgress.Clear();
        completedQuests.Clear();
        claimedQuests.Clear();
        effectiveTargets.Clear();
        valueTargetsInitialized = false;

        if (allQuests == null) return;

        foreach (var quest in allQuests)
        {
            if (quest == null) continue;
            questProgress[quest.questId] = 0;
        }
    }

    private IEnumerator InitializeEffectiveTargetsWhenSettled()
    {
        StartingValues previousValues = default;
        bool hasPreviousValues = false;
        float stableTime = 0f;

        while (!valueTargetsInitialized)
        {
            if (TryGetCurrentStartingValues(out StartingValues currentValues))
            {
                if (!hasPreviousValues || !currentValues.Equals(previousValues))
                {
                    previousValues = currentValues;
                    hasPreviousValues = true;
                    stableTime = 0f;
                }
                else
                {
                    stableTime += Time.unscaledDeltaTime;
                }

                if (stableTime >= startingValuesSettleDelay)
                {
                    InitializeEffectiveTargets(currentValues);
                    yield break;
                }
            }
            else
            {
                hasPreviousValues = false;
                stableTime = 0f;
            }

            yield return null;
        }
    }

    private bool TryGetCurrentStartingValues(out StartingValues values)
    {
        PlayerNation player = PlayerNation.Instance;
        if (player?.Nation == null || player.OwnedProvinces == null || ArmyManager.Instance == null)
        {
            values = default;
            return false;
        }

        values = new StartingValues(
            GetCurrentValue(QuestType.AccumulateGold),
            GetCurrentValue(QuestType.ReachIncome),
            GetCurrentValue(QuestType.ReachTotalPopulation),
            GetCurrentValue(QuestType.ReachArmySize));
        return true;
    }

    private void InitializeEffectiveTargets(StartingValues startingValues)
    {
        if (allQuests == null) return;

        int margin = Mathf.Max(1, startingValueTargetMargin);
        float multiplier = Mathf.Max(1f, startingValueTargetMultiplier);

        foreach (QuestData quest in allQuests)
        {
            if (quest == null || !IsValueQuest(quest.questType)) continue;

            int startingValue = startingValues.GetValue(quest.questType);
            int dynamicTarget = Mathf.CeilToInt(startingValue * multiplier + margin);
            int effectiveTarget = Mathf.Max(quest.targetCount + margin, dynamicTarget);
            effectiveTargets[quest.questId] = effectiveTarget;
        }

        valueTargetsInitialized = true;
        OnQuestTargetsInitialized?.Invoke();
    }

    private struct StartingValues : IEquatable<StartingValues>
    {
        private readonly int gold;
        private readonly int income;
        private readonly int population;
        private readonly int army;

        public StartingValues(int gold, int income, int population, int army)
        {
            this.gold = gold;
            this.income = income;
            this.population = population;
            this.army = army;
        }

        public int GetValue(QuestType questType)
        {
            switch (questType)
            {
                case QuestType.AccumulateGold:
                    return gold;
                case QuestType.ReachIncome:
                    return income;
                case QuestType.ReachTotalPopulation:
                    return population;
                case QuestType.ReachArmySize:
                    return army;
                default:
                    return 0;
            }
        }

        public bool Equals(StartingValues other)
        {
            return gold == other.gold
                && income == other.income
                && population == other.population
                && army == other.army;
        }

        public override bool Equals(object obj)
        {
            return obj is StartingValues other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = gold;
                hash = (hash * 397) ^ income;
                hash = (hash * 397) ^ population;
                hash = (hash * 397) ^ army;
                return hash;
            }
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
        if (allQuests == null || amount <= 0) return;

        HashSet<int> unlockedQuestIds = new HashSet<int>();
        foreach (QuestData quest in allQuests)
        {
            if (quest != null && IsQuestUnlocked(quest.questId))
            {
                unlockedQuestIds.Add(quest.questId);
            }
        }

        bool completedQuest = false;
        foreach (var quest in allQuests)
        {
            if (quest == null) continue;
            if (quest.questType != questType) continue;
            if (!unlockedQuestIds.Contains(quest.questId)) continue;
            if (completedQuests.Contains(quest.questId)) continue;
            
            questProgress[quest.questId] += amount;
            int target = GetEffectiveTarget(quest.questId);
            GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} progress: {questProgress[quest.questId]}/{target}");
            
            OnQuestProgressUpdated?.Invoke(quest.questId);
            
            if (questProgress[quest.questId] >= target)
            {
                completedQuests.Add(quest.questId);
                GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} COMPLETED!");
                OnQuestCompleted?.Invoke(quest.questId);
                completedQuest = true;
            }
        }

        if (completedQuest)
        {
            CheckValueQuests();
        }
    }
    
    /// <summary>
    /// Value-based quest check. Reads the current live value for each value-type
    /// quest (gold, income, population, army size) and completes it once the
    /// value reaches the target. Already-completed quests are skipped.
    /// </summary>
    public void CheckValueQuests()
    {
        if (!valueTargetsInitialized || allQuests == null) return;

        PlayerNation player = PlayerNation.Instance;
        if (player?.Nation == null) return;
        
        foreach (var quest in allQuests)
        {
            if (quest == null) continue;
            if (!IsValueQuest(quest.questType)) continue;
            if (!IsQuestUnlocked(quest.questId)) continue;
            if (completedQuests.Contains(quest.questId)) continue;
            
            int currentValue = GetQuestCurrentValue(quest);
            if (currentValue == questProgress[quest.questId]) continue;
            
            questProgress[quest.questId] = currentValue;
            OnQuestProgressUpdated?.Invoke(quest.questId);
            
            int target = GetEffectiveTarget(quest.questId);
            if (currentValue >= target)
            {
                completedQuests.Add(quest.questId);
                GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} COMPLETED! (value {currentValue}/{target})");
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
        return GetCurrentValue(quest.questType);
    }

    private int GetCurrentValue(QuestType questType)
    {
        PlayerNation player = PlayerNation.Instance;
        switch (questType)
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
                return 0;
        }
    }
    
    public bool IsQuestUnlocked(int questId)
    {
        QuestData quest = GetQuestById(questId);
        if (quest == null) return false;
        
        if (quest.prerequisiteQuestId < 0) return true;
        
        return completedQuests.Contains(quest.prerequisiteQuestId);
    }

    public bool CanClaimQuest(int questId)
    {
        QuestData quest = GetQuestById(questId);
        if (quest == null || !IsQuestCompleted(questId) || IsQuestClaimed(questId)) return false;

        if (quest.prerequisiteQuestId >= 0 && !IsQuestClaimed(quest.prerequisiteQuestId))
        {
            return false;
        }

        return true;
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

    public int GetEffectiveTarget(int questId)
    {
        QuestData quest = GetQuestById(questId);
        if (quest == null) return 0;

        return effectiveTargets.TryGetValue(questId, out int target) ? target : quest.targetCount;
    }
    
    public QuestData GetQuestById(int questId)
    {
        return allQuests == null ? null : allQuests.Find(q => q != null && q.questId == questId);
    }
    
    public bool TryClaimQuest(int questId)
    {
        QuestData quest = GetQuestById(questId);
        if (quest == null || !CanClaimQuest(questId)) return false;
        
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
