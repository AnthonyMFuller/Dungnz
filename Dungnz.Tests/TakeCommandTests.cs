using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for TAKE command enhancements:
///   1. No-argument TAKE → interactive menu via ShowTakeMenuAndSelect
///   2. Take All sentinel (Name == "__TAKE_ALL__") → picks up every room item
///   3. Fuzzy Levenshtein matching for typed argument
/// NOTE: These tests require Barton's squad/take-command-menu branch to be merged
/// (adds ShowTakeMenuAndSelect to IDisplayService and FakeDisplayService).
/// </summary>
[Collection("PrestigeTests")]
public class TakeCommandTests
{
    /// <summary>
    /// Subclass that re-implements IDisplayService so the GameLoop's interface-typed
    /// _display field dispatches ShowTakeMenuAndSelect to this class, not FakeDisplayService.
    /// Once Barton adds ShowTakeMenuAndSelect to FakeDisplayService, this override takes
    /// precedence via C# interface re-implementation semantics.
    /// </summary>
    private sealed class TakeFakeDisplay : FakeDisplayService, IDisplayService
    {
        public bool ShowTakeMenuCalled { get; private set; }
        public Item? ShowTakeMenuResult { get; set; }

        // Re-implements IDisplayService.ShowTakeMenuAndSelect, so interface dispatch
        // lands here even though GameLoop holds _display as IDisplayService.
        public new Item? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems)
        {
            ShowTakeMenuCalled = true;
            AllOutput.Add("take_menu");
            return ShowTakeMenuResult;
        }
    }

    private static (Player player, Room room, TakeFakeDisplay display, Mock<ICombatEngine> combat) MakeSetup()
    {
        var player = new Player { Name = "Tester", HP = 100, MaxHP = 100, Attack = 10, Defense = 5 };
        var room = new Room { Description = "Test room" };
        var display = new TakeFakeDisplay();
        var combat = new Mock<ICombatEngine>();
        return (player, room, display, combat);
    }

    private static GameLoop MakeLoop(TakeFakeDisplay display, ICombatEngine combat, params string[] inputs)
    {
        var reader = new FakeInputReader(inputs);
        return new GameLoop(display, combat, reader);
    }

    // ── No-argument TAKE ──────────────────────────────────────────────────────

    [Fact]
    public void HandleTake_NoArgument_EmptyRoom_ShowsError()
    {
        // Arrange — room has no items
        var (player, room, display, combat) = MakeSetup();
        var loop = MakeLoop(display, combat.Object, "take", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        display.ShowTakeMenuCalled.Should().BeFalse("menu should not appear when there is nothing to take");
        display.Errors.Should().NotBeEmpty("an error should be shown when the room has no items");
    }

    [Fact]
    public void HandleTake_NoArgument_ItemsInRoom_ShowsMenu()
    {
        // Arrange
        var (player, room, display, combat) = MakeSetup();
        room.Items.Add(new Item { Name = "Health Potion", Type = ItemType.Consumable });
        display.ShowTakeMenuResult = null; // user cancels — we only care that menu appeared

        var loop = MakeLoop(display, combat.Object, "take", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        display.ShowTakeMenuCalled.Should().BeTrue("the take menu must appear when the room has items");
    }

    [Fact]
    public void HandleTake_NoArgument_UserCancels_NoItemTaken()
    {
        // Arrange
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        room.Items.Add(potion);
        display.ShowTakeMenuResult = null; // user cancels (Escape / null return)

        var loop = MakeLoop(display, combat.Object, "take", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        player.Inventory.Should().NotContain(potion, "cancelling the menu should not pick up any item");
        room.Items.Should().Contain(potion, "the item should remain in the room after cancel");
    }

    [Fact]
    public void HandleTake_NoArgument_UserSelectsItem_ItemMovedToInventory()
    {
        // Arrange
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, Weight = 1 };
        room.Items.Add(potion);
        display.ShowTakeMenuResult = potion; // user highlights and confirms this item

        var loop = MakeLoop(display, combat.Object, "take", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        player.Inventory.Should().Contain(potion, "the selected item should move to the player's inventory");
        room.Items.Should().NotContain(potion, "the selected item should be removed from the room");
    }

    // ── Take All ──────────────────────────────────────────────────────────────

    [Fact]
    public void HandleTake_TakeAll_AllItemsTaken()
    {
        // Arrange
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, Weight = 1 };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Weight = 2 };
        room.Items.Add(potion);
        room.Items.Add(sword);
        // Sentinel item signals "take everything"
        display.ShowTakeMenuResult = new Item { Name = "__TAKE_ALL__" };

        var loop = MakeLoop(display, combat.Object, "take", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        player.Inventory.Should().Contain(potion, "Take All should pick up every item in the room");
        player.Inventory.Should().Contain(sword, "Take All should pick up every item in the room");
        room.Items.Should().BeEmpty("all items should be removed from the room after Take All");
    }

    [Fact]
    public void HandleTake_TakeAll_InventoryFull_StopsGracefully()
    {
        // Arrange — fill inventory to the slot limit
        var (player, room, display, combat) = MakeSetup();
        for (int i = 0; i < Player.MaxInventorySize; i++)
            player.Inventory.Add(new Item { Name = $"Filler {i}", Type = ItemType.Consumable, Weight = 0 });

        var leftover = new Item { Name = "Leftover Gem", Type = ItemType.Consumable, Weight = 1 };
        var extra = new Item { Name = "Extra Crystal", Type = ItemType.Consumable, Weight = 1 };
        room.Items.Add(leftover);
        room.Items.Add(extra);
        display.ShowTakeMenuResult = new Item { Name = "__TAKE_ALL__" };

        var loop = MakeLoop(display, combat.Object, "take", "quit");

        // Act — must not throw
        loop.Run(player, room);

        // Assert
        room.Items.Should().NotBeEmpty("items that do not fit must stay in the room");
        display.AllOutput.Should().Contain(
            s => s.Contains("full", StringComparison.OrdinalIgnoreCase),
            "the player should be notified that their inventory is full");
    }

    // ── Typed argument TAKE ───────────────────────────────────────────────────

    [Fact]
    public void HandleTake_WithArgument_ExactMatch_ItemTaken()
    {
        // Arrange
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, Weight = 1 };
        room.Items.Add(potion);

        var loop = MakeLoop(display, combat.Object, "take potion", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        player.Inventory.Should().Contain(potion, "exact-match take should move the item to inventory");
        room.Items.Should().NotContain(potion, "the taken item should leave the room");
    }

    [Fact]
    public void HandleTake_WithArgument_FuzzyMatch_ItemTaken()
    {
        // Arrange — item in room is named "Potion"; player types "potoin" (one transposition)
        // Levenshtein("potoin", "potion") == 1; tolerance == Max(3, 6/2) == 3 → within range
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Potion", Type = ItemType.Consumable, Weight = 1 };
        room.Items.Add(potion);

        var loop = MakeLoop(display, combat.Object, "take potoin", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        player.Inventory.Should().Contain(potion, "fuzzy match should still pick up the item despite the typo");
        room.Items.Should().NotContain(potion, "the fuzzy-matched item should leave the room");
    }

    [Fact]
    public void HandleTake_WithArgument_NoMatch_ShowsError()
    {
        // Arrange — "axe" is far from "Health Potion"; no fuzzy match possible
        var (player, room, display, combat) = MakeSetup();
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        room.Items.Add(potion);

        var loop = MakeLoop(display, combat.Object, "take axe", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        player.Inventory.Should().NotContain(potion, "no-match take should not pick up anything");
        display.Errors.Should().NotBeEmpty("an error should inform the player nothing matched");
    }

    [Fact]
    public void HandleTake_InventoryFull_ShowsError()
    {
        // Arrange — fill inventory to the slot limit, then try to take one more item
        var (player, room, display, combat) = MakeSetup();
        for (int i = 0; i < Player.MaxInventorySize; i++)
            player.Inventory.Add(new Item { Name = $"Filler {i}", Type = ItemType.Consumable, Weight = 0 });

        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, Weight = 5 };
        room.Items.Add(sword);

        var loop = MakeLoop(display, combat.Object, "take sword", "quit");

        // Act
        loop.Run(player, room);

        // Assert
        player.Inventory.Should().NotContain(sword, "a full inventory must prevent picking up the item");
        room.Items.Should().Contain(sword, "the item must remain in the room when inventory is full");
        display.AllOutput.Should().Contain(
            s => s.Contains("full", StringComparison.OrdinalIgnoreCase),
            "the player should be told their inventory is full");
    }
}
