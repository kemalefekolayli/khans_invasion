using UnityEngine;
using System.Collections.Generic;

public class AINationController
{
    public AINationData EconomyData { get; private set; }
    public AIStateMachine StateMachine { get; private set; }
    public NationModel Nation => EconomyData.nation;
    public string LastActionDescription { get; private set; } = "Initialized";

    public AINationController(NationModel nation)
    {
        EconomyData = new AINationData(nation);
        StateMachine = new AIStateMachine(nation.nationAgression, nation.nationName);
    }

    public void ProcessTurn(int turnNumber)
    {
        // 1. Recalculate stats from current provinces
        EconomyData.RecalculateStats();

        // 2. Collect income
        EconomyData.CollectIncome();

        // 3. Evaluate state machine
        StateMachine.Evaluate(EconomyData);

        // 4. Execute state actions (for now just log intent)
        ExecuteStateActions(turnNumber);
    }

    private void ExecuteStateActions(int turnNumber)
    {
        switch (StateMachine.CurrentState)
        {
            case AIState.Idle:
                // Just save money
                string msg = $"Turn {turnNumber}: Idle — saving gold ({EconomyData.gold:F0}g)";
                LastActionDescription = msg;
                Debug.Log($"[AI: {Nation.nationName}] {msg}");
                break;

            case AIState.Expanding:
                // Spend POPULATION to increase ARMY (Defense Force)
                ExecuteExpandAction(turnNumber);
                break;

            case AIState.Fortifying:
                // Spend GOLD to build BUILDINGS
                ExecuteFortifyAction(turnNumber);
                break;
        }
    }

    private void ExecuteFortifyAction(int turnNumber)
    {
        // Try to build something in a random province
        // 1. Filter provinces that can accept buildings
        // 2. Pick one, pick a valid building
        // 3. Check affordability

        if (Builder.Instance == null) return;

        List<ProvinceModel> validProvinces = new List<ProvinceModel>();
        foreach (var p in Nation.provinceList)
        {
            if (p != null) validProvinces.Add(p);
        }

        // Shuffle to avoid always building in the first one
        Shuffle(validProvinces);

        foreach (ProvinceModel province in validProvinces)
        {
            List<string> available = Builder.Instance.GetAvailableBuildings(province);
            if (available.Count == 0) continue;

            // Pick a building (naively random for now)
            string buildingToBuild = available[UnityEngine.Random.Range(0, available.Count)];
            float cost = Builder.Instance.GetBuildingCost(buildingToBuild);

            if (EconomyData.gold >= cost)
            {
                // Build it!
                float result = Builder.Instance.BuildBuilding(province, buildingToBuild, EconomyData.gold);
                
                if (result > 0)
                {
                    EconomyData.gold -= cost;
                    EconomyData.gold -= cost;
                    string msg = $"Turn {turnNumber}: Built {buildingToBuild} in {province.provinceName} (-{cost}g)";
                    LastActionDescription = msg;
                    Debug.Log($"[AI: {Nation.nationName}] {msg}");
                    return; // Done for this turn
                }
            }
        }

        // If we get here, we couldn't build anything (too poor or no slots)
        string failMsg = $"Turn {turnNumber}: Fortifying — couldn't build anything (Gold: {EconomyData.gold:F0}g)";
        LastActionDescription = failMsg;
        Debug.Log($"[AI: {Nation.nationName}] {failMsg}");
    }

    private void ExecuteExpandAction(int turnNumber)
    {
        // Recruit militia in a border province
        ProvinceModel target = FindExpansionTarget();
        
        if (target != null)
        {
            // Recruit 10% of population as militia
            float recruitAmount = target.provinceCurrentPop * 0.10f;
            
            // Cap at 200 to be safe
            if (recruitAmount > 200f) recruitAmount = 200f;

            if (recruitAmount >= 10f)
            {
                target.provinceCurrentPop -= recruitAmount;
                target.defenceForceSize += recruitAmount;
                
                target.defenceForceSize += recruitAmount;
                
                string msg = $"Turn {turnNumber}: Expanding — recruited {recruitAmount:F0} militia in {target.provinceName}";
                LastActionDescription = msg;
                Debug.Log($"[AI: {Nation.nationName}] {msg}");
            }
            else
            {
                string msg = $"Turn {turnNumber}: Expanding — target {target.provinceName} has too little pop ({target.provinceCurrentPop:F0})";
                LastActionDescription = msg;
                Debug.Log($"[AI: {Nation.nationName}] {msg}");
            }
        }
        else
        {
            // No border provinces found? Just pick a random one
            if (Nation.provinceList.Count > 0)
            {
                ProvinceModel random = Nation.provinceList[UnityEngine.Random.Range(0, Nation.provinceList.Count)];
                ProvinceModel random = Nation.provinceList[UnityEngine.Random.Range(0, Nation.provinceList.Count)];
                string msg = $"Turn {turnNumber}: Expanding — internal recruitment in {random.provinceName}";
                LastActionDescription = msg;
                Debug.Log($"[AI: {Nation.nationName}] {msg}");
            }
        }
    }

    private ProvinceModel FindExpansionTarget()
    {
        // For recruiting, we want to reinforce provinces that neighbor ENEMIES
        List<ProvinceModel> borderProvinces = new List<ProvinceModel>();

        foreach (ProvinceModel owned in Nation.provinceList)
        {
            if (owned == null) continue;
            
            bool isBorder = false;
            foreach (ProvinceModel neighbor in owned.neighbors)
            {
                if (neighbor != null && neighbor.provinceOwner != Nation && !neighbor.CompareTag("River"))
                {
                    isBorder = true;
                    break;
                }
            }
            
            if (isBorder) borderProvinces.Add(owned);
        }

        if (borderProvinces.Count > 0)
        {
            // Return random border province
            return borderProvinces[UnityEngine.Random.Range(0, borderProvinces.Count)];
        }

        return null;
    }

    // Helper to shuffle list
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
