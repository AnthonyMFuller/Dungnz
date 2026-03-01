using Dungnz.Models;

namespace Dungnz.Engine;

/// <summary>
/// Defines the contract for enemy AI decision-making during combat.
/// Each enemy type can implement its own TakeTurn logic for composable,
/// testable AI behaviors.
/// </summary>
public interface IEnemyAI
{
    /// <summary>
    /// Executes the enemy's turn logic: choosing attacks, healing, applying
    /// status effects, or other combat actions.
    /// </summary>
    /// <param name="self">The enemy performing the action.</param>
    /// <param name="player">The player being targeted.</param>
    /// <param name="context">Combat context with round number, player HP%, and floor info.</param>
    void TakeTurn(Enemy self, Player player, CombatContext context);
}

/// <summary>
/// Provides contextual information about the current combat state,
/// used by <see cref="IEnemyAI"/> implementations to make tactical decisions.
/// </summary>
/// <param name="RoundNumber">The current combat round (1-based).</param>
/// <param name="PlayerHPPercent">The player's current HP as a fraction of MaxHP (0.0–1.0).</param>
/// <param name="CurrentFloor">The dungeon floor the combat is occurring on.</param>
public record CombatContext(int RoundNumber, double PlayerHPPercent, int CurrentFloor);
