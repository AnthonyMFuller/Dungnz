using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Dungnz.Display.Spectre;

/// <summary>
/// Defines the 6-panel Spectre.Console Live+Layout structure for the game UI.
/// Layout ratios: Top row 20% (Map 60% | Stats 40%), Middle row 50% (Content 70% | Gear 30%),
/// Bottom row 30% vertical stack (Log 70% / Command 30%).
/// </summary>
[ExcludeFromCodeCoverage]
public static class SpectreLayout
{
    /// <summary>Panel name constants for layout updates.</summary>
    public static class Panels
    {
        /// <summary>Top-left map panel (60% of top row, 20% height).</summary>
        public const string Map = "Map";
        /// <summary>Top-right stats panel (40% of top row, 20% height).</summary>
        public const string Stats = "Stats";
        /// <summary>Middle-left content/narration panel (70% of middle row, 50% height).</summary>
        public const string Content = "Content";
        /// <summary>Middle-right gear panel (30% of middle row, 50% height).</summary>
        public const string Gear = "Gear";
        /// <summary>Bottom log panel (70% of bottom row height, 30% total height).</summary>
        public const string Log = "Log";
        /// <summary>Bottom command input panel (30% of bottom row height, 30% total height).</summary>
        public const string Input = "Input";
    }

    /// <summary>
    /// Creates the 5-panel layout tree with correct sizing ratios.
    /// </summary>
    /// <returns>A configured <see cref="Layout"/> ready for Live rendering.</returns>
    public static Layout Create()
    {
        // Root layout — vertical split into three rows
        var root = new Layout("Root")
            .SplitRows(
                // Top row: 20% height — Map (60%) | Stats (40%)
                new Layout("TopRow")
                    .Ratio(2)
                    .SplitColumns(
                        new Layout(Panels.Map).Ratio(6),   // 60% width
                        new Layout(Panels.Stats).Ratio(4)  // 40% width
                    ),
                // Middle row: 50% height — Content (70%) | Gear (30%)
                new Layout("MiddleRow")
                    .Ratio(5)
                    .SplitColumns(
                        new Layout(Panels.Content).Ratio(7),  // 70% width
                        new Layout(Panels.Gear).Ratio(3)      // 30% width
                    ),
                // Bottom row: 30% height — vertical stack: Log (70%) / Command (30%)
                new Layout("BottomRow")
                    .Ratio(3)
                    .SplitRows(
                        new Layout(Panels.Log).Ratio(7),   // 70% of bottom height
                        new Layout(Panels.Input).Ratio(3)  // 30% of bottom height
                    )
            );

        // Initialize each panel with placeholder content
        root[Panels.Map].Update(
            new Panel(new Text(""))
                .Header("[bold green]Dungeon Map[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
        );

        root[Panels.Stats].Update(
            new Panel(new Text(""))
                .Header("[bold cyan]Player Stats[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
        );

        root[Panels.Content].Update(
            new Panel(new Text(""))
                .Header("[bold white]Adventure[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
        );

        root[Panels.Gear].Update(
            new Panel(new Text(""))
                .Header("[bold yellow]⚔  Gear[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Gold1)
        );

        root[Panels.Log].Update(
            new Panel(new Text(""))
                .Header("[bold grey]Message Log[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
        );

        root[Panels.Input].Update(
            new Panel(new Text("[grey]Type commands here...[/]"))
                .Header("[bold yellow]Command[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow)
        );

        return root;
    }
}
