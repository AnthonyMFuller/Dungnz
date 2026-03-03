using Dungnz.Display;
using FluentAssertions;
using Spectre.Console;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Regression tests for issue #870: ShowHelp() crashed with
/// <c>System.InvalidOperationException: Encountered malformed markup tag at position 26</c>
/// because square-bracket sequences like <c>[north|south|east|west]</c> were not
/// escaped with the Spectre double-bracket convention <c>[[…]]</c>.
/// </summary>
[Collection("console-output")]
public sealed class HelpDisplayRegressionTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole;
    private readonly StringWriter _writer;

    public HelpDisplayRegressionTests()
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
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _writer.Dispose();
    }

    /// <summary>
    /// ShowHelp() must not throw InvalidOperationException (malformed markup) — issue #870.
    /// </summary>
    [Fact]
    public void ShowHelp_DoesNotThrow()
    {
        var svc = new SpectreDisplayService();
        var act = () => svc.ShowHelp();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that the rendered help table contains key command names.
    /// </summary>
    [Fact]
    public void ShowHelp_RendersKeyCommandNames()
    {
        new SpectreDisplayService().ShowHelp();
        var output = _writer.ToString();

        output.Should().Contain("go");
        output.Should().Contain("use");
        output.Should().Contain("equip");
        output.Should().Contain("help");
    }
}
