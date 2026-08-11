using System;
using UnityEngine;

public enum CityOperationType { Discovery, Raid, Siege, Battle, Building }

/// <summary>
/// Central event system for game-wide communication.
/// Components subscribe to events they care about and invoke events when state changes.
/// </summary>
public static class GameEvents
{
    
    public static event Action OnProvinceDataLoaded;
    // ===== INITIALIZATION EVENTS =====
    
    public static event Action OnNationsLoaded;
    public static event Action OnProvincesAssigned;
    public static event Action OnPlayerNationReady;
    public static event Action OnMapLoaded;

    // ===== GENERAL EVENTS =====
    public static event Action<CityCenter, General> OnGeneralEnteredCityCenter;
    
    // ===== GAMEPLAY EVENTS =====
    public static event Action<int> OnTurnEnded;
    public static event Action<NationModel> OnPlayerNationChanged;
    public static event Action<ProvinceModel, NationModel, NationModel> OnProvinceOwnerChanged;
    public static event Action OnPlayerStatsChanged;
    
    // ===== PROVINCE DETECTION EVENTS =====
    
    public static event Action<ProvinceModel> OnProvinceEnter;
    public static event Action<ProvinceModel> OnProvinceExit;
    
    // ===== CITY CENTER EVENTS =====
    public static event Action<ProvinceModel> OnPlayerNationCapitalSet;
    public static event Action<CityCenter> OnCityCenterEnter;
    public static event Action<CityCenter> OnCityCenterExit;
    public static event Action<NationModel, ProvinceModel, CityOperationType, General> OnCityOperation;
    
    // ===== PROVINCE INTERACTION EVENTS =====
    
    public static event Action<ProvinceModel> OnProvinceManagementOpened;  // Player-owned
    public static event Action<ProvinceModel> OnProvinceInteractionOpened; // Enemy/neutral
    public static event Action OnProvincePanelClosed;

    public static event Action OnBarrackMenuOpened;
    
    // ===== BUILDING EVENTS =====
    
    public static event Action<ProvinceModel, string> OnBuildingConstructed;
    public static event Action<ProvinceModel, string> OnBuildingDestroyed;
    
    // ===== ARMY EVENTS =====
    
    public static event Action<Army, General> OnArmySpawned;
    public static event Action<Army> OnArmyDestroyed;
    public static event Action<Army, General> OnArmyAssigned;
    public static event Action<Army> OnArmySizeChanged;
    public static event Action<Army> OnArmySupplyChanged;
    public static event Action<General> OnGeneralLootChanged;
    public static event Action<TribeGroup, General> OnTribeRecruited;
    public static event Action<Army, Army> OnArmyBattleStarted;
    public static event Action<Army, Army, float, float, int, int, int> OnArmyBattleTick;
    public static event Action<Army, Army, ArmyBattleEndReason> OnArmyBattleEnded;
    
    // ===== RAID EVENTS =====
    
    public static event Action<ProvinceModel, General, float> OnProvinceRaided; // province, raider, lootAmount
    
    // ===== SIEGE EVENTS =====
    
    public static event Action<ProvinceModel, General, float> OnProvinceSieged; // province, attacker, defenseStrength
    public static event Action<ProvinceModel, General, SiegeManager.SiegeResult> OnSiegeFailed; // province, attacker, reason
    public static event Action<ProvinceModel, NationModel, NationModel> OnProvinceConquered; // province, oldOwner, newOwner
    public static event Action<ProvinceModel> OnSiegeCancelled; // province (siege abandoned)
    public static event Action<ProvinceModel, General, int, int> OnSiegeCasualties; // province, general, casualties, turnsRemaining

    public static void ProvinceDataLoaded()
{
    GameLog.Log(GameLogCategory.Events, ">> Event: ProvinceDataLoaded");
    OnProvinceDataLoaded?.Invoke();
}
    // ===== TROOP LEVEL EVENTS =====
    
    public static event System.Action<TroopLevel, int, int> OnTroopLevelUp;  // troopLevel, fromLevel, toLevel
    public static event System.Action<TroopLevel> OnTroopMaxLevel;  
    // ===== INVOKE METHODS =====
    
    public static void BarrackMenuOpened()
    {
        OnBarrackMenuOpened?.Invoke();
    }

    
    public static void NationsLoaded()
    {
        OnNationsLoaded?.Invoke();
    }
    
