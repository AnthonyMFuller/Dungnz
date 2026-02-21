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
    private static string SaveDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Dungnz",
        "saves"
    );

    internal static void OverrideSaveDirectory(string path) => SaveDirectory = path;

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
            CurrentFloor = state.CurrentFloor,
            UnlockedSkills = state.Player.Skills.UnlockedSkills.Select(s => s.ToString()).ToList(),
            Version = 1,
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
                ShrineUsed = r.ShrineUsed,
                Merchant = r.Merchant,
                Hazard = r.Hazard,
                RoomType = r.Type,
                BossState = r.Enemy is Dungnz.Systems.Enemies.DungeonBoss boss
                    ? new BossSaveState
                    {
                        IsEnraged = boss.IsEnraged,
                        IsCharging = boss.IsCharging,
                        ChargeActive = boss.ChargeActive
                    }
                    : null
            }).ToList()
        };

        var fileName = Path.Combine(SaveDirectory, $"{saveName}.json");
        Directory.CreateDirectory(SaveDirectory);
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

            // v2 → v3 migration: Version 0 means an old save without the Version field
            if (saveData.Version == 0)
            {
                Console.WriteLine("[SaveSystem] Migrating v2 save to v3: applying default difficulty 'Normal'.");
            }

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
                    ShrineUsed = roomData.ShrineUsed,
                    Merchant = roomData.Merchant,
                    Hazard = roomData.Hazard,
                    Type = roomData.RoomType
                };
                if (roomData.BossState is { } bossState &&
                    room.Enemy is Dungnz.Systems.Enemies.DungeonBoss dungeonBoss)
                {
                    dungeonBoss.IsEnraged = bossState.IsEnraged;
                    dungeonBoss.IsCharging = bossState.IsCharging;
                    dungeonBoss.ChargeActive = bossState.ChargeActive;
                }
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

            // Restore unlocked skills (bonuses already baked into saved stats)
            foreach (var skillName in saveData.UnlockedSkills)
            {
                if (Enum.TryParse<Dungnz.Systems.Skill>(skillName, out var skill))
                    saveData.Player.Skills.Unlock(skill);
            }

            return new GameState(saveData.Player, currentRoom, saveData.CurrentFloor);
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
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .Select(Path.GetFileNameWithoutExtension)
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

    /// <summary>The dungeon floor the player is currently on.</summary>
    public int CurrentFloor { get; }

    /// <summary>
    /// Creates a new game state with the given player, current room, and floor number.
    /// </summary>
    /// <param name="player">The player to associate with this state.</param>
    /// <param name="currentRoom">The room the player is currently in.</param>
    /// <param name="currentFloor">The floor number the player is currently on. Defaults to 1.</param>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="player"/> or <paramref name="currentRoom"/> is <see langword="null"/>.</exception>
    public GameState(Player player, Room currentRoom, int currentFloor = 1)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrentRoom = currentRoom ?? throw new ArgumentNullException(nameof(currentRoom));
        CurrentFloor = currentFloor;
    }
}

internal class SaveData
{
    public required Player Player { get; init; }
    public required Guid CurrentRoomId { get; init; }
    public required List<RoomSaveData> Rooms { get; init; }

    /// <summary>The floor number the player was on when the game was saved.</summary>
    public int CurrentFloor { get; init; } = 1;

    /// <summary>Names of skills the player had unlocked at save time.</summary>
    public List<string> UnlockedSkills { get; init; } = new();

    /// <summary>Save format version. 0 = pre-v3 legacy save; 1 = v3+.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public int Version { get; init; }
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

    /// <summary>The merchant present in this room, or <see langword="null"/> if none.</summary>
    public Merchant? Merchant { get; init; }

    /// <summary>The environmental hazard in this room.</summary>
    public HazardType Hazard { get; init; }

    /// <summary>The environmental flavour type of this room.</summary>
    public RoomType RoomType { get; init; }

    /// <summary>
    /// Persisted combat flags for a <see cref="Dungnz.Systems.Enemies.DungeonBoss"/>.
    /// <see langword="null"/> when the room's enemy is not a boss.
    /// </summary>
    public BossSaveState? BossState { get; init; }
}

/// <summary>
/// Snapshot of the three runtime combat flags that control <see cref="Dungnz.Systems.Enemies.DungeonBoss"/>
/// behaviour: enrage phase, charge wind-up, and charge release. Serialised alongside the room so
/// that a mid-boss save/load cycle restores the fight to the exact same state.
/// </summary>
internal class BossSaveState
{
    /// <summary>Whether the boss has entered its enraged phase (HP ≤ 40 %).</summary>
    public bool IsEnraged { get; init; }

    /// <summary>Whether the boss is currently winding up a charge attack.</summary>
    public bool IsCharging { get; init; }

    /// <summary>Whether the boss's charged attack fires this turn.</summary>
    public bool ChargeActive { get; init; }
}
