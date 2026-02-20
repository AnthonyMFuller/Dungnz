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
        var type = rng.Next(4);
        return type switch
        {
            0 => new Goblin(_enemyConfig?.GetValueOrDefault("Goblin"), _itemConfig),
            1 => new Skeleton(_enemyConfig?.GetValueOrDefault("Skeleton"), _itemConfig),
            2 => new Troll(_enemyConfig?.GetValueOrDefault("Troll"), _itemConfig),
            _ => new DarkKnight(_enemyConfig?.GetValueOrDefault("DarkKnight"), _itemConfig)
        };
    }

    public static Enemy CreateBoss(Random rng)
    {
        return new DungeonBoss(_enemyConfig?.GetValueOrDefault("DungeonBoss"), _itemConfig);
    }
}
