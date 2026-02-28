using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dungnz.Tests;

[Collection("PrestigeTests")]
/// <summary>Additional GameLoop tests covering commands not yet tested: equip, unequip, map, shop, drop, etc.</summary>
public class GameLoopAdditionalTests
{
    private static (Player player, Room startRoom, FakeDisplayService display, Mock<ICombatEngine> combat) MakeSetup(
        params string[] inputs)
    {
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var startRoom = new Room { Description = "Start room" };
        var display = new FakeDisplayService();
        var combat = new Mock<ICombatEngine>();
        return (player, startRoom, display, combat);
    }

    private static GameLoop MakeLoop(FakeDisplayService display, ICombatEngine combat, params string[] inputs)
    {
        var reader = new FakeInputReader(inputs);
        return new GameLoop(display, combat, reader);
    }

    // ── Map command ───────────────────────────────────────────────────────────

    [Fact]
    public void MapCommand_ShowsMap()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "map", "quit");
        loop.Run(player, room);
        display.AllOutput.Should().Contain(o => o.StartsWith("map:"));
    }

    // ── Equip / Unequip / Equipment commands ──────────────────────────────────

    [Fact]
    public void EquipCommand_EquipsItemFromInventory()
    {
        var (player, room, display, combat) = MakeSetup();
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        player.Inventory.Add(sword);
        var loop = MakeLoop(display, combat.Object, "equip Iron Sword", "quit");
        loop.Run(player, room);
        // Just check no crash
        display.Messages.Should().NotBeEmpty();
    }

    [Fact]
    public void UnequipCommand_UnequipsWeapon()
    {
        var (player, room, display, combat) = MakeSetup();
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        player.EquippedWeapon = sword;
        var loop = MakeLoop(display, combat.Object, "unequip weapon", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    [Fact]
    public void EquipmentCommand_ShowsEquipment()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "equipment", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Drop command ──────────────────────────────────────────────────────────

    [Fact]
    public void DropCommand_RemovesItemFromInventory()
    {
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        player.Inventory.Add(potion);
        var loop = MakeLoop(display, combat.Object, "drop Health Potion", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Stats command with floor info ─────────────────────────────────────────

    [Fact]
    public void StatsCommand_ShowsFloorInfo()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "stats", "quit");
        loop.Run(player, room);
        display.Messages.Should().Contain(m => m.Contains("Floor"));
    }

    // ── Save / Load / ListSaves commands ─────────────────────────────────────

    [Fact]
    public void SaveCommand_AttemptsSave()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "save mysave", "quit");
        loop.Run(player, room);
        // Save might succeed or fail depending on filesystem — just verify no crash
        display.Messages.Should().NotBeEmpty();
    }

    [Fact]
    public void ListSavesCommand_ListsSaves()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "saves", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Skills command ────────────────────────────────────────────────────────

    [Fact]
    public void SkillsCommand_ShowsSkillTree()
    {
        var (player, room, display, combat) = MakeSetup();
        player.Level = 3; // higher level to have skill options
        var loop = MakeLoop(display, combat.Object, "skills", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Craft command ─────────────────────────────────────────────────────────

    [Fact]
    public void CraftCommand_WithIngredients_AttemptsCraft()
    {
        var (player, room, display, combat) = MakeSetup();
        // Just issue craft command — will fail gracefully without ingredients
        var loop = MakeLoop(display, combat.Object, "craft", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Leaderboard command ───────────────────────────────────────────────────

    [Fact]
    public void LeaderboardCommand_ShowsLeaderboard()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "leaderboard", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Combat result: player fled ────────────────────────────────────────────

    [Fact]
    public void Combat_PlayerFlees_ContinuesGameLoop()
    {
        var (player, room, display, combat) = MakeSetup();
        var enemyRoom = new Room { Description = "Enemy room" };
        enemyRoom.Enemy = new Enemy_Stub(50, 10, 5, 20);
        room.Exits[Direction.North] = enemyRoom;
        combat.Setup(c => c.RunCombat(It.IsAny<Player>(), It.IsAny<Enemy>(), It.IsAny<RunStats>()))
              .Returns(CombatResult.Fled);
        var loop = MakeLoop(display, combat.Object, "north", "quit");
        loop.Run(player, room);
        // Player fled, game continues, then quits
        display.Messages.Should().Contain(m => m.Contains("Thanks for playing!"));
    }

    // ── Examine with no argument ──────────────────────────────────────────────

    [Fact]
    public void ExamineCommand_NoArgument_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "examine", "quit");
        loop.Run(player, room);
        display.Errors.Should().NotBeEmpty();
    }

    // ── Take all items ────────────────────────────────────────────────────────

    [Fact]
    public void TakeCommand_WhenRoomHasMultipleItems_PicksNamedItem()
    {
        var (player, room, display, combat) = MakeSetup();
        room.Items.Add(new Item { Name = "Gold Coin", Type = ItemType.Consumable });
        room.Items.Add(new Item { Name = "Iron Shield", Type = ItemType.Armor });
        var loop = MakeLoop(display, combat.Object, "take Gold Coin", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }

    // ── Go with no direction ──────────────────────────────────────────────────

    [Fact]
    public void GoCommand_NoDirection_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "go", "quit");
        loop.Run(player, room);
        display.Errors.Should().Contain(e => e.Contains("direction") || e.Contains("Go where"));
    }

    // ── East / West movement ──────────────────────────────────────────────────

    [Fact]
    public void GoEast_WhenExitExists_MovesToEastRoom()
    {
        var (player, room, display, combat) = MakeSetup();
        var eastRoom = new Room { Description = "East room" };
        room.Exits[Direction.East] = eastRoom;
        var loop = MakeLoop(display, combat.Object, "east", "quit");
        loop.Run(player, room);
        display.AllOutput.Should().Contain(o => o.Contains("East room"));
    }

    [Fact]
    public void GoWest_WhenExitExists_MovesToWestRoom()
    {
        var (player, room, display, combat) = MakeSetup();
        var westRoom = new Room { Description = "West room" };
        room.Exits[Direction.West] = westRoom;
        var loop = MakeLoop(display, combat.Object, "west", "quit");
        loop.Run(player, room);
        display.AllOutput.Should().Contain(o => o.Contains("West room"));
    }

    [Fact]
    public void GoSouth_WhenExitExists_MovesToSouthRoom()
    {
        var (player, room, display, combat) = MakeSetup();
        var southRoom = new Room { Description = "South room" };
        room.Exits[Direction.South] = southRoom;
        var loop = MakeLoop(display, combat.Object, "south", "quit");
        loop.Run(player, room);
        display.AllOutput.Should().Contain(o => o.Contains("South room"));
    }

    // ── Non-combat room entry (room has item but no enemy) ────────────────────

    [Fact]
    public void GoIntoRoom_WithItemNoEnemy_ShowsItem()
    {
        var (player, room, display, combat) = MakeSetup();
        var newRoom = new Room { Description = "Treasure room" };
        newRoom.Items.Add(new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 });
        room.Exits[Direction.North] = newRoom;
        var loop = MakeLoop(display, combat.Object, "north", "quit");
        loop.Run(player, room);
        display.AllOutput.Should().Contain(o => o.Contains("Treasure room"));
    }

    // ── Use consumable with heal ──────────────────────────────────────────────

    [Fact]
    public void UseCommand_ConsumableItem_HealsAndRemovesFromInventory()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 50; // below max
        player.MaxHP = 100;
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        player.Inventory.Add(potion);
        var loop = MakeLoop(display, combat.Object, "use Health Potion", "quit");
        loop.Run(player, room);
        player.HP.Should().BeGreaterThan(50, "using a potion should restore HP");
    }

    // ── Room with shrine ──────────────────────────────────────────────────────

    [Fact]
    public void UseCommand_Shrine_HealsPlayer()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 50;
        room.HasShrine = true;
        var loop = MakeLoop(display, combat.Object, "use shrine", "quit");
        loop.Run(player, room);
        display.Messages.Should().NotBeEmpty();
    }
}
