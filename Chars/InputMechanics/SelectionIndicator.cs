using UnityEngine;
using TMPro;

/// <summary>
/// Visual indicator that appears around the selected general.
/// Can be a circle, arrow, or any sprite that pulses/rotates.
/// 
/// SETUP: Add as a child object to each general prefab.
/// TextMeshPro is created dynamically at runtime.
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
    
    [Header("Name Display")]
    [SerializeField] private float fontSize = 4f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Vector3 nameOffset = new Vector3(0, 0.8f, 0);
    
    private SpriteRenderer spriteRenderer;
    private Vector3 basePosition;
    private Vector3 baseScale;
    
    // Dynamically created text
    private TextMeshPro nameText;
    private GameObject nameTextObject;
    private Vector3 nameTextBaseScale;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
        
        // Create TextMeshPro dynamically
        CreateNameText();
    }
    
    private void CreateNameText()
    {
        // Create a new GameObject for the text
        nameTextObject = new GameObject("GeneralNameText");
        nameTextObject.transform.SetParent(transform);
        nameTextObject.transform.localPosition = nameOffset;
        nameTextObject.transform.localRotation = Quaternion.identity;
        nameTextObject.transform.localScale = Vector3.one;
        
        // Add TextMeshPro component
        nameText = nameTextObject.AddComponent<TextMeshPro>();
        nameText.text = "";
        nameText.fontSize = fontSize;
        nameText.color = textColor;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.sortingOrder = 100; // Make sure it's on top
        
        // Set the RectTransform size
        RectTransform rectTransform = nameText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(5f, 1f);
        
        // Store base scale for counter-scaling during pulse
        nameTextBaseScale = nameTextObject.transform.localScale;
        
        // Start hidden until name is set
        nameTextObject.SetActive(false);
        

    }
    
    private void Update()
    {
        float currentPulse = 1f;
        
        if (enablePulse)
        {
            currentPulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            transform.localScale = baseScale * currentPulse;
            
            // Counter-scale the text so it stays same size
            if (nameText != null && currentPulse > 0.01f)
            {
                nameTextObject.transform.localScale = nameTextBaseScale / currentPulse;
            }
        }
        
        if (enableRotation)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            
            // Counter-rotate the text so it stays upright
            if (nameTextObject != null)
            {
                nameTextObject.transform.rotation = Quaternion.identity;
            }
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
    
    /// <summary>
    /// Set the general name to display on the indicator.
    /// </summary>
    public void SetGeneralName(string name)
    {
        if (nameText != null)
        {
            nameText.text = name;
            nameTextObject.SetActive(true);
            Debug.Log($"[SelectionIndicator] Set name: {name}");
        }
    }
}

