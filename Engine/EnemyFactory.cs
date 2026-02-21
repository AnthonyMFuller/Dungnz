namespace Dungnz.Engine;

using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;

/// <summary>
/// Static factory responsible for creating all enemy instances used by the dungeon
/// generator and combat system. Enemies are constructed from configuration data loaded
/// via <see cref="Initialize"/>, and their stats can be scaled to the player's current
/// level and dungeon floor to maintain a consistent challenge curve.
/// </summary>
public static class EnemyFactory
{
    private static Dictionary<string, EnemyStats>? _enemyConfig;
    private static List<ItemStats>? _itemConfig;

    /// <summary>
    /// Loads enemy base-stat and item-drop configuration from external data files so
    /// that all subsequent factory methods have data to work from. Must be called once
    /// before any <c>Create*</c> method is invoked.
    /// </summary>
    /// <param name="enemyConfigPath">
    /// File-system path to the JSON or CSV file that defines base stats for every
    /// enemy type (HP, attack, defense, XP value, gold range, etc.).
    /// </param>
    /// <param name="itemConfigPath">
    /// File-system path to the JSON or CSV file that defines the item drop pool used
    /// when constructing enemy loot tables.
    /// </param>
    public static void Initialize(string enemyConfigPath, string itemConfigPath)
    {
        _enemyConfig = EnemyConfig.Load(enemyConfigPath);
        _itemConfig = ItemConfig.Load(itemConfigPath);
    }

    /// <summary>
    /// Creates a randomly chosen enemy from the full pool of available types, with a
    /// 5 % chance that the selected enemy is promoted to an Elite variant (1.5× stats,
    /// "Elite" name prefix, and <see cref="Enemy.IsElite"/> flag set).
    /// </summary>
    /// <param name="rng">
    /// The random-number generator used to pick the enemy type and decide elite status.
    /// </param>
    /// <returns>
    /// A fully initialised <see cref="Enemy"/> instance ready to be placed in a room.
    /// </returns>
    public static Enemy CreateRandom(Random rng)
    {
        var type = rng.Next(9);
        var enemy = type switch
        {
            0 => (Enemy)new Goblin(_enemyConfig?.GetValueOrDefault("Goblin"), _itemConfig),
            1 => new Skeleton(_enemyConfig?.GetValueOrDefault("Skeleton"), _itemConfig),
            2 => new Troll(_enemyConfig?.GetValueOrDefault("Troll"), _itemConfig),
            3 => new DarkKnight(_enemyConfig?.GetValueOrDefault("DarkKnight"), _itemConfig),
            4 => new GoblinShaman(_enemyConfig?.GetValueOrDefault("GoblinShaman"), _itemConfig),
            5 => new StoneGolem(_enemyConfig?.GetValueOrDefault("StoneGolem"), _itemConfig),
            6 => new Wraith(_enemyConfig?.GetValueOrDefault("Wraith"), _itemConfig),
            7 => new VampireLord(_enemyConfig?.GetValueOrDefault("VampireLord"), _itemConfig),
            _ => new Mimic(_enemyConfig?.GetValueOrDefault("Mimic"), _itemConfig)
        };

        // 5% chance to spawn as Elite variant
        if (rng.Next(100) < 5)
        {
            enemy.HP = enemy.MaxHP = (int)(enemy.MaxHP * 1.5);
            enemy.Attack = (int)(enemy.Attack * 1.5);
            enemy.Defense = (int)(enemy.Defense * 1.5);
            enemy.Name = $"Elite {enemy.Name}";
            enemy.IsElite = true;
        }

        return enemy;
    }

    /// <summary>
    /// Creates a <see cref="DungeonBoss"/> enemy using the configured base stats.
    /// The <paramref name="rng"/> parameter is accepted for API consistency but is
    /// not currently used (the boss type is fixed, not randomised).
    /// </summary>
    /// <param name="rng">Random-number generator (reserved for future use).</param>
    /// <returns>A <see cref="DungeonBoss"/> instance ready for a boss-room encounter.</returns>
    public static Enemy CreateBoss(Random rng)
    {
        return new DungeonBoss(_enemyConfig?.GetValueOrDefault("DungeonBoss"), _itemConfig);
    }

