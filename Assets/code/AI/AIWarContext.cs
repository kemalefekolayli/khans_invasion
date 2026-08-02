using System.Collections.Generic;

public class AIWarContext
{
    public NationModel Nation;
    public int BaseAggression;
    public int EffectiveAggression;

    public float OwnTroops;
    public float OwnStrength;
    public int OwnArmyCount;
    public float NeighborWeightedStrength;
    public float ReadinessRatio;

    public bool MeetsMinimumAttackForce;
    public bool MeetsMinimumRaidForce;
    public bool HasConnectedEnemyNeighbor;
    public bool CanScout;
    public bool CanAttack;
    public bool CanRaid;
    public bool CanSiege;

    public AIWarAction PreferredAction;
    public List<NationModel> ConnectedEnemyNations = new List<NationModel>();
    public List<ProvinceModel> OwnBorderProvinces = new List<ProvinceModel>();
    public List<ProvinceModel> EnemyBorderProvinces = new List<ProvinceModel>();
}
