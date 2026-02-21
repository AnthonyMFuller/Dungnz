namespace Dungnz.Models;

/// <summary>
/// Categorises an <see cref="Item"/> by its function, determining how the player can interact
/// with it (equip into a slot, consume for an instant effect, or collect as currency).
/// </summary>
public enum ItemType
{
    /// <summary>An offensive weapon equipped in the weapon slot; increases the player's attack stat.</summary>
    Weapon,

    /// <summary>Defensive body armour equipped in the armor slot; increases the player's defense stat.</summary>
    Armor,

    /// <summary>A wearable accessory equipped in the accessory slot; may grant special bonuses such as dodge chance or max mana.</summary>
    Accessory,

    /// <summary>A single-use item consumed from the inventory for an immediate effect such as healing.</summary>
    Consumable,

    /// <summary>Currency picked up directly from a room or enemy drop; added to the player's gold total rather than held as an inventory item.</summary>
    Gold
}
