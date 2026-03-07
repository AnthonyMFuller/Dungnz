namespace Dungnz.Models;

/// <summary>
/// Describes the outcome of a player's attempt to use an item from inventory,
/// allowing the calling code to display the appropriate feedback message.
/// </summary>
public enum UseResult
{
    /// <summary>The item was found in the inventory and its effect was applied successfully.</summary>
    Used,

    /// <summary>The item was found but cannot be used in the current context (e.g., a non-consumable item or full HP).</summary>
    NotUsable,

    /// <summary>No item matching the requested name was found in the player's inventory.</summary>
    NotFound
}
