using UnityEngine;

[System.Serializable]
public class AINationData
{
    public NationModel nation;
    public float gold;
    public float totalTaxIncome;
    public float totalTradeIncome;
    public float totalPopulation;
    public float totalDefenseForce;

    public float TotalIncome => totalTaxIncome + totalTradeIncome;

    public AINationData(NationModel nation)
    {
        this.nation = nation;
        this.gold = 0f;
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        totalTaxIncome = 0f;
        totalTradeIncome = 0f;
        totalPopulation = 0f;
        totalDefenseForce = 0f;

        if (nation == null || nation.provinceList == null) return;

        foreach (ProvinceModel province in nation.provinceList)
        {
            if (province != null)
            {
                totalTaxIncome += province.provinceTaxIncome;
                totalTradeIncome += province.provinceTradePower;
                totalPopulation += province.provinceCurrentPop;
                totalDefenseForce += province.defenceForceSize;
            }
        }
    }

    public void CollectIncome()
    {
        float previousGold = gold;
        gold += TotalIncome;
        Debug.Log($"[AI: {nation.nationName}] Income: Tax={totalTaxIncome:F0}, Trade={totalTradeIncome:F0}, Gold: {previousGold:F0} → {gold:F0}");
    }
}
