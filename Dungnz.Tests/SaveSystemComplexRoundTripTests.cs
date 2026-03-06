using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #946 — Complex save/load round-trip tests: player with inventory, stats, equipment,
/// dungeon progress, plus edge cases like empty inventory and corrupted saves.
/// </summary>
[Collection("save-system")]
public class SaveSystemComplexRoundTripTests : IDisposable
{
    private readonly string _saveDir;

    public SaveSystemComplexRoundTripTests()
    {
        _saveDir = Path.Combine(Path.GetTempPath(), $"dungnz_complex_save_{Guid.NewGuid()}");
        SaveSystem.OverrideSaveDirectory(_saveDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_saveDir))
            Directory.Delete(_saveDir, recursive: true);
    }

    [Fact]
    public void RoundTrip_ComplexState_AllFieldsPreserved()
    {
        var player = new Player { Name = "Hero" };
        player.AddGold(500);
        player.AddXP(300);
        var sword = new Item { Name = "Flame Sword", Type = ItemType.Weapon, AttackBonus = 12, Tier = ItemTier.Epic };
        var shield = new Item { Name = "Iron Shield", Type = ItemType.Armor, DefenseBonus = 8, Slot = ArmorSlot.OffHand };
        player.Inventory.Add(sword);
        player.Inventory.Add(shield);
        player.EquippedWeapon = sword;
        player.EquippedOffHand = shield;

        var room = new Room { Description = "Boss chamber", IsExit = true, HasShrine = true, ShrineUsed = true };
        var state = new GameState(player, room, currentFloor: 5, seed: 42, difficulty: Difficulty.Hard);

        SaveSystem.SaveGame(state, "complex");
        var loaded = SaveSystem.LoadGame("complex");

        loaded.Player.Name.Should().Be("Hero");
        loaded.Player.Gold.Should().Be(500);
        loaded.Player.XP.Should().Be(300);
        loaded.Player.Inventory.Should().HaveCount(2);
        loaded.CurrentFloor.Should().Be(5);
        loaded.Seed.Should().Be(42);
        loaded.Difficulty.Should().Be(Difficulty.Hard);
        loaded.CurrentRoom.HasShrine.Should().BeTrue();
        loaded.CurrentRoom.ShrineUsed.Should().BeTrue();
        loaded.CurrentRoom.IsExit.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_EmptyInventory_LoadsCorrectly()
    {
        var player = new Player { Name = "Bare" };
        var state = new GameState(player, new Room { Description = "Start" });

        SaveSystem.SaveGame(state, "empty-inv");
        var loaded = SaveSystem.LoadGame("empty-inv");

        loaded.Player.Inventory.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_RoomHazards_Preserved()
    {
        var room = new Room
        {
            Description = "Lava room",
            Hazard = HazardType.Fire,
            EnvironmentalHazard = RoomHazard.LavaSeam,
            Type = RoomType.Scorched
        };
        var state = new GameState(new Player { Name = "T" }, room);

        SaveSystem.SaveGame(state, "hazard");
        var loaded = SaveSystem.LoadGame("hazard");

        loaded.CurrentRoom.Hazard.Should().Be(HazardType.Fire);
        loaded.CurrentRoom.EnvironmentalHazard.Should().Be(RoomHazard.LavaSeam);
        loaded.CurrentRoom.Type.Should().Be(RoomType.Scorched);
    }

    [Fact]
    public void RoundTrip_TrapRoom_Preserved()
    {
        var room = new Room
        {
            Description = "Trap room",
            Type = RoomType.TrapRoom,
            Trap = TrapVariant.PoisonGas,
            SpecialRoomUsed = true
        };
        var state = new GameState(new Player { Name = "T" }, room);

        SaveSystem.SaveGame(state, "trap");
        var loaded = SaveSystem.LoadGame("trap");

        loaded.CurrentRoom.Type.Should().Be(RoomType.TrapRoom);
        loaded.CurrentRoom.Trap.Should().Be(TrapVariant.PoisonGas);
        loaded.CurrentRoom.SpecialRoomUsed.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_RoomState_Preserved()
    {
        var room = new Room { Description = "Old room", State = RoomState.Cleared, Visited = true, Looted = true };
        var state = new GameState(new Player { Name = "T" }, room);

        SaveSystem.SaveGame(state, "room-state");
        var loaded = SaveSystem.LoadGame("room-state");

        loaded.CurrentRoom.State.Should().Be(RoomState.Cleared);
        loaded.CurrentRoom.Visited.Should().BeTrue();
        loaded.CurrentRoom.Looted.Should().BeTrue();
    }

    [Fact]
    public void LoadGame_CorruptedJson_ThrowsInvalidDataException()
    {
        Directory.CreateDirectory(_saveDir);
        File.WriteAllText(Path.Combine(_saveDir, "corrupt.json"), "{{{not json at all");

        var act = () => SaveSystem.LoadGame("corrupt");
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void LoadGame_MissingFile_ThrowsFileNotFoundException()
    {
        var act = () => SaveSystem.LoadGame("nonexistent_save_xyz");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void SaveGame_EmptyName_ThrowsArgumentException()
    {
        var state = new GameState(new Player { Name = "T" }, new Room { Description = "R" });
        var act = () => SaveSystem.SaveGame(state, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RoundTrip_BlessedHealApplied_Preserved()
    {
        var room = new Room
        {
            Description = "Blessed clearing",
            EnvironmentalHazard = RoomHazard.BlessedClearing,
            BlessedHealApplied = true
        };
        var state = new GameState(new Player { Name = "T" }, room);

        SaveSystem.SaveGame(state, "blessed");
        var loaded = SaveSystem.LoadGame("blessed");

        loaded.CurrentRoom.BlessedHealApplied.Should().BeTrue();
        loaded.CurrentRoom.EnvironmentalHazard.Should().Be(RoomHazard.BlessedClearing);
    }

    [Fact]
    public void RoundTrip_MultipleRooms_ExitsReconnected()
    {
        var roomA = new Room { Description = "Room A" };
        var roomB = new Room { Description = "Room B" };
        var roomC = new Room { Description = "Room C" };
        roomA.Exits[Direction.North] = roomB;
        roomB.Exits[Direction.South] = roomA;
        roomB.Exits[Direction.East] = roomC;
        roomC.Exits[Direction.West] = roomB;
        var state = new GameState(new Player { Name = "T" }, roomA);

        SaveSystem.SaveGame(state, "multi-room");
        var loaded = SaveSystem.LoadGame("multi-room");

        loaded.CurrentRoom.Exits.Should().ContainKey(Direction.North);
        var loadedB = loaded.CurrentRoom.Exits[Direction.North];
        loadedB.Description.Should().Be("Room B");
        loadedB.Exits.Should().ContainKey(Direction.East);
        loadedB.Exits[Direction.East].Description.Should().Be("Room C");
    }

    // ── #1153: FloorHistory round-trip (#1151) ────────────────────────────────

    [Fact]
    public void RoundTrip_FloorHistory_Preserved()
    {
        var floor1Entrance = new Room { Description = "Floor 1 entrance.", IsEntrance = true };
        var currentRoom = new Room { Description = "Floor 2 room." };
        var player = new Player { Name = "Climber" };
        var floorHistory = new Dictionary<int, Room> { [1] = floor1Entrance };

        var state = new GameState(
            player,
            currentRoom,
            currentFloor: 2,
            floorHistory: floorHistory,
            floorEntranceRoom: currentRoom);

        SaveSystem.SaveGame(state, "floor-history");
        var loaded = SaveSystem.LoadGame("floor-history");

        loaded.CurrentFloor.Should().Be(2, "current floor must survive the round-trip");
        loaded.FloorHistory.Should().ContainKey(1,
            "floor 1 entry must be present in FloorHistory after load");
        loaded.FloorHistory[1].Description.Should().Be("Floor 1 entrance.",
            "the saved entrance room description must survive the round-trip");
        loaded.FloorHistory[1].IsEntrance.Should().BeTrue(
            "IsEntrance flag must survive the round-trip inside FloorHistory");
    }
}
