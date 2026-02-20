namespace Dungnz.Models;

public class Ability
{
    public string Name { get; }
    public string Description { get; }
    public int ManaCost { get; }
    public int CooldownTurns { get; }
    public int UnlockLevel { get; }
    public AbilityType Type { get; }
    
    public Ability(string name, string description, int manaCost, int cooldownTurns, int unlockLevel, AbilityType type)
    {
        Name = name;
        Description = description;
        ManaCost = manaCost;
        CooldownTurns = cooldownTurns;
        UnlockLevel = unlockLevel;
        Type = type;
    }
}

public enum AbilityType
{
    PowerStrike,
    DefensiveStance,
    PoisonDart,
    SecondWind
}
