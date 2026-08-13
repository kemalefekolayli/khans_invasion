using UnityEngine;

/// <summary>
/// Owns player military capacity rules and end-of-turn army maintenance.
/// Runtime modifiers are additive so quests and buildings can apply bonuses without
/// coupling themselves to recruitment or turn-processing code.
/// </summary>
public class MilitaryEconomy : MonoBehaviour, ITurnProcessor
{
    public static MilitaryEconomy Instance { get; private set; }

    [Header("Force Capacity")]
    [SerializeField, Min(0f)] private float populationForceLimitRatio = 0.40f;
    [SerializeField] private float forceLimitFlatModifier = 0f;
    [SerializeField, Min(0f)] private float forceLimitMultiplier = 1f;

    [Header("General Capacity")]
    [SerializeField, Min(0)] private int baseGeneralLimit = 1;
    [SerializeField] private int generalLimitFlatModifier = 0;
    [SerializeField, Min(0f)] private float generalLimitMultiplier = 1f;

    [Header("Army Maintenance")]
    [SerializeField, Min(0f)] private float maintenanceGoldPerSoldier = 0.1f;
    [SerializeField] private int processingPriority = 5;

    private TurnManager registeredTurnManager;

    public int ProcessingPriority => processingPriority;
    public float CurrentSoldiers => ArmyManager.Instance != null ? ArmyManager.Instance.TotalPlayerSoldiers : 0f;
    public float ForceLimit => CalculateForceLimit();
    public float RemainingForceCapacity => Mathf.Max(0f, ForceLimit - CurrentSoldiers);
    public int GeneralLimit => Mathf.Max(0, Mathf.FloorToInt((baseGeneralLimit + generalLimitFlatModifier) * generalLimitMultiplier));
    public int CurrentGeneralCount => CountPlayerGenerals();
    public float LastMaintenanceCost { get; private set; }
    public float LastPaidMaintenance { get; private set; }
    public float UnpaidMaintenance { get; private set; }
    public float CurrentMaintenanceCost => Mathf.Max(0f, CurrentSoldiers * maintenanceGoldPerSoldier);

    public static MilitaryEconomy GetOrCreate()
    {
        if (Instance != null) return Instance;

        MilitaryEconomy existing = FindFirstObjectByType<MilitaryEconomy>();
        if (existing != null) return existing;

        GameObject obj = new GameObject("MilitaryEconomy");
        MilitaryEconomy created = obj.AddComponent<MilitaryEconomy>();
        GameLog.Warning(GameLogCategory.Economy,
            "[MilitaryEconomy] Scene component missing; using runtime defaults for this scene.");
        return created;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnMapLoaded += TryRegisterTurnProcessor;
        GameEvents.OnPlayerNationReady += TryRegisterTurnProcessor;
    }

    private void Start()
    {
        TryRegisterTurnProcessor();
    }

    private void OnDisable()
    {
        GameEvents.OnMapLoaded -= TryRegisterTurnProcessor;
        GameEvents.OnPlayerNationReady -= TryRegisterTurnProcessor;

        if (registeredTurnManager != null)
            registeredTurnManager.UnregisterTurnProcessor(this);

        registeredTurnManager = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public float ClampNewSoldiers(float requestedAmount, string sourceLabel, bool showWarning = true)
    {
        float requested = Mathf.Max(0f, requestedAmount);
        float allowed = Mathf.Min(requested, RemainingForceCapacity);
        if (allowed + 0.001f >= requested) return allowed;

        string context = string.IsNullOrEmpty(sourceLabel) ? "Recruitment" : sourceLabel;
        string message = allowed <= 0f
            ? "Force limit reached"
            : $"Force limit: {allowed:F0} troops available";

        if (showWarning) CenterWarningPopupSpawner.Show(message);
        GameLog.Warning(GameLogCategory.Economy,
            $"[MilitaryEconomy] {context} limited to {allowed:F0}/{requested:F0} (force {CurrentSoldiers:F0}/{ForceLimit:F0}).");
        return allowed;
    }

    public bool CanCreateGeneral(bool showWarning = true)
    {
        if (CurrentGeneralCount < GeneralLimit) return true;

        if (showWarning) CenterWarningPopupSpawner.Show("General limit reached");
        GameLog.Warning(GameLogCategory.Economy,
            $"[MilitaryEconomy] General limit reached ({CurrentGeneralCount}/{GeneralLimit}).");
        return false;
    }

    public void ProcessTurnEnd(int turnNumber)
    {
        PlayerNation player = PlayerNation.Instance;
        if (player == null || player.currentNation == null) return;

        LastMaintenanceCost = CurrentMaintenanceCost;
        float availableGold = Mathf.Max(0f, player.nationMoney);
        LastPaidMaintenance = Mathf.Min(availableGold, LastMaintenanceCost);
        UnpaidMaintenance = Mathf.Max(0f, LastMaintenanceCost - LastPaidMaintenance);
        player.nationMoney = Mathf.Max(0f, availableGold - LastPaidMaintenance);

        if (UnpaidMaintenance > 0.001f)
        {
            CenterWarningPopupSpawner.Show($"Army upkeep unpaid: {UnpaidMaintenance:F0} gold");
            GameLog.Warning(GameLogCategory.Economy,
                $"[MilitaryEconomy] Upkeep shortfall {UnpaidMaintenance:F1} (paid {LastPaidMaintenance:F1}/{LastMaintenanceCost:F1}).");
        }

        GameEvents.PlayerStatsChanged();
    }

    public void AddForceLimitFlat(float amount)
    {
        forceLimitFlatModifier += amount;
        GameEvents.PlayerStatsChanged();
    }

    public void AddForceLimitMultiplier(float additiveMultiplier)
    {
        forceLimitMultiplier = Mathf.Max(0f, forceLimitMultiplier + additiveMultiplier);
        GameEvents.PlayerStatsChanged();
    }

    public void AddGeneralLimitFlat(int amount)
    {
        generalLimitFlatModifier += amount;
        GameEvents.PlayerStatsChanged();
    }

    public void AddGeneralLimitMultiplier(float additiveMultiplier)
    {
        generalLimitMultiplier = Mathf.Max(0f, generalLimitMultiplier + additiveMultiplier);
        GameEvents.PlayerStatsChanged();
    }

    private float CalculateForceLimit()
    {
        float civilianPopulation = PlayerNation.Instance != null ? PlayerNation.Instance.PopulationSize : 0f;
        float totalPlayerPopulation = civilianPopulation + CurrentSoldiers;
        return Mathf.Max(0f, (totalPlayerPopulation * populationForceLimitRatio + forceLimitFlatModifier) * forceLimitMultiplier);
    }

    private int CountPlayerGenerals()
    {
        GeneralSelectionManager selectionManager = GeneralSelectionManager.Instance;
        if (selectionManager == null) return 0;

        int count = 0;
        foreach (SelectableGeneral selectable in selectionManager.RegisteredGenerals)
        {
            if (selectable == null) continue;

            General general = selectable.GetComponent<General>();
            if (general == null) continue;
            if (general.IsCaptured) continue;

            Army army = general.CommandedArmy;
            if (selectable.IsKhan || army == null || army.IsPlayerArmy)
                count++;
        }

        return count;
    }

    private void TryRegisterTurnProcessor()
    {
        TurnManager manager = TurnManager.Instance;
        if (manager == null || registeredTurnManager == manager) return;

        if (registeredTurnManager != null)
            registeredTurnManager.UnregisterTurnProcessor(this);

        manager.RegisterTurnProcessor(this);
        registeredTurnManager = manager;
    }
}
