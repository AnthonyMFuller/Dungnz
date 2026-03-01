namespace Dungnz.Systems;
using Dungnz.Models;

/// <summary>Passive skills that can be unlocked by a player once they reach the required level.</summary>
public enum Skill
{
    /// <summary>Increases the player's attack damage by 15% on each hit.</summary>
    PowerStrike,    // +15% damage on attacks
    /// <summary>Permanently adds 3 to the player's defense stat when unlocked.</summary>
    IronSkin,       // +3 Defense permanently
    /// <summary>Adds a 5% flat bonus to the player's dodge chance.</summary>
    Swiftness,      // +0.05 dodge bonus
    /// <summary>Increases maximum mana by 10 and grants 5 mana regeneration per turn.</summary>
    ManaFlow,       // +10 max mana + mana regen +5/turn
    /// <summary>Reduces all incoming damage by 5%.</summary>
    BattleHardened, // Take 5% less damage

    // Warrior passives
    /// <summary>Warrior passive: +15 MaxHP, +5% damage reduction.</summary>
    IronConstitution,
    /// <summary>Warrior passive: When HP drops below 25%, gain Regen for 2 turns (once per combat).</summary>
    UndyingWill,
    /// <summary>Warrior passive: +10% damage per 25% HP missing (stacks, max +40%).</summary>
    BerserkersEdge,
    /// <summary>Warrior passive: Last Stand ability can be activated at ≤50% HP instead of 40%.</summary>
    Unbreakable,

    // Mage passives
    /// <summary>Mage passive: +20 MaxMana.</summary>
    ArcaneReservoir,
    /// <summary>Mage passive: All spells cost 10% less mana (rounded down, min 1).</summary>
    SpellWeaver,
    /// <summary>Mage passive: Mana regen per turn +5.</summary>
    LeyConduit,
    /// <summary>Mage passive: When mana > 80% max, spell damage +25%.</summary>
    Overcharge,

    // Rogue passives
    /// <summary>Rogue passive: +5% dodge chance.</summary>
    QuickReflexes,
    /// <summary>Rogue passive: Backstab conditional bonus also triggers on Poison (not just Slow/Stun/Bleed).</summary>
    Opportunist,
    /// <summary>Rogue passive: Flurry and Assassinate cooldowns reduced by 1.</summary>
    Relentless,
    /// <summary>Rogue passive: Evade grants 2 Combo Points instead of 1.</summary>
    ShadowMaster,

    // Paladin passives
    /// <summary>Paladin passive: +3 DEF permanently on unlock.</summary>
    BlessedArmor,
    /// <summary>Paladin passive: Reduce all incoming damage by 5%.</summary>
    AuraOfProtection,
    /// <summary>Paladin passive: HolyStrike and Consecrate deal +15% bonus damage.</summary>
    HolyFervor,
    /// <summary>Paladin passive: When HP &lt; 20%: ATK +20%, all heals +25%.</summary>
    MartyrResolve,

    // Necromancer passives
    /// <summary>Necromancer passive: Minions have +20% HP.</summary>
    UndyingServants,
    /// <summary>Necromancer passive: LifeDrain heals +15% extra.</summary>
    VampiricTouch,
    /// <summary>Necromancer passive: Raise Dead cooldown -1; minions have +10% ATK.</summary>
    MasterOfDeath,
    /// <summary>Necromancer passive: When HP &lt; 15%, all abilities cost 0 mana for 1 turn.</summary>
    LichsBargain,

    // Ranger passives
    /// <summary>Ranger passive: PreciseShot deals +10% damage.</summary>
    KeenEye,
    /// <summary>Ranger passive: Wolf companion ATK +15%.</summary>
    PackTactics,
    /// <summary>Ranger passive: Traps can trigger twice before expiring.</summary>
    TrapMastery,
    /// <summary>Ranger passive: If enemy HP &lt; 40%, all Ranger attacks deal +20% damage.</summary>
    ApexPredator,
}

/// <summary>
/// Tracks which passive skills the player has unlocked and applies their stat bonuses.
/// Skills are gated by minimum player level and can only be unlocked once per run.
/// </summary>
public class SkillTree
{
    private readonly HashSet<Skill> _unlocked = new();

