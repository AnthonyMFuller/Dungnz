using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Dungnz.Models;
using Dungnz.Systems.Enemies;

namespace Dungnz.Engine;

/// <summary>
/// Provides a configured <see cref="JsonSerializerOptions"/> instance with all concrete
/// <see cref="Enemy"/> subtypes registered for polymorphic serialisation. This avoids
/// compile-time <c>[JsonDerivedType]</c> attributes on <see cref="Enemy"/>, which would
/// create a circular project reference between Dungnz.Models and Dungnz.Systems.Enemies.
/// </summary>
internal static class EnemyTypeRegistry
{
    /// <summary>
    /// Creates a <see cref="JsonSerializerOptions"/> instance with enemy polymorphism
    /// and standard save-system settings pre-configured.
    /// </summary>
    public static JsonSerializerOptions CreateOptions() =>
        new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { AddEnemyPolymorphism }
            }
        };

    private static void AddEnemyPolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(Enemy)) return;

        typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type"
        };

        var derived = typeInfo.PolymorphismOptions.DerivedTypes;
        derived.Add(new JsonDerivedType(typeof(Goblin), "goblin"));
        derived.Add(new JsonDerivedType(typeof(GoblinShaman), "goblinshaman"));
        derived.Add(new JsonDerivedType(typeof(Skeleton), "skeleton"));
        derived.Add(new JsonDerivedType(typeof(Troll), "troll"));
        derived.Add(new JsonDerivedType(typeof(DarkKnight), "darkknight"));
        derived.Add(new JsonDerivedType(typeof(Mimic), "mimic"));
        derived.Add(new JsonDerivedType(typeof(StoneGolem), "stonegolem"));
        derived.Add(new JsonDerivedType(typeof(VampireLord), "vampirelord"));
        derived.Add(new JsonDerivedType(typeof(Wraith), "wraith"));
        derived.Add(new JsonDerivedType(typeof(DungeonBoss), "dungeonboss"));
        derived.Add(new JsonDerivedType(typeof(GoblinWarchief), "goblinwarchief"));
        derived.Add(new JsonDerivedType(typeof(PlagueHoundAlpha), "plaguehoundalpha"));
        derived.Add(new JsonDerivedType(typeof(IronSentinel), "ironsentinel"));
        derived.Add(new JsonDerivedType(typeof(BoneArchon), "bonearchon"));
        derived.Add(new JsonDerivedType(typeof(CrimsonVampire), "crimsonvampire"));
        derived.Add(new JsonDerivedType(typeof(LichKing), "lichking"));
        derived.Add(new JsonDerivedType(typeof(StoneTitan), "stonetitan"));
        derived.Add(new JsonDerivedType(typeof(ShadowWraith), "shadowwraith"));
        derived.Add(new JsonDerivedType(typeof(VampireBoss), "vampireboss"));
        derived.Add(new JsonDerivedType(typeof(GiantRat), "giantrat"));
        derived.Add(new JsonDerivedType(typeof(CursedZombie), "cursedzombie"));
        derived.Add(new JsonDerivedType(typeof(BloodHound), "bloodhound"));
        derived.Add(new JsonDerivedType(typeof(IronGuard), "ironguard"));
        derived.Add(new JsonDerivedType(typeof(NightStalker), "nightstalker"));
        derived.Add(new JsonDerivedType(typeof(FrostWyvern), "frostwyvern"));
        derived.Add(new JsonDerivedType(typeof(ChaosKnight), "chaosknight"));
        derived.Add(new JsonDerivedType(typeof(ShadowImp), "shadowimp"));
        derived.Add(new JsonDerivedType(typeof(CarrionCrawler), "carrioncrawler"));
        derived.Add(new JsonDerivedType(typeof(DarkSorcerer), "darksorcerer"));
        derived.Add(new JsonDerivedType(typeof(BoneArcher), "bonearcher"));
        derived.Add(new JsonDerivedType(typeof(CryptPriest), "cryptpriest"));
        derived.Add(new JsonDerivedType(typeof(PlagueBear), "plaguebear"));
        derived.Add(new JsonDerivedType(typeof(SiegeOgre), "siegeogre"));
        derived.Add(new JsonDerivedType(typeof(BladeDancer), "bladedancer"));
        derived.Add(new JsonDerivedType(typeof(ManaLeech), "manaleech"));
        derived.Add(new JsonDerivedType(typeof(ShieldBreaker), "shieldbreaker"));
        derived.Add(new JsonDerivedType(typeof(ArchlichSovereign), "archlichsovereign"));
        derived.Add(new JsonDerivedType(typeof(AbyssalLeviathan), "abyssalleviathan"));
        derived.Add(new JsonDerivedType(typeof(InfernalDragon), "infernaldragon"));
        derived.Add(new JsonDerivedType(typeof(GenericEnemy), "genericenemy"));
    }

    /// <summary>
    /// Returns all concrete <see cref="Enemy"/> subtypes registered by this registry,
    /// keyed by their JSON type discriminator string.
    /// </summary>
    internal static IReadOnlyDictionary<string, Type> RegisteredTypes()
    {
        var options = CreateOptions();
        var typeInfo = options.GetTypeInfo(typeof(Enemy));
        return typeInfo.PolymorphismOptions!.DerivedTypes
            .ToDictionary(dt => (string)dt.TypeDiscriminator!, dt => dt.DerivedType);
    }
}
