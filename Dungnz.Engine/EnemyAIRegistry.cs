namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems.Enemies;

/// <summary>
/// Registry that maps enemy types to their AI implementations.
/// Provides a centralized lookup for enemy combat behaviors.
/// </summary>
public static class EnemyAIRegistry
{
    private static readonly DefaultEnemyAI _defaultAI = new();
    private static readonly GoblinAI _goblinAI = new();
    private static readonly SkeletonAI _skeletonAI = new();
    
    private static readonly Dictionary<Type, IEnemyAI> _aiByType = new()
    {
        // Specialized AI implementations
        { typeof(Goblin), _goblinAI },
        { typeof(Skeleton), _skeletonAI },
        
        // Regular enemies - using default AI
        { typeof(BladeDancer), _defaultAI },
        { typeof(BloodHound), _defaultAI },
        { typeof(BoneArcher), _defaultAI },
        { typeof(CarrionCrawler), _defaultAI },
        { typeof(ChaosKnight), _defaultAI },
        { typeof(CryptPriest), _defaultAI },
        { typeof(CursedZombie), _defaultAI },
        { typeof(DarkKnight), _defaultAI },
        { typeof(DarkSorcerer), _defaultAI },
        { typeof(FrostWyvern), _defaultAI },
        { typeof(GiantRat), _defaultAI },
        { typeof(GoblinShaman), _defaultAI },
        { typeof(IronGuard), _defaultAI },
        { typeof(ManaLeech), _defaultAI },
        { typeof(Mimic), _defaultAI },
        { typeof(NightStalker), _defaultAI },
        { typeof(PlagueBear), _defaultAI },
        { typeof(ShadowImp), _defaultAI },
        { typeof(ShieldBreaker), _defaultAI },
        { typeof(SiegeOgre), _defaultAI },
        { typeof(StoneGolem), _defaultAI },
        { typeof(Troll), _defaultAI },
        { typeof(VampireLord), _defaultAI },
        { typeof(Wraith), _defaultAI },
        
        // Boss enemies - using default AI
        { typeof(GoblinWarchief), _defaultAI },
        { typeof(PlagueHoundAlpha), _defaultAI },
        { typeof(IronSentinel), _defaultAI },
        { typeof(BoneArchon), _defaultAI },
        { typeof(CrimsonVampire), _defaultAI },
        { typeof(LichKing), _defaultAI },
        { typeof(StoneTitan), _defaultAI },
        { typeof(ShadowWraith), _defaultAI },
        { typeof(VampireBoss), _defaultAI },
        { typeof(ArchlichSovereign), _defaultAI },
        { typeof(AbyssalLeviathan), _defaultAI },
        { typeof(InfernalDragon), _defaultAI }
    };
    
    /// <summary>
    /// Gets the AI implementation for the given enemy, or null if none is registered.
    /// </summary>
    public static IEnemyAI? GetAI(Enemy enemy)
    {
        if (_aiByType.TryGetValue(enemy.GetType(), out var ai))
            return ai;
        return null;
    }
    
    /// <summary>
    /// Registers a custom AI implementation for a specific enemy type.
    /// Used by tests or to add new AI behaviors at runtime.
    /// </summary>
    public static void RegisterAI<T>(IEnemyAI ai) where T : Enemy
    {
        _aiByType[typeof(T)] = ai;
    }
}
