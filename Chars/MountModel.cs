using UnityEngine;
using UnityEngine.InputSystem;
public class MountModel : MonoBehaviour
{
    public string mountName;
    public long mountId;

    public float mountSpeed;
    public float mountAccel;

    Rigidbody2D rb;
    PlayerInput input;
    Vector2 moveInput;

    public SpriteRenderer spriteRenderer;




}