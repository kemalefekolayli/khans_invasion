using UnityEngine;
using UnityEngine.InputSystem;

public class RegionClickManager_NewInput : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference click;   // <Pointer>/press
    [SerializeField] private InputActionReference point;   // <Pointer>/position

    [Header("Raycast Settings")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask regionLayer; // set to your "Regions" layer

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

private void OnEnable()
{
    Debug.Log("RegionClickManager enabled");

    if (click == null || click.action == null) Debug.LogError("CLICK reference missing!");
    if (point == null || point.action == null) Debug.LogError("POINT reference missing!");

    click.action.Enable();
    point.action.Enable();
    click.action.performed += OnClickPerformed;

    Debug.Log("Actions enabled: click=" + click.action.enabled + " point=" + point.action.enabled);
}

    private void OnDisable()
    {
        click.action.performed -= OnClickPerformed;
        click.action.Disable();
        point.action.Disable();
    }

private void OnClickPerformed(InputAction.CallbackContext ctx)
{
    Vector2 screenPos = point.action.ReadValue<Vector2>();
    Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);

    Debug.Log($"CLICK screen={screenPos} world={worldPos}");

    Collider2D hitAny = Physics2D.OverlapPoint(worldPos);
    Debug.Log("HitAny = " + (hitAny ? hitAny.name : "NONE"));

    Collider2D hitRegion = Physics2D.OverlapPoint(worldPos, regionLayer);
    Debug.Log("HitRegion = " + (hitRegion ? hitRegion.name : "NONE"));

    if (hitRegion == null) return;

    var region = hitRegion.GetComponent<ClickableRegion>();
    Debug.Log("ClickableRegion component? " + (region != null));
    region?.OnClicked();
}
}
