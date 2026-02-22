using System.Text.Json.Serialization;
using Dungnz.Systems.Enemies;

namespace Dungnz.Models;

/// <summary>
/// Abstract base class for all dungeon enemies. Defines core combat stats, special mechanic
/// flags, and the loot table used when the enemy is defeated. Concrete enemy types derive
/// from this class and configure their specific behaviours in their constructors.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Goblin), "Goblin")]
[JsonDerivedType(typeof(GoblinShaman), "GoblinShaman")]
[JsonDerivedType(typeof(Skeleton), "Skeleton")]
[JsonDerivedType(typeof(Troll), "Troll")]
[JsonDerivedType(typeof(DarkKnight), "DarkKnight")]
[JsonDerivedType(typeof(Mimic), "Mimic")]
[JsonDerivedType(typeof(StoneGolem), "StoneGolem")]
[JsonDerivedType(typeof(VampireLord), "VampireLord")]
[JsonDerivedType(typeof(Wraith), "Wraith")]
[JsonDerivedType(typeof(DungeonBoss), "DungeonBoss")]
[JsonDerivedType(typeof(LichKing), "lichking")]
[JsonDerivedType(typeof(StoneTitan), "stonetitan")]
[JsonDerivedType(typeof(ShadowWraith), "shadowwraith")]
[JsonDerivedType(typeof(VampireBoss), "vampireboss")]
public abstract class Enemy
{
    /// <summary>Gets or sets the enemy's display name used in combat and room descriptions.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the enemy's current hit points. Reaches 0 when the enemy is defeated.</summary>
    public int HP { get; set; }

    /// <summary>Gets or sets the enemy's maximum hit points, used to initialise <see cref="HP"/> and display health bars.</summary>
    public int MaxHP { get; set; }

    /// <summary>Gets or sets the enemy's base attack power used to calculate damage dealt to the player before defense is applied.</summary>
    public int Attack { get; set; }

    /// <summary>Gets or sets the enemy's defense value, which reduces incoming damage from the player's attacks.</summary>
    public int Defense { get; set; }

    /// <summary>Gets or sets the amount of experience points awarded to the player upon defeating this enemy.</summary>
    public int XPValue { get; set; }

    /// <summary>Gets or sets the loot table that determines what items and gold are dropped when this enemy dies.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public LootTable LootTable { get; set; } = new LootTable();

    // Special mechanic flags
    /// <summary>
    /// Gets whether this enemy is immune to all status effects (e.g., Poison, Bleed, Stun).
    /// When <c>true</c>, attempts to apply effects are silently ignored.
    /// </summary>
    public bool IsImmuneToEffects { get; protected set; }

    /// <summary>
    /// Gets the enemy's flat dodge chance in the range [0, 1], overriding the default
    /// DEF-based dodge formula. A value of <c>-1</c> signals that the standard formula should be used.
    /// </summary>
    public float FlatDodgeChance { get; protected set; } = -1f; // -1 = use DEF-based formula

    /// <summary>
    /// Gets the fraction of damage dealt by this enemy that is converted back into HP for the enemy,
    /// in the range [0, 1]. A value of 0 means no lifesteal.
    /// </summary>
    public float LifestealPercent { get; protected set; }

    /// <summary>
    /// Gets whether each successful hit from this enemy applies the Poison status effect to the player.
    /// </summary>
    public bool AppliesPoisonOnHit { get; protected set; }

    /// <summary>
    /// Gets or sets whether this enemy has ambush initiative, allowing it to attack before the
    /// player on the very first turn of combat.
    /// </summary>
    public bool IsAmbush { get; set; } // first-turn surprise

    /// <summary>
    /// Gets or sets whether this enemy is an elite variant with boosted stats and a guaranteed
    /// tier-2 or higher item drop from its <see cref="LootTable"/>.
    /// </summary>
    public bool IsElite { get; set; }  // boosted stats, guaranteed tier-2 drop
}
