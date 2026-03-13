using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the main content panel displaying room descriptions, combat, menus, etc.
/// </summary>
public partial class ContentPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _contentLines = new();

    [ObservableProperty]
    private string _headerText = "Adventure";

    // TODO: P3-P8 implementation
    public void AppendMessage(string message)
    {
        ContentLines.Add(message);
    }
}
