using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dungnz.Tests;

public class GameLoopTests
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

    [Fact]
    public void QuitCommand_EndsGameLoop()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "quit");
        loop.Run(player, room); // should return
        display.Messages.Should().Contain("Thanks for playing!");
    }

    [Fact]
    public void LookCommand_ShowsRoom()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "look", "quit");
        loop.Run(player, room);
        display.AllOutput.Count(o => o.StartsWith("room:")).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void GoNorth_WhenExitExists_MovesPlayer()
    {
        var (player, room, display, combat) = MakeSetup();
        var northRoom = new Room { Description = "North room" };
        room.Exits[Direction.North] = northRoom;

        var loop = MakeLoop(display, combat.Object, "north", "quit");
        loop.Run(player, room);

        display.AllOutput.Should().Contain(o => o.Contains("North room"));
    }

    [Fact]
    public void GoNorth_WhenNoExit_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "north", "quit");
        loop.Run(player, room);

        display.Errors.Should().Contain(e => e.Contains("can't go that way"));
    }

    [Fact]
    public void BossGate_CannotEnterExitRoomWithBossAlive()
    {
        var (player, room, display, combat) = MakeSetup();
        var bossRoom = new Room { Description = "Boss room", IsExit = true };
        bossRoom.Enemy = new Enemy_Stub(100, 10, 5, 50);
        room.Exits[Direction.North] = bossRoom;

        var loop = MakeLoop(display, combat.Object, "north", "quit");
        loop.Run(player, room);

        display.Errors.Should().Contain(e => e.Contains("boss blocks"));
    }

    [Fact]
    public void WinCondition_EnteringExitRoomWithBossDead()
    {
        var (player, room, display, combat) = MakeSetup();
        var exitRoom = new Room { Description = "Exit room", IsExit = true, Enemy = null };
        room.Exits[Direction.North] = exitRoom;

        var loop = MakeLoop(display, combat.Object, "north");
        loop.Run(player, room);

        display.Messages.Should().Contain(m => m.Contains("Floor 1") || m.Contains("escaped") || m.Contains("cleared"));
    }

    [Fact]
    public void TakeCommand_PicksUpItemFromRoom()
    {
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        room.Items.Add(potion);

        var loop = MakeLoop(display, combat.Object, "take potion", "quit");
        loop.Run(player, room);

        player.Inventory.Should().Contain(potion);
        room.Items.Should().NotContain(potion);
    }

    [Fact]
    public void TakeCommand_NoItem_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "take sword", "quit");
        loop.Run(player, room);

        display.Errors.Should().Contain(e => e.Contains("sword"));
    }

    [Fact]
    public void UseCommand_HealsConsumable()
    {
        var (player, room, display, combat) = MakeSetup();
        player.HP = 50;
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        player.Inventory.Add(potion);

        var loop = MakeLoop(display, combat.Object, "use potion", "quit");
        loop.Run(player, room);

        player.HP.Should().Be(80);
        player.Inventory.Should().NotContain(potion);
    }

    [Fact]
    public void UseCommand_EquipsWeapon_AddsAttackBonus()
    {
        var (player, room, display, combat) = MakeSetup();
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5, IsEquippable = true };
        player.Inventory.Add(sword);

        var loop = MakeLoop(display, combat.Object, "equip sword", "quit");
        loop.Run(player, room);

        player.Attack.Should().Be(15);
        player.Inventory.Should().NotContain(sword);
    }

    [Fact]
    public void UseCommand_ItemNotInInventory_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "use potion", "quit");
        loop.Run(player, room);

        display.Errors.Should().Contain(e => e.Contains("potion"));
    }

    [Fact]
    public void ExamineCommand_Enemy_ShowsStats()
    {
        var (player, room, display, combat) = MakeSetup();
        var enemy = new Enemy_Stub(30, 8, 2, 15) { };
        // Use a named enemy
        enemy.Name = "Goblin";
        room.Enemy = enemy;

        var loop = MakeLoop(display, combat.Object, "examine goblin", "quit");
        loop.Run(player, room);

        display.Messages.Should().Contain(m => m.Contains("Goblin") && m.Contains("HP"));
    }

    [Fact]
    public void ExamineCommand_RoomItem_ShowsDescription()
    {
        var (player, room, display, combat) = MakeSetup();
        var sword = new Item { Name = "Iron Sword", Description = "A sturdy blade", Type = ItemType.Weapon };
        room.Items.Add(sword);

        var loop = MakeLoop(display, combat.Object, "examine sword", "quit");
        loop.Run(player, room);

        display.Messages.Should().Contain(m => m.Contains("Iron Sword") && m.Contains("sturdy blade"));
    }

    [Fact]
    public void ExamineCommand_InventoryItem_ShowsDescription()
    {
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Description = "Restores 20 HP", Type = ItemType.Consumable };
        player.Inventory.Add(potion);

        var loop = MakeLoop(display, combat.Object, "examine potion", "quit");
        loop.Run(player, room);

        display.Messages.Should().Contain(m => m.Contains("Health Potion") && m.Contains("Restores 20 HP"));
    }

    [Fact]
    public void StatsCommand_DisplaysStats()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "stats", "quit");
        loop.Run(player, room);

        display.AllOutput.Should().Contain(o => o.StartsWith("stats:"));
    }

    [Fact]
    public void InventoryCommand_DisplaysInventory()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "inventory", "quit");
        loop.Run(player, room);

        display.AllOutput.Should().Contain(o => o.StartsWith("inventory:"));
    }

    [Fact]
    public void PlayerDeathInCombat_EndsGameLoop()
    {
        var (player, room, display, combat) = MakeSetup();
        var enemy = new Enemy_Stub(100, 10, 5, 50);
        var nextRoom = new Room { Description = "Danger room", Enemy = enemy };
        room.Exits[Direction.North] = nextRoom;

        combat.Setup(c => c.RunCombat(player, enemy, It.IsAny<RunStats?>())).Returns(CombatResult.PlayerDied);

        var loop = MakeLoop(display, combat.Object, "north");
        loop.Run(player, room);

        display.Messages.Should().Contain(m => m.Contains("YOU HAVE FALLEN") || m.Contains("defeated"));
    }

    [Fact]
    public void HelpCommand_DisplaysHelp()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "help", "quit");
        loop.Run(player, room);

        display.AllOutput.Should().Contain(o => o == "help");
    }

    [Fact]
    public void UnknownCommand_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "xyzzy", "quit");
        loop.Run(player, room);

        display.Errors.Should().Contain(e => e.Contains("Unknown command"));
    }

    [Fact]
    public void GoWithNoExit_GoSouth_ShowsError()
    {
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "go", "quit");
        loop.Run(player, room);

        display.Errors.Should().Contain(e => e.Contains("direction"));
    }
}
