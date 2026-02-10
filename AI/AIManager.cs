using UnityEngine;
using System.Collections.Generic;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logAISummary = true;

    private List<AINationController> aiNations = new List<AINationController>();
    public List<AINationController> AINations => aiNations; // For Debugger
    private bool initialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        TurnManager.OnAITurnsStart += OnAITurnsStart;
        GameEvents.OnProvincesAssigned += OnProvincesAssigned;
    }

    private void OnDisable()
    {
        TurnManager.OnAITurnsStart -= OnAITurnsStart;
        GameEvents.OnProvincesAssigned -= OnProvincesAssigned;
    }

    private void OnProvincesAssigned()
    {
        if (initialized) return;
        InitializeAINations();
    }

    private void InitializeAINations()
    {
        NationLoader loader = FindFirstObjectByType<NationLoader>();
        if (loader == null)
        {
            Debug.LogError("[AIManager] NationLoader not found!");
            return;
        }

        aiNations.Clear();

        foreach (NationModel nation in loader.allNations)
        {
            if (nation.isPlayer) continue;
            if (nation.provinceList == null || nation.provinceList.Count == 0) continue;

            AINationController controller = new AINationController(nation);
            aiNations.Add(controller);
        }

        initialized = true;
        Debug.Log($"[AIManager] Initialized {aiNations.Count} AI nations");
    }

    private void OnAITurnsStart()
    {
        if (!initialized)
        {
            Debug.LogWarning("[AIManager] AI turns started but not initialized yet!");
            return;
        }

        int turnNumber = TurnManager.Instance != null ? TurnManager.Instance.CurrentTurn : 0;

        Debug.Log($"[AIManager] === Processing AI Turns (Turn {turnNumber}) ===");

        foreach (AINationController controller in aiNations)
        {
            controller.ProcessTurn(turnNumber);
        }

        if (logAISummary)
        {
            LogAISummary();
        }
    }

    private void LogAISummary()
    {
        Debug.Log("[AIManager] === AI Summary ===");
        int expanding = 0, fortifying = 0, idle = 0;

        foreach (AINationController controller in aiNations)
        {
            switch (controller.StateMachine.CurrentState)
            {
                case AIState.Expanding: expanding++; break;
                case AIState.Fortifying: fortifying++; break;
                case AIState.Idle: idle++; break;
            }
        }

        Debug.Log($"[AIManager] Expanding: {expanding} | Fortifying: {fortifying} | Idle: {idle}");
    }
}
