using CommunityToolkit.Mvvm.ComponentModel;
using Dungnz.Systems;
using System.Collections.ObjectModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the log panel showing timestamped game events.
/// </summary>
public partial class LogPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _logLines = new();

    private readonly List<string> _logHistory = new();
    private const int MaxLogHistory = 50;
    private const int MaxDisplayedLog = 12;

    /// <summary>
    /// Appends a log entry with timestamp, type icon, and ANSI color classification.
    /// </summary>
    public void AppendLog(string message, string type = "info")
    {
        var timestamp = DateTime.Now.ToString("HH:mm");
        string icon;
        string color;

        if (type == "combat")
        {
            icon = ClassifyCombatLogIcon(message);
            color = ClassifyCombatLogColor(message);
        }
        else
        {
            (icon, color) = type switch
            {
                "error" => ("❌", ColorCodes.BrightRed),
                "loot"  => ("💰", ColorCodes.Yellow),
                _       => ("ℹ", "")
            };
        }

        var logLine = string.IsNullOrEmpty(color)
            ? $"{timestamp} {icon} {message}"
            : $"{timestamp} {icon} {color}{message}{ColorCodes.Reset}";

        _logHistory.Add(logLine);
        if (_logHistory.Count > MaxLogHistory)
            _logHistory.RemoveAt(0);

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        LogLines.Clear();
        foreach (var line in _logHistory.TakeLast(MaxDisplayedLog))
        {
            LogLines.Add(line);
        }
    }

    private static string ClassifyCombatLogIcon(string message) =>
        message.Contains("Critical", StringComparison.OrdinalIgnoreCase) ? "💥" :
        message.Contains("Healed", StringComparison.OrdinalIgnoreCase) ? "💚" :
        message.Contains("Poison", StringComparison.OrdinalIgnoreCase) ? "☠" :
        message.Contains("Burn", StringComparison.OrdinalIgnoreCase) ? "🔥" :
        "⚔";

    /// <summary>
    /// Returns the ANSI color code appropriate for a combat log message based on
    /// keyword classification (critical hits, healing, status effects).
    /// </summary>
    private static string ClassifyCombatLogColor(string message) =>
        message.Contains("Critical", StringComparison.OrdinalIgnoreCase) ? ColorCodes.BrightRed :
        message.Contains("Healed", StringComparison.OrdinalIgnoreCase) ? ColorCodes.Green :
        message.Contains("Poison", StringComparison.OrdinalIgnoreCase) ? ColorCodes.Magenta :
        message.Contains("Burn", StringComparison.OrdinalIgnoreCase) ? ColorCodes.Yellow :
        ColorCodes.Red;
}
