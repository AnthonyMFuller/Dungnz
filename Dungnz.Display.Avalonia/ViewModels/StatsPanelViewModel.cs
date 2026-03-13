using CommunityToolkit.Mvvm.ComponentModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the stats panel showing player HP, MP, level, gear, etc.
/// </summary>
public partial class StatsPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statsText = "Stats will appear here";
}
