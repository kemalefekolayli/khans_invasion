using UnityEngine;
using UnityEngine.InputSystem;
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
    
    [Header("Camera")]
    [Tooltip("Camera controller for focusing on selected general")]
    [SerializeField] private CameraController cameraController;
    
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
            GameLog.Warning(GameLogCategory.Core, "[GeneralSelectionManager] Duplicate instance destroyed!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Try to find CameraController if not assigned
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
        }
        

    }
    
    private void Update()
    {
        // V key to cycle between generals
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            GameLog.Log(GameLogCategory.Core, "[GeneralSelectionManager] V pressed - cycling to next general");
            SelectNext();
        }
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
            GameLog.Warning(GameLogCategory.Core, $"[GeneralSelectionManager] {general.DisplayName} already registered");
            return;
        }
        
        _registeredGenerals.Add(general);
        OnGeneralRegistered?.Invoke(general);
        
        GameLog.Log(GameLogCategory.Core, $"[GeneralSelectionManager] Registered: {general.DisplayName} (Total: {_registeredGenerals.Count})");
        
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
        
        GameLog.Log(GameLogCategory.Core, $"[GeneralSelectionManager] Unregistered: {general.DisplayName} (Remaining: {_registeredGenerals.Count})");
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
            GameLog.Warning(GameLogCategory.Core, "[GeneralSelectionManager] Cannot select null general");
            return;
        }

        if (general.IsControlLocked)
        {
            GameLog.Warning(GameLogCategory.Core, $"[GeneralSelectionManager] {general.DisplayName} is locked and cannot be selected");
            return;
        }
        
        if (!_registeredGenerals.Contains(general))
        {
            GameLog.Warning(GameLogCategory.Core, $"[GeneralSelectionManager] {general.DisplayName} is not registered!");
            return;
        }
        
        // Skip if already selected
        if (_selectedGeneral == general)
        {
            GameLog.Log(GameLogCategory.Core, $"[GeneralSelectionManager] {general.DisplayName} is already selected");
            return;
        }
        
        // Close any open province panels when switching generals
        GameEvents.ProvincePanelClosed();
        
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
        
        // Focus camera on new selection
        if (cameraController != null)
        {
            cameraController.SetCameraPosition(_selectedGeneral.transform.position);
        }
        
        GameLog.Log(GameLogCategory.Core, $"✓ [GeneralSelectionManager] Selected: {general.DisplayName}");
    }
    
    /// <summary>
    /// Select a general by index in the registered list.
    /// </summary>
    public void SelectByIndex(int index)
    {
        if (index < 0 || index >= _registeredGenerals.Count)
        {
            GameLog.Warning(GameLogCategory.Core, $"[GeneralSelectionManager] Invalid index: {index}");
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
        
        GameLog.Log(GameLogCategory.Core, $"[GeneralSelectionManager] Deselected: {previous.DisplayName}");
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
        
        for (int offset = 1; offset <= _registeredGenerals.Count; offset++)
        {
            int nextIndex = (currentIndex + offset) % _registeredGenerals.Count;
            SelectableGeneral candidate = _registeredGenerals[nextIndex];
            if (candidate != null && !candidate.IsControlLocked)
            {
                Select(candidate);
                return;
            }
        }
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
        
        for (int offset = 1; offset <= _registeredGenerals.Count; offset++)
        {
            int prevIndex = (currentIndex - offset + _registeredGenerals.Count) % _registeredGenerals.Count;
            SelectableGeneral candidate = _registeredGenerals[prevIndex];
            if (candidate != null && !candidate.IsControlLocked)
            {
                Select(candidate);
                return;
            }
        }
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
        GameLog.Log(GameLogCategory.Core, $"=== GeneralSelectionManager Status ===");
        GameLog.Log(GameLogCategory.Core, $"Registered: {_registeredGenerals.Count}");
        GameLog.Log(GameLogCategory.Core, $"Selected: {(_selectedGeneral != null ? _selectedGeneral.DisplayName : "None")}");
        
        for (int i = 0; i < _registeredGenerals.Count; i++)
        {
            var g = _registeredGenerals[i];
            string marker = g == _selectedGeneral ? " [SELECTED]" : "";
            GameLog.Log(GameLogCategory.Core, $"  {i}: {g.DisplayName}{marker}");
        }
    }
    
    [ContextMenu("Select Next")]
    private void DebugSelectNext()
    {
        SelectNext();
    }
    
    #endregion
}
