using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

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
        boss.Should().BeOfType<DungeonBoss>();
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
}
