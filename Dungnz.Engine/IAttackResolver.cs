namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Handles all attack calculation logic for combat, including hit/dodge/crit rolls
/// and damage application for both player and enemy attacks.
/// </summary>
public interface IAttackResolver
{
    /// <summary>
    /// The current combat turn number. Set by <see cref="CombatEngine"/> after each
    /// increment so that early-combat dodge bonuses (e.g. Eagle Eye) apply correctly.
    /// </summary>
    int CombatTurn { get; set; }

    /// <summary>
    /// Provides the run-scoped stats accumulator so the resolver can record damage
    /// dealt/taken. Must be called at the start of each combat encounter.
    /// </summary>
    void SetStats(RunStats stats);

    /// <summary>Resolves the player attack action against the enemy for one turn.</summary>
    void PerformPlayerAttack(Player player, Enemy enemy);

    /// <summary>Rolls a dodge check using the given defense value.</summary>
    bool RollDodge(int defense);

    /// <summary>Rolls a dodge check for the player, incorporating equipment and class bonuses.</summary>
    bool RollPlayerDodge(Player player);

    /// <summary>Rolls a critical hit check, optionally incorporating player equipment bonuses.</summary>
    bool RollCrit(Player? player = null);
}
