namespace Dungnz.Systems;

using System;
using System.IO;
using System.Text.Json;
using Dungnz.Models;

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
                Looted = r.Looted
            }).ToList()
        };

        var fileName = Path.Combine(SaveDirectory, $"{saveName}.json");
        var json = JsonSerializer.Serialize(saveData, JsonOptions);
        File.WriteAllText(fileName, json);
    }

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
                    Looted = roomData.Looted
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

    public static string[] ListSaves()
    {
        if (!Directory.Exists(SaveDirectory))
            return Array.Empty<string>();

        return Directory.GetFiles(SaveDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderByDescending(File.GetLastWriteTime)
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

public class GameState
{
    public Player Player { get; }
    public Room CurrentRoom { get; }

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
}
