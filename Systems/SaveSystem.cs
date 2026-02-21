namespace Dungnz.Systems;

using System;
using System.IO;
using System.Text.Json;
using Dungnz.Models;

/// <summary>
/// Provides static methods for serialising game state to JSON save files and
/// deserialising them back, plus enumeration of existing saves. Save files are stored
/// in the user's AppData folder under <c>Dungnz/saves/</c>.
/// </summary>
public static class SaveSystem
{
    private static readonly string SaveDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Dungnz",
        "saves"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    static SaveSystem()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }
    }

    /// <summary>
    /// Serialises the current game state — including the player, all reachable rooms, and their
    /// interconnections — to a named JSON file in the save directory.
    /// </summary>
    /// <param name="state">The game state snapshot to persist.</param>
    /// <param name="saveName">The name of the save slot; used as the file name (without extension).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="saveName"/> is null or whitespace.</exception>
    public static void SaveGame(GameState state, string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            throw new ArgumentException("Save name cannot be empty", nameof(saveName));

        var roomMap = CollectRooms(state.CurrentRoom);
        var saveData = new SaveData
        {
            Player = state.Player,
            CurrentRoomId = state.CurrentRoom.Id,
            Rooms = roomMap.Values.Select(r => new RoomSaveData
            {
                Id = r.Id,
                Description = r.Description,
                ExitIds = r.Exits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id),
                Enemy = r.Enemy,
                Items = r.Items,
                IsExit = r.IsExit,
                Visited = r.Visited,
                Looted = r.Looted,
                HasShrine = r.HasShrine,
                ShrineUsed = r.ShrineUsed
            }).ToList()
        };

        var fileName = Path.Combine(SaveDirectory, $"{saveName}.json");
        var json = JsonSerializer.Serialize(saveData, JsonOptions);
        File.WriteAllText(fileName, json);
    }

    /// <summary>
    /// Deserialises the named save file back into a <see cref="GameState"/>, reconstructing
    /// the room graph with its exit links fully restored.
    /// </summary>
    /// <param name="saveName">The name of the save slot to load.</param>
    /// <returns>A <see cref="GameState"/> containing the player and their current room.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="saveName"/> is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when no save file with the given name exists.</exception>
    /// <exception cref="InvalidDataException">Thrown when the save file is corrupt or contains invalid JSON.</exception>
    public static GameState LoadGame(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            throw new ArgumentException("Save name cannot be empty", nameof(saveName));

        var fileName = Path.Combine(SaveDirectory, $"{saveName}.json");
        
        if (!File.Exists(fileName))
            throw new FileNotFoundException($"Save file '{saveName}' not found");

        try
        {
            var json = File.ReadAllText(fileName);
            var saveData = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
            
            if (saveData == null)
                throw new InvalidDataException("Save file is corrupt or empty");

            var roomDict = new Dictionary<Guid, Room>();
            foreach (var roomData in saveData.Rooms)
            {
                var room = new Room
                {
                    Id = roomData.Id,
                    Description = roomData.Description,
                    Enemy = roomData.Enemy,
                    Items = roomData.Items.ToList(),
                    IsExit = roomData.IsExit,
                    Visited = roomData.Visited,
                    Looted = roomData.Looted,
                    HasShrine = roomData.HasShrine,
                    ShrineUsed = roomData.ShrineUsed
                };
                roomDict[room.Id] = room;
            }

            foreach (var roomData in saveData.Rooms)
            {
                var room = roomDict[roomData.Id];
                foreach (var exit in roomData.ExitIds)
                {
                    if (roomDict.TryGetValue(exit.Value, out var targetRoom))
                    {
                        room.Exits[exit.Key] = targetRoom;
                    }
                }
            }

            var currentRoom = roomDict[saveData.CurrentRoomId];
            return new GameState(saveData.Player, currentRoom);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Failed to load save file '{saveName}': corrupt data", ex);
        }
    }

    /// <summary>
    /// Returns the names of all available save slots, sorted by most recently written first.
    /// Returns an empty array if the save directory does not exist.
    /// </summary>
    /// <returns>An array of save slot names (without file extensions).</returns>
    public static string[] ListSaves()
    {
        if (!Directory.Exists(SaveDirectory))
            return Array.Empty<string>();

        return Directory.GetFiles(SaveDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderByDescending(f => File.GetLastWriteTime(f!))
            .ToArray()!;
    }

    private static Dictionary<Guid, Room> CollectRooms(Room startRoom)
    {
        var visited = new Dictionary<Guid, Room>();
        var queue = new Queue<Room>();
        queue.Enqueue(startRoom);

        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            if (visited.ContainsKey(room.Id))
                continue;

            visited[room.Id] = room;

            foreach (var exit in room.Exits.Values)
            {
                if (!visited.ContainsKey(exit.Id))
                    queue.Enqueue(exit);
            }
        }

        return visited;
    }
}

/// <summary>
/// An immutable snapshot of the game's core runtime state, pairing a player with the room
/// they currently occupy. Used as the unit of data passed to and from the save system.
/// </summary>
public class GameState
{
    /// <summary>The player character, including stats, inventory, level, and experience.</summary>
    public Player Player { get; }

    /// <summary>The room the player is currently standing in when the state was captured.</summary>
    public Room CurrentRoom { get; }

    /// <summary>
    /// Creates a new game state with the given player and current room.
    /// </summary>
    /// <param name="player">The player to associate with this state.</param>
    /// <param name="currentRoom">The room the player is currently in.</param>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="player"/> or <paramref name="currentRoom"/> is <see langword="null"/>.</exception>
    public GameState(Player player, Room currentRoom)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrentRoom = currentRoom ?? throw new ArgumentNullException(nameof(currentRoom));
    }
}

internal class SaveData
{
    public required Player Player { get; init; }
    public required Guid CurrentRoomId { get; init; }
    public required List<RoomSaveData> Rooms { get; init; }
}

internal class RoomSaveData
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public required Dictionary<Direction, Guid> ExitIds { get; init; }
    public Enemy? Enemy { get; init; }
    public required List<Item> Items { get; init; }
    public required bool IsExit { get; init; }
    public required bool Visited { get; init; }
    public required bool Looted { get; init; }
    public bool HasShrine { get; init; }
    public bool ShrineUsed { get; init; }
}
