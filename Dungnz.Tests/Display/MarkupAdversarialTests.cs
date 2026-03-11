using Dungnz.Display;
using Dungnz.Display.Spectre;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Builders;
using FluentAssertions;
using Spectre.Console;

namespace Dungnz.Tests.Display;

/// <summary>
/// Adversarial markup smoke tests — resolves issues #1331 and #1332.
///
/// Issue #1331: Verifies that all ShowXxx display methods survive input containing
/// bracket characters (e.g., "[HERO]", "[CHARGED]") without throwing an
/// <see cref="Spectre.Console.MarkupException"/> or
/// <see cref="InvalidOperationException"/> from Spectre's markup parser.
///
/// Issue #1332: Verifies that <see cref="SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup"/>
/// produces ≤8 lines for a typical combat state. The Stats panel is 20% screen height
/// (~8 visible rows); overflowing it is invisible until a player reports truncated stats.
///
/// Strategy:
/// - <see cref="SpectreDisplayService"/> tests use AnsiConsole.Create (no-color, no-interactive)
///   to capture output without a live terminal, following the pattern in DisplayServiceSmokeTests.
/// - Panel markup tests call <see cref="SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup"/>
///   directly (internal, visible to Dungnz.Tests via InternalsVisibleTo) and validate both
///   Spectre Markup construction and rendered line count.
/// </summary>
[Collection("console-output")]
public sealed class MarkupAdversarialTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole;
    private readonly StringWriter _writer;
    private readonly SpectreDisplayService _svc;

    public MarkupAdversarialTests()
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
        _svc = new SpectreDisplayService();
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _writer.Dispose();
    }

    // ── Issue #1331: ShowCombatStatus adversarial markup ─────────────────────

    /// <summary>
    /// Regression guard for the [CHARGED] crash class.
    /// Passes a player with fully charged Momentum through ShowCombatStatus.
    /// Spectre parses any unescaped [WORD] as a style tag; this test ensures
    /// the CHARGED state does not escape into unquoted markup.
    /// </summary>
    [Fact]
    public void ShowCombatStatus_WithChargedStatusEffect_DoesNotThrow()
    {
        var player = new PlayerBuilder()
            .Named("Warrior")
            .WithClass(PlayerClass.Warrior)
            .Build();
        player.Momentum = new MomentumResource(3);
        player.Momentum.Add(3); // IsCharged = true

        var enemy = new Goblin();

        var act = () => _svc.ShowCombatStatus(player, enemy,
            Array.Empty<ActiveEffect>(),
            Array.Empty<ActiveEffect>());

        act.Should().NotThrow();
    }

    /// <summary>
    /// Player name containing square brackets must not crash Spectre markup rendering.
    /// An unescaped name like "[HERO]" would produce "[bold][HERO][/]", which Spectre
    /// would interpret as nested malformed tags.
    /// </summary>
    [Fact]
    public void ShowCombatStatus_PlayerNameWithBrackets_DoesNotThrow()
    {
        var player = new PlayerBuilder().Named("[HERO]").Build();
        var enemy  = new Goblin();

        var act = () => _svc.ShowCombatStatus(player, enemy,
            Array.Empty<ActiveEffect>(),
            Array.Empty<ActiveEffect>());

        act.Should().NotThrow();
    }

    /// <summary>
    /// Enemy name containing square brackets must not crash markup rendering.
    /// </summary>
    [Fact]
    public void ShowCombatStatus_EnemyNameWithBrackets_DoesNotThrow()
    {
        var player = new PlayerBuilder().Build();
        var enemy  = new EnemyBuilder().Named("[BOSS]").Build();

        var act = () => _svc.ShowCombatStatus(player, enemy,
            Array.Empty<ActiveEffect>(),
            Array.Empty<ActiveEffect>());

        act.Should().NotThrow();
    }

    // ── Issue #1331: ShowRoom adversarial markup ──────────────────────────────

    /// <summary>
    /// Room description with bracket content must not crash the panel header or body markup.
    /// </summary>
    [Fact]
    public void ShowRoom_DescriptionWithBrackets_DoesNotThrow()
    {
        var room = new RoomBuilder()
            .Named("[Ancient Hall] of the Damned")
            .Build();

        var act = () => _svc.ShowRoom(room);

        act.Should().NotThrow();
    }

    // ── Issue #1331: ShowCombatMessage adversarial markup ─────────────────────

    /// <summary>
    /// ShowCombatMessage with bracket content in the message text must not crash.
    /// Combat messages can contain ability names like "[CHARGED] strike!" from narration.
    /// </summary>
    [Fact]
    public void ShowCombatMessage_WithBracketContent_DoesNotThrow()
    {
        var act = () => _svc.ShowCombatMessage("[CHARGED] strike! You deal 42 damage!");

        act.Should().NotThrow();
    }

    // ── Issue #1331: ShowPlayerStats adversarial markup ───────────────────────

    /// <summary>
    /// ShowPlayerStats with a bracket-containing player name must not crash the table rendering.
    /// </summary>
    [Fact]
    public void ShowPlayerStats_PlayerNameWithBrackets_DoesNotThrow()
    {
        var player = new PlayerBuilder().Named("[Legendary] Hero").Build();

        var act = () => _svc.ShowPlayerStats(player);

        act.Should().NotThrow();
    }

    // ── Issue #1331: BuildPlayerStatsPanelMarkup — CHARGED path ──────────────

    /// <summary>
    /// The stats panel markup builder must produce valid Spectre markup when the player
    /// is in the CHARGED momentum state. The literal text "[CHARGED]" must appear in the
    /// output as the escaped form "[[CHARGED]]" so Spectre renders it as text, not a tag.
    /// An InvalidOperationException from <c>new Markup(string)</c> is the regression signal.
    /// </summary>
    [Fact]
    public void BuildPlayerStatsPanelMarkup_WithChargedMomentum_ProducesValidMarkup()
    {
        var player = new PlayerBuilder()
            .Named("Warrior")
            .WithClass(PlayerClass.Warrior)
            .Build();
        player.Momentum = new MomentumResource(3);
        player.Momentum.Add(3); // IsCharged = true

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        // Must parse without throwing — this is the crash gate.
        var act = () => _ = new Markup(markup);
        act.Should().NotThrow("unescaped [CHARGED] would throw InvalidOperationException");

        // The escaped form must be present, not the raw form.
        markup.Should().Contain("[[CHARGED]]", "Spectre escape for literal [CHARGED] is [[CHARGED]]");
    }

    /// <summary>
    /// Uncharged momentum must produce valid markup with no [[CHARGED]] suffix.
    /// </summary>
    [Fact]
    public void BuildPlayerStatsPanelMarkup_WithUnchargedMomentum_ProducesValidMarkup()
    {
        var player = new PlayerBuilder()
            .Named("Mage")
            .WithClass(PlayerClass.Mage)
            .Build();
        player.Momentum = new MomentumResource(3);
        player.Momentum.Add(1); // Partially charged, not full

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var act = () => _ = new Markup(markup);
        act.Should().NotThrow();
        markup.Should().NotContain("[[CHARGED]]");
    }

    /// <summary>
    /// Player name with bracket characters must be escaped in the panel markup.
    /// </summary>
    [Fact]
    public void BuildPlayerStatsPanelMarkup_PlayerNameWithBrackets_ProducesValidMarkup()
    {
        var player = new PlayerBuilder().Named("[HERO]").Build();

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var act = () => _ = new Markup(markup);
        act.Should().NotThrow("player name with brackets must be Markup.Escaped");
    }

    // ── Issue #1332: RenderCombatStatsPanel line count ────────────────────────

    /// <summary>
    /// The Stats panel is 20% of the terminal height (~8 visible rows).
    /// BuildPlayerStatsPanelMarkup must not produce more than 8 newlines for the
    /// typical warrior-in-combat state (HP+MP bars, ATK/DEF, Gold, XP, Momentum).
    /// Hardcoded to 8 — Issue #1334 will move this to LayoutConstants.cs.
    /// </summary>
    [Fact]
    public void BuildPlayerStatsPanelMarkup_TypicalCombatState_RendersWithinStatsPanelBounds()
    {
        const int maxLines = 8;

        var player = new PlayerBuilder()
            .Named("Warrior")
            .WithClass(PlayerClass.Warrior)
            .WithHP(80).WithMaxHP(100)
            .WithMana(15).WithMaxMana(30)
            .Build();
        player.Momentum = new MomentumResource(5);
        player.Momentum.Add(3); // Partially charged

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var lineCount = markup.Count(c => c == '\n');
        lineCount.Should().BeLessOrEqualTo(maxLines,
            $"Stats panel is ~{maxLines} visible rows; {lineCount} newlines would overflow the panel");
    }

    /// <summary>
    /// Even with CHARGED momentum, the panel must stay within 8 lines.
    /// This is the worst-case for a momentum-bearing class with mana.
    /// </summary>
    [Fact]
    public void BuildPlayerStatsPanelMarkup_ChargedMomentumWithMana_RendersWithinStatsPanelBounds()
    {
        const int maxLines = 8;

        var player = new PlayerBuilder()
            .Named("Mage")
            .WithClass(PlayerClass.Mage)
            .WithHP(60).WithMaxHP(100)
            .WithMana(10).WithMaxMana(50)
            .Build();
        player.Momentum = new MomentumResource(3);
        player.Momentum.Add(3); // IsCharged = true

        var markup = SpectreLayoutDisplayService.BuildPlayerStatsPanelMarkup(
            player, Array.Empty<(string, int)>());

        var lineCount = markup.Count(c => c == '\n');
        lineCount.Should().BeLessOrEqualTo(maxLines,
            $"CHARGED mage with mana: {lineCount} lines exceeds the {maxLines}-line Stats panel");
    }
}
