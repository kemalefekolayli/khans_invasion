using TMPro;
using UnityEngine;

/// <summary>World-space supply display configured on an army or its commanding general.</summary>
public class ArmySupplyWorldText : MonoBehaviour
{
    [Header("World Text")]
    [SerializeField] private TextMeshPro supplyText;

    private Army army;
    private General general;

    private void Awake() => ResolveReferences();
    private void OnEnable()
    {
        GameEvents.OnArmySpawned += OnArmySpawned;
        GameEvents.OnArmyAssigned += OnArmyAssigned;
        GameEvents.OnArmySupplyChanged += OnArmySupplyChanged;
    }
    private void OnDisable()
    {
        GameEvents.OnArmySpawned -= OnArmySpawned;
        GameEvents.OnArmyAssigned -= OnArmyAssigned;
        GameEvents.OnArmySupplyChanged -= OnArmySupplyChanged;
    }
    private void Start() => RefreshDisplay();
    private void OnArmySpawned(Army spawnedArmy, General assignedGeneral) { ResolveReferences(); if (spawnedArmy == army || assignedGeneral == general) RefreshDisplay(); }
    private void OnArmyAssigned(Army assignedArmy, General assignedGeneral) { ResolveReferences(); if (assignedArmy == army || assignedGeneral == general) RefreshDisplay(); }
    private void OnArmySupplyChanged(Army changedArmy) { if (changedArmy == army) RefreshDisplay(); }
    private void RefreshDisplay()
    {
        ResolveReferences();
        ArmySupplyAccount account = army != null ? army.GetComponent<ArmySupplyAccount>() : null;
        if (supplyText != null) supplyText.text = account != null ? $"{Mathf.FloorToInt(account.Available)}/{Mathf.FloorToInt(account.MaximumSupply)}" : string.Empty;
    }
    private void ResolveReferences()
    {
        general = GetComponent<General>() ?? GetComponentInParent<General>();
        army = general != null ? general.CommandedArmy : GetComponentInParent<Army>();
    }
}
