using Terminal.Gui;

namespace Dungnz.Display.Tui;

/// <summary>
/// Defines the main Terminal.Gui application layout with split-screen panels for
/// map, stats, content, message log, and command input.
/// </summary>
public sealed class TuiLayout
{
    /// <summary>Gets the main application window that hosts all panels.</summary>
    public Toplevel MainWindow { get; }

    /// <summary>Gets the map display panel (top-left, showing ASCII dungeon map).</summary>
    public FrameView MapPanel { get; }

    /// <summary>Gets the player stats panel (top-right, showing HP, MP, level, gold, equipment).</summary>
    public FrameView StatsPanel { get; }

    /// <summary>Gets the main content panel (middle, for room descriptions, combat text, menus).</summary>
    public TextView ContentPanel { get; }

    /// <summary>Gets the scrollable message log panel (above command input).</summary>
    public TextView MessageLogPanel { get; }

    /// <summary>Gets the command input text field (bottom).</summary>
    public TextField CommandInput { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TuiLayout"/> class with the
    /// split-screen layout: map (top-left 60%), stats (top-right 40%), content (middle 50%),
    /// message log (bottom 15%), and command input (bottom 5%).
    /// </summary>
    public TuiLayout()
    {
        MainWindow = new Toplevel();

        // Top row: Map (left 60%) and Stats (right 40%), taking 30% of height
        MapPanel = new FrameView("🗺  Dungeon Map")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(60),
            Height = Dim.Percent(30)
        };

        StatsPanel = new FrameView("⚔  Player Stats")
        {
            X = Pos.Right(MapPanel),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(30)
        };

        // Middle: Content area (50% height)
        var contentFrame = new FrameView("📜 Adventure")
        {
            X = 0,
            Y = Pos.Bottom(MapPanel),
            Width = Dim.Fill(),
            Height = Dim.Percent(50)
        };

        ContentPanel = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        contentFrame.Add(ContentPanel);

        // Message log (20% height)
        var logFrame = new FrameView("📋 Message Log")
        {
            X = 0,
            Y = Pos.Bottom(contentFrame),
            Width = Dim.Fill(),
            Height = Dim.Percent(15)
        };

        MessageLogPanel = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        logFrame.Add(MessageLogPanel);

        // Command input (5% height, bottom)
        var inputFrame = new FrameView("⌨  Command")
        {
            X = 0,
            Y = Pos.Bottom(logFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        CommandInput = new TextField
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };
        inputFrame.Add(CommandInput);

        // Add all panels to main window
        MainWindow.Add(MapPanel, StatsPanel, contentFrame, logFrame, inputFrame);
    }

    /// <summary>
    /// Appends text to the content panel.
    /// </summary>
    /// <param name="text">The text to append.</param>
    public void AppendContent(string text)
    {
        var current = ContentPanel.Text.ToString() ?? string.Empty;
        ContentPanel.Text = current + text;
    }

    /// <summary>
    /// Sets the content panel text, replacing previous content.
    /// </summary>
    /// <param name="text">The new text.</param>
    public void SetContent(string text)
    {
        ContentPanel.Text = text;
    }

    /// <summary>
    /// Appends a line to the message log panel.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void AppendLog(string message)
    {
        var current = MessageLogPanel.Text.ToString() ?? string.Empty;
        MessageLogPanel.Text = current + message + "\n";
    }

    /// <summary>
    /// Sets the map panel content.
    /// </summary>
    /// <param name="mapText">The ASCII map text.</param>
    public void SetMap(string mapText)
    {
        MapPanel.RemoveAll();
        var mapView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = mapText
        };
        MapPanel.Add(mapView);
    }

    /// <summary>
    /// Sets the stats panel content.
    /// </summary>
    /// <param name="statsText">The stats text.</param>
    public void SetStats(string statsText)
    {
        StatsPanel.RemoveAll();
        var statsView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = statsText
        };
        StatsPanel.Add(statsView);
    }
}
