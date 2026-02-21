namespace Dungnz.Models;

/// <summary>
/// Represents the player character, tracking combat stats, inventory, equipment, mana, and
/// progression throughout the dungeon crawl. Exposes methods for taking damage, healing,
/// managing gold and XP, equipping items, and levelling up.
/// </summary>
public class Player
{
    /// <summary>Gets or sets the player's display name shown in UI and combat messages.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the player's current hit points. Reduced by <see cref="TakeDamage"/> and restored
    /// by <see cref="Heal"/>; always clamped between 0 and <see cref="MaxHP"/>.
    /// </summary>
    public int HP { get; private set; } = 100;

    /// <summary>
    /// Gets the player's maximum hit points. Increases on level-up via <see cref="LevelUp"/>,
    /// via <see cref="FortifyMaxHP"/>, and when certain equipment with a positive
    /// <see cref="Item.StatModifier"/> is equipped.
    /// </summary>
    public int MaxHP { get; private set; } = 100;

    /// <summary>
    /// Gets the player's base attack power used to calculate raw damage against enemies before
    /// their defense is subtracted.
    /// </summary>
    public int Attack { get; private set; } = 10;

    /// <summary>
    /// Gets the player's base defense value used to reduce incoming damage and influence
    /// dodge-chance calculations during combat.
    /// </summary>
    public int Defense { get; private set; } = 5;

    /// <summary>Gets the amount of gold the player is currently carrying.</summary>
    public int Gold { get; private set; }

    /// <summary>
    /// Gets the player's total accumulated experience points. Callers must check this value
    /// externally to decide when to call <see cref="LevelUp"/>.
    /// </summary>
    public int XP { get; private set; }

    /// <summary>
    /// Gets the player's current level. Starts at 1 and increments each time
    /// <see cref="LevelUp"/> is called.
    /// </summary>
    public int Level { get; private set; } = 1;

    /// <summary>
    /// Gets the collection of items the player is currently carrying but not equipped.
    /// Equipped items are removed from this list and tracked in the corresponding equipment slot.
    /// </summary>
    public List<Item> Inventory { get; private set; } = new();

    // Equipment slots
    /// <summary>Gets the weapon currently equipped by the player, or <c>null</c> if the weapon slot is empty.</summary>
    public Item? EquippedWeapon { get; private set; }

    /// <summary>Gets the armor currently equipped by the player, or <c>null</c> if the armor slot is empty.</summary>
    public Item? EquippedArmor { get; private set; }

    /// <summary>Gets the accessory currently equipped by the player, or <c>null</c> if the accessory slot is empty.</summary>
    public Item? EquippedAccessory { get; private set; }

    // Mana system
    /// <summary>
    /// Gets the player's current mana. Spent by abilities via <see cref="SpendMana"/> and
    /// restored via <see cref="RestoreMana"/>; always clamped between 0 and <see cref="MaxMana"/>.
    /// </summary>
    public int Mana { get; private set; } = 30;

    /// <summary>
    /// Gets the player's maximum mana capacity. Increases on level-up and via
    /// <see cref="FortifyMaxMana"/>.
    /// </summary>
    public int MaxMana { get; private set; } = 30;

