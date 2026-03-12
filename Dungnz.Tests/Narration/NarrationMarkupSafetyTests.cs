using Dungnz.Display;
using Dungnz.Systems;
using FluentAssertions;
using Spectre.Console;

namespace Dungnz.Tests.Narration;

/// <summary>
/// Safety tests verifying that narration output does not produce unescaped Spectre markup
/// characters when player-controlled text (enemy names, item names) flows through
/// <see cref="NarrationService"/> to the display layer.
///
/// Safety contract:
///   1. <see cref="NarrationService.Pick(string[], object[])"/> inserts args verbatim
///      via <c>string.Format</c> — the returned string contains raw brackets if the arg does.
///   2. The display layer (<see cref="SpectreDisplayService.ShowCombatMessage"/> and
///      <see cref="SpectreDisplayService.ShowCombat"/>) wraps every message in
///      <c>Markup.Escape(...)</c> before handing it to Spectre.
///   3. Therefore the end-to-end rendering path is safe even when enemy/player names
///      contain Spectre-significant characters (<c>[</c>, <c>]</c>, <c>[[CHARGED]]</c>).
///
/// Tests in this file guard both sides of that contract:
///   — That <see cref="NarrationService"/> methods behave correctly at the string level.
///   — That messages produced from adversarial names survive the full Spectre render path.
///
/// Closes #1362.
/// </summary>
[Collection("console-output")]
public sealed class NarrationMarkupSafetyTests : IDisposable
{
    // ── AnsiConsole isolation ─────────────────────────────────────────────────

    private readonly IAnsiConsole _originalConsole;
    private readonly StringWriter _writer;
    private readonly SpectreDisplayService _display;
    private readonly NarrationService _narration;