    /// <summary>Returns <see langword="true"/> if the specified skill has been unlocked.</summary>
    /// <param name="skill">The skill to query.</param>
    public bool IsUnlocked(Skill skill) => _unlocked.Contains(skill);

    /// <summary>
    /// Directly marks <paramref name="skill"/> as unlocked without a level check or re-applying
    /// stat bonuses. Used by the save system to restore persisted skill state on load.
    /// </summary>
    /// <param name="skill">The skill to mark as learned.</param>
    public void Unlock(Skill skill) => _unlocked.Add(skill);

    /// <summary>Gets a read-only collection of all skills that have been unlocked.</summary>
    public IReadOnlyCollection<Skill> UnlockedSkills => _unlocked;

    /// <summary>
    /// Attempts to unlock the specified skill for the player. Fails if the skill is already
    /// unlocked or the player has not yet reached the required level, or if the skill has
    /// a class restriction that doesn't match the player's class.
    /// </summary>
    /// <param name="player">The player attempting to unlock the skill.</param>
    /// <param name="skill">The skill to unlock.</param>
    /// <returns><see langword="true"/> if the skill was successfully unlocked; otherwise <see langword="false"/>.</returns>
    public bool TryUnlock(Player player, Skill skill)
    {
        if (_unlocked.Contains(skill)) return false;
        
        var (minLevel, classRestriction) = GetSkillRequirements(skill);
        
        if (player.Level < minLevel) return false;
        if (classRestriction != null && classRestriction != player.Class) return false;
        
        _unlocked.Add(skill);
        ApplySkillBonus(player, skill);
        return true;
    }

    /// <summary>
    /// Returns the minimum level and class restriction for a given skill.
    /// </summary>
    private static (int minLevel, PlayerClass? classRestriction) GetSkillRequirements(Skill skill)
    {
        return skill switch
        {
            // Universal skills (no class restriction)
            Skill.PowerStrike => (3, null),
            Skill.IronSkin => (3, null),
            Skill.Swiftness => (5, null),
            Skill.ManaFlow => (4, null),
            Skill.BattleHardened => (6, null),

            // Warrior passives
            Skill.IronConstitution => (3, PlayerClass.Warrior),
            Skill.UndyingWill => (5, PlayerClass.Warrior),
            Skill.BerserkersEdge => (6, PlayerClass.Warrior),
            Skill.Unbreakable => (8, PlayerClass.Warrior),

            // Mage passives
            Skill.ArcaneReservoir => (3, PlayerClass.Mage),
            Skill.SpellWeaver => (4, PlayerClass.Mage),
            Skill.LeyConduit => (6, PlayerClass.Mage),
            Skill.Overcharge => (8, PlayerClass.Mage),

            // Rogue passives
            Skill.QuickReflexes => (3, PlayerClass.Rogue),
            Skill.Opportunist => (4, PlayerClass.Rogue),
            Skill.Relentless => (6, PlayerClass.Rogue),
            Skill.ShadowMaster => (8, PlayerClass.Rogue),

            // Paladin passives
            Skill.BlessedArmor => (3, PlayerClass.Paladin),
            Skill.AuraOfProtection => (4, PlayerClass.Paladin),
            Skill.HolyFervor => (6, PlayerClass.Paladin),
            Skill.MartyrResolve => (8, PlayerClass.Paladin),

            // Necromancer passives
            Skill.UndyingServants => (3, PlayerClass.Necromancer),
            Skill.VampiricTouch => (4, PlayerClass.Necromancer),
            Skill.MasterOfDeath => (6, PlayerClass.Necromancer),
            Skill.LichsBargain => (8, PlayerClass.Necromancer),

            // Ranger passives
            Skill.KeenEye => (3, PlayerClass.Ranger),
            Skill.PackTactics => (4, PlayerClass.Ranger),
            Skill.TrapMastery => (6, PlayerClass.Ranger),
            Skill.ApexPredator => (8, PlayerClass.Ranger),

            _ => (1, null)
        };
    }

