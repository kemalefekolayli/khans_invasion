using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Button that recruits troops for the selected general.
/// - If the general has no army: Creates a new army
/// - If the general has an army with room: Adds troops to existing army
/// - If overflow: First fills existing idle armies, then creates new if needed
/// </summary>
public class GetArmyButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button getArmyButton;
    
    [Header("Troop Settings")]
    [SerializeField] private float troopsToRecruit = 100f;
    [SerializeField] private float startingArmyQuality = 1.0f;
    
    [Header("New Army Defaults")]
    [Tooltip("Default max size for newly created armies. Can be modified by upgrades/buffs.")]
    [SerializeField] private float defaultMaxArmySize = 1000f;
    
    [Header("Debug")]
    [SerializeField] private bool logActions = true;

    private void Awake()
    {
        if (getArmyButton != null)
        {
            getArmyButton.onClick.AddListener(OnGetArmyButtonClicked);
        }
    }

    public void OnGetArmyButtonClicked()
    {
        // Get the currently selected general
        SelectableGeneral selected = GeneralSelectionManager.Instance?.SelectedGeneral;
        if (selected == null)
        {
            Debug.LogWarning("[GetArmyButton] No general selected! Select a general first.");
            return;
        }
        
        // Get the General component from the selected
        General general = selected.GetComponent<General>();
        if (general == null)
        {
            Debug.LogError($"[GetArmyButton] {selected.DisplayName} has no General component!");
            return;
        }
        
        // Check if already has an army
        if (general.HasArmy)
        {
            // Try to add troops to existing army
            AddTroopsToExistingArmy(general, selected);
        }
        else
        {
            // Create a new army for this general
            CreateNewArmyForGeneral(general, selected);
        }
    }

    /// <summary>
    /// Add troops to the general's existing army.
    /// If overflow occurs, distribute to idle armies first.
    /// </summary>
    private void AddTroopsToExistingArmy(General general, SelectableGeneral selectable)
    {
        Army existingArmy = general.CommandedArmy;
        ArmyData data = existingArmy.Data;
        
        float currentSize = data.size;
        float maxSize = data.maxSize;
        float availableSpace = maxSize - currentSize;
        
        if (logActions)
        {
            Debug.Log($"[GetArmyButton] {selectable.DisplayName}'s army: {currentSize}/{maxSize} (Space: {availableSpace})");
        }
        
        if (availableSpace >= troopsToRecruit)
        {
            // Enough room - just add to existing army
            existingArmy.AddSoldiers(troopsToRecruit);
            
            if (logActions)
            {
                Debug.Log($"✓ [GetArmyButton] Added {troopsToRecruit} troops to {selectable.DisplayName}'s army. New size: {existingArmy.ArmySize}");
            }
        }
        else if (availableSpace > 0)
        {
            // Partial room - fill up and distribute overflow
            float overflow = troopsToRecruit - availableSpace;
            
            // Fill existing army to max
            existingArmy.AddSoldiers(availableSpace);
            
            if (logActions)
            {
                Debug.Log($"[GetArmyButton] {selectable.DisplayName}'s army maxed at {maxSize}. Distributing {overflow} overflow troops...");
            }
            
            // Distribute overflow to idle armies or create new
            DistributeTroopsToIdleArmies(selectable, overflow);
        }
        else
        {
            // Army is at max capacity - distribute to idle armies or create new
            if (logActions)
            {
                Debug.Log($"[GetArmyButton] {selectable.DisplayName}'s army is FULL. Distributing {troopsToRecruit} troops to reserves...");
            }
            
            DistributeTroopsToIdleArmies(selectable, troopsToRecruit);
        }
    }

    /// <summary>
    /// Create a brand new army assigned to the general.
    /// </summary>
    private void CreateNewArmyForGeneral(General general, SelectableGeneral selectable)
    {
        ArmyFactory factory = GetFactory();
        if (factory == null) return;
        
        // Create army data - use configurable max size
        ArmyData armyData = new ArmyData(troopsToRecruit, startingArmyQuality, true);
        armyData.armyName = $"{selectable.DisplayName}'s Army";
        armyData.maxSize = defaultMaxArmySize;
        
        // Create army and assign to general
        Army army = factory.CreateArmyForGeneral(general, armyData);
        
        if (army != null)
        {
            if (logActions)
            {
                Debug.Log($"✓ [GetArmyButton] Created new army for {selectable.DisplayName} (Size: {troopsToRecruit}, Max: {defaultMaxArmySize})");
            }
            GameEvents.ArmySpawned(army, general);
        }
        else
        {
            Debug.LogError("[GetArmyButton] Failed to create army!");
        }
    }

    /// <summary>
    /// Distribute troops to existing idle armies first, then create new armies only if needed.
    /// This prevents spam of many small armies by consolidating into fewer, larger ones.
    /// </summary>
    private void DistributeTroopsToIdleArmies(SelectableGeneral selectable, float troopCount)
    {
        if (ArmyManager.Instance == null)
        {
            Debug.LogWarning("[GetArmyButton] ArmyManager not found! Creating new army directly.");
            CreateStandaloneArmy(selectable, troopCount);
            return;
        }
        
        float remainingTroops = troopCount;
        
        // Step 1: Try to fill existing idle armies that have space
        List<Army> idleArmiesWithSpace = ArmyManager.Instance.GetIdleArmiesWithSpace();
        
        foreach (Army idleArmy in idleArmiesWithSpace)
        {
            if (remainingTroops <= 0) break;
            
            float availableSpace = idleArmy.Data.maxSize - idleArmy.ArmySize;
            float toAdd = Mathf.Min(remainingTroops, availableSpace);
            
            if (toAdd > 0)
            {
                idleArmy.AddSoldiers(toAdd);
                remainingTroops -= toAdd;
                
                if (logActions)
                {
                    Debug.Log($"  → Added {toAdd} troops to existing reserve army. Now: {idleArmy.ArmySize}/{idleArmy.Data.maxSize}");
                }
            }
        }
        
        // Step 2: If there are still remaining troops, create new army/armies
        if (remainingTroops > 0)
        {
            // Create as few armies as possible by making them at max capacity
            while (remainingTroops > 0)
            {
                float armySize = Mathf.Min(remainingTroops, defaultMaxArmySize);
                CreateStandaloneArmy(selectable, armySize);
                remainingTroops -= armySize;
            }
        }
        else
        {
            if (logActions)
            {
                Debug.Log($"✓ [GetArmyButton] All troops consolidated into existing armies!");
            }
        }
    }

    /// <summary>
    /// Create a standalone army near the general (no general assigned).
    /// This army can later be assigned to another general or merged.
    /// </summary>
    private void CreateStandaloneArmy(SelectableGeneral selectable, float size)
    {
        ArmyFactory factory = GetFactory();
        if (factory == null) return;
        
        // Count existing idle armies for offset positioning
        int idleCount = ArmyManager.Instance?.GetIdlePlayerArmies().Count ?? 0;
        
        // Spawn position offset from the general - stagger multiple armies
        float offsetX = 0.5f + (idleCount * 0.3f);
        float offsetY = -0.5f - (idleCount * 0.2f);
        Vector3 spawnPos = selectable.transform.position + new Vector3(offsetX, offsetY, 0);
        
        // Create army data (no general, standalone) with configurable max size
        ArmyData armyData = new ArmyData(size, startingArmyQuality, true);
        armyData.armyName = "Reserve Force";
        armyData.maxSize = defaultMaxArmySize;
        
        // Create army without a general
        Army army = factory.CreateArmy(spawnPos, armyData);
        
        if (army != null)
        {
            if (logActions)
            {
                Debug.Log($"✓ [GetArmyButton] Created new reserve army near {selectable.DisplayName} (Size: {size}, Max: {defaultMaxArmySize})");
            }
            GameEvents.ArmySpawned(army, null);
        }
    }

    /// <summary>
    /// Get the ArmyFactory instance.
    /// </summary>
    private ArmyFactory GetFactory()
    {
        ArmyFactory factory = ArmyFactory.Instance;
        if (factory == null)
        {
            factory = FindFirstObjectByType<ArmyFactory>();
        }
        
        if (factory == null)
        {
            Debug.LogError("[GetArmyButton] ArmyFactory not found!");
            return null;
        }
        
        return factory;
    }
    
    /// <summary>
    /// Set the default max army size at runtime.
    /// Use this for upgrades/buffs that increase army capacity.
    /// </summary>
    public void SetDefaultMaxArmySize(float newMax)
    {
        defaultMaxArmySize = newMax;
        if (logActions)
        {
            Debug.Log($"[GetArmyButton] Default max army size updated to: {defaultMaxArmySize}");
        }
    }
    
    /// <summary>
    /// Get the current default max army size.
    /// </summary>
    public float GetDefaultMaxArmySize()
    {
        return defaultMaxArmySize;
    }
}