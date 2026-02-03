using System;
using UnityEngine;

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
    Debug.Log(">> Event: ProvinceDataLoaded");
    OnProvinceDataLoaded?.Invoke();
}
    // ===== TROOP LEVEL EVENTS =====
    
    public static event System.Action<TroopLevel, int, int> OnTroopLevelUp;  // troopLevel, fromLevel, toLevel
    public static event System.Action<TroopLevel> OnTroopMaxLevel;  
    // ===== INVOKE METHODS =====
    
    public static void BarrackMenuOpened()
    {
        Debug.Log($">> Event: BarrackMenuOpened ");
        OnBarrackMenuOpened?.Invoke();
    }

    
    public static void NationsLoaded()
    {
        Debug.Log(">> Event: NationsLoaded");
        OnNationsLoaded?.Invoke();
    }
    
    public static void ProvincesAssigned()
    {
        Debug.Log(">> Event: ProvincesAssigned");
        OnProvincesAssigned?.Invoke();
    }
    
    public static void PlayerNationReady()
    {
        Debug.Log(">> Event: PlayerNationReady");
        OnPlayerNationReady?.Invoke();
    }
    
    public static void MapLoaded()
    {
        Debug.Log(">> Event: MapLoaded");
        OnMapLoaded?.Invoke();
    }
    
    public static void TurnEnded(int newTurn)
    {
        Debug.Log($">> Event: TurnEnded (Turn {newTurn})");
        OnTurnEnded?.Invoke(newTurn);
    }
    
    public static void PlayerNationChanged(NationModel newNation)
    {
        Debug.Log($">> Event: PlayerNationChanged ({newNation?.nationName})");
        OnPlayerNationChanged?.Invoke(newNation);
    }
    
    public static void ProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        Debug.Log($">> Event: ProvinceOwnerChanged ({province?.provinceName}: {oldOwner?.nationName} -> {newOwner?.nationName})");
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
        Debug.Log($">> Event: PlayerCapitalSet ({capitalProvince?.provinceName})");
        OnPlayerNationCapitalSet?.Invoke(capitalProvince);
    }
    public static void CityCenterEnter(CityCenter cityCenter)
    {
        Debug.Log($">> Event: CityCenterEnter ({cityCenter?.Province?.provinceName})");
        OnCityCenterEnter?.Invoke(cityCenter);
    }
    
    public static void CityCenterExit(CityCenter cityCenter)
    {
        Debug.Log($">> Event: CityCenterExit ({cityCenter?.Province?.provinceName})");
        OnCityCenterExit?.Invoke(cityCenter);
    }
    
    // Province Interaction Events
    public static void ProvinceManagementOpened(ProvinceModel province)
    {
        Debug.Log($">> Event: ProvinceManagementOpened ({province?.provinceName})"); 
        OnProvinceManagementOpened?.Invoke(province);
    }
    
    public static void ProvinceInteractionOpened(ProvinceModel province)
    {
        Debug.Log($">> Event: ProvinceInteractionOpened ({province?.provinceName})");
        OnProvinceInteractionOpened?.Invoke(province);
    }
    
    public static void ProvincePanelClosed()
    {
        Debug.Log(">> Event: ProvincePanelClosed");
        OnProvincePanelClosed?.Invoke();
    }
    
    // Building Events
    public static void BuildingConstructed(ProvinceModel province, string buildingType)
    {
        Debug.Log($">> Event: BuildingConstructed ({buildingType} in {province?.provinceName})");
        OnBuildingConstructed?.Invoke(province, buildingType);
    }
    
    public static void BuildingDestroyed(ProvinceModel province, string buildingType)
    {
        Debug.Log($">> Event: BuildingDestroyed ({buildingType} in {province?.provinceName})");
        OnBuildingDestroyed?.Invoke(province, buildingType);
    }
    
    // Army Events
    public static void ArmySpawned(Army army, General general)
    {
        Debug.Log($">> Event: ArmySpawned (Size: {army?.ArmySize} for {general?.GeneralName})");
        OnArmySpawned?.Invoke(army, general);
    }
    
    public static void ArmyDestroyed(Army army)
    {
        Debug.Log($">> Event: ArmyDestroyed");
        OnArmyDestroyed?.Invoke(army);
    }
    
    public static void ArmyAssigned(Army army, General general)
    {
        Debug.Log($">> Event: ArmyAssigned ({general?.GeneralName})");
        OnArmyAssigned?.Invoke(army, general);
    }

    // Troop Level Events
    public static void TroopLevelUp(TroopLevel troop, int fromLevel, int toLevel)
    {
        Debug.Log($">> Event: TroopLevelUp ({fromLevel} -> {toLevel})");
        OnTroopLevelUp?.Invoke(troop, fromLevel, toLevel);
        
        // Also fire max level event if applicable
        if (toLevel >= TroopLevelData.MAX_LEVEL)
        {
            TroopMaxLevel(troop);
        }
    }
    
    public static void TroopMaxLevel(TroopLevel troop)
    {
        Debug.Log($">> Event: TroopMaxLevel (GOLDEN!)");
        OnTroopMaxLevel?.Invoke(troop);
    }

    public static void GeneralEnteredCityCenter(CityCenter cityCenter, General general)
{
    Debug.Log($">> Event: GeneralEnteredCityCenter - {general?.GeneralName}");
    OnGeneralEnteredCityCenter?.Invoke(cityCenter, general);
}

    // Raid Events
    public static void ProvinceRaided(ProvinceModel province, General raider, float lootAmount)
    {
        Debug.Log($">> Event: ProvinceRaided ({province?.provinceName} by {raider?.GeneralName}, Loot: {lootAmount:F0})");
        OnProvinceRaided?.Invoke(province, raider, lootAmount);
    }
    
    // Siege Events
    public static void ProvinceSieged(ProvinceModel province, General attacker, float defenseStrength)
    {
        Debug.Log($">> Event: ProvinceSieged ({province?.provinceName} by {attacker?.GeneralName}, Defense: {defenseStrength:F0})");
        OnProvinceSieged?.Invoke(province, attacker, defenseStrength);
    }
    
    public static void SiegeFailed(ProvinceModel province, General attacker, SiegeManager.SiegeResult result)
    {
        Debug.Log($">> Event: SiegeFailed ({province?.provinceName} - {result})");
        OnSiegeFailed?.Invoke(province, attacker, result);
    }
    
    public static void ProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        Debug.Log($">> Event: ProvinceConquered ({province?.provinceName}: {oldOwner?.nationName} -> {newOwner?.nationName})");
        OnProvinceConquered?.Invoke(province, oldOwner, newOwner);
    }
    
    public static void SiegeCancelled(ProvinceModel province)
    {
        Debug.Log($">> Event: SiegeCancelled ({province?.provinceName})");
        OnSiegeCancelled?.Invoke(province);
    }
    
    public static void SiegeCasualties(ProvinceModel province, General general, int casualties, int turnsRemaining)
    {
        Debug.Log($">> Event: SiegeCasualties ({province?.provinceName} - {general?.GeneralName} lost {casualties}, {turnsRemaining} turns left)");
        OnSiegeCasualties?.Invoke(province, general, casualties, turnsRemaining);
    }
    
    // ===== POPULATION EVENTS =====
    
    public static event Action<ProvinceModel, float> OnPopulationGrowth; // province, growthAmount
    
    public static void PopulationGrowth(ProvinceModel province, float growthAmount)
    {
        OnPopulationGrowth?.Invoke(province, growthAmount);
    }
}