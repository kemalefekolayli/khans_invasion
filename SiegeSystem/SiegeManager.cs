using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for the siege system.
/// Handles defense calculations, siege attempts, casualties, and pending province conquests.
/// Fortress sieges take multiple turns; normal cities are conquered after 1 turn.
/// </summary>
public class SiegeManager : MonoBehaviour, ITurnProcessor
{
    public static SiegeManager Instance { get; private set; }
    
    [Header("Defense Calculation Settings")]
    [Tooltip("Multiplier for tax income in defense calculation")]
    [SerializeField] private float taxDefenseMultiplier = 4f;
    
    [Tooltip("Divider for population in defense calculation")]
    [SerializeField] private float populationDivider = 5f;
    
    [Tooltip("Flat bonus for having a fortress")]
    [SerializeField] private float fortressDefenseBonus = 100f;
    
    [Tooltip("Multiplier applied AFTER fortress bonus")]
    [SerializeField] private float fortressDefenseMultiplier = 1.2f;
    
    [Header("Siege Requirements")]
    [Tooltip("Minimum troops required to siege a city with a fortress")]
    [SerializeField] private float fortressMinTroops = 600f;
    
    [Tooltip("Number of turns required to siege a fortress")]
    [SerializeField] private int fortressSiegeTurns = 3;
    
    [Header("Casualty Settings - Successful Siege")]
    [Tooltip("Base casualty percentage for any successful siege (minimum cost)")]
    [SerializeField] private float successBaseCasualty = 0.03f; // 3%
    
    [Tooltip("How much defense ratio affects casualties (higher = more punishing)")]
    [SerializeField] private float successCasualtyScaling = 0.12f; // 12% at ratio 1.0
    
    [Tooltip("Maximum casualty percentage for successful siege")]
    [SerializeField] private float successMaxCasualty = 0.15f; // 15%
    
    [Header("Casualty Settings - Failed Siege")]
    [Tooltip("Base casualty percentage for failed siege")]
    [SerializeField] private float failedBaseCasualty = 0.50f; // 50%
    
    [Tooltip("How quickly failed siege casualties scale with ratio")]
    [SerializeField] private float failedCasualtyScaling = 0.60f; // 60%
    
    [Tooltip("Maximum casualty for failed siege")]
    [SerializeField] private float failedMaxCasualty = 0.80f; // 80%
    
    [Header("Fortress Per-Turn Casualties")]
    [Tooltip("Per-turn casualty divisor for fortress sieges (total / this = per turn)")]
    [SerializeField] private float fortressCasualtyDivisor = 3f;
    
    [Header("Debug")]
    [SerializeField] private bool logSiegeEvents = true;
    
    // Active sieges (province -> siege state)
    private Dictionary<ProvinceModel, SiegeState> activeSieges = new Dictionary<ProvinceModel, SiegeState>();
    
    // Siege state class
    private class SiegeState
    {
        public ProvinceModel province;
        public int turnsRemaining;
        public NationModel originalOwner;
        public bool isFortress;
        public HashSet<General> participatingGenerals = new HashSet<General>();
    }
    
