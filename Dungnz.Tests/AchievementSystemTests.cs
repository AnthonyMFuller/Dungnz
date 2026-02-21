using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class AchievementSystemTests : IDisposable
{
    private readonly string _tempPath;
    private readonly AchievementSystem _system;

    public AchievementSystemTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"dungnz_ach_test_{Guid.NewGuid()}.json");
        AchievementSystem.OverrideHistoryPath(_tempPath);
        _system = new AchievementSystem();
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath))
            File.Delete(_tempPath);
    }

    private static Player PlayerWithHP(int hp)
    {
        var p = new Player { Name = "Tester" };
        // Set HP via TakeDamage from default 100
        if (hp < 100) p.TakeDamage(100 - hp);
        return p;
    }

    [Fact]
    public void GlassCannon_Unlocks_WhenHP_Below10_AndWon()
    {
        var player = PlayerWithHP(5);
        var stats = new RunStats();

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().Contain(a => a.Name == "Glass Cannon");
    }

    [Fact]
    public void GlassCannon_DoesNotUnlock_WhenHP_AtOrAbove10_AndWon()
    {
        var player = PlayerWithHP(10);
        var stats = new RunStats();

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().NotContain(a => a.Name == "Glass Cannon");
    }

    [Fact]
    public void Untouchable_Unlocks_WhenDamageTaken0_AndWon()
    {
        var player = new Player { Name = "Hero" };
        var stats = new RunStats { DamageTaken = 0 };

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().Contain(a => a.Name == "Untouchable");
    }

    [Fact]
    public void Untouchable_DoesNotUnlock_WhenDamageTaken_IsPositive_AndWon()
    {
        var player = new Player { Name = "Hero" };
        var stats = new RunStats { DamageTaken = 5 };

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().NotContain(a => a.Name == "Untouchable");
    }

    [Fact]
    public void Hoarder_Unlocks_WhenGoldCollected500_AndWon()
    {
        var player = new Player { Name = "Hero" };
        var stats = new RunStats { GoldCollected = 500 };

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().Contain(a => a.Name == "Hoarder");
    }

    [Fact]
    public void EliteHunter_Unlocks_WhenEnemiesDefeated10_AndWon()
    {
        var player = new Player { Name = "Hero" };
        var stats = new RunStats { EnemiesDefeated = 10 };

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().Contain(a => a.Name == "Elite Hunter");
    }

    [Fact]
    public void SpeedRunner_Unlocks_WhenTurnsTaken99_AndWon()
    {
        var player = new Player { Name = "Hero" };
        var stats = new RunStats { TurnsTaken = 99 };

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().Contain(a => a.Name == "Speed Runner");
    }

    [Fact]
    public void SpeedRunner_DoesNotUnlock_WhenTurnsTaken100_AndWon()
    {
        var player = new Player { Name = "Hero" };
        var stats = new RunStats { TurnsTaken = 100 };

        var result = _system.Evaluate(stats, player, won: true);

        result.Should().NotContain(a => a.Name == "Speed Runner");
    }

    [Fact]
    public void NoAchievements_UnlockWhen_Won_IsFalse()
    {
        var player = PlayerWithHP(1);
        var stats = new RunStats { DamageTaken = 0, GoldCollected = 500, EnemiesDefeated = 10, TurnsTaken = 1 };

        var result = _system.Evaluate(stats, player, won: false);

        result.Should().BeEmpty();
    }

    [Fact]
    public void PreviouslyUnlocked_IsNotReturnedAgain_OnSecondEvaluate()
    {
        var player = new Player { Name = "Hero" };
        var stats = new RunStats { DamageTaken = 0 };

        var firstRun = _system.Evaluate(stats, player, won: true);
        firstRun.Should().Contain(a => a.Name == "Untouchable");

        var secondRun = _system.Evaluate(stats, new Player { Name = "Hero" }, won: true);
        secondRun.Should().NotContain(a => a.Name == "Untouchable");
    }

    [Fact]
    public void Persistence_FileCreated_OnUnlock_AndNotReturnedOnReload()
    {
        File.Exists(_tempPath).Should().BeFalse("file should not exist before first unlock");

        var player = new Player { Name = "Hero" };
        var stats = new RunStats { DamageTaken = 0 };
        _system.Evaluate(stats, player, won: true);

        File.Exists(_tempPath).Should().BeTrue("achievements.json should be created after unlock");

        // Re-create system to simulate fresh load from disk
        var system2 = new AchievementSystem();
        var result2 = system2.Evaluate(stats, new Player { Name = "Hero" }, won: true);

        result2.Should().NotContain(a => a.Name == "Untouchable");
    }
}
