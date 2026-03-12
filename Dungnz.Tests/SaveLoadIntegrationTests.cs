using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

public class SaveLoadIntegrationTests : IDisposable
{
    private readonly string _saveDir;
    public SaveLoadIntegrationTests()
    {
        _saveDir = Path.Combine(Path.GetTempPath(), $"DungnzSLT_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_saveDir);
        SaveSystem.OverrideSaveDirectory(_saveDir);
    }
    public void Dispose()
    {
        SaveSystem.OverrideSaveDirectory(Path.GetTempPath());
        if (Directory.Exists(_saveDir)) Directory.Delete(_saveDir, recursive: true);
    }
    private static (Player, Room) Make() =>
        (new PlayerBuilder().WithHP(75).WithMaxHP(100).WithAttack(12).WithDefense(4).WithLevel(3).WithGold(200).Build(),
         new Room { Description = "A damp stone chamber" });

    [Fact]
    public void SaveLoad_PlayerHP_RoundTrips()
    {
        var (p, r) = Make(); p.SetHPDirect(75); p.MaxHP = 100;
        SaveSystem.SaveGame(new GameState(p, r), "s1");
        var l = SaveSystem.LoadGame("s1");
        l.Player.HP.Should().Be(75); l.Player.MaxHP.Should().Be(100);
    }

    [Fact]
    public void SaveLoad_PlayerGold_RoundTrips()
    {
        var (p, r) = Make(); p.Gold = 350;
        SaveSystem.SaveGame(new GameState(p, r), "s2");
        SaveSystem.LoadGame("s2").Player.Gold.Should().Be(350);
    }

    [Fact]
    public void SaveLoad_PlayerLevelAndXP_RoundTrips()
    {
        var (p, r) = Make(); p.Level = 5; p.XP = 460;
        SaveSystem.SaveGame(new GameState(p, r), "s3");
        var l = SaveSystem.LoadGame("s3");
        l.Player.Level.Should().Be(5); l.Player.XP.Should().Be(460);
    }

    [Fact]
    public void SaveLoad_CurrentFloor_RoundTrips()
    {
        var (p, r) = Make();
        SaveSystem.SaveGame(new GameState(p, r, currentFloor: 3), "s4");
        SaveSystem.LoadGame("s4").CurrentFloor.Should().Be(3);
    }

    [Fact]
    public void SaveLoad_DifficultyHard_RoundTrips()
    {
        var (p, r) = Make();
        SaveSystem.SaveGame(new GameState(p, r, difficulty: Difficulty.Hard), "s5");
        SaveSystem.LoadGame("s5").Difficulty.Should().Be(Difficulty.Hard);
    }

    [Fact]
    public void SaveLoad_PlayerAttackAndDefense_RoundTrips()
    {
        var (p, r) = Make(); p.Attack = 18; p.Defense = 9;
        SaveSystem.SaveGame(new GameState(p, r), "s6");
        var l = SaveSystem.LoadGame("s6");
        l.Player.Attack.Should().Be(18); l.Player.Defense.Should().Be(9);
    }

    [Fact]
    public void SaveLoad_PlayerInventoryItems_RoundTrip()
    {
        var (p, r) = Make();
        p.Inventory.Add(new Item { Name = "Potion of Testing", Type = ItemType.Consumable });
        p.Inventory.Add(new Item { Name = "Ancient Relic", Type = ItemType.Weapon });
        SaveSystem.SaveGame(new GameState(p, r), "s7");
        var l = SaveSystem.LoadGame("s7");
        l.Player.Inventory.Should().Contain(i => i.Name == "Potion of Testing");
        l.Player.Inventory.Should().Contain(i => i.Name == "Ancient Relic");
    }

    [Fact]
    public void SaveLoad_RoomProperties_RoundTrip()
    {
        var (p, _) = Make();
        var r = new Room { Description = "Throne room", IsExit = true, Visited = true };
        SaveSystem.SaveGame(new GameState(p, r), "s8");
        var l = SaveSystem.LoadGame("s8");
        l.CurrentRoom.Description.Should().Be("Throne room");
        l.CurrentRoom.IsExit.Should().BeTrue();
        l.CurrentRoom.Visited.Should().BeTrue();
    }

    [Fact]
    public void SaveLoad_RoomConnections_RoundTrip()
    {
        var (p, start) = Make();
        var north = new Room { Description = "Northern Passage" };
        start.Exits[Direction.North] = north; north.Exits[Direction.South] = start;
        SaveSystem.SaveGame(new GameState(p, start), "s9");
        var l = SaveSystem.LoadGame("s9");
        l.CurrentRoom.Exits.Should().ContainKey(Direction.North);
        l.CurrentRoom.Exits[Direction.North].Description.Should().Be("Northern Passage");
    }

    [Fact]
    public void SaveLoad_ShrineUsedFlag_RoundTrips()
    {
        var (p, r) = Make(); r.HasShrine = true; r.ShrineUsed = true;
        SaveSystem.SaveGame(new GameState(p, r), "s10");
        var l = SaveSystem.LoadGame("s10");
        l.CurrentRoom.HasShrine.Should().BeTrue();
        l.CurrentRoom.ShrineUsed.Should().BeTrue();
    }
}
