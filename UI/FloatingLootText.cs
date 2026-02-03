using UnityEngine;
using TMPro;

/// <summary>
/// Floating text that displays loot gained and fades away.
/// Spawns above city center when a province is raided.
/// </summary>
public class FloatingLootText : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float riseSpeed = 1f;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float lifetime = 2f;
    
    [Header("References")]
    [SerializeField] private TMP_Text textComponent;
    
    private float spawnTime;
    private Vector3 startPosition;
    private Color startColor;
    
    private void Awake()
    {
        // Try to find text component
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshPro>();
        }
        
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
        
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TMP_Text>();
        }
    }
    
    /// <summary>
    /// Initialize with default settings (from SerializeField values)
    /// </summary>
    public void Initialize(float lootAmount, Vector3 worldPosition)
    {
        Initialize(lootAmount, worldPosition, riseSpeed, lifetime);
    }
    
    /// <summary>
    /// Initialize with custom settings from spawner
    /// </summary>
    public void Initialize(float lootAmount, Vector3 worldPosition, float customRiseSpeed, float customLifetime)
    {
        spawnTime = Time.time;
        startPosition = worldPosition;
        riseSpeed = customRiseSpeed;
        lifetime = customLifetime;
        fadeDuration = lifetime * 0.6f; // Fade during last 60% of lifetime
        
        // Set text
        if (textComponent != null)
        {
            textComponent.text = $"+{lootAmount:F0} <color=#FFD700>Gold</color>";
            startColor = textComponent.color;
            startColor.a = 1f;
            textComponent.color = startColor;
        }
        
        // Position
        transform.position = worldPosition + Vector3.up * 0.5f;
    }
    
    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        
        // Rise up
        transform.position = startPosition + Vector3.up * (0.5f + elapsed * riseSpeed);
        
        // Fade out (start fading after (lifetime - fadeDuration))
        if (textComponent != null)
        {
            float fadeStart = lifetime - fadeDuration;
            if (elapsed > fadeStart)
            {
                float fadeProgress = (elapsed - fadeStart) / fadeDuration;
                Color c = startColor;
                c.a = 1f - Mathf.Clamp01(fadeProgress);
                textComponent.color = c;
            }
        }
        
        // Destroy after lifetime
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}

