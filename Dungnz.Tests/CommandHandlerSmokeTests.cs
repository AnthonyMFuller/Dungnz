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
/// Smoke tests for the most important command handlers (#943):
/// Look, Go, Use, Equip, Inventory, Stats, Help, Examine.
/// </summary>
public class CommandHandlerSmokeTests
{
    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        TestDisplayService? display = null,
        ICombatEngine? combat = null)
    {
        var p = player ?? new PlayerBuilder().Build();
        var r = room ?? new Room { Description = "A dusty room." };
        var d = display ?? new TestDisplayService();
        var c = combat ?? new Mock<ICombatEngine>().Object;
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
            Combat = c,
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

    // ── LookCommandHandler ────────────────────────────────────────────────────

    [Fact]
    public void LookHandler_ShowsRoom()
    {
        var display = new TestDisplayService();
        var ctx = MakeContext(display: display, room: new Room { Description = "Cobblestone hall." });
        var handler = new LookCommandHandler();

        handler.Handle("", ctx);

        display.AllOutput.Should().Contain(o => o.Contains("Cobblestone hall"));
    }

    // ── GoCommandHandler ──────────────────────────────────────────────────────

    [Fact]
    public void GoHandler_NoDirection_ShowsError()
    {
        var display = new TestDisplayService();
        var ctx = MakeContext(display: display);
        var handler = new GoCommandHandler();

        handler.Handle("", ctx);

        display.Errors.Should().Contain(e => e.Contains("Go where"));
        ctx.TurnConsumed.Should().BeFalse();
    }

    [Fact]
    public void GoHandler_InvalidDirection_ShowsError()
    {
        var display = new TestDisplayService();
        var ctx = MakeContext(display: display);
        var handler = new GoCommandHandler();

        handler.Handle("up", ctx);

        display.Errors.Should().Contain(e => e.Contains("Invalid direction"));
        ctx.TurnConsumed.Should().BeFalse();
    }

    [Fact]
    public void GoHandler_NoExit_ShowsError()
    {
        var display = new TestDisplayService();
        var ctx = MakeContext(display: display);
        var handler = new GoCommandHandler();

        handler.Handle("north", ctx);

        display.Errors.Should().Contain(e => e.Contains("can't go"));
        ctx.TurnConsumed.Should().BeFalse();
    }

    [Fact]
    public void GoHandler_ValidExit_MovesToRoom()
    {
        var display = new TestDisplayService();
        var start = new Room { Description = "Start" };
        var next = new Room { Description = "Next room" };
        start.Exits[Direction.North] = next;
        var ctx = MakeContext(display: display, room: start);
        var handler = new GoCommandHandler();

        handler.Handle("north", ctx);

        ctx.CurrentRoom.Should().BeSameAs(next);
        display.AllOutput.Should().Contain(o => o.Contains("Next room"));
    }

    // ── HelpCommandHandler ────────────────────────────────────────────────────

    [Fact]
    public void HelpHandler_CallsShowHelp()
    {
        var display = new TestDisplayService();
        var ctx = MakeContext(display: display);
        var handler = new HelpCommandHandler();

        handler.Handle("", ctx);

        display.AllOutput.Should().Contain("help");
    }

    // ── StatsCommandHandler ───────────────────────────────────────────────────

    [Fact]
    public void StatsHandler_ShowsPlayerStats()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().Named("TestHero").Build();
        var ctx = MakeContext(player: player, display: display);
        var handler = new StatsCommandHandler();

        handler.Handle("", ctx);

        display.AllOutput.Should().Contain(o => o.Contains("TestHero"));
        display.Messages.Should().Contain(m => m.Contains("Floor"));
    }

    // ── InventoryCommandHandler ───────────────────────────────────────────────

    [Fact]
    public void InventoryHandler_ShowsInventory_NoTurnConsumed()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().Build();
        var ctx = MakeContext(player: player, display: display);
        var handler = new InventoryCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse();
    }

    // ── EquipCommandHandler ───────────────────────────────────────────────────

    [Fact]
    public void EquipHandler_EquipsItem()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().WithAttack(10).Build();
        var sword = new ItemBuilder().Named("Iron Sword").WithDamage(5).Build();
        player.Inventory.Add(sword);
        var ctx = MakeContext(player: player, display: display);
        var handler = new EquipCommandHandler();

        handler.Handle("Iron Sword", ctx);

        player.EquippedWeapon.Should().BeSameAs(sword);
        player.Attack.Should().Be(15);
    }

    [Fact]
    public void EquipHandler_ItemNotInInventory_ShowsError()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().Build();
        var ctx = MakeContext(player: player, display: display);
        var handler = new EquipCommandHandler();

        handler.Handle("Phantom Blade", ctx);

        display.Errors.Should().NotBeEmpty();
    }

    // ── UseCommandHandler ─────────────────────────────────────────────────────

    [Fact]
    public void UseHandler_NoArgument_NoConsumables_ShowsError()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().Build();
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("", ctx);

        display.Errors.Should().Contain(e => e.Contains("no usable items"));
    }

    [Fact]
    public void UseHandler_HealingPotion_RestoresHP()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).Build();
        var potion = new ItemBuilder().Named("Health Potion").WithHeal(30).Build();
        player.Inventory.Add(potion);
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("Health Potion", ctx);

        player.HP.Should().Be(80);
        player.Inventory.Should().NotContain(potion);
    }

    [Fact]
    public void UseHandler_NonExistentItem_ShowsError()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().Build();
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("Nonexistent", ctx);

        display.Errors.Should().NotBeEmpty();
        ctx.TurnConsumed.Should().BeFalse();
    }

    [Fact]
    public void UseHandler_WeaponItem_SuggestsEquip()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().Build();
        var sword = new ItemBuilder().Named("Sword").WithDamage(5).Build();
        player.Inventory.Add(sword);
        var ctx = MakeContext(player: player, display: display);
        var handler = new UseCommandHandler();

        handler.Handle("Sword", ctx);

        display.Errors.Should().Contain(e => e.Contains("EQUIP"));
        ctx.TurnConsumed.Should().BeFalse();
    }

    // ── ExamineCommandHandler ─────────────────────────────────────────────────

    [Fact]
    public void ExamineHandler_NoArgument_ShowsError()
    {
        var display = new TestDisplayService();
        var ctx = MakeContext(display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("", ctx);

        display.Errors.Should().Contain(e => e.Contains("Examine what"));
        ctx.TurnConsumed.Should().BeFalse();
    }

    [Fact]
    public void ExamineHandler_EnemyInRoom_ShowsEnemyInfo()
    {
        var display = new TestDisplayService();
        var room = new Room { Description = "Dark room", Enemy = new Enemy_Stub(30, 8, 3, 10) };
        var ctx = MakeContext(display: display, room: room);
        var handler = new ExamineCommandHandler();

        handler.Handle("TestEnemy", ctx);

        display.Messages.Should().Contain(m => m.Contains("TestEnemy") && m.Contains("30"));
    }

    [Fact]
    public void ExamineHandler_ItemInRoom_DoesNotError()
    {
        var display = new TestDisplayService();
        var potion = new ItemBuilder().Named("Magic Potion").WithHeal(10).WithDescription("Sparkles blue.").Build();
        var room = new Room { Description = "Alchemy room", Items = new List<Item> { potion } };
        var ctx = MakeContext(display: display, room: room);
        var handler = new ExamineCommandHandler();

        handler.Handle("Magic Potion", ctx);

        display.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ExamineHandler_ItemInInventory_DoesNotError()
    {
        var display = new TestDisplayService();
        var player = new PlayerBuilder().Build();
        var ring = new ItemBuilder().Named("Gold Ring").WithId("ring").OfType(ItemType.Accessory).AsEquippable().WithDescription("Shiny.").Build();
        player.Inventory.Add(ring);
        var ctx = MakeContext(player: player, display: display);
        ctx.GetCurrentlyEquippedForItem = (_, _) => null;
        var handler = new ExamineCommandHandler();

        handler.Handle("Gold Ring", ctx);

        display.Errors.Should().BeEmpty();
        display.AllOutput.Should().Contain(o => o.Contains("equipment_compare"));
    }

    [Fact]
    public void ExamineHandler_UnknownTarget_ShowsError()
    {
        var display = new TestDisplayService();
        var ctx = MakeContext(display: display);
        var handler = new ExamineCommandHandler();

        handler.Handle("invisible thing", ctx);

        display.Errors.Should().Contain(e => e.Contains("don't see"));
        ctx.TurnConsumed.Should().BeFalse();
    }
}
