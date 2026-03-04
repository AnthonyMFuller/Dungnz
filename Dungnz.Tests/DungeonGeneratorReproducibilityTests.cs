using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #951 — Tests that DungeonGenerator produces deterministic layouts with same seed,
/// different layouts with different seeds, and handles boundary conditions.
/// </summary>
[Collection("EnemyFactory")]
public class DungeonGeneratorReproducibilityTests
{
    private static HashSet<Room> CollectAllRooms(Room start)
    {
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
        return visited;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(9999)]
    public void SameSeed_ProducesSameRoomCount(int seed)
    {
        var gen1 = new DungeonGenerator(seed, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(seed, Array.Empty<Item>());
        var rooms1 = CollectAllRooms(gen1.Generate().startRoom);
        var rooms2 = CollectAllRooms(gen2.Generate().startRoom);

        rooms1.Count.Should().Be(rooms2.Count, "same seed should produce same room count");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    public void SameSeed_ProducesSameDescriptions(int seed)
    {
        var gen1 = new DungeonGenerator(seed, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(seed, Array.Empty<Item>());
        var descs1 = CollectAllRooms(gen1.Generate().startRoom).Select(r => r.Description).OrderBy(d => d).ToList();
        var descs2 = CollectAllRooms(gen2.Generate().startRoom).Select(r => r.Description).OrderBy(d => d).ToList();

        descs1.Should().Equal(descs2, "same seed should produce same descriptions");
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentLayouts()
    {
        var gen1 = new DungeonGenerator(1, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(999, Array.Empty<Item>());
        var (start1, _) = gen1.Generate();
        var (start2, _) = gen2.Generate();

        var descs1 = CollectAllRooms(start1).Select(r => r.Description).OrderBy(d => d).ToList();
        var descs2 = CollectAllRooms(start2).Select(r => r.Description).OrderBy(d => d).ToList();

        descs1.Should().NotEqual(descs2, "different seeds should produce different layouts");
    }

    [Fact]
    public void SameSeed_BossType_IsDeterministic()
    {
        var gen1 = new DungeonGenerator(42, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(42, Array.Empty<Item>());
        var (_, exit1) = gen1.Generate();
        var (_, exit2) = gen2.Generate();

        exit1.Enemy!.GetType().Should().Be(exit2.Enemy!.GetType());
    }

    [Fact]
    public void MinimumSize_1x1_ProducesOneRoom()
    {
        var gen = new DungeonGenerator(1, Array.Empty<Item>());
        var (start, exit) = gen.Generate(width: 1, height: 1);

        start.Should().BeSameAs(exit, "1x1 grid has only one room");
    }

    [Fact]
    public void SmallGrid_2x2_Produces4Rooms()
    {
        var gen = new DungeonGenerator(5, Array.Empty<Item>());
        var (start, _) = gen.Generate(width: 2, height: 2);
        var rooms = CollectAllRooms(start);

        rooms.Count.Should().Be(4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public void Generate_WithFloor_ExitHasBoss(int floor)
    {
        var gen = new DungeonGenerator(10, Array.Empty<Item>());
        var (_, exit) = gen.Generate(floor: floor);

        exit.Enemy.Should().NotBeNull($"floor {floor} exit should have a boss");
        exit.Enemy.Should().BeAssignableTo<DungeonBoss>();
    }

    [Fact]
    public void SameSeed_SameFloor_SameEnemyPlacement()
    {
        var gen1 = new DungeonGenerator(77, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(77, Array.Empty<Item>());
        var rooms1 = CollectAllRooms(gen1.Generate(floor: 3).startRoom);
        var rooms2 = CollectAllRooms(gen2.Generate(floor: 3).startRoom);

        var enemyCount1 = rooms1.Count(r => r.Enemy != null);
        var enemyCount2 = rooms2.Count(r => r.Enemy != null);

        enemyCount1.Should().Be(enemyCount2, "same seed + floor should produce same enemy count");
    }
}
