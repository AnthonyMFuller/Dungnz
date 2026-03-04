using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #1004 — Tests for room hazards (entry hazards, environmental hazards, traps)
/// and special room types (ForgottenShrine, PetrifiedLibrary, ContestedArmory, TrapRoom).
/// </summary>
[Collection("EnemyFactory")]
public class RoomHazardAndSpecialRoomTests
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

    // ── Room Hazard Defaults ─────────────────────────────────────────────────

    [Fact]
    public void Room_DefaultHazards_AreNone()
    {
        var room = new Room { Description = "Test" };
        room.Hazard.Should().Be(HazardType.None);
        room.EnvironmentalHazard.Should().Be(RoomHazard.None);
        room.Trap.Should().BeNull();
    }

    [Fact]
    public void Room_DefaultState_IsFresh()
    {
        var room = new Room { Description = "Test" };
        room.State.Should().Be(RoomState.Fresh);
        room.Visited.Should().BeFalse();
        room.Looted.Should().BeFalse();
    }

    [Fact]
    public void Room_DefaultType_IsStandard()
    {
        var room = new Room { Description = "Test" };
        room.Type.Should().Be(RoomType.Standard);
    }

    // ── Entry Hazard Types ───────────────────────────────────────────────────

    [Theory]
    [InlineData(HazardType.Spike)]
    [InlineData(HazardType.Poison)]
    [InlineData(HazardType.Fire)]
    public void Room_CanHaveEntryHazard(HazardType hazard)
    {
        var room = new Room { Description = "Hazardous", Hazard = hazard };
        room.Hazard.Should().Be(hazard);
    }

    // ── Environmental Hazards ────────────────────────────────────────────────

    [Theory]
    [InlineData(RoomHazard.LavaSeam)]
    [InlineData(RoomHazard.CorruptedGround)]
    [InlineData(RoomHazard.BlessedClearing)]
    public void Room_CanHaveEnvironmentalHazard(RoomHazard hazard)
    {
        var room = new Room { Description = "Env", EnvironmentalHazard = hazard };
        room.EnvironmentalHazard.Should().Be(hazard);
    }

    [Fact]
    public void Room_BlessedClearing_TracksBlessedHealApplied()
    {
        var room = new Room
        {
            Description = "Clearing",
            EnvironmentalHazard = RoomHazard.BlessedClearing,
            BlessedHealApplied = false
        };
        room.BlessedHealApplied.Should().BeFalse();

        room.BlessedHealApplied = true;
        room.BlessedHealApplied.Should().BeTrue();
    }

    // ── Trap Rooms ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TrapVariant.ArrowVolley)]
    [InlineData(TrapVariant.PoisonGas)]
    [InlineData(TrapVariant.CollapsingFloor)]
    public void TrapRoom_HasCorrectVariant(TrapVariant variant)
    {
        var room = new Room { Description = "Trapped", Type = RoomType.TrapRoom, Trap = variant };
        room.Type.Should().Be(RoomType.TrapRoom);
        room.Trap.Should().Be(variant);
    }

    // ── Special Room Types ───────────────────────────────────────────────────

    [Theory]
    [InlineData(RoomType.ForgottenShrine)]
    [InlineData(RoomType.PetrifiedLibrary)]
    [InlineData(RoomType.ContestedArmory)]
    [InlineData(RoomType.TrapRoom)]
    public void SpecialRoom_TracksUsedState(RoomType type)
    {
        var room = new Room { Description = "Special", Type = type, SpecialRoomUsed = false };
        room.SpecialRoomUsed.Should().BeFalse();

        room.SpecialRoomUsed = true;
        room.SpecialRoomUsed.Should().BeTrue();
    }

    // ── Generator Placement ──────────────────────────────────────────────────

    [Fact]
    public void Generator_PlacesHazards_OnSomeRooms()
    {
        bool foundHazard = false;
        for (int seed = 0; seed < 20 && !foundHazard; seed++)
        {
            var gen = new DungeonGenerator(seed, Array.Empty<Item>());
            var (start, _) = gen.Generate(floor: 3);
            var rooms = CollectAllRooms(start);
            if (rooms.Any(r => r.Hazard != HazardType.None))
                foundHazard = true;
        }
        foundHazard.Should().BeTrue("generator should place hazards on some rooms");
    }

    [Fact]
    public void Generator_HighFloor_MayHaveLavaSeam()
    {
        bool foundLava = false;
        for (int seed = 0; seed < 30 && !foundLava; seed++)
        {
            var gen = new DungeonGenerator(seed, Array.Empty<Item>());
            var (start, _) = gen.Generate(floor: 7);
            var rooms = CollectAllRooms(start);
            if (rooms.Any(r => r.EnvironmentalHazard == RoomHazard.LavaSeam))
                foundLava = true;
        }
        foundLava.Should().BeTrue("floor 7+ should have LavaSeam on some rooms");
    }

    [Fact]
    public void Generator_LowFloor_MayHaveBlessedClearing()
    {
        bool foundBlessed = false;
        for (int seed = 0; seed < 30 && !foundBlessed; seed++)
        {
            var gen = new DungeonGenerator(seed, Array.Empty<Item>());
            var (start, _) = gen.Generate(floor: 3);
            var rooms = CollectAllRooms(start);
            if (rooms.Any(r => r.EnvironmentalHazard == RoomHazard.BlessedClearing))
                foundBlessed = true;
        }
        foundBlessed.Should().BeTrue("low floors should have BlessedClearing on some rooms");
    }

    [Fact]
    public void Room_Shrine_TracksUsage()
    {
        var room = new Room { Description = "Shrine room", HasShrine = true, ShrineUsed = false };
        room.HasShrine.Should().BeTrue();
        room.ShrineUsed.Should().BeFalse();

        room.ShrineUsed = true;
        room.ShrineUsed.Should().BeTrue();
    }
}
