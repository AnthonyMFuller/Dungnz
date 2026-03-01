namespace Dungnz.Engine;

using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Procedurally generates a fully connected grid-based dungeon floor by creating a
/// rectangular room grid, wiring bidirectional exits between adjacent cells, placing
/// enemies (scaled to player level and floor depth), scattering loot items, and
/// optionally seeding healing shrines — all driven by a seeded or random RNG so runs
/// can be reproduced exactly.
/// </summary>
public class DungeonGenerator
{
    private readonly Random _rng;
    private readonly IReadOnlyList<Item> _allItems = Array.Empty<Item>();

    /// <summary>
    /// Initialises a new <see cref="DungeonGenerator"/> with an optional fixed seed.
    /// Using the same seed value produces an identical dungeon layout every time.
    /// </summary>
    /// <param name="seed">
    /// A seed value for the internal <see cref="Random"/> instance. When
    /// <see langword="null"/>, a non-deterministic seed is used.
    /// </param>
    /// <param name="allItems">
    /// All available items used to populate merchant stock. When <see langword="null"/>,
    /// merchants fall back to a hardcoded default set.
    /// </param>
    public DungeonGenerator(int? seed, IReadOnlyList<Item> allItems)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        _allItems = allItems;
    }

    /// <summary>
    /// Generates a <paramref name="width"/> × <paramref name="height"/> grid of
    /// interconnected rooms with bidirectional exits, then populates it with
    /// level-scaled enemies (~60 % occupancy), random loot items (~30 % per room),
    /// healing shrines (~15 % in non-terminal rooms), and a mandatory boss enemy in
    /// the bottom-right exit room. Returns the top-left starting room and the
    /// bottom-right exit room.
    /// </summary>
    /// <param name="width">Number of rooms along the horizontal axis. Defaults to 5.</param>
    /// <param name="height">Number of rooms along the vertical axis. Defaults to 4.</param>
    /// <param name="playerLevel">
    /// The player's current level, used to scale enemy stats via
    /// <see cref="EnemyFactory.CreateScaled"/>. Defaults to 1.
    /// </param>
    /// <param name="floorMultiplier">
    /// Additional stat multiplier representing dungeon depth (1.0 = floor 1, 1.5 = floor 2, etc.).
    /// Defaults to 1.0.
    /// </param>
    /// <param name="difficulty">
    /// Optional difficulty settings whose <see cref="DifficultySettings.EnemyStatMultiplier"/>
    /// is combined with <paramref name="floorMultiplier"/> to scale all enemy stats.
    /// Defaults to <see langword="null"/> (treated as Normal, multiplier = 1.0).
    /// </param>
    /// <param name="floor">
    /// The dungeon floor number (1–8), used to select the appropriate themed description pool
    /// via <see cref="RoomDescriptions.ForFloor"/>. Defaults to 1.
    /// </param>
    /// <returns>
    /// A tuple of (<c>startRoom</c>, <c>exitRoom</c>) where <c>startRoom</c> is the
    /// player's entry point and <c>exitRoom</c> is the boss-guarded exit.
    /// </returns>
    public (Room startRoom, Room exitRoom) Generate(int width = 5, int height = 4, int playerLevel = 1, float floorMultiplier = 1.0f, DifficultySettings? difficulty = null, int floor = 1)
    {
        float effectiveMult = floorMultiplier * (difficulty?.EnemyStatMultiplier ?? 1.0f);
        var roomPool = RoomDescriptions.ForFloor(floor);
        var grid = new Room[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = new Room
                {
                    Description = roomPool[_rng.Next(roomPool.Length)]
                };
            }
        }

        // Connect adjacent rooms bidirectionally
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var current = grid[y, x];
                
                // North connection
                if (y > 0)
                {
                    var north = grid[y - 1, x];
                    current.Exits[Direction.North] = north;
                    north.Exits[Direction.South] = current;
                }
                
                // East connection
                if (x < width - 1)
                {
                    var east = grid[y, x + 1];
                    current.Exits[Direction.East] = east;
                    east.Exits[Direction.West] = current;
                }
            }
        }

        // Set start and exit rooms
        var startRoom = grid[0, 0];
        var exitRoom = grid[height - 1, width - 1];
        exitRoom.IsExit = true;
        exitRoom.Description = "A grand chamber with ornate pillars and a massive stone door leading to freedom.";

        // Place boss in exit room
        exitRoom.Enemy = EnemyFactory.CreateBoss(_rng, floor);

        // Place enemies in ~60% of non-start, non-exit rooms using floor-appropriate spawn pools
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var room = grid[y, x];
                if (room == startRoom || room == exitRoom) continue;

                room.Type = (RoomType)_rng.Next(6);

                if (_rng.NextDouble() < 0.6)
                {
                    var enemyType = FloorSpawnPools.GetRandomEnemyForFloor(floor, _rng);
                    room.Enemy = EnemyFactory.CreateScaled(enemyType, playerLevel, effectiveMult);

                    // Apply elite boost on floors 4+ (5% chance; 10% on floor 8)
                    int eliteThreshold = FloorSpawnPools.GetEliteChanceForFloor(floor);
                    if (eliteThreshold > 0 && _rng.Next(100) < eliteThreshold)
                    {
                        room.Enemy.HP = room.Enemy.MaxHP = (int)(room.Enemy.MaxHP * 1.5);
                        room.Enemy.Attack = (int)(room.Enemy.Attack * 1.25);
                        room.Enemy.Name = $"Elite {room.Enemy.Name}";
                        room.Enemy.IsElite = true;
                    }
                }

                if (_rng.Next(100) < Math.Min(35, (int)(20 * (difficulty?.MerchantSpawnMultiplier ?? 1.0f)))) room.Merchant = Merchant.CreateRandom(_rng, floor, _allItems, difficulty);
                if (_rng.Next(100) < 15) room.Hazard = (HazardType)(_rng.Next(3) + 1); // 1-3 = Spike/Poison/Fire

                // Assign per-action environmental hazards by floor range
                if (floor >= 7 && _rng.Next(100) < 15)
                    room.EnvironmentalHazard = RoomHazard.LavaSeam;
                else if (floor >= 5 && _rng.Next(100) < 10)
                    room.EnvironmentalHazard = RoomHazard.CorruptedGround;
                else if (floor <= 6 && _rng.Next(100) < 8)
                    room.EnvironmentalHazard = RoomHazard.BlessedClearing;
            }
        }

        // Place items in some rooms
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var room = grid[y, x];
                if (room == startRoom || room == exitRoom) continue;
                if (_rng.NextDouble() < 0.3)
                {
                    room.Items.Add(CreateRandomItem(floor));
                }
            }
        }

        // Place shrines in 15% of non-start, non-exit rooms
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var room = grid[y, x];
                if (room == startRoom || room == exitRoom) continue;
                if (_rng.NextDouble() < Math.Min(0.35, 0.15 * (difficulty?.ShrineSpawnMultiplier ?? 1.0f)))
                    room.HasShrine = true;
            }
        }

        // Place special rooms (one per type where floor range allows)
        var eligibleRooms = new List<Room>();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var r = grid[y, x];
                if (r != startRoom && r != exitRoom && r.Type != RoomType.ForgottenShrine
                    && r.Type != RoomType.PetrifiedLibrary && r.Type != RoomType.ContestedArmory
                    && r.Type != RoomType.TrapRoom)
                    eligibleRooms.Add(r);
            }
        eligibleRooms = eligibleRooms.OrderBy(_ => _rng.Next()).ToList();
        int specialIdx = 0;
        if (floor >= 2 && specialIdx < eligibleRooms.Count)
            eligibleRooms[specialIdx++].Type = RoomType.ForgottenShrine;
        if (floor >= 3 && specialIdx < eligibleRooms.Count)
            eligibleRooms[specialIdx++].Type = RoomType.PetrifiedLibrary;
        if (floor >= 4 && specialIdx < eligibleRooms.Count)
            eligibleRooms[specialIdx++].Type = RoomType.ContestedArmory;
        if (floor >= 1 && specialIdx < eligibleRooms.Count)
        {
            var trapRoom = eligibleRooms[specialIdx++];
            trapRoom.Type = RoomType.TrapRoom;
            trapRoom.Trap = (TrapVariant)_rng.Next(3);
        }

        // Assign context-aware descriptions now that all room contents are set
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var room = grid[y, x];
                if (room == exitRoom) continue;

                var ctx = room switch
                {
                    { Enemy: not null }    => RoomContext.Enemy,
                    { Merchant: not null } => RoomContext.Merchant,
                    { HasShrine: true }    => RoomContext.Shrine,
                    { Type: RoomType.ForgottenShrine
                         or RoomType.PetrifiedLibrary
                         or RoomType.ContestedArmory
                         or RoomType.TrapRoom } => RoomContext.Special,
                    _                      => RoomContext.Empty,
                };
                var pool = RoomDescriptions.ForFloorAndContext(floor, ctx);
                room.Description = pool[_rng.Next(pool.Length)];
            }
        }

        // Verify path exists using BFS
        if (!PathExists(startRoom, exitRoom))
        {
            // Safety net: this should never happen with a fully connected grid, but if it
            // does, reject the layout rather than silently handing the player an unwinnable dungeon.
            throw new InvalidOperationException("Generated disconnected dungeon — this should never happen with current generator.");
        }

        return (startRoom, exitRoom);
    }

    private Item CreateRandomItem(int floor)
    {
        ItemTier tier;
        if (floor <= 2)
            tier = ItemTier.Common;
        else if (floor <= 4)
            tier = ItemTier.Uncommon;
        else
        {
            // Floors 5-6: 8% Epic chance; floors 7-8: 15% Epic chance; otherwise Rare
            double epicChance = floor >= 7 ? 0.15 : 0.08;
            tier = _rng.NextDouble() < epicChance ? ItemTier.Epic : ItemTier.Rare;
        }

        if (_allItems.Count > 0)
        {
            var pool = _allItems.Where(i => i.Tier == tier).ToList();
            if (pool.Count > 0)
                return pool[_rng.Next(pool.Count)].Clone();
        }

        // Fallback when item config not loaded
        return new Item
        {
            Name = "Health Potion",
            Type = ItemType.Consumable,
            HealAmount = 20,
            Description = "Restores 20 HP",
            Tier = ItemTier.Common
        };
    }

    private bool PathExists(Room start, Room target)
    {
        var visited = new HashSet<Room>();
        var queue = new Queue<Room>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target) return true;

            foreach (var exit in current.Exits.Values)
            {
                if (!visited.Contains(exit))
                {
                    visited.Add(exit);
                    queue.Enqueue(exit);
                }
            }
        }

        return false;
    }
}
