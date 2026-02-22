namespace Dungnz.Models;

/// <summary>
/// Indicates the power tier of an <see cref="Item"/>, used to colour-code item names in the UI
/// and to communicate rarity/progression level to the player at a glance.
/// </summary>
public enum ItemTier
{
    /// <summary>White — Tier 1 items found at levels 1–3. Basic gear with no special mechanics.</summary>
    Common,

    /// <summary>Green — Tier 2 items found at levels 4–6. May include special on-hit or passive mechanics.</summary>
    Uncommon,

    /// <summary>BrightCyan — Tier 3 items found at level 7+. Unique effects; the best gear in the dungeon.</summary>
    Rare
}
