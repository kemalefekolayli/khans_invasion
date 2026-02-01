using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Singleton manager that controls which general is currently selected and receiving input.
/// Only one general can be active at a time.
/// 
/// SETUP: Add to a persistent GameObject in the scene (e.g., GameManager).
/// </summary>
public class GeneralSelectionManager : MonoBehaviour
{
    public static GeneralSelectionManager Instance { get; private set; }
    
    [Header("Settings")]
    [Tooltip("Automatically select the first registered general if none is selected")]
    public bool autoSelectFirst = true;
    
    [Tooltip("Allow clicking in world to select generals")]
    public bool enableClickSelection = true;
    
    [Tooltip("Layer mask for click selection raycasts")]
    public LayerMask selectableLayerMask = ~0; // Default: all layers
    
    [Header("Debug")]
    [SerializeField] private SelectableGeneral _selectedGeneral;
    [SerializeField] private List<SelectableGeneral> _registeredGenerals = new List<SelectableGeneral>();
    
    // Events
    public static event Action<SelectableGeneral> OnGeneralSelected;
    public static event Action<SelectableGeneral> OnGeneralDeselected;
    public static event Action<SelectableGeneral> OnGeneralRegistered;
    public static event Action<SelectableGeneral> OnGeneralUnregistered;
    
