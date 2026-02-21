using Dungnz.Display;
using Dungnz.Models;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for DisplayService that capture Console output.
/// These validate that display methods produce expected output without throwing.
/// </summary>
public class DisplayServiceTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;

    public DisplayServiceTests()
    {
        _originalOut = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    private string Output => _output.ToString();

    [Fact]
    public void ShowMessage_WritesMessage()
    {
        var svc = new ConsoleDisplayService();
        svc.ShowMessage("Hello world");
        Output.Should().Contain("Hello world");
    }

    [Fact]
    public void ShowError_WritesError()
    {
        var svc = new ConsoleDisplayService();
        svc.ShowError("Something went wrong");
        Output.Should().Contain("Something went wrong");
    }

    [Fact]
    public void ShowCombat_WritesCombatMessage()
    {
        var svc = new ConsoleDisplayService();
        svc.ShowCombat("A Goblin attacks!");
        Output.Should().Contain("Goblin attacks");
    }

    [Fact]
    public void ShowCombatMessage_WritesMessage()
    {
        var svc = new ConsoleDisplayService();
        svc.ShowCombatMessage("You hit for 5 damage!");
        Output.Should().Contain("5 damage");
    }

    [Fact]
    public void ShowLootDrop_WritesItemName()
    {
        var svc = new ConsoleDisplayService();
        var item = new Item { Name = "Magic Sword" };
        svc.ShowLootDrop(item);
        Output.Should().Contain("Magic Sword");
    }

    [Fact]
    public void ShowCommandPrompt_WritesPrompt()
    {
        var svc = new ConsoleDisplayService();
        svc.ShowCommandPrompt();
        Output.Should().Contain(">");
    }

    [Fact]
    public void ShowCombatPrompt_WritesOptions()
    {
        var svc = new ConsoleDisplayService();
        svc.ShowCombatPrompt();
        Output.Should().Contain("ttack");
    }

    [Fact]
    public void ShowCombatStatus_WritesHp()
    {
        var svc = new ConsoleDisplayService();
        var player = new Player { HP = 80, MaxHP = 100 };
        var enemy = new Enemy_Stub(30, 5, 2, 10);
        svc.ShowCombatStatus(player, enemy);
        Output.Should().Contain("80/100").And.Contain("30");
    }

    [Fact]
    public void ShowPlayerStats_WritesStats()
    {
        var svc = new ConsoleDisplayService();
        var player = new Player { Name = "Hero", HP = 100, MaxHP = 100, Attack = 10, Defense = 5, Gold = 50, XP = 200, Level = 3 };
        svc.ShowPlayerStats(player);
        Output.Should().Contain("Hero")
            .And.Contain("100")
            .And.Contain("10")
            .And.Contain("50")
            .And.Contain("200")
            .And.Contain("3");
    }

    [Fact]
    public void ShowInventory_EmptyInventory_ShowsEmpty()
    {
        var svc = new ConsoleDisplayService();
        var player = new Player();
        svc.ShowInventory(player);
        Output.Should().Contain("empty");
    }

    [Fact]
    public void ShowInventory_WithItems_ShowsItemNames()
    {
        var svc = new ConsoleDisplayService();
        var player = new Player();
        player.Inventory.Add(new Item { Name = "Iron Sword", Type = ItemType.Weapon });
        svc.ShowInventory(player);
        Output.Should().Contain("Iron Sword");
    }

    [Fact]
    public void ShowHelp_WritesCommands()
    {
        var svc = new ConsoleDisplayService();
        svc.ShowHelp();
        Output.Should().ContainAny("help", "COMMANDS", "quit");
    }

    [Fact]
    public void ShowRoom_WritesDescription()
    {
        var svc = new ConsoleDisplayService();
        var room = new Room { Description = "A dark stone chamber" };
        svc.ShowRoom(room);
        Output.Should().Contain("dark stone chamber");
    }

    [Fact]
    public void ShowRoom_WithEnemy_ShowsEnemyName()
    {
        var svc = new ConsoleDisplayService();
        var room = new Room { Description = "Test room" };
        room.Enemy = new Enemy_Stub(20, 5, 2, 10);
        room.Enemy.Name = "Goblin";
        svc.ShowRoom(room);
        Output.Should().Contain("Goblin");
    }

    [Fact]
    public void ShowRoom_WithItems_ShowsItemNames()
    {
        var svc = new ConsoleDisplayService();
        var room = new Room { Description = "Test room" };
        room.Items.Add(new Item { Name = "Sword" });
        svc.ShowRoom(room);
        Output.Should().Contain("Sword");
    }

    [Fact]
    public void ShowRoom_WithExits_ShowsExits()
    {
        var svc = new ConsoleDisplayService();
        var room = new Room { Description = "Test room" };
        room.Exits[Direction.North] = new Room { Description = "North room" };
        svc.ShowRoom(room);
        Output.Should().Contain("North");
    }
}
