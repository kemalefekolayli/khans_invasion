using UnityEngine;

public enum AIState
{
    Idle,
    Recruiting,
    Attacking,
    Developing,
    Fortifying
}

public class AIStateMachine
{
    public AIState CurrentState { get; private set; }

    private nationAgression aggression;
    private int baseAggressionLevel;
    private string nationName;

    public AIStateMachine(nationAgression aggression, string nationName)
    {
        this.aggression = aggression;
        this.baseAggressionLevel = ConvertLegacyAggression(aggression);
        this.nationName = nationName;
        CurrentState = AIState.Idle;
    }

    public AIStateMachine(int aggressionLevel, string nationName)
    {
        this.aggression = nationAgression.mediumAgression;
        this.baseAggressionLevel = Mathf.Clamp(aggressionLevel, 1, 6);
        this.nationName = nationName;
        CurrentState = AIState.Idle;
    }

    public void Evaluate(AINationData data)
    {
        Evaluate(data, null, null);
    }

    public void Evaluate(AINationData data, AIWarContext warContext, AISettings settings)
    {
        AIState previousState = CurrentState;

        int effectiveAggression = warContext != null ? warContext.EffectiveAggression : baseAggressionLevel;

        // Base personality weights
        float expandWeight = AIPersonality.GetExpandWeight(effectiveAggression); // expanding -> aggression desire
        float fortifyWeight = AIPersonality.GetFortifyWeight(effectiveAggression);
        float idleWeight = AIPersonality.GetIdleWeight(effectiveAggression);
        float developWeight = idleWeight * 0.8f; 

        // Situational modifiers
        bool isRich = data.gold > data.TotalIncome * 8f;
        bool isPoor = data.gold < data.TotalIncome * 2f;

        if (isPoor)
        {
            expandWeight *= 0.2f;
            fortifyWeight *= 0.5f;
            idleWeight *= 2.0f;
            developWeight *= 0.5f;
        }
        else if (isRich)
        {
            expandWeight *= 1.5f;
            developWeight *= 1.5f;
            fortifyWeight *= 1.2f;
            idleWeight *= 0.2f;
        }

        int provinceCount = data.nation.provinceList.Count;
        if (provinceCount <= 2)
        {
            developWeight *= 1.5f; 
            fortifyWeight *= 1.2f;
        }
        else if (provinceCount >= 6)
        {
            expandWeight *= 1.2f;
        }

        if (data.totalPopulation > 1000 && data.TotalIncome < 50) 
        {
            developWeight *= 2.0f; 
        }

        // --- Combat Logic Split ---
        // Instead of generic "Expanding", we decide between Recruiting and Attacking based on readiness.
        
        bool isReadyForWar = warContext != null
            ? (warContext.CanAttack || warContext.CanRaid)
            : data.totalTroops >= Mathf.Max(500f, data.totalMaxPopulation * 0.05f);
        bool canScout = warContext != null && warContext.CanScout;

        float militaryDesire = expandWeight; // Use expansion desire as base for military action

        // Calculate final weights for states
        float recruitingWeight = 0f;
        float attackingWeight = 0f;

        if (militaryDesire > 0)
        {
            if (isReadyForWar)
            {
                attackingWeight = militaryDesire * 1.2f; // Boost because we are ready
                recruitingWeight = militaryDesire * 0.2f; // Maintenance only
            }
            else if (canScout)
            {
                attackingWeight = militaryDesire * 0.45f;
                recruitingWeight = militaryDesire * 0.8f;
            }
            else
            {
                recruitingWeight = militaryDesire * 1.5f; // Must recruit first
                attackingWeight = 0f; // Not ready
            }
        }

        // Pick the state with highest weight
        float maxWeight = Mathf.Max(recruitingWeight, attackingWeight, fortifyWeight, idleWeight, developWeight);

        if (maxWeight == attackingWeight)
            CurrentState = AIState.Attacking;
        else if (maxWeight == recruitingWeight)
            CurrentState = AIState.Recruiting;
        else if (maxWeight == developWeight)
            CurrentState = AIState.Developing;
        else if (maxWeight == fortifyWeight)
            CurrentState = AIState.Fortifying;
        else
            CurrentState = AIState.Idle;

        if (CurrentState != previousState)
        {
            GameLog.Log(GameLogCategory.AIWar, $"[AI: {nationName}] State Change: {previousState} -> {CurrentState} " +
                      $"(Weights: Rec={recruitingWeight:F1}, Atk={attackingWeight:F1}, Dev={developWeight:F1}, Fort={fortifyWeight:F1}, Idle={idleWeight:F1})");
        }
    }

    private static int ConvertLegacyAggression(nationAgression aggression)
    {
        switch (aggression)
        {
            case nationAgression.heavyAgression: return 5;
            case nationAgression.mediumAgression: return 3;
            case nationAgression.lightAgression: return 2;
            default: return 3;
        }
    }
}
