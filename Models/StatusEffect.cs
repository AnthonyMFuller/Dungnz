namespace Dungnz.Models;

/// <summary>
/// Enumerates the status conditions that can be applied to the player or an enemy via abilities,
/// weapon procs, or enemy attacks. Debuffs are tracked as <see cref="ActiveEffect"/> instances
/// and tick down each combat turn.
/// </summary>
public enum StatusEffect
{
    /// <summary>Deals periodic damage each turn; applied by poisonous enemies or the PoisonDart ability.</summary>
    Poison,

    /// <summary>Causes the target to lose HP each turn from a bleeding wound; applied by weapons with <see cref="Item.AppliesBleedOnHit"/>.</summary>
    Bleed,

    /// <summary>Prevents the target from taking any action for the duration; effectively skips their turn.</summary>
    Stun,

    /// <summary>Restores a small amount of HP to the bearer each turn; a beneficial buff.</summary>
    Regen,

    /// <summary>Temporarily increases the bearer's defense stat, reducing damage taken while active.</summary>
    Fortified,

    /// <summary>Temporarily reduces the bearer's attack stat, lowering damage dealt while active.</summary>
    Weakened,

    /// <summary>Reduces attack damage by 25% and makes the target attack last in turn order.</summary>
    Slow,

    /// <summary>Increases the bearer's attack power by 25%, applied by Battle Cry ability.</summary>
    BattleCry,

    /// <summary>Deals 8 fire damage per turn; applied by Infernal Dragon's Flame Breath.</summary>
    Burn
}
