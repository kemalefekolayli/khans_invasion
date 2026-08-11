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
    public void Configure(float maximum, float starting) { maximumSupply = Mathf.Max(1f, maximum); currentSupply = Mathf.Clamp(starting, 0f, maximumSupply); NotifyChanged(); }
    public void ReconfigureMaximumPreservingFill(float maximum)
    {
        float fill = maximumSupply > 0f ? currentSupply / maximumSupply : 0f;
        maximumSupply = Mathf.Max(1f, maximum);
        currentSupply = Mathf.Clamp(fill * maximumSupply, 0f, maximumSupply);
        NotifyChanged();
    }
    public void IncreaseMaximumPreservingFill(float maximum)
    {
        if (maximum <= maximumSupply + 0.001f) return;
        ReconfigureMaximumPreservingFill(maximum);
    }
    public void SetMaximumKeepingCurrent(float maximum)
    {
        maximumSupply = Mathf.Max(1f, maximum);
        NotifyChanged();
    }
    public bool TryFund(float amount) { amount = Mathf.Max(0f, amount); if (currentSupply + 0.001f < amount) return false; currentSupply -= amount; NotifyChanged(); return true; }
    public float SpendUpTo(float amount)
    {
        float spent = Mathf.Min(Mathf.Max(0f, amount), currentSupply);
        if (spent <= 0f) return 0f;
        currentSupply -= spent;
        NotifyChanged();
        return spent;
    }
    public void Restore(float amount)
    {
        if (currentSupply < maximumSupply)
            currentSupply = Mathf.Min(maximumSupply, currentSupply + Mathf.Max(0f, amount));
        NotifyChanged();
    }
    public void RestoreFull() { currentSupply = maximumSupply; NotifyChanged(); }
    public void Refund(float amount) { currentSupply += Mathf.Max(0f, amount); NotifyChanged(); }

    private void NotifyChanged() => GameEvents.ArmySupplyChanged(GetComponent<Army>());
}