    public static void ProvincesAssigned()
    {
        OnProvincesAssigned?.Invoke();
    }
    
    public static void PlayerNationReady()
    {
        OnPlayerNationReady?.Invoke();
    }
    
    public static void MapLoaded()
    {
        OnMapLoaded?.Invoke();
    }
    
    public static void TurnEnded(int newTurn)
    {
        OnTurnEnded?.Invoke(newTurn);
    }
    
    public static void PlayerNationChanged(NationModel newNation)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: PlayerNationChanged ({newNation?.nationName})");
        OnPlayerNationChanged?.Invoke(newNation);
    }
    
    public static void ProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ProvinceOwnerChanged ({province?.provinceName}: {oldOwner?.nationName} -> {newOwner?.nationName})");
        OnProvinceOwnerChanged?.Invoke(province, oldOwner, newOwner);
    }
    
    public static void PlayerStatsChanged()
    {
        OnPlayerStatsChanged?.Invoke();
    }
    
    public static void ProvinceEnter(ProvinceModel province)
    {
        OnProvinceEnter?.Invoke(province);
    }
    
    public static void ProvinceExit(ProvinceModel province)
    {
        OnProvinceExit?.Invoke(province);
    }
    
    // City Center Events
    public static void PlayerCapitalSet(ProvinceModel capitalProvince)
    {
        OnPlayerNationCapitalSet?.Invoke(capitalProvince);
    }
    public static void CityCenterEnter(CityCenter cityCenter)
    {
        OnCityCenterEnter?.Invoke(cityCenter);
    }
    
    public static void CityCenterExit(CityCenter cityCenter)
    {
        OnCityCenterExit?.Invoke(cityCenter);
    }
    
    public static void RecordCityOperation(NationModel nation, ProvinceModel province, CityOperationType operationType, General general = null)
    {
        if (nation == null || province == null) return;

        GameLog.Log(GameLogCategory.Events, $"CITY OP nation={nation.nationName} city={province.provinceName} type={operationType}");
        OnCityOperation?.Invoke(nation, province, operationType, general);
    }

    // Province Interaction Events
    public static void ProvinceManagementOpened(ProvinceModel province)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ProvinceManagementOpened ({province?.provinceName})"); 
        OnProvinceManagementOpened?.Invoke(province);
    }
    
    public static void ProvinceInteractionOpened(ProvinceModel province)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ProvinceInteractionOpened ({province?.provinceName})");
        OnProvinceInteractionOpened?.Invoke(province);
    }
    
    public static void ProvincePanelClosed()
    {
        OnProvincePanelClosed?.Invoke();
    }
    
    // Building Events
    public static void BuildingConstructed(ProvinceModel province, string buildingType)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: BuildingConstructed ({buildingType} in {province?.provinceName})");
        OnBuildingConstructed?.Invoke(province, buildingType);
    }
    
    public static void BuildingDestroyed(ProvinceModel province, string buildingType)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: BuildingDestroyed ({buildingType} in {province?.provinceName})");
        OnBuildingDestroyed?.Invoke(province, buildingType);
    }
    
    // Army Events
    public static void ArmySpawned(Army army, General general)
    {
        OnArmySpawned?.Invoke(army, general);
    }
    
    public static void ArmyDestroyed(Army army)
    {
        GameLog.Log(GameLogCategory.Events, ">> Event: ArmyDestroyed");
        OnArmyDestroyed?.Invoke(army);
    }
    
    public static void ArmyAssigned(Army army, General general)
    {
        OnArmyAssigned?.Invoke(army, general);
    }
    
    public static void ArmySizeChanged(Army army)
    {
        OnArmySizeChanged?.Invoke(army);
    }

    public static void ArmySupplyChanged(Army army)
    {
        OnArmySupplyChanged?.Invoke(army);
    }

    public static void GeneralLootChanged(General general)
    {
        OnGeneralLootChanged?.Invoke(general);
    }

    public static void TribeRecruited(TribeGroup tribe, General recruiter)
    {
        OnTribeRecruited?.Invoke(tribe, recruiter);
    }

    public static void ArmyBattleStarted(Army armyA, Army armyB)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ArmyBattleStarted ({armyA?.Data.armyName} vs {armyB?.Data.armyName})");
        OnArmyBattleStarted?.Invoke(armyA, armyB);
    }

    public static void ArmyBattleTick(Army armyA, Army armyB, float armyALoss, float armyBLoss, int armyARoll, int armyBRoll, int turn)
    {
        OnArmyBattleTick?.Invoke(armyA, armyB, armyALoss, armyBLoss, armyARoll, armyBRoll, turn);
    }

    public static void ArmyBattleEnded(Army winner, Army loser, ArmyBattleEndReason reason)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ArmyBattleEnded ({winner?.Data.armyName} vs {loser?.Data.armyName}, {reason})");
        OnArmyBattleEnded?.Invoke(winner, loser, reason);
    }

    // Troop Level Events
    public static void TroopLevelUp(TroopLevel troop, int fromLevel, int toLevel)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: TroopLevelUp ({fromLevel} -> {toLevel})");
        OnTroopLevelUp?.Invoke(troop, fromLevel, toLevel);
        
        // Also fire max level event if applicable
        if (toLevel >= TroopLevelData.MAX_LEVEL)
        {
            TroopMaxLevel(troop);
        }
    }
    
    public static void TroopMaxLevel(TroopLevel troop)
    {
        GameLog.Log(GameLogCategory.Events, ">> Event: TroopMaxLevel (GOLDEN!)");
        OnTroopMaxLevel?.Invoke(troop);
    }

    public static void GeneralEnteredCityCenter(CityCenter cityCenter, General general)
    {
        OnGeneralEnteredCityCenter?.Invoke(cityCenter, general);
    }

    // Raid Events
    public static void ProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ProvinceRaided ({province?.provinceName} by {raider?.GeneralName}, Loot: {lootAmount:F0})");
        OnProvinceRaided?.Invoke(province, raider, lootAmount);
    }
    
    // Siege Events
    public static void ProvinceSieged(ProvinceModel province, General attacker, float defenseStrength)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ProvinceSieged ({province?.provinceName} by {attacker?.GeneralName}, Defense: {defenseStrength:F0})");
        OnProvinceSieged?.Invoke(province, attacker, defenseStrength);
    }
    
    public static void SiegeFailed(ProvinceModel province, General attacker, SiegeManager.SiegeResult result)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: SiegeFailed ({province?.provinceName} - {result})");
        OnSiegeFailed?.Invoke(province, attacker, result);
    }
    
    public static void ProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: ProvinceConquered ({province?.provinceName}: {oldOwner?.nationName} -> {newOwner?.nationName})");
        OnProvinceConquered?.Invoke(province, oldOwner, newOwner);
    }
    
    public static void SiegeCancelled(ProvinceModel province)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: SiegeCancelled ({province?.provinceName})");
        OnSiegeCancelled?.Invoke(province);
    }
    
    public static void SiegeCasualties(ProvinceModel province, General general, int casualties, int turnsRemaining)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: SiegeCasualties ({province?.provinceName} - {general?.GeneralName} lost {casualties}, {turnsRemaining} turns left)");
        OnSiegeCasualties?.Invoke(province, general, casualties, turnsRemaining);
    }
    
    // ===== POPULATION EVENTS =====
    
    public static event Action<ProvinceModel, float> OnPopulationGrowth; // province, growthAmount
    
    public static void PopulationGrowth(ProvinceModel province, float growthAmount)
    {
        OnPopulationGrowth?.Invoke(province, growthAmount);
    }
    
    // ===== QUEST EVENTS =====
    
    public static event Action<string, ProvinceModel> OnBuildingBuilt;
    public static event Action<Army> OnArmyDefeated;
    public static event Action<NationModel> OnNationDestroyed;
    public static event Action OnQuestPanelOpened;
    public static event Action OnQuestPanelClosed;
    
    public static void BuildingBuilt(string buildingType, ProvinceModel province)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: BuildingBuilt ({buildingType} in {province?.provinceName})");
        OnBuildingBuilt?.Invoke(buildingType, province);
    }
    
    public static void ArmyDefeated(Army army)
    {
        GameLog.Log(GameLogCategory.Events, ">> Event: ArmyDefeated");
        OnArmyDefeated?.Invoke(army);
    }
    
    public static void NationDestroyed(NationModel nation)
    {
        GameLog.Log(GameLogCategory.Events, $">> Event: NationDestroyed ({nation?.nationName})");
        OnNationDestroyed?.Invoke(nation);
    }
    
    public static void QuestPanelOpened()
    {
        OnQuestPanelOpened?.Invoke();
    }
    
    public static void QuestPanelClosed()
    {
        OnQuestPanelClosed?.Invoke();
    }
}
