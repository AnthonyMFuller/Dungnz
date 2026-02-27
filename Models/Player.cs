namespace Dungnz.Models;

/// <summary>
/// Represents the player character, tracking combat stats, inventory, equipment, mana, and
/// progression throughout the dungeon crawl. Exposes methods for taking damage, healing,
/// managing gold and XP, equipping items, and levelling up.
/// </summary>
public partial class Player
{
    /// <summary>Gets or sets the player's display name shown in UI and combat messages.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of status effects currently active on the player. Used for save/load
    /// persistence and is managed directly by <see cref="Dungnz.Systems.StatusEffectManager"/>.
    /// </summary>
    public List<ActiveEffect> ActiveEffects { get; } = new();

    /// <summary>Gets the list of minions currently summoned to fight alongside the player.</summary>
    public List<Minion> ActiveMinions { get; set; } = new();

    /// <summary>Tracks the MaxHP of the last enemy killed, used by Necromancer Raise Dead.</summary>
    public int LastKilledEnemyHp { get; set; }

    /// <summary>Gets the list of traps the player has placed that have not yet triggered.</summary>
    public List<Trap> ActiveTraps { get; set; } = new();

    /// <summary>Gets or sets whether a trap has already triggered during the current combat.</summary>
    public bool TrapTriggeredThisCombat { get; set; } = false;

    /// <summary>
    /// Gets or sets a temporary attack bonus applied for the rest of the current floor
    /// (Holy Strength blessing from a Forgotten Shrine). Reset to 0 on floor descent.
    /// </summary>
    public int TempAttackBonus { get; set; } = 0;

    /// <summary>
    /// Temporary defense bonus applied by consumables (e.g. Stone Skin Elixir, Elixir of Swiftness).
    /// Reset to 0 on floor descent.
    /// </summary>
    public int TempDefenseBonus { get; set; } = 0;

    /// <summary>
    /// When <see langword="true"/>, the next shrine visit automatically heals 30% MaxHP
    /// (Sacred Ground blessing from a Forgotten Shrine). Cleared after triggering.
    /// </summary>
    public bool SacredGroundActive { get; set; } = false;

    /// <summary>
    /// When <see langword="true"/>, enemy attacks have a 20% miss chance for the current
    /// combat (Warding Veil blessing from a Forgotten Shrine). Cleared at combat end.
    /// </summary>
    public bool WardingVeilActive { get; set; } = false;

    /// <summary>Paladin: number of turns Divine Shield is still active (0 = inactive).</summary>
    public int DivineShieldTurnsRemaining { get; set; } = 0;

    /// <summary>Paladin: once-per-combat auto-heal at 30% HP has been used.</summary>
    public bool DivineHealUsedThisCombat { get; set; } = false;

    /// <summary>Ranger: once-per-combat Hunter's Mark (+25% first attack) has been used.</summary>
    public bool HunterMarkUsedThisCombat { get; set; } = false;

    /// <summary>Necromancer: LichsBargain passive is active (0-cost abilities for 1 turn).</summary>
    public bool LichsBargainActive { get; set; } = false;

    // ── Passive effect per-combat/per-run flags ─────────────────────────────

    /// <summary>Tracks whether the Aegis of the Immortal survive-at-one passive has fired this combat.</summary>
    public bool AegisUsedThisCombat { get; set; } = false;

    /// <summary>Tracks whether the Shadowmeld Cloak first-attack-dodge passive has fired this combat.</summary>
    public bool ShadowmeldUsedThisCombat { get; set; } = false;

    /// <summary>Tracks whether the Ring of Warding +DEF passive has fired this combat.</summary>
    public bool WardingRingActivated { get; set; } = false;

    /// <summary>Tracks whether the Amulet of the Phoenix revive passive has fired this dungeon run.</summary>
    public bool PhoenixUsedThisRun { get; set; } = false;

    /// <summary>Tracks whether the bonus flee granted by Boots of the Windrunner has been used this combat.</summary>
    public bool BonusFleeUsed { get; set; } = false;

    /// <summary>Number of extra free flee attempts granted by passive effects this combat.</summary>
    public int ExtraFleeCount { get; set; } = 0;

    /// <summary>Counter used by the Shadowstalker 3-piece set bonus — auto-crit on every 3rd attack.</summary>
    public int ShadowDanceCounter { get; set; } = 0;

    // ── Class passive state (reset each combat) ───────────────────────────────────────

    /// <summary>Warrior: current Battle Hardened stacks (max 4, each granting +2 ATK).</summary>
    public int BattleHardenedStacks { get; set; } = 0;

    /// <summary>Mage: true when the next ability costs 1 less mana (set after spending mana).</summary>
    public bool ArcaneSurgeReady { get; set; } = false;

    /// <summary>Rogue: true when Shadow Strike first-attack double-damage is available.</summary>
    public bool ShadowStrikeReady { get; set; } = true;

    /// <summary>Paladin: true once Divine Bulwark (Fortified at &lt;25% HP) has already fired this combat.</summary>
    public bool DivineBulwarkFired { get; set; } = false;

    /// <summary>
    /// Resets all class passive state to its default at the start of each combat.
    /// Also reverses any Battle Hardened ATK bonus accumulated during the previous fight.
    /// </summary>
    public void ResetCombatPassives()
    {
        if (BattleHardenedStacks > 0)
            ModifyAttack(-BattleHardenedStacks * 2);
        BattleHardenedStacks = 0;
        ArcaneSurgeReady = false;
        ShadowStrikeReady = true;
        DivineBulwarkFired = false;
    }

    // ── 4-piece set bonus flags (set by SetBonusManager.ApplySetBonuses) ──────

    /// <summary>Ironclad 4-piece: fraction of incoming damage reflected back to the attacker (e.g. 0.1 = 10%).</summary>
    public float DamageReflectPercent { get; set; } = 0f;

    /// <summary>Shadowstep 4-piece: guaranteed Bleed application on every hit.</summary>
    public bool SetBonusAppliesBleed { get; set; } = false;

    /// <summary>Arcane Ascendant 4-piece: flat mana discount applied to all ability costs.</summary>
    public int ManaDiscount { get; set; } = 0;

    /// <summary>Sentinel 4-piece: player cannot be stunned.</summary>
    public bool IsStunImmune { get; set; } = false;
}
