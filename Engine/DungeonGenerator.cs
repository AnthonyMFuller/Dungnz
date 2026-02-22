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

    /// <summary>
    /// Initialises a new <see cref="DungeonGenerator"/> with an optional fixed seed.
    /// Using the same seed value produces an identical dungeon layout every time.
    /// </summary>
    /// <param name="seed">
    /// A seed value for the internal <see cref="Random"/> instance. When
    /// <see langword="null"/>, a non-deterministic seed is used.
    /// </param>
    public DungeonGenerator(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
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
    /// The dungeon floor number (1–5), used to select the appropriate themed description pool
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
        exitRoom.Enemy = EnemyFactory.CreateBoss(_rng);

        // Place enemies in ~60% of non-start, non-exit rooms
        var enemyTypes = new[] { "goblin", "skeleton", "troll", "darkknight", "goblinshaman", "stonegolem", "wraith", "vampirelord", "mimic" };
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var room = grid[y, x];
                if (room == startRoom || room == exitRoom) continue;

                room.Type = (RoomType)_rng.Next(6);

                if (_rng.NextDouble() < 0.6)
                {
                    var enemyType = enemyTypes[_rng.Next(enemyTypes.Length)];
                    room.Enemy = EnemyFactory.CreateScaled(enemyType, playerLevel, effectiveMult);
                }

                if (_rng.Next(100) < 20) room.Merchant = Merchant.CreateRandom(_rng);
                if (_rng.Next(100) < 15) room.Hazard = (HazardType)(_rng.Next(3) + 1); // 1-3 = Spike/Poison/Fire
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
                    room.Items.Add(CreateRandomItem());
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
                if (_rng.NextDouble() < 0.15)
                    room.HasShrine = true;
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

    private Item CreateRandomItem()
    {
        var itemType = _rng.Next(4);
        return itemType switch
        {
            0 => new Item 
            { 
                Name = "Health Potion", 
                Type = ItemType.Consumable, 
                HealAmount = 20, 
                Description = "Restores 20 HP",
                Tier = ItemTier.Common
            },
            1 => new Item 
            { 
                Name = "Iron Sword", 
                Type = ItemType.Weapon, 
                AttackBonus = 5, 
                IsEquippable = true,
                Description = "A sturdy iron blade",
                Tier = ItemTier.Common
            },
            2 => new Item 
            { 
                Name = "Leather Armor", 
                Type = ItemType.Armor, 
                DefenseBonus = 3, 
                IsEquippable = true,
                Description = "Basic leather protection",
                Tier = ItemTier.Common
            },
            _ => new Item 
            { 
                Name = "Large Health Potion", 
                Type = ItemType.Consumable, 
                HealAmount = 50, 
                Description = "Restores 50 HP",
                Tier = ItemTier.Common
            }
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
