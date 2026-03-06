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
/// Tests for <see cref="SkillsCommandHandler"/> and <see cref="LearnCommandHandler"/>.
/// </summary>
public class SkillsCommandHandlerTests
{
    /// <summary>
    /// Re-implements IDisplayService so interface dispatch routes ShowSkillTreeMenu here.
    /// </summary>
    private sealed class SkillsTestDisplay : TestDisplayService, IDisplayService
    {
        public Skill? SkillMenuResult { get; set; }

        // ShowSkillTreeMenu is virtual in TestDisplayService, so override is valid here.
        public override Skill? ShowSkillTreeMenu(Player player)
        {
            AllOutput.Add("skill_menu");
            return SkillMenuResult;
        }
    }

    private static CommandContext MakeContext(
        Player? player = null,
        Room? room = null,
        SkillsTestDisplay? display = null)
    {
        var p = player ?? new PlayerBuilder().Build();
        var r = room ?? new Room { Description = "A skill shrine." };
        var d = display ?? new SkillsTestDisplay();
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

    // ── SkillsCommandHandler: null return from menu ───────────────────────────

    [Fact]
    public void SkillsHandler_NullMenuResult_CallsShowRoom_TurnNotConsumed()
    {
        var display = new SkillsTestDisplay { SkillMenuResult = null };
        var room = new Room { Description = "A dimly lit shrine." };
        var ctx = MakeContext(room: room, display: display);
        var handler = new SkillsCommandHandler();

        handler.Handle("", ctx);

        ctx.TurnConsumed.Should().BeFalse("cancelling the skill menu should not consume a turn");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called when the player cancels the skill menu");
    }

    // ── SkillsCommandHandler: TryUnlock succeeds ──────────────────────────────

    [Fact]
    public void SkillsHandler_SkillSelected_TryUnlockSucceeds_ShowsLearnedMessage()
    {
        // PowerStrike requires level 3, no class restriction
        var player = new PlayerBuilder().WithLevel(5).Build();
        var display = new SkillsTestDisplay { SkillMenuResult = Skill.PowerStrike };
        var ctx = MakeContext(player: player, display: display);
        var handler = new SkillsCommandHandler();

        handler.Handle("", ctx);

        display.Messages.Should().Contain(m => m.Contains("PowerStrike"),
            "a success message should mention the skill that was learned");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called after learning a skill");
        ctx.TurnConsumed.Should().BeFalse("the skills command never consumes a turn");
    }

    // ── SkillsCommandHandler: TryUnlock fails ────────────────────────────────

    [Fact]
    public void SkillsHandler_SkillSelected_TryUnlockFails_ShowsCannotLearnMessage()
    {
        // Level 1 player cannot learn PowerStrike (requires level 3)
        var player = new PlayerBuilder().WithLevel(1).Build();
        var display = new SkillsTestDisplay { SkillMenuResult = Skill.PowerStrike };
        var ctx = MakeContext(player: player, display: display);
        var handler = new SkillsCommandHandler();

        handler.Handle("", ctx);

        display.Messages.Should().Contain(m => m.Contains("Cannot learn"),
            "a failure message should indicate the skill cannot be learned right now");
        ctx.TurnConsumed.Should().BeFalse("a failed skill unlock should not consume a turn");
    }

    // ── LearnCommandHandler: invalid skill name ───────────────────────────────

    [Fact]
    public void LearnHandler_InvalidSkillName_ShowsError_TurnNotConsumed()
    {
        var display = new SkillsTestDisplay();
        var ctx = MakeContext(display: display);
        var handler = new LearnCommandHandler();

        handler.Handle("NotARealSkill", ctx);

        ctx.TurnConsumed.Should().BeFalse("unknown skill should not consume a turn");
        display.Errors.Should().Contain(e => e.Contains("NotARealSkill"),
            "the error should mention the unrecognised skill name");
    }

    // ── LearnCommandHandler: valid skill name → delegates to HandleLearnSpecificSkill

    [Fact]
    public void LearnHandler_ValidSkillName_CallsHandleLearnSpecificSkill()
    {
        // PowerStrike requires level 3; player at level 5 should successfully unlock it
        var player = new PlayerBuilder().WithLevel(5).Build();
        var display = new SkillsTestDisplay();
        var ctx = MakeContext(player: player, display: display);
        var handler = new LearnCommandHandler();

        handler.Handle("PowerStrike", ctx);

        display.Messages.Should().Contain(m => m.Contains("PowerStrike"),
            "a message about PowerStrike should be shown (either learned or cannot learn)");
        display.AllOutput.Should().Contain(s => s.StartsWith("room:"),
            "ShowRoom should be called after the learn attempt");
    }
}
