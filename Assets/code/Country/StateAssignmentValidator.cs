using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Reports incomplete province-to-state assignments after the map has finished assigning provinces.
/// </summary>
public class StateAssignmentValidator : MonoBehaviour
{
    private bool validationScheduled;
    private bool validationCompleted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<StateAssignmentValidator>() != null) return;

        GameObject validatorObject = new GameObject(nameof(StateAssignmentValidator));
        DontDestroyOnLoad(validatorObject);
        validatorObject.AddComponent<StateAssignmentValidator>();
    }

    private void OnEnable()
    {
        GameEvents.OnProvincesAssigned += ScheduleValidation;
    }

    private void OnDisable()
    {
        GameEvents.OnProvincesAssigned -= ScheduleValidation;
    }

    private void ScheduleValidation()
    {
        if (validationScheduled || validationCompleted)
        {
            return;
        }

        validationScheduled = true;
        StartCoroutine(ValidateAfterFrame());
    }

    private IEnumerator ValidateAfterFrame()
    {
        yield return null;

        validationScheduled = false;
        validationCompleted = true;
        ValidateAssignments();
    }

    private void ValidateAssignments()
    {
        ProvinceModel[] provinces = FindObjectsByType<ProvinceModel>(FindObjectsSortMode.None);
        List<ProvinceModel> missingStates = new List<ProvinceModel>();
        int checkedProvinceCount = 0;

        foreach (ProvinceModel province in provinces)
        {
            if (province == null || province.CompareTag("River"))
            {
                continue;
            }

            checkedProvinceCount++;
            if (province.provinceState == null)
            {
                missingStates.Add(province);
            }
        }

        if (missingStates.Count == 0)
        {
            GameLog.Log(GameLogCategory.Core,
                $"[StateAssignmentValidator] All {checkedProvinceCount} non-river provinces have a state assignment.");
        }
        else
        {
            GameLog.Error(GameLogCategory.Core,
                $"[StateAssignmentValidator] {missingStates.Count} of {checkedProvinceCount} non-river provinces are missing a state assignment.");
            GameLog.Error(GameLogCategory.Core,
                $"[StateAssignmentValidator] Provinces without a state: {FormatMissingProvinces(missingStates)}");
        }

        StateModel[] states = FindObjectsByType<StateModel>(FindObjectsSortMode.None);
        foreach (StateModel state in states)
        {
            if (state.provinceList == null || state.provinceList.Count == 0)
            {
                GameLog.Warning(GameLogCategory.Core,
                    $"[StateAssignmentValidator] State '{state.stateName}' (ID: {state.stateId}) has no assigned provinces.");
            }
        }
    }

    private static string FormatMissingProvinces(List<ProvinceModel> provinces)
    {
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < provinces.Count; i++)
        {
            ProvinceModel province = provinces[i];
            if (i > 0)
            {
                result.Append(", ");
            }

            result.Append("'");
            result.Append(string.IsNullOrEmpty(province.provinceName) ? province.gameObject.name : province.provinceName);
            result.Append("' (ID: ");
            result.Append(province.provinceId);
            result.Append(")");
        }

        return result.ToString();
    }
}
