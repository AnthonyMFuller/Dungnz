using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace Dungnz.Display.Tui;

/// <summary>
/// Defines the main Terminal.Gui application layout with split-screen panels for
/// map, stats, content, message log, and command input.
/// </summary>
public sealed class TuiLayout
{
    private readonly List<string> _messageHistory = new();
    private const int MaxMessageHistory = 100;

    // Persistent TextViews for map and stats — updated in place, never recreated (#1042)
    private readonly TextView _mapView;
    private readonly TextView _statsView;

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
        // High-contrast color schemes (#1036) — null-safe for test environments
        var normalScheme = new ColorScheme
        {
            Normal   = MakeAttr(Color.White, Color.Blue),
            Focus    = MakeAttr(Color.White, Color.Blue),
            HotNormal = MakeAttr(Color.BrightYellow, Color.Blue),
            HotFocus  = MakeAttr(Color.BrightYellow, Color.Blue),
            Disabled  = MakeAttr(Color.Gray, Color.Blue)
        };

        var mapScheme = new ColorScheme
        {
            Normal   = MakeAttr(Color.BrightGreen, Color.Black),
            Focus    = MakeAttr(Color.BrightGreen, Color.Black),
            HotNormal = MakeAttr(Color.BrightYellow, Color.Black),
            HotFocus  = MakeAttr(Color.BrightYellow, Color.Black),
            Disabled  = MakeAttr(Color.Gray, Color.Black)
        };

        var statsScheme = new ColorScheme
        {
            Normal   = MakeAttr(Color.BrightCyan, Color.Black),
            Focus    = MakeAttr(Color.BrightCyan, Color.Black),
            HotNormal = MakeAttr(Color.BrightYellow, Color.Black),
            HotFocus  = MakeAttr(Color.BrightYellow, Color.Black),
            Disabled  = MakeAttr(Color.Gray, Color.Black)
        };

        var logScheme = new ColorScheme
        {
            Normal   = MakeAttr(Color.White, Color.Black),
            Focus    = MakeAttr(Color.White, Color.Black),
            HotNormal = MakeAttr(Color.BrightYellow, Color.Black),
            HotFocus  = MakeAttr(Color.BrightYellow, Color.Black),
            Disabled  = MakeAttr(Color.Gray, Color.Black)
        };

        var inputScheme = new ColorScheme
        {
            Normal   = MakeAttr(Color.BrightYellow, Color.Black),
            Focus    = MakeAttr(Color.BrightYellow, Color.Black),
            HotNormal = MakeAttr(Color.BrightYellow, Color.Black),
            HotFocus  = MakeAttr(Color.BrightYellow, Color.Black),
            Disabled  = MakeAttr(Color.Gray, Color.Black)
        };

        MainWindow = new Toplevel { ColorScheme = normalScheme };

        // Top row: Map (left 60%) and Stats (right 40%), taking 30% of height
        MapPanel = new FrameView("🗺  Dungeon Map")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(60),
            Height = Dim.Percent(30),
            ColorScheme = mapScheme
        };

        // Persistent map TextView (#1042)
        _mapView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            ColorScheme = mapScheme
        };
        MapPanel.Add(_mapView);

        StatsPanel = new FrameView("⚔  Player Stats")
        {
            X = Pos.Right(MapPanel),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(30),
            ColorScheme = statsScheme
        };

        // Persistent stats TextView (#1042)
        _statsView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            ColorScheme = statsScheme
        };
        StatsPanel.Add(_statsView);

        // Middle: Content area (50% height)
        var contentFrame = new FrameView("📜 Adventure")
        {
            X = 0,
            Y = Pos.Bottom(MapPanel),
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            ColorScheme = normalScheme
        };

        ContentPanel = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            ColorScheme = normalScheme
        };
        contentFrame.Add(ContentPanel);

        // Message log (15% height)
        var logFrame = new FrameView("📋 Message Log")
        {
            X = 0,
            Y = Pos.Bottom(contentFrame),
            Width = Dim.Fill(),
            Height = Dim.Percent(15),
            ColorScheme = logScheme
        };

        MessageLogPanel = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            ColorScheme = logScheme
        };
        logFrame.Add(MessageLogPanel);

        // Command input (bottom)
        var inputFrame = new FrameView("⌨  Command")
        {
            X = 0,
            Y = Pos.Bottom(logFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = inputScheme
        };

        CommandInput = new TextField
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            ColorScheme = inputScheme
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
        var combined = current + text;
        
        // Cap at 500 lines to prevent unbounded growth
        var lines = combined.Split('\n');
        if (lines.Length > 500)
        {
            combined = string.Join("\n", lines.Skip(lines.Length - 500));
        }
        
        ContentPanel.Text = combined;
        if (Application.Driver is not null)
            Application.Refresh();
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
    /// Appends a line to the message log panel with timestamp and color-coding.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="type">The message type for color-coding (error, combat, info, loot).</param>
    public void AppendLog(string message, string type = "info")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var prefix = type switch
        {
            "error" => "❌",
            "combat" => "⚔",
            "loot" => "💰",
            _ => "ℹ"
        };

        var logLine = $"[{timestamp}] {prefix} {message}";
        _messageHistory.Add(logLine);

        // Keep only last 100 messages
        if (_messageHistory.Count > MaxMessageHistory)
        {
            _messageHistory.RemoveAt(0);
        }

        // Update the log panel
        MessageLogPanel.Text = string.Join("\n", _messageHistory);

        // Auto-scroll to bottom
        MessageLogPanel.MoveEnd();
        if (Application.Driver is not null)
            Application.Refresh();
    }

    /// <summary>
    /// Sets the map panel content by updating the persistent TextView (#1042).
    /// </summary>
    /// <param name="mapText">The ASCII map text.</param>
    public void SetMap(string mapText)
    {
        _mapView.Text = mapText;
        if (Application.Driver is not null)
            Application.Refresh();
    }

    /// <summary>
    /// Sets the stats panel content by updating the persistent TextView (#1042).
    /// </summary>
    /// <param name="statsText">The stats text.</param>
    public void SetStats(string statsText)
    {
        _statsView.Text = statsText;
        if (Application.Driver is not null)
            Application.Refresh();
    }

    /// <summary>
    /// Creates a Terminal.Gui Attribute safely, returning a default when Application.Driver
    /// is not initialized (e.g., during unit tests).
    /// </summary>
    private static Terminal.Gui.Attribute MakeAttr(Color fg, Color bg)
    {
        return Application.Driver is not null
            ? Application.Driver.MakeAttribute(fg, bg)
            : new Terminal.Gui.Attribute();
    }
}