    private void ApplySkillBonus(Player player, Skill skill)
    {
        switch (skill)
        {
            case Skill.IronSkin:
                player.ModifyDefense(3);
                break;
            case Skill.ManaFlow:
                player.MaxMana += 10;
                player.Mana = Math.Min(player.Mana + 10, player.MaxMana);
                break;
            case Skill.IronConstitution:
                player.FortifyMaxHP(15);
                break;
            case Skill.ArcaneReservoir:
                player.FortifyMaxMana(20);
                break;
            case Skill.BlessedArmor:
                player.ModifyDefense(3);
                break;
        }
        // PowerStrike, Swiftness, BattleHardened, and class-specific combat passives are applied in combat
    }

    /// <summary>
    /// Returns a short human-readable description of the passive bonus granted by the given skill.
    /// </summary>
    /// <param name="skill">The skill to describe.</param>
    /// <returns>A string describing the skill's effect.</returns>
    public static string GetDescription(Skill skill) => skill switch {
        Skill.PowerStrike => "+15% attack damage",
        Skill.IronSkin => "+3 Defense (immediate)",
        Skill.Swiftness => "+5% dodge chance",
        Skill.ManaFlow => "+10 max mana, +5 mana/turn",
        Skill.BattleHardened => "Take 5% less damage",

        // Warrior passives
        Skill.IronConstitution => "Years of being hit have made you hard to kill. (+15 MaxHP, +5% damage reduction)",
        Skill.UndyingWill => "The body gives out. The will does not. (Regen when HP < 25%)",
        Skill.BerserkersEdge => "The closer to death, the more dangerous you become. (+10% damage per 25% HP missing)",
        Skill.Unbreakable => "When the odds are worst, you find another gear. (Last Stand at ≤50% HP)",

        // Mage passives
        Skill.ArcaneReservoir => "You've learned to hold more of the void inside you. (+20 MaxMana)",
        Skill.SpellWeaver => "Magic bends to your will, not the other way around. (Spells cost 10% less mana)",
        Skill.LeyConduit => "The dungeon's energy bleeds into you with every breath. (+5 mana regen/turn)",
        Skill.Overcharge => "A full well casts the longest shadow. (+25% spell damage when mana > 80%)",

        // Rogue passives
        Skill.QuickReflexes => "You've survived this long by moving first. (+5% dodge)",
        Skill.Opportunist => "You never let an opening go to waste. (Backstab bonus on Poisoned enemies)",
        Skill.Relentless => "Rest is for the dead. (Flurry/Assassinate cooldowns -1)",
        Skill.ShadowMaster => "You were never really there. (Evade grants 2 Combo Points)",

        // Paladin passives
        Skill.BlessedArmor => "Your armor is blessed with holy light. (+3 Defense)",
        Skill.AuraOfProtection => "A holy aura shields you from harm. (5% damage reduction)",
        Skill.HolyFervor => "Holy power surges through your strikes. (HolyStrike and Consecrate +15% damage)",
        Skill.MartyrResolve => "The closer to death, the more righteous your wrath. (HP < 20%: ATK +20%, heals +25%)",

        // Necromancer passives
        Skill.UndyingServants => "Your servants refuse to truly die. (Minion HP +20%)",
        Skill.VampiricTouch => "You drink deeply from your fallen foes. (LifeDrain heals +15% extra)",
        Skill.MasterOfDeath => "Death itself bends to your will. (RaiseDead CD -1, minion ATK +10%)",
        Skill.LichsBargain => "The lich offers power at a terrible price. (HP < 15%: free abilities for 1 turn)",

        // Ranger passives
        Skill.KeenEye => "Years of hunting have sharpened your aim. (PreciseShot +10% damage)",
        Skill.PackTactics => "You and your wolf move as one. (Wolf ATK +15%)",
        Skill.TrapMastery => "You've learned to make your traps last. (Traps trigger twice)",
        Skill.ApexPredator => "You always finish what you start. (Enemy HP < 40%: +20% damage)",

        _ => ""
    };

    /// <summary>
    /// Returns a list of all skills available to the specified player class at the given level.
    /// Includes universal skills and class-specific skills.
    /// </summary>
    public static List<Skill> GetAvailableSkills(Player player)
    {
        var allSkills = Enum.GetValues<Skill>();
        return allSkills
            .Where(s => {
                var (minLevel, classRestriction) = GetSkillRequirements(s);
                return player.Level >= minLevel && 
                       (classRestriction == null || classRestriction == player.Class);
            })
            .ToList();
    }
}
