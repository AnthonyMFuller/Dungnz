namespace Dungnz.Engine;
using Dungnz.Data;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;

/// <summary>
/// Resolves all player-attack calculations: dodge rolls, crit rolls, damage formulas,
/// passive bonuses, and bleed-on-hit. Migrated from <see cref="CombatEngine"/> in #1204.
/// </summary>
public class AttackResolver : IAttackResolver
{
    private readonly IDisplayService _display;
    private readonly Random _rng;
    private readonly StatusEffectManager _statusEffects;
    private readonly NarrationService _narration;
    private readonly PassiveEffectProcessor _passives;
    private readonly DifficultySettings _difficulty;
    private readonly List<CombatTurn> _turnLog;
    private RunStats _stats = new();

    /// <inheritdoc/>
    public int CombatTurn { get; set; }

    /// <summary>Initialises a new <see cref="AttackResolver"/> with the required dependencies.</summary>
    public AttackResolver(
        IDisplayService display,
        Random rng,
        StatusEffectManager statusEffects,
        NarrationService narration,
        PassiveEffectProcessor passives,
        DifficultySettings difficulty,
        List<CombatTurn> turnLog)
    {
        _display = display;
        _rng = rng;
        _statusEffects = statusEffects;
        _narration = narration;
        _passives = passives;
        _difficulty = difficulty;
        _turnLog = turnLog;
    }

    /// <inheritdoc/>
    public void SetStats(RunStats stats) => _stats = stats;

    private static string ReplaceLastOccurrence(string source, string find, string replace)
    {
        int lastIndex = source.LastIndexOf(find);
        if (lastIndex < 0) return source;
        return source.Substring(0, lastIndex) + replace + source.Substring(lastIndex + find.Length);
    }

    private string ColorizeDamage(string message, int damage, bool isCrit = false, bool isHealing = false)
    {
        var damageStr = damage.ToString();
        var coloredDamage = isHealing
            ? ColorCodes.Colorize(damageStr, ColorCodes.Green)
            : ColorCodes.Colorize(damageStr, ColorCodes.BrightRed);
        if (isCrit)
            return ColorCodes.Colorize(ReplaceLastOccurrence(message, damageStr, coloredDamage), ColorCodes.Yellow + ColorCodes.Bold);
        return ReplaceLastOccurrence(message, damageStr, coloredDamage);
    }

