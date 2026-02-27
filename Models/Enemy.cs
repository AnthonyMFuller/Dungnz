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
[JsonDerivedType(typeof(GiantRat), "giantrat")]
[JsonDerivedType(typeof(CursedZombie), "cursedzombie")]
[JsonDerivedType(typeof(BloodHound), "bloodhound")]
[JsonDerivedType(typeof(IronGuard), "ironguard")]
[JsonDerivedType(typeof(NightStalker), "nightstalker")]
[JsonDerivedType(typeof(FrostWyvern), "frostwyvern")]
[JsonDerivedType(typeof(ChaosKnight), "chaosknight")]
[JsonDerivedType(typeof(ShadowImp), "shadowimp")]
[JsonDerivedType(typeof(CarrionCrawler), "carrioncrawler")]
[JsonDerivedType(typeof(DarkSorcerer), "darksorcerer")]
[JsonDerivedType(typeof(BoneArcher), "bonearcher")]
[JsonDerivedType(typeof(CryptPriest), "cryptpriest")]
[JsonDerivedType(typeof(PlagueBear), "plaguebear")]
[JsonDerivedType(typeof(SiegeOgre), "siegeogre")]
[JsonDerivedType(typeof(BladeDancer), "bladedancer")]
[JsonDerivedType(typeof(ManaLeech), "manaleech")]
[JsonDerivedType(typeof(ShieldBreaker), "shieldbreaker")]
[JsonDerivedType(typeof(ArchlichSovereign), "archlichsovereign")]
[JsonDerivedType(typeof(AbyssalLeviathan), "abyssalleviathan")]
[JsonDerivedType(typeof(InfernalDragon), "infernaldragon")]
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

    /// <summary>
    /// Gets or sets whether this enemy is of an undead creature type (e.g. Skeleton, Zombie, Lich).
    /// Used by abilities, items, and effects that interact specifically with undead.
    /// </summary>
    public bool IsUndead { get; set; }

    /// <summary>
    /// Gets the ASCII art lines for this enemy, displayed before combat if non-empty.
    /// An empty array means no art is shown.
    /// </summary>
    public string[] AsciiArt { get; protected set; } = Array.Empty<string>();

    // ── WI-C1/C2/C3 special mechanic properties ────────────────────────────

    /// <summary>GiantRat: pack size (1–3); ATK += 2 * (PackCount - 1) up to +6.</summary>
    public int PackCount { get; set; }

    /// <summary>CursedZombie: status effect to apply to the player on death (null = none).</summary>
    public StatusEffect? OnDeathEffect { get; protected set; }

    /// <summary>BloodHound / PlagueBear on-hit: chance [0,1] to apply Bleed to the player.</summary>
    public float BleedOnHitChance { get; protected set; }

    /// <summary>IronGuard: probability [0,1] of a counter-strike after the player attacks.</summary>
    public float CounterStrikeChance { get; protected set; }

    /// <summary>NightStalker / BoneArcher: multiplier for the first attack in combat.</summary>
    public float FirstAttackMultiplier { get; protected set; } = 1f;

    /// <summary>NightStalker / BoneArcher: set to true once the first-attack bonus has fired.</summary>
    public bool FirstAttackUsed { get; set; }

    /// <summary>FrostWyvern: use Frost Breath every Nth attack (0 = disabled).</summary>
    public int FrostBreathEvery { get; protected set; }

    /// <summary>FrostWyvern: cumulative count of attacks performed this combat.</summary>
    public int AttackCount { get; set; }

    /// <summary>ChaosKnight: flat critical-hit chance [0,1] on every attack.</summary>
    public float EnemyCritChance { get; protected set; }

    /// <summary>ChaosKnight: when true, Stun applications silently fail.</summary>
    public bool IsStunImmune { get; protected set; }

    /// <summary>CarrionCrawler / CryptPriest: HP regenerated at the start of the enemy turn each round.</summary>
    public int RegenPerTurn { get; protected set; }

    /// <summary>DarkSorcerer: chance [0,1] to apply Weakened to the player instead of dealing damage.</summary>
    public float WeakenOnAttackChance { get; protected set; }

    /// <summary>CryptPriest: self-heal amount triggered every <see cref="SelfHealEveryTurns"/> turns.</summary>
    public int SelfHealAmount { get; protected set; }

    /// <summary>CryptPriest: number of turns between self-heals.</summary>
    public int SelfHealEveryTurns { get; protected set; }

    /// <summary>CryptPriest: internal cooldown counter; decremented each enemy turn.</summary>
    public int SelfHealCooldown { get; set; }

    /// <summary>PlagueBear: when true, applies Poison to the player at combat start.</summary>
    public bool PoisonOnCombatStart { get; protected set; }

    /// <summary>PlagueBear: chance [0,1] to reapply Poison on death.</summary>
    public float PoisonOnDeathChance { get; protected set; }

    /// <summary>SiegeOgre: remaining hits that receive a flat damage reduction (thick hide).</summary>
    public int ThickHideHitsRemaining { get; set; }

    /// <summary>SiegeOgre: flat damage reduction per hit while thick hide is active.</summary>
    public int ThickHideDamageReduction { get; protected set; }

    /// <summary>BladeDancer: chance [0,1] of a counter-attack when the player successfully dodges.</summary>
    public float OnDodgeCounterChance { get; protected set; }

    /// <summary>ManaLeech: mana drained from the player on each successful hit.</summary>
    public int ManaDrainPerHit { get; protected set; }

    /// <summary>ManaLeech: ATK bonus multiplier applied when the player has 0 mana.</summary>
    public float ZeroManaAtkBonus { get; protected set; }

    /// <summary>ShieldBreaker: player DEF threshold above which 50% of DEF is ignored.</summary>
    public int ShieldBreakerDefThreshold { get; protected set; }

    /// <summary>ShadowImp: flat damage reduction applied on each incoming hit (simulates pack).</summary>
    public int GroupDamageReduction { get; protected set; }

    /// <summary>BoneArcher: critical-hit chance on the first attack only (stacks with FirstAttackMultiplier).</summary>
    public float FirstAttackCritChance { get; protected set; }

    // ── Boss mechanic properties ────────────────────────────────────────────

    /// <summary>ArchlichSovereign: number of skeletal-add guards currently alive.</summary>
    public int AddsAlive { get; set; }

    /// <summary>ArchlichSovereign / InfernalDragon: when true, player attacks hit an add instead of the boss.</summary>
    public bool DamageImmune { get; set; }

    /// <summary>ArchlichSovereign: has already triggered its once-per-combat revive.</summary>
    public bool HasRevived { get; set; }

    /// <summary>ArchlichSovereign: true once the adds phase has fired (prevents premature revive).</summary>
    public bool Phase2Triggered { get; set; }

    /// <summary>AbyssalLeviathan: turn counter used to track submerge cycles.</summary>
    public int TurnCount { get; set; }

    /// <summary>AbyssalLeviathan: when true, the player's attack is skipped this turn.</summary>
    public bool IsSubmerged { get; set; }

    /// <summary>InfernalDragon: when true, the flight-phase 40% miss chance is active.</summary>
    public bool FlightPhaseActive { get; set; }

    /// <summary>InfernalDragon: turns until next Flame Breath (fires every 2nd enemy turn).</summary>
    public int FlameBreathCooldown { get; set; }
}
