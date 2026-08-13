using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class QuestCatalogMigration
{
    private const string QuestFolder = "Assets/Quests";
    private const string CatalogPath = "Assets/Resources/QuestCatalog.asset";

    private sealed class Definition
    {
        public int Id;
        public string Title;
        public string Description;
        public QuestType Type;
        public int Target;
        public int Prerequisite;
        public RewardType Reward;
        public int Amount;
        public float GoldTurns;
        public string RewardDescription;
        public string RequiredBuilding;
        public bool DynamicTarget;
    }

    [MenuItem("Tools/Khans Invasion/Build Quest Catalog")]
    public static void BuildQuestCatalog()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(QuestFolder);

        Definition[] definitions = CreateDefinitions();
        List<QuestData> orderedQuests = new List<QuestData>(definitions.Length);
        foreach (Definition definition in definitions)
        {
            string path = $"{QuestFolder}/Quest_{definition.Id + 1}.asset";
            QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
            if (quest == null)
            {
                quest = ScriptableObject.CreateInstance<QuestData>();
                AssetDatabase.CreateAsset(quest, path);
            }

            ApplyDefinition(quest, definition);
            EditorUtility.SetDirty(quest);
            orderedQuests.Add(quest);
        }

        QuestCatalog catalog = AssetDatabase.LoadAssetAtPath<QuestCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<QuestCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.quests = orderedQuests;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[QuestCatalogMigration] Configured 25 quests and Resources/QuestCatalog.asset.");
    }

    private static void ApplyDefinition(QuestData quest, Definition definition)
    {
        quest.questId = definition.Id;
        quest.questTitle = definition.Title;
        quest.questDescription = definition.Description;
        quest.questType = definition.Type;
        quest.targetCount = definition.Target;
        quest.useDynamicStartingTarget = definition.DynamicTarget;
        quest.requiredBuildingType = definition.RequiredBuilding ?? string.Empty;
        quest.rewardType = definition.Reward;
        quest.rewardAmount = definition.Amount;
        quest.goldRewardIncomeTurns = definition.GoldTurns;
        quest.rewardDescription = definition.RewardDescription;
        quest.prerequisiteQuestId = definition.Prerequisite;
    }

    private static Definition[] CreateDefinitions()
    {
        return new[]
        {
            Q(0, "Lay the Foundations", "Build in {target} distinct provinces.", QuestType.BuildInDistinctProvinces, 3, -1, RewardType.Gold, 25, .5f, "{gold} gold"),
            Q(1, "Steady Revenue", "Reach {target} income per turn.", QuestType.ReachIncome, 50, 0, RewardType.TradeIncome, 50, .5f, "+50 trade income and {gold} gold"),
            Q(2, "Local Works", "Build {target} buildings.", QuestType.BuildBuildings, 5, 1, RewardType.Gold, 50, .75f, "{gold} gold"),
            Q(3, "Granaries and Markets", "Build {target} Farms.", QuestType.BuildBuildings, 3, 2, RewardType.BuildingCostReductionPercent, 10, 1f, "10% lower building costs and {gold} gold", "Farm"),
            Q(4, "Secure Roads", "Reach {target} income per turn.", QuestType.ReachIncome, 100, 3, RewardType.FriendlySupplyCostReductionPercent, 50, 1f, "50% lower friendly supply cost and {gold} gold"),
            Q(5, "A Full Treasury", "Accumulate {target} gold.", QuestType.AccumulateGold, 500, 4, RewardType.Gold, 150, 1.5f, "{gold} gold", null, true),
            Q(6, "Master Builders", "Build {target} buildings.", QuestType.BuildBuildings, 15, 5, RewardType.BuildingCostReductionPercent, 15, 2f, "15% lower building costs and {gold} gold"),
            Q(7, "Prosperous Realm", "Reach {target} income per turn.", QuestType.ReachIncome, 175, 6, RewardType.Gold, 250, 2f, "{gold} gold"),
            Q(8, "Imperial Treasury", "Reach {target} income per turn.", QuestType.ReachIncome, 250, 7, RewardType.Gold, 400, 3f, "{gold} gold"),

            Q(9, "Raise the Banners", "Recruit {target} troops.", QuestType.RecruitTroops, 100, 0, RewardType.Gold, 50, .5f, "{gold} gold"),
            Q(10, "Field a Host", "Reach an army size of {target}.", QuestType.ReachArmySize, 200, 9, RewardType.Gold, 75, .75f, "{gold} gold"),
            Q(11, "First Conquests", "Conquer {target} provinces.", QuestType.ConquerProvinces, 3, 10, RewardType.Gold, 100, 1f, "{gold} gold"),
            Q(12, "Break the Walls", "Take over {target} fortress province.", QuestType.TakeoverFortressProvinces, 1, 11, RewardType.GeneralLimitFlat, 1, 1f, "+1 general limit and {gold} gold"),
            Q(13, "Blooded in Battle", "Defeat {target} enemy armies.", QuestType.DefeatArmies, 3, 12, RewardType.Gold, 150, 1.5f, "{gold} gold"),
            Q(14, "Headhunter", "Capture {target} enemy commanders.", QuestType.CaptureEnemyCommanders, 3, 13, RewardType.Gold, 200, 1.5f, "{gold} gold"),
            Q(15, "Master of Sieges", "Take over {target} fortress provinces.", QuestType.TakeoverFortressProvinces, 5, 14, RewardType.SupplyCapacityPercent, 20, 2f, "+20% supply capacity and {gold} gold"),
            Q(16, "Legendary Warlord", "Defeat {target} enemy armies.", QuestType.DefeatArmies, 10, 15, RewardType.PlayerDiceFlatBonus, 1, 3f, "+1 player battle roll and {gold} gold"),

            Q(17, "Growing Realm", "Reach a total population of {target}.", QuestType.ReachTotalPopulation, 1000, 0, RewardType.Gold, 50, .5f, "{gold} gold"),
            Q(18, "Room to Grow", "Build {target} Housing buildings.", QuestType.BuildBuildings, 3, 17, RewardType.PopulationCapacity, 100, .75f, "+100 population capacity in every province and {gold} gold", "Housing"),
            Q(19, "A Khan's Presence", "Reach {target} charisma.", QuestType.ReachCharisma, 35, 18, RewardType.Gold, 100, 1f, "{gold} gold"),
            Q(20, "Caravan Discipline", "Reach a total population of {target}.", QuestType.ReachTotalPopulation, 1500, 19, RewardType.CargoSupplyCostMultiplier, 75, 1f, "Cargo-funded supply costs 1.5x normal and {gold} gold"),
            Q(21, "Trade Routes", "Build {target} Trade Buildings.", QuestType.BuildBuildings, 3, 20, RewardType.TradeIncome, 50, 1.25f, "+50 trade income and {gold} gold", "Trade_Building"),
            Q(22, "A Crowded Heartland", "Reach a total population of {target}.", QuestType.ReachTotalPopulation, 2500, 21, RewardType.Gold, 200, 1.5f, "{gold} gold"),
            Q(23, "Renowned Khan", "Reach {target} charisma.", QuestType.ReachCharisma, 50, 22, RewardType.Gold, 250, 2f, "{gold} gold"),
            Q(24, "A Living Empire", "Reach a total population of {target}.", QuestType.ReachTotalPopulation, 4000, 23, RewardType.PopulationCapacity, 250, 3f, "+250 population capacity in every province and {gold} gold")
        };
    }

    private static Definition Q(
        int id, string title, string description, QuestType type, int target, int prerequisite,
        RewardType reward, int amount, float goldTurns, string rewardDescription,
        string requiredBuilding = null, bool dynamicTarget = false)
    {
        return new Definition
        {
            Id = id,
            Title = title,
            Description = description,
            Type = type,
            Target = target,
            Prerequisite = prerequisite,
            Reward = reward,
            Amount = amount,
            GoldTurns = goldTurns,
            RewardDescription = rewardDescription,
            RequiredBuilding = requiredBuilding,
            DynamicTarget = dynamicTarget
        };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        string name = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
