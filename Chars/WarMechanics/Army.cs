using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Army : MonoBehaviour
{
    public bool isPlayerArmy;

    [SerializeField] private SpriteRenderer armySprite;
    public Rigidbody2D horseRigidBody;

    [SerializeField] private List<Sprite> sprites;

    void Awake()
    {
        int randomIndex = Random.Range(0, sprites.Count);
        armySprite.sprite = sprites[randomIndex];
    }
    void Start()
    {
        if (sprites == null || sprites.Count == 0)
        {
            Debug.LogWarning("Army: Sprite list is empty!");
            return;
        }


    }

    // army mechanics
    private float armySize;
    private float armyStr;

    // movement stuff
    public float moveSpeed = 5f;
    private Vector2 moveDir;

    private HashSet<ProvinceModel> currentProvinces = new HashSet<ProvinceModel>();
    private ProvinceModel currentProvince;
    private CityCenter currentCityCenter;
}
