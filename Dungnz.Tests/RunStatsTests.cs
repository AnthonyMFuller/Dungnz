using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for RunStats accumulation — FloorsVisited, DamageTaken, and history recording.
/// RunStats is a POCO; GameLoop writes to it directly, so we exercise it directly here.
/// </summary>
public class RunStatsTests
{
    [Fact]
    public void FloorsVisited_IncrementsOnEachDescend()
    {
        var stats = new RunStats();

        // Simulate GameLoop.HandleDescend(): it sets FloorsVisited = _currentFloor
        stats.FloorsVisited = 2; // after first descend (now on floor 2)
        stats.FloorsVisited = 3; // after second descend (now on floor 3)

        stats.FloorsVisited.Should().Be(3);
    }

    [Fact]
    public void FloorsVisited_StartsAtZero()
    {
        var stats = new RunStats();
        stats.FloorsVisited.Should().Be(0);
    }

    [Fact]
    public void DamageTaken_TrackedForHazardDamage()
    {
        var stats = new RunStats();
        const int hazardDamage = 5; // matches GameLoop hazard trap damage

        stats.DamageTaken += hazardDamage;

        stats.DamageTaken.Should().Be(hazardDamage);
    }

    [Fact]
    public void DamageTaken_AccumulatesAcrossMultipleHits()
    {
        var stats = new RunStats();

        stats.DamageTaken += 5;  // trap hit 1
        stats.DamageTaken += 3;  // trap hit 2

        stats.DamageTaken.Should().Be(8);
    }

    [Fact]
    public void RecordRunEnd_CalledForCombatDeath_HistoryContainsEntry()
    {
        var stats = new RunStats
        {
            TurnsTaken = 7,
            EnemiesDefeated = 3,
            DamageDealt = 42,
            DamageTaken = 15,
            GoldCollected = 10,
            ItemsFound = 2,
            FinalLevel = 2,
            TimeElapsed = TimeSpan.FromSeconds(90),
            Won = false,
        };

        // Ensure any prior corruption is cleared before writing
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dungnz");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "stats-history.json");
        File.WriteAllText(path, "[]");

        RunStats.AppendToHistory(stats, won: false);

        var history = RunStats.LoadHistory();
        history.Should().NotBeEmpty();
        history.Should().Contain(h => h.TurnsTaken == 7 && h.EnemiesDefeated == 3 && !h.Won);
    }

    [Fact]
    public void RecordRunEnd_CalledForTrapDeath_HistoryContainsEntry()
    {
        var stats = new RunStats
        {
            TurnsTaken = 4,
            EnemiesDefeated = 1,
            DamageDealt = 20,
            DamageTaken = 100, // lethal trap
            GoldCollected = 0,
            ItemsFound = 0,
            FinalLevel = 1,
            TimeElapsed = TimeSpan.FromSeconds(30),
            Won = false,
        };

        // Ensure any prior corruption is cleared before writing
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dungnz");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "stats-history.json");
        File.WriteAllText(path, "[]");

        RunStats.AppendToHistory(stats, won: false);

        var history = RunStats.LoadHistory();
        history.Should().NotBeEmpty();
        history.Should().Contain(h => h.TurnsTaken == 4 && h.DamageTaken == 100 && !h.Won);
    }
}
