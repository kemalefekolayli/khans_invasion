using System;
using UnityEngine;

/// <summary>
/// Central event system for game-wide communication.
/// Components subscribe to events they care about and invoke events when state changes.
/// </summary>
public static class GameEvents
{
    // ===== INITIALIZATION EVENTS =====
    
    public static event Action OnNationsLoaded;
    public static event Action OnProvincesAssigned;
    public static event Action OnPlayerNationReady;
    public static event Action OnMapLoaded;
    
    // ===== GAMEPLAY EVENTS =====
    
    public static event Action<int> OnTurnEnded;
    public static event Action<NationModel> OnPlayerNationChanged;
    public static event Action<ProvinceModel, NationModel, NationModel> OnProvinceOwnerChanged;
    public static event Action OnPlayerStatsChanged;
    
    // ===== PROVINCE DETECTION EVENTS =====
    
    public static event Action<ProvinceModel> OnProvinceEnter;
    public static event Action<ProvinceModel> OnProvinceExit;
    
    // ===== INVOKE METHODS =====
    
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
}