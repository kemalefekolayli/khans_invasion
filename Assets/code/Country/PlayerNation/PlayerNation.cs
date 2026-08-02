using UnityEngine;
using System;

public class PlayerNation : MonoBehaviour
{
    [Header("Current Nation Reference")]
    [NonSerialized] // Don't show in Inspector - will be set at runtime
    public NationModel currentNation;
    
    [Header("Player-Specific Data")]
    [Tooltip("Gold the player starts with - applied to the nation treasury on init")]
    [UnityEngine.Serialization.FormerlySerializedAs("nationMoney")]
    [SerializeField] private float startingMoney = 0f;

    // Single source of truth is the nation's treasury (shared model with AI nations)
    public float nationMoney
    {
        get => currentNation?.treasury ?? 0f;
        set { if (currentNation != null) currentNation.treasury = value; }
    }
    public int currentTurn = 1;
    
    [Header("Quest Rewards")]
    public float bonusTradeIncome = 0f;
    public bool canMoveCapital = false;
    
    [Header("Initialization")]
    public int startingNationId = 0;
    
    // Singleton for easy access
    public static PlayerNation Instance { get; private set; }

    // Convenience properties - delegate to currentNation
    public NationModel Nation => currentNation;
    public System.Collections.Generic.List<ProvinceModel> OwnedProvinces => currentNation?.provinceList;
    public long NationId => currentNation?.nationId ?? 0;
    public string NationName => currentNation?.nationName ?? "No Nation";
    public string NationColor => currentNation?.nationColor ?? "#808080";
    public int CityCount => currentNation?.provinceList.Count ?? 0;
    
    // Calculated properties
    public float TaxIncome { get; private set; }
    public float TradeIncome { get; private set; }
    public float TotalIncome => TaxIncome + TradeIncome + bonusTradeIncome;
    public float PopulationSize { get; private set; }
    public float ArmySize { get; private set; }
    public float ArmyStrength { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    
    private void OnEnable()
    {
        GameLog.Log(GameLogCategory.Core, "PlayerNation.OnEnable - subscribing to events");
        GameEvents.OnProvinceDataLoaded += OnProvincesAssigned;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        
        // Army events
        GameEvents.OnArmySpawned += OnArmySpawned;
        GameEvents.OnArmyDestroyed += OnArmyDestroyed;
        GameEvents.OnArmySizeChanged += OnArmySizeChanged;
        GameEvents.OnSiegeCasualties += OnSiegeCasualtiesLoop;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceDataLoaded -= OnProvincesAssigned;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
        
        // Army events
        GameEvents.OnArmySpawned -= OnArmySpawned;
        GameEvents.OnArmyDestroyed -= OnArmyDestroyed;
        GameEvents.OnArmySizeChanged -= OnArmySizeChanged;
        GameEvents.OnSiegeCasualties -= OnSiegeCasualtiesLoop;
    }
    
    // Event handlers to trigger recalculation
    private void OnArmySpawned(Army army, General general) => RecalculateStats();
    private void OnArmyDestroyed(Army army) => RecalculateStats();
    private void OnArmySizeChanged(Army army) => RecalculateStats();
    private void OnSiegeCasualtiesLoop(ProvinceModel province, General general, int casualties, int turns) => RecalculateStats();

    private void OnProvincesAssigned()
    {
        GameLog.Log(GameLogCategory.Core, "PlayerNation received OnProvinceDataLoaded event");
        InitializePlayer();
    }

    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (currentNation != null && (oldOwner == currentNation || newOwner == currentNation))
        {
            RecalculateStats();
            GameEvents.PlayerStatsChanged();
        }
    }

