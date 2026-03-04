using Dungnz.Engine;

namespace Dungnz.Display.Tui;

/// <summary>
/// Terminal.Gui implementation of <see cref="IInputReader"/> that reads player
/// input from the TUI command input field via the game thread bridge.
/// </summary>
public sealed class TerminalGuiInputReader : IInputReader
{
    private readonly GameThreadBridge _bridge;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalGuiInputReader"/> class.
    /// </summary>
    /// <param name="bridge">The thread bridge for coordinating between game and UI threads.</param>
    public TerminalGuiInputReader(GameThreadBridge bridge)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    /// <inheritdoc/>
    public string? ReadLine()
    {
        // Block game thread until user types a command in the TUI
        return _bridge.WaitForCommand();
    }

    /// <inheritdoc/>
    public ConsoleKeyInfo? ReadKey()
    {
        // Terminal.Gui handles all key input through its event loop.
        // Arrow-key menu navigation is handled by TuiMenuDialog directly.
        // This method is not used in the TUI path.
        return null;
    }

    /// <inheritdoc/>
    public bool IsInteractive => false; // TUI uses modal dialogs, not Console.ReadKey
}
