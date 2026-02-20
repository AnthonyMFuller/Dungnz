namespace Dungnz.Engine;

using Dungnz.Models;

public class DungeonGenerator
{
    private readonly Random _rng;
    private static readonly string[] RoomDescriptions = 
    {
        "A damp corridor with moss-covered stone walls. Water drips from the ceiling.",
        "A dusty chamber filled with broken furniture and cobwebs.",
        "A narrow passage with ancient runes carved into the walls.",
        "A large hall with crumbling pillars and a high vaulted ceiling.",
        "A torch-lit room with shadows dancing on the walls. The air smells of decay.",
        "A cold stone chamber with rusted chains hanging from the ceiling.",
        "A cramped space littered with bones and debris.",
        "An eerie room with strange symbols painted in faded colors on the floor.",
        "A dank cell with iron bars on one wall and scratches on the stone.",
        "A spacious vault with collapsed sections of ceiling allowing dim light through."
    };

    public DungeonGenerator(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public (Room startRoom, Room exitRoom) Generate(int width = 5, int height = 4, int playerLevel = 1)
    {
        // Create grid of rooms
        var grid = new Room[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = new Room
                {
                    Description = RoomDescriptions[_rng.Next(RoomDescriptions.Length)]
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
        exitRoom.Enemy = EnemyFactory.CreateScaled("dungeonboss", playerLevel);

        // Place enemies in ~60% of non-start, non-exit rooms
        var enemyTypes = new[] { "goblin", "skeleton", "troll", "darkknight" };
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var room = grid[y, x];
                if (room == startRoom || room == exitRoom) continue;
                
                if (_rng.NextDouble() < 0.6)
                {
                    var enemyType = enemyTypes[_rng.Next(enemyTypes.Length)];
                    room.Enemy = EnemyFactory.CreateScaled(enemyType, playerLevel);
                }
            }
        }

        // Place items in some rooms
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var room = grid[y, x];
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
            // This should never happen with a full grid, but as a safety measure
            // we could add corridors. For now, the full grid guarantees connectivity.
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
                Description = "Restores 20 HP" 
            },
            1 => new Item 
            { 
                Name = "Iron Sword", 
                Type = ItemType.Weapon, 
                AttackBonus = 5, 
                Description = "A sturdy iron blade" 
            },
            2 => new Item 
            { 
                Name = "Leather Armor", 
                Type = ItemType.Armor, 
                DefenseBonus = 3, 
                Description = "Basic leather protection" 
            },
            _ => new Item 
            { 
                Name = "Large Health Potion", 
                Type = ItemType.Consumable, 
                HealAmount = 50, 
                Description = "Restores 50 HP" 
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
