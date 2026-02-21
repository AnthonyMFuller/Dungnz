namespace Dungnz.Engine;
using Dungnz.Models;

/// <summary>
/// Defines the contract for resolving a combat encounter between the player and an enemy,
/// returning the outcome once the fight concludes.
/// </summary>
public interface ICombatEngine
{
    /// <summary>
    /// Executes a full combat encounter between <paramref name="player"/> and <paramref name="enemy"/>,
    /// processing turns until the player wins, flees, or is killed.
    /// </summary>
    /// <param name="player">The player character participating in combat.</param>
    /// <param name="enemy">The enemy the player is fighting.</param>
    /// <returns>
    /// A <see cref="CombatResult"/> indicating whether the player won, fled, or died.
    /// </returns>
    CombatResult RunCombat(Player player, Enemy enemy);
}
