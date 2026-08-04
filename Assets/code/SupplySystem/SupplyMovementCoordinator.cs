using System.Collections.Generic;
using UnityEngine;

/// <summary>Orchestrates player movement, cost policy, route state and operation commits.</summary>
public class SupplyMovementCoordinator : MonoBehaviour
{
    public static SupplyMovementCoordinator Instance { get; private set; }

    [Header("Army Supply")]
    [SerializeField, Min(1f)] private float maximumSupply = 100f;
    [SerializeField, Min(0f)] private float startingSupply = 100f;
    [Header("Diagnostics")]
    [SerializeField] private bool diagnosticsEnabled = true;
    [Header("Construction Cost")]
    [SerializeField] private SupplyCostSettings costSettings = new();

    private readonly Dictionary<Army, int> movementSequences = new();
    private readonly Dictionary<Army, float> nextBlockLogTime = new();
    private const float BlockLogCooldown = 2f;
    private SupplyCostCalculator costCalculator;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<SupplyMovementCoordinator>() != null) return;
        GameObject host = new(nameof(SupplyMovementCoordinator));
        DontDestroyOnLoad(host);
        host.AddComponent<SupplyMovementCoordinator>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        costCalculator = new SupplyCostCalculator(costSettings);
    }

    private void OnEnable()
    {
        GameEvents.OnProvinceEnter += OnProvinceEnter;
        GameEvents.OnCityOperation += OnCityOperation;
        GameEvents.OnArmySpawned += OnArmySpawned;
        GameEvents.OnArmyAssigned += OnArmyAssigned;
        GameEvents.OnTurnEnded += OnTurnEnded;
        GeneralSelectionManager.OnGeneralSelected += OnGeneralSelected;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceEnter -= OnProvinceEnter;
        GameEvents.OnCityOperation -= OnCityOperation;
        GameEvents.OnArmySpawned -= OnArmySpawned;
        GameEvents.OnArmyAssigned -= OnArmyAssigned;
        GameEvents.OnTurnEnded -= OnTurnEnded;
        GeneralSelectionManager.OnGeneralSelected -= OnGeneralSelected;
    }

    private void Start()
    {
        SelectableGeneral selected = GeneralSelectionManager.Instance?.SelectedGeneral;
        if (selected != null) OnGeneralSelected(selected);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnArmySpawned(Army army, General general) => PrepareArmy(army, general?.GetComponent<SelectableGeneral>()?.CurrentProvince);
    private void OnArmyAssigned(Army army, General general) => PrepareArmy(army, general?.GetComponent<SelectableGeneral>()?.CurrentProvince);
    private void OnGeneralSelected(SelectableGeneral selectable) => PrepareArmy(selectable?.GetComponent<General>()?.CommandedArmy, selectable?.CurrentProvince);

    private void PrepareArmy(Army army, ProvinceModel observedProvince)
    {
        NationModel player = PlayerNation.Instance?.currentNation;
        if (!IsPlayerArmy(army, player)) return;

        ArmySupplyAccount account = army.GetComponent<ArmySupplyAccount>();
        if (account == null)
        {
            account = army.gameObject.AddComponent<ArmySupplyAccount>();
            account.Configure(maximumSupply, startingSupply);
        }

        ProvinceModel origin = observedProvince ?? army.CurrentProvince ?? player.capitalProvince;
        SupplyRouteTracker.Instance?.SeedArmy(army, origin);
    }

    public bool CanEnterProvince(Army army, ProvinceModel from, ProvinceModel destination, out string reason)
    {
        reason = null;
        NationModel nation = PlayerNation.Instance?.currentNation;
        SupplyRouteTracker tracker = SupplyRouteTracker.Instance;
        ArmySupplyAccount account = army != null ? army.GetComponent<ArmySupplyAccount>() : null;
        if (army == null || from == null || destination == null || from == destination || !IsPlayerArmy(army, nation) || tracker == null || account == null)
            return true;

        if (tracker.HasActiveEdge(army, from, destination) || tracker.IsImmediatePendingBacktrack(army, from, destination))
            return true;

        bool known = tracker.IsKnownCityReadOnly(nation, destination);
        float required = costCalculator.Calculate(new SupplyMoveContext(army.ArmySize, destination.provinceOwner == nation, known));
        if (account.Available + 0.001f >= required)
            return true;

        reason = "Supply depleted. Return to the nearest resupply city.";
        LogBlocked(army, from, destination, required, account.Available);
        return false;
    }
    private void OnProvinceEnter(ProvinceModel destination)
    {
        SelectableGeneral selected = GeneralSelectionManager.Instance?.SelectedGeneral;
        if (destination == null || selected == null || selected.CurrentProvince != destination) return;

        General actor = selected.GetComponent<General>();
        Army army = actor?.CommandedArmy;
        NationModel nation = PlayerNation.Instance?.currentNation;
        if (!IsPlayerArmy(army, nation)) return;

        SupplyRouteTracker tracker = SupplyRouteTracker.Instance;
        PrepareArmy(army, null);
        SupplyRouteTracker.ArmyRouteState route = tracker?.GetArmyState(army);
        if (route == null) return;

        if (route.CurrentProvince == null || route.CurrentProvince == destination)
        {
            ProvinceModel fallback = army.CurrentProvince != destination ? army.CurrentProvince : nation.capitalProvince;
            tracker.SeedArmy(army, fallback);
            route = tracker.GetArmyState(army);
            if (route.CurrentProvince == destination) return;
        }

        ProvinceModel source = route.CurrentProvince;
        ArmySupplyAccount account = army.GetComponent<ArmySupplyAccount>();
        int sequence = NextSequence(army);
        bool owned = destination.provinceOwner == nation;
        bool capital = destination == nation.capitalProvince;
        bool first = !tracker.IsKnownCity(nation, destination);
        Log($"SUP ENTER seq={sequence} army={army.name} from={Name(source)} to={Name(destination)} own={(owned ? 1 : 0)} cap={(capital ? 1 : 0)} first={(first ? 1 : 0)} stock={(account != null ? account.Available : 0f):0.#}", army);

        FreeTraversalResult traversal = tracker.TryRecordFreeTraversal(army, destination, out RouteEdge edge);
        if (traversal == FreeTraversalResult.ActivePass)
        {
            Log($"SUP PASS seq={sequence} army={army.name} edge={Name(source)}>{Name(destination)} count={edge.TraversalCount}", army);
        }
        else if (traversal == FreeTraversalResult.PendingPop)
        {
            Log($"SUP POP seq={sequence} army={army.name} edge={Name(source)}>{Name(destination)}", army);
        }
        else
        {
            BuildEdge(sequence, nation, army, source, destination, tracker);
        }

        if (SupplyRouteTracker.IsCity(destination) && first)
            GameEvents.RecordCityOperation(nation, destination, CityOperationType.Discovery, actor);

        route = tracker.GetArmyState(army);
        Log($"SUP ROUTE seq={sequence} city={Name(destination)} pending={Mathf.Max(0, route.PendingRoute.Count - 1)} active={route.ActiveEdges.Count}", army);
    }

    private void BuildEdge(int sequence, NationModel nation, Army army, ProvinceModel source, ProvinceModel destination, SupplyRouteTracker tracker)
    {
        bool known = tracker.IsKnownCity(nation, destination);
        SupplyMoveContext context = new(army.ArmySize, destination.provinceOwner == nation, known);
        float cost = costCalculator.Calculate(context);
        ArmySupplyAccount account = army.GetComponent<ArmySupplyAccount>();
        if (account == null || !account.TryFund(cost))
        {
            float available = account != null ? account.Available : 0f;
            Log($"SUP LOW seq={sequence} army={army.name} edge={Name(source)}>{Name(destination)} need={cost:0.#} have={available:0.#}", army);
            tracker.SynchronizePosition(army, destination);
            return;
        }

        tracker.RecordPaidTransition(army, destination, SupplyFundingType.Stock, out _);
        Log($"SUP BUILD seq={sequence} army={army.name} edge={Name(source)}>{Name(destination)} cost={cost:0.#} stock={account.Available:0.#}", army);
    }

    private void OnCityOperation(NationModel nation, ProvinceModel province, CityOperationType operation, General actor)
    {
        SupplyRouteTracker tracker = SupplyRouteTracker.Instance;
        if (tracker == null || nation == null || province == null) return;

        tracker.MarkKnownCity(nation, province);
        Army army = actor?.CommandedArmy;
        if (!IsPlayerArmy(army, nation)) return;
        tracker.CommitPending(army, province);
    }

    private void OnTurnEnded(int turnNumber)
    {
        NationModel nation = PlayerNation.Instance?.currentNation;
        ProvinceModel capital = nation?.capitalProvince;
        if (nation == null || capital == null || capital.provinceOwner != nation || ArmyManager.Instance == null) return;

        foreach (Army army in ArmyManager.Instance.GetPlayerArmies())
        {
            SupplyRouteTracker.ArmyRouteState route = SupplyRouteTracker.Instance?.GetArmyState(army);
            if (route == null || route.CurrentProvince != capital) continue;

            PrepareArmy(army, capital);
            ArmySupplyAccount account = army.GetComponent<ArmySupplyAccount>();
            SupplyRouteTracker.Instance.ResetExpedition(army, capital);
            account?.RestoreFull();
            Log($"SUP REFILL army={army.name} stock={(account != null ? account.Available : 0f):0.#} route=clear", army);
        }
    }

    public void RestoreSupply(Army army, float amount, bool full = false)
    {
        ArmySupplyAccount account = army != null ? army.GetComponent<ArmySupplyAccount>() : null;
        if (account == null) return;
        if (full) account.RestoreFull(); else account.Restore(amount);
        Log($"SUP REFILL army={army.name} stock={account.Available:0.#}", army);
    }

    public void DumpArmyState(Army army)
    {
        ArmySupplyAccount account = army != null ? army.GetComponent<ArmySupplyAccount>() : null;
        SupplyRouteTracker.ArmyRouteState route = SupplyRouteTracker.Instance?.GetArmyState(army);
        if (army == null || account == null || route == null) return;
        Log($"SUP STATE army={army.name} city={Name(route.CurrentProvince)} stock={account.Available:0.#} pending={Mathf.Max(0, route.PendingRoute.Count - 1)} active={route.ActiveEdges.Count}", army);
    }

    private void LogBlocked(Army army, ProvinceModel from, ProvinceModel destination, float required, float available)
    {
        if (!diagnosticsEnabled) return;
        if (nextBlockLogTime.TryGetValue(army, out float nextAllowed) && Time.unscaledTime < nextAllowed) return;
        nextBlockLogTime[army] = Time.unscaledTime + BlockLogCooldown;
        Log($"SUP BLOCK army={army.name} from={Name(from)} to={Name(destination)} need={required:0.#} have={available:0.#}", army);
    }
    private int NextSequence(Army army)
    {
        movementSequences.TryGetValue(army, out int sequence);
        sequence++;
        movementSequences[army] = sequence;
        return sequence;
    }

    private static bool IsPlayerArmy(Army army, NationModel nation) => army != null && nation != null && army.IsPlayerArmy && (army.OwnerNation == null || army.OwnerNation == nation);
    private static string Name(ProvinceModel province) => province != null ? province.provinceName : "none";

    private void Log(string message, Object context)
    {
        if (diagnosticsEnabled) GameLog.Diagnostic(GameLogCategory.Supply, message, context);
    }
}
