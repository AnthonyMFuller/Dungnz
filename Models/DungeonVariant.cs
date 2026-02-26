namespace Dungnz.Models;

/// <summary>
/// Describes the themed name and narrative messages associated with a specific dungeon floor.
/// </summary>
public class DungeonVariant
{
    /// <summary>Gets the display name of this dungeon variant (e.g. "Goblin Caves").</summary>
    public string Name { get; init; } = "Dungeon";

    /// <summary>Gets the flavour text shown to the player when they descend into this floor.</summary>
    public string EntryMessage { get; init; } = "";

    /// <summary>Gets the flavour text shown to the player when they leave this floor.</summary>
    public string ExitMessage { get; init; } = "";

    /// <summary>
    /// Returns the <see cref="DungeonVariant"/> appropriate for the given floor number,
    /// cycling through themed variants for floors 1–5 and falling back to a generic variant beyond that.
    /// </summary>
    /// <param name="floor">The 1-based dungeon floor number.</param>
    /// <returns>A <see cref="DungeonVariant"/> with matching name and entry/exit messages.</returns>
    public static DungeonVariant ForFloor(int floor) => floor switch {
        1 => new() { Name = "Goblin Caves",
            EntryMessage = "You descend into damp, torch-lit goblin caves.",
            ExitMessage = "You emerge from the goblin warrens." },
        2 => new() { Name = "Skeleton Catacombs",
            EntryMessage = "Ancient bones line the walls of the undead catacombs.",
            ExitMessage = "You leave the echoing halls of the dead." },
        3 => new() { Name = "Troll Warrens",
            EntryMessage = "The smell of rot fills these troll-infested tunnels.",
            ExitMessage = "You escape the foul troll warrens." },
        4 => new() { Name = "Shadow Realm",
            EntryMessage = "Reality warps in the shadow-touched corridors.",
            ExitMessage = "You tear free from the shadow realm." },
        5 => new() { Name = "Dragon's Lair",
            EntryMessage = "Scorched stone and treasure glints in this fearsome lair.",
            ExitMessage = "You conquered the Dragon's Lair!" },
        6 => new() { Name = "Void Antechamber",
            EntryMessage = "Reality frays at the edges here. Something watches from the cracks.",
            ExitMessage = "You tear free from the void." },
        7 => new() { Name = "Bone Palace",
            EntryMessage = "The palace of a dead king built from the bones of his enemies. You are in his house.",
            ExitMessage = "You leave the palace of the dead behind." },
        8 => new() { Name = "Final Sanctum",
            EntryMessage = "This is the end. The dungeon's heart. The oldest darkness.",
            ExitMessage = "" },
        _ => new() { Name = $"Floor {floor}", EntryMessage = "You descend into unknown depths.", ExitMessage = "" }
    };
}
