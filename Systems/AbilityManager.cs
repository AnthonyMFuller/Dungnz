namespace Dungnz.Systems;
using Dungnz.Models;
using Dungnz.Display;

public class AbilityManager
{
    private readonly Dictionary<AbilityType, int> _cooldowns = new();
    private readonly List<Ability> _abilities;
    
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
    
    public List<Ability> GetUnlockedAbilities(Player player)
    {
        return _abilities.Where(a => a.UnlockLevel <= player.Level).ToList();
    }
    
    public List<Ability> GetAvailableAbilities(Player player)
    {
        return GetUnlockedAbilities(player)
            .Where(a => !IsOnCooldown(a.Type) && player.Mana >= a.ManaCost)
            .ToList();
    }
    
    public bool IsOnCooldown(AbilityType type)
    {
        return _cooldowns.ContainsKey(type) && _cooldowns[type] > 0;
    }
    
    public int GetCooldown(AbilityType type)
    {
        return _cooldowns.ContainsKey(type) ? _cooldowns[type] : 0;
    }
    
    public void PutOnCooldown(AbilityType type, int turns)
    {
        _cooldowns[type] = turns;
    }
    
    public void TickCooldowns()
    {
        var keys = _cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            if (_cooldowns[key] > 0)
                _cooldowns[key]--;
        }
    }
    
    public Ability? GetAbility(AbilityType type)
    {
        return _abilities.FirstOrDefault(a => a.Type == type);
    }
    
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

public enum UseAbilityResult
{
    Success,
    InvalidAbility,
    NotUnlocked,
    OnCooldown,
    InsufficientMana
}
