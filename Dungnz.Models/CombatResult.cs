namespace Dungnz.Models;

/// <summary>
/// Describes how a combat encounter ended, allowing post-combat logic to branch on victory,
/// escape, or defeat.
/// </summary>
public enum CombatResult
{
    /// <summary>The player defeated the enemy; XP and loot rewards should be applied.</summary>
    Won,

    /// <summary>The player successfully fled the encounter; no rewards are granted and the enemy remains in the room.</summary>
    Fled,

    /// <summary>The player's HP reached 0; the game-over sequence should be triggered.</summary>
    PlayerDied
}
