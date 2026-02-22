using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for the ColorizeDamage behaviour inside CombatEngine (Issue #220).
///
/// ColorizeDamage is private, so its output is observed through
/// ShowCombatMessage calls captured by RawCombatMessages on FakeDisplayService.
/// ControlledRandom(0.9) ensures: no player/enemy dodge, no crits, and the
/// first narration template is always selected.
/// </summary>
public class ColorizeDamageTests
{
    // First enemy-hit template: "{0} strikes you for {1} damage!"
    private const string EnemyHitTemplate = "{0} strikes you for {1} damage!";

    /// <summary>
    /// Runs one combat exchange so the enemy lands exactly one hit on the player.
    /// Returns the raw (ANSI-intact) combat messages captured.
    /// </summary>
    private static List<string> RunAndCapture(string enemyName, int enemyAtk, int enemyHp = 20)
    {
        var player = new Player { HP = 200, MaxHP = 200, Attack = 10, Defense = 0 };
        var enemy = new Enemy_Stub(enemyHp, enemyAtk, 0, 10);
        enemy.Name = enemyName;

        var display = new FakeDisplayService();
        var input = new FakeInputReader("A", "A");
        var rng = new ControlledRandom(defaultDouble: 0.9);
        var engine = new CombatEngine(display, input, rng);

        engine.RunCombat(player, enemy);
        return display.RawCombatMessages;
    }

    [Fact]
    public void ColorizeDamage_NormalCase_OnlyColorizesDamageNumber()
    {
        // Arrange — enemy name does NOT contain the damage number
        // damage = Max(1, 7 - 0) = 7; message = "TestEnemy strikes you for 7 damage!"
        const string enemyName = "TestEnemy";
        const int enemyAtk = 7;

        // Act
        var rawMessages = RunAndCapture(enemyName, enemyAtk);

        // Assert — find the enemy-hit message in the raw output
        var expectedPlain = string.Format(EnemyHitTemplate, enemyName, enemyAtk);
        var hit = rawMessages.FirstOrDefault(m =>
            ColorCodes.StripAnsiCodes(m).Contains(expectedPlain));
        hit.Should().NotBeNull($"expected an enemy hit message matching '{expectedPlain}'");

        // The damage number "7" should appear colourised exactly once
        int colourizedCount = CountColorized(hit!, enemyAtk.ToString());
        colourizedCount.Should().Be(1, "the single damage number should be wrapped in ANSI colour");

        // The plain text content is preserved after stripping
        ColorCodes.StripAnsiCodes(hit!).Should().Be(expectedPlain);
    }

    [Fact]
    public void ColorizeDamage_EdgeCase_OnlyLastOccurrenceIsColorized_WhenDamageAppearsInEnemyName()
    {
        // Arrange — enemy name IS the same string as the damage value.
        // damage = Max(1, 5 - 0) = 5
        // First narration template: "{0} strikes you for {1} damage!"
        // → "5 strikes you for 5 damage!" — the number "5" appears twice.
        const string enemyName = "5";
        const int enemyAtk = 5;

        // Act
        var rawMessages = RunAndCapture(enemyName, enemyAtk);

        // Assert — find the hit message whose stripped form matches the expected plain text
        var expectedPlain = string.Format(EnemyHitTemplate, enemyName, enemyAtk);
        var hit = rawMessages.FirstOrDefault(m =>
            ColorCodes.StripAnsiCodes(m).Contains(expectedPlain));
        hit.Should().NotBeNull($"expected an enemy hit message matching '{expectedPlain}'");

        // Only the LAST occurrence should be colourised (the damage value, not the name).
        int colorizedCount = CountColorized(hit!, enemyAtk.ToString());
        colorizedCount.Should().Be(1,
            "ColorizeDamage must only colorize the last occurrence of the damage number (fix for #220)");

        // The first "5" (in the enemy name position) must NOT be wrapped in ANSI.
        // The raw message must start with the plain enemy name, not an escape sequence.
        hit!.Should().StartWith(enemyName,
            "the first occurrence of the damage number (in the enemy name) must not be colourised");

        // The last "5" (the damage value) must be wrapped in ANSI colour.
        var colorizedDamage = ColorCodes.Colorize(enemyAtk.ToString(), ColorCodes.BrightRed);
        hit.Should().Contain(colorizedDamage,
            "the last occurrence of the damage number (the actual damage) must be colourised in red");
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Counts how many times <paramref name="value"/> appears wrapped in
    /// BrightRed ANSI colour within <paramref name="raw"/>.
    /// </summary>
    private static int CountColorized(string raw, string value)
    {
        var colorized = ColorCodes.Colorize(value, ColorCodes.BrightRed);
        int count = 0, pos = 0;
        while ((pos = raw.IndexOf(colorized, pos, StringComparison.Ordinal)) >= 0)
        {
            count++;
            pos += colorized.Length;
        }
        return count;
    }
}