    /// <summary>
    /// Adds the specified amount to the player's current mana, capping at <see cref="MaxMana"/>.
    /// </summary>
    /// <param name="amount">The positive amount of mana to restore.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is negative.</exception>
    public void RestoreMana(int amount)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        Mana = Math.Min(MaxMana, Mana + amount);
    }

    /// <summary>
    /// Attempts to deduct the specified amount of mana. Returns <c>false</c> without modifying
    /// state if the player does not have sufficient mana.
    /// </summary>
    /// <param name="amount">The positive amount of mana required.</param>
    /// <returns>
    /// <c>true</c> if mana was successfully deducted; <c>false</c> if the player had insufficient mana.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is negative.</exception>
    public bool SpendMana(int amount)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (Mana < amount) return false;
        Mana -= amount;
        return true;
    }

    /// <summary>
    /// Raised whenever the player's <see cref="HP"/> value changes, supplying the values
    /// immediately before and after the change.
    /// </summary>
    public event EventHandler<HealthChangedEventArgs>? OnHealthChanged;

    /// <summary>
    /// Reduces the player's HP by the specified damage amount, clamping at 0.
    /// Fires <see cref="OnHealthChanged"/> if HP actually changes.
    /// </summary>
    /// <param name="amount">The positive amount of damage to apply.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is negative.</exception>
    public void TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Damage amount cannot be negative.", nameof(amount));

        var oldHP = HP;
        HP = Math.Max(0, HP - amount);
        
        if (HP != oldHP)
            OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }

    /// <summary>
    /// Restores the player's HP by the specified amount, clamping at <see cref="MaxHP"/>.
    /// Fires <see cref="OnHealthChanged"/> if HP actually changes.
    /// </summary>
    /// <param name="amount">The positive amount of HP to restore.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is negative.</exception>
    public void Heal(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Heal amount cannot be negative.", nameof(amount));

        var oldHP = HP;
        HP = Math.Min(MaxHP, HP + amount);
        
        if (HP != oldHP)
            OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }

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

    /// <summary>
    /// Permanently raises <see cref="MaxHP"/> by <paramref name="amount"/> and proportionally
    /// heals the player by the same value, firing <see cref="OnHealthChanged"/> if HP changes.
    /// </summary>
    /// <param name="amount">The positive amount to add to the maximum HP ceiling.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is not positive.</exception>
    public void FortifyMaxHP(int amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive.", nameof(amount));
        MaxHP += amount;
        var oldHP = HP;
        HP = Math.Min(MaxHP, HP + amount);
        if (HP != oldHP) OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }

    /// <summary>
    /// Permanently raises <see cref="MaxMana"/> by <paramref name="amount"/> and immediately
    /// restores mana by the same value, capped at the new maximum.
    /// </summary>
    /// <param name="amount">The positive amount to add to the maximum mana ceiling.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is not positive.</exception>
    public void FortifyMaxMana(int amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive.", nameof(amount));
        MaxMana += amount;
        Mana = Math.Min(MaxMana, Mana + amount);
    }

    /// <summary>
    /// Adds the specified amount of experience points to the player's cumulative XP total.
    /// Does not automatically trigger a level-up; callers must compare XP against thresholds.
    /// </summary>
    /// <param name="amount">The positive amount of XP to award.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is negative.</exception>
    public void AddXP(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("XP amount cannot be negative.", nameof(amount));
        XP += amount;
    }

    /// <summary>
    /// Adjusts the player's attack stat by <paramref name="delta"/>, enforcing a minimum of 1.
    /// Pass a negative value to temporarily reduce attack (e.g., from the Weakened status effect).
    /// </summary>
    /// <param name="delta">The signed amount to add to the current attack value.</param>
    public void ModifyAttack(int delta)
    {
        Attack = Math.Max(1, Attack + delta);
    }

    /// <summary>
    /// Adjusts the player's defense stat by <paramref name="delta"/>, enforcing a minimum of 0.
    /// Pass a negative value to temporarily reduce defense (e.g., from a debuff or unequip).
    /// </summary>
    /// <param name="delta">The signed amount to add to the current defense value.</param>
    public void ModifyDefense(int delta)
    {
        Defense = Math.Max(0, Defense + delta);
    }

    /// <summary>
    /// Advances the player by one level: increments <see cref="Level"/>, raises Attack by 2,
    /// Defense by 1, MaxHP and MaxMana by 10, then fully restores both HP and Mana.
    /// Fires <see cref="OnHealthChanged"/>.
    /// </summary>
    public void LevelUp()
    {
        Level++;
        ModifyAttack(2);
        ModifyDefense(1);
        var oldMaxHP = MaxHP;
        MaxHP += 10;
        var oldHP = HP;
        HP = MaxHP;
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
        MaxMana += 10;
        Mana = MaxMana;
    }

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
    /// The case-insensitive slot to unequip: <c>"weapon"</c>, <c>"armor"</c>, or <c>"accessory"</c>.
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

            case "armor":
                if (EquippedArmor == null)
                    throw new InvalidOperationException("No armor equipped.");
                item = EquippedArmor;
                EquippedArmor = null;
                break;

            case "accessory":
                if (EquippedAccessory == null)
                    throw new InvalidOperationException("No accessory equipped.");
                item = EquippedAccessory;
                EquippedAccessory = null;
                break;

            default:
                throw new ArgumentException($"Invalid slot name: {slotName}. Use 'weapon', 'armor', or 'accessory'.", nameof(slotName));
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
    }

}

/// <summary>
/// Provides data for the <see cref="Player.OnHealthChanged"/> event, capturing the player's HP
/// value immediately before and after the change so that subscribers can react to the magnitude
/// and direction of the modification.
/// </summary>
public class HealthChangedEventArgs : EventArgs
{
    /// <summary>Gets the player's HP value recorded immediately before the change was applied.</summary>
    public int OldHP { get; }

    /// <summary>Gets the player's HP value recorded immediately after the change was applied.</summary>
    public int NewHP { get; }

    /// <summary>
    /// Gets the signed difference (<see cref="NewHP"/> − <see cref="OldHP"/>).
    /// A negative value indicates damage was taken; a positive value indicates healing.
    /// </summary>
    public int Delta => NewHP - OldHP;

    /// <summary>
    /// Initialises a new <see cref="HealthChangedEventArgs"/> with the HP snapshots taken
    /// before and after the change.
    /// </summary>
    /// <param name="oldHP">The HP value before the change.</param>
    /// <param name="newHP">The HP value after the change.</param>
    public HealthChangedEventArgs(int oldHP, int newHP)
    {
        OldHP = oldHP;
        NewHP = newHP;
    }
}