    public int ProcessingPriority => 50; // Process sieges after income but before other things
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✓ SiegeManager initialized");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Register with TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterTurnProcessor(this);
        }
    }
    
    private void OnEnable()
    {
        TurnManager.OnPlayerTurnStart += OnPlayerTurnStart;
    }
    
    private void OnDisable()
    {
        TurnManager.OnPlayerTurnStart -= OnPlayerTurnStart;
    }
    
    #region Defense Calculation
    
    /// <summary>
    /// Calculate the defense strength of a province.
    /// Formula: (TaxIncome * 4 + Population / 5)
    /// If Fortress: (Base + 100) * 1.2
    /// </summary>
    public float CalculateDefenseStrength(ProvinceModel province)
    {
        if (province == null) return 0f;
        
        // Base defense from tax income and population
        float baseDefense = (province.provinceTaxIncome * taxDefenseMultiplier) + 
                           (province.provinceCurrentPop / populationDivider);
        
        // Check if province has a fortress
        bool hasFortress = province.buildings.Contains("Fortress");
        
        if (hasFortress)
        {
            // Add flat bonus, then apply multiplier
            float defenseWithFortress = (baseDefense + fortressDefenseBonus) * fortressDefenseMultiplier;
            return defenseWithFortress;
        }
        
        return baseDefense;
    }
    
    /// <summary>
    /// Check if a province has a fortress.
    /// </summary>
    public bool HasFortress(ProvinceModel province)
    {
        if (province == null) return false;
        return province.buildings.Contains("Fortress");
    }
    
    #endregion
    
    #region Casualty Calculation
    
    /// <summary>
    /// Calculate casualty percentage for an attack.
    /// </summary>
    /// <param name="armySize">Size of attacking army</param>
    /// <param name="defenseStrength">Province defense strength</param>
    /// <param name="isForPerTurn">If true, divides for per-turn fortress casualties</param>
    /// <param name="isSuccessful">Whether the siege will succeed</param>
    /// <returns>Casualty percentage (0.0 to 1.0)</returns>
    public float CalculateCasualtyPercent(float armySize, float defenseStrength, bool isForPerTurn, bool isSuccessful)
    {
        if (armySize <= 0) return 0f;
        
        float defenseRatio = defenseStrength / armySize;
        float casualtyPercent;
        
        if (isSuccessful)
        {
            // Successful siege: base + scaled by ratio
            casualtyPercent = successBaseCasualty + (defenseRatio * successCasualtyScaling);
            casualtyPercent = Mathf.Min(casualtyPercent, successMaxCasualty);
        }
        else
        {
            // Failed siege: devastating
            float excessRatio = defenseRatio - 1f; // How much stronger defense is
            casualtyPercent = failedBaseCasualty + (excessRatio * failedCasualtyScaling);
            casualtyPercent = Mathf.Min(casualtyPercent, failedMaxCasualty);
        }
        
        // For fortress per-turn casualties, divide by number of turns
        if (isForPerTurn)
        {
            casualtyPercent /= fortressCasualtyDivisor;
        }
        
        return Mathf.Max(0f, casualtyPercent);
    }
    
    /// <summary>
    /// Apply casualties to an army.
    /// </summary>
    private int ApplyCasualties(Army army, float casualtyPercent)
    {
        if (army == null) return 0;
        
        int currentSize = (int)army.ArmySize;
        int casualties = Mathf.RoundToInt(currentSize * casualtyPercent);
        
        // Always lose at least 1 if there are casualties
        if (casualtyPercent > 0 && casualties == 0 && currentSize > 1)
        {
            casualties = 1;
        }
        
        // Apply casualties
        int newSize = Mathf.Max(0, currentSize - casualties);
        army.SetArmySize(newSize);
        
        return casualties;
    }
    
    #endregion
    
    #region Siege Execution
    
    /// <summary>
    /// Result of a siege attempt.
    /// </summary>
    public enum SiegeResult
    {
        Success,
        NotEnoughTroops,      // Need 600+ for fortress
        DefenseTooStrong,     // Army size < defense
        AlreadySiegedThisTurn,
        NoArmy,
        InvalidProvince
    }
    
    /// <summary>
    /// Check if a province can be sieged by the given general.
    /// Returns the reason if it cannot be sieged.
    /// </summary>
    public SiegeResult CanSiegeProvince(ProvinceModel province, General attacker)
    {
        if (province == null)
            return SiegeResult.InvalidProvince;
            
        if (attacker == null || !attacker.HasArmy)
            return SiegeResult.NoArmy;
        
        // Check if already under siege
        if (activeSieges.ContainsKey(province))
            return SiegeResult.AlreadySiegedThisTurn;
        
        float armySize = attacker.CommandedArmy.ArmySize;
        
        // Check fortress minimum troop requirement
        if (HasFortress(province) && armySize < fortressMinTroops)
            return SiegeResult.NotEnoughTroops;
        
        // Check if army can beat defense
        float defenseStrength = CalculateDefenseStrength(province);
        if (armySize <= defenseStrength)
            return SiegeResult.DefenseTooStrong;
        
        return SiegeResult.Success;
    }
    
    /// <summary>
    /// Execute a siege on a province.
    /// For normal cities: marked for conquest next turn with immediate casualties.
    /// For fortress cities: starts multi-turn siege.
    /// </summary>
    public SiegeResult ExecuteSiege(ProvinceModel province, General attacker)
    {
        SiegeResult canSiege = CanSiegeProvince(province, attacker);
        
        if (canSiege != SiegeResult.Success)
        {
            if (canSiege == SiegeResult.DefenseTooStrong || canSiege == SiegeResult.NotEnoughTroops)
            {
                // Apply failed siege casualties
                ApplyFailedSiegeCasualties(province, attacker);
            }
            
            if (logSiegeEvents)
            {
                Debug.Log($"[SiegeManager] Siege FAILED on {province?.provinceName}: {canSiege}");
            }
            
            // Fire failed siege event
            GameEvents.SiegeFailed(province, attacker, canSiege);
            return canSiege;
        }
        
        // Siege can proceed
        Army army = attacker.CommandedArmy;
        float armySize = army.ArmySize;
        float defenseStrength = CalculateDefenseStrength(province);
        bool hasFortress = HasFortress(province);
        
        // Create siege state
        SiegeState state = new SiegeState
        {
            province = province,
            turnsRemaining = hasFortress ? fortressSiegeTurns : 1,
            originalOwner = province.provinceOwner,
            isFortress = hasFortress
        };
        state.participatingGenerals.Add(attacker);
        
        activeSieges[province] = state;
        
        // Apply initial casualties
        float casualtyPercent = CalculateCasualtyPercent(armySize, defenseStrength, hasFortress, true);
        int casualties = ApplyCasualties(army, casualtyPercent);
        
        if (logSiegeEvents)
        {
            Debug.Log($"[SiegeManager] ═══ SIEGE STARTED ═══");
            Debug.Log($"  Attacker: {attacker.GeneralName} ({armySize:F0} troops)");
            Debug.Log($"  Province: {province.provinceName}");
            Debug.Log($"  Defense: {defenseStrength:F0}");
            Debug.Log($"  Fortress: {hasFortress}");
            Debug.Log($"  Turns Required: {state.turnsRemaining}");
            Debug.Log($"  Initial Casualties: {casualties} ({casualtyPercent:P1})");
            Debug.Log($"  Army After: {army.ArmySize:F0}");
        }
        
        // Fire siege started event
        GameEvents.ProvinceSieged(province, attacker, defenseStrength);
        
        return SiegeResult.Success;
    }
    
    /// <summary>
    /// Apply devastating casualties for a failed siege attempt.
    /// </summary>
    private void ApplyFailedSiegeCasualties(ProvinceModel province, General attacker)
    {
        if (attacker == null || !attacker.HasArmy) return;
        
        Army army = attacker.CommandedArmy;
        float armySize = army.ArmySize;
        float defenseStrength = CalculateDefenseStrength(province);
        
        float casualtyPercent = CalculateCasualtyPercent(armySize, defenseStrength, false, false);
        int casualties = ApplyCasualties(army, casualtyPercent);
        
        if (logSiegeEvents)
        {
            Debug.Log($"[SiegeManager] ═══ SIEGE FAILED - DEVASTATING LOSSES ═══");
            Debug.Log($"  Attacker: {attacker.GeneralName}");
            Debug.Log($"  Province Defense: {defenseStrength:F0}");
            Debug.Log($"  Casualties: {casualties} ({casualtyPercent:P1})");
            Debug.Log($"  Army Remaining: {army.ArmySize:F0}");
        }
    }
    
    /// <summary>
    /// Get a human-readable failure message for a siege result.
    /// </summary>
    public string GetSiegeFailureMessage(SiegeResult result, ProvinceModel province)
    {
        switch (result)
        {
            case SiegeResult.NotEnoughTroops:
                return $"Need at least {fortressMinTroops:F0} troops to siege a fortress!";
            case SiegeResult.DefenseTooStrong:
                float defense = province != null ? CalculateDefenseStrength(province) : 0;
                return $"Not strong enough! Province defense: {defense:F0}";
            case SiegeResult.AlreadySiegedThisTurn:
                return "This province is already under siege!";
            case SiegeResult.NoArmy:
                return "Need an army to siege!";
            case SiegeResult.InvalidProvince:
                return "Invalid province!";
            default:
                return "Siege failed!";
        }
    }
    
    #endregion
    
    #region Turn Processing
    
    /// <summary>
    /// Called at end of player turn - process ongoing sieges.
    /// </summary>
    public void ProcessTurnEnd(int turnNumber)
    {
        if (activeSieges.Count == 0) return;
        
        List<ProvinceModel> completedSieges = new List<ProvinceModel>();
        List<ProvinceModel> cancelledSieges = new List<ProvinceModel>();
        
        foreach (var kvp in activeSieges)
        {
            ProvinceModel province = kvp.Key;
            SiegeState state = kvp.Value;
            
            // Find armies currently on this province
            List<General> presentGenerals = FindGeneralsOnProvince(province);
            
            // For FORTRESS sieges: army must stay the whole time
            // For normal cities: army can leave after starting siege
            if (state.isFortress && presentGenerals.Count == 0)
            {
                // No army present on fortress siege - CANCELLED
                cancelledSieges.Add(province);
                if (logSiegeEvents)
                {
                    Debug.Log($"[SiegeManager] Siege of {province.provinceName} CANCELLED - army abandoned fortress siege!");
                }
                continue;
            }
            
            // Apply per-turn casualties to present armies (only for fortress sieges with turns remaining)
            if (state.isFortress && state.turnsRemaining > 1 && presentGenerals.Count > 0)
            {
                float defenseStrength = CalculateDefenseStrength(province);
                
                foreach (General general in presentGenerals)
                {
                    if (general.HasArmy)
                    {
                        Army army = general.CommandedArmy;
                        float casualtyPercent = CalculateCasualtyPercent(army.ArmySize, defenseStrength, true, true);
                        int casualties = ApplyCasualties(army, casualtyPercent);
                        
                        if (logSiegeEvents && casualties > 0)
                        {
                            Debug.Log($"[SiegeManager] Turn {turnNumber} siege casualties: {general.GeneralName} lost {casualties} ({casualtyPercent:P1})");
                        }
                        
                        // Fire casualties event
                        if (casualties > 0)
                        {
                            GameEvents.SiegeCasualties(province, general, casualties, state.turnsRemaining - 1);
                        }
                    }
                    
                    // Add to participating generals
                    state.participatingGenerals.Add(general);
                }
            }
            
            // Decrement turns
            state.turnsRemaining--;
            
            if (state.turnsRemaining <= 0)
            {
                // Siege complete!
                completedSieges.Add(province);
            }
            else if (logSiegeEvents)
            {
                Debug.Log($"[SiegeManager] Siege of {province.provinceName}: {state.turnsRemaining} turns remaining");
            }
        }
        
        // Remove cancelled sieges and fire events
        foreach (var province in cancelledSieges)
        {
            activeSieges.Remove(province);
            GameEvents.SiegeCancelled(province);
        }
        
        // Don't complete sieges here - do it at turn START
    }
    
    /// <summary>
    /// Called at the start of player turn.
    /// Converts completed sieges to player ownership.
    /// </summary>
    private void OnPlayerTurnStart()
    {
        List<ProvinceModel> toConquer = new List<ProvinceModel>();
        
        foreach (var kvp in activeSieges)
        {
            if (kvp.Value.turnsRemaining <= 0)
            {
                toConquer.Add(kvp.Key);
            }
        }
        
        if (toConquer.Count == 0) return;
        
        if (logSiegeEvents)
        {
            Debug.Log($"[SiegeManager] Processing {toConquer.Count} completed sieges...");
        }
        
        PlayerNation player = PlayerNation.Instance;
        if (player == null || player.currentNation == null)
        {
            Debug.LogError("[SiegeManager] PlayerNation not found! Cannot process conquests.");
            return;
        }
        
        foreach (ProvinceModel province in toConquer)
        {
            SiegeState state = activeSieges[province];
            
            // For FORTRESS sieges: verify army is still on province
            // For normal cities: army presence not required (they can leave after starting)
            if (state.isFortress)
            {
                List<General> presentGenerals = FindGeneralsOnProvince(province);
                if (presentGenerals.Count > 0)
                {
                    ConquerProvince(province, player.currentNation);
                }
                else
                {
                    // This shouldn't happen (should have been cancelled in ProcessTurnEnd)
                    // but handle it just in case
                    if (logSiegeEvents)
                    {
                        Debug.Log($"[SiegeManager] Fortress siege of {province.provinceName} failed - no army at completion!");
                    }
                    GameEvents.SiegeCancelled(province);
                }
            }
            else
            {
                // Non-fortress: conquer regardless of army position
                ConquerProvince(province, player.currentNation);
            }
            
            activeSieges.Remove(province);
        }
        
        // Recalculate player stats
        player.RecalculateStats();
        GameEvents.PlayerStatsChanged();
    }
    
    /// <summary>
    /// Find all player generals currently on a province.
    /// </summary>
    private List<General> FindGeneralsOnProvince(ProvinceModel province)
    {
        List<General> result = new List<General>();
        
        if (province == null) return result;
        
        // Get province bounds
        Collider2D provinceCollider = province.GetComponent<Collider2D>();
        if (provinceCollider == null) return result;
        
        // Find all generals
        General[] allGenerals = FindObjectsByType<General>(FindObjectsSortMode.None);
        
        foreach (General general in allGenerals)
        {
            if (general == null) continue;
            
            // Check if general is within province bounds
            if (provinceCollider.OverlapPoint(general.transform.position))
            {
                // Check if this is a player general
                if (general.OwnerNation == PlayerNation.Instance?.currentNation)
                {
                    result.Add(general);
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Transfer ownership of a province to a new nation.
    /// </summary>
    private void ConquerProvince(ProvinceModel province, NationModel newOwner)
    {
        if (province == null || newOwner == null) return;
        
        NationModel oldOwner = activeSieges.ContainsKey(province) 
            ? activeSieges[province].originalOwner 
            : province.provinceOwner;
        
        // Remove from old owner's list
        if (oldOwner != null && oldOwner.provinceList.Contains(province))
        {
            oldOwner.provinceList.Remove(province);
            
            if (logSiegeEvents)
            {
                Debug.Log($"[SiegeManager] Removed {province.provinceName} from {oldOwner.nationName}");
            }
        }
        
        // Add to new owner's list
        if (!newOwner.provinceList.Contains(province))
        {
            newOwner.provinceList.Add(province);
        }
        
        // Update province owner reference
        province.provinceOwner = newOwner;
        
        // Update province color to new nation's color
        Color newColor = NationLoader.HexToColor(newOwner.nationColor);
        province.SetNationColor(newColor);
        
        if (logSiegeEvents)
        {
            Debug.Log($"[SiegeManager] ✓ {province.provinceName} conquered by {newOwner.nationName}!");
        }
        
        // Fire conquest complete event
        GameEvents.ProvinceConquered(province, oldOwner, newOwner);
        
        // Also fire the existing ProvinceOwnerChanged event for compatibility
        GameEvents.ProvinceOwnerChanged(province, oldOwner, newOwner);
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Check if a province is currently under siege.
    /// </summary>
    public bool IsUnderSiege(ProvinceModel province)
    {
        return activeSieges.ContainsKey(province);
    }
    
    /// <summary>
    /// Get remaining turns for a siege.
    /// </summary>
    public int GetSiegeTurnsRemaining(ProvinceModel province)
    {
        if (activeSieges.TryGetValue(province, out SiegeState state))
        {
            return state.turnsRemaining;
        }
        return 0;
    }
    
    /// <summary>
    /// Get list of all provinces under siege.
    /// </summary>
    public List<ProvinceModel> GetActiveSieges()
    {
        return new List<ProvinceModel>(activeSieges.Keys);
    }
    
    #endregion
}
