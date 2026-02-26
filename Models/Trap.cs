namespace Dungnz.Models;

/// <summary>
/// Represents a trap placed by the player that triggers once before an enemy attack,
/// dealing damage and optionally applying a status effect to the enemy.
/// </summary>
public class Trap
{
    /// <summary>Gets or sets the trap's display name shown in combat messages.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the fraction of the player's ATK that the trap deals as damage.</summary>
    public float DamagePercent { get; set; }  // % of player ATK

    /// <summary>Gets or sets the optional status effect applied to the enemy when this trap triggers.</summary>
    public StatusEffect? AppliedStatus { get; set; }  // nullable

    /// <summary>Gets or sets the number of turns the applied status effect lasts.</summary>
    public int StatusDuration { get; set; }

    /// <summary>Gets or sets the flavour text displayed when the trap fires.</summary>
    public string FlavorText { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this trap has already fired during the current combat.</summary>
    public bool Triggered { get; set; } = false;
}
