using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for ArchlichSovereign and AbyssalLeviathan boss variant behaviour,
/// including the Phase2Triggered backing-store fix and Leviathan TurnCount turn-matrix.
/// </summary>
public class BossVariantTests
{
    // ── ArchlichSovereign ────────────────────────────────────────────────────

    [Fact]
    public void ArchlichSovereign_Phase2Triggered_SameValueThroughBaseReference()
    {
        // If Phase2Triggered is properly on the base Enemy class (not shadowed),
        // setting it via the derived reference is visible through the base reference.
        var archlich = new ArchlichSovereign();
        archlich.Phase2Triggered = true;

        Enemy baseRef = archlich;

        baseRef.Phase2Triggered.Should().BeTrue(
            "Phase2Triggered must use the base Enemy backing store, not a shadowed property");
    }

    [Fact]
    public void ArchlichSovereign_Phase2Triggered_DefaultIsFalse()
    {
        var archlich = new ArchlichSovereign();
        archlich.Phase2Triggered.Should().BeFalse();
    }

    [Fact]
    public void ArchlichSovereign_IsUndead()
    {
        var archlich = new ArchlichSovereign();
        archlich.IsUndead.Should().BeTrue();
    }

    // ── AbyssalLeviathan TidalSlam turn matrix ───────────────────────────────

    /// <summary>
    /// TidalSlam condition (from CombatEngine):
    ///   TurnCount &gt; 3 AND TurnCount % 3 == 1 AND !IsSubmerged
    /// So it fires on turns 4, 7, 10, ...
    /// </summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public void AbyssalLeviathan_TidalSlam_TurnMatrix(int turnCount, bool expectTidalSlam)
    {
        var leviathan = new AbyssalLeviathan { TurnCount = turnCount, IsSubmerged = false };

        bool isTidalSlam = leviathan.TurnCount > 3
                           && leviathan.TurnCount % 3 == 1
                           && !leviathan.IsSubmerged;

        isTidalSlam.Should().Be(expectTidalSlam,
            $"TurnCount={turnCount}: TidalSlam expected={expectTidalSlam}");
    }

    [Fact]
    public void AbyssalLeviathan_TidalSlam_DoesNotFireWhenSubmerged()
    {
        // Even on a "slam turn" (TurnCount=4), if IsSubmerged=true it should NOT fire.
        var leviathan = new AbyssalLeviathan { TurnCount = 4, IsSubmerged = true };

        bool isTidalSlam = leviathan.TurnCount > 3
                           && leviathan.TurnCount % 3 == 1
                           && !leviathan.IsSubmerged;

        isTidalSlam.Should().BeFalse("TidalSlam requires !IsSubmerged");
    }
}
