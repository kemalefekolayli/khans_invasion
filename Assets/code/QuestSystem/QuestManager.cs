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
    private readonly Dictionary<int, HashSet<long>> distinctProvinceProgress = new();
    private readonly Dictionary<int, int> claimedGoldRewards = new();
    private bool valueTargetsInitialized;
    private Coroutine effectiveTargetInitialization;
    
    public event Action<int> OnQuestProgressUpdated;
    public event Action<int> OnQuestCompleted;
    public event Action<int> OnQuestClaimed;
    public event Action OnQuestTargetsInitialized;
    public bool HasClaimableQuests
    {
        get
        {
            if (allQuests == null) return false;
            foreach (QuestData quest in allQuests)
                if (quest != null && CanClaimQuest(quest.questId)) return true;
            return false;
        }
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        QuestCatalog catalog = Resources.Load<QuestCatalog>("QuestCatalog");
        if (catalog != null && catalog.quests != null && catalog.quests.Count > 0)
            allQuests = new List<QuestData>(catalog.quests);
        ValidateQuestDefinitions();
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
        distinctProvinceProgress.Clear();
        claimedGoldRewards.Clear();
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
            GetCurrentValue(QuestType.ReachArmySize),
            GetCurrentValue(QuestType.ReachCharisma));
        return true;
    }

    private void InitializeEffectiveTargets(StartingValues startingValues)
    {
        if (allQuests == null) return;

        int margin = Mathf.Max(1, startingValueTargetMargin);
        float multiplier = Mathf.Max(1f, startingValueTargetMultiplier);

        foreach (QuestData quest in allQuests)
        {
            if (quest == null || !quest.useDynamicStartingTarget || !IsValueQuest(quest.questType)) continue;

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
        private readonly int charisma;

        public StartingValues(int gold, int income, int population, int army, int charisma)
        {
            this.gold = gold;
            this.income = income;
            this.population = population;
            this.army = army;
            this.charisma = charisma;
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
                case QuestType.ReachCharisma:
                    return charisma;
                default:
                    return 0;
            }
        }

        public bool Equals(StartingValues other)
        {
            return gold == other.gold
                && income == other.income
                && population == other.population
                && army == other.army
                && charisma == other.charisma;
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
                hash = (hash * 397) ^ charisma;
                return hash;
            }
        }
    }
    
    private void SubscribeToEvents()
    {
        GameEvents.OnProvinceRaided += OnProvinceRaided;
        GameEvents.OnBuildingBuilt += OnBuildingBuilt;
        GameEvents.OnEnemyCommanderCaptured += OnEnemyCommanderCaptured;
        GameEvents.OnPlayerTroopsRecruited += OnPlayerTroopsRecruited;
        GameEvents.OnProvinceConquered += OnProvinceConquered;
        GameEvents.OnArmyDefeated += OnArmyDefeated;
        GameEvents.OnNationDestroyed += OnNationDestroyed;
        GameEvents.OnPlayerStatsChanged += OnPlayerStatsChanged;
        GameEvents.OnPopulationGrowth += OnPopulationGrowth;
        GameEvents.OnTurnEnded += OnTurnEnded;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnProvinceRaided -= OnProvinceRaided;
        GameEvents.OnBuildingBuilt -= OnBuildingBuilt;
        GameEvents.OnEnemyCommanderCaptured -= OnEnemyCommanderCaptured;
        GameEvents.OnPlayerTroopsRecruited -= OnPlayerTroopsRecruited;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
        GameEvents.OnArmyDefeated -= OnArmyDefeated;
        GameEvents.OnNationDestroyed -= OnNationDestroyed;
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
        if (allQuests == null || province == null || province.provinceOwner != PlayerNation.Instance?.Nation) return;

        foreach (QuestData quest in allQuests)
        {
            if (quest == null || !IsQuestUnlocked(quest.questId) || completedQuests.Contains(quest.questId)) continue;

            if (quest.questType == QuestType.BuildBuildings
                && (string.IsNullOrEmpty(quest.requiredBuildingType) || quest.requiredBuildingType == buildingType))
            {
                AddProgressToQuest(quest, 1);
            }
            else if (quest.questType == QuestType.BuildInDistinctProvinces)
            {
                if (!distinctProvinceProgress.TryGetValue(quest.questId, out HashSet<long> provinces))
                {
                    provinces = new HashSet<long>();
                    distinctProvinceProgress[quest.questId] = provinces;
                }

                if (provinces.Add(province.provinceId))
                    AddProgressToQuest(quest, 1);
            }
        }
    }

    private void ValidateQuestDefinitions()
    {
        if (allQuests == null) return;
        HashSet<int> ids = new HashSet<int>();
        foreach (QuestData quest in allQuests)
        {
            if (quest == null) continue;
            if (!ids.Add(quest.questId))
                GameLog.Warning(GameLogCategory.Quest, $"[QuestManager] Duplicate quest ID {quest.questId}.");
        }

        foreach (QuestData quest in allQuests)
        {
            if (quest != null && quest.prerequisiteQuestId >= 0 && !ids.Contains(quest.prerequisiteQuestId))
                GameLog.Warning(GameLogCategory.Quest, $"[QuestManager] Quest {quest.questId} missing prerequisite {quest.prerequisiteQuestId}.");
        }
    }

    private void OnEnemyCommanderCaptured(Army captive, Army playerCaptor)
    {
        if (captive == null || playerCaptor == null || !playerCaptor.IsPlayerArmy || captive.IsPlayerArmy) return;
        AddProgress(QuestType.CaptureEnemyCommanders, 1);
    }

    private void OnPlayerTroopsRecruited(Army army, float amount)
    {
        AddProgress(QuestType.RecruitTroops, Mathf.FloorToInt(amount));
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
            
            completedQuest |= AddProgressToQuest(quest, amount);
        }

        if (completedQuest)
        {
            CheckValueQuests();
        }
    }

    private bool AddProgressToQuest(QuestData quest, int amount)
    {
        if (quest == null || amount <= 0 || completedQuests.Contains(quest.questId)) return false;
        questProgress[quest.questId] += amount;
        int target = GetEffectiveTarget(quest.questId);
        GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} progress: {questProgress[quest.questId]}/{target}");
        OnQuestProgressUpdated?.Invoke(quest.questId);
        if (questProgress[quest.questId] < target) return false;

        completedQuests.Add(quest.questId);
        GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {quest.questId} COMPLETED!");
        OnQuestCompleted?.Invoke(quest.questId);
        return true;
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
            case QuestType.ReachCharisma:
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
            case QuestType.ReachCharisma:
                return Mathf.FloorToInt(player.GetComponent<CharismaSystem>()?.Current ?? 0f);
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

    public string GetCurrentDisplayDescription(int questId)
    {
        QuestData quest = GetQuestById(questId);
        return quest == null ? string.Empty : ReplaceDynamicTokens(quest.questDescription, quest);
    }

    public string GetCurrentRewardDescription(int questId)
    {
        QuestData quest = GetQuestById(questId);
        return quest == null ? string.Empty : ReplaceDynamicTokens(quest.rewardDescription, quest);
    }

    private string ReplaceDynamicTokens(string text, QuestData quest)
    {
        return (text ?? string.Empty)
            .Replace("{target}", GetEffectiveTarget(quest.questId).ToString())
            .Replace("{gold}", GetEffectiveGoldReward(quest).ToString());
    }
    
    public bool TryClaimQuest(int questId)
    {
        QuestData quest = GetQuestById(questId);
        if (quest == null || !CanClaimQuest(questId)) return false;
        
        ApplyReward(quest);
        claimedQuests.Add(questId);

        GameLog.Log(GameLogCategory.Core, $"[QuestManager] Quest {questId} CLAIMED! Reward: {GetCurrentRewardDescription(questId)}");
        OnQuestClaimed?.Invoke(questId);
        
        return true;
    }
    
    private void ApplyReward(QuestData quest)
    {
        PlayerNation player = PlayerNation.Instance;
        if (player == null) return;
        
        int goldReward = GetEffectiveGoldReward(quest);
        claimedGoldRewards[quest.questId] = goldReward;

        switch (quest.rewardType)
        {
            case RewardType.Gold:
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
            case RewardType.GeneralLimitFlat:
                MilitaryEconomy.GetOrCreate().AddGeneralLimitFlat(quest.rewardAmount);
                break;
            case RewardType.SupplyCapacityPercent:
                SupplyMovementCoordinator.Instance?.AddSupplyCapacityPercent(quest.rewardAmount);
                break;
            case RewardType.FriendlySupplyCostReductionPercent:
                SupplyMovementCoordinator.Instance?.AddFriendlySupplyCostReductionPercent(quest.rewardAmount);
                break;
            case RewardType.CargoSupplyCostMultiplier:
                SupplyMovementCoordinator.Instance?.SetCargoSupplyCostPercentOfBase(quest.rewardAmount);
                break;
            case RewardType.BuildingCostReductionPercent:
                (Builder.Instance != null ? Builder.Instance : FindFirstObjectByType<Builder>())?.AddPlayerBuildingCostReductionPercent(quest.rewardAmount);
                break;
            case RewardType.PlayerDiceFlatBonus:
                (ArmyBattleManager.Instance != null ? ArmyBattleManager.Instance : FindFirstObjectByType<ArmyBattleManager>())?.AddPlayerDiceFlatBonus(quest.rewardAmount);
                break;
        }

        if (goldReward > 0)
        {
            player.nationMoney += goldReward;
            GameLog.Log(GameLogCategory.Quest, $"[QuestManager] Reward: +{goldReward} gold");
        }

        CharismaSystem charisma = player.GetComponent<CharismaSystem>();
        if (charisma != null)
            charisma.AddCharisma(0.5f, "quest reward claimed");

        GameEvents.PlayerStatsChanged();
    }

    private int GetEffectiveGoldReward(QuestData quest)
    {
        if (quest == null) return 0;
        if (claimedGoldRewards.TryGetValue(quest.questId, out int claimedGold)) return claimedGold;
        float income = PlayerNation.Instance != null ? PlayerNation.Instance.TotalIncome : 0f;
        int dynamicGold = Mathf.Max(0, Mathf.RoundToInt(income * Mathf.Max(0f, quest.goldRewardIncomeTurns)));
        return quest.rewardType == RewardType.Gold ? Mathf.Max(quest.rewardAmount, dynamicGold) : dynamicGold;
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
