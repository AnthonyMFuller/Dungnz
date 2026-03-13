using CommunityToolkit.Mvvm.ComponentModel;
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
    /// Appends a log entry with timestamp and type classification.
    /// </summary>
    public void AppendLog(string message, string type = "info")
    {
        var timestamp = DateTime.Now.ToString("HH:mm");
        string icon;

        if (type == "combat")
        {
            icon = ClassifyCombatLogIcon(message);
        }
        else
        {
            icon = type switch
            {
                "error" => "❌",
                "loot"  => "💰",
                _       => "ℹ"
            };
        }

        _logHistory.Add($"{timestamp} {icon} {message}");
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
}
