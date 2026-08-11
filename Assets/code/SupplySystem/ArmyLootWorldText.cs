using TMPro;
using UnityEngine;

/// <summary>Displays the commanded general's carried loot above an army or general.</summary>
public class ArmyLootWorldText : MonoBehaviour
{
    [Header("World Text")]
    [SerializeField] private TextMeshPro lootText;

    private Army army;
    private General general;

    private void Awake() => ResolveReferences();
    private void OnEnable()
    {
        GameEvents.OnArmySpawned += OnArmySpawned;
        GameEvents.OnArmyAssigned += OnArmyAssigned;
        GameEvents.OnGeneralLootChanged += OnGeneralLootChanged;
    }
    private void OnDisable()
    {
        GameEvents.OnArmySpawned -= OnArmySpawned;
        GameEvents.OnArmyAssigned -= OnArmyAssigned;
        GameEvents.OnGeneralLootChanged -= OnGeneralLootChanged;
    }
    private void Start() => RefreshDisplay();
    private void OnArmySpawned(Army spawnedArmy, General assignedGeneral) { ResolveReferences(); if (spawnedArmy == army || assignedGeneral == general) SetGeneral(assignedGeneral); }
    private void OnArmyAssigned(Army assignedArmy, General assignedGeneral) { ResolveReferences(); if (assignedArmy == army || assignedGeneral == general) SetGeneral(assignedGeneral); }
    private void OnGeneralLootChanged(General changedGeneral) { if (changedGeneral == general) RefreshDisplay(); }
    private void SetGeneral(General assignedGeneral) { general = assignedGeneral ?? general ?? army?.CommandingGeneral; RefreshDisplay(); }
    private void RefreshDisplay()
    {
        ResolveReferences();
        if (lootText != null) lootText.text = general != null ? Mathf.FloorToInt(general.CarriedLoot).ToString() : string.Empty;
    }
    private void ResolveReferences()
    {
        general = GetComponent<General>() ?? GetComponentInParent<General>();
        army = general != null ? general.CommandedArmy : GetComponentInParent<Army>();
        if (general == null) general = army?.CommandingGeneral;
    }
}
