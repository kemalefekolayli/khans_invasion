using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns route topology and nation knowledge. It does not calculate or spend supply.</summary>
public class SupplyRouteTracker : MonoBehaviour
{
    public static SupplyRouteTracker Instance { get; private set; }
    private readonly Dictionary<NationModel, NationRouteState> nationStates = new();
    private readonly Dictionary<Army, ArmyRouteState> armyStates = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<SupplyRouteTracker>() != null) return;
        GameObject host = new(nameof(SupplyRouteTracker));
        DontDestroyOnLoad(host);
        host.AddComponent<SupplyRouteTracker>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerNationReady += RegisterInitialCities;
        GameEvents.OnPlayerNationChanged += RegisterInitialCities;
    }

    private void Start() => RegisterInitialCities(PlayerNation.Instance?.currentNation);
    private void OnDisable()
    {
        GameEvents.OnPlayerNationReady -= RegisterInitialCities;
        GameEvents.OnPlayerNationChanged -= RegisterInitialCities;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public NationRouteState GetNationState(NationModel nation)
    {
        if (nation == null) return null;
        if (!nationStates.TryGetValue(nation, out NationRouteState state))
        {
            state = new NationRouteState();
            nationStates.Add(nation, state);
        }
        return state;
    }

    public ArmyRouteState GetArmyState(Army army)
    {
        if (army == null) return null;
        if (!armyStates.TryGetValue(army, out ArmyRouteState state))
        {
            state = new ArmyRouteState();
            armyStates.Add(army, state);
        }
        return state;
    }

    public void SeedArmy(Army army, ProvinceModel province)
    {
        if (army == null || province == null) return;
        ArmyRouteState state = GetArmyState(army);
        if (state.CurrentProvince != null) return;
        state.CurrentProvince = province;
        state.PendingRoute.Add(province);
    }

    public FreeTraversalResult TryRecordFreeTraversal(Army army, ProvinceModel destination, out RouteEdge edge)
    {
        edge = null;
        ArmyRouteState state = GetArmyState(army);
        if (state?.CurrentProvince == null || destination == null || state.CurrentProvince == destination) return FreeTraversalResult.None;
        ProvinceModel source = state.CurrentProvince;
        if (state.TryGetActiveEdge(source, destination, out edge))
        {
            edge.TraversalCount++;
            state.CurrentProvince = destination;
            ResetPending(state, destination);
            return FreeTraversalResult.ActivePass;
        }
        int last = state.PendingRoute.Count - 1;
        if (last > 0 && state.PendingRoute[last] == source && state.PendingRoute[last - 1] == destination)
        {
            edge = new RouteEdge(source, destination, SupplyFundingType.Stock);
            state.PendingRoute.RemoveAt(last);
            state.CurrentProvince = destination;
            return FreeTraversalResult.PendingPop;
        }
        return FreeTraversalResult.None;
    }

    public bool HasActiveEdge(Army army, ProvinceModel from, ProvinceModel to)
    {
        return army != null && armyStates.TryGetValue(army, out ArmyRouteState state) && state.TryGetActiveEdge(from, to, out _);
    }

    public bool IsImmediatePendingBacktrack(Army army, ProvinceModel from, ProvinceModel to)
    {
        if (army == null || !armyStates.TryGetValue(army, out ArmyRouteState state)) return false;
        int last = state.PendingRoute.Count - 1;
        return last > 0 && state.PendingRoute[last] == from && state.PendingRoute[last - 1] == to;
    }

    public bool IsKnownCityReadOnly(NationModel nation, ProvinceModel province)
    {
        return nation != null && province != null && nationStates.TryGetValue(nation, out NationRouteState state) && state.KnownCities.Contains(province);
    }
    public RouteEdge RecordPaidTransition(Army army, ProvinceModel destination, SupplyFundingType funding, out bool loopCollapsed)
    {
        loopCollapsed = false;
        ArmyRouteState state = GetArmyState(army);
        if (state?.CurrentProvince == null || destination == null || state.CurrentProvince == destination) return null;
        ProvinceModel source = state.CurrentProvince;
        EnsurePendingEndsAt(state, source);
        RouteEdge edge = new(source, destination, funding);
        int earlierIndex = state.PendingRoute.IndexOf(destination);
        if (earlierIndex >= 0)
        {
            state.PendingRoute.RemoveRange(earlierIndex + 1, state.PendingRoute.Count - earlierIndex - 1);
            loopCollapsed = true;
        }
        else state.PendingRoute.Add(destination);
        state.CurrentProvince = destination;
        return edge;
    }

    public int CommitPending(Army army, ProvinceModel operationProvince)
    {
        ArmyRouteState state = GetArmyState(army);
        if (state == null || operationProvince == null) return 0;
        EnsurePendingEndsAt(state, operationProvince);
        int added = 0;
        for (int i = 1; i < state.PendingRoute.Count; i++)
        {
            ProvinceModel first = state.PendingRoute[i - 1];
            ProvinceModel second = state.PendingRoute[i];
            if (state.TryGetActiveEdge(first, second, out _)) continue;
            state.ActiveEdges.Add(new RouteEdge(first, second, SupplyFundingType.Stock));
            added++;
        }
        ResetPending(state, operationProvince);
        state.CurrentProvince = operationProvince;
        return added;
    }

    public void SynchronizePosition(Army army, ProvinceModel province)
    {
        ArmyRouteState state = GetArmyState(army);
        if (state == null || province == null) return;
        state.CurrentProvince = province;
        ResetPending(state, province);
    }

    public void ResetExpedition(Army army, ProvinceModel capital)
    {
        if (army == null || capital == null) return;
        ArmyRouteState state = GetArmyState(army);
        state.ActiveEdges.Clear();
        state.CurrentProvince = capital;
        ResetPending(state, capital);
    }

    public bool IsKnownCity(NationModel nation, ProvinceModel province) => GetNationState(nation)?.KnownCities.Contains(province) == true;
    public void MarkKnownCity(NationModel nation, ProvinceModel province) { if (nation != null && province != null) GetNationState(nation).KnownCities.Add(province); }
    private void RegisterInitialCities() => RegisterInitialCities(PlayerNation.Instance?.currentNation);
    private void RegisterInitialCities(NationModel nation)
    {
        if (nation?.provinceList == null) return;
        NationRouteState state = GetNationState(nation);
        foreach (ProvinceModel province in nation.provinceList) if (IsCity(province)) state.KnownCities.Add(province);
    }

    public static bool IsCity(ProvinceModel province) => province != null && province.GetComponentInChildren<CityCenter>(true) != null;
    private static void EnsurePendingEndsAt(ArmyRouteState state, ProvinceModel province) { if (state.PendingRoute.Count == 0 || state.PendingRoute[^1] != province) state.PendingRoute.Add(province); }
    private static void ResetPending(ArmyRouteState state, ProvinceModel province) { state.PendingRoute.Clear(); state.PendingRoute.Add(province); }

    public sealed class NationRouteState { public readonly HashSet<ProvinceModel> KnownCities = new(); }
    public sealed class ArmyRouteState
    {
        public ProvinceModel CurrentProvince;
        public readonly List<ProvinceModel> PendingRoute = new();
        public readonly List<RouteEdge> ActiveEdges = new();
        public bool TryGetActiveEdge(ProvinceModel first, ProvinceModel second, out RouteEdge edge)
        {
            foreach (RouteEdge candidate in ActiveEdges) if (candidate.Matches(first, second)) { edge = candidate; return true; }
            edge = null;
            return false;
        }
    }
}

public enum FreeTraversalResult { None, ActivePass, PendingPop }
public enum SupplyFundingType { Stock, Cargo }
public sealed class RouteEdge
{
    public readonly ProvinceModel First;
    public readonly ProvinceModel Second;
    public readonly SupplyFundingType Funding;
    public int TraversalCount = 1;
    public RouteEdge(ProvinceModel first, ProvinceModel second, SupplyFundingType funding) { First = first; Second = second; Funding = funding; }
    public bool Matches(ProvinceModel first, ProvinceModel second) => (First == first && Second == second) || (First == second && Second == first);
}
