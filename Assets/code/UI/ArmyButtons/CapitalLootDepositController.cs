using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Builds and controls a temporary carried-loot deposit companion button from the
/// runtime interaction button. No prefab or scene reference is required.
/// </summary>
public class CapitalLootDepositController : MonoBehaviour
{
    [Header("Companion Button")]
    [SerializeField] private GameObject interactionButtonRoot;
    [SerializeField] private string buttonLabel = "Deposit";
    [SerializeField, Range(0.25f, 1f)] private float companionScale = 0.8f;
    [SerializeField] private Vector2 companionOffset = new Vector2(215f, 0f);

    [Header("Temporary Input")]
    [SerializeField] private Key depositKey = Key.J;

    private GameObject companionObject;
    private Button companionButton;

    public void Initialize(GameObject runtimeInteractionRoot)
    {
        interactionButtonRoot = runtimeInteractionRoot;
        EnsureCompanionButton();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        GeneralSelectionManager.OnGeneralSelected += OnGeneralSelectionChanged;
        GeneralSelectionManager.OnGeneralDeselected += OnGeneralSelectionChanged;
        GameEvents.OnCityCenterEnter += OnCityCenterChanged;
        GameEvents.OnCityCenterExit += OnCityCenterChanged;
        GameEvents.OnGeneralLootChanged += OnGeneralLootChanged;
        GameEvents.OnPlayerNationChanged += OnPlayerNationChanged;
        GameEvents.OnPlayerNationCapitalSet += OnPlayerCapitalChanged;
        GameEvents.OnProvinceOwnerChanged += OnProvinceOwnerChanged;
        RefreshVisibility();
    }

    private void OnDisable()
    {
        GeneralSelectionManager.OnGeneralSelected -= OnGeneralSelectionChanged;
        GeneralSelectionManager.OnGeneralDeselected -= OnGeneralSelectionChanged;
        GameEvents.OnCityCenterEnter -= OnCityCenterChanged;
        GameEvents.OnCityCenterExit -= OnCityCenterChanged;
        GameEvents.OnGeneralLootChanged -= OnGeneralLootChanged;
        GameEvents.OnPlayerNationChanged -= OnPlayerNationChanged;
        GameEvents.OnPlayerNationCapitalSet -= OnPlayerCapitalChanged;
        GameEvents.OnProvinceOwnerChanged -= OnProvinceOwnerChanged;
    }

    private void OnDestroy()
    {
        if (companionButton != null)
            companionButton.onClick.RemoveListener(OnDepositClicked);
    }

    private void Update()
    {
        if (companionObject == null || !companionObject.activeSelf) return;
        if (Keyboard.current == null || !Keyboard.current[depositKey].wasPressedThisFrame) return;
        if (TurnManager.Instance != null && !TurnManager.Instance.CanPlayerAct) return;

        TryDepositSelectedLoot();
    }

    private void EnsureCompanionButton()
    {
        if (companionObject != null) return;
        if (interactionButtonRoot == null) interactionButtonRoot = gameObject;

        Button sourceButton = interactionButtonRoot.GetComponentInChildren<Button>(true);
        if (sourceButton == null)
        {
            GameLog.Warning(GameLogCategory.UI, "[LootDeposit] Runtime interaction button was not found.");
            return;
        }

        companionObject = Instantiate(sourceButton.gameObject, sourceButton.transform.parent);
        companionObject.name = "DepositLootButton";

        InteractionButtonController interactionController = companionObject.GetComponent<InteractionButtonController>();
        if (interactionController != null)
        {
            interactionController.enabled = false;
            Destroy(interactionController);
        }

        companionButton = companionObject.GetComponent<Button>();
        if (companionButton == null)
        {
            GameLog.Warning(GameLogCategory.UI, "[LootDeposit] Cloned interaction object has no Button component.");
            companionObject.SetActive(false);
            return;
        }

        companionButton.onClick.RemoveAllListeners();
        companionButton.onClick.AddListener(OnDepositClicked);

        RectTransform sourceRect = sourceButton.transform as RectTransform;
        RectTransform companionRect = companionObject.transform as RectTransform;
        if (sourceRect != null && companionRect != null)
        {
            companionRect.anchoredPosition = sourceRect.anchoredPosition + companionOffset;
            companionRect.localScale = sourceRect.localScale * companionScale;
        }

        TMP_Text label = companionObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = buttonLabel;
    }

    private void OnDepositClicked()
    {
        if (TurnManager.Instance != null && !TurnManager.Instance.CanPlayerAct) return;
        TryDepositSelectedLoot();
    }

    private void TryDepositSelectedLoot()
    {
        General general = GetSelectedGeneral();
        if (general == null || !general.DepositLootToTreasury(out _))
            RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (companionObject == null) return;
        companionObject.SetActive(CapitalLootDepositPolicy.CanDeposit(GetSelectedGeneral()));
    }

    private static General GetSelectedGeneral()
    {
        return GeneralSelectionManager.Instance?.SelectedGeneral?.GetComponent<General>();
    }

    private void OnGeneralSelectionChanged(SelectableGeneral selectable) => RefreshVisibility();
    private void OnCityCenterChanged(CityCenter cityCenter) => RefreshVisibility();
    private void OnGeneralLootChanged(General general) => RefreshVisibility();
    private void OnPlayerNationChanged(NationModel nation) => RefreshVisibility();
    private void OnPlayerCapitalChanged(ProvinceModel province) => RefreshVisibility();
    private void OnProvinceOwnerChanged(ProvinceModel province, NationModel oldOwner, NationModel newOwner) => RefreshVisibility();
}
