using UnityEngine;

public interface IArmySupplyFundingSource
{
    SupplyFundingType FundingType { get; }
    float Available { get; }
    bool TryFund(float amount);
}

/// <summary>Stores only an army's supply balance.</summary>
public class ArmySupplyAccount : MonoBehaviour, IArmySupplyFundingSource
{
    [Header("Supply Stock")]
    [SerializeField, Min(1f)] private float maximumSupply = 100f;
    [SerializeField, Min(0f)] private float currentSupply = 100f;
    public SupplyFundingType FundingType => SupplyFundingType.Stock;
    public float Available => currentSupply;
    public float MaximumSupply => maximumSupply;
    public void Configure(float maximum, float starting) { maximumSupply = Mathf.Max(1f, maximum); currentSupply = Mathf.Clamp(starting, 0f, maximumSupply); }
    public bool TryFund(float amount) { amount = Mathf.Max(0f, amount); if (currentSupply + 0.001f < amount) return false; currentSupply -= amount; return true; }
    public void Restore(float amount) => currentSupply = Mathf.Min(maximumSupply, currentSupply + Mathf.Max(0f, amount));
    public void RestoreFull() => currentSupply = maximumSupply;
}
