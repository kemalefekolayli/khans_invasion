using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
        if (IsPointerOverUI()) return;

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

        if (IsPointerOverUI())
        {
            // A drag that started over a UI panel must never become a world drag
            // when the pointer leaves the panel.
            isDragging = false;
            return;
        }

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
        if (IsPointerOverUI()) return;

        float scroll = context.ReadValue<Vector2>().y;
        cam.orthographicSize -= scroll * zoomSpeed * 0.01f;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    private bool IsPointerOverUI()
    {
        if (Mouse.current == null) return false;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
            return true;

        // IsPointerOverGameObject can lag one UI event behind the camera Update.
        // Check the quest panel bounds directly so the first wheel/drag event is
        // isolated as well.
        QuestPanelController questPanel = QuestPanelController.Instance;
        if (questPanel == null || !questPanel.IsOpen || questPanel.questPanel == null)
            return false;

        RectTransform panelRect = questPanel.questPanel.GetComponent<RectTransform>();
        return panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(
            panelRect,
            Mouse.current.position.ReadValue(),
            null);
    }

    public void SetCameraPosition(Vector2 position)
{
    transform.position = new Vector3(position.x, position.y, transform.position.z);
}

public void SetCameraPosition(Vector3 position)
{
    transform.position = new Vector3(position.x, position.y, transform.position.z);
}
}
