using CommunityToolkit.Mvvm.ComponentModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the gear/equipment panel.
/// </summary>
public partial class GearPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _gearText = "Gear will appear here";
}
