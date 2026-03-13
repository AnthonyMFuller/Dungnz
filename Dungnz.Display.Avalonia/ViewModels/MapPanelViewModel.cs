using CommunityToolkit.Mvvm.ComponentModel;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the map panel showing BFS-based ASCII dungeon map.
/// </summary>
public partial class MapPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _mapText = "Map will appear here";
}
