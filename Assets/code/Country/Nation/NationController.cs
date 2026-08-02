using UnityEngine;
using System;
public class NationController : MonoBehaviour
{
    
    public void SetNationCapital(NationModel nation, ProvinceModel capitalProvince)
    {
        if (nation != null && capitalProvince != null)
        {
            nation.capitalProvince = capitalProvince;
            GameEvents.PlayerCapitalSet(capitalProvince);
        }
    }
}
