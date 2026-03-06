using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Engine.Commands;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for <see cref="UseCommandHandler"/>.
/// </summary>
public class UseCommandHandlerTests
{
    /// <summary>
    /// Re-implements IDisplayService so interface dispatch routes ShowUseMenuAndSelect here.
    /// </summary>
    private sealed class UseTestDisplay : TestDisplayService, IDisplayService
    {
        public Item? UseMenuResult { get; set; }
        public bool ShowItemDetailCalled { get; private set; }

        public new Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable)
        {
            AllOutput.Add("use_menu");
            return UseMenuResult;
        }

        public new void ShowItemDetail(Item item)
        {
            ShowItemDetailCalled = true;
            AllOutput.Add($"item_detail:{item.Name}");
        }
    }

    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        UseTestDisplay? display = null)
    {
        var p = player ?? new PlayerBuilder().Build();
        var r = room ?? new Room { Description = "A dusty chamber." };
        var d = display ?? new UseTestDisplay();
        var equipMgr = new EquipmentManager(d);
        var invMgr = new InventoryManager(d);

        return new CommandContext
        {
            Player = p,
            CurrentRoom = r,
            Rng = new Random(42),
            Stats = new RunStats(),
            SessionStats = new SessionStats(),
            RunStart = DateTime.UtcNow,
            Display = d,
            Combat = new Mock<ICombatEngine>().Object,
            Equipment = equipMgr,
            InventoryManager = invMgr,
            Narration = new NarrationService(new Random(42)),
            Achievements = new AchievementSystem(),
            AllItems = new List<Item>(),
            Difficulty = DifficultySettings.For(Difficulty.Normal),
            DifficultyLevel = Difficulty.Normal,
            Logger = new Mock<ILogger>().Object,
            Events = new GameEvents(),
            CurrentFloor = 1,
            FloorHistory = new Dictionary<int, Room>(),
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

    // ── Empty inventory ───────────────────────────────────────────────────────

    [Fact]
    public void Handle_NoArgument_EmptyUsableInventory_ShowsError_TurnNotConsumed()
    {
        var display = new UseTestDisplay();
        var ctx = MakeContext(display: display);
        var handler = new UseCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("no usable items means the turn should not be consumed");
        display.Errors.Should().Contain(e => e.Contains("no usable items"),
            "player must be informed there are no usable items");
    }

    // ── Null selection from menu ──────────────────────────────────────────────

    [Fact]
    public void Handle_NoArgument_NullMenuSelection_CallsShowRoom_TurnNotConsumed()
    {
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        var player = new PlayerBuilder().WithItem(potion).Build();
        var room = new Room { Description = "A quiet room." };
        var display = new UseTestDisplay { UseMenuResult = null };
        var ctx = MakeContext(player: player, room: room, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("cancelling the menu should not consume a turn");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called when the player cancels");
    }

    // ── Use consumable with HealAmount ────────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_HealConsumable_HealsPlayer_RemovesItem_TurnConsumed()
    {
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).WithItem(potion).Build();
        var display = new UseTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("Health Potion", ctx);

        player.HP.Should().BeGreaterThan(50, "the potion should heal the player");
        player.Inventory.Should().NotContain(potion, "the potion should be removed after use");
        ctx.TurnConsumed.Should().BeTrue("using a consumable consumes a turn");
    }

    // ── Use consumable with ManaRestore ───────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_ManaConsumable_RestoresMana_RemovesItem()
    {
        var elixir = new Item { Name = "Mana Elixir", Type = ItemType.Consumable, ManaRestore = 20 };
        var player = new PlayerBuilder().WithMana(5).WithMaxMana(50).WithItem(elixir).Build();
        var display = new UseTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("Mana Elixir", ctx);

        player.Mana.Should().BeGreaterThan(5, "the elixir should restore mana");
        player.Inventory.Should().NotContain(elixir, "the elixir should be consumed after use");
    }

    // ── Item not found ────────────────────────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_ItemNotFound_ShowsError_TurnNotConsumed()
    {
        var display = new UseTestDisplay();
        var ctx = MakeContext(display: display);
        var handler = new UseCommandHandler();

        handler.Handle("xyzzy nonexistent item", ctx);

        ctx.TurnConsumed.Should().BeFalse("missing item should not consume a turn");
        display.Errors.Should().NotBeEmpty("an error should be shown when item is not found");
    }

    // ── Item is a weapon (not usable) ─────────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_WeaponItem_ShowsEquipError_TurnNotConsumed()
    {
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, IsEquippable = true };
        var player = new PlayerBuilder().WithItem(sword).Build();
        var display = new UseTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("Iron Sword", ctx);

        ctx.TurnConsumed.Should().BeFalse("using a weapon should not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("EQUIP"),
            "the error should tell the player to use EQUIP instead");
    }

    // ── Fuzzy match ───────────────────────────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_FuzzyMatch_FindsAndUsesItem()
    {
        // "potoin" is one transposition away from "potion" — within fuzzy tolerance
        var potion = new Item { Name = "Potion", Type = ItemType.Consumable, HealAmount = 20 };
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).WithItem(potion).Build();
        var display = new UseTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("potoin", ctx);

        player.Inventory.Should().NotContain(potion, "fuzzy match should successfully use and remove the item");
        player.HP.Should().BeGreaterThan(50, "the potion should have healed the player via fuzzy match");
    }

    // ── Menu selection uses the selected item ─────────────────────────────────

    [Fact]
    public void Handle_NoArgument_MenuSelectsItem_UsesItem_RemovesFromInventory()
    {
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).WithItem(potion).Build();
        var display = new UseTestDisplay { UseMenuResult = potion };
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("", ctx);

        player.Inventory.Should().NotContain(potion, "menu-selected item should be used and removed from inventory");
    }
}
