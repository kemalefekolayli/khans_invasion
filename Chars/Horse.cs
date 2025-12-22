using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Horse : MonoBehaviour, IProvinceDetector
{
    public Rigidbody2D horseRigidBody;
    public float moveSpeed = 5f;
    public SpriteRenderer spriteRenderer;

    private Vector2 moveDir;
    private HashSet<ProvinceModel> currentProvinces = new HashSet<ProvinceModel>();
    private ProvinceModel currentProvince;
    private CityCenter currentCityCenter;

    // IProvinceDetector implementation
    public ProvinceModel CurrentProvince => currentProvince;
    public CityCenter CurrentCityCenter => currentCityCenter;
    public Vector3 Position => transform.position;
    
    // Check if horse is on a city center
    public bool IsOnCityCenter => currentCityCenter != null;

    private void Awake()
    {
        if (horseRigidBody == null)
            horseRigidBody = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;

        moveDir = input.normalized;

        if (moveDir.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveDir.x < -0.01f)
            spriteRenderer.flipX = true;

        CheckCurrentProvince();
        CheckCityCenter();
    }

    private void FixedUpdate()
    {
        if (moveDir.sqrMagnitude < 0.0001f) return;

        Vector2 targetPos = horseRigidBody.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        if (!IsPositionBlocked(targetPos))
            horseRigidBody.MovePosition(targetPos);
    }

    private void CheckCurrentProvince()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        currentProvinces.Clear();
        ProvinceModel topProvince = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Province"))
            {
                ProvinceModel province = hit.GetComponent<ProvinceModel>();
                if (province != null)
                {
                    currentProvinces.Add(province);
                    if (topProvince == null)
                        topProvince = province;
                }
            }
        }

        if (currentProvince != topProvince)
        {
            if (currentProvince != null)
                GameEvents.ProvinceExit(currentProvince);

            if (topProvince != null)
                GameEvents.ProvinceEnter(topProvince);

            currentProvince = topProvince;
        }
    }

    private void CheckCityCenter()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        
        CityCenter detectedCityCenter = null;
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("CityCenter"))
            {
                CityCenter center = hit.GetComponent<CityCenter>();
                if (center != null)
                {
                    detectedCityCenter = center;
                    break;
                }
            }
        }
        
        if (currentCityCenter != detectedCityCenter)
        {
            if (currentCityCenter != null)
            {
                currentCityCenter.SetHighlight(false);
                GameEvents.CityCenterExit(currentCityCenter);
            }
            
            if (detectedCityCenter != null)
            {
                detectedCityCenter.SetHighlight(true);
                GameEvents.CityCenterEnter(detectedCityCenter);
            }
            
            currentCityCenter = detectedCityCenter;
        }
    }

    private bool IsPositionBlocked(Vector2 position)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(position);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("River"))
                return true;
        }
        return false;
    }
}