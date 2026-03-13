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
}
