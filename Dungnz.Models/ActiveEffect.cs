namespace Dungnz.Models;

/// <summary>
/// Represents an instance of a <see cref="StatusEffect"/> currently applied to a combatant,
/// tracking how many turns remain before it expires. One <see cref="ActiveEffect"/> is created
/// per application; the same effect can stack by creating additional instances.
/// </summary>
public class ActiveEffect
{
    /// <summary>Gets or sets the type of status condition this instance represents.</summary>
    public StatusEffect Effect { get; set; }

    /// <summary>
    /// Gets or sets the number of combat turns remaining before this effect expires and is removed.
    /// Decremented at the start or end of each turn depending on the combat system's tick order.
    /// </summary>
    public int RemainingTurns { get; set; }

    /// <summary>
    /// Initialises a new <see cref="ActiveEffect"/> for the given status condition with the
    /// specified duration.
    /// </summary>
    /// <param name="effect">The <see cref="StatusEffect"/> to apply.</param>
    /// <param name="duration">The number of turns the effect should last.</param>
    public ActiveEffect(StatusEffect effect, int duration) { Effect = effect; RemainingTurns = duration; }

    /// <summary>
    /// Gets whether this effect is a debuff (harmful condition) — specifically
    /// <see cref="StatusEffect.Poison"/>, <see cref="StatusEffect.Bleed"/>,
    /// <see cref="StatusEffect.Stun"/>, <see cref="StatusEffect.Weakened"/>, or <see cref="StatusEffect.Slow"/>.
    /// </summary>
    public bool IsDebuff => Effect is StatusEffect.Poison or StatusEffect.Bleed or StatusEffect.Stun 
        or StatusEffect.Weakened or StatusEffect.Slow or StatusEffect.Burn
        or StatusEffect.Freeze or StatusEffect.Silence or StatusEffect.Curse;

    /// <summary>
    /// Gets whether this effect is a buff (beneficial condition), i.e. the inverse of
    /// <see cref="IsDebuff"/>.
    /// </summary>
    public bool IsBuff => !IsDebuff;
}
