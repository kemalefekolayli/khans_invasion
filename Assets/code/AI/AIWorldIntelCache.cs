using System.Collections.Generic;
using UnityEngine;

public class AIWorldIntelCache
{
    private readonly Dictionary<NationModel, List<NationModel>> connectedNeighborNations = new Dictionary<NationModel, List<NationModel>>();
    private readonly Dictionary<NationModel, List<ProvinceModel>> ownBorderProvinces = new Dictionary<NationModel, List<ProvinceModel>>();
    private readonly Dictionary<NationModel, List<ProvinceModel>> enemyBorderProvinces = new Dictionary<NationModel, List<ProvinceModel>>();
    private readonly Dictionary<NationModel, float> nationTroops = new Dictionary<NationModel, float>();
    private readonly Dictionary<NationModel, float> nationStrength = new Dictionary<NationModel, float>();
    private readonly Dictionary<NationModel, int> nationArmyCount = new Dictionary<NationModel, int>();

    private bool borderDirty = true;
    private bool militaryDirty = true;

    public void MarkBordersDirty()
    {
        borderDirty = true;
    }

    public void MarkMilitaryDirty()
    {
        militaryDirty = true;
    }

    public void RebuildIfNeeded(IReadOnlyList<NationModel> nations)
    {
        if (borderDirty)
        {
            RebuildBorderIntel(nations);
            borderDirty = false;
        }

        if (militaryDirty)
        {
            RebuildMilitaryIntel();
            militaryDirty = false;
        }
    }

    public List<NationModel> GetConnectedNeighborNations(NationModel nation)
    {
        if (nation == null) return EmptyNationList();
        return connectedNeighborNations.TryGetValue(nation, out var result) ? result : EmptyNationList();
    }

    public List<ProvinceModel> GetOwnBorderProvinces(NationModel nation)
    {
        if (nation == null) return EmptyProvinceList();
        return ownBorderProvinces.TryGetValue(nation, out var result) ? result : EmptyProvinceList();
    }

    public List<ProvinceModel> GetEnemyBorderProvinces(NationModel nation)
    {
        if (nation == null) return EmptyProvinceList();
        return enemyBorderProvinces.TryGetValue(nation, out var result) ? result : EmptyProvinceList();
    }

    public float GetTroops(NationModel nation)
    {
        return nation != null && nationTroops.TryGetValue(nation, out float result) ? result : 0f;
    }

    public float GetStrength(NationModel nation)
    {
        return nation != null && nationStrength.TryGetValue(nation, out float result) ? result : 0f;
    }

    public int GetArmyCount(NationModel nation)
    {
        return nation != null && nationArmyCount.TryGetValue(nation, out int result) ? result : 0;
    }

    private void RebuildBorderIntel(IReadOnlyList<NationModel> nations)
    {
        connectedNeighborNations.Clear();
        ownBorderProvinces.Clear();
        enemyBorderProvinces.Clear();

        if (nations == null) return;

        foreach (NationModel nation in nations)
        {
            if (nation == null) continue;

            HashSet<NationModel> neighborSet = new HashSet<NationModel>();
            HashSet<ProvinceModel> ownBorderSet = new HashSet<ProvinceModel>();
            HashSet<ProvinceModel> enemyBorderSet = new HashSet<ProvinceModel>();

            foreach (ProvinceModel province in nation.provinceList)
            {
                if (province == null || province.neighbors == null) continue;

                foreach (ProvinceModel neighbor in province.neighbors)
                {
                    if (neighbor == null || neighbor.provinceOwner == null || neighbor.provinceOwner == nation)
                        continue;

                    neighborSet.Add(neighbor.provinceOwner);
                    ownBorderSet.Add(province);
                    enemyBorderSet.Add(neighbor);
                }
            }

            connectedNeighborNations[nation] = new List<NationModel>(neighborSet);
            ownBorderProvinces[nation] = new List<ProvinceModel>(ownBorderSet);
            enemyBorderProvinces[nation] = new List<ProvinceModel>(enemyBorderSet);
        }
    }

    private void RebuildMilitaryIntel()
    {
        nationTroops.Clear();
        nationStrength.Clear();
        nationArmyCount.Clear();

        if (ArmyManager.Instance == null) return;

        foreach (Army army in ArmyManager.Instance.GetAllArmies())
        {
            if (army == null || army.OwnerNation == null) continue;

            NationModel nation = army.OwnerNation;

            if (!nationTroops.ContainsKey(nation))
            {
                nationTroops[nation] = 0f;
                nationStrength[nation] = 0f;
                nationArmyCount[nation] = 0;
            }

            nationTroops[nation] += army.ArmySize;
            nationStrength[nation] += army.GetEffectiveStrength();
            nationArmyCount[nation]++;
        }
    }

    private static List<NationModel> EmptyNationList()
    {
        return new List<NationModel>(0);
    }

    private static List<ProvinceModel> EmptyProvinceList()
    {
        return new List<ProvinceModel>(0);
    }
}
