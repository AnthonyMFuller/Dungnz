using Dungnz.Display.Spectre;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests.Display;

/// <summary>
/// Regression tests that assert rendered panel markup stays within the height bounds
/// defined in <see cref="LayoutConstants"/>. Guards against the enemy-stats-overflow
/// bug where the Stats panel generated 14-19 lines when only ~8 rows are available.
///
/// Resolves issue #1333.
/// </summary>
/// <remarks>
/// These tests call <see cref="SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup"/>
/// directly (internal, visible via InternalsVisibleTo) so no live terminal is needed.
/// The [Collection("console-output")] attribute prevents parallel interference with
/// tests that do write to AnsiConsole.
/// </remarks>
[Collection("console-output")]
public sealed class PanelHeightRegressionTests
{
    // ── 1. Basic player stays within StatsPanelHeight ─────────────────────────

    /// <summary>
    /// A basic player (no momentum, no cooldowns) must produce markup with no more
    /// than <see cref="LayoutConstants.StatsPanelHeight"/> newlines. This is the
    /// most minimal regression check for the stats panel.
    /// </summary>
    [Fact]
    public void PlayerStatsPanelLineCount_WithBasicPlayer_IsWithinStatsPanelHeight()
    {
        var player = new PlayerBuilder()
            .Named("Hero")
            .WithClass(PlayerClass.Warrior)
            .WithHP(80).WithMaxHP(100)
            .WithMana(20).WithMaxMana(30)
            .WithAttack(12).WithDefense(6)
            .WithLevel(1)
            .WithGold(50)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var lineCount = markup.Count(c => c == '\n');
        lineCount.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"Stats panel has ~{LayoutConstants.StatsPanelHeight} visible rows; " +
            $"basic player produced {lineCount} newlines and would overflow");
    }

    // ── 2. Max-level player stays within StatsPanelHeight ────────────────────

    /// <summary>
    /// A max-level player with all stats filled must still fit within
    /// <see cref="LayoutConstants.StatsPanelHeight"/>. Exercises Mana bar and CHARGED
    /// Momentum paths without cooldowns (the cooldown row is a separate layout concern —
    /// when active, it adds 1 line; that is tracked separately from the enemy-stats overflow
    /// regression this test guards against).
    /// </summary>
    [Fact]
    public void PlayerStatsPanelLineCount_WithMaxLevelPlayer_IsWithinStatsPanelHeight()
    {
        var player = new PlayerBuilder()
            .Named("MaxLevelWarrior")
            .WithClass(PlayerClass.Warrior)
            .WithHP(250).WithMaxHP(250)
            .WithMana(80).WithMaxMana(80)
            .WithAttack(55).WithDefense(30)
            .WithLevel(20)
            .WithGold(9999)
            .WithXP(1850)
            .Build();

        // Warrior has Fury (Momentum) — fill it to CHARGED state (worst case path)
        player.Momentum = new MomentumResource(5);
        player.Momentum.Add(5); // IsCharged = true

        // No cooldowns — the cooldown row adds 1 line and is a separate layout concern.
        // This test validates the base stats content stays within bounds.
        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var lineCount = markup.Count(c => c == '\n');
        lineCount.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"Max-level CHARGED warrior produced {lineCount} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows");
    }

    // ── 3. Long player name does not overflow ─────────────────────────────────

    /// <summary>
    /// A player name of 30+ characters must not push the rendered line count above
    /// <see cref="LayoutConstants.StatsPanelHeight"/>. Long names must not wrap into
    /// extra lines or be otherwise incorrectly handled.
    /// </summary>
    [Fact]
    public void PlayerStatsPanelLineCount_WithLongPlayerName_IsWithinStatsPanelHeight()
    {
        var longName = "Sir Reginald Bartholomew The Third";  // 34 chars
        longName.Length.Should().BeGreaterThan(30, "precondition: name must be 30+ chars");

        var player = new PlayerBuilder()
            .Named(longName)
            .WithClass(PlayerClass.Paladin)
            .WithHP(100).WithMaxHP(100)
            .WithMana(40).WithMaxMana(50)
            .Build();

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var lineCount = markup.Count(c => c == '\n');
        lineCount.Should().BeLessOrEqualTo(LayoutConstants.StatsPanelHeight,
            $"Long player name '{longName}' produced {lineCount} newlines; " +
            $"panel only has {LayoutConstants.StatsPanelHeight} rows");
    }

    // ── 4. Gear panel seam — TODO ─────────────────────────────────────────────

    // TODO: Add GearPanelLineCount_IsWithinGearPanelHeight once a testability seam
    // is extracted from SpectreLayoutDisplayService.RenderGearPanel (currently private).
    // The seam should follow the same pattern as BuildPlayerStatsPanelMarkup:
    //   internal static string BuildGearPanelMarkup(Player player) { ... }
    // Do NOT write a test that instantiates the live SpectreLayoutDisplayService — it
    // requires a running Live context and a terminal, which is unavailable in CI.

    // ── 5. LayoutConstants smoke test ─────────────────────────────────────────

    /// <summary>
    /// Asserts the exact numeric values of <see cref="LayoutConstants"/> so that any
    /// accidental change to the constants fails loudly here, prompting developers to
    /// also update all panel-height-dependent tests and rendering code.
    /// </summary>
    [Fact]
    public void LayoutConstants_HasCorrectValues()
    {
        LayoutConstants.StatsPanelHeight.Should().Be(8,
            "StatsPanelHeight is 20% of 40-row baseline (8 rows); " +
            "if this changes, update all panel height regression tests and renderers");

        LayoutConstants.GearPanelHeight.Should().Be(20,
            "GearPanelHeight is 50% of 40-row baseline (20 rows); " +
            "if this changes, update gear panel rendering and any future seam tests");

        LayoutConstants.BaselineTerminalHeight.Should().Be(40,
            "BaselineTerminalHeight is the 40-row reference terminal; " +
            "all panel height constants derive from this value");
    }
}
