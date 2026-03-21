using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests.Regression;

/// <summary>
/// Console-mode regression suite — verifies core game paths still work through
/// <see cref="FakeDisplayService"/> and <see cref="FakeInputReader"/> after any
/// Avalonia work. Focus is "no crash" + expected state transitions.
/// </summary>
public class ConsoleRegressionTests
{
    // ── Scenario 1: Game initialization ────────────────────────────────────

    [Fact]
    public void Initialization_CreatePlayerAndRoom_NoCrash()
    {
        // Arrange & Act
        var player = new PlayerBuilder()
            .Named("Regressor")
            .WithHP(100).WithMaxHP(100)
            .WithAttack(10).WithDefense(5)
            .Build();
        var room = new RoomBuilder()
            .Named("Starting Chamber")
            .OfType(RoomType.Standard)
            .Build();

        // Assert — basic objects created without exception
        player.Name.Should().Be("Regressor");
        player.HP.Should().Be(100);
        room.Description.Should().Be("Starting Chamber");
        room.Enemy.Should().BeNull();
    }

    // ── Scenario 2: Room navigation ────────────────────────────────────────

    [Fact]
    public void Navigation_MovesBetweenRooms_UpdatesState()
    {
        // Arrange
        var roomA = new RoomBuilder().Named("Room A").Build();
        var roomB = new RoomBuilder().Named("Room B").Build();
        roomA.Exits[Direction.North] = roomB;
        roomB.Exits[Direction.South] = roomA;

        // Act
        var currentRoom = roomA;
        if (currentRoom.Exits.TryGetValue(Direction.North, out var next))
            currentRoom = next;

        // Assert
        currentRoom.Should().BeSameAs(roomB);
        currentRoom.Description.Should().Be("Room B");
        currentRoom.Exits.Should().ContainKey(Direction.South);
    }

    // ── Scenario 3: Combat resolution ──────────────────────────────────────

    [Fact]
    public void Combat_AttackWeakEnemy_WinsCombat()
    {
        // Arrange
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A"); // Attack
        var rng = new ControlledRandom(defaultDouble: 0.9); // no crits
        var player = new PlayerBuilder()
            .WithHP(100).WithMaxHP(100)
            .WithAttack(50).WithDefense(10)
            .Build();
        var enemy = new EnemyBuilder()
            .Named("Weak Rat")
            .WithHP(1).WithAttack(1).WithDefense(0)
            .Build();

        var combat = new CombatEngine(display, input, rng);
        var stats = new RunStats();

        // Act
        var result = combat.RunCombat(player, enemy, stats);

        // Assert
        result.Should().Be(CombatResult.Won);
        player.HP.Should().BeGreaterThan(0);
        stats.EnemiesDefeated.Should().BeGreaterOrEqualTo(1);
        display.AllOutput.Should().NotBeEmpty();
    }

    // ── Scenario 4: Equipment equip/unequip ────────────────────────────────

    [Fact]
    public void Inventory_EquipAndUnequipItem_StateChanges()
    {
        // Arrange
        var display = new FakeDisplayService();
        var player = new PlayerBuilder()
            .WithAttack(10).WithDefense(5)
            .Build();
        var sword = new ItemBuilder()
            .Named("Test Sword")
            .OfType(ItemType.Weapon)
            .WithDamage(8)
            .AsEquippable()
            .Build();
        player.Inventory.Add(sword);

        var equipMgr = new EquipmentManager(display);

        // Act — equip by name (HandleEquip API)
        var (success, errorMessage) = equipMgr.HandleEquip(player, "test sword");

        // Assert — equipped
        success.Should().BeTrue();
        errorMessage.Should().BeNull();
        player.EquippedWeapon.Should().BeSameAs(sword);

        // Act — unequip by slot name
        equipMgr.HandleUnequip(player, "weapon");

        // Assert — unequipped
        player.EquippedWeapon.Should().BeNull();
    }

    // ── Scenario 5: Display method coverage ────────────────────────────────

    [Fact]
    public void DisplayMethods_CalledThroughFakeDisplayService_NoExceptions()
    {
        // Arrange
        var display = new FakeDisplayService();
        var player = new PlayerBuilder()
            .WithHP(80).WithMaxHP(100)
            .WithGold(50)
            .Build();
        var enemy = new EnemyBuilder().Named("Goblin").WithHP(30).Build();
        var room = new RoomBuilder()
            .Named("Test Room")
            .WithEnemy(enemy)
            .Build();
        var item = new ItemBuilder().Named("Potion").WithHeal(20).Build();

        // Act & Assert — none of these should throw
        var actions = new List<Action>
        {
            () => display.ShowTitle(),
            () => display.ShowHelp(),
            () => display.ShowRoom(room),
            () => display.ShowMap(room, 1),
            () => display.ShowMessage("Test message"),
            () => display.ShowError("Test error"),
            () => display.ShowCombat("You attack!"),
            () => display.ShowCombatMessage("Critical hit!"),
            () => display.ShowCombatStatus(player, enemy,
                new List<ActiveEffect>(), new List<ActiveEffect>()),
            () => display.ShowPlayerStats(player),
            () => display.RefreshDisplay(player, room, 1),
            () => display.ShowInventory(player),
            () => display.ShowLootDrop(item, player),
            () => display.ShowGoldPickup(10, 60),
            () => display.ShowEquipment(player),
            () => display.ShowItemDetail(item),
            () => display.ShowColoredMessage("colored", "red"),
            () => display.ShowColoredCombatMessage("combat colored", "green"),
            () => display.ShowColoredStat("HP", "80/100", "yellow"),
            () => display.ShowCombatStart(enemy),
            () => display.ShowEnemyDetail(enemy),
            () => display.ShowFloorBanner(1, 5, DungeonVariant.ForFloor(1)),
            () => display.ShowCombatHistory(),
        };

        foreach (var action in actions)
            action.Should().NotThrow();

        // Verify some output was captured
        display.AllOutput.Should().NotBeEmpty();
        display.Messages.Should().NotBeEmpty();
    }

