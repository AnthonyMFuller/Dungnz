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
/// Tests for <see cref="HistoryCommandHandler"/>.
/// Verifies that the HISTORY command delegates to <see cref="IDisplayService.ShowCombatHistory"/>
/// regardless of whether combat messages have been logged.
/// </summary>
public class HistoryCommandHandlerTests
{
    private static CommandContext MakeContext(TestDisplayService display)
    {
        var player = new PlayerBuilder().Build();
        var room   = new Room { Description = "A dimly lit corridor." };
        var equipMgr = new EquipmentManager(display);
        var invMgr   = new InventoryManager(display);

        return new CommandContext
        {
            Player           = player,
            CurrentRoom      = room,
            Rng              = new Random(1),
            Stats            = new RunStats(),
            SessionStats     = new SessionStats(),
            RunStart         = DateTime.UtcNow,
            Display          = display,
            Combat           = new Mock<ICombatEngine>().Object,
            Equipment        = equipMgr,
            InventoryManager = invMgr,
            Narration        = new NarrationService(new Random(1)),
            Achievements     = new AchievementSystem(),
            AllItems         = new List<Item>(),
            Difficulty       = DifficultySettings.For(Difficulty.Normal),
            DifficultyLevel  = Difficulty.Normal,
            Logger           = new Mock<ILogger>().Object,
            Events           = new GameEvents(),
            CurrentFloor     = 1,
            FloorHistory     = new Dictionary<int, Room>(),
            TurnConsumed     = true,
            GameOver         = false,
            ExitRun          = _ => { },
            RecordRunEnd     = (_, _) => { },
            GetCurrentlyEquippedForItem = (_, _) => null,
            GetDifficultyName           = () => "Normal",
            HandleShrine                = () => { },
            HandleContestedArmory       = () => { },
            HandlePetrifiedLibrary      = () => { },
            HandleTrapRoom              = () => { },
        };
    }

    /// <summary>
    /// HISTORY with no prior combat output should still invoke ShowCombatHistory
    /// (the display layer owns the "nothing to show" rendering).
    /// </summary>
    [Fact]
    public void HistoryCommand_NoLog_ShowsEmpty()
    {
        var display = new TestDisplayService();
        var ctx     = MakeContext(display);
        var handler = new HistoryCommandHandler();

        handler.Handle("", ctx);

        display.AllOutput.Should().Contain("combat_history",
            "ShowCombatHistory must be called even when no combat messages have been logged");
    }

    /// <summary>
    /// HISTORY after combat messages have been logged should call ShowCombatHistory
    /// so the player can review the full scrollback.
    /// </summary>
    [Fact]
    public void HistoryCommand_WithLog_ShowsEntries()
    {
        var display = new TestDisplayService();
        var ctx     = MakeContext(display);

        // Simulate combat messages logged during an earlier fight.
        display.ShowCombat("You strike the Goblin for 12 damage.");
        display.ShowCombat("The Goblin retaliates for 5 damage.");

        var handler = new HistoryCommandHandler();
        handler.Handle("", ctx);

        display.AllOutput.Should().Contain("combat_history",
            "ShowCombatHistory must be called to render the logged combat turns");
    }
}
