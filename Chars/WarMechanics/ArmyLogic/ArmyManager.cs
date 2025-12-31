using UnityEngine;
using System.Collections.Generic;


public class ArmyManager : MonoBehaviour
{
    public static ArmyManager Instance { get; private set; }
    
    // Army tracking
    private List<Army> allArmies = new List<Army>();
    private List<Army> playerArmies = new List<Army>();
    private List<Army> enemyArmies = new List<Army>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✓ ArmyManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        GameEvents.OnArmySpawned += OnArmySpawned;
        GameEvents.OnArmyDestroyed += OnArmyDestroyed;
    }
    
    private void OnDisable()
    {
        GameEvents.OnArmySpawned -= OnArmySpawned;
        GameEvents.OnArmyDestroyed -= OnArmyDestroyed;
    }
    
    private void OnArmySpawned(Army army, General general)
    {
        RegisterArmy(army);
    }
    
    private void OnArmyDestroyed(Army army)
    {
        UnregisterArmy(army);
    }

    public void RegisterArmy(Army army)
    {
        if (army == null || allArmies.Contains(army)) return;
        
        allArmies.Add(army);
        
        if (army.IsPlayerArmy)
            playerArmies.Add(army);
        else
            enemyArmies.Add(army);
        
        Debug.Log($"[ArmyManager] Registered army. Total: {allArmies.Count}");
    }
    

    public void UnregisterArmy(Army army)
    {
        if (army == null) return;
        
        allArmies.Remove(army);
        playerArmies.Remove(army);
        enemyArmies.Remove(army);
        
        Debug.Log($"[ArmyManager] Unregistered army. Remaining: {allArmies.Count}");
    }
    
    // ===== QUERIES =====
    
    public List<Army> GetAllArmies() => new List<Army>(allArmies);
    public List<Army> GetPlayerArmies() => new List<Army>(playerArmies);
    public List<Army> GetEnemyArmies() => new List<Army>(enemyArmies);
    
    public int TotalArmyCount => allArmies.Count;
    public int PlayerArmyCount => playerArmies.Count;
    public int EnemyArmyCount => enemyArmies.Count;
    

    public float TotalPlayerSoldiers
    {
        get
        {
            float total = 0;
            foreach (var army in playerArmies)
            {
                if (army != null)
                    total += army.ArmySize;
            }
            return total;
        }
    }
    

    public float TotalPlayerStrength
    {
        get
        {
            float total = 0;
            foreach (var army in playerArmies)
            {
                if (army != null)
                    total += army.GetEffectiveStrength();
            }
            return total;
        }
    }
    

    public List<Army> GetArmiesNear(Vector3 position, float radius)
    {
        List<Army> nearby = new List<Army>();
        float radiusSqr = radius * radius;
        
        foreach (var army in allArmies)
        {
            if (army == null) continue;
            
            float distSqr = (army.transform.position - position).sqrMagnitude;
            if (distSqr <= radiusSqr)
            {
                nearby.Add(army);
            }
        }
        
        return nearby;
    }

    public List<Army> GetEnemyArmiesNear(Vector3 position, float radius)
    {
        List<Army> nearby = new List<Army>();
        float radiusSqr = radius * radius;
        
        foreach (var army in enemyArmies)
        {
            if (army == null) continue;
            
            float distSqr = (army.transform.position - position).sqrMagnitude;
            if (distSqr <= radiusSqr)
            {
                nearby.Add(army);
            }
        }
        
        return nearby;
    }
}