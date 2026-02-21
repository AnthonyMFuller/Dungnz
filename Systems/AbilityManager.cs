namespace Dungnz.Systems;
using Dungnz.Models;
using Dungnz.Display;

/// <summary>
/// Manages the player's combat abilities: tracking unlock eligibility by level, enforcing
/// cooldown turns, verifying mana costs, and executing ability effects during combat.
/// </summary>
public class AbilityManager
{
    private readonly Dictionary<AbilityType, int> _cooldowns = new();
    private readonly List<Ability> _abilities;
    
    /// <summary>
    /// Initialises the manager and registers all available abilities with their names,
    /// descriptions, mana costs, cooldown turns, unlock levels, and ability types.
    /// </summary>
    public AbilityManager()
    {
        _abilities = new List<Ability>
        {
            new Ability("Power Strike", "Deal 2x normal damage", 10, 2, 1, AbilityType.PowerStrike),
            new Ability("Defensive Stance", "+50% DEF for 2 turns", 8, 3, 3, AbilityType.DefensiveStance),
            new Ability("Poison Dart", "Apply Poison status effect", 12, 4, 5, AbilityType.PoisonDart),
            new Ability("Second Wind", "Heal 30% of MaxHP", 15, 5, 7, AbilityType.SecondWind)
        };
    }
    
    /// <summary>
    /// Returns all abilities whose required unlock level is less than or equal to the player's
    /// current level, regardless of cooldown or mana.
    /// </summary>
    /// <param name="player">The player whose level determines which abilities are accessible.</param>
    /// <returns>A list of abilities the player has unlocked.</returns>
    public List<Ability> GetUnlockedAbilities(Player player)
    {
        return _abilities.Where(a => a.UnlockLevel <= player.Level).ToList();
    }
    
    /// <summary>
    /// Returns the subset of unlocked abilities that are not currently on cooldown and whose
    /// mana cost the player can afford right now.
    /// </summary>
    /// <param name="player">The player whose level and current mana are checked.</param>
    /// <returns>A list of abilities ready to use this turn.</returns>
    public List<Ability> GetAvailableAbilities(Player player)
    {
        return GetUnlockedAbilities(player)
            .Where(a => !IsOnCooldown(a.Type) && player.Mana >= a.ManaCost)
            .ToList();
    }
    
    /// <summary>
    /// Returns <see langword="true"/> if the specified ability type has remaining cooldown turns and cannot be used.
    /// </summary>
    /// <param name="type">The ability type to check.</param>
    /// <returns><see langword="true"/> if the ability is on cooldown; otherwise <see langword="false"/>.</returns>
    public bool IsOnCooldown(AbilityType type)
    {
        return _cooldowns.ContainsKey(type) && _cooldowns[type] > 0;
    }
    
    /// <summary>
    /// Returns the number of turns remaining before the specified ability can be used again.
    /// Returns 0 if the ability is not on cooldown.
    /// </summary>
    /// <param name="type">The ability type to query.</param>
    /// <returns>Remaining cooldown turns, or 0 if ready.</returns>
    public int GetCooldown(AbilityType type)
    {
        return _cooldowns.ContainsKey(type) ? _cooldowns[type] : 0;
    }
    
    /// <summary>
    /// Registers the specified ability as being on cooldown for the given number of turns,
    /// preventing its use until <see cref="TickCooldowns"/> has been called that many times.
    /// </summary>
    /// <param name="type">The ability type to put on cooldown.</param>
    /// <param name="turns">The number of turns before the ability becomes available again.</param>
    public void PutOnCooldown(AbilityType type, int turns)
    {
        _cooldowns[type] = turns;
    }
    
    /// <summary>
    /// Decrements the remaining cooldown for every ability currently on cooldown by one turn.
    /// Should be called once at the end of each player turn.
    /// </summary>
    public void TickCooldowns()
    {
        var keys = _cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            if (_cooldowns[key] > 0)
                _cooldowns[key]--;
        }
    }
    
    /// <summary>
    /// Looks up and returns the <see cref="Ability"/> definition for the given type,
    /// or <see langword="null"/> if no matching ability is registered.
    /// </summary>
    /// <param name="type">The ability type to retrieve.</param>
    /// <returns>The matching <see cref="Ability"/>, or <see langword="null"/> if not found.</returns>
    public Ability? GetAbility(AbilityType type)
    {
        return _abilities.FirstOrDefault(a => a.Type == type);
    }
    
    /// <summary>
    /// Attempts to use the specified ability in the current combat encounter. Validates level,
    /// cooldown, and mana requirements before spending mana, applying the cooldown, and executing
    /// the ability's combat effect (damage, buff, debuff, or healing).
    /// </summary>
    /// <param name="player">The player attempting to use the ability.</param>
    /// <param name="enemy">The current enemy target for offensive abilities.</param>
    /// <param name="type">The ability type to execute.</param>
    /// <param name="statusEffects">The active status effect manager for applying buffs and debuffs.</param>
    /// <param name="display">The display service for emitting combat feedback messages.</param>
    /// <returns>A <see cref="UseAbilityResult"/> indicating whether the ability was used or why it failed.</returns>
    public UseAbilityResult UseAbility(Player player, Enemy enemy, AbilityType type, StatusEffectManager statusEffects, IDisplayService display)
    {
        var ability = GetAbility(type);
        if (ability == null)
            return UseAbilityResult.InvalidAbility;
        
        if (ability.UnlockLevel > player.Level)
            return UseAbilityResult.NotUnlocked;
        
        if (IsOnCooldown(type))
            return UseAbilityResult.OnCooldown;
        
        if (player.Mana < ability.ManaCost)
            return UseAbilityResult.InsufficientMana;
        
        player.SpendMana(ability.ManaCost);
        PutOnCooldown(type, ability.CooldownTurns);
        
        switch (type)
        {
            case AbilityType.PowerStrike:
                var damage = Math.Max(1, player.Attack * 2 - enemy.Defense);
                enemy.HP -= damage;
                display.ShowCombatMessage($"Power Strike! You deal {damage} damage to {enemy.Name}!");
                break;
                
            case AbilityType.DefensiveStance:
                statusEffects.Apply(player, StatusEffect.Fortified, 2);
                display.ShowCombatMessage("Defensive Stance activated! Your defense is boosted!");
                break;
                
            case AbilityType.PoisonDart:
                statusEffects.Apply(enemy, StatusEffect.Poison, 3);
                display.ShowCombatMessage($"Poison Dart! {enemy.Name} is poisoned!");
                break;
                
            case AbilityType.SecondWind:
                var healAmount = (int)(player.MaxHP * 0.3);
                player.Heal(healAmount);
                display.ShowCombatMessage($"Second Wind! You heal {healAmount} HP!");
                break;
        }
        
        return UseAbilityResult.Success;
    }
}

/// <summary>Represents the outcome of a <see cref="AbilityManager.UseAbility"/> call.</summary>
public enum UseAbilityResult
{
    /// <summary>The ability was used successfully and its effects have been applied.</summary>
    Success,

    /// <summary>No ability with the requested type exists in the manager's registry.</summary>
    InvalidAbility,

    /// <summary>The player has not yet reached the level required to unlock this ability.</summary>
    NotUnlocked,

    /// <summary>The ability was used recently and must wait for its cooldown to expire.</summary>
    OnCooldown,

    /// <summary>The player does not have enough mana to activate the ability.</summary>
    InsufficientMana
}
