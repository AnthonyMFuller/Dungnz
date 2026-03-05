using Spectre.Console;
using Spectre.Console.Rendering;

namespace Dungnz.Display.Spectre;

/// <summary>
/// Defines the 5-panel Spectre.Console Live+Layout structure for the game UI.
/// Layout ratios: Top row 30% (Map 60% | Stats 40%), Content 50%, Bottom row 20% (Log 70% | Input 30%).
/// </summary>
public static class SpectreLayout
{
    /// <summary>Panel name constants for layout updates.</summary>
    public static class Panels
    {
        /// <summary>Top-left map panel (60% of top row, 30% height).</summary>
        public const string Map = "Map";
        /// <summary>Top-right stats panel (40% of top row, 30% height).</summary>
        public const string Stats = "Stats";
        /// <summary>Middle content/narration panel (50% height).</summary>
        public const string Content = "Content";
        /// <summary>Bottom-left log panel (70% of bottom row, 20% height).</summary>
        public const string Log = "Log";
        /// <summary>Bottom-right input area (30% of bottom row, 20% height).</summary>
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
                // Top row: 30% height — Map (60%) | Stats (40%)
                new Layout("TopRow")
                    .Ratio(3)  // 30% of 10 units
                    .SplitColumns(
                        new Layout(Panels.Map).Ratio(6),   // 60% width
                        new Layout(Panels.Stats).Ratio(4)  // 40% width
                    ),
                // Middle row: 50% height — Content panel
                new Layout(Panels.Content).Ratio(5),  // 50% of 10 units
                // Bottom row: 20% height — Log (70%) | Input (30%)
                new Layout("BottomRow")
                    .Ratio(2)  // 20% of 10 units
                    .SplitColumns(
                        new Layout(Panels.Log).Ratio(7),   // 70% width
                        new Layout(Panels.Input).Ratio(3)  // 30% width
                    )
            );

        // Initialize each panel with placeholder content
        root[Panels.Map].Update(
            new Panel(new Text(""))
                .Header("[bold green]🗺  Dungeon Map[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
        );

        root[Panels.Stats].Update(
            new Panel(new Text(""))
                .Header("[bold cyan]⚔  Player Stats[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
        );

        root[Panels.Content].Update(
            new Panel(new Text(""))
                .Header("[bold white]📜 Adventure[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
        );

        root[Panels.Log].Update(
            new Panel(new Text(""))
                .Header("[bold grey]📋 Message Log[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
        );

        root[Panels.Input].Update(
            new Panel(new Text("[grey]Type commands here...[/]"))
                .Header("[bold yellow]⌨  Command[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow)
        );

        return root;
    }
}
