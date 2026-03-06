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
/// #1153 — Core ascension tests for <see cref="AscendCommandHandler"/>.
/// </summary>
public class AscendCommandHandlerTests
{
    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        int floor = 2,
        Dictionary<int, Room>? floorHistory = null,
        TestDisplayService? display = null)
    {
        var p = player ?? new PlayerBuilder().Build();
        var r = room ?? new Room { Description = "Floor 2 entrance.", IsEntrance = true };
        var d = display ?? new TestDisplayService();
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
            CurrentFloor = floor,
            FloorHistory = floorHistory ?? new Dictionary<int, Room>(),
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

    // ── Test 1: Cannot ascend from non-entrance room ─────────────────────────

    [Fact]
    public void Handle_NonEntranceRoom_ShowsError_TurnNotConsumed()
    {
        var room = new Room { Description = "A random corridor.", IsEntrance = false };
        var display = new TestDisplayService();
        var ctx = MakeContext(room: room, display: display);
        var handler = new AscendCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("ascend on a non-entrance room must not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("entrance"),
            "player must be told they need to be at the entrance");
    }

    // ── Test 2: Cannot ascend on floor 1 ────────────────────────────────────

    [Fact]
    public void Handle_Floor1EntranceRoom_ShowsError_TurnNotConsumed()
    {
        var room = new Room { Description = "Floor 1 entrance.", IsEntrance = true };
        var display = new TestDisplayService();
        var ctx = MakeContext(room: room, floor: 1, display: display);
        var handler = new AscendCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("ascend from floor 1 must not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("first floor"),
            "player must be told they are already on floor 1");
    }

    // ── Test 3: Cannot ascend with empty floor history ───────────────────────

    [Fact]
    public void Handle_EmptyFloorHistory_ShowsError_TurnNotConsumed()
    {
        var room = new Room { Description = "Floor 2 entrance.", IsEntrance = true };
        var display = new TestDisplayService();
        var ctx = MakeContext(room: room, floor: 2, floorHistory: new Dictionary<int, Room>(), display: display);
        var handler = new AscendCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("ascend with no floor history must not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("ascend"),
            "player must be told they cannot find a way to ascend");
    }

    // ── Test 4: Successful ascent decrements floor and sets current room ─────

    [Fact]
    public void Handle_ValidAscent_DecrementsFloor_AndSetsCurrentRoom()
    {
        var floor1Entrance = new Room { Description = "Floor 1 entrance.", IsEntrance = true };
        var floor2Entrance = new Room { Description = "Floor 2 entrance.", IsEntrance = true };
        var history = new Dictionary<int, Room> { [1] = floor1Entrance };
        var ctx = MakeContext(room: floor2Entrance, floor: 2, floorHistory: history);
        var handler = new AscendCommandHandler();

        handler.Handle("", ctx);

        ctx.CurrentFloor.Should().Be(1, "floor number must decrement on successful ascent");
        ctx.CurrentRoom.Should().BeSameAs(floor1Entrance,
            "current room must be restored to the floor 1 entrance");
    }

    // ── Test 5: Temp buffs cleared on successful ascent ──────────────────────

    [Fact]
    public void Handle_ValidAscent_ClearsTempAttackDefenseBuffsAndWardingVeil()
    {
        var player = new PlayerBuilder().WithAttack(10).WithDefense(5).Build();
        player.TempAttackBonus = 4;
        player.ModifyAttack(4);
        player.TempDefenseBonus = 3;
        player.ModifyDefense(3);
        player.WardingVeilActive = true;

        var floor1Entrance = new Room { Description = "Floor 1 entrance.", IsEntrance = true };
        var floor2Entrance = new Room { Description = "Floor 2 entrance.", IsEntrance = true };
        var history = new Dictionary<int, Room> { [1] = floor1Entrance };
        var ctx = MakeContext(player: player, room: floor2Entrance, floor: 2, floorHistory: history);
        var handler = new AscendCommandHandler();

        handler.Handle("", ctx);

        player.TempAttackBonus.Should().Be(0, "temp attack bonus must be zeroed on ascent");
        player.TempDefenseBonus.Should().Be(0, "temp defense bonus must be zeroed on ascent");
        player.WardingVeilActive.Should().BeFalse("warding veil must be deactivated on ascent");
    }

    // ── Test 6: TurnConsumed is true on successful ascent ────────────────────

    [Fact]
    public void Handle_ValidAscent_TurnConsumedIsTrue()
    {
        var floor1Entrance = new Room { Description = "Floor 1 entrance.", IsEntrance = true };
        var floor2Entrance = new Room { Description = "Floor 2 entrance.", IsEntrance = true };
        var history = new Dictionary<int, Room> { [1] = floor1Entrance };
        var ctx = MakeContext(room: floor2Entrance, floor: 2, floorHistory: history);
        var handler = new AscendCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeTrue("a successful ascent must consume a turn");
    }
}

/// <summary>
/// #1153 — Verifies that <see cref="DescendCommandHandler"/> populates
/// <see cref="CommandContext.FloorHistory"/> so ascension can later restore it.
/// </summary>
[Collection("EnemyFactory")]
public class DescendFloorHistoryTests
{
    private static CommandContext MakeDescendContext(Room entrance, Room exit, TestDisplayService? display = null)
    {
        var d = display ?? new TestDisplayService();
        return new CommandContext
        {
            Player = new PlayerBuilder().Build(),
            CurrentRoom = exit,
            FloorEntranceRoom = entrance,
            Rng = new Random(42),
            Stats = new RunStats(),
            SessionStats = new SessionStats(),
            RunStart = DateTime.UtcNow,
            Display = d,
            Combat = new Mock<ICombatEngine>().Object,
            Equipment = new EquipmentManager(d),
            InventoryManager = new InventoryManager(d),
            Narration = new NarrationService(new Random(42)),
            Achievements = new AchievementSystem(),
            AllItems = new List<Item>(),
            Difficulty = DifficultySettings.For(Difficulty.Normal),
            DifficultyLevel = Difficulty.Normal,
            Logger = new Mock<ILogger>().Object,
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

    // ── Test 9: FloorHistory populated after descent ─────────────────────────

    [Fact]
    public void Descend_StoresFloorEntranceRoom_InFloorHistory()
    {
        var floor1Entrance = new Room { Description = "Floor 1 entrance.", IsEntrance = true };
        var floor1Exit = new Room { Description = "Floor 1 exit.", IsExit = true }; // no enemy
        var ctx = MakeDescendContext(entrance: floor1Entrance, exit: floor1Exit);
        var handler = new DescendCommandHandler();

        handler.Handle("", ctx);

        ctx.FloorHistory.Should().ContainKey(1,
            "DescendCommandHandler must record floor 1 entrance in FloorHistory");
        ctx.FloorHistory[1].Should().BeSameAs(floor1Entrance,
            "the recorded entrance must be the floor 1 entrance room");
    }
}
