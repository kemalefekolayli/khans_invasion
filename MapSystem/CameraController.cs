using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Ayarlar")]
    public float panSpeed = 20f;
    public float dragSpeed = 2f;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;

    private Camera cam;
    private Vector2 moveInput;
    private Vector2 mousePosition;
    private Vector2 lastMousePosition;
    private bool isDragging;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 10f;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleDrag();
    }

  void HandleMovement()
{
    moveInput = Vector2.zero;

    // Only ARROW KEYS move the camera
    if (Keyboard.current.upArrowKey.isPressed)
        moveInput.y += 1;
    if (Keyboard.current.downArrowKey.isPressed)
        moveInput.y -= 1;
    if (Keyboard.current.leftArrowKey.isPressed)
        moveInput.x -= 1;
    if (Keyboard.current.rightArrowKey.isPressed)
        moveInput.x += 1;

    transform.position += new Vector3(moveInput.x, moveInput.y, 0) * panSpeed * Time.deltaTime;
}


    void HandleZoom()
    {
        // Mouse scroll wheel
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y / 120f; // 120 = bir scroll birimi
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandleDrag()
    {
        if (Mouse.current == null) return;

        // Sağ tık ile sürükleme
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (isDragging)
        {
            mousePosition = Mouse.current.position.ReadValue();
            Vector2 delta = mousePosition - lastMousePosition;
            
            // Ekran koordinatlarını dünya koordinatlarına çevir
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * dragSpeed * cam.orthographicSize * 0.001f;
            transform.position += move;
            
            lastMousePosition = mousePosition;
        }
    }

    // Eğer Input Actions kullanmak istersen (daha advanced)
    // Bu method'ları InputAction'lara bağlayabilirsin
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<Vector2>().y;
        cam.orthographicSize -= scroll * zoomSpeed * 0.01f;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }
}