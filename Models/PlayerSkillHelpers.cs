namespace Dungnz.Models;
using Dungnz.Systems;

public partial class Player
{
    /// <summary>
    /// Checks if the player has unlocked the specified skill.
    /// Used by Phase 3 ability implementations to check passive effects.
    /// </summary>
    public bool HasSkill(Skill skill) => Skills.IsUnlocked(skill);

    /// <summary>
    /// Returns the HP threshold percentage for Last Stand activation.
    /// Default is 40%, but Unbreakable passive raises it to 50%.
    /// </summary>
    public float GetLastStandThreshold()
    {
        return HasSkill(Skill.Unbreakable) ? 0.50f : 0.40f;
    }

    /// <summary>
    /// Returns the cooldown reduction for ability types based on Relentless passive.
    /// Used to reduce Flurry and Assassinate cooldowns by 1 turn.
    /// </summary>
    public int GetCooldownReduction(AbilityType abilityType)
    {
        if (!HasSkill(Skill.Relentless)) return 0;
        
        return abilityType switch
        {
            AbilityType.Flurry => 1,
            AbilityType.Assassinate => 1,
            _ => 0
        };
    }

    /// <summary>
    /// Returns the number of combo points granted by the Evade ability.
    /// Default is 1, but Shadow Master passive increases it to 2.
    /// </summary>
    public int GetEvadeComboPointGrant()
    {
        return HasSkill(Skill.ShadowMaster) ? 2 : 1;
    }

    /// <summary>
    /// Checks if the player should receive a Backstab damage bonus against the given enemy.
    /// Normally requires Stun/Slow/Bleed, but Opportunist passive adds Poison.
    /// </summary>
    public bool ShouldTriggerBackstabBonus(Enemy enemy, Func<Enemy, StatusEffect, bool> hasEffect)
    {
        bool hasCondition = hasEffect(enemy, StatusEffect.Stun) ||
                           hasEffect(enemy, StatusEffect.Slow) ||
                           hasEffect(enemy, StatusEffect.Bleed);
        
        if (HasSkill(Skill.Opportunist))
            hasCondition = hasCondition || hasEffect(enemy, StatusEffect.Poison);
        
        return hasCondition;
    }

    /// <summary>
    /// Calculates the mana cost multiplier from the Spell Weaver passive.
    /// Returns 0.90 (10% reduction) if the skill is unlocked, otherwise 1.0 (no reduction).
    /// </summary>
    public float GetSpellCostMultiplier()
    {
        return HasSkill(Skill.SpellWeaver) ? 0.90f : 1.0f;
    }

    /// <summary>
    /// Checks if the Overcharge passive should trigger (+25% spell damage).
    /// Requires mana > 80% of max mana.
    /// </summary>
    public bool IsOverchargeActive()
    {
        return HasSkill(Skill.Overcharge) && Mana > MaxMana * 0.80f;
    }

    /// <summary>
    /// Checks if Undying Will passive should trigger (HP below 25%).
    /// Returns true if the passive is unlocked and HP is below the threshold.
    /// Phase 3 should track "once per combat" usage separately.
    /// </summary>
    public bool ShouldTriggerUndyingWill()
    {
        return HasSkill(Skill.UndyingWill) && HP < MaxHP * 0.25f;
    }
}
