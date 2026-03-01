using System.Diagnostics.CodeAnalysis;
using Dungnz.Engine;
using Dungnz.Systems;

namespace Dungnz.Display;

/// <summary>
/// Arrow-key navigable menu for interactive console sessions.
/// Falls back to numbered ReadLine selection when stdin is redirected (CI, piped input).
/// </summary>
[ExcludeFromCodeCoverage]   // TTY-only class; tests inject FakeMenuNavigator
[Obsolete("Use SpectreDisplayService with SelectionPrompt instead")]
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
        bool firstRender = true;
        int maxVisible = Math.Max(3, Console.WindowHeight - 4);
        int scrollTop = 0;

        try
        {
            try { Console.CursorVisible = false; } catch { /* output may be redirected */ }

            RenderOptions(options, selected, ref firstRender, scrollTop, maxVisible);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                int optVisible = options.Count > maxVisible ? maxVisible - 2 : options.Count;
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = (selected - 1 + options.Count) % options.Count;
                        if (selected < scrollTop)
                            scrollTop = selected;
                        else if (selected == options.Count - 1)
                            scrollTop = Math.Max(0, options.Count - optVisible);
                        RenderOptions(options, selected, ref firstRender, scrollTop, maxVisible);
                        break;
                    case ConsoleKey.DownArrow:
                        selected = (selected + 1) % options.Count;
                        if (selected >= scrollTop + optVisible)
                            scrollTop = selected - optVisible + 1;
                        else if (selected == 0)
                            scrollTop = 0;
                        RenderOptions(options, selected, ref firstRender, scrollTop, maxVisible);
                        break;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return options[selected].Value;
                    case ConsoleKey.X:
                    case ConsoleKey.Escape:
                        // Escape/X are no-ops — press Enter to confirm selection.
                        break;
                }
            }
        }
        finally
        {
            try { Console.CursorVisible = true; } catch { /* output may be redirected */ }
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

    private static void RenderOptions<T>(IReadOnlyList<MenuOption<T>> options, int selected, ref bool firstRender, int scrollTop, int maxVisible)
    {
        bool needsScroll = options.Count > maxVisible;
        // When scrolling, reserve 2 rows for ↑/↓ indicators; remaining rows = option rows
        int optRowCount = needsScroll ? Math.Max(1, maxVisible - 2) : options.Count;
        int totalRows   = needsScroll ? maxVisible : options.Count;

        // Move cursor up to start of menu using ANSI relative positioning
        if (!firstRender)
            Console.Write($"\x1b[{totalRows - 1}A");
        firstRender = false;

        int row = 0;

        // Scroll-up indicator
        if (needsScroll)
        {
            Console.Write("\r\x1b[2K");
            if (scrollTop > 0)
                Console.Write($"  {ColorCodes.Gray}↑ more{ColorCodes.Reset}");
            Console.WriteLine();
            row++;
        }

        // Visible option rows
        for (int i = 0; i < optRowCount; i++)
        {
            Console.Write("\r\x1b[2K");
            int optIdx = scrollTop + i;
            if (optIdx < options.Count)
            {
                bool isSelected = optIdx == selected;
                string prefix   = isSelected ? $"{ColorCodes.Cyan}▶ {ColorCodes.Reset}" : "  ";
                string label    = isSelected
                    ? $"{ColorCodes.BrightWhite}{options[optIdx].Label}{ColorCodes.Reset}"
                    : $"{ColorCodes.Gray}{options[optIdx].Label}{ColorCodes.Reset}";
                string subtitle = options[optIdx].Subtitle != null
                    ? $"  {ColorCodes.Gray}{options[optIdx].Subtitle}{ColorCodes.Reset}"
                    : "";
                Console.Write($"  {prefix}{label}{subtitle}");
            }
            row++;
            if (row < totalRows)
                Console.WriteLine();
        }

        // Scroll-down indicator
        if (needsScroll)
        {
            Console.WriteLine();
            Console.Write("\r\x1b[2K");
            if (scrollTop + optRowCount < options.Count)
                Console.Write($"  {ColorCodes.Gray}↓ more{ColorCodes.Reset}");
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
