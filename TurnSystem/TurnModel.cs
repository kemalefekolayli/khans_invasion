using UnityEngine;

public class TurnModel : MonoBehaviour
{
    public enum TurnPhase
    {
        PlayerTurn,
        EnemyTurn,
    }
    // add logic somewhere so that when its enemyturn we cant move our characters
    public TurnPhase currentPhase;
    public int turnNumber = 1;
    void Start()
    {
        turnNumber = 1;
        currentPhase = TurnPhase.PlayerTurn;
    }

    public void AdvanceTurn()
    {
        switch (currentPhase)
        {
            case TurnPhase.PlayerTurn:
                currentPhase = TurnPhase.EnemyTurn;
                break;
            case TurnPhase.EnemyTurn:
                currentPhase = TurnPhase.PlayerTurn;
                break;
        }
    }
}