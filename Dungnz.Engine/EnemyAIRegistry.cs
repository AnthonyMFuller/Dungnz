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

    // Specialized regular-enemy AIs (#1375)
    private static readonly GoblinShamanAI _goblinShamanAI = new();
    private static readonly WraithAI _wraithAI = new();
    private static readonly StoneGolemAI _stoneGolemAI = new();
    private static readonly VampireLordAI _vampireLordAI = new();
    private static readonly MimicAI _mimicAI = new();

    // Boss AIs (#1375)
    private static readonly GoblinWarchiefAI _goblinWarchiefAI = new();
    private static readonly PlagueHoundAlphaAI _plagueHoundAlphaAI = new();
    private static readonly IronSentinelAI _ironSentinelAI = new();
    private static readonly BoneArchonAI _boneArchonAI = new();
    private static readonly CrimsonVampireAI _crimsonVampireAI = new();
    private static readonly LichKingAI _lichKingAI = new();
    private static readonly StoneTitanAI _stoneTitanAI = new();
    private static readonly ShadowWraithAI _shadowWraithAI = new();
    private static readonly VampireBossAI _vampireBossAI = new();
    private static readonly ArchlichSovereignAI _archlichSovereignAI = new();
    private static readonly AbyssalLeviathanAI _abyssalLeviathanAI = new();
    private static readonly InfernalDragonAI _infernalDragonAI = new();
    
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
        { typeof(GoblinShaman), _goblinShamanAI },
        { typeof(IronGuard), _defaultAI },
        { typeof(ManaLeech), _defaultAI },
        { typeof(Mimic), _mimicAI },
        { typeof(NightStalker), _defaultAI },
        { typeof(PlagueBear), _defaultAI },
        { typeof(ShadowImp), _defaultAI },
        { typeof(ShieldBreaker), _defaultAI },
        { typeof(SiegeOgre), _defaultAI },
        { typeof(StoneGolem), _stoneGolemAI },
        { typeof(Troll), _defaultAI },
        { typeof(VampireLord), _vampireLordAI },
        { typeof(Wraith), _wraithAI },
        
        // Boss enemies - using default AI
        { typeof(GoblinWarchief), _goblinWarchiefAI },
        { typeof(PlagueHoundAlpha), _plagueHoundAlphaAI },
        { typeof(IronSentinel), _ironSentinelAI },
        { typeof(BoneArchon), _boneArchonAI },
        { typeof(CrimsonVampire), _crimsonVampireAI },
        { typeof(LichKing), _lichKingAI },
        { typeof(StoneTitan), _stoneTitanAI },
        { typeof(ShadowWraith), _shadowWraithAI },
        { typeof(VampireBoss), _vampireBossAI },
        { typeof(ArchlichSovereign), _archlichSovereignAI },
        { typeof(AbyssalLeviathan), _abyssalLeviathanAI },
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
