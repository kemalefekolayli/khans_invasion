using UnityEngine;

/// <summary>
/// Component that manages troop leveling.
/// Attach to Army prefab alongside Army component.
/// Handles XP gain, level ups, and notifies visuals to update.
/// </summary>
public class TroopLevel : MonoBehaviour
{
    [Header("Level Data")]
    [SerializeField] private TroopLevelData data = new TroopLevelData();
    
    // References
    private Army army;
    private TroopLevelVisuals visuals;
    
    // Properties
    public TroopLevelData Data => data;
    public int CurrentLevel => data.currentLevel;
    public bool IsMaxLevel => data.IsMaxLevel;
    public TroopColorVariant ColorVariant => data.colorVariant;
    public float LevelProgress => data.LevelProgress;
    
    private void Awake()
    {
        army = GetComponent<Army>();
        visuals = GetComponent<TroopLevelVisuals>();
    }
    
    private void Start()
    {
        // Update visuals on start to match inspector values
        UpdateVisuals();
    }
    
    // Called when Inspector values change (Editor only)
    private void OnValidate()
    {
        // Clamp level
        data.currentLevel = Mathf.Clamp(data.currentLevel, TroopLevelData.MIN_LEVEL, TroopLevelData.MAX_LEVEL);
        
        // Update visuals in editor
        if (visuals == null)
            visuals = GetComponent<TroopLevelVisuals>();
        
        if (visuals != null)
        {
            // Use DelayCall in editor to avoid errors
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && visuals != null)
                    visuals.UpdateVisuals(data);
            };
            #endif
        }
    }
    
    /// <summary>
    /// Initialize with specific color and level.
    /// </summary>
    public void Initialize(TroopColorVariant color, int startLevel = 1)
    {
        data = new TroopLevelData(color, startLevel);
        UpdateVisuals();
    }
    
    /// <summary>
    /// Initialize with existing data.
    /// </summary>
    public void Initialize(TroopLevelData levelData)
    {
        data = levelData.Clone();
        UpdateVisuals();
    }
    
    /// <summary>
    /// Add experience points. Handles level up if threshold reached.
    /// </summary>
    public void GainXP(float amount)
    {
        if (data.IsMaxLevel)
        {
            Debug.Log($"[TroopLevel] {army?.Data.armyName} is max level, XP ignored");
            return;
        }
        
        int previousLevel = data.currentLevel;
        bool leveledUp = data.AddXP(amount);
        
        if (leveledUp)
        {
            OnLevelUp(previousLevel, data.currentLevel);
        }
        
        // Always update visuals (progress bar changes)
        UpdateVisuals();
    }
    
    /// <summary>
    /// Force level up (for testing or special events).
    /// </summary>
    [ContextMenu("Force Level Up")]
    public void ForceLevelUp()
    {
        if (data.IsMaxLevel)
        {
            Debug.Log("[TroopLevel] Already at max level!");
            return;
        }
        
        int previousLevel = data.currentLevel;
        data.SetLevel(data.currentLevel + 1);
        OnLevelUp(previousLevel, data.currentLevel);
        UpdateVisuals();
    }
    
    /// <summary>
    /// Set specific level directly.
    /// </summary>
    public void SetLevel(int level)
    {
        int previousLevel = data.currentLevel;
        data.SetLevel(level);
        
        if (level != previousLevel)
        {
            OnLevelUp(previousLevel, data.currentLevel);
        }
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Change color variant (keeps current level).
    /// </summary>
    public void SetColorVariant(TroopColorVariant color)
    {
        data.colorVariant = color;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Force refresh visuals (call after changing data in inspector)
    /// </summary>
    [ContextMenu("Refresh Visuals")]
    public void RefreshVisuals()
    {
        UpdateVisuals();
    }
    
    private void OnLevelUp(int fromLevel, int toLevel)
    {
        string armyName = army?.Data.armyName ?? gameObject.name;
        Debug.Log($"✓ [{armyName}] LEVEL UP: {fromLevel} -> {toLevel}!");
        
        // Fire event (only at runtime)
        if (Application.isPlaying)
        {
            GameEvents.TroopLevelUp(this, fromLevel, toLevel);
        }
        
        // Play effects, sounds, etc. can be triggered here
        if (toLevel == TroopLevelData.MAX_LEVEL)
        {
            Debug.Log($"★ [{armyName}] reached MAX LEVEL - GOLDEN ELITE!");
        }
    }
    
    private void UpdateVisuals()
    {
        if (visuals == null)
            visuals = GetComponent<TroopLevelVisuals>();
            
        if (visuals != null)
        {
            visuals.UpdateVisuals(data);
        }
    }
    
    // Debug helper
    [ContextMenu("Debug Print Status")]
    private void DebugPrintStatus()
    {
        Debug.Log($"[TroopLevel] Level: {data.currentLevel}/{TroopLevelData.MAX_LEVEL}, " +
                  $"XP: {data.currentXP}/{data.xpToNextLevel}, " +
                  $"Color: {data.colorVariant}");
    }
}