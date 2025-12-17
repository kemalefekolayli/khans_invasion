using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Horse : MonoBehaviour
{
    public Rigidbody2D horseRigidBody;
    public float moveSpeed = 5f;
    public SpriteRenderer spriteRenderer;
    public ProvinceNameDisplay provinceNameDisplay;

    private Vector2 moveDir;
    private HashSet<ProvinceModel> currentProvinces = new HashSet<ProvinceModel>();

    private ProvinceModel lastHighlightedProvince = null;

    private void Awake()
    {
        if (horseRigidBody == null)
            horseRigidBody = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (provinceNameDisplay == null)
            provinceNameDisplay = FindFirstObjectByType<ProvinceNameDisplay>();
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
        {
            spriteRenderer.flipX = false;
        }
        else if (moveDir.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }

        // Her frame'de province kontrolü
        CheckCurrentProvince();
    }

    private void FixedUpdate()
    {
        if (moveDir.sqrMagnitude < 0.0001f) return;

        Vector2 targetPos = horseRigidBody.position +
                            moveDir * moveSpeed * Time.fixedDeltaTime;

        if (!IsPositionBlocked(targetPos))
        {
            horseRigidBody.MovePosition(targetPos);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Province"))
        {

        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Province"))
        {

        }
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

        // Eski province'in rengini geri al
        if (lastHighlightedProvince != null && lastHighlightedProvince != topProvince)
        {
            lastHighlightedProvince.spriteRenderer.color = lastHighlightedProvince.provinceColor;
        }

        // Yeni province'i highlight et
        if (topProvince != null)
        {
            provinceNameDisplay.ShowProvinceName(topProvince);

            // Rengi koyulaştır (%70 daha koyu)
            Color darkened = topProvince.provinceColor * 0.7f;
            darkened.a = topProvince.provinceColor.a; // Alpha'yı koru
            topProvince.spriteRenderer.color = darkened;

            lastHighlightedProvince = topProvince;
        }
        else
        {
            provinceNameDisplay.HideProvinceName();
            lastHighlightedProvince = null;
        }
    }
    private bool IsPositionBlocked(Vector2 position)
{
    Collider2D[] hits = Physics2D.OverlapPointAll(position);
    foreach (var hit in hits)
    {
        if (hit.CompareTag("River") )
            return true;
    }
    return false;
}
}