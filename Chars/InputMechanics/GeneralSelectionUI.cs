using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI panel that displays all registered generals with selection buttons.
/// Shows which general is currently selected and allows clicking to switch.
/// 
/// SETUP:
/// 1. Create a UI panel with a vertical layout group
/// 2. Create a button prefab for general entries
/// 3. Assign references in inspector
/// </summary>
public class GeneralSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject generalButtonPrefab;
    
    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color khanColor = new Color(1f, 0.8f, 0.2f); // Gold for Khan
    
    [Header("Display")]
    [SerializeField] private bool showGeneralIndex = true;
    [SerializeField] private string indexFormat = "[{0}] {1}";
    
    // Tracking
    private Dictionary<SelectableGeneral, GeneralButtonEntry> buttonEntries = new Dictionary<SelectableGeneral, GeneralButtonEntry>();
    
    private class GeneralButtonEntry
    {
        public GameObject buttonObject;
        public Button button;
        public TextMeshProUGUI nameText;
        public Image background;
    }
    
    private void OnEnable()
    {
        // Subscribe to selection events
        GeneralSelectionManager.OnGeneralRegistered += OnGeneralRegistered;
        GeneralSelectionManager.OnGeneralUnregistered += OnGeneralUnregistered;
        GeneralSelectionManager.OnGeneralSelected += OnGeneralSelected;
        GeneralSelectionManager.OnGeneralDeselected += OnGeneralDeselected;
        
        // Build initial list
        RefreshList();
    }
    
    private void OnDisable()
    {
        GeneralSelectionManager.OnGeneralRegistered -= OnGeneralRegistered;
        GeneralSelectionManager.OnGeneralUnregistered -= OnGeneralUnregistered;
        GeneralSelectionManager.OnGeneralSelected -= OnGeneralSelected;
        GeneralSelectionManager.OnGeneralDeselected -= OnGeneralDeselected;
    }
    
    private void RefreshList()
    {
        if (GeneralSelectionManager.Instance == null) return;
        
        // Clear existing buttons
        ClearButtons();
        
        // Create buttons for all registered generals
        foreach (var general in GeneralSelectionManager.Instance.RegisteredGenerals)
        {
            CreateButtonForGeneral(general);
        }
        
        // Update selection visual
        UpdateSelectionVisuals();
    }
    
    private void ClearButtons()
    {
        foreach (var entry in buttonEntries.Values)
        {
            if (entry.buttonObject != null)
            {
                Destroy(entry.buttonObject);
            }
        }
        buttonEntries.Clear();
    }
    
    private void CreateButtonForGeneral(SelectableGeneral general)
    {
        if (generalButtonPrefab == null || buttonContainer == null)
        {
            Debug.LogWarning("[GeneralSelectionUI] Missing prefab or container reference!");
            return;
        }
        
        // Instantiate button
        GameObject buttonObj = Instantiate(generalButtonPrefab, buttonContainer);
        
        // Get components
        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI nameText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        Image background = buttonObj.GetComponent<Image>();
        
        // Set up entry
        GeneralButtonEntry entry = new GeneralButtonEntry
        {
            buttonObject = buttonObj,
            button = button,
            nameText = nameText,
            background = background
        };
        
        buttonEntries[general] = entry;
        
        // Set name text
        if (nameText != null)
        {
            int index = GeneralSelectionManager.Instance.GetIndex(general);
            
            if (showGeneralIndex)
            {
                nameText.text = string.Format(indexFormat, index + 1, general.DisplayName);
            }
            else
            {
                nameText.text = general.DisplayName;
            }
        }
        
        // Set up click handler
        if (button != null)
        {
            // Need to capture general in closure
            SelectableGeneral capturedGeneral = general;
            button.onClick.AddListener(() => OnButtonClicked(capturedGeneral));
        }
        
        // Set initial color
        UpdateButtonVisual(general);
    }
    
    private void OnButtonClicked(SelectableGeneral general)
    {
        if (GeneralSelectionManager.Instance != null)
        {
            GeneralSelectionManager.Instance.Select(general);
        }
    }
    
    private void OnGeneralRegistered(SelectableGeneral general)
    {
        CreateButtonForGeneral(general);
    }
    
    private void OnGeneralUnregistered(SelectableGeneral general)
    {
        if (buttonEntries.TryGetValue(general, out GeneralButtonEntry entry))
        {
            if (entry.buttonObject != null)
            {
                Destroy(entry.buttonObject);
            }
            buttonEntries.Remove(general);
        }
    }
    
    private void OnGeneralSelected(SelectableGeneral general)
    {
        UpdateSelectionVisuals();
    }
    
    private void OnGeneralDeselected(SelectableGeneral general)
    {
        UpdateSelectionVisuals();
    }
    
    private void UpdateSelectionVisuals()
    {
        foreach (var kvp in buttonEntries)
        {
            UpdateButtonVisual(kvp.Key);
        }
    }
    
    private void UpdateButtonVisual(SelectableGeneral general)
    {
        if (!buttonEntries.TryGetValue(general, out GeneralButtonEntry entry)) return;
        if (entry.background == null) return;
        
        bool isSelected = GeneralSelectionManager.Instance != null && 
                          GeneralSelectionManager.Instance.IsSelected(general);
        
        if (isSelected)
        {
            entry.background.color = selectedColor;
        }
        else if (general.IsKhan)
        {
            entry.background.color = khanColor;
        }
        else
        {
            entry.background.color = normalColor;
        }
    }
    
    [ContextMenu("Refresh List")]
    public void ForceRefresh()
    {
        RefreshList();
    }
}
