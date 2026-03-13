using CommunityToolkit.Mvvm.ComponentModel;
using Dungnz.Display;
using Dungnz.Models;

namespace Dungnz.Display.Avalonia.ViewModels;

/// <summary>
/// View model for the map panel showing BFS-based ASCII dungeon map.
/// </summary>
public partial class MapPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _mapText = "Map will appear here";

    [ObservableProperty]
    private int _currentFloor = 1;

    /// <summary>
    /// Updates the map panel with the current room and floor.
    /// </summary>
    public void Update(Room currentRoom, int floor)
    {
        CurrentFloor = floor;
        MapText = MapRenderer.BuildPlainTextMap(currentRoom, floor);
    }
}