    /// <inheritdoc/>
    public void PerformPlayerAttack(Player player, Enemy enemy)
    {
        // AbyssalLeviathan submerge: player's attack is skipped
        if (enemy.IsSubmerged)
        {
            _display.ShowCombatMessage("The Leviathan submerges — your attack meets only water!");
            enemy.IsSubmerged = false;
            return;
        }

        // ArchlichSovereign / DamageImmune: redirect hit to adds
        if (enemy.DamageImmune && enemy.AddsAlive > 0)
        {
            _display.ShowCombatMessage("Your attack strikes one of the skeletal guardians!");
            enemy.AddsAlive--;
            if (enemy.AddsAlive == 0)
            {
                enemy.DamageImmune = false;
                _display.ShowCombatMessage("The last guardian falls! The boss is vulnerable again!");
            }
            return;
        }

        // InfernalDragon flight phase: 40% miss chance
        if (enemy.FlightPhaseActive && _rng.NextDouble() < 0.40)
        {
            _display.ShowCombatMessage("The dragon banks away — your attack misses!");
            _turnLog.Add(new CombatTurn("You", "Attack", 0, false, true, null));
            return;
        }

        // Use flat dodge chance for enemies like Wraith, otherwise DEF-based
        bool dodged = enemy.FlatDodgeChance >= 0
            ? _rng.NextDouble() < enemy.FlatDodgeChance
            : RollDodge(enemy.Defense);

        if (dodged)
        {
            var missPool = player.Class switch {
                PlayerClass.Warrior => CombatNarration.WarriorMissMessages,
                PlayerClass.Mage    => CombatNarration.MageMissMessages,
                PlayerClass.Rogue   => CombatNarration.RogueMissMessages,
                _                   => CombatNarration.PlayerMissMessages
            };
            _display.ShowCombatMessage(_narration.Pick(missPool, enemy.Name));
            _turnLog.Add(new CombatTurn("You", "Attack", 0, false, true, null));

            // BladeDancer: 50% counter on player dodge
            if (enemy.OnDodgeCounterChance > 0 && _rng.NextDouble() < enemy.OnDodgeCounterChance)
            {
                _display.ShowCombatMessage($"The {enemy.Name} spins and counters your missed attack!");
                var counterDmg = Math.Max(1, enemy.Attack - player.Defense);
                player.TakeDamage(counterDmg);
                _stats.DamageTaken += counterDmg;
                _display.ShowCombatMessage(ColorizeDamage($"{enemy.Name} deals {counterDmg} counter damage!", counterDmg));
            }
        }
        else
        {
            var playerEffAtk = player.Attack + _statusEffects.GetStatModifier(player, "Attack");
            int attackBonus = SetBonusManager.GetActiveBonuses(player).Sum(b => b.AttackBonus);
            playerEffAtk += attackBonus;
            var effectiveDef = Math.Max(0, enemy.Defense - player.EnemyDefReduction);
            var playerDmg = Math.Max(1, playerEffAtk - effectiveDef);

            // SiegeOgre thick hide
            if (enemy.ThickHideHitsRemaining > 0)
            {
                playerDmg = Math.Max(1, playerDmg - enemy.ThickHideDamageReduction);
                enemy.ThickHideHitsRemaining--;
                if (enemy.ThickHideHitsRemaining == 0)
                    _display.ShowCombatMessage($"You break through the {enemy.Name}'s thick hide!");
                else
                    _display.ShowCombatMessage($"The {enemy.Name}'s thick hide absorbs some of the blow!");
            }

            var isCrit = RollCrit(player);
            if (isCrit)
            {
                playerDmg *= 2;
            }
            // Warrior passive: +5% damage when HP < 50%
            if (player.Class == PlayerClass.Warrior && player.HP < player.MaxHP / 2.0)
                playerDmg = (int)(playerDmg * 1.05);
            // Bug #86: PowerStrike skill passive — +15% damage
            if (player.Skills.IsUnlocked(Skill.PowerStrike))
                playerDmg = Math.Max(1, (int)(playerDmg * 1.15));
            // Berserker's Edge passive: +10% damage per 25% HP missing
            if (player.Skills.IsUnlocked(Skill.BerserkersEdge))
            {
                var hpPercent = (float)player.HP / player.MaxHP;
                var multiplier = 1.0f;
                if (hpPercent <= 0.25f) multiplier = 1.40f;      // 75% missing = +40%
                else if (hpPercent <= 0.50f) multiplier = 1.30f; // 50% missing = +30%
                else if (hpPercent <= 0.75f) multiplier = 1.20f; // 25% missing = +20%
                else multiplier = 1.0f;                          // <25% missing = no bonus
                playerDmg = Math.Max(1, (int)(playerDmg * multiplier));
            }
            // Last Stand damage boost — +50% damage
            if (player.LastStandTurns > 0)
                playerDmg = Math.Max(1, (int)(playerDmg * 1.5f));
            // MartyrResolve passive (Paladin) — ATK +20% when HP < 20%
            if (player.Skills.IsUnlocked(Skill.MartyrResolve) && player.HP < player.MaxHP * 0.20f)
                playerDmg = Math.Max(1, (int)(playerDmg * 1.20));
            // ApexPredator passive (Ranger) — +20% when enemy HP < 40%
            if (player.Skills.IsUnlocked(Skill.ApexPredator) && enemy.HP < enemy.MaxHP * 0.40f)
                playerDmg = Math.Max(1, (int)(playerDmg * 1.20));
            // Hunter's Mark passive (Ranger) — first attack +25%
            if (player.Class == PlayerClass.Ranger && !player.HunterMarkUsedThisCombat)
            {
                player.HunterMarkUsedThisCombat = true;
                playerDmg = Math.Max(1, (int)(playerDmg * 1.25));
                _display.ShowCombatMessage("🎯 Hunter's Mark! First strike deals bonus damage!");
            }

            // Shadow Strike (Rogue): first attack each combat deals 2x damage
            if (player.Class == PlayerClass.Rogue && player.ShadowStrikeReady)
            {
                playerDmg *= 2;
                player.ShadowStrikeReady = false;
                _display.ShowCombatMessage("[Shadow Strike] From the shadows — double damage!");
            }

            // IronSentinel: 50% damage reduction from plating
            if (enemy is IronSentinel sentinel)
                playerDmg = Math.Max(1, (int)(playerDmg * (1.0 - sentinel.ProtectionDR)));

            // Holy damage bonus vs undead enemies
            if (enemy.IsUndead && player.HolyDamageVsUndead > 0f)
            {
                playerDmg = Math.Max(1, (int)(playerDmg * (1f + player.HolyDamageVsUndead)));
                _display.ShowColoredCombatMessage($"✨ Holy damage — +{(int)(player.HolyDamageVsUndead * 100)}% vs undead!", ColorCodes.Yellow);
            }
            playerDmg = Math.Max(1, (int)(playerDmg * _difficulty.PlayerDamageMultiplier));
            enemy.HP = Math.Max(0, enemy.HP - playerDmg);
            _stats.DamageDealt += playerDmg;

            // HPOnHit: heal player for aggregate equipped-item HP-on-hit value
            int hpOnHit = (int)((player.EquippedWeapon?.HPOnHit ?? 0)
                        + (player.EquippedAccessory?.HPOnHit ?? 0)
                        + player.AllEquippedArmor.Sum(a => a.HPOnHit));
            if (hpOnHit > 0 && player.HP < player.MaxHP)
            {
                player.Heal(hpOnHit);
                _display.ShowColoredCombatMessage($"💚 HP on Hit: +{hpOnHit} HP", ColorCodes.Green);
            }

            // Fix #542: physical damage breaks Freeze
            _statusEffects.NotifyPhysicalDamage(enemy);

            // ── Passive effects: on player hit ──────────────────────────────
            if (!enemy.IsDead)
                _passives.ProcessPassiveEffects(player, PassiveEffectTrigger.OnPlayerHit, enemy, playerDmg);
            else
            {
                // Soul Harvest (Necromancer): heal 5 HP on enemy kill
                if (player.Class == PlayerClass.Necromancer)
                {
                    player.Heal(5);
                    _display.ShowCombatMessage("[Soul Harvest] You absorb the fallen's essence. +5 HP");
                }
                // on-kill bonus damage from thunderstrike
                int killBonus = _passives.ProcessPassiveEffects(player, PassiveEffectTrigger.OnEnemyKilled, enemy, playerDmg);
                if (killBonus > 0) _stats.DamageDealt += killBonus;
            }

            var hitPool = player.Class switch {
                PlayerClass.Warrior => CombatNarration.WarriorHitMessages,
                PlayerClass.Mage    => CombatNarration.MageHitMessages,
                PlayerClass.Rogue   => CombatNarration.RogueHitMessages,
                PlayerClass.Paladin => CombatNarration.PaladinHitMessages,
                PlayerClass.Necromancer => CombatNarration.NecromancerHitMessages,
                PlayerClass.Ranger  => CombatNarration.RangerHitMessages,
                _                   => CombatNarration.PlayerHitMessages
            };
            var critPool = player.Class switch {
                PlayerClass.Warrior => CombatNarration.WarriorCritMessages,
                PlayerClass.Mage    => CombatNarration.MageCritMessages,
                PlayerClass.Rogue   => CombatNarration.RogueCritMessages,
                _                   => CombatNarration.CritMessages
            };
            if (isCrit)
            {
                _display.ShowCombatMessage(ColorizeDamage(_narration.Pick(critPool, enemy.Name, playerDmg), playerDmg, true));
                _display.ShowCombatMessage(_narration.Pick(CombatNarration.CritFlavor));
            }
            else
                _display.ShowCombatMessage(ColorizeDamage(_narration.Pick(hitPool, enemy.Name, playerDmg), playerDmg));

            // Killing-blow atmospheric flavor
            if (enemy.IsDead)
            {
                var killPool = player.Class switch
                {
                    PlayerClass.Warrior or PlayerClass.Paladin => CombatNarration.KillMelee,
                    PlayerClass.Ranger                         => CombatNarration.KillRanged,
                    PlayerClass.Mage or PlayerClass.Necromancer => CombatNarration.KillMagic,
                    _                                          => CombatNarration.KillGeneric
                };
                _display.ShowCombatMessage(_narration.Pick(killPool));
            }

            string? statusApplied = null;
            // Bug #110: bleed-on-hit from equipped weapon (10% chance, 3 turns)
            if (player.EquippedWeaponAppliesBleed && _rng.NextDouble() < 0.10)
            {
                _statusEffects.Apply(enemy, StatusEffect.Bleed, 3);
                statusApplied = "Bleed";
                _display.ShowColoredCombatMessage($"{enemy.Name} is bleeding!", ColorCodes.Red);
            }
            // Shadowstep 4-pc set bonus: guaranteed bleed on every hit
            if (player.SetBonusAppliesBleed && !enemy.IsDead)
            {
                _statusEffects.Apply(enemy, StatusEffect.Bleed, 3);
                statusApplied ??= "Bleed";
                _display.ShowColoredCombatMessage($"[Shadowstep] {enemy.Name} is bleeding!", ColorCodes.Red);
            }
            _turnLog.Add(new CombatTurn("You", "Attack", playerDmg, isCrit, false, statusApplied));

            // IronGuard counter-strike: fires AFTER player hits, BEFORE status ticks
            if (!enemy.IsDead && enemy.CounterStrikeChance > 0 && _rng.NextDouble() < enemy.CounterStrikeChance)
            {
                var counterDmg = Math.Max(1, playerDmg / 2);
                player.TakeDamage(counterDmg);
                _stats.DamageTaken += counterDmg;
                _display.ShowCombatMessage(ColorizeDamage($"⚔ The {enemy.Name} counters with a swift riposte — {counterDmg} damage!", counterDmg));
            }
        }
    }

