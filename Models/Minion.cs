namespace Dungnz.Models;

/// <summary>
/// Represents a summoned minion that fights alongside the player, dealing damage each combat turn.
/// </summary>
public class Minion
{
    /// <summary>Gets or sets the minion's display name shown in combat messages.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the minion's current hit points.</summary>
    public int HP { get; set; }

    /// <summary>Gets or sets the minion's maximum hit points.</summary>
    public int MaxHP { get; set; }

    /// <summary>Gets or sets the minion's attack power used to calculate damage dealt to the enemy.</summary>
    public int ATK { get; set; }

    /// <summary>Gets or sets the flavour text displayed when the minion attacks.</summary>
    public string AttackFlavorText { get; set; } = string.Empty;
}
