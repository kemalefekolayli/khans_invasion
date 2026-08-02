using UnityEngine;
using TMPro;

/// <summary>
/// Displays army size text for THIS specific army.
/// Only updates when this army's data changes.
/// </summary>
public class ArmyText : MonoBehaviour
{
    public TextMeshPro armySizeText;    // Army size
    public TextMeshPro armyStrText;     // Army strength (reserved for future use)

    private Army myArmy;

    private void Awake()
    {
        // Cache reference to OUR army (check self and parent)
        myArmy = GetComponent<Army>();
        if (myArmy == null)
            myArmy = GetComponentInParent<Army>();
    }

    private void Start()
    {
        // Initial update on spawn
        RefreshDisplay();
    }

    private void OnEnable()
    {
        GameEvents.OnArmySpawned += OnAnyArmySpawned;
    }
    
    private void OnDisable()
    {
        GameEvents.OnArmySpawned -= OnAnyArmySpawned;
    }

    /// <summary>
    /// Called when ANY army spawns - only update if it's OUR army
    /// </summary>
    private void OnAnyArmySpawned(Army army, General general)
    {
        // Only update if this is OUR army
        if (army == myArmy)
        {
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Legacy method for compatibility - updates display with given size
    /// </summary>
    public void UpdateArmyText(int size)
    {
        if (armySizeText != null)
        {
            armySizeText.text = size.ToString();
        }
    }

    /// <summary>
    /// Refresh the display using our army's current data.
    /// Call this whenever army size changes.
    /// </summary>
    public void RefreshDisplay()
    {
        if (myArmy == null)
        {
            // Try to find army again (might have been added after Awake)
            myArmy = GetComponent<Army>();
            if (myArmy == null)
                myArmy = GetComponentInParent<Army>();
        }

        if (myArmy != null && armySizeText != null)
        {
            armySizeText.text = ((int)myArmy.ArmySize).ToString();
        }
    }

    /// <summary>
    /// Set the army reference manually (useful when created via factory)
    /// </summary>
    public void SetArmy(Army army)
    {
        myArmy = army;
        RefreshDisplay();
    }
}