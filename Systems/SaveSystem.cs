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

        // Sanitize: reject names containing path separators or other invalid filename chars
        var invalid = Path.GetInvalidFileNameChars();
        if (saveName.Contains('/') || saveName.Contains('\\') || saveName.Contains("..") ||
            saveName.IndexOfAny(invalid) >= 0)
            throw new ArgumentException($"Save name '{saveName}' contains invalid characters.", nameof(saveName));

        var roomMap = CollectRooms(state.CurrentRoom);
        var saveData = new SaveData
        {
            Player = state.Player,
            CurrentRoomId = state.CurrentRoom.Id,
            CurrentFloor = state.CurrentFloor,
            Seed = state.Seed,
            Difficulty = state.Difficulty,
            UnlockedSkills = state.Player.Skills.UnlockedSkills.Select(s => s.ToString()).ToList(),
            StatusEffects = state.Player.ActiveEffects.ToList(),
            Version = SaveData.CurrentVersion,
            Rooms = roomMap.Values.Select(ToRoomSaveData).ToList(),
            FloorEntranceRoomId = state.FloorEntranceRoom?.Id
        };

        if (state.FloorHistory.Count > 0)
        {
            saveData.FloorHistoryRooms = new();
            saveData.FloorHistoryEntranceIds = new();
            foreach (var (floor, entranceRoom) in state.FloorHistory)
            {
                var floorRooms = CollectRooms(entranceRoom);
                saveData.FloorHistoryRooms[floor] = floorRooms.Values.Select(ToRoomSaveData).ToList();
                saveData.FloorHistoryEntranceIds[floor] = entranceRoom.Id;
            }
        }

        Directory.CreateDirectory(SaveDirectory);
        var finalPath = Path.Combine(SaveDirectory, $"{saveName}.json");
        var tmpPath   = finalPath + ".tmp";
        var json = JsonSerializer.Serialize(saveData, JsonOptions);
        try
        {
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, finalPath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tmpPath); } catch { /* best effort */ }
            throw;
        }
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

            if (saveData.Player == null)
                throw new InvalidDataException("Save file is missing player data");

            if (saveData.Rooms == null || saveData.Rooms.Count == 0)
                throw new InvalidDataException("Save file is missing room data");

            // Apply migrations to bring old saves up to current version
            saveData = MigrateToLatest(saveData);

            var roomDict = new Dictionary<Guid, Room>();
            foreach (var roomData in saveData.Rooms)
            {
                if (roomData == null)
                    throw new InvalidDataException("Save file contains a null room entry");

                var room = new Room
                {
                    Id = roomData.Id,
                    Description = roomData.Description ?? string.Empty,
                    Enemy = roomData.Enemy,
                    IsExit = roomData.IsExit,
                    IsEntrance = roomData.IsEntrance,
                    Visited = roomData.Visited,
                    Looted = roomData.Looted,
                    HasShrine = roomData.HasShrine,
                    ShrineUsed = roomData.ShrineUsed,
                    Merchant = roomData.Merchant,
                    Hazard = roomData.Hazard,
                    Type = roomData.RoomType,
                    SpecialRoomUsed = roomData.SpecialRoomUsed,
                    BlessedHealApplied = roomData.BlessedHealApplied,
                    EnvironmentalHazard = roomData.EnvironmentalHazard,
                    Trap = roomData.Trap,
                    State = roomData.State
                };
                foreach (var item in roomData.Items ?? new List<Item>())
                    room.AddItem(item);
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
                if (roomData.ExitIds == null) continue;
                var room = roomDict[roomData.Id];
                foreach (var exit in roomData.ExitIds)
                {
                    if (roomDict.TryGetValue(exit.Value, out var targetRoom))
                    {
                        room.Exits[exit.Key] = targetRoom;
                    }
                }
            }

            if (!roomDict.TryGetValue(saveData.CurrentRoomId, out var currentRoom))
                throw new InvalidDataException($"Save file references unknown current room {saveData.CurrentRoomId}");

            // Restore unlocked skills (bonuses already baked into saved stats)
            if (saveData.UnlockedSkills != null)
            {
                foreach (var skillName in saveData.UnlockedSkills)
                {
                    if (Enum.TryParse<Dungnz.Models.Skill>(skillName, out var skill))
                        saveData.Player.Skills.Unlock(skill);
                }
            }

            // Restore active status effects onto the player
            saveData.Player.ActiveEffects.Clear();
            if (saveData.StatusEffects != null)
                saveData.Player.ActiveEffects.AddRange(saveData.StatusEffects);

            return new GameState(saveData.Player, currentRoom, saveData.CurrentFloor, saveData.Seed, saveData.Difficulty,
                floorHistory: RestoreFloorHistory(saveData),
                floorEntranceRoom: saveData.FloorEntranceRoomId.HasValue && roomDict.TryGetValue(saveData.FloorEntranceRoomId.Value, out var entrRoom) ? entrRoom : null);
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

    private static RoomSaveData ToRoomSaveData(Room r) => new()
    {
        Id = r.Id,
        Description = r.Description,
        ExitIds = r.Exits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id),
        Enemy = r.Enemy,
        Items = r.Items.ToList(),
        IsExit = r.IsExit,
        IsEntrance = r.IsEntrance,
        Visited = r.Visited,
        Looted = r.Looted,
        HasShrine = r.HasShrine,
        ShrineUsed = r.ShrineUsed,
        Merchant = r.Merchant,
        Hazard = r.Hazard,
        RoomType = r.Type,
        SpecialRoomUsed = r.SpecialRoomUsed,
        BlessedHealApplied = r.BlessedHealApplied,
        EnvironmentalHazard = r.EnvironmentalHazard,
        Trap = r.Trap,
        State = r.State,
        BossState = r.Enemy is Dungnz.Systems.Enemies.DungeonBoss boss
            ? new BossSaveState
            {
                IsEnraged = boss.IsEnraged,
                IsCharging = boss.IsCharging,
                ChargeActive = boss.ChargeActive
            }
            : null
    };

    private static Dictionary<int, Room> RestoreFloorHistory(SaveData data)
    {
        var floorHistory = new Dictionary<int, Room>();
        if (data.FloorHistoryRooms == null || data.FloorHistoryEntranceIds == null)
            return floorHistory;

        foreach (var (floor, roomList) in data.FloorHistoryRooms)
        {
            var floorRoomDict = new Dictionary<Guid, Room>();
            foreach (var roomData in roomList)
            {
                var room = new Room
                {
                    Id = roomData.Id,
                    Description = roomData.Description ?? string.Empty,
                    Enemy = roomData.Enemy,
                    IsExit = roomData.IsExit,
                    IsEntrance = roomData.IsEntrance,
                    Visited = roomData.Visited,
                    Looted = roomData.Looted,
                    HasShrine = roomData.HasShrine,
                    ShrineUsed = roomData.ShrineUsed,
                    Merchant = roomData.Merchant,
                    Hazard = roomData.Hazard,
                    Type = roomData.RoomType,
                    SpecialRoomUsed = roomData.SpecialRoomUsed,
                    BlessedHealApplied = roomData.BlessedHealApplied,
                    EnvironmentalHazard = roomData.EnvironmentalHazard,
                    Trap = roomData.Trap,
                    State = roomData.State
                };
                foreach (var item in roomData.Items ?? new List<Item>())
                    room.AddItem(item);
                floorRoomDict[room.Id] = room;
            }
            foreach (var roomData in roomList)
            {
                if (roomData.ExitIds == null) continue;
                var room = floorRoomDict[roomData.Id];
                foreach (var exit in roomData.ExitIds)
                    if (floorRoomDict.TryGetValue(exit.Value, out var targetRoom))
                        room.Exits[exit.Key] = targetRoom;
            }
            if (data.FloorHistoryEntranceIds.TryGetValue(floor, out var entranceId) &&
                floorRoomDict.TryGetValue(entranceId, out var entranceRoom))
                floorHistory[floor] = entranceRoom;
        }
        return floorHistory;
    }

    /// <summary>
    /// Recursively migrates a SaveData object from any old version to the latest version.
    /// Throws <see cref="InvalidDataException"/> if the save version is unknown or unsupported.
    /// </summary>
    private static SaveData MigrateToLatest(SaveData data)
    {
        return data.Version switch
        {
            SaveData.CurrentVersion => data,
            1 => MigrateToLatest(MigrateV1ToV2(data)),
            0 => MigrateToLatest(MigrateV0ToV1(data)),
            _ => throw new InvalidDataException($"Unknown save version {data.Version}. Expected {SaveData.CurrentVersion} or earlier.")
        };
    }

    /// <summary>
    /// Migrates a Version 0 (legacy) save to Version 1.
    /// Currently a no-op because V0 and V1 are compatible, but this establishes
    /// the pattern for when we add Version 2.
    /// </summary>
    private static SaveData MigrateV0ToV1(SaveData data)
    {
        // V0 -> V1: No structural changes.
        // CurrentFloor defaults to 1 (already the field default).
        // UnlockedSkills defaults to empty (already the field default).
        // StatusEffects defaults to empty (already the field default).
        data.Version = 1;
        return data;
    }

    private static SaveData MigrateV1ToV2(SaveData data)
    {
        // V1 -> V2: Added FloorHistoryRooms, FloorHistoryEntranceIds, FloorEntranceRoomId (nullable, default null = empty).
        data.Version = 2;
        return data;
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

    /// <summary>The seed used to generate this dungeon, or null if no seed was specified.</summary>
    public int? Seed { get; }

    /// <summary>The difficulty level chosen when this run began.</summary>
    public Difficulty Difficulty { get; }

    /// <summary>Maps floor number → entrance room of that floor, enabling ascension back to previous floors.</summary>
    public Dictionary<int, Room> FloorHistory { get; }

    /// <summary>The entrance room of the current floor. Used to restore FloorEntranceRoom on load.</summary>
    public Room? FloorEntranceRoom { get; }

    /// <summary>
    /// Creates a new game state with the given player, current room, and floor number.
    /// </summary>
    /// <param name="player">The player to associate with this state.</param>
    /// <param name="currentRoom">The room the player is currently in.</param>
    /// <param name="currentFloor">The floor number the player is currently on. Defaults to 1.</param>
    /// <param name="seed">The seed used to generate the dungeon. Defaults to null.</param>
    /// <param name="difficulty">The difficulty level for this run. Defaults to Normal.</param>
    /// <param name="floorHistory">The floor history mapping floor numbers to entrance rooms.</param>
    /// <param name="floorEntranceRoom">The entrance room of the current floor.</param>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="player"/> or <paramref name="currentRoom"/> is <see langword="null"/>.</exception>
    public GameState(Player player, Room currentRoom, int currentFloor = 1, int? seed = null, Difficulty difficulty = Difficulty.Normal, Dictionary<int, Room>? floorHistory = null, Room? floorEntranceRoom = null)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrentRoom = currentRoom ?? throw new ArgumentNullException(nameof(currentRoom));
        CurrentFloor = currentFloor;
        Seed = seed;
        Difficulty = difficulty;
        FloorHistory = floorHistory ?? new();
        FloorEntranceRoom = floorEntranceRoom;
    }
}

