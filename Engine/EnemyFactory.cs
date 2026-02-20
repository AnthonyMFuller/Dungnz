namespace Dungnz.Engine;

using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;

public static class EnemyFactory
{
    private static Dictionary<string, EnemyStats>? _enemyConfig;
    private static List<ItemStats>? _itemConfig;
    
    public static void Initialize(string enemyConfigPath, string itemConfigPath)
    {
        _enemyConfig = EnemyConfig.Load(enemyConfigPath);
        _itemConfig = ItemConfig.Load(itemConfigPath);
    }
    
    public static Enemy CreateRandom(Random rng)
    {
        var type = rng.Next(9);
        return type switch
        {
            0 => new Goblin(_enemyConfig?.GetValueOrDefault("Goblin")),
            1 => new Skeleton(_enemyConfig?.GetValueOrDefault("Skeleton"), _itemConfig),
            2 => new Troll(_enemyConfig?.GetValueOrDefault("Troll"), _itemConfig),
            3 => new DarkKnight(_enemyConfig?.GetValueOrDefault("DarkKnight"), _itemConfig),
            4 => new GoblinShaman(_enemyConfig?.GetValueOrDefault("GoblinShaman"), _itemConfig),
            5 => new StoneGolem(_enemyConfig?.GetValueOrDefault("StoneGolem"), _itemConfig),
            6 => new Wraith(_enemyConfig?.GetValueOrDefault("Wraith"), _itemConfig),
            7 => new VampireLord(_enemyConfig?.GetValueOrDefault("VampireLord"), _itemConfig),
            _ => new Mimic(_enemyConfig?.GetValueOrDefault("Mimic"), _itemConfig)
        };
    }

    public static Enemy CreateBoss(Random rng)
    {
        return new DungeonBoss(_enemyConfig?.GetValueOrDefault("DungeonBoss"), _itemConfig);
    }

    public static Enemy CreateScaled(string enemyType, int playerLevel)
    {
        var scalar = 1.0f + (playerLevel - 1) * 0.12f;
        
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

        var scaledStats = new EnemyStats
        {
            Name = baseStats.Name,
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
