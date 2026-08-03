using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Owns player charisma and converts core gameplay outcomes into configurable changes.</summary>
public class CharismaSystem : MonoBehaviour
{
    public static event System.Action<float> OnCharismaChanged;
    [Header("Current Value")]
    [SerializeField, Range(0f, 100f)] private float charisma = 20f;
    [SerializeField, Range(0f, 100f)] private float startingCharisma = 20f;

    [Header("Battle Rewards")]
    [SerializeField] private float battleWinMin = 5f;
    [SerializeField] private float battleWinEven = 10f;
    [SerializeField] private float battleWinMax = 15f;
    [SerializeField] private float battleLossMin = 10f;
    [SerializeField] private float battleLossEven = 15f;
    [SerializeField] private float battleLossMax = 20f;

    [Header("Province Rewards")]
    [SerializeField] private float provinceCaptureMin = 1f;
    [SerializeField] private float provinceCaptureMax = 5f;
    [SerializeField] private float capitalCaptureBonus = 5f;
    [SerializeField] private float provinceLossMin = 2f;
    [SerializeField] private float provinceLossMax = 8f;

    [Header("General Capture")]
    [SerializeField, Range(0f, 1f)] private float capturedGeneralLossPercent = 0.4f;

    private readonly HashSet<General> previouslyCapturedGenerals = new HashSet<General>();
    private bool initialized;

    public float Current => charisma;

    private void Start()
    {
        if (!initialized)
        {
            charisma = Mathf.Clamp(startingCharisma, 0f, 100f);
            initialized = true;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnArmyBattleEnded += OnArmyBattleEnded;
        GameEvents.OnProvinceConquered += OnProvinceConquered;
    }

    private void OnDisable()
    {
        GameEvents.OnArmyBattleEnded -= OnArmyBattleEnded;
        GameEvents.OnProvinceConquered -= OnProvinceConquered;
    }

    private void Update()
    {
        foreach (General general in FindObjectsByType<General>(FindObjectsSortMode.None))
        {
            if (general == null || general.OwnerNation != PlayerNation.Instance?.Nation) continue;

            if (general.IsCaptured && previouslyCapturedGenerals.Add(general))
            {
                LoseCharisma(charisma * capturedGeneralLossPercent, "general captured");
            }
            else if (!general.IsCaptured)
            {
                previouslyCapturedGenerals.Remove(general);
            }
        }
    }

    public void AddCharisma(float amount, string reason = null)
    {
        ChangeCharisma(Mathf.Abs(amount), reason);
    }

    public void LoseCharisma(float amount, string reason = null)
    {
        ChangeCharisma(-Mathf.Abs(amount), reason);
    }

    public void ChangeCharisma(float amount, string reason = null)
    {
        float previous = charisma;
        charisma = Mathf.Clamp(charisma + amount, 0f, 100f);
        GameLog.Log(GameLogCategory.Core, $"[Charisma] {reason ?? "change"}: {previous:F1} -> {charisma:F1}");
        OnCharismaChanged?.Invoke(charisma);
    }

    private void OnArmyBattleEnded(Army winner, Army loser, ArmyBattleEndReason reason)
    {
        NationModel player = PlayerNation.Instance?.Nation;
        if (player == null || winner == null || loser == null) return;

        float ratio = loser.ArmySize / Mathf.Max(1f, winner.ArmySize);
        if (winner.OwnerNation == player)
        {
            AddCharisma(RewardForRatio(ratio, battleWinMin, battleWinEven, battleWinMax), "battle victory");
        }
        else if (loser.OwnerNation == player)
        {
            float enemyToPlayerRatio = winner.ArmySize / Mathf.Max(1f, loser.ArmySize);
            LoseCharisma(RewardForRatio(enemyToPlayerRatio, battleLossMin, battleLossEven, battleLossMax), "battle defeat");
        }
    }

    private void OnProvinceConquered(ProvinceModel province, NationModel oldOwner, NationModel newOwner)
    {
        NationModel player = PlayerNation.Instance?.Nation;
        if (province == null || player == null) return;

        float populationRatio = province.provinceCurrentPop / Mathf.Max(1f, AveragePopulation(player));

        if (newOwner == player)
        {
            float reward = Mathf.Lerp(provinceCaptureMin, provinceCaptureMax, Mathf.InverseLerp(0.5f, 1.5f, populationRatio));
            if (oldOwner != null && oldOwner.capitalProvince == province) reward += capitalCaptureBonus;
            AddCharisma(reward, "province captured");
        }
        else if (oldOwner == player)
        {
            float loss = Mathf.Lerp(provinceLossMin, provinceLossMax, Mathf.InverseLerp(0.5f, 1.5f, populationRatio));
            LoseCharisma(loss, "province lost");
        }
    }

    private static float AveragePopulation(NationModel nation)
    {
        IEnumerable<ProvinceModel> provinces = nation.provinceList.Where(province => province != null);
        return provinces.Any() ? provinces.Average(province => province.provinceCurrentPop) : 1f;
    }

    private static float RewardForRatio(float ratio, float minimum, float even, float maximum)
    {
        if (ratio <= 1f)
        {
            return Mathf.Lerp(minimum, even, Mathf.InverseLerp(0.5f, 1f, ratio));
        }

        return Mathf.Lerp(even, maximum, Mathf.InverseLerp(1f, 1.5f, ratio));
    }
}
