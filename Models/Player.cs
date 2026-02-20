namespace Dungnz.Models;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int HP { get; private set; } = 100;
    public int MaxHP { get; private set; } = 100;
    public int Attack { get; private set; } = 10;
    public int Defense { get; private set; } = 5;
    public int Gold { get; private set; }
    public int XP { get; private set; }
    public int Level { get; private set; } = 1;
    public List<Item> Inventory { get; private set; } = new();

    // Equipment slots
    public Item? EquippedWeapon { get; private set; }
    public Item? EquippedArmor { get; private set; }
    public Item? EquippedAccessory { get; private set; }

    public event EventHandler<HealthChangedEventArgs>? OnHealthChanged;

    public void TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Damage amount cannot be negative.", nameof(amount));

        var oldHP = HP;
        HP = Math.Max(0, HP - amount);
        
        if (HP != oldHP)
            OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }

    public void Heal(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Heal amount cannot be negative.", nameof(amount));

        var oldHP = HP;
        HP = Math.Min(MaxHP, HP + amount);
        
        if (HP != oldHP)
            OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
    }

    public void AddGold(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("Gold amount cannot be negative.", nameof(amount));
        Gold += amount;
    }

    public void AddXP(int amount)
    {
        if (amount < 0)
            throw new ArgumentException("XP amount cannot be negative.", nameof(amount));
        XP += amount;
    }

    public void ModifyAttack(int delta)
    {
        Attack = Math.Max(1, Attack + delta);
    }

    public void ModifyDefense(int delta)
    {
        Defense = Math.Max(0, Defense + delta);
    }

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
    }
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

public class HealthChangedEventArgs : EventArgs
{
    public int OldHP { get; }
    public int NewHP { get; }
    public int Delta => NewHP - OldHP;

    public HealthChangedEventArgs(int oldHP, int newHP)
    {
        OldHP = oldHP;
        NewHP = newHP;
    }
}
