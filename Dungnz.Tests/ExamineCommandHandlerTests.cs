using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Engine.Commands;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for <see cref="ExamineCommandHandler"/>.
/// </summary>
public class ExamineCommandHandlerTests
{
    /// <summary>
    /// Re-implements IDisplayService to track ShowItemDetail calls.
    /// </summary>
    private sealed class ExamineTestDisplay : TestDisplayService, IDisplayService
    {
        public bool ShowItemDetailCalled { get; private set; }
        public Item? LastItemDetail { get; private set; }

        public new void ShowItemDetail(Item item)
        {
            ShowItemDetailCalled = true;
            LastItemDetail = item;
            AllOutput.Add($"item_detail:{item.Name}");
        }
    }

    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        ExamineTestDisplay? display = null,
        Func<Player, Item?, Item?>? getEquipped = null)
    {
        var p = player ?? new PlayerBuilder().Build();
        var r = room ?? new Room { Description = "A shadowy hall." };
        var d = display ?? new ExamineTestDisplay();
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

    // ── No argument ───────────────────────────────────────────────────────────

    [Fact]
    public void Handle_NoArgument_ShowsExamineWhatError_TurnNotConsumed()
    {
        var display = new ExamineTestDisplay();
        var ctx = MakeContext(display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("   ", ctx);

        ctx.TurnConsumed.Should().BeFalse("examine with no target should not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("Examine what"),
            "player must be prompted to specify a target");
    }

    // ── Enemy in room ─────────────────────────────────────────────────────────

    [Fact]
    public void Handle_EnemyInRoom_ShowsEnemyStats()
    {
        var enemy = new GenericEnemy(new EnemyStats { Name = "Goblin", MaxHP = 30, Attack = 5, Defense = 2 });
        var room = new Room { Description = "A mossy cave.", Enemy = enemy };
        var display = new ExamineTestDisplay();
        var ctx = MakeContext(room: room, display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("Goblin", ctx);

        display.Messages.Should().Contain(m => m.Contains("Goblin") && m.Contains("HP"),
            "enemy stats message should include the enemy name and HP");
    }

    // ── Item in room ──────────────────────────────────────────────────────────

    [Fact]
    public void Handle_ItemInRoom_CallsShowItemDetailAndShowRoom()
    {
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable };
        var room = new Room { Description = "A loot chamber." };
        room.AddItem(potion);
        var display = new ExamineTestDisplay();
        var ctx = MakeContext(room: room, display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("Health Potion", ctx);

        display.ShowItemDetailCalled.Should().BeTrue("ShowItemDetail must be called when examining a room item");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called after examining a room item");
    }

    // ── Equippable item in inventory ──────────────────────────────────────────

    [Fact]
    public void Handle_EquippableInventoryItem_CallsShowItemDetail_ShowEquipmentComparison_ShowRoom()
    {
        var sword = new Item { Name = "Steel Sword", Type = ItemType.Weapon, IsEquippable = true };
        var player = new PlayerBuilder().WithItem(sword).Build();
        var room = new Room { Description = "An armoury." };
        var display = new ExamineTestDisplay();
        var ctx = MakeContext(player: player, room: room, display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("Steel Sword", ctx);

        display.ShowItemDetailCalled.Should().BeTrue("ShowItemDetail must be called for an inventory item");
        display.AllOutput.Should().Contain(s => s.StartsWith("equipment_compare:"),
            "ShowEquipmentComparison must be called for an equippable inventory item");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called after examining an equippable inventory item");
    }

    // ── Non-equippable item in inventory ──────────────────────────────────────

    [Fact]
    public void Handle_NonEquippableInventoryItem_NoEquipmentComparison_CallsShowRoom()
    {
        var potion = new Item { Name = "Healing Draft", Type = ItemType.Consumable, IsEquippable = false };
        var player = new PlayerBuilder().WithItem(potion).Build();
        var room = new Room { Description = "A still corridor." };
        var display = new ExamineTestDisplay();
        var ctx = MakeContext(player: player, room: room, display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("Healing Draft", ctx);

        display.ShowItemDetailCalled.Should().BeTrue("ShowItemDetail must be called for a consumable inventory item");
        display.AllOutput.Should().NotContain(s => s.StartsWith("equipment_compare:"),
            "ShowEquipmentComparison must NOT be called for a non-equippable item");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called after examining a consumable inventory item");
    }

    // ── Unknown target ────────────────────────────────────────────────────────

    [Fact]
    public void Handle_UnknownTarget_ShowsError_TurnNotConsumed()
    {
        var display = new ExamineTestDisplay();
        var room = new Room { Description = "An empty room." };
        var ctx = MakeContext(room: room, display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("invisible dragon", ctx);

        ctx.TurnConsumed.Should().BeFalse("unknown target should not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("invisible dragon"),
            "the error should include the target name the player tried to examine");
    }
}