internal class SaveData
{
    /// <summary>Current save format version.</summary>
    public const int CurrentVersion = 2;

    public required Player Player { get; init; }
    public required Guid CurrentRoomId { get; init; }
    public required List<RoomSaveData> Rooms { get; init; }

    /// <summary>The floor number the player was on when the game was saved.</summary>
    public int CurrentFloor { get; init; } = 1;

    /// <summary>Names of skills the player had unlocked at save time.</summary>
    public List<string> UnlockedSkills { get; init; } = new();

    /// <summary>Active status effects on the player at save time, restored on load.</summary>
    public List<ActiveEffect> StatusEffects { get; init; } = new();

    /// <summary>The seed used to generate the dungeon, or null if no seed was specified.</summary>
    public int? Seed { get; init; }

    /// <summary>The difficulty level selected when this run was started.</summary>
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;

    /// <summary>Floor history: maps floor number → list of rooms for that floor (entrance-reachable graph).</summary>
    public Dictionary<int, List<RoomSaveData>>? FloorHistoryRooms { get; set; }

    /// <summary>Floor history: maps floor number → entrance room ID for that floor.</summary>
    public Dictionary<int, Guid>? FloorHistoryEntranceIds { get; set; }

    /// <summary>The entrance room ID of the current floor, or null if not tracked.</summary>
    public Guid? FloorEntranceRoomId { get; set; }

    /// <summary>Save format version. 0 = pre-v3 legacy save; 1 = v3+; 2 = floor ascension.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public int Version { get; set; }
}

internal class RoomSaveData
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public required Dictionary<Direction, Guid> ExitIds { get; init; }
    public Enemy? Enemy { get; init; }
    public required List<Item> Items { get; init; }
    public required bool IsExit { get; init; }
    public bool IsEntrance { get; init; }
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

    /// <summary>Whether the special room effect has already been used (once-per-run reward).</summary>
    public bool SpecialRoomUsed { get; init; }

    /// <summary>Whether the BlessedClearing one-time heal has already been applied.</summary>
    public bool BlessedHealApplied { get; init; }

    /// <summary>The environmental room hazard (e.g. LavaSeam, CorruptedGround).</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public RoomHazard EnvironmentalHazard { get; init; }

    /// <summary>The trap variant for TrapRoom rooms, or null if none.</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TrapVariant? Trap { get; init; }

    /// <summary>The narrative state of the room (Fresh, Cleared, Revisited).</summary>
    public RoomState State { get; init; } = RoomState.Fresh;

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
