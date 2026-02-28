using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for PrestigeSystem. Each test uses a temp directory to avoid touching
/// the real ApplicationData folder. The override is reset in Dispose().
/// </summary>
[Collection("PrestigeTests")]
public class PrestigeSystemTests : IDisposable
{
    private readonly string _tempDir;

    public PrestigeSystemTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PrestigeTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        PrestigeSystem.SetSavePathForTesting(Path.Combine(_tempDir, "prestige.json"));
    }

    public void Dispose()
    {
        PrestigeSystem.SetSavePathForTesting(null);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void RecordRun_Win_IncrementsWins()
    {
        PrestigeSystem.RecordRun(won: true);

        var data = PrestigeSystem.Load();
        data.TotalWins.Should().Be(1);
        data.TotalRuns.Should().Be(1);
    }

    [Fact]
    public void RecordRun_Loss_DoesNotIncrementWins()
    {
        PrestigeSystem.RecordRun(won: false);

        var data = PrestigeSystem.Load();
        data.TotalWins.Should().Be(0);
        data.TotalRuns.Should().Be(1);
    }

    [Fact]
    public void RecordRun_ThreeWins_UnlocksPrestigeLevel()
    {
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);

        var data = PrestigeSystem.Load();
        data.PrestigeLevel.Should().Be(1);
    }

    [Fact]
    public void RecordRun_BonusesScale_WithPrestigeLevel()
    {
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);

        var data = PrestigeSystem.Load();
        data.BonusStartAttack.Should().Be(1);
        data.BonusStartDefense.Should().Be(1);
        data.BonusStartHP.Should().Be(5);
    }

    [Fact]
    public void Load_CreatesDefaultIfNoFile()
    {
        // No RecordRun called — file doesn't exist
        var data = PrestigeSystem.Load();

        data.Should().NotBeNull();
        data.PrestigeLevel.Should().Be(0);
        data.TotalWins.Should().Be(0);
        data.TotalRuns.Should().Be(0);
    }
}
