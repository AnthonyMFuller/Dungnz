namespace Dungnz.Models;

public partial class Player
{
    /// <summary>Gets the weapon currently equipped by the player, or <c>null</c> if the weapon slot is empty.</summary>
    public Item? EquippedWeapon { get; set; }

    /// <summary>Gets the armor currently equipped by the player, or <c>null</c> if the armor slot is empty.</summary>
    public Item? EquippedArmor { get; set; }

    /// <summary>Gets the accessory currently equipped by the player, or <c>null</c> if the accessory slot is empty.</summary>
    public Item? EquippedAccessory { get; set; }

    /// <summary>
    /// Gets the sum of all <see cref="Item.DodgeBonus"/> values across every currently equipped item,
    /// expressed as a fraction in [0, 1]. Recalculated automatically on equip and unequip.
    /// </summary>
    public float DodgeBonus { get; set; }

    /// <summary>
    /// Gets whether the player is currently immune to the Poison status effect.
    /// <c>true</c> if any equipped item has <see cref="Item.PoisonImmunity"/> set; recalculated on equip and unequip.
    /// </summary>
    public bool PoisonImmune { get; set; }

    /// <summary>
    /// Gets whether the currently equipped weapon applies the Bleed status effect on each hit.
    /// </summary>
    public bool EquippedWeaponAppliesBleed => EquippedWeapon?.AppliesBleedOnHit ?? false;

    /// <summary>
    /// Equips an item from the player's inventory into the appropriate slot (weapon, armor, or
    /// accessory). If a different item already occupies that slot, it is unequipped and moved back
    /// into the inventory. Stat bonuses from the new item are applied immediately.
    /// </summary>
    /// <param name="item">The equippable item to put on; must already be present in <see cref="Inventory"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="item"/> is not equippable, is not in the inventory, or has
    /// an item type that does not map to a valid equipment slot.
    /// </exception>
    public void EquipItem(Item item)
    {
        if (!item.IsEquippable)
            throw new ArgumentException($"Item {item.Name} is not equippable.", nameof(item));

        if (!Inventory.Contains(item))
            throw new ArgumentException($"Item {item.Name} is not in inventory.", nameof(item));

        Item? previousItem = null;

        switch (item.Type)
        {
            case ItemType.Weapon:
                if (EquippedWeapon != null)
                {
                    previousItem = EquippedWeapon;
                    RemoveStatBonuses(previousItem);
                }
                EquippedWeapon = item;
                break;

            case ItemType.Armor:
                if (EquippedArmor != null)
                {
                    previousItem = EquippedArmor;
                    RemoveStatBonuses(previousItem);
                }
                EquippedArmor = item;
                break;

            case ItemType.Accessory:
                if (EquippedAccessory != null)
                {
                    previousItem = EquippedAccessory;
                    RemoveStatBonuses(previousItem);
                }
                EquippedAccessory = item;
                break;

            default:
                throw new ArgumentException($"Invalid item type for equipment: {item.Type}", nameof(item));
        }

        Inventory.Remove(item);
        ApplyStatBonuses(item);

        if (previousItem != null)
        {
            Inventory.Add(previousItem);
        }
    }

