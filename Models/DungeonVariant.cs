namespace Dungnz.Models;

public class DungeonVariant
{
    public string Name { get; init; } = "Dungeon";
    public string EntryMessage { get; init; } = "";
    public string ExitMessage { get; init; } = "";

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
        _ => new() { Name = $"Floor {floor}", EntryMessage = "You descend into unknown depths.", ExitMessage = "" }
    };
}
