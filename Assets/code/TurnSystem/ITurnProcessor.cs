/// <summary>
/// Interface for components that need to process logic at the end of each turn.
/// Implement this interface and register with TurnManager to receive turn end callbacks.
/// 
/// Examples: Income calculation, population growth, army upkeep, etc.
/// </summary>
public interface ITurnProcessor
{
    /// <summary>
    /// Called by TurnManager at the end of each turn.
    /// </summary>
    /// <param name="turnNumber">The turn number that just ended</param>
    void ProcessTurnEnd(int turnNumber);
    
    /// <summary>
    /// Priority for processing order (lower = earlier).
    /// Default implementations should use 0.
    /// </summary>
    int ProcessingPriority { get; }
}