    /// <summary>
    /// Creates a specific enemy type whose HP, attack, defense, XP value, and gold
    /// drop are all multiplied by a scalar derived from the player's level and the
    /// current dungeon floor, so encounters remain appropriately challenging as the
    /// player progresses.
    /// </summary>
    /// <param name="enemyType">
    /// Case-insensitive enemy type identifier (e.g. "goblin", "troll", "dungeonboss").
    /// Throws <see cref="ArgumentException"/> for unrecognised values.
    /// </param>
    /// <param name="playerLevel">
    /// The player's current level; each level above 1 adds 12 % to the base stats.
    /// </param>
    /// <param name="floorMultiplier">
    /// An additional flat multiplier for the current dungeon floor (e.g. 1.5 on floor 2).
    /// Defaults to <c>1.0</c> (no floor scaling).
    /// </param>
    /// <returns>
    /// A fully initialised and scaled <see cref="Enemy"/> ready to be placed in a room.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="enemyType"/> does not match any known enemy key.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Initialize"/> has not been called before this method.
    /// </exception>
    public static Enemy CreateScaled(string enemyType, int playerLevel, float floorMultiplier = 1.0f)
    {
        var scalar = (1.0f + (playerLevel - 1) * 0.12f) * floorMultiplier;
        
        var baseStats = enemyType.ToLower() switch
        {
            "goblin" => _enemyConfig?.GetValueOrDefault("Goblin"),
            "skeleton" => _enemyConfig?.GetValueOrDefault("Skeleton"),
            "troll" => _enemyConfig?.GetValueOrDefault("Troll"),
            "darkknight" => _enemyConfig?.GetValueOrDefault("DarkKnight"),
            "dungeonboss" => _enemyConfig?.GetValueOrDefault("DungeonBoss"),
            "goblinshaman" => _enemyConfig?.GetValueOrDefault("GoblinShaman"),
            "stonegolem" => _enemyConfig?.GetValueOrDefault("StoneGolem"),
            "wraith" => _enemyConfig?.GetValueOrDefault("Wraith"),
            "vampirelord" => _enemyConfig?.GetValueOrDefault("VampireLord"),
            "mimic" => _enemyConfig?.GetValueOrDefault("Mimic"),
            _ => throw new ArgumentException($"Unknown enemy type: {enemyType}", nameof(enemyType))
        };

        if (baseStats == null)
        {
            throw new InvalidOperationException("Enemy configuration not loaded. Call Initialize() first.");
        }

        var scaledStats = baseStats with
        {
            MaxHP = (int)Math.Round(baseStats.MaxHP * scalar),
            Attack = (int)Math.Round(baseStats.Attack * scalar),
            Defense = (int)Math.Round(baseStats.Defense * scalar),
            XPValue = (int)Math.Round(baseStats.XPValue * scalar),
            MinGold = (int)Math.Round(baseStats.MinGold * scalar),
            MaxGold = (int)Math.Round(baseStats.MaxGold * scalar)
        };

        return enemyType.ToLower() switch
        {
            "goblin" => new Goblin(scaledStats, _itemConfig),
            "skeleton" => new Skeleton(scaledStats, _itemConfig),
            "troll" => new Troll(scaledStats, _itemConfig),
            "darkknight" => new DarkKnight(scaledStats, _itemConfig),
            "dungeonboss" => new DungeonBoss(scaledStats, _itemConfig),
            "goblinshaman" => new GoblinShaman(scaledStats, _itemConfig),
            "stonegolem" => new StoneGolem(scaledStats, _itemConfig),
            "wraith" => new Wraith(scaledStats, _itemConfig),
            "vampirelord" => new VampireLord(scaledStats, _itemConfig),
            "mimic" => new Mimic(scaledStats, _itemConfig),
            _ => throw new ArgumentException($"Unknown enemy type: {enemyType}", nameof(enemyType))
        };
    }
}
