using System;
using UnityEngine;

[Serializable]
public sealed class AIDecisionTelemetryRecord
{
    public int schemaVersion = 1;
    public int turn;
    public long nationId;
    public string nationName;
    public int provinceCount;
    public float treasury;
    public float income;
    public float population;
    public float troops;
    public int armyCount;
    public int connectedEnemyCount;
    public string selectedState;
    public long targetNationId;
    public string targetNationName;
    public bool hasTargetNation;
    public int effectiveAggression;
    public float readinessRatio;
    public bool canScout;
    public bool canRaid;
    public bool canAttack;
    public bool canSiege;
    public string lastActionDescription;

    public string ToCompactJson()
    {
        return JsonUtility.ToJson(this, false);
    }
}
