using UnityEngine;
using UnityEngine.InputSystem;

public class Horse : MonoBehaviour
{
    public Rigidbody2D horseRigidBody;
    public float moveSpeed = 5f;

    // assign in inspector OR get in Awake()
    public SpriteRenderer spriteRenderer;

    private Vector2 moveDir;

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

        // WASD = world-space X/Y movement
        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;

        moveDir = input.normalized;

        // ---- Flip sprite horizontally depending on direction ----
        if (moveDir.x > 0.01f)
        {
            // going right
            spriteRenderer.flipX = false;
        }
        else if (moveDir.x < -0.01f)
        {
            // going left
            spriteRenderer.flipX = true;
        }
    }

    private void FixedUpdate()
    {
        if (moveDir.sqrMagnitude < 0.0001f) return;

        Vector2 targetPos = horseRigidBody.position +
                            moveDir * moveSpeed * Time.fixedDeltaTime;

        horseRigidBody.MovePosition(targetPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
    if (other.CompareTag("Province"))
    {
        Debug.Log("Ata province girdi: " + other.name);
        // burada istediğin işlemi yap
    }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
    if (other.CompareTag("Province"))
    {
        Debug.Log("At province'den çıktı: " + other.name);
    }
    }   
}
