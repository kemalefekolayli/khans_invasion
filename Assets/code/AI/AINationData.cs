using UnityEngine;

[System.Serializable]
public class AINationData
{
    public NationModel nation;
    public float gold
    {
        get => nation?.treasury ?? 0f;
        set { if (nation != null) nation.treasury = value; }
    }
    public float totalTaxIncome;
    public float totalTradeIncome;
    public float totalPopulation;
    public float totalMaxPopulation;
    public float totalTroops;
    public int armyCount;


    public float TotalIncome => totalTaxIncome + totalTradeIncome;

    public AINationData(NationModel nation)
    {
        this.nation = nation;
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        totalTaxIncome = 0f;
        totalTradeIncome = 0f;
        totalPopulation = 0f;
        totalMaxPopulation = 0f;
        totalTroops = 0f;
        armyCount = 0;


        if (nation == null || nation.provinceList == null) return;

        foreach (ProvinceModel province in nation.provinceList)
        {
            if (province != null)
            {
                totalTaxIncome += province.provinceTaxIncome;
                totalTradeIncome += province.provinceTradePower;
                totalPopulation += province.provinceCurrentPop;
                totalMaxPopulation += province.provinceMaxPop;

            }
        }
    }

    public void CollectIncome()
    {
        float previousGold = gold;
        gold += TotalIncome;
        GameLog.Log(GameLogCategory.Economy, $"[AI: {nation.nationName}] Income: Tax={totalTaxIncome:F0}, Trade={totalTradeIncome:F0}, Gold: {previousGold:F0} -> {gold:F0}");
    }
}