    /// <summary>
    /// Removes the item from the named equipment slot, reverses its stat bonuses, and places it
    /// back into the player's inventory.
    /// </summary>
    /// <param name="slotName">
    /// The case-insensitive slot to unequip. Accepts <c>"weapon"</c>, <c>"accessory"</c>,
    /// and any armor slot: <c>"head"</c>, <c>"shoulders"</c>, <c>"chest"</c> (alias <c>"armor"</c>),
    /// <c>"hands"</c>, <c>"legs"</c>, <c>"feet"</c>, <c>"back"</c>, <c>"offhand"</c>.
    /// </param>
    /// <returns>The item that was removed from the slot and returned to inventory.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the specified slot is empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="slotName"/> is not a recognised slot name.</exception>
    public Item? UnequipItem(string slotName)
    {
        Item? item = null;
        var slotLower = slotName.ToLowerInvariant();

        switch (slotLower)
        {
            case "weapon":
                if (EquippedWeapon == null)
                    throw new InvalidOperationException("No weapon equipped.");
                item = EquippedWeapon;
                EquippedWeapon = null;
                break;

            // "armor" kept as legacy alias for chest
            case "armor":
            case "chest":
                if (EquippedChest == null)
                    throw new InvalidOperationException("No chest armor equipped.");
                item = EquippedChest;
                EquippedChest = null;
                break;

            case "head":
                if (EquippedHead == null)
                    throw new InvalidOperationException("No head armor equipped.");
                item = EquippedHead;
                EquippedHead = null;
                break;

            case "shoulders":
                if (EquippedShoulders == null)
                    throw new InvalidOperationException("No shoulder armor equipped.");
                item = EquippedShoulders;
                EquippedShoulders = null;
                break;

            case "hands":
                if (EquippedHands == null)
                    throw new InvalidOperationException("No hand armor equipped.");
                item = EquippedHands;
                EquippedHands = null;
                break;

            case "legs":
                if (EquippedLegs == null)
                    throw new InvalidOperationException("No leg armor equipped.");
                item = EquippedLegs;
                EquippedLegs = null;
                break;

            case "feet":
                if (EquippedFeet == null)
                    throw new InvalidOperationException("No foot armor equipped.");
                item = EquippedFeet;
                EquippedFeet = null;
                break;

            case "back":
                if (EquippedBack == null)
                    throw new InvalidOperationException("No back armor equipped.");
                item = EquippedBack;
                EquippedBack = null;
                break;

            case "offhand":
            case "off-hand":
            case "off_hand":
                if (EquippedOffHand == null)
                    throw new InvalidOperationException("No off-hand item equipped.");
                item = EquippedOffHand;
                EquippedOffHand = null;
                break;

            case "accessory":
                if (EquippedAccessory == null)
                    throw new InvalidOperationException("No accessory equipped.");
                item = EquippedAccessory;
                EquippedAccessory = null;
                break;

            default:
                throw new ArgumentException(
                    $"Invalid slot name: {slotName}. Use weapon, accessory, or an armor slot: head, shoulders, chest, hands, legs, feet, back, offhand.",
                    nameof(slotName));
        }

        RemoveStatBonuses(item);
        Inventory.Add(item);
        return item;
    }

    private void ApplyStatBonuses(Item item)
    {
        if (item.AttackBonus != 0)
            ModifyAttack(item.AttackBonus);
        if (item.DefenseBonus != 0)
            ModifyDefense(item.DefenseBonus);
        if (item.StatModifier != 0)
        {
            var oldMaxHP = MaxHP;
            MaxHP += item.StatModifier;
            if (MaxHP < 1)
                MaxHP = 1;

            // If MaxHP increased, heal proportionally
            if (MaxHP > oldMaxHP && HP > 0)
            {
                var oldHP = HP;
                HP = Math.Min(MaxHP, HP + (MaxHP - oldMaxHP));
                if (HP != oldHP)
                    OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
            }
        }
        if (item.MaxManaBonus != 0)
            FortifyMaxMana(item.MaxManaBonus);
        RecalculateDerivedBonuses();
    }

    private void RemoveStatBonuses(Item item)
    {
        if (item.AttackBonus != 0)
            ModifyAttack(-item.AttackBonus);
        if (item.DefenseBonus != 0)
            ModifyDefense(-item.DefenseBonus);
        if (item.StatModifier != 0)
        {
            var oldMaxHP = MaxHP;
            MaxHP -= item.StatModifier;
            if (MaxHP < 1)
                MaxHP = 1;

            // If HP exceeds new MaxHP, clamp it
            if (HP > MaxHP)
            {
                var oldHP = HP;
                HP = MaxHP;
                OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
            }
        }
        if (item.MaxManaBonus != 0)
        {
            MaxMana -= item.MaxManaBonus;
            if (MaxMana < 0) MaxMana = 0;
            if (Mana > MaxMana) Mana = MaxMana;
        }
        RecalculateDerivedBonuses();
    }

    private void RecalculateDerivedBonuses()
    {
        DodgeBonus = (EquippedWeapon?.DodgeBonus ?? 0f)
                   + (EquippedAccessory?.DodgeBonus ?? 0f)
                   + AllEquippedArmor.Sum(a => a.DodgeBonus);

        PoisonImmune = (EquippedWeapon?.PoisonImmunity ?? false)
                    || (EquippedAccessory?.PoisonImmunity ?? false)
                    || AllEquippedArmor.Any(a => a.PoisonImmunity);
    }
}
