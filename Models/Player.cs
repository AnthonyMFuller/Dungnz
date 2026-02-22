namespace Dungnz.Models;

/// <summary>
/// Represents the player character, tracking combat stats, inventory, equipment, mana, and
/// progression throughout the dungeon crawl. Exposes methods for taking damage, healing,
/// managing gold and XP, equipping items, and levelling up.
/// </summary>
public partial class Player
{
    /// <summary>Gets or sets the player's display name shown in UI and combat messages.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of status effects currently active on the player. Used for save/load
    /// persistence and is managed directly by <see cref="Dungnz.Systems.StatusEffectManager"/>.
    /// </summary>
    public List<ActiveEffect> ActiveEffects { get; } = new();
}
