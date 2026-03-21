using Dungnz.Display.Avalonia.ViewModels;
using FluentAssertions;

namespace Dungnz.Tests.ViewModels;

public class LogPanelViewModelTests
{
    [Fact]
    public void AppendLog_InfoMessage_AppearsInLogLines()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act
        vm.AppendLog("You entered a dark room.", "info");

        // Assert
        vm.LogLines.Should().ContainSingle();
        vm.LogLines[0].Should().Contain("You entered a dark room.");
        vm.LogLines[0].Should().Contain("ℹ"); // info icon
    }

    [Fact]
    public void AppendLog_ErrorMessage_ShowsErrorIcon()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act
        vm.AppendLog("Invalid command!", "error");

        // Assert
        vm.LogLines[0].Should().Contain("❌");
    }

    [Fact]
    public void AppendLog_LootMessage_ShowsLootIcon()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act
        vm.AppendLog("You found gold!", "loot");

        // Assert
        vm.LogLines[0].Should().Contain("💰");
    }

    [Fact]
    public void AppendLog_CombatWithCritical_ShowsCriticalIcon()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act
        vm.AppendLog("Critical hit for 25 damage!", "combat");

        // Assert
        vm.LogLines[0].Should().Contain("💥");
    }

    [Fact]
    public void AppendLog_CombatWithHealed_ShowsHealIcon()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act
        vm.AppendLog("Healed for 15 HP!", "combat");

        // Assert
        vm.LogLines[0].Should().Contain("💚");
    }

    [Fact]
    public void AppendLog_CombatGeneric_ShowsSwordIcon()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act
        vm.AppendLog("You attack the goblin.", "combat");

        // Assert
        vm.LogLines[0].Should().Contain("⚔");
    }

    [Fact]
    public void AppendLog_TrimsAtDisplayLimit()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act — MaxDisplayedLog is 12
        for (int i = 0; i < 20; i++)
            vm.AppendLog($"Event {i}");

        // Assert
        vm.LogLines.Should().HaveCount(12);
        vm.LogLines[^1].Should().Contain("Event 19");
    }

    [Fact]
    public void AppendLog_TrimsHistoryAtMaxLimit()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act — MaxLogHistory is 50
        for (int i = 0; i < 55; i++)
            vm.AppendLog($"Log {i}");

        // Assert — display shows last 12 of 50 retained
        vm.LogLines.Should().HaveCount(12);
        // Oldest retained history entry should be Log 5 (0-4 trimmed from 55-50=5)
        vm.LogLines[0].Should().Contain("Log 43"); // 50 retained (5..54), last 12 = 43..54
    }

    [Fact]
    public void AppendLog_IncludesTimestamp()
    {
        // Arrange
        var vm = new LogPanelViewModel();

        // Act
        vm.AppendLog("Test event");

        // Assert — timestamp format is HH:mm
        var line = vm.LogLines[0];
        line.Should().MatchRegex(@"\d{2}:\d{2}");
    }
}
