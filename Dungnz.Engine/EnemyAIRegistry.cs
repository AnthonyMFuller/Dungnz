namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;

/// <summary>
/// Registry that maps enemy types to their AI implementations.
/// Provides a centralized lookup for enemy combat behaviors.
/// </summary>
public static class EnemyAIRegistry
{
    private static readonly Dictionary<Type, IEnemyAI> _aiByType = new()
    {
        { typeof(Goblin), new GoblinAI() },
        { typeof(Skeleton), new SkeletonAI() }
    };
    
    /// <summary>
    /// Gets the AI implementation for the given enemy, or null if none is registered.
    /// </summary>
    public static IEnemyAI? GetAI(Enemy enemy)
    {
        if (_aiByType.TryGetValue(enemy.GetType(), out var ai))
            return ai;
        return null;
    }
    
    /// <summary>
    /// Registers a custom AI implementation for a specific enemy type.
    /// Used by tests or to add new AI behaviors at runtime.
    /// </summary>
    public static void RegisterAI<T>(IEnemyAI ai) where T : Enemy
    {
        _aiByType[typeof(T)] = ai;
    }
}
