using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AINationController
{
    public AINationData EconomyData;
    public AIStateMachine StateMachine;
    public NationModel Nation;
    
    private AISettings settings;

    public string LastActionDescription { get; private set; } = "Initialized";
    public NationModel TargetNation { get; private set; }
    public AIWarContext LastWarContext { get; private set; }

    public AINationController(NationModel nation, AISettings settings)
    {
        this.Nation = nation;
        this.settings = settings;
        this.EconomyData = new AINationData(nation);
        this.StateMachine = new AIStateMachine(nation.baseAggressionLevel, nation.nationName);
    }

    public void ProcessTurn(int turnNumber)
    {
        ProcessTurn(turnNumber, null);
    }

    public void ProcessTurn(int turnNumber, AIWorldIntelCache worldIntel)
    {
        // 1. Recalculate stats from current provinces
        EconomyData.RecalculateStats();
        
        // 1b. Calculate Military Stats
        if (worldIntel != null)
        {
            EconomyData.totalTroops = worldIntel.GetTroops(Nation);
            EconomyData.armyCount = worldIntel.GetArmyCount(Nation);
        }
        else if (ArmyManager.Instance != null)
        {
            var armies = ArmyManager.Instance.GetAllArmies();
            foreach(var army in armies)
            {
                // Only count our own armies
                if (army != null && army.OwnerNation == Nation)
                {
                    EconomyData.totalTroops += army.ArmySize;
                    EconomyData.armyCount++;
                }
            }
        }

        // 2. Collect income
        EconomyData.CollectIncome();

        // 3. Evaluate war context and state machine
        LastWarContext = AIAggressionEvaluator.Evaluate(Nation, settings, worldIntel);
        LogWarContext(turnNumber, LastWarContext);
        StateMachine.Evaluate(EconomyData, LastWarContext, settings);

        // 4. Execute state actions (for now just log intent)
        ExecuteStateActions(turnNumber, LastWarContext, worldIntel);

        // 5. Peace-time repositioning only. Attacking issues its own border-crossing orders.
        if (StateMachine.CurrentState != AIState.Attacking)
        {
            ExecuteArmyRandomWalk();
        }
    }

    /// <summary>
    /// Moves every army owned by this nation to a random neighboring province that's
    /// also owned by this nation. Neighbors come from the geometric adjacency graph
    /// (ProvinceModel.neighbors), so an army can never cross into another nation's
    /// territory, and if this nation's land is split into disconnected clusters, an
    /// army simply has no valid same-nation neighbor to cross into and stays put.
    /// </summary>
    private void ExecuteArmyRandomWalk()
    {
        if (ArmyManager.Instance == null) return;

        int moved = 0;
        int maxMoves = settings != null ? Mathf.Max(0, settings.MaxArmiesMovedPerNationPerTurn) : 3;
        foreach (var army in ArmyManager.Instance.GetAllArmies())
        {
            if (moved >= maxMoves) break;
            if (army == null || army.OwnerNation != Nation) continue;
            if (army.HasGeneral) continue; // let the general's own movement drive it instead
            if (army.IsInBattle) continue;

            ProvinceModel current = army.CurrentProvince;
            if (current == null) continue; // position unknown - nothing to walk from

            var candidates = current.neighbors.Where(n => n != null && n.provinceOwner == Nation).ToList();
            if (candidates.Count == 0) continue;

            ProvinceModel target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            army.MoveToProvince(target);
            moved++;
        }
    }

    private void ExecuteStateActions(int turnNumber, AIWarContext warContext, AIWorldIntelCache worldIntel)
    {
        switch (StateMachine.CurrentState)
        {
            case AIState.Idle:
                TargetNation = null; // Clear target in peace
                // Just save money
                string msg = $"Turn {turnNumber}: Idle â€” saving gold ({EconomyData.gold:F0}g)";
                LastActionDescription = msg;
                GameLog.Log(GameLogCategory.AI, $"[AI: {Nation.nationName}] {msg}");
                break;

            case AIState.Recruiting:
                // Spend POPULATION/GOLD to INCREASE ARMY (Reinforce > Create)
                ExecuteRecruitingAction(turnNumber, isEmergency: false);
                break;

            case AIState.Attacking:
                // Identify Target -> Move Armies / Create Forward Bases
                ExecuteAttackingAction(turnNumber, warContext, worldIntel);
                break;

            case AIState.Developing:
                TargetNation = null; 
                // Spend GOLD to build ECONOMY (Farms, Trade, Housing) near Capital
                ExecuteDevelopingAction(turnNumber);
                break;

            case AIState.Fortifying:
                TargetNation = null;
                // Spend GOLD to build DEFENSE (Fortress) on important borders
                ExecuteFortifyAction(turnNumber);
                break;
        }
    }

    private void ExecuteDevelopingAction(int turnNumber)
    {
         if (Builder.Instance == null) return;

         // Logic: Build mainly Economy/Housing, prioritize provinces CLOSE to Capital
         BuildRoutine(turnNumber, "Developing", (prov, building) => 
         {
             return CalculateBuildingScore(prov, building, AIState.Developing);
         });
    }

    private void ExecuteFortifyAction(int turnNumber)
    {
        if (Builder.Instance == null) return;
        
        // Logic: Build mainly Defensive, prioritize HIGH VALUE provinces (Border or High Pop)
        BuildRoutine(turnNumber, "Fortifying", (prov, building) => 
        {
             return CalculateBuildingScore(prov, building, AIState.Fortifying);
        });
    }

    // Generic Build Routine to reduce duplication
    private void BuildRoutine(int turnNumber, string actionName, System.Func<ProvinceModel, string, float> scoreFunc)
    {
        int buildingsBuilt = 0;
        int maxBuilds = settings != null ? Mathf.Max(0, settings.MaxBuildingsPerNationPerTurn) : 2;
        float developmentReserve = settings != null ? Mathf.Max(0f, settings.DevelopmentGoldReserve) : 80f;
        
        while (EconomyData.gold > developmentReserve && buildingsBuilt < maxBuilds)
        {
            var best = EvaluateBuildingOptions(scoreFunc);
            
            if (best.province == null || best.score <= 0) break;

            float cost = Builder.Instance.GetBuildingCost(best.building);
            if (EconomyData.gold >= cost)
            {
                float result = Builder.Instance.BuildBuilding(best.province, best.building, EconomyData.gold);
                if (result >= 0)
                {
                    EconomyData.gold -= cost;
                    buildingsBuilt++;
                    string msg = $"Turn {turnNumber}: {actionName} â€” Built {best.building} in {best.province.provinceName} (Score: {best.score:F1})";
                    LastActionDescription = msg;
                    GameLog.Log(GameLogCategory.Province, $"[AI: {Nation.nationName}] {msg}");
                    continue;
                }
            }
            break;
        }
        
        if (buildingsBuilt == 0 && EconomyData.gold > developmentReserve + 150f)
        {
             // Log only if we have gold but found nothing
             //GameLog.Log(GameLogCategory.Core, $"[AI: {Nation.nationName}] {actionName} â€” nothing suitable to build.");
        }
    }

    private (ProvinceModel province, string building, float score) EvaluateBuildingOptions(System.Func<ProvinceModel, string, float> scoreFunc)
    {
        ProvinceModel bestProv = null;
        string bestBuild = "";
        float bestScore = -1f;

        foreach (var province in Nation.provinceList)
        {
            if (province == null) continue;

            List<string> available = Builder.Instance.GetAvailableBuildings(province);
            foreach (string building in available)
            {
                // Check affordability
                float cost = Builder.Instance.GetBuildingCost(building);
                if (EconomyData.gold < cost) continue;

                float score = scoreFunc(province, building);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestProv = province;
                    bestBuild = building;
                }
            }
        }
        return (bestProv, bestBuild, bestScore);
    }
    
    // Overload for backward compatibility / generic use
    private (ProvinceModel province, string building, float score) EvaluateBuildingOptions()
    {
        return EvaluateBuildingOptions((p, b) => CalculateBuildingScore(p, b, StateMachine.CurrentState));
    }

    private float CalculateBuildingScore(ProvinceModel province, string building, AIState contextState)
    {
        float score = 0f;

        // Settings
        float baseHousing = settings != null ? settings.BaseHousingScore : 10f;
        float baseBarracks = settings != null ? settings.BaseBarracksScore : 15f;
        float baseFortress = settings != null ? settings.BaseFortressScore : 5f;
        float baseFarm = settings != null ? settings.BaseFarmScore : 12f;
        float baseTrade = settings != null ? settings.BaseTradeScore : 14f;
        
        float distPenalty = settings != null ? settings.DistanceToCapitalPenalty : 2f;
        float importanceWeight = settings != null ? settings.ProvinceImportanceWeight : 0.5f;

        // 1. Contextual Bonuses
        
        // DEVELOPING: Penalize distance from Capital
        if (contextState == AIState.Developing)
        {
            if (Nation.capitalProvince != null)
            {
                float dist = Vector3.Distance(province.transform.position, Nation.capitalProvince.transform.position);
                // Assuming typical map distances 1 unit ~ 1 province width? tweak multiplier
                score -= dist * distPenalty; 
            }
        }
        
        // FORTIFYING: Bonus for "Important" provinces (High Pop/Gold)
        if (contextState == AIState.Fortifying)
        {
            float importance = (province.provinceCurrentPop * 0.01f) + (province.provinceTaxIncome * 2f);
            score += importance * importanceWeight;
        }

        // 2. Building Type Scoring
        switch (building)
        {
            case "Housing":
                score += baseHousing;
                if (province.provinceMaxPop > 0)
                {
                    float saturation = province.provinceCurrentPop / province.provinceMaxPop;
                    if (saturation >= (settings != null ? settings.PopulationSaturationThreshold : 0.9f))
                        score += 50f; // Critical need
                    else
                        score += saturation * 10f;
                }
                break;

            case "Farm":
            case "Farms": 
                score += baseFarm;
                // Farms good for developing economy
                if (contextState == AIState.Developing) score += 10f;
                break;

            case "Market":
            case "Trade":
                score += baseTrade;
                if (contextState == AIState.Developing) score += 10f;
                // Markets better in high pop
                score += (province.provinceCurrentPop / 500f) * 5f;
                break;

            case "Barracks":
                 // ... (Keep existing logic or simplify)
                 score += baseBarracks;
                 // See previous logic for "Desperate for barracks"
                 if (!HasBarracks(province) && province.provinceCurrentPop > 200)
                 {
                     bool noBarracks = !Nation.provinceList.Any(p => HasBarracks(p));
                     if (noBarracks)
                     {
                         float noArmyMultiplier = settings != null ? settings.NoArmyBarracksMultiplier : 5f;
                         score += 200f * noArmyMultiplier;
                     }
                 }
                 break;

            case "Fortress":
                score += baseFortress;
                if (contextState == AIState.Fortifying) score += 30f; // Main goal of fortifying
                if (province.buildings.Contains("Fortress")) score = -100f; // Unique?
                break;
        }

        // 3. Synergies
        score += province.buildings.Count * 2f; // Slight grouping bonus

        // 4. Random
        score += UnityEngine.Random.Range(0f, 3f);

        return score;
    }

    private void ExecuteRecruitingAction(int turnNumber, bool isEmergency)
    {
        // 1. Ensure we have recruitment infrastructure (Barracks)
        // If we have plenty of gold but no barracks, we really should build one.
        int barracksCount = 0;
        foreach(var p in Nation.provinceList) if(HasBarracks(p)) barracksCount++;

        bool allowFieldRecruitment = settings == null || settings.AllowAIFieldRecruitment;

        if (barracksCount == 0 && EconomyData.gold >= 200) // Assuming barracks cost ~200
        {
             // Force build a barracks
             if (Builder.Instance != null)
             {
                 var bestProv = Nation.provinceList.OrderByDescending(p => p.provinceCurrentPop).FirstOrDefault();
                 if (bestProv != null)
                 {
                     float result = Builder.Instance.BuildBuilding(bestProv, "Barracks", EconomyData.gold);
                     if (result >= 0)
                     {
                         EconomyData.gold -= result;
                         barracksCount++;
                         GameLog.Log(GameLogCategory.AI, $"[AI: {Nation.nationName}] Recruiting - Forced barracks construction in {bestProv.provinceName}");
                     }
                 }
             }
        }

        // 2. Mass Recruitment Loop
        // We want to spend a significant portion of gold on troops
        float budget = EconomyData.gold - (settings != null ? settings.RecruitmentGoldReserve : 50f);
        if (isEmergency) budget = EconomyData.gold; // Spend everything!

        int recruitsCount = 0;

        // Find all provinces with barracks, or use field recruitment while testing AI tempo.
        var recruitProvinces = Nation.provinceList
            .Where(p => p != null && p.provinceCurrentPop > 50 && (allowFieldRecruitment || HasBarracks(p)))
            .ToList();
        
        if (recruitProvinces.Count == 0 || budget < 10) 
        {
             if(budget > 200 && barracksCount > 0) 
                GameLog.Log(GameLogCategory.AI, $"[AI: {Nation.nationName}] Recruiting - Has gold but no pop/barracks ready.");
             return;
        }

        Shuffle(recruitProvinces);
        int activeArmyCount = ArmyManager.Instance != null
            ? ArmyManager.Instance.GetAllArmies().Count(a => a != null && a.OwnerNation == Nation)
            : 0;

        foreach (var prov in recruitProvinces)
        {
            if (budget < 10) break;

            // Recruit logic
            float recruitFraction = settings != null ? settings.AIRecruitPopulationFraction : 0.18f;
            float recruitCap = settings != null ? settings.AIRecruitMaxPerProvince : 350f;
            float amount = Mathf.Min(prov.provinceCurrentPop * recruitFraction, recruitCap);
            float cost = amount * 1f; // 1g per unit

            if (cost > budget)
            {
                amount = budget;
                cost = budget;
            }

            if (amount < 10) continue;

            if (ArmyManager.Instance == null || ArmyFactory.Instance == null) continue;

            float maxArmy = settings != null ? settings.MaxArmySize : 1000f;
            int maxArmies = settings != null ? Mathf.Max(1, settings.MaxArmiesPerNation) : 4;
            var allOwnedArmies = ArmyManager.Instance.GetAllArmies()
                .Where(a => a != null && a.OwnerNation == Nation)
                .ToList();
            var reinforceableArmies = allOwnedArmies
                .Where(a => a.ArmySize < maxArmy)
                .OrderBy(a => a.ArmySize)
                .ToList();

            Army receivingArmy = reinforceableArmies.FirstOrDefault();
            bool createsNewArmy = receivingArmy == null && activeArmyCount < maxArmies;
            if (receivingArmy == null && !createsNewArmy) continue;

            if (receivingArmy != null)
            {
                amount = Mathf.Min(amount, maxArmy - receivingArmy.ArmySize);
                cost = amount;
            }

            if (amount < 10f) continue;

            prov.provinceCurrentPop -= amount;
            EconomyData.gold -= cost;
            budget -= cost;
            recruitsCount += (int)amount;

            if (receivingArmy != null)
            {
                receivingArmy.AddSoldiers(amount);
            }
            else
            {
                Army newArmy = ArmyFactory.Instance.CreateArmy(prov.transform.position, amount, 1.0f, false);
                if (newArmy != null)
                {
                    newArmy.OwnerNation = Nation;
                    newArmy.CurrentProvince = prov;
                }
            }
        }

        if (recruitsCount > 0)
        {
            string msg = $"Turn {turnNumber}: Recruiting â€” Raised {recruitsCount} troops across empire.";
            LastActionDescription = msg;
            GameLog.Log(GameLogCategory.AI, $"[AI: {Nation.nationName}] {msg}");
        }
    }

    private void ExecuteAttackingAction(int turnNumber, AIWarContext warContext, AIWorldIntelCache worldIntel)
    {
        if (warContext == null || !warContext.HasConnectedEnemyNeighbor)
        {
            TargetNation = null;
            LastActionDescription = $"Turn {turnNumber}: Attacking - no connected land targets.";
            return;
        }

        if (TargetNation == null
            || TargetNation.provinceList.Count == 0
            || !warContext.ConnectedEnemyNations.Contains(TargetNation))
        {
            PickBestTarget(warContext, worldIntel);
        }

        if (TargetNation == null)
        {
            LastActionDescription = $"Turn {turnNumber}: Attacking - no valid connected targets.";
            return;
        }

        int movedArmies = MoveArmiesTowardTarget(warContext);
        string msg = $"Turn {turnNumber}: {warContext.PreferredAction} - Target: {TargetNation.nationName}.";
        LastActionDescription = $"{msg} Advanced {movedArmies} armies.";

        if (warContext.PreferredAction == AIWarAction.SiegeProvince)
        {
            LastActionDescription += " Siege intent prepared; execution waits for army-based manager API.";
        }

        GameLog.Log(GameLogCategory.AIWar, $"[AI: {Nation.nationName}] {LastActionDescription}");
    }

    private int MoveArmiesTowardTarget(AIWarContext warContext)
    {
        if (ArmyManager.Instance == null || TargetNation == null) return 0;

        int movedArmies = 0;
        int maxAttackOrders = settings != null ? Mathf.Max(0, settings.MaxAttackOrdersPerNationPerTurn) : 2;
        var ownedArmies = ArmyManager.Instance.GetAllArmies()
            .Where(a => a != null && a.OwnerNation == Nation && !a.HasGeneral && !a.IsInBattle && a.CurrentProvince != null)
            .OrderByDescending(a => a.ArmySize)
            .ToList();

        foreach (Army army in ownedArmies)
        {
            if (movedArmies >= maxAttackOrders) break;

            ProvinceModel target = PickAttackProvinceForArmy(army, warContext);
            if (target == null) continue;

            bool conquered = TryExecuteAIConquest(army, target, warContext);
            if (!conquered)
            {
                TryExecuteAIRaid(army, target, warContext);
            }
            army.MoveToProvince(target);
            movedArmies++;
        }

        return movedArmies;
    }

    private ProvinceModel PickAttackProvinceForArmy(Army army, AIWarContext warContext)
    {
        ProvinceModel current = army.CurrentProvince;
        if (current == null) return null;

        bool canCrossBorder = warContext != null && (warContext.CanAttack || warContext.CanRaid);
        ProvinceModel adjacentEnemy = current.neighbors
            .Where(p => p != null && p.provinceOwner == TargetNation)
            .OrderByDescending(GetProvinceAttackValue)
            .FirstOrDefault();

        if (adjacentEnemy != null && canCrossBorder)
        {
            return adjacentEnemy;
        }

        ProvinceModel targetCapital = TargetNation.capitalProvince;
        List<ProvinceModel> targetBorders = warContext != null ? warContext.EnemyBorderProvinces : null;
        return current.neighbors
            .Where(p => p != null && p.provinceOwner == Nation)
            .OrderByDescending(p => p.neighbors.Count(n => n != null && n.provinceOwner == TargetNation))
            .ThenBy(p => GetDistanceToTargetBorderSqr(p, targetBorders, targetCapital))
            .FirstOrDefault();
    }

    private void TryExecuteAIRaid(Army army, ProvinceModel target, AIWarContext warContext)
    {
        if (army == null || target == null || warContext == null) return;
        if (!warContext.CanRaid) return;
        if (settings != null && !settings.EnableAIRaids) return;
        if (target.provinceOwner == null || target.provinceOwner == Nation) return;
        if (RaidManager.Instance == null) return;

        float loot = RaidManager.Instance.ExecuteRaid(target, army);
        if (loot > 0f)
        {
            GameLog.Log(GameLogCategory.AIWar, $"[AI: {Nation.nationName}] Raided {target.provinceName} for {loot:F0} gold.");
        }
    }

    private bool TryExecuteAIConquest(Army army, ProvinceModel target, AIWarContext warContext)
    {
        if (army == null || target == null || warContext == null) return false;
        if (target.provinceOwner == null || target.provinceOwner == Nation) return false;
        if (AIManager.Instance == null) return false;
        if (target.buildings != null && target.buildings.Contains("Fortress"))
        {
            int siegeLevel = settings != null ? settings.SiegeAggressionLevel : 6;
            if (warContext.EffectiveAggression < siegeLevel) return false;
        }
        if (!AIManager.Instance.ShouldEscalateToConquest(Nation, target.provinceOwner, warContext)) return false;

        NationModel oldOwner = target.provinceOwner;
        TransferProvince(target, oldOwner, Nation);
        AIManager.Instance.ClearRaidPressure(Nation, oldOwner);

        GameLog.Log(GameLogCategory.AIWar, $"[AI Conquest] {Nation.nationName} took {target.provinceName} from {oldOwner.nationName} after sustained raids.");
        return true;
    }

    private void TransferProvince(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        if (province == null || newOwner == null) return;

        if (oldOwner != null && oldOwner.provinceList != null)
        {
            oldOwner.provinceList.Remove(province);
        }

        if (newOwner.provinceList != null && !newOwner.provinceList.Contains(province))
        {
            newOwner.provinceList.Add(province);
        }

        province.provinceOwner = newOwner;
        province.SetNationColor(NationLoader.HexToColor(newOwner.nationColor));

        GameEvents.ProvinceConquered(province, oldOwner, newOwner);
        GameEvents.ProvinceOwnerChanged(province, oldOwner, newOwner);

        // Fire nation destroyed event if the old owner no longer has any provinces
        if (oldOwner != null && oldOwner.provinceList != null && oldOwner.provinceList.Count == 0)
        {
            GameEvents.NationDestroyed(oldOwner);
        }
    }

    private void PickBestTarget(AIWarContext warContext, AIWorldIntelCache worldIntel)
    {
        if (warContext == null || warContext.ConnectedEnemyNations.Count == 0) return;

        NationModel bestTarget = null;
        float highestScore = -1000f;

        foreach (NationModel candidate in warContext.ConnectedEnemyNations)
        {
            if (candidate == null) continue;

            float enemyStrength = worldIntel != null ? worldIntel.GetStrength(candidate) : 0f;
            float myStrength = Mathf.Max(1f, warContext.OwnStrength);
            float score = 0f;

            float weaknessWeight = settings != null ? settings.EnemyWeaknessTargetWeight : 50f;
            if (enemyStrength < myStrength * 0.5f) score += weaknessWeight;
            else if (enemyStrength > myStrength) score -= weaknessWeight;

            float richnessWeight = settings != null ? settings.EnemyRichnessTargetWeight : 1f;
            float enemyPop = candidate.provinceList.Sum(p => p != null ? p.provinceCurrentPop : 0f);
            score += (enemyPop / 1000f) * richnessWeight;

            if (candidate.provinceList.Count > Nation.provinceList.Count * 2)
                score -= 30f;

            if (candidate.isPlayer)
            {
                float playerMultiplier = settings != null ? settings.PlayerTargetScoreMultiplier : 0.35f;
                score *= playerMultiplier;
            }

            if (score > highestScore)
            {
                highestScore = score;
                bestTarget = candidate;
            }
        }

        TargetNation = bestTarget;
        if (TargetNation != null)
        {
            GameLog.Log(GameLogCategory.AIWar, $"[AI: {Nation.nationName}] Selected connected target: {TargetNation.nationName} (Score: {highestScore:F1})");
        }
    }

    private float GetDistanceToTargetBorderSqr(ProvinceModel province, List<ProvinceModel> targetBorders, ProvinceModel fallbackTarget)
    {
        if (province == null) return float.MaxValue;

        float best = float.MaxValue;
        if (targetBorders != null)
        {
            foreach (ProvinceModel border in targetBorders)
            {
                if (border == null || border.provinceOwner != TargetNation) continue;

                float distance = (province.transform.position - border.transform.position).sqrMagnitude;
                if (distance < best) best = distance;
            }
        }

        if (best < float.MaxValue) return best;
        return fallbackTarget != null
            ? (province.transform.position - fallbackTarget.transform.position).sqrMagnitude
            : 0f;
    }

    private void LogWarContext(int turnNumber, AIWarContext context)
    {
        if (context == null) return;
        if (settings != null && !settings.LogWarIntent) return;

        GameLog.Log(
            GameLogCategory.AIWar,
            $"[AI: {Nation.nationName}] WarIntent T{turnNumber}: " +
            $"Agg={context.BaseAggression}->{context.EffectiveAggression}, " +
            $"Ratio={context.ReadinessRatio:F2}, Action={context.PreferredAction}, " +
            $"Neighbors={context.ConnectedEnemyNations.Count}");
    }

    private void ExecuteAttackingAction(int turnNumber)
    {
        // 1. Pick Target if none
        if (TargetNation == null || TargetNation.provinceList.Count == 0) // Invalid target
        {
            PickBestTarget();
        }

        if (TargetNation == null)
        {
             LastActionDescription = $"Turn {turnNumber}: Attacking â€” No valid targets found. Peace?";
             return;
        }

        int movedArmies = MoveArmiesTowardTarget();

        // 2. Execute Attack Logic
        // For now, we simulate "planning" by logging and maybe moving troops if we had that API exposed.
        // We can also spawn "Invasion Forces" near the border if we have money.
        
        string msg = $"Turn {turnNumber}: Attacking â€” Target: {TargetNation.nationName}. Marshalling forces.";
        LastActionDescription = $"{msg} Advanced {movedArmies} armies.";
        GameLog.Log(GameLogCategory.AI, $"[AI: {Nation.nationName}] {LastActionDescription}");
        
        // The actual army movement happens above; battle starts when opposing armies overlap.
    }

    private int MoveArmiesTowardTarget()
    {
        if (ArmyManager.Instance == null || TargetNation == null) return 0;

        int movedArmies = 0;
        var ownedArmies = ArmyManager.Instance.GetAllArmies()
            .Where(a => a != null && a.OwnerNation == Nation && !a.HasGeneral && !a.IsInBattle && a.CurrentProvince != null)
            .OrderByDescending(a => a.ArmySize)
            .ToList();

        foreach (Army army in ownedArmies)
        {
            ProvinceModel target = PickAttackProvinceForArmy(army);
            if (target == null) continue;

            army.MoveToProvince(target);
            movedArmies++;
        }

        return movedArmies;
    }

    private ProvinceModel PickAttackProvinceForArmy(Army army)
    {
        ProvinceModel current = army.CurrentProvince;
        if (current == null) return null;

        var adjacentEnemy = current.neighbors
            .Where(p => p != null && p.provinceOwner == TargetNation)
            .OrderByDescending(GetProvinceAttackValue)
            .FirstOrDefault();

        if (adjacentEnemy != null)
        {
            return adjacentEnemy;
        }

        ProvinceModel targetCapital = TargetNation.capitalProvince;
        return current.neighbors
            .Where(p => p != null && p.provinceOwner == Nation)
            .OrderByDescending(p => p.neighbors.Count(n => n != null && n.provinceOwner == TargetNation))
            .ThenBy(p => targetCapital != null ? Vector3.Distance(p.transform.position, targetCapital.transform.position) : 0f)
            .FirstOrDefault();
    }

    private float GetProvinceAttackValue(ProvinceModel province)
    {
        if (province == null) return 0f;

        float value = province.provinceCurrentPop * 0.01f + province.provinceTaxIncome + province.provinceTradePower;
        if (province == TargetNation.capitalProvince)
        {
            value += settings != null ? settings.CapitalProvinceAttackBonus : 100f;
        }

        return value;
    }

    private void PickBestTarget()
    {
        // Find neighbors
        HashSet<NationModel> neighbors = new HashSet<NationModel>();
        
        foreach (var p in Nation.provinceList)
        {
            if (p == null) continue;
            foreach (var n in p.neighbors)
            {
                if (n != null && n.provinceOwner != null && n.provinceOwner != Nation)
                {
                    neighbors.Add(n.provinceOwner);
                }
            }
        }

        if (neighbors.Count == 0) return;

        NationModel bestTarget = null;
        float highestScore = -1000f;

        foreach (var n in neighbors)
        {
            // Score neighbors. 
            // We want weak neighbors.
            // We want rich neighbors.
            
            // Get their estimated strength (if we cheat and peek, or use known info)
            // Let's peek for now.
            float enemyTroops = 0f;
            if (ArmyManager.Instance != null)
            {
                enemyTroops = ArmyManager.Instance.GetAllArmies()
                    .Where(a => a.OwnerNation == n).Sum(a => a.ArmySize);
            }

            float myTroops = EconomyData.totalTroops;
            
            float score = 0f;

            // 1. Weakness Score
            if (enemyTroops < myTroops * 0.5f) score += 50f; // Very weak
            else if (enemyTroops > myTroops) score -= 50f; // Too strong

            // 2. Richness
            float enemyPop = n.provinceList.Sum(p => p.provinceCurrentPop);
            score += enemyPop / 1000f;

            // 3. Logic: Don't attack if they are huge
            if (n.provinceList.Count > Nation.provinceList.Count * 2) score -= 30f;

            if (score > highestScore)
            {
                highestScore = score;
                bestTarget = n;
            }
        }

        TargetNation = bestTarget;
        if (TargetNation != null)
        {
            GameLog.Log(GameLogCategory.AI, $"[AI: {Nation.nationName}] Selected new target: {TargetNation.nationName} (Score: {highestScore:F1})");
        }
    }

    private bool HasBarracks(ProvinceModel p)
    {
        if (p.buildings == null) return false;
        return p.buildings.Contains("Barracks");
    }

    // Helper to shuffle list

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
