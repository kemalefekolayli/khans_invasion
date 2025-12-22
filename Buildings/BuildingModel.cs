using UnityEngine;

public class BuildingModel : MonoBehaviour
{
        private long buildId;
        private SpriteRenderer buildingSprite;
        private float buildingCost; // will be cheaper for some etc.

        private BuildingType buildType;


        public float getBuildCost()
        {
        return buildingCost; // will make this take nation as a parameter and compare the calculate the price
                             // diff prices for diff buildings
        }


}