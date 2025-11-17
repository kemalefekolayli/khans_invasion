using System.Collections.Generic;
using UnityEngine;

public class NationModel : MonoBehaviour 
{
    public long nationId;
    public string nationName;
    
    public List<ProvinceModel> provinceList = new List<ProvinceModel>();
    public List<StateModel> stateList = new List<StateModel>();
}