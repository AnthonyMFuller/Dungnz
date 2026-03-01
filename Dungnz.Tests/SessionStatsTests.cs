using Dungnz.Systems;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dungnz.Tests;

public class SessionStatsTests
{
    [Fact]
    public void SessionStats_DefaultValues_AreZero()
    {
        var stats = new SessionStats();
        stats.EnemiesKilled.Should().Be(0);
        stats.GoldEarned.Should().Be(0);
        stats.FloorsCleared.Should().Be(0);
        stats.BossKills.Should().Be(0);
        stats.DamageDealt.Should().Be(0);
    }

    [Fact]
    public void SessionStats_TracksIncrements()
    {
        var stats = new SessionStats();
        stats.EnemiesKilled = 5;
        stats.GoldEarned = 120;
        stats.FloorsCleared = 3;
        stats.BossKills = 1;
        stats.DamageDealt = 350;

        stats.EnemiesKilled.Should().Be(5);
        stats.GoldEarned.Should().Be(120);
        stats.FloorsCleared.Should().Be(3);
        stats.BossKills.Should().Be(1);
        stats.DamageDealt.Should().Be(350);
    }

    [Fact]
    public void LogBalanceSummary_LogsWithoutThrowing()
    {
        var logger = new Mock<ILogger>();
        var stats = new SessionStats
        {
            EnemiesKilled = 3,
            GoldEarned = 50,
            FloorsCleared = 2,
            BossKills = 0,
            DamageDealt = 200
        };

        var act = () => SessionLogger.LogBalanceSummary(logger.Object, stats, "Defeat");
        act.Should().NotThrow();
    }

    [Fact]
    public void LogBalanceSummary_InvokesLogger()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var stats = new SessionStats
        {
            EnemiesKilled = 10,
            GoldEarned = 200,
            FloorsCleared = 5,
            BossKills = 2,
            DamageDealt = 800
        };

        SessionLogger.LogBalanceSummary(logger.Object, stats, "Victory");

        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Victory")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
