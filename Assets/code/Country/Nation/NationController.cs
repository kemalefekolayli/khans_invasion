using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NationController : MonoBehaviour
{
    private const string CapitalRegistryResourceName = "NationCapitalRegistry";
    private const string FortressBuilding = "Fortress";

    [Header("Capital Selection Weights")]
    [Min(0f)] [SerializeField] private float populationWeight = 0.05f;
    [Min(0f)] [SerializeField] private float buildingCountWeight = 25f;
    [Min(0f)] [SerializeField] private float ownedNeighborWeight = 20f;
    [Min(0f)] [SerializeField] private float connectedRegionWeight = 5f;

    private NationCapitalRegistry capitalRegistry;

    private void OnEnable()
    {
        GameEvents.OnProvinceDataLoaded += InitializeCapitals;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnProvinceDataLoaded -= InitializeCapitals;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
    }

    public void SetNationCapital(NationModel nation, ProvinceModel capitalProvince)
    {
        if (nation == null || capitalProvince == null || capitalProvince.provinceOwner != nation)
        {
            return;
        }

        nation.capitalProvince = capitalProvince;
        EnsureCapitalFortress(capitalProvince);

        if (nation.isPlayer)
        {
            GameEvents.PlayerCapitalSet(capitalProvince);
        }
    }

    private void InitializeCapitals()
    {
        NationLoader loader = FindFirstObjectByType<NationLoader>();
        if (loader == null) return;

        capitalRegistry = Resources.Load<NationCapitalRegistry>(CapitalRegistryResourceName);
        foreach (NationModel nation in loader.allNations)
        {
            if (nation == null || nation.provinceList == null || nation.provinceList.Count == 0) continue;

            ProvinceModel configuredCapital = FindConfiguredCapital(nation);
            SetNationCapital(nation, configuredCapital ?? SelectBestCapital(nation));
        }
    }

    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (oldOwner != null && oldOwner.capitalProvince == province)
        {
            SetNationCapital(oldOwner, SelectBestCapital(oldOwner));
        }

        if (newOwner != null && newOwner.capitalProvince == null)
        {
            SetNationCapital(newOwner, SelectBestCapital(newOwner));
        }
    }

    private ProvinceModel FindConfiguredCapital(NationModel nation)
    {
        if (capitalRegistry == null || !capitalRegistry.TryGetCapitalProvinceId(nation.nationId, out int provinceId))
        {
            return null;
        }

        return nation.provinceList.FirstOrDefault(province =>
            province != null
            && province.provinceId == provinceId
            && province.provinceOwner == nation);
    }

    private ProvinceModel SelectBestCapital(NationModel nation)
    {
        if (nation?.provinceList == null) return null;

        List<ProvinceModel> candidates = nation.provinceList
            .Where(province => province != null && province.provinceOwner == nation)
            .ToList();

        ProvinceModel bestCapital = null;
        float bestScore = float.MinValue;

        foreach (ProvinceModel candidate in candidates)
        {
            int ownedNeighborCount = candidate.neighbors?.Count(neighbor => neighbor != null && neighbor.provinceOwner == nation) ?? 0;
            int connectedRegionSize = GetConnectedRegionSize(candidate, nation);
            int buildingCount = candidate.buildings?.Count ?? 0;

            float score = candidate.provinceCurrentPop * populationWeight
                + buildingCount * buildingCountWeight
                + ownedNeighborCount * ownedNeighborWeight
                + connectedRegionSize * connectedRegionWeight;

            if (score > bestScore || (Mathf.Approximately(score, bestScore) && (bestCapital == null || candidate.provinceId < bestCapital.provinceId)))
            {
                bestScore = score;
                bestCapital = candidate;
            }
        }

        if (bestCapital != null)
        {
            GameLog.Log(GameLogCategory.Province,
                $"[NationController] Selected {bestCapital.provinceName} as {nation.nationName}'s capital (score {bestScore:F1}).");
        }

        return bestCapital;
    }

    private static int GetConnectedRegionSize(ProvinceModel start, NationModel nation)
    {
        if (start == null || nation == null) return 0;

        HashSet<ProvinceModel> visited = new HashSet<ProvinceModel> { start };
        Queue<ProvinceModel> frontier = new Queue<ProvinceModel>();
        frontier.Enqueue(start);

        while (frontier.Count > 0)
        {
            ProvinceModel current = frontier.Dequeue();
            if (current.neighbors == null) continue;

            foreach (ProvinceModel neighbor in current.neighbors)
            {
                if (neighbor == null || neighbor.provinceOwner != nation || !visited.Add(neighbor)) continue;
                frontier.Enqueue(neighbor);
            }
        }

        return visited.Count;
    }

    private static void EnsureCapitalFortress(ProvinceModel capital)
    {
        if (capital == null || capital.buildings.Contains(FortressBuilding)) return;

        capital.buildings.Add(FortressBuilding);
        GameEvents.BuildingConstructed(capital, FortressBuilding);
    }
}
