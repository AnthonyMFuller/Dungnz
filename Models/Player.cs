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
