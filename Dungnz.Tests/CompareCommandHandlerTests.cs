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
/// Tests for <see cref="CompareCommandHandler"/>.
/// </summary>
public class CompareCommandHandlerTests
{
    /// <summary>
    /// Re-implements IDisplayService so interface dispatch routes ShowEquipMenuAndSelect here.
    /// </summary>
    private sealed class CompareTestDisplay : TestDisplayService, IDisplayService
    {
        public Item? EquipMenuResult { get; set; }

        public new Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
        {
            AllOutput.Add("equip_menu");
            return EquipMenuResult;
        }
    }

    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        CompareTestDisplay? display = null,
        Func<Player, Item?, Item?>? getEquipped = null)
    {
        var p = player ?? new PlayerBuilder().Build();
        var r = room ?? new Room { Description = "A stone vault." };
        var d = display ?? new CompareTestDisplay();
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
            GetCurrentlyEquippedForItem = getEquipped ?? ((_, _) => null),
            GetDifficultyName = () => "Normal",
            HandleShrine = () => { },
            HandleContestedArmory = () => { },
            HandlePetrifiedLibrary = () => { },
            HandleTrapRoom = () => { },
        };
    }

    // ── No equippable items ───────────────────────────────────────────────────

    [Fact]
    public void Handle_NoArgument_NoEquippableItems_ShowsError_TurnNotConsumed()
    {
        var consumable = new Item { Name = "Potion", Type = ItemType.Consumable, IsEquippable = false };
        var player = new PlayerBuilder().WithItem(consumable).Build();
        var display = new CompareTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new CompareCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("no equippable items means the turn should not be consumed");
        display.Errors.Should().Contain(e => e.Contains("no equippable items"),
            "player must be told there is nothing to compare");
    }

    // ── Null selection from menu ──────────────────────────────────────────────

    [Fact]
    public void Handle_NoArgument_NullMenuSelection_CallsShowRoom_TurnNotConsumed()
    {
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, IsEquippable = true };
        var player = new PlayerBuilder().WithItem(sword).Build();
        var room = new Room { Description = "An armoury." };
        var display = new CompareTestDisplay { EquipMenuResult = null };
        var ctx = MakeContext(player: player, room: room, display: display);
        var handler = new CompareCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("cancelling the menu should not consume a turn");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called when the player cancels");
    }

    // ── Item not found by name ────────────────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_ItemNotFound_ShowsError_TurnNotConsumed()
    {
        var player = new PlayerBuilder().Build();
        var display = new CompareTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new CompareCommandHandler();

        handler.Handle("legendary axe of doom", ctx);

        ctx.TurnConsumed.Should().BeFalse("item not found should not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("legendary axe of doom"),
            "the error should mention the item name the player provided");
    }

    // ── Item not equippable ───────────────────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_ItemNotEquippable_ShowsError_TurnNotConsumed()
    {
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, IsEquippable = false };
        var player = new PlayerBuilder().WithItem(potion).Build();
        var display = new CompareTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new CompareCommandHandler();

        handler.Handle("Health Potion", ctx);

        ctx.TurnConsumed.Should().BeFalse("non-equippable item should not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("cannot be equipped"),
            "player must be told the item cannot be equipped");
    }

    // ── Successful comparison ─────────────────────────────────────────────────

    [Fact]
    public void Handle_WithArgument_EquippableItem_ShowsComparisonAndRoom()
    {
        var sword = new Item { Name = "Steel Sword", Type = ItemType.Weapon, IsEquippable = true };
        var player = new PlayerBuilder().WithItem(sword).Build();
        var room = new Room { Description = "The forge room." };
        var display = new CompareTestDisplay();
        var ctx = MakeContext(player: player, room: room, display: display);
        var handler = new CompareCommandHandler();

        handler.Handle("Steel Sword", ctx);

        display.AllOutput.Should().Contain(s => s.StartsWith("equipment_compare:"),
            "ShowEquipmentComparison should be called for a valid equippable item");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called after the comparison");
    }
}
