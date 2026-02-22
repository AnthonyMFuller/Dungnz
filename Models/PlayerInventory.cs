namespace Dungnz.Models;

public partial class Player
{
    /// <summary>The maximum number of items the player's inventory can hold.</summary>
    public const int MaxInventorySize = 20;

    /// <summary>
    /// Gets the collection of items the player is currently carrying but not equipped.
    /// Equipped items are removed from this list and tracked in the corresponding equipment slot.
    /// </summary>
    public List<Item> Inventory { get; set; } = new();

    /// <summary>Gets the amount of gold the player is currently carrying.</summary>
    public int Gold { get; set; }


    /// <summary>
    /// Increases the player's gold by the specified amount.
    /// </summary>
    /// <param name="amount">The positive number of gold coins to award.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is negative.</exception>
    public void AddGold(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Gold amount cannot be negative.", nameof(amount));
        Gold += amount;
    }

    /// <summary>
    /// Deducts the specified amount of gold from the player's total.
    /// </summary>
    /// <param name="amount">The positive number of gold coins to spend.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the player does not have enough gold.</exception>
    public void SpendGold(int amount)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (Gold < amount) throw new InvalidOperationException("Not enough gold.");
        Gold -= amount;
    }
}
