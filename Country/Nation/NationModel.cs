using System.Collections.Generic;
using UnityEngine;

public enum nationAgression
{
    lightAgression,
    mediumAgression,
    heavyAgression
}
[System.Serializable]
public class NationModel
{
    public bool isPlayer = false;
    public long nationId;
    public string nationName;
    public string nationColor;
    public nationAgression nationAgression;

    public List<ProvinceModel> provinceList = new List<ProvinceModel>();
    public List<StateModel> stateList = new List<StateModel>();
}