    // ── Scenario 6: Full game loop smoke test ──────────────────────────────

    [Fact]
    public void GameLoopSmoke_ScriptedInputs_CompletesGracefully()
    {
        // Arrange — scripted inputs: look around, then quit
        var input = new FakeInputReader("look", "stats", "help", "inventory", "quit");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(defaultDouble: 0.9);
        var combat = new CombatEngine(display, input, rng);
        var events = new GameEvents();
        var navigator = new FakeMenuNavigator();

        var loop = new GameLoop(display, combat, input, events,
            seed: 42,
            difficulty: DifficultySettings.For(Difficulty.Normal),
            navigator: navigator);

        // Build a player and starting room
        var player = new PlayerBuilder()
            .Named("SmokePlayer")
            .WithHP(100).WithMaxHP(100)
            .WithAttack(10).WithDefense(5)
            .Build();
        var room = new RoomBuilder()
            .Named("Starting Room")
            .Build();

        // Act & Assert — should complete without throwing
        var act = () => loop.Run(player, room);
        act.Should().NotThrow();

        // Verify game produced output
        display.AllOutput.Should().NotBeEmpty();
    }

    // ── Scenario 7: Multiple combats in sequence ───────────────────────────

    [Fact]
    public void SequentialCombats_PlayerSurvivesMultipleEncounters()
    {
        // Arrange
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "A", "A"); // Attack each
        var rng = new ControlledRandom(defaultDouble: 0.9);
        var player = new PlayerBuilder()
            .WithHP(200).WithMaxHP(200)
            .WithAttack(50).WithDefense(15)
            .Build();
        var combat = new CombatEngine(display, input, rng);
        var stats = new RunStats();

        // Act — fight three weak enemies
        for (int i = 0; i < 3; i++)
        {
            var enemy = new EnemyBuilder()
                .Named($"Rat #{i + 1}")
                .WithHP(1).WithAttack(1).WithDefense(0)
                .Build();
            var result = combat.RunCombat(player, enemy, stats);
            result.Should().Be(CombatResult.Won);
        }

        // Assert
        player.HP.Should().BeGreaterThan(0);
        stats.EnemiesDefeated.Should().BeGreaterOrEqualTo(3);
    }

    // ── Scenario 8: Display service output capture consistency ─────────────

    [Fact]
    public void FakeDisplayService_CapturesAllOutputCategories()
    {
        // Arrange
        var display = new FakeDisplayService();

        // Act
        display.ShowMessage("msg1");
        display.ShowError("err1");
        display.ShowCombat("cbt1");
        display.ShowCombatMessage("cmsg1");

        // Assert — each category captured independently
        display.Messages.Should().Contain("msg1");
        display.Errors.Should().Contain("err1");
        display.CombatMessages.Should().Contain("cbt1");
        display.CombatMessages.Should().Contain("cmsg1");

        // AllOutput captures everything
        display.AllOutput.Should().HaveCountGreaterOrEqualTo(4);
    }

    // ── Scenario 9: Player stats display ───────────────────────────────────

    [Fact]
    public void ShowPlayerStats_CapturesPlayerName()
    {
        // Arrange
        var display = new FakeDisplayService();
        var player = new PlayerBuilder()
            .Named("StatsHero")
            .WithHP(75).WithMaxHP(100)
            .WithLevel(5)
            .Build();

        // Act
        display.ShowPlayerStats(player);

        // Assert
        display.AllOutput.Should().Contain(s => s.Contains("StatsHero"));
    }

    // ── Scenario 10: Room with items ───────────────────────────────────────

    [Fact]
    public void RoomWithItems_ShowRoom_CapturesDescription()
    {
        // Arrange
        var display = new FakeDisplayService();
        var loot = new ItemBuilder().Named("Gold Ring").Build();
        var room = new RoomBuilder()
            .Named("Treasure Vault")
            .WithLoot(loot)
            .Build();

        // Act
        display.ShowRoom(room);

        // Assert
        display.AllOutput.Should().Contain(s => s.Contains("Treasure Vault"));
        display.ShowRoomCallCount.Should().Be(1);
    }
}
