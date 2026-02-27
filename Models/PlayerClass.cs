namespace Dungnz.Models;

/// <summary>The available character classes a player can choose at the start of a run.</summary>
public enum PlayerClass
{
    /// <summary>Represents the Warrior class: high HP and defense, slow mana regeneration.</summary>
    Warrior,
    /// <summary>Represents the Mage class: high mana and spell power, low HP.</summary>
    Mage,
    /// <summary>Represents the Rogue class: balanced stats with an additional dodge bonus.</summary>
    Rogue,
    /// <summary>Represents the Paladin class: holy warrior with high defense and HP, smites undead.</summary>
    Paladin,
    /// <summary>Represents the Necromancer class: death magic and minion summoning, high mana, low survivability.</summary>
    Necromancer,
    /// <summary>Represents the Ranger class: traps, wolf companion, and precise shots.</summary>
    Ranger
}

/// <summary>
/// Holds the full definition of a player class, including its base stat modifiers,
/// passive trait description, and a static catalogue of all available classes.
/// </summary>
public class PlayerClassDefinition
{
    /// <summary>Gets the <see cref="PlayerClass"/> enum value this definition corresponds to.</summary>
    public PlayerClass Class { get; init; }

    /// <summary>Gets the human-readable display name of this class.</summary>
    public string Name { get; init; } = "";

    /// <summary>Gets a short description of the class shown during class selection.</summary>
    public string Description { get; init; } = "";

    /// <summary>Gets the flat bonus added to the player's Attack stat when this class is chosen.</summary>
    public int BonusAttack { get; init; }

    /// <summary>Gets the flat bonus added to the player's Defense stat when this class is chosen.</summary>
    public int BonusDefense { get; init; }

    /// <summary>Gets the flat bonus added to the player's maximum HP when this class is chosen.</summary>
    public int BonusMaxHP { get; init; }

    /// <summary>Gets the flat bonus added to the player's maximum mana when this class is chosen.</summary>
    public int BonusMaxMana { get; init; }

    /// <summary>The predefined definition for the Warrior class.</summary>
    public static readonly PlayerClassDefinition Warrior = new() {
        Class = PlayerClass.Warrior, Name = "Warrior",
        Description = "High HP and defense. Slow mana.",
        BonusAttack = 3, BonusDefense = 2, BonusMaxHP = 20, BonusMaxMana = -10
    };
    /// <summary>The predefined definition for the Mage class.</summary>
    public static readonly PlayerClassDefinition Mage = new() {
        Class = PlayerClass.Mage, Name = "Mage",
        Description = "High mana and powerful spells. Low HP.",
        BonusAttack = 0, BonusDefense = -1, BonusMaxHP = -10, BonusMaxMana = 30
    };
    /// <summary>The predefined definition for the Rogue class.</summary>
    public static readonly PlayerClassDefinition Rogue = new() {
        Class = PlayerClass.Rogue, Name = "Rogue",
        Description = "Balanced. Extra dodge chance.",
        BonusAttack = 2, BonusDefense = 0, BonusMaxHP = 0, BonusMaxMana = 0
    };
    /// <summary>The predefined definition for the Paladin class.</summary>
    public static readonly PlayerClassDefinition Paladin = new() {
        Class = PlayerClass.Paladin, Name = "Paladin",
        Description = "Holy warrior. High DEF/HP. Smites undead.",
        BonusAttack = 1, BonusDefense = 4, BonusMaxHP = 30, BonusMaxMana = 5
    };
    /// <summary>The predefined definition for the Necromancer class.</summary>
    public static readonly PlayerClassDefinition Necromancer = new() {
        Class = PlayerClass.Necromancer, Name = "Necromancer",
        Description = "Death magic and minions. High mana, low HP.",
        BonusAttack = -2, BonusDefense = -2, BonusMaxHP = -20, BonusMaxMana = 40
    };
    /// <summary>The predefined definition for the Ranger class.</summary>
    public static readonly PlayerClassDefinition Ranger = new() {
        Class = PlayerClass.Ranger, Name = "Ranger",
        Description = "Traps, a wolf companion, and precise shots.",
        BonusAttack = 2, BonusDefense = 0, BonusMaxHP = 5, BonusMaxMana = 10
    };

    /// <summary>Gets a short description of the class's passive combat trait (Phase 8-C2).</summary>
    public string TraitDescription => Class switch {
        PlayerClass.Warrior     => "Battle Hardened — ATK↑ as HP↓",
        PlayerClass.Mage        => "Arcane Surge — next cast -1 mana",
        PlayerClass.Rogue       => "Shadow Strike — 2× first hit",
        PlayerClass.Paladin     => "Divine Bulwark — Fortified <25% HP",
        PlayerClass.Necromancer => "Soul Harvest — +5 HP per kill",
        PlayerClass.Ranger      => "Eagle Eye — +dodge turns 1-2",
        _ => ""
    };

    /// <summary>Gets a read-only list of all available class definitions.</summary>
    public static IReadOnlyList<PlayerClassDefinition> All => new[] { Warrior, Mage, Rogue, Paladin, Necromancer, Ranger };
}
