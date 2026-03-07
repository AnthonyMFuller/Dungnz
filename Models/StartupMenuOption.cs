namespace Dungnz.Models;

/// <summary>
/// Represents the player's choice at the main startup menu.
/// </summary>
public enum StartupMenuOption
{
    /// <summary>Start a new game with random seed.</summary>
    NewGame,
    
    /// <summary>Load an existing save file.</summary>
    LoadSave,
    
    /// <summary>Start a new game with a specific seed.</summary>
    NewGameWithSeed,
    
    /// <summary>Exit the application.</summary>
    Exit
}
