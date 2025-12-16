using System;
using UnityEngine;

/// <summary>
/// Central event system for game-wide communication.
/// Components subscribe to events they care about and invoke events when state changes.
/// </summary>
public static class GameEvents
{
    // ===== INITIALIZATION EVENTS =====
    
    /// <summary>Fired when NationLoader finishes loading all nations from JSON</summary>
    public static event Action OnNationsLoaded;
    
    /// <summary>Fired when all provinces are assigned to nations</summary>
    public static event Action OnProvincesAssigned;
    
    /// <summary>Fired when PlayerNation is initialized and ready</summary>
    public static event Action OnPlayerNationReady;
    
    /// <summary>Fired when the map (provinces) is fully loaded</summary>
    public static event Action OnMapLoaded;
    
    // ===== GAMEPLAY EVENTS =====
    
    /// <summary>Fired when a turn ends</summary>
    public static event Action<int> OnTurnEnded; // passes new turn number
    
    /// <summary>Fired when player's nation changes (e.g., switched nations)</summary>
    public static event Action<NationModel> OnPlayerNationChanged;
    
    /// <summary>Fired when a province changes ownership</summary>
    public static event Action<ProvinceModel, NationModel, NationModel> OnProvinceOwnerChanged; // province, oldOwner, newOwner
    
    /// <summary>Fired when player stats need to be recalculated and GUI updated</summary>
    public static event Action OnPlayerStatsChanged;
    
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
}