using Dungnz.Models;

namespace Dungnz.Tests.Builders;

/// <summary>
/// Fluent builder for creating <see cref="Enemy"/> instances in tests.
/// Produces a lightweight <see cref="TestEnemy"/> subclass suitable for unit tests.
/// </summary>
public class EnemyBuilder
{
    private string _name = "Test Enemy";
    private int _hp = 20;
    private int _attack = 8;
    private int _defense = 2;
    private int _xpValue = 15;
    private int _minGold;
    private int _maxGold;
    private bool _isElite;
    private bool _isUndead;

    public EnemyBuilder Named(string name) { _name = name; return this; }
    public EnemyBuilder WithHP(int hp) { _hp = hp; return this; }
    public EnemyBuilder WithAttack(int atk) { _attack = atk; return this; }
    public EnemyBuilder WithDefense(int def) { _defense = def; return this; }
    public EnemyBuilder WithXP(int xp) { _xpValue = xp; return this; }
    public EnemyBuilder WithGold(int min, int max) { _minGold = min; _maxGold = max; return this; }
    public EnemyBuilder AsElite() { _isElite = true; return this; }
    public EnemyBuilder AsUndead() { _isUndead = true; return this; }

    public Enemy Build()
    {
        var enemy = new BuilderEnemy
        {
            Name = _name,
            HP = _hp,
            MaxHP = _hp,
            Attack = _attack,
            Defense = _defense,
            XPValue = _xpValue,
            IsElite = _isElite,
            IsUndead = _isUndead,
            LootTable = new LootTable(minGold: _minGold, maxGold: _maxGold),
        };
        enemy.SetFlatDodgeChance(0f);
        return enemy;
    }

    /// <summary>Concrete Enemy subclass for builder-created test enemies.</summary>
    private class BuilderEnemy : Enemy
    {
        public void SetFlatDodgeChance(float value) => FlatDodgeChance = value;
    }
}