    /// <inheritdoc/>
    public bool RollDodge(int defense)
    {
        var dodgeChance = defense / (double)(defense + 20);
        return _rng.NextDouble() < dodgeChance;
    }

    /// <inheritdoc/>
    public bool RollPlayerDodge(Player player)
    {
        // Bug #85: add flat equipment and class bonuses on top of DEF-based chance
        float dodgeChance = player.Defense / (player.Defense + 20f)
                          + player.DodgeBonus
                          + player.ClassDodgeBonus
                          + player.SetBonusDodge;
        // Bug #86: Swiftness skill passive — +5% dodge chance
        if (player.Skills.IsUnlocked(Skill.Swiftness))
            dodgeChance += 0.05f;
        // Quick Reflexes passive — +5% dodge chance
        if (player.Skills.IsUnlocked(Skill.QuickReflexes))
            dodgeChance += 0.05f;
        // Eagle Eye (Ranger): +15% dodge on turns 1–2
        if (player.Class == PlayerClass.Ranger && CombatTurn <= 2)
            dodgeChance += 0.15f;
        dodgeChance = Math.Min(dodgeChance, 0.95f);
        return _rng.NextDouble() < dodgeChance;
    }

    /// <inheritdoc/>
    public bool RollCrit(Player? player = null)
    {
        float baseCrit = 0.15f;
        if (player != null)
        {
            float bonus = (player.EquippedWeapon?.CritChance ?? 0)
                        + (player.EquippedAccessory?.CritChance ?? 0)
                        + player.AllEquippedArmor.Sum(a => a.CritChance);
            baseCrit += bonus;
        }
        return _rng.NextDouble() < baseCrit;
    }
}
