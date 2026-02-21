namespace Dungnz.Engine;

/// <summary>
/// Immutable record that captures everything that happened during a single combat turn,
/// enabling the combat log to display a concise history of recent exchanges.
/// </summary>
/// <param name="Actor">The display name of the entity that took the action (e.g. "You" or the enemy's name).</param>
/// <param name="Action">A short label describing the action taken (e.g. "Attack").</param>
/// <param name="Damage">The amount of damage dealt this turn (0 when the attack was dodged).</param>
/// <param name="IsCrit">Whether the hit was a critical strike that doubled damage.</param>
/// <param name="IsDodge">Whether the attack was completely avoided by the target.</param>
/// <param name="StatusApplied">The name of any status effect applied this turn, or <see langword="null"/> if none.</param>
public record CombatTurn(string Actor, string Action, int Damage, bool IsCrit, bool IsDodge, string? StatusApplied);
