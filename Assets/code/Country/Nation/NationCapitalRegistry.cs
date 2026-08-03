using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NationCapitalRegistry", menuName = "Khans Invasion/Nation Capital Registry")]
public class NationCapitalRegistry : ScriptableObject
{
    public List<NationCapitalAssignment> assignments = new List<NationCapitalAssignment>();

    public bool TryGetCapitalProvinceId(long nationId, out int provinceId)
    {
        NationCapitalAssignment assignment = assignments.Find(entry => entry.nationId == nationId);
        if (assignment != null)
        {
            provinceId = assignment.provinceId;
            return true;
        }

        provinceId = -1;
        return false;
    }

    public void SetCapital(long nationId, int provinceId)
    {
        NationCapitalAssignment assignment = assignments.Find(entry => entry.nationId == nationId);
        if (assignment == null)
        {
            assignments.Add(new NationCapitalAssignment { nationId = nationId, provinceId = provinceId });
            return;
        }

        assignment.provinceId = provinceId;
    }
}

[Serializable]
public class NationCapitalAssignment
{
    public long nationId;
    public int provinceId;
}
