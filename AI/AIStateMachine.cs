using UnityEngine;

public enum AIState
{
    Idle,
    Expanding,
    Fortifying
}

public class AIStateMachine
{
    public AIState CurrentState { get; private set; }

    private nationAgression aggression;
    private string nationName;

    public AIStateMachine(nationAgression aggression, string nationName)
    {
        this.aggression = aggression;
        this.nationName = nationName;
        CurrentState = AIState.Idle;
    }

    public void Evaluate(AINationData data)
    {
        AIState previousState = CurrentState;

        float expandWeight = AIPersonality.GetExpandWeight(aggression);
        float fortifyWeight = AIPersonality.GetFortifyWeight(aggression);
        float idleWeight = AIPersonality.GetIdleWeight(aggression);

        // Situational modifiers
        // Low gold makes expanding less attractive, idling more attractive
        if (data.gold < data.TotalIncome * 2f)
        {
            expandWeight *= 0.3f;
            idleWeight *= 1.5f;
        }
        // High gold makes expanding more attractive
        else if (data.gold > data.TotalIncome * 5f)
        {
            expandWeight *= 1.5f;
            idleWeight *= 0.5f;
        }

        // Few provinces = prefer fortifying what you have
        if (data.nation.provinceList.Count <= 2)
        {
            fortifyWeight *= 1.5f;
            expandWeight *= 0.5f;
        }
        // Many provinces = more likely to expand or idle
        else if (data.nation.provinceList.Count >= 6)
        {
            expandWeight *= 1.3f;
        }

        // Pick the state with highest weight
        if (expandWeight >= fortifyWeight && expandWeight >= idleWeight)
        {
            CurrentState = AIState.Expanding;
        }
        else if (fortifyWeight >= expandWeight && fortifyWeight >= idleWeight)
        {
            CurrentState = AIState.Fortifying;
        }
        else
        {
            CurrentState = AIState.Idle;
        }

        if (CurrentState != previousState)
        {
            Debug.Log($"[AI: {nationName}] State: {previousState} → {CurrentState}");
        }
    }
}
