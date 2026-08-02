using System.Collections.Generic;
using UnityEngine;

public static class AIAggressionEvaluator
{
    public static AIWarContext Evaluate(NationModel nation, AISettings settings, AIWorldIntelCache intel)
    {
        AISettings safeSettings = settings;
        AIWarContext context = new AIWarContext
        {
            Nation = nation,
            BaseAggression = ClampAggression(nation != null ? nation.baseAggressionLevel : 3, safeSettings),
            ConnectedEnemyNations = intel != null ? new List<NationModel>(intel.GetConnectedNeighborNations(nation)) : new List<NationModel>(),
            OwnBorderProvinces = intel != null ? new List<ProvinceModel>(intel.GetOwnBorderProvinces(nation)) : new List<ProvinceModel>(),
            EnemyBorderProvinces = intel != null ? new List<ProvinceModel>(intel.GetEnemyBorderProvinces(nation)) : new List<ProvinceModel>()
        };

        if (nation == null || intel == null)
        {
            context.EffectiveAggression = context.BaseAggression;
            return context;
        }

        context.OwnTroops = intel.GetTroops(nation);
        context.OwnStrength = intel.GetStrength(nation);
        context.OwnArmyCount = intel.GetArmyCount(nation);
        context.HasConnectedEnemyNeighbor = context.ConnectedEnemyNations.Count > 0;
        context.NeighborWeightedStrength = CalculateWeightedNeighborStrength(context.ConnectedEnemyNations, settings, intel);
        context.ReadinessRatio = context.NeighborWeightedStrength > 0f
            ? context.OwnStrength / context.NeighborWeightedStrength
            : (context.OwnStrength > 0f ? float.PositiveInfinity : 0f);

        context.MeetsMinimumAttackForce =
            context.OwnTroops >= GetMinTroops(settings)
            && context.OwnArmyCount >= GetMinArmies(settings);
        context.MeetsMinimumRaidForce =
            context.OwnTroops >= GetMinRaidTroops(settings)
            && context.OwnArmyCount >= GetMinArmies(settings);

        int dynamicAggression = context.BaseAggression;
        if (context.ReadinessRatio >= GetStrongRatio(settings))
            dynamicAggression += GetStrongBonus(settings);
        else if (context.ReadinessRatio > 0f && context.ReadinessRatio <= GetWeakRatio(settings))
            dynamicAggression -= GetWeakPenalty(settings);

        context.EffectiveAggression = ClampAggression(dynamicAggression, settings);
        nation.effectiveAggressionLevel = context.EffectiveAggression;

        context.CanScout = context.HasConnectedEnemyNeighbor
            && context.EffectiveAggression >= GetScoutLevel(settings);
        context.CanAttack = context.CanScout
            && context.MeetsMinimumAttackForce
            && context.EffectiveAggression >= GetAttackLevel(settings)
            && context.ReadinessRatio >= GetAttackRatio(settings);
        context.CanRaid = context.CanScout
            && context.MeetsMinimumRaidForce
            && context.EffectiveAggression >= GetRaidLevel(settings)
            && context.ReadinessRatio >= GetRaidRatio(settings);
        context.CanSiege = context.CanAttack
            && context.EffectiveAggression >= GetSiegeLevel(settings);
        context.PreferredAction = PickPreferredAction(context);

        return context;
    }

    private static float CalculateWeightedNeighborStrength(List<NationModel> neighbors, AISettings settings, AIWorldIntelCache intel)
    {
        if (neighbors == null || intel == null) return 0f;

        float total = 0f;
        foreach (NationModel neighbor in neighbors)
        {
            if (neighbor == null) continue;

            float weight = neighbor.isPlayer ? GetPlayerStrengthWeight(settings) : 1f;
            total += intel.GetStrength(neighbor) * weight;
        }

        return total;
    }

    private static AIWarAction PickPreferredAction(AIWarContext context)
    {
        if (context.CanSiege) return AIWarAction.SiegeProvince;
        if (context.CanRaid) return AIWarAction.RaidProvince;
        if (context.CanAttack) return AIWarAction.InvadeProvince;
        if (context.CanScout) return AIWarAction.ScoutBorder;
        return AIWarAction.None;
    }

    private static int ClampAggression(int value, AISettings settings)
    {
        int min = settings != null ? settings.AggressionMin : 1;
        int max = settings != null ? settings.AggressionMax : 6;
        return Mathf.Clamp(value, min, max);
    }

    private static float GetAttackRatio(AISettings settings) => settings != null ? settings.AttackReadinessRatio : 1.2f;
    private static float GetRaidRatio(AISettings settings) => settings != null ? settings.RaidReadinessRatio : 0.75f;
    private static float GetStrongRatio(AISettings settings) => settings != null ? settings.StrongReadinessRatio : 1.5f;
    private static float GetWeakRatio(AISettings settings) => settings != null ? settings.WeakReadinessRatio : 0.8f;
    private static float GetPlayerStrengthWeight(AISettings settings) => settings != null ? settings.PlayerNeighborStrengthWeight : 0.35f;
    private static float GetMinTroops(AISettings settings) => settings != null ? settings.MinTroopsBeforeAttack : 500f;
    private static float GetMinRaidTroops(AISettings settings) => settings != null ? settings.MinTroopsBeforeRaid : 100f;
    private static int GetMinArmies(AISettings settings) => settings != null ? settings.MinArmiesBeforeAttack : 1;
    private static int GetStrongBonus(AISettings settings) => settings != null ? settings.StrongAggressionBonus : 1;
    private static int GetWeakPenalty(AISettings settings) => settings != null ? settings.WeakAggressionPenalty : 1;
    private static int GetScoutLevel(AISettings settings) => settings != null ? settings.ScoutAggressionLevel : 2;
    private static int GetAttackLevel(AISettings settings) => settings != null ? settings.AttackAggressionLevel : 3;
    private static int GetRaidLevel(AISettings settings) => settings != null ? settings.RaidAggressionLevel : 3;
    private static int GetSiegeLevel(AISettings settings) => settings != null ? settings.SiegeAggressionLevel : 6;
}
