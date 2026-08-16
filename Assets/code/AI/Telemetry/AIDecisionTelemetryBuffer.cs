using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class AIDecisionTelemetryBuffer
{
    private readonly Queue<AIDecisionTelemetryRecord> records = new Queue<AIDecisionTelemetryRecord>();

    public void Record(AINationController controller, int turn, AISettings settings)
    {
        if (settings == null || !settings.EnableDecisionTelemetry || controller?.Nation == null) return;

        AIDecisionTelemetryRecord record = Capture(controller, turn);
        int capacity = Mathf.Max(1, settings.DecisionTelemetryBufferCapacity);
        while (records.Count >= capacity)
            records.Dequeue();
        records.Enqueue(record);

        if (settings.EmitDecisionTelemetryToGameLog)
            GameLog.Diagnostic(GameLogCategory.AI, record.ToCompactJson());
    }

    public IReadOnlyList<AIDecisionTelemetryRecord> GetSnapshot()
    {
        return new List<AIDecisionTelemetryRecord>(records);
    }

    public string ToJsonLines()
    {
        StringBuilder builder = new StringBuilder();
        foreach (AIDecisionTelemetryRecord record in records)
        {
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(record.ToCompactJson());
        }
        return builder.ToString();
    }

    private static AIDecisionTelemetryRecord Capture(AINationController controller, int turn)
    {
        NationModel nation = controller.Nation;
        AIWarContext context = controller.LastWarContext;
        NationModel target = controller.TargetNation;

        float income = 0f;
        float population = 0f;
        int provinceCount = 0;
        if (nation.provinceList != null)
        {
            foreach (ProvinceModel province in nation.provinceList)
            {
                if (province == null || province.provinceOwner != nation) continue;
                provinceCount++;
                income += province.provinceTaxIncome + province.provinceTradePower;
                population += province.provinceCurrentPop;
            }
        }

        float troops = 0f;
        int armyCount = 0;
        if (ArmyManager.Instance != null)
        {
            foreach (Army army in ArmyManager.Instance.GetAllArmies())
            {
                if (army == null || army.OwnerNation != nation) continue;
                troops += army.ArmySize;
                armyCount++;
            }
        }

        return new AIDecisionTelemetryRecord
        {
            turn = turn,
            nationId = nation.nationId,
            nationName = nation.nationName,
            provinceCount = provinceCount,
            treasury = Finite(nation.treasury),
            income = Finite(income),
            population = Finite(population),
            troops = Finite(troops),
            armyCount = armyCount,
            connectedEnemyCount = context?.ConnectedEnemyNations?.Count ?? 0,
            selectedState = controller.StateMachine != null ? controller.StateMachine.CurrentState.ToString() : null,
            targetNationId = target != null ? target.nationId : 0,
            targetNationName = target != null ? target.nationName : null,
            hasTargetNation = target != null,
            effectiveAggression = context != null ? context.EffectiveAggression : nation.effectiveAggressionLevel,
            readinessRatio = Finite(context != null ? context.ReadinessRatio : 0f),
            canScout = context != null && context.CanScout,
            canRaid = context != null && context.CanRaid,
            canAttack = context != null && context.CanAttack,
            canSiege = context != null && context.CanSiege,
            lastActionDescription = controller.LastActionDescription
        };
    }

    private static float Finite(float value)
    {
        if (float.IsPositiveInfinity(value)) return float.MaxValue;
        if (float.IsNegativeInfinity(value)) return float.MinValue;
        return float.IsNaN(value) ? 0f : value;
    }
}
