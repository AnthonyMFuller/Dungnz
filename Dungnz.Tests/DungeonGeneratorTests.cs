using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

[Collection("EnemyFactory")]
public class DungeonGeneratorTests
{

    [Fact]
    public void Generate_ReturnsNonNullRooms()
    {
        var gen = new DungeonGenerator(1, Array.Empty<Item>());
        var (start, exit) = gen.Generate();
        start.Should().NotBeNull();
        exit.Should().NotBeNull();
    }

    [Fact]
    public void ExitRoom_IsMarkedAsExit()
    {
        var gen = new DungeonGenerator(1, Array.Empty<Item>());
        var (_, exit) = gen.Generate();
        exit.IsExit.Should().BeTrue();
    }

    [Fact]
    public void StartRoom_IsNotExit()
    {
        var gen = new DungeonGenerator(1, Array.Empty<Item>());
        var (start, _) = gen.Generate();
        start.IsExit.Should().BeFalse();
    }

    [Fact]
    public void ExitRoom_HasBossEnemy()
    {
        var gen = new DungeonGenerator(1, Array.Empty<Item>());
        var (_, exit) = gen.Generate();
        exit.Enemy.Should().NotBeNull();
        exit.Enemy!.HP.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BossIsAtExit_NotAtStart()
    {
        var gen = new DungeonGenerator(1, Array.Empty<Item>());
        var (start, exit) = gen.Generate();
        exit.Enemy.Should().BeAssignableTo<DungeonBoss>();
        // Start room either has no enemy or has a non-boss enemy
        if (start.Enemy != null)
            start.Enemy.Should().NotBeAssignableTo<DungeonBoss>();
    }

    [Fact]
    public void GridFullyConnected_BfsReachesExit()
    {
        var gen = new DungeonGenerator(42, Array.Empty<Item>());
        var (start, exit) = gen.Generate();

        var visited = new HashSet<Room>();
        var queue = new Queue<Room>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in current.Exits.Values)
                if (visited.Add(next))
                    queue.Enqueue(next);
        }

        visited.Should().Contain(exit);
    }

    [Fact]
    public void DefaultGrid_Has20Rooms()
    {
        var gen = new DungeonGenerator(7, Array.Empty<Item>());
        var (start, _) = gen.Generate(5, 4);

        var visited = new HashSet<Room>();
        var queue = new Queue<Room>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in current.Exits.Values)
                if (visited.Add(next))
                    queue.Enqueue(next);
        }

        visited.Count.Should().Be(20);
    }

    [Fact]
    public void SeededGenerator_SameDescriptionForStart()
    {
        var gen1 = new DungeonGenerator(100, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(100, Array.Empty<Item>());
        var (start1, _) = gen1.Generate();
        var (start2, _) = gen2.Generate();
        start1.Description.Should().Be(start2.Description);
    }

    [Fact]
    public void SomeRoomsHaveEnemies()
    {
        var gen = new DungeonGenerator(5, Array.Empty<Item>());
        var (start, _) = gen.Generate(5, 4);

        var visited = new HashSet<Room>();
        var queue = new Queue<Room>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in current.Exits.Values)
                if (visited.Add(next))
                    queue.Enqueue(next);
        }

        visited.Any(r => r.Enemy != null).Should().BeTrue();
    }

    [Fact]
    public void SomeRoomsHaveItems()
    {
        // Use multiple seeds to guarantee at least one produces items
        bool foundItems = false;
        for (int seed = 0; seed < 20 && !foundItems; seed++)
        {
            var gen = new DungeonGenerator(seed: seed);
            var (start, _) = gen.Generate(5, 4);

            var visited = new HashSet<Room>();
            var queue = new Queue<Room>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in current.Exits.Values)
                    if (visited.Add(next))
                        queue.Enqueue(next);
            }

            if (visited.Any(r => r.Items.Count > 0))
                foundItems = true;
        }

        foundItems.Should().BeTrue();
    }
}
