namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Handles applying and evaluating status effects triggered by combat events such as
/// enemy death, boss revival checks, and end-of-combat cleanup.
/// </summary>
public interface IStatusEffectApplicator
{
    /// <summary>Applies on-death status effects from the enemy to the player (e.g. CursedZombie Weakened).</summary>
    void ApplyOnDeathEffects(Player player, Enemy enemy);

    /// <summary>
    /// Checks whether the enemy should revive or trigger on-death mechanics.
    /// Returns true if the enemy revived and combat should continue.
    /// </summary>
    bool CheckOnDeathEffects(Player player, Enemy enemy, Random rng);

    /// <summary>Clears all transient combat status effects from both combatants at the end of a fight.</summary>
    void ResetCombatEffects(Player player, Enemy enemy);
}
