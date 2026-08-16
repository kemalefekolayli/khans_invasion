using UnityEngine;
using UnityEngine.UI;

/// <summary>Recruits troops into the selected general's commanded army.</summary>
public class GetArmyButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button getArmyButton;

    [Header("Troop Settings")]
    [SerializeField] private float troopsToRecruit = 100f;
    [SerializeField] private float startingArmyQuality = 1f;

    [Header("New Army Defaults")]
    [Tooltip("Default max size for newly created armies. Can be modified by upgrades/buffs.")]
    [SerializeField] private float defaultMaxArmySize = 1000f;

    [Header("Conscription Settings")]
    [Tooltip("Minimum population required to conscript troops")]
    [SerializeField] private float minPopulationRequired = 200f;

    [Header("Debug")]
    [SerializeField] private bool logActions = true;

    private void Awake()
    {
        if (getArmyButton != null)
            getArmyButton.onClick.AddListener(OnGetArmyButtonClicked);
    }

    public void OnGetArmyButtonClicked()
    {
        SelectableGeneral selected = GeneralSelectionManager.Instance?.SelectedGeneral;
        if (selected == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[GetArmyButton] No general selected! Select a general first.");
            return;
        }

        General general = selected.GetComponent<General>();
        if (general == null)
        {
            GameLog.Error(GameLogCategory.Core, $"[GetArmyButton] {selected.DisplayName} has no General component!");
            return;
        }

        ProvinceModel province = selected.CurrentProvince;
        if (province == null)
        {
            GameLog.Warning(GameLogCategory.Core, "[GetArmyButton] General is not in a province!");
            SpawnPopup(selected.transform.position, "No province!", Color.red);
            return;
        }

        if (PlayerNation.Instance == null || !PlayerNation.Instance.OwnsProvince(province))
        {
            GameLog.Warning(GameLogCategory.Core, "[GetArmyButton] Cannot recruit from enemy province!");
            SpawnPopup(selected.transform.position, "Enemy province!", Color.red);
            return;
        }

        if (province.provinceCurrentPop < minPopulationRequired)
        {
            GameLog.Warning(GameLogCategory.Core,
                $"[GetArmyButton] Province {province.provinceName} has insufficient population ({province.provinceCurrentPop:F0} < {minPopulationRequired:F0})");
            SpawnPopup(province.transform.position, "Not enough\npopulation!", new Color(1f, 0.5f, 0f));
            return;
        }

        float populationCapacity = Mathf.Max(0f, province.provinceCurrentPop - minPopulationRequired);
        float actualRecruit = Mathf.Min(troopsToRecruit, populationCapacity);
        actualRecruit = MilitaryEconomy.GetOrCreate().ClampNewSoldiers(actualRecruit, "Troop recruitment");

        Army commandedArmy = general.CommandedArmy;
        if (commandedArmy != null)
        {
            float currentSize = commandedArmy.ArmySize;
            float maxSize = commandedArmy.Data.maxSize;
            float commandCapacity = Mathf.Max(0f, maxSize - currentSize);
            if (commandCapacity <= 0.001f)
            {
                string message = $"Army command limit reached: {currentSize:F0}/{maxSize:F0}";
                CenterWarningPopupSpawner.Show(message);
                GameLog.Warning(GameLogCategory.Core, $"[GetArmyButton] {message}");
                return;
            }

            actualRecruit = Mathf.Min(actualRecruit, commandCapacity);
        }
        else
        {
            actualRecruit = Mathf.Min(actualRecruit, defaultMaxArmySize);
        }

        if (actualRecruit <= 0f)
        {
            if (populationCapacity <= 0f)
            {
                GameLog.Warning(GameLogCategory.Core,
                    $"[GetArmyButton] Cannot recruit - would leave province with less than {minPopulationRequired:F0} population");
                SpawnPopup(province.transform.position, "Not enough\npopulation!", new Color(1f, 0.5f, 0f));
            }
            return;
        }

        Army recruitedArmy;
        if (commandedArmy != null)
        {
            commandedArmy.AddSoldiers(actualRecruit);
            recruitedArmy = commandedArmy;
            if (logActions)
                GameLog.Log(GameLogCategory.Core,
                    $"[GetArmyButton] Added {actualRecruit:F0} troops to {selected.DisplayName}'s army. New size: {commandedArmy.ArmySize:F0}/{commandedArmy.Data.maxSize:F0}");
        }
        else
        {
            recruitedArmy = CreateNewArmyForGeneral(general, selected, actualRecruit);
            if (recruitedArmy == null) return;
        }

        province.provinceCurrentPop -= actualRecruit;
        if (logActions)
            GameLog.Log(GameLogCategory.Core,
                $"[GetArmyButton] Conscripted {actualRecruit:F0} troops from {province.provinceName}. Pop: {province.provinceCurrentPop:F0}");

        SpawnPopup(province.transform.position, $"-{actualRecruit:F0} pop", new Color(0.8f, 0.6f, 0.2f));
        GameEvents.PlayerTroopsRecruited(recruitedArmy, actualRecruit);
        PlayerNation.Instance.RecalculateStats();
        GameEvents.PlayerStatsChanged();
    }

    private void SpawnPopup(Vector3 position, string message, Color color)
    {
        GameObject textObj = new GameObject($"RecruitPopup_{message}");
        textObj.transform.position = position + new Vector3(0f, 0.5f, 0f);

        TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = 3f;
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;
        tmp.sortingOrder = 100;

        FloatingPopupText floatScript = textObj.AddComponent<FloatingPopupText>();
        floatScript.Initialize(textObj.transform.position, 1f, 2f);
        textObj.AddComponent<PopupBillboard>();
    }

    private Army CreateNewArmyForGeneral(General general, SelectableGeneral selectable, float amount)
    {
        ArmyFactory factory = GetFactory();
        if (factory == null) return null;

        ArmyData armyData = new ArmyData(amount, startingArmyQuality, true)
        {
            armyName = $"{selectable.DisplayName}'s Army",
            maxSize = defaultMaxArmySize
        };

        Army army = factory.CreateArmyForGeneral(general, armyData);
        if (army == null)
        {
            GameLog.Error(GameLogCategory.Core, "[GetArmyButton] Failed to create army!");
            return null;
        }

        army.OwnerNation = PlayerNation.Instance?.currentNation;
        if (logActions)
            GameLog.Log(GameLogCategory.Core,
                $"[GetArmyButton] Created new army for {selectable.DisplayName} (Size: {amount:F0}, Max: {defaultMaxArmySize:F0})");
        GameEvents.ArmySpawned(army, general);
        return army;
    }

    private ArmyFactory GetFactory()
    {
        ArmyFactory factory = ArmyFactory.Instance != null
            ? ArmyFactory.Instance
            : FindFirstObjectByType<ArmyFactory>();
        if (factory == null)
            GameLog.Error(GameLogCategory.Core, "[GetArmyButton] ArmyFactory not found!");
        return factory;
    }

    public void SetDefaultMaxArmySize(float newMax)
    {
        defaultMaxArmySize = newMax;
        if (logActions)
            GameLog.Log(GameLogCategory.Core, $"[GetArmyButton] Default max army size updated to: {defaultMaxArmySize}");
    }

    public float GetDefaultMaxArmySize() => defaultMaxArmySize;
}
