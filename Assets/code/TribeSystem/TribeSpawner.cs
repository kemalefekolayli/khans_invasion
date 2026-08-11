using System.Collections.Generic;
using UnityEngine;

/// <summary>Creates neutral tribes using designer-tunable population and spawn settings.</summary>
public class TribeSpawner : MonoBehaviour
{
    private const string DefaultTribePrefabPath = "Tribes/TribePrefab";

    [Header("Spawn Settings")]
    [SerializeField, Min(0)] private int initialTribeCount = 5;
    [SerializeField] private bool spawnInitialTribesNearPlayer = true;
    [SerializeField, Min(0)] private int initialNearbyTribeCount = 3;
    [SerializeField, Range(0f, 1f)] private float spawnChancePerTurn = 0.2f;
    [SerializeField, Min(1)] private int maximumActiveTribes = 12;

    [Header("Tribe Population")]
    [SerializeField, Min(1f)] private float minimumPopulation = 40f;
    [SerializeField, Min(1f)] private float maximumPopulation = 100f;
    [SerializeField, Min(0.01f)] private float populationStandardDeviation = 14f;

    [Header("Visual")]
    [Tooltip("Optional override. If empty, Resources/Tribes/TribePrefab is used.")]
    [SerializeField] private GameObject tribePrefab;

    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (FindFirstObjectByType<TribeSpawner>() != null) return;
        new GameObject("TribeSpawner").AddComponent<TribeSpawner>();
    }

    private void Start() => TryInitialize();

    private void OnEnable()
    {
        GameEvents.OnProvincesAssigned += TryInitialize;
        GameEvents.OnMapLoaded += TryInitialize;
        GameEvents.OnPlayerNationReady += TryInitialize;
        GameEvents.OnTurnEnded += OnTurnEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= TryInitialize;
        GameEvents.OnMapLoaded -= TryInitialize;
        GameEvents.OnPlayerNationReady -= TryInitialize;
        GameEvents.OnTurnEnded -= OnTurnEnded;
    }

    private void TryInitialize()
    {
        if (initialized || PlayerNation.Instance?.currentNation == null || GetEligibleStates().Count == 0) return;

        initialized = true;
        for (int i = 0; i < initialTribeCount; i++)
        {
            List<StateModel> nearbyStates = spawnInitialTribesNearPlayer && i < initialNearbyTribeCount
                ? GetNearbyEligibleStates()
                : null;
            bool spawned = TrySpawnTribe(nearbyStates);
            if (!spawned) break;
            if (nearbyStates != null && nearbyStates.Count > 0)
                GameLog.Log(GameLogCategory.Core, $"[TribeSpawner] Initial tribe {i + 1} spawned using nearby player states.");
        }
    }

    private void OnTurnEnded(int turnNumber)
    {
        TryInitialize();
        if (!initialized || Random.value > spawnChancePerTurn) return;
        TrySpawnTribe();
    }

    private bool TrySpawnTribe(List<StateModel> preferredStates = null)
    {
        if (GetActiveTribeCount() >= maximumActiveTribes) return false;

        List<StateModel> eligibleStates = GetEligibleStates();
        eligibleStates.RemoveAll(HasActiveTribeInState);
        if (eligibleStates.Count == 0) return false;

        if (preferredStates != null && preferredStates.Count > 0)
        {
            eligibleStates.RemoveAll(state => !preferredStates.Contains(state));
            if (eligibleStates.Count == 0)
                return TrySpawnTribe();
        }

        StateModel state = eligibleStates[Random.Range(0, eligibleStates.Count)];
        List<ProvinceModel> provinces = GetEligibleProvinces(state);
        ProvinceModel province = provinces[Random.Range(0, provinces.Count)];

        float population = GetPopulationSample();
        GameObject prefab = tribePrefab != null ? tribePrefab : Resources.Load<GameObject>(DefaultTribePrefabPath);
        if (prefab == null)
        {
            GameLog.Warning(GameLogCategory.Core, $"[TribeSpawner] Missing Resources/{DefaultTribePrefabPath} prefab.");
            return false;
        }
        GameObject tribeObject = Instantiate(prefab, province.transform.position, Quaternion.identity, GetTribesContainer());
        TribeGroup tribe = tribeObject.GetComponent<TribeGroup>();
        if (tribe == null) tribe = tribeObject.AddComponent<TribeGroup>();
        if (tribeObject.GetComponent<TribeVisual>() == null) tribeObject.AddComponent<TribeVisual>();
        tribe.Initialize(state, province, population);
        tribeObject.name = $"Tribe of {(string.IsNullOrEmpty(state.stateName) ? "Wanderers" : state.stateName)}";
        GameLog.Log(GameLogCategory.Core, $"[TribeSpawner] Spawned {tribeObject.name} ({population:F0} population) in {province.provinceName}.");
        return true;
    }

    private static List<StateModel> GetNearbyEligibleStates()
    {
        List<StateModel> nearby = new List<StateModel>();
        NationModel playerNation = PlayerNation.Instance?.currentNation;
        if (playerNation?.provinceList == null) return nearby;

        foreach (ProvinceModel province in playerNation.provinceList)
        {
            if (province == null || province.neighbors == null) continue;
            foreach (ProvinceModel neighbor in province.neighbors)
            {
                StateModel state = neighbor?.provinceState;
                if (state == null || nearby.Contains(state) || GetEligibleProvinces(state).Count == 0 || HasActiveTribeInState(state)) continue;
                nearby.Add(state);
            }
        }
        return nearby;
    }

    private static Transform GetTribesContainer()
    {
        GameObject container = GameObject.Find("Tribes");
        if (container == null) container = new GameObject("Tribes");
        return container.transform;
    }
    private float GetPopulationSample()
    {
        float minimum = Mathf.Min(minimumPopulation, maximumPopulation);
        float maximum = Mathf.Max(minimumPopulation, maximumPopulation);
        float mean = (minimum + maximum) * 0.5f;
        float u1 = Mathf.Max(0.0001f, Random.value);
        float u2 = Random.value;
        float standardNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        return Mathf.Clamp(mean + standardNormal * populationStandardDeviation, minimum, maximum);
    }

    private static int GetActiveTribeCount() => FindObjectsByType<TribeGroup>(FindObjectsSortMode.None).Length;

    private static bool HasActiveTribeInState(StateModel state)
    {
        foreach (TribeGroup tribe in FindObjectsByType<TribeGroup>(FindObjectsSortMode.None))
        {
            if (tribe != null && tribe.HomeState == state) return true;
        }

        return false;
    }

    private static List<StateModel> GetEligibleStates()
    {
        List<StateModel> states = new List<StateModel>();
        foreach (StateModel state in FindObjectsByType<StateModel>(FindObjectsSortMode.None))
        {
            if (GetEligibleProvinces(state).Count > 0) states.Add(state);
        }

        return states;
    }

    private static List<ProvinceModel> GetEligibleProvinces(StateModel state)
    {
        List<ProvinceModel> provinces = new List<ProvinceModel>();
        if (state == null) return provinces;

        foreach (ProvinceModel province in state.provinceList)
        {
            if (province != null && !province.CompareTag("River")) provinces.Add(province);
        }

        return provinces;
    }
}
