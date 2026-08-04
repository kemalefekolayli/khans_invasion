using UnityEngine;

[System.Serializable]
public sealed class SupplyCostSettings
{
    [Min(0f)] public float baseConstructionCost = 10f;
    [Min(0f)] public float friendlyMultiplier = 0.5f;
    [Min(0f)] public float foreignKnownMultiplier = 1f;
    [Min(0f)] public float foreignUnknownMultiplier = 1.25f;
    [Min(1f)] public float armySizeReference = 100f;
    [Min(0f)] public float armySizePenaltyPerReference = 0.2f;
}

public readonly struct SupplyMoveContext
{
    public readonly float ArmySize;
    public readonly bool IsFriendly;
    public readonly bool IsKnown;
    public SupplyMoveContext(float armySize, bool isFriendly, bool isKnown) { ArmySize = armySize; IsFriendly = isFriendly; IsKnown = isKnown; }
}

/// <summary>Pure supply construction-cost policy.</summary>
public sealed class SupplyCostCalculator
{
    private readonly SupplyCostSettings settings;
    public SupplyCostCalculator(SupplyCostSettings settings) => this.settings = settings;
    public float Calculate(in SupplyMoveContext context)
    {
        float territory = context.IsFriendly ? settings.friendlyMultiplier : context.IsKnown ? settings.foreignKnownMultiplier : settings.foreignUnknownMultiplier;
        float sizeSteps = Mathf.Max(0f, context.ArmySize / settings.armySizeReference - 1f);
        return Mathf.Max(0f, settings.baseConstructionCost * territory * (1f + sizeSteps * settings.armySizePenaltyPerReference));
    }
}
