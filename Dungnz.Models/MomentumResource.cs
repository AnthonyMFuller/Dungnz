namespace Dungnz.Models;

/// <summary>
/// Represents a per-class momentum resource that builds up during combat and unlocks a charged
/// ability when full. Used by Warrior (Fury, max 5), Mage (Arcane Charge, max 3),
/// Paladin (Devotion, max 4), and Ranger (Focus, max 3).
/// Rogue uses <see cref="Player.ComboPoints"/> instead and does not receive a Momentum instance.
/// </summary>
public sealed class MomentumResource
{
    /// <summary>Gets the current momentum charge, always in the range [0, <see cref="Maximum"/>].</summary>
    public int Current { get; private set; }

    /// <summary>Gets the maximum momentum this resource can hold before it becomes charged.</summary>
    public int Maximum { get; }

    /// <summary>
    /// Gets a value indicating whether this resource is fully charged
    /// (i.e. <see cref="Current"/> has reached <see cref="Maximum"/>).
    /// </summary>
    public bool IsCharged => Current >= Maximum;

    /// <summary>
    /// Initialises a new <see cref="MomentumResource"/> with the given maximum capacity.
    /// </summary>
    /// <param name="maximum">The maximum charge level; must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maximum"/> is not positive.</exception>
    public MomentumResource(int maximum)
    {
        if (maximum <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum must be greater than zero.");
        Maximum = maximum;
    }

    /// <summary>
    /// Adds <paramref name="amount"/> points of momentum, clamping the result to [0, <see cref="Maximum"/>].
    /// </summary>
    /// <param name="amount">Number of momentum points to add. Defaults to 1.</param>
    public void Add(int amount = 1) => Current = Math.Clamp(Current + amount, 0, Maximum);

    /// <summary>Resets momentum to zero (called at the start or end of each combat).</summary>
    public void Reset() => Current = 0;

    /// <summary>
    /// Atomically checks whether this resource is charged and, if so, resets it to zero.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the resource was charged (and has now been reset to zero);
    /// <see langword="false"/> if the resource was not charged (no state change).
    /// </returns>
    public bool Consume()
    {
        if (!IsCharged) return false;
        Reset();
        return true;
    }
}
