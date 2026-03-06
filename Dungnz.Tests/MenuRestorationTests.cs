using Dungnz.Engine;
using Dungnz.Engine.Commands;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Regression tests for ShowRoom() restoration after menu interactions.
/// Tests for issues #1168, #1169, #1170, #1171, #1172, #1173, #1174.
/// Each test verifies that ShowRoomCallCount increases after the command handler
/// completes, proving the room view is restored after menu interactions.
/// </summary>
public class MenuRestorationTests
{
    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        FakeDisplayService? display = null)
    {
        var p = player ?? new Player { Name = "Tester", HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var r = room ?? new Room { Description = "A dusty room." };
        var d = display ?? new FakeDisplayService();
        var combat = new Mock<ICombatEngine>().Object;
        var equipMgr = new EquipmentManager(d);
        var invMgr = new InventoryManager(d);
        var narration = new NarrationService(new Random(42));
        var achievements = new AchievementSystem();
        var logger = new Mock<ILogger>();

        return new CommandContext
        {
            Player = p,
            CurrentRoom = r,
            Rng = new Random(42),
            Stats = new RunStats(),
            SessionStats = new SessionStats(),
            RunStart = DateTime.UtcNow,
            Display = d,
            Combat = combat,
            Equipment = equipMgr,
            InventoryManager = invMgr,
            Narration = narration,
            Achievements = achievements,
            AllItems = new List<Item>(),
            Difficulty = DifficultySettings.For(Difficulty.Normal),
            DifficultyLevel = Difficulty.Normal,
            Logger = logger.Object,
            Events = new GameEvents(),
            CurrentFloor = 1,
            TurnConsumed = true,
            GameOver = false,
            ExitRun = _ => { },
            RecordRunEnd = (_, _) => { },
            GetCurrentlyEquippedForItem = (_, _) => null,
            GetDifficultyName = () => "Normal",
            HandleShrine = () => { },
            HandleContestedArmory = () => { },
            HandlePetrifiedLibrary = () => { },
            HandleTrapRoom = () => { },
        };
    }

    // ────────────────────────────────────────────────────────────────────────
    // #1168: InventoryCommandHandler — item select path calls ShowRoom
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryCommandHandler_SelectItem_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var item = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        player.Inventory.Add(item);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new InventoryCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called when user cancels inventory selection (returns null)");
    }

    [Fact]
    public void InventoryCommandHandler_EmptyInventory_DoesNotCallShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new InventoryCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().Be(initialShowRoomCount, 
            "ShowRoom should not be called when inventory is empty (early return)");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #1169: UseCommandHandler — item use from menu calls ShowRoom
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UseCommandHandler_CancelMenu_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        player.Inventory.Add(potion);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called when user cancels use menu (selected = null)");
    }

    [Fact]
    public void UseCommandHandler_NoUsableItems_DoesNotCallShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var sword = new Item { Name = "Sword", Type = ItemType.Weapon };
        player.Inventory.Add(sword);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().Be(initialShowRoomCount, 
            "ShowRoom should not be called when there are no usable items (early return)");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #1170: CompareCommandHandler — after comparison calls ShowRoom
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompareCommandHandler_CancelMenu_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, IsEquippable = true };
        player.Inventory.Add(sword);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new CompareCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called when user cancels comparison menu (selected = null)");
    }

    [Fact]
    public void CompareCommandHandler_NoEquippableItems_DoesNotCallShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        player.Inventory.Add(potion);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new CompareCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().Be(initialShowRoomCount, 
            "ShowRoom should not be called when there are no equippable items (early return)");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #1171: ExamineCommandHandler — after item detail calls ShowRoom
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ExamineCommandHandler_ItemInInventory_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Description = "A sturdy blade" };
        player.Inventory.Add(sword);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new ExamineCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle("sword", ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called after showing item details to restore room view");
    }

    [Fact]
    public void ExamineCommandHandler_ItemNotFound_DoesNotCallShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new ExamineCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle("nonexistent", ctx);

        display.ShowRoomCallCount.Should().Be(initialShowRoomCount, 
            "ShowRoom should not be called when item is not found (error path)");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #1172: StatsCommandHandler — calls ShowRoom; MapCommandHandler — calls ShowRoom
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void StatsCommandHandler_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new StatsCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called after showing stats to restore room view");
    }

    [Fact]
    public void MapCommandHandler_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new MapCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called after showing map to restore room view");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #1173: CraftCommandHandler — after craft attempt calls ShowRoom
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CraftCommandHandler_CancelMenu_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new CraftCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called after canceling craft menu to restore room view");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #1174: SkillsCommandHandler — after learning skill calls ShowRoom
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SkillsCommandHandler_CancelMenu_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new SkillsCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called when user cancels skill tree menu (skillToLearn = null)");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Additional edge cases
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryCommandHandler_WithEquippableItem_ShowsComparisonButStillCallsShowRoomOnCancel()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, IsEquippable = true, AttackBonus = 5 };
        player.Inventory.Add(sword);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new InventoryCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle(string.Empty, ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called on cancel even when item is equippable");
    }

    [Fact]
    public void CompareCommandHandler_WithDirectItemName_CallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100 };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, IsEquippable = true };
        player.Inventory.Add(sword);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new CompareCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle("Iron Sword", ctx);

        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called after comparison even when item name is provided directly");
    }

    [Fact]
    public void UseCommandHandler_WithDirectItemName_ConsumesItemAndCallsShowRoom()
    {
        var display = new FakeDisplayService();
        var player = new Player { Name = "Tester", HP = 50, MaxHP = 100 };
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 20 };
        player.Inventory.Add(potion);
        
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();
        var initialShowRoomCount = display.ShowRoomCallCount;

        handler.Handle("Health Potion", ctx);

        // When item name is provided directly (no menu), item is consumed
        player.Inventory.Should().NotContain(potion, "potion should be consumed");
        player.HP.Should().BeGreaterThan(50, "HP should be restored");
        
        display.ShowRoomCallCount.Should().BeGreaterThan(initialShowRoomCount, 
            "ShowRoom should be called after using item to restore room view");
    }
}