    public NarrationMarkupSafetyTests()
    {
        _originalConsole = AnsiConsole.Console;
        _writer = new StringWriter();
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi        = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out         = new AnsiConsoleOutput(_writer),
            Interactive = InteractionSupport.No,
        });
        _display = new SpectreDisplayService();
        _narration = new NarrationService(new Random(0));
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _writer.Dispose();
    }

    // ── Pick (single pool) — null / empty safety ─────────────────────────────

    /// <summary>Pick with a null pool must not throw and must return an empty string.</summary>
    [Fact]
    public void Pick_NullPool_ReturnsEmptyString()
    {
        // Arrange / Act / Assert
        var result = _narration.Pick(null!);
        result.Should().BeEmpty("Pick must return empty string when pool is null");
    }

    /// <summary>Pick with an empty pool must not throw and must return an empty string.</summary>
    [Fact]
    public void Pick_EmptyPool_ReturnsEmptyString()
    {
        var result = _narration.Pick(Array.Empty<string>());
        result.Should().BeEmpty("Pick must return empty string when pool is empty");
    }

    // ── Pick (format overload) — bracket passthrough ──────────────────────────

    /// <summary>
    /// Pick with a single-bracket arg inserts the literal brackets into the output string.
    /// This documents the contract: NarrationService returns raw text; callers (i.e. the
    /// display layer) are responsible for escaping before passing to Spectre markup rendering.
    /// </summary>
    [Fact]
    public void Pick_FormatWithOpenBracketInArg_ReturnsBracketLiterallyInOutput()
    {
        // Arrange
        var pool = new[] { "The {0} attacks!" };

        // Act
        var result = _narration.Pick(pool, "[DangerousEnemy]");

        // Assert — raw bracket in output is expected; caller must Markup.Escape before rendering
        result.Should().Be("The [DangerousEnemy] attacks!",
            "Pick uses string.Format verbatim — raw brackets are the caller's escaping responsibility");
    }

    /// <summary>
    /// Pick with a double-bracket arg (already Spectre-escaped by the caller) passes
    /// the doubled brackets through unchanged, preserving the caller's pre-escaping.
    /// </summary>
    [Fact]
    public void Pick_FormatWithDoubleEscapedBrackets_PreservesEscaping()
    {
        var pool = new[] { "Status: {0}" };
        var result = _narration.Pick(pool, "[[CHARGED]]");

        result.Should().Be("Status: [[CHARGED]]",
            "Pre-escaped double brackets must be preserved verbatim by Pick");
    }

    /// <summary>
    /// Pick with a name containing only a lone open bracket does not throw.
    /// </summary>
    [Fact]
    public void Pick_FormatWithLoneOpenBracket_DoesNotThrow()
    {
        var pool = new[] { "The {0} lunges!" };
        var act = () => _narration.Pick(pool, "[");
        act.Should().NotThrow();
    }

    /// <summary>
    /// Pick with a name containing only a lone close bracket does not throw.
    /// </summary>
    [Fact]
    public void Pick_FormatWithLoneCloseBracket_DoesNotThrow()
    {
        var pool = new[] { "The {0} lunges!" };
        var act = () => _narration.Pick(pool, "]");
        act.Should().NotThrow();
    }

    // ── Enemy-name narration methods — adversarial names ─────────────────────

    /// <summary>
    /// GetEnemyCritReaction with a bracket-containing enemy name must not throw.
    /// Exercises the fallback path that returns a generic reaction pool.
    /// </summary>
    [Fact]
    public void GetEnemyCritReaction_BracketEnemyName_DoesNotThrow()
    {
        var act = () => _narration.GetEnemyCritReaction("[BracketMonster]");
        act.Should().NotThrow();
    }

    /// <summary>GetEnemyCritReaction always returns a non-empty string regardless of name.</summary>
    [Fact]
    public void GetEnemyCritReaction_BracketEnemyName_ReturnsNonEmptyString()
    {
        var result = _narration.GetEnemyCritReaction("[BracketMonster]");
        result.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// GetEnemyIdleTaunt with a bracket-containing name must not throw.
    /// Unknown names return null (no custom taunts defined).
    /// </summary>
    [Fact]
    public void GetEnemyIdleTaunt_BracketEnemyName_DoesNotThrow()
    {
        var act = () => _narration.GetEnemyIdleTaunt("[BracketMonster]");
        act.Should().NotThrow();
    }

    /// <summary>
    /// GetEnemyDesperationLine with a bracket-containing name must not throw.
    /// </summary>
    [Fact]
    public void GetEnemyDesperationLine_BracketEnemyName_DoesNotThrow()
    {
        var act = () => _narration.GetEnemyDesperationLine("[BracketMonster]");
        act.Should().NotThrow();
    }

    // ── Phase-aware narration — Spectre safety through display layer ──────────

    /// <summary>
    /// Phase-aware narration strings rendered through ShowCombatMessage must not throw.
    /// The display layer applies Markup.Escape, so even strings with incidental brackets
    /// must survive the full path.
    /// </summary>
    [Theory]
    [InlineData(1,  1.0, 1.0)]  // Opening phase: turn 1, both at full HP
    [InlineData(5,  0.6, 0.6)]  // MidFight phase: turn 5, moderate HP
    [InlineData(10, 0.2, 0.8)]  // Desperate phase: player low HP
    [InlineData(10, 0.8, 0.2)]  // Desperate phase: enemy low HP
    [InlineData(8,  0.8, 0.8)]  // Desperate phase: turn 8+ threshold
    public void GetPhaseAwareAttackNarration_AllPhases_RendersWithoutThrow(
        int turnNumber, double playerHpPercent, double enemyHpPercent)
    {
        // Arrange
        var text = _narration.GetPhaseAwareAttackNarration(turnNumber, playerHpPercent, enemyHpPercent);

        // Act — pipe through the display layer to exercise Markup.Escape
        var act = () => _display.ShowCombatMessage(text);

        // Assert
        act.Should().NotThrow(
            $"narration for turn={turnNumber}, playerHP={playerHpPercent}, enemyHP={enemyHpPercent} " +
            $"must render safely through ShowCombatMessage");
    }

    // ── Room entry narration — Spectre safety through display layer ───────────

    /// <summary>
    /// Room entry narration for every state must render through ShowCombat without throw.
    /// These strings can contain characters like apostrophes or dashes that must survive
    /// Markup.Escape at the display boundary.
    /// </summary>
    [Theory]
    [InlineData(RoomNarrationState.FirstVisit)]
    [InlineData(RoomNarrationState.ActiveEnemies)]
    [InlineData(RoomNarrationState.Cleared)]
    [InlineData(RoomNarrationState.Merchant)]
    [InlineData(RoomNarrationState.Shrine)]
    [InlineData(RoomNarrationState.Boss)]
    public void GetRoomEntryNarration_AllStates_RendersWithoutThrow(RoomNarrationState state)
    {
        // Arrange
        var text = _narration.GetRoomEntryNarration(state);

        // Act — ShowCombat wraps in Markup.Escape before handing to Spectre Rule
        var act = () => _display.ShowCombat(text);

        // Assert
        act.Should().NotThrow(
            $"room narration for {state} must render safely through ShowCombat");
    }

    // ── End-to-end: adversarial name through Pick → ShowCombatMessage ─────────

    /// <summary>
    /// The full path: Pick formats an enemy name containing Spectre-significant characters
    /// into a pool string, then ShowCombatMessage renders via Markup.Escape. Must not throw.
    /// This is the primary regression guard for the bracket-injection class of bugs.
    /// </summary>
    [Theory]
    [InlineData("[BracketMonster]")]   // Spectre tag-like: [Word]
    [InlineData("[CHARGED]")]          // Status-effect label (ALL_CAPS — known crash class)
    [InlineData("[DarkKnight]")]       // CamelCase — known crash class per content authoring spec
    [InlineData("]broken[")]          // Reversed brackets
    [InlineData("[[AlreadyEscaped]]")] // Pre-escaped brackets
    [InlineData("[")]                  // Lone open bracket
    [InlineData("]")]                  // Lone close bracket
    public void Pick_AdversarialEnemyName_ThroughShowCombatMessage_DoesNotThrow(string dangerousName)
    {
        // Arrange
        var pool = new[] { "The {0} attacks!", "{0} deals {1} damage!", "You strike the {0}!" };

        // Act — format name into a random pool string, then render through display
        var narrated = _narration.Pick(pool, dangerousName, 15);
        var act = () => _display.ShowCombatMessage(narrated);

        // Assert
        act.Should().NotThrow(
            $"name '{dangerousName}' formatted into pool string and rendered through " +
            $"ShowCombatMessage (which calls Markup.Escape) must never throw");
    }

    /// <summary>
    /// The same adversarial names through ShowCombat (the rule-based combat log path).
    /// </summary>
    [Theory]
    [InlineData("[BracketMonster]")]
    [InlineData("[CHARGED]")]
    [InlineData("[DarkKnight]")]
    [InlineData("]broken[")]
    [InlineData("[")]
    [InlineData("]")]
    public void Pick_AdversarialEnemyName_ThroughShowCombat_DoesNotThrow(string dangerousName)
    {
        // Arrange
        var pool = new[] { "The {0} falls." };

        // Act
        var narrated = _narration.Pick(pool, dangerousName);
        var act = () => _display.ShowCombat(narrated);

        // Assert
        act.Should().NotThrow(
            $"name '{dangerousName}' through ShowCombat must not throw");
    }
}
