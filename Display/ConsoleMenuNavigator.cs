using Dungnz.Engine;
using Dungnz.Systems;

namespace Dungnz.Display;

/// <summary>
/// Arrow-key navigable menu for interactive console sessions.
/// Falls back to numbered ReadLine selection when stdin is redirected (CI, piped input).
/// </summary>
public sealed class ConsoleMenuNavigator : IMenuNavigator
{
    /// <summary>Presents a cursor-navigable list and returns the selected value.</summary>
    public T Select<T>(IReadOnlyList<MenuOption<T>> options, string? title = null)
    {
        if (options.Count == 0)
            throw new ArgumentException("Options list must not be empty.", nameof(options));

        if (Console.IsInputRedirected)
            return FallbackReadLine(options, title);

        if (title != null)
        {
            Console.WriteLine();
            Console.WriteLine(title);
        }

        int selected = 0;
        int startRow = Console.CursorTop;

        RenderOptions(options, selected, startRow);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selected > 0) { selected--; RenderOptions(options, selected, startRow); }
                    break;
                case ConsoleKey.DownArrow:
                    if (selected < options.Count - 1) { selected++; RenderOptions(options, selected, startRow); }
                    break;
                case ConsoleKey.Enter:
                    // Move cursor below the rendered menu
                    Console.SetCursorPosition(0, startRow + options.Count);
                    Console.WriteLine();
                    return options[selected].Value;
            }
        }
    }

    /// <summary>Shows a Y/N confirmation prompt and returns true when the player confirms.</summary>
    public bool Confirm(string prompt)
    {
        return Select(
            new List<MenuOption<bool>>
            {
                new("Yes", true),
                new("No",  false),
            },
            title: prompt);
    }

    private static void RenderOptions<T>(IReadOnlyList<MenuOption<T>> options, int selected, int startRow)
    {
        Console.SetCursorPosition(0, startRow);
        for (int i = 0; i < options.Count; i++)
        {
            bool isSelected = i == selected;
            string prefix   = isSelected ? $"{ColorCodes.Cyan}▶ {ColorCodes.Reset}" : "  ";
            string label    = isSelected
                ? $"{ColorCodes.BrightWhite}{options[i].Label}{ColorCodes.Reset}"
                : $"{ColorCodes.Gray}{options[i].Label}{ColorCodes.Reset}";
            string subtitle = options[i].Subtitle != null
                ? $"  {ColorCodes.Gray}{options[i].Subtitle}{ColorCodes.Reset}"
                : "";
            // Trailing spaces clear any stale characters from longer previous lines
            Console.WriteLine($"  {prefix}{label}{subtitle}                    ");
        }
    }

    private static T FallbackReadLine<T>(IReadOnlyList<MenuOption<T>> options, string? title)
    {
        if (title != null) Console.WriteLine(title);
        for (int i = 0; i < options.Count; i++)
            Console.WriteLine($"  [{i + 1}] {options[i].Label}");
        Console.Write("> ");
        var input = Console.ReadLine()?.Trim() ?? "1";
        if (int.TryParse(input, out var idx) && idx >= 1 && idx <= options.Count)
            return options[idx - 1].Value;
        return options[0].Value;
    }
}
