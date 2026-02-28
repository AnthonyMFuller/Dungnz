namespace Dungnz.Engine;

/// <summary>
/// Presents the player with a fixed list of choices and returns their selection.
/// Real implementation uses Console.ReadKey with cursor highlighting.
/// Test implementation accepts a pre-scripted sequence of selections.
/// </summary>
public interface IMenuNavigator
{
    /// <summary>
    /// Displays a cursor-navigable list of options and returns the value of the
    /// selected entry. Blocks until the player confirms with Enter.
    /// </summary>
    T Select<T>(IReadOnlyList<MenuOption<T>> options, string? title = null);

    /// <summary>
    /// Shows a Y/N confirmation prompt. Returns true when the player confirms.
    /// </summary>
    bool Confirm(string prompt);
}

/// <summary>Represents a single selectable option in a navigable menu.</summary>
/// <param name="Label">The display text shown for this option.</param>
/// <param name="Value">The value returned when this option is selected.</param>
/// <param name="Subtitle">Optional secondary text shown beneath the label.</param>
public record MenuOption<T>(string Label, T Value, string? Subtitle = null);
