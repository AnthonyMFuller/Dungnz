using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class SaveSystemTests : IDisposable
{
    private readonly string _saveDir;

    public SaveSystemTests()
    {
        _saveDir = Path.Combine(Path.GetTempPath(), $"dungnz_save_test_{Guid.NewGuid()}");
        SaveSystem.OverrideSaveDirectory(_saveDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_saveDir))
            Directory.Delete(_saveDir, recursive: true);
    }

    private static GameState MakeState(string saveName = "test")
    {
        var player = new Player { Name = "Tester" };
        player.AddXP(50);
        player.AddGold(25);
        var room = new Room { Description = "A dark corridor" };
        return new GameState(player, room);
    }

    [Fact]
    public void RoundTrip_PlayerStats_Preserved()
    {
        var player = new Player { Name = "Tester" };
        player.TakeDamage(10);
        player.AddGold(99);
        player.AddXP(150);
        player.LevelUp();
        var state = new GameState(player, new Room { Description = "Room" });

        SaveSystem.SaveGame(state, "stats");
        var loaded = SaveSystem.LoadGame("stats");

        loaded.Player.Name.Should().Be("Tester");
        loaded.Player.HP.Should().Be(player.HP);
        loaded.Player.Gold.Should().Be(99);
        loaded.Player.XP.Should().Be(150);
        loaded.Player.Level.Should().Be(player.Level);
    }

    [Fact]
    public void RoundTrip_InventoryItems_Preserved()
    {
        var player = new Player { Name = "Tester" };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        player.Inventory.Add(sword);
        var state = new GameState(player, new Room { Description = "Room" });

        SaveSystem.SaveGame(state, "inventory");
        var loaded = SaveSystem.LoadGame("inventory");

        loaded.Player.Inventory.Should().ContainSingle(i => i.Name == "Iron Sword");
    }

    [Fact]
    public void RoundTrip_RoomGraph_Reconnected()
    {
        var roomA = new Room { Description = "Room A" };
        var roomB = new Room { Description = "Room B" };
        roomA.Exits[Direction.North] = roomB;
        roomB.Exits[Direction.South] = roomA;
        var state = new GameState(new Player { Name = "Tester" }, roomA);

        SaveSystem.SaveGame(state, "roomgraph");
        var loaded = SaveSystem.LoadGame("roomgraph");

        loaded.CurrentRoom.Exits.Should().ContainKey(Direction.North);
        loaded.CurrentRoom.Exits[Direction.North].Description.Should().Be("Room B");
    }

    [Fact]
    public void RoundTrip_RoomFlags_Preserved()
    {
        var room = new Room { Description = "Exit Room", IsExit = true, Visited = true, Looted = true };
        var state = new GameState(new Player { Name = "Tester" }, room);

        SaveSystem.SaveGame(state, "flags");
        var loaded = SaveSystem.LoadGame("flags");

        loaded.CurrentRoom.IsExit.Should().BeTrue();
        loaded.CurrentRoom.Visited.Should().BeTrue();
        loaded.CurrentRoom.Looted.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_ShrineState_Preserved()
    {
        var room = new Room { Description = "Shrine Room", HasShrine = true, ShrineUsed = true };
        var state = new GameState(new Player { Name = "Tester" }, room);

        SaveSystem.SaveGame(state, "shrine");
        var loaded = SaveSystem.LoadGame("shrine");

        loaded.CurrentRoom.HasShrine.Should().BeTrue();
        loaded.CurrentRoom.ShrineUsed.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_EnemyInRoom_Preserved()
    {
        var room = new Room { Description = "Combat Room" };
        var goblin = new Dungnz.Systems.Enemies.Goblin();
        goblin.HP = 15;
        room.Enemy = goblin;
        var state = new GameState(new Player { Name = "Tester" }, room);

        SaveSystem.SaveGame(state, "enemy");
        var loaded = SaveSystem.LoadGame("enemy");

        loaded.CurrentRoom.Enemy.Should().NotBeNull();
        loaded.CurrentRoom.Enemy!.Name.Should().Be("Goblin");
        loaded.CurrentRoom.Enemy.HP.Should().Be(15);
    }

    [Fact]
    public void LoadGame_NonExistentFile_ThrowsFileNotFoundException()
    {
        Action act = () => SaveSystem.LoadGame("does_not_exist_xyz");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ListSaves_ReturnsSaveNames_SortedByRecency()
    {
        var state = MakeState();

        SaveSystem.SaveGame(state, "older");
        System.Threading.Thread.Sleep(20); // ensure different write times
        SaveSystem.SaveGame(state, "newer");

        var saves = SaveSystem.ListSaves();

        saves.Should().Contain("older").And.Contain("newer");
        saves[0].Should().Be("newer");
    }

    [Fact]
    public void SaveGame_CreatesDirectory_IfNotExists()
    {
        var subDir = Path.Combine(_saveDir, "sub");
        SaveSystem.OverrideSaveDirectory(subDir);

        Directory.Exists(subDir).Should().BeFalse("dir should not exist yet");

        var state = MakeState();
        SaveSystem.SaveGame(state, "newdir");

        Directory.Exists(subDir).Should().BeTrue();
        File.Exists(Path.Combine(subDir, "newdir.json")).Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_CurrentRoomId_Matches()
    {
        var room = new Room { Description = "Start Room" };
        var state = new GameState(new Player { Name = "Tester" }, room);
        var originalId = room.Id;

        SaveSystem.SaveGame(state, "currentroom");
        var loaded = SaveSystem.LoadGame("currentroom");

        loaded.CurrentRoom.Id.Should().Be(originalId);
    }

    [Fact]
    public void RoundTrip_BossEnrageState_Preserved()
    {
        var boss = new Dungnz.Systems.Enemies.DungeonBoss();
        boss.HP = 30; // below 40 % threshold
        boss.IsEnraged = true;
        boss.IsCharging = true;
        boss.ChargeActive = false;

        var exitRoom = new Room { Description = "Boss Chamber", IsExit = true };
        exitRoom.Enemy = boss;
        var state = new GameState(new Player { Name = "Tester" }, exitRoom);

        SaveSystem.SaveGame(state, "boss");
        var loaded = SaveSystem.LoadGame("boss");

        var loadedBoss = loaded.CurrentRoom.Enemy as Dungnz.Systems.Enemies.DungeonBoss;
        loadedBoss.Should().NotBeNull();
        loadedBoss!.IsEnraged.Should().BeTrue();
        loadedBoss.IsCharging.Should().BeTrue();
        loadedBoss.ChargeActive.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_CurrentFloor_Preserved()
    {
        var state = new GameState(new Player { Name = "Tester" }, new Room { Description = "Floor3" }, 3);

        SaveSystem.SaveGame(state, "floor");
        var loaded = SaveSystem.LoadGame("floor");

        loaded.CurrentFloor.Should().Be(3);
    }

    [Fact]
    public void RoundTrip_UnlockedSkills_Preserved()
    {
        var player = new Player { Name = "Tester" };
        player.Skills.Unlock(Dungnz.Systems.Skill.PowerStrike);
        player.Skills.Unlock(Dungnz.Systems.Skill.Swiftness);
        var state = new GameState(player, new Room { Description = "Skill Room" });

        SaveSystem.SaveGame(state, "skills");
        var loaded = SaveSystem.LoadGame("skills");

        loaded.Player.Skills.IsUnlocked(Dungnz.Systems.Skill.PowerStrike).Should().BeTrue();
        loaded.Player.Skills.IsUnlocked(Dungnz.Systems.Skill.Swiftness).Should().BeTrue();
        loaded.Player.Skills.IsUnlocked(Dungnz.Systems.Skill.IronSkin).Should().BeFalse();
    }

    [Theory]
    [InlineData("lichking")]
    [InlineData("stonetitan")]
    [InlineData("shadowwraith")]
    [InlineData("vampireboss")]
    public void RoundTrip_BossVariant_Preserved(string bossType)
    {
        Dungnz.Systems.Enemies.DungeonBoss boss = bossType switch
        {
            "lichking"     => new Dungnz.Systems.Enemies.LichKing(),
            "stonetitan"   => new Dungnz.Systems.Enemies.StoneTitan(),
            "shadowwraith" => new Dungnz.Systems.Enemies.ShadowWraith(),
            _              => new Dungnz.Systems.Enemies.VampireBoss(),
        };
        boss.IsEnraged = true;
        var room = new Room { Description = "Boss Room" };
        room.Enemy = boss;
        var state = new GameState(new Player { Name = "Tester" }, room);

        SaveSystem.SaveGame(state, $"boss_{bossType}");
        var loaded = SaveSystem.LoadGame($"boss_{bossType}");

        var loadedBoss = loaded.CurrentRoom.Enemy as Dungnz.Systems.Enemies.DungeonBoss;
        loadedBoss.Should().NotBeNull();
        loadedBoss!.GetType().Should().Be(boss.GetType());
        loadedBoss.IsEnraged.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_RoomMerchantHazardType_Preserved()
    {
        var room = new Room
        {
            Description = "Shop",
            Merchant = new Merchant { Name = "Bob" },
            Hazard = HazardType.Fire,
            Type = RoomType.Scorched,
        };
        var state = new GameState(new Player { Name = "Tester" }, room);

        SaveSystem.SaveGame(state, "roomextra");
        var loaded = SaveSystem.LoadGame("roomextra");

        loaded.CurrentRoom.Merchant.Should().NotBeNull();
        loaded.CurrentRoom.Merchant!.Name.Should().Be("Bob");
        loaded.CurrentRoom.Hazard.Should().Be(HazardType.Fire);
        loaded.CurrentRoom.Type.Should().Be(RoomType.Scorched);
    }

}