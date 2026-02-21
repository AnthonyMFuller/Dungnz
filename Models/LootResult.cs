namespace Dungnz.Models;

/// <summary>
/// An immutable value type returned by <see cref="LootTable.RollDrop"/> that bundles the
/// optional item drop and the gold amount yielded by a defeated enemy into a single result.
/// </summary>
public readonly struct LootResult
{
    /// <summary>
    /// Gets the item dropped by the enemy, or <c>null</c> if the loot roll produced no item.
    /// </summary>
    public Item? Item { get; init; }

    /// <summary>
    /// Gets the amount of gold dropped by the enemy. May be 0 if the loot table's gold range
    /// is (0, 0) or the roll produced no gold.
    /// </summary>
    public int Gold { get; init; }
}