    private void InitializePlayer()
    {

        
        if (currentNation == null)
        {
            NationLoader loader = FindFirstObjectByType<NationLoader>();
            
            if (loader == null)
            {
                GameLog.Error(GameLogCategory.Core, "NationLoader not found!");
                return;
            }
            

            
            currentNation = loader.GetNationById(startingNationId);

            if (currentNation != null)
            {
                currentNation.isPlayer = true;
                currentNation.treasury = startingMoney;
            }
            else
            {
                GameLog.Error(GameLogCategory.Core, $"GetNationById({startingNationId}) returned null!");
                return;
            }
        }
        else
        {
            GameLog.Log(GameLogCategory.Core, $"currentNation already set: {currentNation.nationName}");
        }
        
        RecalculateStats();
        GameEvents.PlayerNationReady();
    }

    public void SetNation(NationModel nation)
    {
        if (currentNation != null)
        {
            currentNation.isPlayer = false;
        }
        
        currentNation = nation;
        
        if (currentNation != null)
        {
            currentNation.isPlayer = true;

        }
        
        RecalculateStats();
        GameEvents.PlayerNationChanged(currentNation);
        GameEvents.PlayerStatsChanged();
    }

    public void SetNationById(int nationId)
    {
        NationLoader loader = FindFirstObjectByType<NationLoader>();
        if (loader != null)
        {
            NationModel nation = loader.GetNationById(nationId);
            if (nation != null)
            {
                SetNation(nation);
            }
            else
            {
                GameLog.Error(GameLogCategory.Core, $"Nation with ID {nationId} not found!");
            }
        }
    }

    public void RecalculateStats()
    {
        if (currentNation == null)
        {
            GameLog.Warning(GameLogCategory.Core, "RecalculateStats: currentNation is null!");
            return;
        }
        
        TaxIncome = 0f;
        TradeIncome = 0f;
        PopulationSize = 0f;
        ArmySize = 0f;
        ArmyStrength = 0f;
        
        foreach (ProvinceModel province in currentNation.provinceList)
        {
            if (province != null)
            {
                TaxIncome += province.provinceTaxIncome;
                TradeIncome += province.provinceTradePower;
                PopulationSize += province.provinceCurrentPop;
            }
        }
        
        // Add mobile army troops - this is all we care about!
        if (ArmyManager.Instance != null)
        {
            ArmySize += ArmyManager.Instance.TotalPlayerSoldiers;
            ArmyStrength += ArmyManager.Instance.TotalPlayerStrength;
        }
        
        // Notify UI
        GameEvents.PlayerStatsChanged();
        

    }

    /// <summary>
    /// DEPRECATED: Use TurnManager.Instance.EndPlayerTurn() instead.
    /// Income is now handled by IncomeProcessor.
    /// </summary>
    [System.Obsolete("Use TurnManager.Instance.EndPlayerTurn() instead.")]
    public void EndTurn()
    {
        GameLog.Warning(GameLogCategory.Core, "[PlayerNation] EndTurn() is deprecated. Use TurnManager.Instance.EndPlayerTurn() instead.");
        
        // Delegate to TurnManager if available
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndPlayerTurn();
        }
        else
        {
            // Legacy fallback
            if (currentNation == null) return;
            
            nationMoney += TotalIncome;
            currentTurn++;
            
            RecalculateStats();
            
            GameLog.Log(GameLogCategory.Core, $"Turn {currentTurn}: +{TotalIncome:F0} gold (Total: {nationMoney:F0})");
            
            GameEvents.TurnEnded(currentTurn);
            GameEvents.PlayerStatsChanged();
        }
    }
    
    /// <summary>
    /// Get the current turn from TurnManager.
    /// </summary>
    public int GetCurrentTurn()
    {
        return TurnManager.Instance?.CurrentTurn ?? currentTurn;
    }

    public bool OwnsProvince(ProvinceModel province)
    {
        if (currentNation == null || province == null) return false;
        return province.provinceOwner == currentNation;
    }

    public Color GetNationColor()
    {
        if (currentNation == null) return Color.gray;
        return NationLoader.HexToColor(currentNation.nationColor);
    }
}