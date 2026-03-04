using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #953 — Tests that generating a new floor properly resets rooms to Fresh state,
/// places appropriate bosses, and that floor transition narration matches floor numbers.
/// </summary>
[Collection("EnemyFactory")]
public class FloorTransitionStateTests
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
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public void NewFloor_AllRooms_StartFresh(int floor)
    {
        var gen = new DungeonGenerator(42, Array.Empty<Item>());
        var (start, _) = gen.Generate(floor: floor);
        var rooms = CollectAllRooms(start);

        rooms.Should().OnlyContain(r => r.State == RoomState.Fresh,
            "all rooms on a new floor should be Fresh");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void NewFloor_NoRoomsVisited(int floor)
    {
        var gen = new DungeonGenerator(10, Array.Empty<Item>());
        var (start, _) = gen.Generate(floor: floor);
        var rooms = CollectAllRooms(start);

        rooms.Should().OnlyContain(r => !r.Visited, "new floor rooms should not be visited");
    }

    [Fact]
    public void NewFloor_ShrinesNotUsed()
    {
        var gen = new DungeonGenerator(42, Array.Empty<Item>());
        var (start, _) = gen.Generate(floor: 4);
        var rooms = CollectAllRooms(start);

        rooms.Where(r => r.HasShrine).Should().OnlyContain(r => !r.ShrineUsed,
            "shrines on new floors should not be used");
    }

    [Fact]
    public void NewFloor_SpecialRoomsNotUsed()
    {
        var gen = new DungeonGenerator(42, Array.Empty<Item>());
        var (start, _) = gen.Generate(floor: 5);
        var rooms = CollectAllRooms(start);

        rooms.Should().OnlyContain(r => !r.SpecialRoomUsed,
            "special rooms on a new floor should not be used");
    }

    [Fact]
    public void NewFloor_BlessedHealNotApplied()
    {
        var gen = new DungeonGenerator(42, Array.Empty<Item>());
        var (start, _) = gen.Generate(floor: 3);
        var rooms = CollectAllRooms(start);

        rooms.Where(r => r.EnvironmentalHazard == RoomHazard.BlessedClearing)
            .Should().OnlyContain(r => !r.BlessedHealApplied,
                "blessed heals on new floors should not be applied");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void FloorTransitionNarration_MatchesFloor(int floor)
    {
        var seq = FloorTransitionNarration.GetSequence(floor);
        seq.Should().HaveCount(5, $"floor {floor} transition should have 5 lines");
        seq.Should().AllSatisfy(line => line.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void ConsecutiveFloors_ProduceDifferentBosses()
    {
        var gen1 = new DungeonGenerator(42, Array.Empty<Item>());
        var gen2 = new DungeonGenerator(42, Array.Empty<Item>());
        var (_, exit1) = gen1.Generate(floor: 1);
        var (_, exit2) = gen2.Generate(floor: 2);

        exit1.Enemy!.GetType().Should().NotBe(exit2.Enemy!.GetType(),
            "different floors should have different boss types");
    }

    [Fact]
    public void FloorTransitionNarration_Floor1_HasNoTransition()
    {
        var seq = FloorTransitionNarration.GetSequence(1);
        seq.Should().BeEmpty("there is no transition to floor 1");
    }

    [Fact]
    public void LowFloor_NoLavaSeam()
    {
        var gen = new DungeonGenerator(42, Array.Empty<Item>());
        var (startLow, _) = gen.Generate(floor: 2);
        var lowRooms = CollectAllRooms(startLow);

        lowRooms.Any(r => r.EnvironmentalHazard == RoomHazard.LavaSeam)
            .Should().BeFalse("LavaSeam should not appear on floor 2");
    }
}
