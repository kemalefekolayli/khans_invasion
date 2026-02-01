using UnityEngine;

/// <summary>
/// Visual indicator that appears around the selected general.
/// Can be a circle, arrow, or any sprite that pulses/rotates.
/// 
/// SETUP: Add as a child object to each general prefab.
/// </summary>
public class SelectionIndicator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.1f;
    
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float rotationSpeed = 30f;
    
    [SerializeField] private bool enableBob = false;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.1f;
    
    [Header("Color")]
    [SerializeField] private bool enableColorPulse = false;
    [SerializeField] private Color color1 = Color.green;
    [SerializeField] private Color color2 = Color.yellow;
    [SerializeField] private float colorPulseSpeed = 1f;
    
    private SpriteRenderer spriteRenderer;
    private Vector3 basePosition;
    private Vector3 baseScale;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
    }
    
    private void Update()
    {
        if (enablePulse)
        {
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            transform.localScale = baseScale * pulse;
        }
        
        if (enableRotation)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
        
        if (enableBob)
        {
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = basePosition + Vector3.up * bob;
        }
        
        if (enableColorPulse && spriteRenderer != null)
        {
            float t = (Mathf.Sin(Time.time * colorPulseSpeed) + 1f) * 0.5f;
            spriteRenderer.color = Color.Lerp(color1, color2, t);
        }
    }
    
    private void OnEnable()
    {
        // Reset to base state when enabled
        transform.localPosition = basePosition;
        transform.localScale = baseScale;
    }
}
