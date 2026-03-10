using Dungnz.Display.Spectre;
using Spectre.Console;

namespace Dungnz.Tests.Display;

/// <summary>
/// Unit tests for <see cref="AnsiMarkupConverter"/> — the ANSI-to-Spectre markup
/// conversion and stripping utilities extracted from SpectreLayoutDisplayService.
/// </summary>
public class AnsiMarkupConverterTests
{
    // ── ConvertAnsiInlineToSpectre ──────────────────────────────────────────

    [Fact]
    public void NoAnsiCodes_ReturnsMarkupEscapedInput()
    {
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre("Hello world");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void PlainTextWithBrackets_EscapesBracketsForSpectre()
    {
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre("dmg [10]");
        Assert.Equal("dmg [[10]]", result);
        Assert.False(result.Contains("<"), "Should not produce angle brackets");
    }

    [Fact]
    public void SimpleColorWrap_ProducesClosedTag()
    {
        // "\x1b[91m15\x1b[0m" — bright-red "15"
        var input = "\x1b[91m15\x1b[0m";
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        Assert.Equal("[red]15[/]", result);
        AssertValidSpectreMarkup(result);
    }

    [Fact]
    public void SimpleBoldWrap_ProducesClosedTag()
    {
        var input = "\x1b[1mBold text\x1b[0m";
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        Assert.Equal("[bold]Bold text[/]", result);
        AssertValidSpectreMarkup(result);
    }

    [Fact]
    public void BoldAndColor_ProducesClosedTag()
    {
        var input = "\x1b[33m\x1b[1mYellow bold\x1b[0m";
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        Assert.Contains("[bold yellow]", result);
        Assert.Contains("[/]", result);
        AssertValidSpectreMarkup(result);
    }

    /// <summary>
    /// Regression test for bug #1304.
    /// ColorizeDamage for crits wraps the message in bold-yellow while embedding a
    /// bright-red damage number. The outer bold-yellow tag must be properly closed
    /// before the inner bright-red tag is opened.
    /// </summary>
    [Fact]
    public void NestedAnsiCodes_CritDamage_AllTagsProperlyClosedAndValid()
    {
        // Simulates the exact ANSI output from ColorizeDamage for a critical hit:
        //   ColorCodes.Colorize(message-with-bright-red-damage, Yellow + Bold)
        // which produces:  \x1b[33m\x1b[1m<text>\x1b[91m<dmg>\x1b[0m<text>\x1b[0m
        const string input =
            "\x1b[33m\x1b[1m\U0001F4A5 CRUSHING BLOW! You hit the Goblin for \x1b[91m42\x1b[0m damage!\x1b[0m";

        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        // Must not contain any unclosed/improperly-stacked tags
        AssertValidSpectreMarkup(result);

        // Outer bold-yellow and inner bold-red (bold is still active at the inner code point)
        // must both appear and be individually closed
        Assert.Contains("[bold yellow]", result);
        Assert.Contains("[bold red]42[/]", result);
    }

    [Fact]
    public void NestedAnsiCodes_TextBeforeAndAfterInnerCode_ProducesValidMarkup()
    {
        // "\x1b[33m[prefix] \x1b[91m[number]\x1b[0m [suffix]\x1b[0m"
        var input = "\x1b[33mprefix \x1b[91m99\x1b[0m suffix\x1b[0m";
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        AssertValidSpectreMarkup(result);

        // Outer yellow and inner red content must both be present
        Assert.Contains("[yellow]prefix ", result);
        Assert.Contains("[red]99[/]", result);
        Assert.Contains("suffix", result);
    }

    [Fact]
    public void MultipleColorSegments_EachProducesClosedTag()
    {
        // Two separate colored segments in the same string
        var input = "\x1b[32mgreen\x1b[0m and \x1b[91mred\x1b[0m";
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        Assert.Equal("[green]green[/] and [red]red[/]", result);
        AssertValidSpectreMarkup(result);
    }

    [Fact]
    public void UnknownAnsiCode_TreatedAsResetProducesNoTag()
    {
        // Unknown code \x1b[35m (magenta, not mapped) → currentColor = ""
        var input = "\x1b[35mtext\x1b[0m";
        var result = AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        // Unknown color → no tag opened, plain escaped text
        Assert.Equal("text", result);
    }

    // ── StripAnsiCodes ──────────────────────────────────────────────────────

    [Fact]
    public void StripAnsiCodes_RemovesAllEscapeSequences()
    {
        var input = "\x1b[33m\x1b[1mhello\x1b[0m \x1b[91mworld\x1b[0m";
        var result = AnsiMarkupConverter.StripAnsiCodes(input);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void StripAnsiCodes_NoAnsiCodes_ReturnsOriginal()
    {
        const string input = "plain text";
        Assert.Equal(input, AnsiMarkupConverter.StripAnsiCodes(input));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that the given string is valid Spectre.Console markup by attempting to
    /// parse it. Spectre throws <see cref="InvalidOperationException"/> on malformed
    /// markup (unclosed tags, unknown styles, etc.).
    /// </summary>
    private static void AssertValidSpectreMarkup(string markup)
    {
        var ex = Record.Exception(() => new Markup(markup));
        Assert.Null(ex); // no exception = valid markup
    }
}
