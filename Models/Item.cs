namespace Dungnz.Models;

/// <summary>
/// Represents an item that can exist in the dungeon, a room, or a player's inventory.
/// Items may be consumables (potions), equippable gear (weapons, armor, accessories),
/// or currency (gold). Stat-altering properties take effect when the item is equipped or used.
/// </summary>
public class Item
{
    /// <summary>Gets or sets the display name of the item shown in inventory and loot lists.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category of this item, determining how it can be used or equipped.</summary>
    public ItemType Type { get; set; }

    /// <summary>
    /// Gets or sets a general-purpose stat modifier applied to the player's MaxHP when this item
    /// is equipped. Positive values increase MaxHP; negative values reduce it.
    /// </summary>
    public int StatModifier { get; set; }

    /// <summary>Gets or sets the flavour text or mechanical description shown when the player inspects this item.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bonus added to the player's <see cref="Player.Attack"/> when this item is equipped.
    /// Only meaningful for <see cref="ItemType.Weapon"/> items.
    /// </summary>
    public int AttackBonus { get; set; }

    /// <summary>
    /// Gets or sets the bonus added to the player's <see cref="Player.Defense"/> when this item is equipped.
    /// Only meaningful for <see cref="ItemType.Armor"/> items.
    /// </summary>
    public int DefenseBonus { get; set; }

    /// <summary>
    /// Gets or sets the amount of HP restored to the player when this consumable item is used.
    /// Only relevant for <see cref="ItemType.Consumable"/> items.
    /// </summary>
    public int HealAmount { get; set; }

    /// <summary>
    /// Gets or sets whether each successful attack made while this weapon is equipped applies the
    /// Bleed status effect to the target.
    /// </summary>
    public bool AppliesBleedOnHit { get; set; }

    /// <summary>
    /// Gets or sets whether equipping this item grants the player immunity to the Poison status effect.
    /// </summary>
    public bool PoisonImmunity { get; set; }

    /// <summary>
    /// Gets or sets the bonus added to the player's <see cref="Player.MaxMana"/> when this item is equipped.
    /// </summary>
    public int MaxManaBonus { get; set; }

    /// <summary>
    /// Gets or sets the additional flat dodge chance granted to the player while this item is equipped,
    /// expressed as a fraction in the range [0, 1] (e.g., <c>0.10</c> for a 10 % bonus).
    /// </summary>
    public float DodgeBonus { get; set; }

    /// <summary>
    /// Gets or sets whether this item can be placed in an equipment slot. Consumables and gold
    /// items are not equippable; weapons, armor, and accessories are.
    /// </summary>
    public bool IsEquippable { get; set; }

    /// <summary>
    /// Gets or sets the amount of mana restored to the player when this consumable item is used.
    /// Distinct from <see cref="MaxManaBonus"/>, which permanently raises the player's mana cap.
    /// </summary>
    public int ManaRestore { get; set; }

    /// <summary>
    /// Gets or sets the carry weight of this item. Defaults to <c>1</c>.
    /// Used by <see cref="Dungnz.Systems.InventoryManager"/> to enforce the per-player weight limit.
    /// </summary>
    public int Weight { get; set; } = 1;
}
