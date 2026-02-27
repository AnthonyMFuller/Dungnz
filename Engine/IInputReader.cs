namespace Dungnz.Engine;

/// <summary>
/// Abstraction over a line-based text input source, enabling the game to read
/// player commands from the console, a test harness, or any other text stream.
/// </summary>
public interface IInputReader
{
    /// <summary>
    /// Reads the next line of text from the underlying input source.
    /// </summary>
    /// <returns>
    /// The line of text entered by the user, or <see langword="null"/> if the input
    /// stream has ended (e.g., end-of-file on stdin).
    /// </returns>
    string? ReadLine();

    /// <summary>
    /// Reads a single keypress from the underlying input source without echoing it
    /// to the console. Used by bounded menus (shop, sell, difficulty, class, level-up,
    /// combat, crafting) to support arrow-key navigation.
    /// </summary>
    /// <returns>
    /// A <see cref="ConsoleKeyInfo"/> describing the key pressed, or
    /// <see langword="null"/> if the input source does not support single-key reads
    /// (e.g., a test stub).
    /// </returns>
    ConsoleKeyInfo? ReadKey();
}

/// <summary>
/// Standard <see cref="IInputReader"/> implementation that reads player input
/// directly from <see cref="Console.ReadLine"/>.
/// </summary>
public class ConsoleInputReader : IInputReader
{
    /// <summary>
    /// Reads the next line of text typed by the player at the console.
    /// </summary>
    /// <returns>
    /// The line entered, or <see langword="null"/> if stdin is closed.
    /// </returns>
    public string? ReadLine() => Console.ReadLine();

    /// <summary>
    /// Reads a single keypress from the console without echoing it to the screen.
    /// </summary>
    /// <returns>A <see cref="ConsoleKeyInfo"/> describing the key pressed.</returns>
    public ConsoleKeyInfo? ReadKey() => Console.ReadKey(intercept: true);
}
