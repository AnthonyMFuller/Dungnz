using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

[Collection("EnemyFactory")]
public class EnemyFactoryTests
{

    [Fact]
    public void CreateRandom_ReturnsValidEnemy()
    {
        var rng = new Random(1);
        var enemy = EnemyFactory.CreateRandom(rng);
        enemy.Should().NotBeNull();
        enemy.HP.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateBoss_ReturnsDungeonBoss()
    {
        var rng = new Random(1);
        var boss = EnemyFactory.CreateBoss(rng);
        boss.Should().BeAssignableTo<DungeonBoss>();
    }

    [Fact]
    public void CreateRandom_ReturnsVariety()
    {
        // With enough calls and deterministic seeds, we should see multiple types
        var types = new HashSet<string>();
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var enemy = EnemyFactory.CreateRandom(rng);
            types.Add(enemy.GetType().Name);
        }
        types.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void CreateRandom_CanReturnGoblin()
    {
        // rng.Next(4)==0 → Goblin
        var rng = new Random(0);
        bool found = false;
        for (int i = 0; i < 20; i++)
        {
            var e = EnemyFactory.CreateRandom(new Random(i));
            if (e is Goblin) { found = true; break; }
        }
        found.Should().BeTrue();
    }

    [Fact]
    public void CreateRandom_CanReturnSkeleton()
    {
        bool found = false;
        for (int i = 0; i < 100; i++)
        {
            var e = EnemyFactory.CreateRandom(new Random(i));
            if (e is Skeleton) { found = true; break; }
        }
        found.Should().BeTrue();
    }

    [Fact]
    public void CreateRandom_CanReturnTroll()
    {
        bool found = false;
        for (int i = 0; i < 100; i++)
        {
            var e = EnemyFactory.CreateRandom(new Random(i));
            if (e is Troll) { found = true; break; }
        }
        found.Should().BeTrue();
    }

    [Fact]
    public void CreateRandom_CanReturnDarkKnight()
    {
        bool found = false;
        for (int i = 0; i < 100; i++)
        {
            var e = EnemyFactory.CreateRandom(new Random(i));
            if (e is DarkKnight) { found = true; break; }
        }
        found.Should().BeTrue();
    }

    [Fact]
    public void CreateScaled_Level1_NoScaling()
    {
        // At level 1, scalar = 1.0 (no change)
        var goblin = EnemyFactory.CreateScaled("goblin", 1);
        
        goblin.Should().BeOfType<Goblin>();
        goblin.MaxHP.Should().Be(20);
        goblin.Attack.Should().Be(8);
        goblin.Defense.Should().Be(2);
        goblin.XPValue.Should().Be(15);
    }

    [Fact]
    public void CreateScaled_Level5_48PercentStronger()
    {
        // At level 5, scalar = 1.0 + (5-1) * 0.12 = 1.48
        var goblin = EnemyFactory.CreateScaled("goblin", 5);
        
        goblin.Should().BeOfType<Goblin>();
        goblin.MaxHP.Should().Be(30); // 20 * 1.48 = 29.6 ≈ 30
        goblin.Attack.Should().Be(12); // 8 * 1.48 = 11.84 ≈ 12
        goblin.Defense.Should().Be(3); // 2 * 1.48 = 2.96 ≈ 3
        goblin.XPValue.Should().Be(22); // 15 * 1.48 = 22.2 ≈ 22
    }

    [Fact]
    public void CreateScaled_Level10_108PercentStronger()
    {
        // At level 10, scalar = 1.0 + (10-1) * 0.12 = 2.08
        var goblin = EnemyFactory.CreateScaled("goblin", 10);
        
        goblin.Should().BeOfType<Goblin>();
        goblin.MaxHP.Should().Be(42); // 20 * 2.08 = 41.6 ≈ 42
        goblin.Attack.Should().Be(17); // 8 * 2.08 = 16.64 ≈ 17
        goblin.Defense.Should().Be(4); // 2 * 2.08 = 4.16 ≈ 4
        goblin.XPValue.Should().Be(31); // 15 * 2.08 = 31.2 ≈ 31
    }

    [Fact]
    public void CreateScaled_GoldRewardsScale()
    {
        // Verify gold rewards scale too
        var level1 = EnemyFactory.CreateScaled("goblin", 1);
        var level5 = EnemyFactory.CreateScaled("goblin", 5);
        var level10 = EnemyFactory.CreateScaled("goblin", 10);

        // Base: MinGold=2, MaxGold=8
        // Level 1: 2-8 (scalar 1.0)
        level1.LootTable.Should().NotBeNull();
        
        // Level 5: 3-12 (scalar 1.48: 2*1.48≈3, 8*1.48≈12)
        level5.LootTable.Should().NotBeNull();
        
        // Level 10: 4-17 (scalar 2.08: 2*2.08≈4, 8*2.08≈17)
        level10.LootTable.Should().NotBeNull();
    }

    [Fact]
    public void CreateScaled_AllEnemyTypes()
    {
        // Verify all enemy types can be scaled
        var enemyTypes = new[] { "goblin", "skeleton", "troll", "darkknight", "dungeonboss" };
        
        foreach (var type in enemyTypes)
        {
            var enemy = EnemyFactory.CreateScaled(type, 5);
            enemy.Should().NotBeNull();
            enemy.MaxHP.Should().BeGreaterThan(0);
            enemy.Attack.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void CreateScaled_Boss_ScalesCorrectly()
    {
        // Dungeon Boss: MaxHP=100, Attack=22, Defense=15
        var level1Boss = EnemyFactory.CreateScaled("dungeonboss", 1);
        var level10Boss = EnemyFactory.CreateScaled("dungeonboss", 10);

        level1Boss.Should().BeOfType<DungeonBoss>();
        level1Boss.MaxHP.Should().Be(100);
        level1Boss.Attack.Should().Be(22);
        level1Boss.Defense.Should().Be(15);

        level10Boss.Should().BeOfType<DungeonBoss>();
        level10Boss.MaxHP.Should().Be(208); // 100 * 2.08
        level10Boss.Attack.Should().Be(46); // 22 * 2.08 = 45.76 ≈ 46
        level10Boss.Defense.Should().Be(31); // 15 * 2.08 = 31.2 ≈ 31
    }

    [Fact]
    public void CreateScaled_UnknownType_ThrowsException()
    {
        var act = () => EnemyFactory.CreateScaled("unknown", 1);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown enemy type: unknown*");
    }

    [Fact]
    public void CreateScaled_Skeleton_Level5()
    {
        // Skeleton: MaxHP=30, Attack=12, Defense=5
        var skeleton = EnemyFactory.CreateScaled("skeleton", 5);
        
        skeleton.Should().BeOfType<Skeleton>();
        skeleton.MaxHP.Should().Be(44); // 30 * 1.48 = 44.4 ≈ 44
        skeleton.Attack.Should().Be(18); // 12 * 1.48 = 17.76 ≈ 18
        skeleton.Defense.Should().Be(7); // 5 * 1.48 = 7.4 ≈ 7
    }

    [Fact]
    public void CreateScaled_Troll_Level10()
    {
        // Troll: MaxHP=60, Attack=10, Defense=8
        var troll = EnemyFactory.CreateScaled("troll", 10);
        
        troll.Should().BeOfType<Troll>();
        troll.MaxHP.Should().Be(125); // 60 * 2.08 = 124.8 ≈ 125
        troll.Attack.Should().Be(21); // 10 * 2.08 = 20.8 ≈ 21
        troll.Defense.Should().Be(17); // 8 * 2.08 = 16.64 ≈ 17
    }

    [Fact]
    public void CreateScaled_DarkKnight_Level5()
    {
        // DarkKnight: MaxHP=45, Attack=18, Defense=12
        var darkKnight = EnemyFactory.CreateScaled("darkknight", 5);
        
        darkKnight.Should().BeOfType<DarkKnight>();
        darkKnight.MaxHP.Should().Be(67); // 45 * 1.48 = 66.6 ≈ 67
        darkKnight.Attack.Should().Be(27); // 18 * 1.48 = 26.64 ≈ 27
        darkKnight.Defense.Should().Be(18); // 12 * 1.48 = 17.76 ≈ 18
    }
}
