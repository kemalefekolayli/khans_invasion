using UnityEngine;

public class CityCenter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ProvinceModel province;

    private NationModel ownerNation;

    void Start()
    {
        if(province == null)
        {
            province = GetComponentInParent<ProvinceModel>();
        }
    }
    public void changeSprite()
    {
        // to change sprite
    }

    // we will create an event for when a nation takes control of a province, we will subsribe to it here so that once the province owner changes so does the city center
}