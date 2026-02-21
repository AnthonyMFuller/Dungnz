namespace Dungnz.Models;

/// <summary>
/// Represents an active combat ability the player can unlock at a certain level and activate
/// by spending mana. Each ability has a cooldown measured in combat turns to prevent
/// continuous use.
/// </summary>
public class Ability
{
    /// <summary>Gets the display name of the ability shown in combat menus and the character sheet.</summary>
    public string Name { get; }

    /// <summary>Gets the text describing the ability's effect, displayed when the player inspects available abilities.</summary>
    public string Description { get; }

    /// <summary>Gets the number of mana points deducted from the player when this ability is activated.</summary>
    public int ManaCost { get; }

    /// <summary>
    /// Gets the number of combat turns that must pass after use before this ability can be
    /// activated again. A value of 0 means the ability can be used every turn.
    /// </summary>
    public int CooldownTurns { get; }

    /// <summary>
    /// Gets the minimum player level required to unlock and use this ability.
    /// The ability is unavailable until <see cref="Player.Level"/> reaches this value.
    /// </summary>
    public int UnlockLevel { get; }

    /// <summary>Gets the type identifier that determines the specific gameplay effect applied when this ability is used.</summary>
    public AbilityType Type { get; }

    /// <summary>
    /// Initialises a new <see cref="Ability"/> with all defining characteristics.
    /// </summary>
    /// <param name="name">Display name of the ability.</param>
    /// <param name="description">Human-readable description of the ability's effect.</param>
    /// <param name="manaCost">Mana points spent on each use.</param>
    /// <param name="cooldownTurns">Combat turns before the ability can be reused.</param>
    /// <param name="unlockLevel">Minimum player level needed to unlock this ability.</param>
    /// <param name="type">The specific effect type applied when this ability is activated.</param>
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

/// <summary>
/// Identifies the specific combat effect executed when an <see cref="Ability"/> is activated,
/// allowing the combat system to branch on ability behaviour without relying on subclasses.
/// </summary>
public enum AbilityType
{
    /// <summary>Deals bonus damage in a single devastating strike, scaling with the player's attack stat.</summary>
    PowerStrike,

    /// <summary>Temporarily boosts the player's defense for a set number of turns, reducing incoming damage.</summary>
    DefensiveStance,

    /// <summary>Fires a ranged dart that applies the Poison status effect to the target enemy.</summary>
    PoisonDart,

    /// <summary>Channels inner resolve to restore a portion of the player's HP mid-combat without using an item.</summary>
    SecondWind
}
