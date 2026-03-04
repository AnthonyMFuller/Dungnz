using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// #952 — Tests for prestige/rebirth cross-run mechanics: stat preservation,
/// bonus accumulation, edge cases (prestige at level 0, multiple prestiges).
/// </summary>
[Collection("PrestigeTests")]
public class PrestigeCrossRunTests : IDisposable
{
    private readonly string _tempDir;

    public PrestigeCrossRunTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PrestigeCross_{Guid.NewGuid():N}");
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
    public void MultiplePrestigeLevels_BonusesCumulative()
    {
        for (int i = 0; i < 6; i++)
            PrestigeSystem.RecordRun(won: true);

        var data = PrestigeSystem.Load();
        data.PrestigeLevel.Should().Be(2);
        data.BonusStartAttack.Should().Be(2);
        data.BonusStartDefense.Should().Be(2);
        data.BonusStartHP.Should().Be(10);
    }

    [Fact]
    public void LossesDoNotIncrementPrestigeLevel()
    {
        for (int i = 0; i < 10; i++)
            PrestigeSystem.RecordRun(won: false);

        var data = PrestigeSystem.Load();
        data.PrestigeLevel.Should().Be(0);
        data.TotalRuns.Should().Be(10);
        data.TotalWins.Should().Be(0);
    }

    [Fact]
    public void MixedWinsAndLosses_OnlyWinsCountForPrestige()
    {
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: false);
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: false);
        PrestigeSystem.RecordRun(won: true);

        var data = PrestigeSystem.Load();
        data.TotalWins.Should().Be(3);
        data.TotalRuns.Should().Be(5);
        data.PrestigeLevel.Should().Be(1);
    }

    [Fact]
    public void PrestigeData_DefaultValues_AreZero()
    {
        var data = new PrestigeData();
        data.PrestigeLevel.Should().Be(0);
        data.TotalWins.Should().Be(0);
        data.TotalRuns.Should().Be(0);
        data.BonusStartAttack.Should().Be(0);
        data.BonusStartDefense.Should().Be(0);
        data.BonusStartHP.Should().Be(0);
    }

    [Fact]
    public void PrestigeDisplay_Level0_ContainsLevel()
    {
        var data = new PrestigeData { PrestigeLevel = 0 };
        var display = PrestigeSystem.GetPrestigeDisplay(data);
        display.Should().NotBeNull();
    }

    [Fact]
    public void PrestigeDisplay_Level3_ContainsLevel()
    {
        var data = new PrestigeData { PrestigeLevel = 3, BonusStartAttack = 3, BonusStartDefense = 3, BonusStartHP = 15 };
        var display = PrestigeSystem.GetPrestigeDisplay(data);
        display.Should().Contain("3");
    }

    [Fact]
    public void SaveAndLoad_PreservesAllFields()
    {
        var original = new PrestigeData
        {
            PrestigeLevel = 5,
            TotalWins = 15,
            TotalRuns = 30,
            BonusStartAttack = 5,
            BonusStartDefense = 5,
            BonusStartHP = 25
        };
        PrestigeSystem.Save(original);

        var loaded = PrestigeSystem.Load();
        loaded.PrestigeLevel.Should().Be(5);
        loaded.TotalWins.Should().Be(15);
        loaded.TotalRuns.Should().Be(30);
        loaded.BonusStartAttack.Should().Be(5);
        loaded.BonusStartDefense.Should().Be(5);
        loaded.BonusStartHP.Should().Be(25);
    }

    [Fact]
    public void TwoWins_NotEnoughForPrestige()
    {
        PrestigeSystem.RecordRun(won: true);
        PrestigeSystem.RecordRun(won: true);

        var data = PrestigeSystem.Load();
        data.PrestigeLevel.Should().Be(0, "need 3 wins for first prestige");
        data.BonusStartAttack.Should().Be(0);
    }

    [Fact]
    public void NineWins_ThreePrestigeLevels()
    {
        for (int i = 0; i < 9; i++)
            PrestigeSystem.RecordRun(won: true);

        var data = PrestigeSystem.Load();
        data.PrestigeLevel.Should().Be(3);
        data.BonusStartAttack.Should().Be(3);
        data.BonusStartDefense.Should().Be(3);
        data.BonusStartHP.Should().Be(15);
    }
}
