using Dungnz.Engine;
using Dungnz.Engine.Commands;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dungnz.Tests;

/// <summary>
/// #1372 — Coverage suite for <see cref="PrestigeCommandHandler"/>.
/// Informational command — never mutates player state.
/// </summary>
[Collection("PrestigeTests")]
public class PrestigeCommandHandlerTests : IDisposable
{
    private readonly string _tempDir;

    public PrestigeCommandHandlerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PrestigeCmd_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        PrestigeSystem.SetSavePathForTesting(Path.Combine(_tempDir, "prestige.json"));
    }

    public void Dispose()
    {
        PrestigeSystem.SetSavePathForTesting(null);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static CommandContext MakeContext(FakeDisplayService display)
    {
        return new CommandContext
        {
            Player           = new PlayerBuilder().Build(),
            CurrentRoom      = new Room { Description = "Hall" },
            Rng              = new Random(1),
            Stats            = new RunStats(),
            SessionStats     = new SessionStats(),
            RunStart         = DateTime.UtcNow,
            Display          = display,
            Combat           = new Mock<ICombatEngine>().Object,
            Equipment        = new EquipmentManager(display),
            InventoryManager = new InventoryManager(display),
            Narration        = new NarrationService(new Random(1)),
            Achievements     = new AchievementSystem(),
            AllItems         = new List<Item>(),
            Difficulty       = DifficultySettings.For(Difficulty.Normal),
            DifficultyLevel  = Difficulty.Normal,
            Logger           = new Mock<ILogger>().Object,
            TurnConsumed     = true,
            GameOver         = false,
            ExitRun          = _ => { },
            RecordRunEnd     = (_, _) => { },
            GetCurrentlyEquippedForItem = (_, _) => null,
            GetDifficultyName = () => "Normal",
            HandleShrine           = () => { },
            HandleContestedArmory  = () => { },
            HandlePetrifiedLibrary = () => { },
            HandleTrapRoom         = () => { },
        };
    }

    [Fact]
    public void Handle_PrestigeLevel0_ShowsEarnHintMessage()
    {
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        new PrestigeCommandHandler().Handle("", ctx);
        display.Messages.Should().Contain(m => m.Contains("Win 3 runs"));
    }

    [Fact]
    public void Handle_PrestigeLevel0_ShowsLevelAndRunCounts()
    {
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        new PrestigeCommandHandler().Handle("", ctx);
        display.Messages.Should().Contain(m => m.Contains("PRESTIGE STATUS"));
        display.Messages.Should().Contain(m => m.Contains("Prestige Level: 0"));
        display.Messages.Should().Contain(m => m.Contains("Total Wins: 0"));
    }

    [Fact]
    public void Handle_PrestigeLevel1_ShowsBonusStats()
    {
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        new PrestigeCommandHandler().Handle("", ctx);
        display.Messages.Should().Contain(m => m.Contains("Bonus") && m.Contains("Attack"));
        display.Messages.Should().Contain(m => m.Contains("Defense"));
        display.Messages.Should().Contain(m => m.Contains("Max HP"));
    }

    [Fact]
    public void Handle_PrestigeLevel1_DoesNotShowEarnHint()
    {
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        new PrestigeCommandHandler().Handle("", ctx);
        display.Messages.Should().NotContain(m => m.Contains("Win 3 runs"));
    }

    [Fact]
    public void Handle_AnyPrestigeLevel_PlayerStateUnchanged()
    {
        var display  = new FakeDisplayService();
        var ctx      = MakeContext(display);
        var player   = ctx.Player;
        var hpBefore = player.HP;
        var gpBefore = player.Gold;
        var xpBefore = player.XP;
        new PrestigeCommandHandler().Handle("", ctx);
        player.HP.Should().Be(hpBefore);
        player.Gold.Should().Be(gpBefore);
        player.XP.Should().Be(xpBefore);
    }

    [Fact]
    public void Handle_Always_TurnConsumedRemainsTrue()
    {
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        ctx.TurnConsumed = true;
        new PrestigeCommandHandler().Handle("", ctx);
        ctx.TurnConsumed.Should().BeTrue();
    }

    [Fact]
    public void Handle_WithIgnoredArgument_StillShowsPrestigeStatus()
    {
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        new PrestigeCommandHandler().Handle("some_ignored_arg", ctx);
        display.Messages.Should().Contain(m => m.Contains("PRESTIGE STATUS"));
    }

    [Fact]
    public void Handle_HighPrestige_ShowsAccumulatedWinsAndRuns()
    {
        for (int i = 0; i < 9; i++) PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: false);
        var display = new FakeDisplayService();
        var ctx     = MakeContext(display);
        new PrestigeCommandHandler().Handle("", ctx);
        display.Messages.Should().Contain(m => m.Contains("Total Wins: 9"));
        display.Messages.Should().Contain(m => m.Contains("Total Runs: 10"));
    }
}
