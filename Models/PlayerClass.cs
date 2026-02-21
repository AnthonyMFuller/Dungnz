namespace Dungnz.Models;

public enum PlayerClass { Warrior, Mage, Rogue }

public class PlayerClassDefinition
{
    public PlayerClass Class { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public int BonusAttack { get; init; }
    public int BonusDefense { get; init; }
    public int BonusMaxHP { get; init; }
    public int BonusMaxMana { get; init; }

    public static readonly PlayerClassDefinition Warrior = new() {
        Class = PlayerClass.Warrior, Name = "Warrior",
        Description = "High HP and defense. Slow mana.",
        BonusAttack = 3, BonusDefense = 2, BonusMaxHP = 20, BonusMaxMana = -10
    };
    public static readonly PlayerClassDefinition Mage = new() {
        Class = PlayerClass.Mage, Name = "Mage",
        Description = "High mana and powerful spells. Low HP.",
        BonusAttack = 0, BonusDefense = -1, BonusMaxHP = -10, BonusMaxMana = 30
    };
    public static readonly PlayerClassDefinition Rogue = new() {
        Class = PlayerClass.Rogue, Name = "Rogue",
        Description = "Balanced. Extra dodge chance.",
        BonusAttack = 2, BonusDefense = 0, BonusMaxHP = 0, BonusMaxMana = 0
    };

    public static IReadOnlyList<PlayerClassDefinition> All => new[] { Warrior, Mage, Rogue };
}
