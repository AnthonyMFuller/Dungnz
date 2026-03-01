using Dungnz.Models;

namespace Dungnz.Tests.Builders;

/// <summary>
/// Fluent builder for creating <see cref="Player"/> instances in tests.
/// Uses <see cref="Player.SetHPDirect"/> for HP assignment since HP has an internal setter.
/// </summary>
public class PlayerBuilder
{
    private int _hp = 100;
    private int _maxHp = 100;
    private int _attack = 10;
    private int _defense = 5;
    private int _level = 1;
    private int _gold;
    private int _xp;
    private int _mana = 30;
    private int _maxMana = 30;
    private string _name = "TestPlayer";
    private PlayerClass _class = PlayerClass.Warrior;
    private readonly List<Item> _inventory = new();
    private Item? _weapon;
    private Item? _accessory;

    public PlayerBuilder WithHP(int hp) { _hp = hp; return this; }
    public PlayerBuilder WithMaxHP(int maxHp) { _maxHp = maxHp; return this; }
    public PlayerBuilder WithAttack(int atk) { _attack = atk; return this; }
    public PlayerBuilder WithDefense(int def) { _defense = def; return this; }
    public PlayerBuilder WithLevel(int level) { _level = level; return this; }
    public PlayerBuilder WithGold(int gold) { _gold = gold; return this; }
    public PlayerBuilder WithXP(int xp) { _xp = xp; return this; }
    public PlayerBuilder WithMana(int mana) { _mana = mana; return this; }
    public PlayerBuilder WithMaxMana(int maxMana) { _maxMana = maxMana; return this; }
    public PlayerBuilder Named(string name) { _name = name; return this; }
    public PlayerBuilder WithClass(PlayerClass cls) { _class = cls; return this; }
    public PlayerBuilder WithItem(Item item) { _inventory.Add(item); return this; }
    public PlayerBuilder WithWeapon(Item weapon) { _weapon = weapon; return this; }
    public PlayerBuilder WithAccessory(Item accessory) { _accessory = accessory; return this; }

    public Player Build()
    {
        var player = new Player
        {
            Name = _name,
            MaxHP = _maxHp,
            Attack = _attack,
            Defense = _defense,
            Level = _level,
            Gold = _gold,
            XP = _xp,
            Mana = _mana,
            MaxMana = _maxMana,
            Class = _class,
            EquippedWeapon = _weapon,
            EquippedAccessory = _accessory,
        };
        player.SetHPDirect(_hp);
        foreach (var item in _inventory)
            player.Inventory.Add(item);
        return player;
    }
}
