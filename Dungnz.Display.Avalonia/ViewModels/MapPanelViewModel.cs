using CommunityToolkit.Mvvm.ComponentModel;
using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;

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
        var plainMap = MapRenderer.BuildPlainTextMap(currentRoom, floor);
        MapText = ColorizeMap(plainMap);
    }

    /// <summary>
    /// Applies ANSI color codes to map markers for rendering in AnsiTextBlock.
    /// </summary>
    private static string ColorizeMap(string plainMap)
    {
        var result = plainMap.Replace("[X]", $"{ColorCodes.BrightWhite}[X]{ColorCodes.Reset}");
        result = result.Replace("[?]", $"{ColorCodes.Gray}[?]{ColorCodes.Reset}");
        result = result.Replace("[E]", $"{ColorCodes.Green}[E]{ColorCodes.Reset}");
        return result;
    }
}
