using UnityEngine;

public class PlayerNation : MonoBehaviour
{
    [Header("Nation Info")]
    public long nationId;
    public string nationName;
    public string nationColor;
    
    [Header("Economy")]
    public float nationMoney;
    public float taxIncome;
    public float tradeIncome;
    
    [Header("Military")]
    public float armySize;
    public float armyStrength;
    
    [Header("Population")]
    public float populationSize;
    public int cityCount;
    
    [Header("Game State")]
    public int currentTurn = 1;

    // Singleton for easy access
    public static PlayerNation Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Call this to initialize from a NationModel
    public void InitializeFromNation(NationModel nation)
    {
        nationId = nation.nationId;
        nationName = nation.nationName;
        nationColor = nation.nationColor;
        
        // Calculate initial values from provinces
        RecalculateStats();
    }

    // Recalculate all stats from owned provinces
    public void RecalculateStats()
    {
        // Find all provinces owned by player
        ProvinceModel[] allProvinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        
        taxIncome = 0f;
        tradeIncome = 0f;
        populationSize = 0f;
        cityCount = 0;
        
        foreach (ProvinceModel province in allProvinces)
        {
            if (province.provinceOwner != null && province.provinceOwner.nationId == nationId)
            {
                taxIncome += province.provinceTaxIncome;
                tradeIncome += province.provinceTradePower;
                populationSize += province.provinceCurrentPop;
                cityCount++;
            }
        }
    }

    // Called at end of each turn
    public void EndTurn()
    {
        // Add income to money
        nationMoney += taxIncome + tradeIncome;
        currentTurn++;
        
        // Recalculate stats
        RecalculateStats();
    }
}