    // Properties
    public SelectableGeneral SelectedGeneral => _selectedGeneral;
    public IReadOnlyList<SelectableGeneral> RegisteredGenerals => _registeredGenerals;
    public int GeneralCount => _registeredGenerals.Count;
    public bool HasSelection => _selectedGeneral != null;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GeneralSelectionManager] Duplicate instance destroyed!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        Debug.Log("✓ GeneralSelectionManager initialized");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    #region Registration
    
    /// <summary>
    /// Register a general with the selection system.
    /// Called automatically by SelectableGeneral.OnEnable().
    /// </summary>
    public void RegisterGeneral(SelectableGeneral general)
    {
        if (general == null) return;
        
        if (_registeredGenerals.Contains(general))
        {
            Debug.LogWarning($"[GeneralSelectionManager] {general.DisplayName} already registered");
            return;
        }
        
        _registeredGenerals.Add(general);
        OnGeneralRegistered?.Invoke(general);
        
        Debug.Log($"[GeneralSelectionManager] Registered: {general.DisplayName} (Total: {_registeredGenerals.Count})");
        
        // Auto-select first if nothing selected
        if (autoSelectFirst && _selectedGeneral == null)
        {
            Select(general);
        }
    }
    
    /// <summary>
    /// Unregister a general from the selection system.
    /// Called automatically by SelectableGeneral.OnDisable().
    /// </summary>
    public void UnregisterGeneral(SelectableGeneral general)
    {
        if (general == null) return;
        
        if (!_registeredGenerals.Contains(general))
        {
            return;
        }
        
        // If this was the selected general, deselect it
        if (_selectedGeneral == general)
        {
            Deselect();
            
            // Try to select another general
            if (autoSelectFirst && _registeredGenerals.Count > 1)
            {
                foreach (var g in _registeredGenerals)
                {
                    if (g != general && g != null)
                    {
                        Select(g);
                        break;
                    }
                }
            }
        }
        
        _registeredGenerals.Remove(general);
        OnGeneralUnregistered?.Invoke(general);
        
        Debug.Log($"[GeneralSelectionManager] Unregistered: {general.DisplayName} (Remaining: {_registeredGenerals.Count})");
    }
    
    #endregion
    
    #region Selection
    
    /// <summary>
    /// Select a general to receive input.
    /// </summary>
    public void Select(SelectableGeneral general)
    {
        if (general == null)
        {
            Debug.LogWarning("[GeneralSelectionManager] Cannot select null general");
            return;
        }
        
        if (!_registeredGenerals.Contains(general))
        {
            Debug.LogWarning($"[GeneralSelectionManager] {general.DisplayName} is not registered!");
            return;
        }
        
        // Skip if already selected
        if (_selectedGeneral == general)
        {
            Debug.Log($"[GeneralSelectionManager] {general.DisplayName} is already selected");
            return;
        }
        
        // Deselect current
        SelectableGeneral previousGeneral = _selectedGeneral;
        if (previousGeneral != null)
        {
            previousGeneral.OnDeselected();
            OnGeneralDeselected?.Invoke(previousGeneral);
        }
        
        // Select new
        _selectedGeneral = general;
        _selectedGeneral.OnSelected();
        OnGeneralSelected?.Invoke(_selectedGeneral);
        
        Debug.Log($"✓ [GeneralSelectionManager] Selected: {general.DisplayName}");
    }
    
    /// <summary>
    /// Select a general by index in the registered list.
    /// </summary>
    public void SelectByIndex(int index)
    {
        if (index < 0 || index >= _registeredGenerals.Count)
        {
            Debug.LogWarning($"[GeneralSelectionManager] Invalid index: {index}");
            return;
        }
        
        Select(_registeredGenerals[index]);
    }
    
    /// <summary>
    /// Deselect the current general (no one receives input).
    /// </summary>
    public void Deselect()
    {
        if (_selectedGeneral == null) return;
        
        SelectableGeneral previous = _selectedGeneral;
        previous.OnDeselected();
        _selectedGeneral = null;
        
        OnGeneralDeselected?.Invoke(previous);
        
        Debug.Log($"[GeneralSelectionManager] Deselected: {previous.DisplayName}");
    }
    
    /// <summary>
    /// Cycle to the next general in the list.
    /// </summary>
    public void SelectNext()
    {
        if (_registeredGenerals.Count == 0) return;
        
        int currentIndex = _selectedGeneral != null 
            ? _registeredGenerals.IndexOf(_selectedGeneral) 
            : -1;
        
        int nextIndex = (currentIndex + 1) % _registeredGenerals.Count;
        Select(_registeredGenerals[nextIndex]);
    }
    
    /// <summary>
    /// Cycle to the previous general in the list.
    /// </summary>
    public void SelectPrevious()
    {
        if (_registeredGenerals.Count == 0) return;
        
        int currentIndex = _selectedGeneral != null 
            ? _registeredGenerals.IndexOf(_selectedGeneral) 
            : 0;
        
        int prevIndex = (currentIndex - 1 + _registeredGenerals.Count) % _registeredGenerals.Count;
        Select(_registeredGenerals[prevIndex]);
    }
    
    #endregion
    
    #region Queries
    
    /// <summary>
    /// Check if a specific general is currently selected.
    /// </summary>
    public bool IsSelected(SelectableGeneral general)
    {
        return _selectedGeneral != null && _selectedGeneral == general;
    }
    
    /// <summary>
    /// Get the index of a general in the registered list.
    /// </summary>
    public int GetIndex(SelectableGeneral general)
    {
        return _registeredGenerals.IndexOf(general);
    }
    
    /// <summary>
    /// Find a general by name.
    /// </summary>
    public SelectableGeneral FindByName(string name)
    {
        foreach (var general in _registeredGenerals)
        {
            if (general.DisplayName == name)
                return general;
        }
        return null;
    }
    
    #endregion
    
    #region Debug
    
    [ContextMenu("Log Status")]
    private void LogStatus()
    {
        Debug.Log($"=== GeneralSelectionManager Status ===");
        Debug.Log($"Registered: {_registeredGenerals.Count}");
        Debug.Log($"Selected: {(_selectedGeneral != null ? _selectedGeneral.DisplayName : "None")}");
        
        for (int i = 0; i < _registeredGenerals.Count; i++)
        {
            var g = _registeredGenerals[i];
            string marker = g == _selectedGeneral ? " [SELECTED]" : "";
            Debug.Log($"  {i}: {g.DisplayName}{marker}");
        }
    }
    
    [ContextMenu("Select Next")]
    private void DebugSelectNext()
    {
        SelectNext();
    }
    
    #endregion
}
