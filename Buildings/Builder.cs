using UnityEngine;

public class Builder  // should this be monobehaviour ? idk ill only use this class to build buildings in provicnes
{
    public void BuildBuilding(ProvinceModel province, string buildingType)
    {
        if (province.buildings.Contains(buildingType))
        {
            Debug.Log($"Building of type {buildingType} already exists in {province} !");
            return;
        }
        switch (buildingType)
        {
            case "Barracks":
            BuildBarracks(province);
                break;

            case "Farm":
            BuildFarm(province);
                break;

            case "Housing":
            BuildHousing(province);
                break;

            case "Trade_Building":
            BuildTradeBuilding(province);
                break;

            case "Fortress":
            BuildFortress(province);
                break;               

            default:
                Debug.Log("Unknown state");
                break;
        }
        
    }

    private void BuildFortress(ProvinceModel province)
    {
        province.buildings.Add("Fortress");
    }
        private void BuildFarm(ProvinceModel province)
    {
        province.buildings.Add("Farm");
    }
        private void BuildBarracks(ProvinceModel province)
    {
        province.buildings.Add("Barracks");
    }
        private void BuildTradeBuilding(ProvinceModel province)
    {
        province.buildings.Add("Trade_Building");
    }
        private void BuildHousing(ProvinceModel province)
    {
        province.buildings.Add("Housing");
    }
}