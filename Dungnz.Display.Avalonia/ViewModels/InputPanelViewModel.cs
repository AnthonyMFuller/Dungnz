using CommunityToolkit.Mvvm.ComponentModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the input panel showing command prompt and accepting user commands.
/// </summary>
public partial class InputPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _commandText = "";

    [ObservableProperty]
    private string _promptText = ">";
}
