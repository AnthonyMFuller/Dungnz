namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Display;
using Dungnz.Systems;

public class CombatEngine : ICombatEngine
{
    private readonly IDisplayService _display;
    private readonly IInputReader _input;
    private readonly Random _rng;
    private readonly GameEvents? _events;
    private readonly StatusEffectManager _statusEffects;
    private readonly AbilityManager _abilities;
    
    public CombatEngine(IDisplayService display, IInputReader? input = null, Random? rng = null, GameEvents? events = null, StatusEffectManager? statusEffects = null, AbilityManager? abilities = null)
    {
        _display = display;
        _input = input ?? new ConsoleInputReader();
        _rng = rng ?? new Random();
        _events = events;
        _statusEffects = statusEffects ?? new StatusEffectManager(display);
        _abilities = abilities ?? new AbilityManager();
    }
    
    public CombatResult RunCombat(Player player, Enemy enemy)
    {
        _display.ShowCombat($"A {enemy.Name} attacks!");

        // Ambush: Mimic gets a free first strike before the player can act
        if (enemy.IsAmbush)
        {
            _display.ShowCombatMessage($"It's a {enemy.Name}! You've been ambushed!");
            PerformEnemyTurn(player, enemy);
            if (player.HP <= 0) return CombatResult.PlayerDied;
        }
        
        while (true)
        {
            _statusEffects.ProcessTurnStart(player);
            _statusEffects.ProcessTurnStart(enemy);
            
            player.RestoreMana(10);
            _abilities.TickCooldowns();
            
            if (enemy.HP <= 0)
            {
                _display.ShowCombat($"You defeated the {enemy.Name}!");
                HandleLootAndXP(player, enemy);
                return CombatResult.Won;
            }
            
            if (player.HP <= 0) return CombatResult.PlayerDied;
            
            if (_statusEffects.HasEffect(player, StatusEffect.Stun))
            {
                _display.ShowCombatMessage("You are stunned and cannot act this turn!");
                PerformEnemyTurn(player, enemy);
                if (player.HP <= 0) return CombatResult.PlayerDied;
                continue;
            }
            
            _display.ShowCombatStatus(player, enemy);
            ShowCombatMenu(player);
            var choice = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
            
            if (choice == "F" || choice == "FLEE")
            {
                if (_rng.NextDouble() < 0.5)
                {
                    _display.ShowMessage("You fled successfully!");
                    _statusEffects.Clear(player);
                    _statusEffects.Clear(enemy);
                    return CombatResult.Fled;
                }
                else
                {
                    _display.ShowMessage("You failed to flee!");
                    PerformEnemyTurn(player, enemy);
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                    continue;
                }
            }
            
            if (choice == "B" || choice == "ABILITY")
            {
                var abilityResult = HandleAbilityMenu(player, enemy);
                if (abilityResult == AbilityMenuResult.Cancel)
                    continue;
                if (abilityResult == AbilityMenuResult.Used)
                {
                    if (enemy.HP <= 0)
                    {
                        _display.ShowCombat($"You defeated the {enemy.Name}!");
                        HandleLootAndXP(player, enemy);
                        return CombatResult.Won;
                    }
                    PerformEnemyTurn(player, enemy);
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                    continue;
                }
            }
            
            if (choice == "A" || choice == "ATTACK")
            {
                PerformPlayerAttack(player, enemy);
                
                if (enemy.HP <= 0)
                {
                    _display.ShowCombat($"You defeated the {enemy.Name}!");
                    HandleLootAndXP(player, enemy);
                    return CombatResult.Won;
                }
                
                PerformEnemyTurn(player, enemy);
                if (player.HP <= 0) return CombatResult.PlayerDied;
            }
        }
    }
    
    private void ShowCombatMenu(Player player)
    {
        _display.ShowMessage("[A]ttack [B]ability [I]tem [F]lee");
        var unlockedAbilities = _abilities.GetUnlockedAbilities(player);
        if (unlockedAbilities.Any())
        {
            _display.ShowMessage($"Mana: {player.Mana}/{player.MaxMana}");
        }
    }
    
    private AbilityMenuResult HandleAbilityMenu(Player player, Enemy enemy)
    {
        var unlocked = _abilities.GetUnlockedAbilities(player);
        if (!unlocked.Any())
        {
            _display.ShowMessage("You haven't unlocked any abilities yet!");
            return AbilityMenuResult.Cancel;
        }
        
        _display.ShowMessage("\n=== Abilities ===");
        int index = 1;
        foreach (var ability in unlocked)
        {
            var status = "";
            if (_abilities.IsOnCooldown(ability.Type))
            {
                status = $" (Cooldown: {_abilities.GetCooldown(ability.Type)} turns)";
            }
            else if (player.Mana < ability.ManaCost)
            {
                status = $" (Need {ability.ManaCost} mana)";
            }
            else
            {
                status = $" [{index}]";
            }
            _display.ShowMessage($"{status} {ability.Name} - {ability.Description} (Cost: {ability.ManaCost} MP, CD: {ability.CooldownTurns} turns)");
            index++;
        }
        _display.ShowMessage("[C]ancel");
        
        var choice = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
        if (choice == "C" || choice == "CANCEL")
            return AbilityMenuResult.Cancel;
        
        if (int.TryParse(choice, out int abilityIndex) && abilityIndex >= 1 && abilityIndex <= unlocked.Count)
        {
            var selectedAbility = unlocked[abilityIndex - 1];
            var result = _abilities.UseAbility(player, enemy, selectedAbility.Type, _statusEffects, _display);
            
            if (result == UseAbilityResult.Success)
                return AbilityMenuResult.Used;
            
            _display.ShowMessage($"Cannot use ability: {result}");
            return AbilityMenuResult.Cancel;
        }
        
        _display.ShowMessage("Invalid choice!");
        return AbilityMenuResult.Cancel;
    }
    
    private void PerformPlayerAttack(Player player, Enemy enemy)
    {
        // Use flat dodge chance for enemies like Wraith, otherwise DEF-based
        bool dodged = enemy.FlatDodgeChance >= 0
            ? _rng.NextDouble() < enemy.FlatDodgeChance
            : RollDodge(enemy.Defense);

        if (dodged)
        {
            _display.ShowCombatMessage($"{enemy.Name} dodged your attack!");
        }
        else
        {
            var playerDmg = Math.Max(1, player.Attack - enemy.Defense);
            var isCrit = RollCrit();
            if (isCrit)
            {
                playerDmg *= 2;
                _display.ShowCombatMessage("Critical hit!");
            }
            enemy.HP -= playerDmg;
            _display.ShowCombatMessage($"You hit {enemy.Name} for {playerDmg} damage!");

            // Poison on hit (e.g. Goblin Shaman)
            if (enemy.AppliesPoisonOnHit && !enemy.IsImmuneToEffects)
                _statusEffects.Apply(player, StatusEffect.Poison, 3);
        }
    }
    
    private void PerformEnemyTurn(Player player, Enemy enemy)
    {
        if (_statusEffects.HasEffect(enemy, StatusEffect.Stun))
        {
            _display.ShowCombatMessage($"{enemy.Name} is stunned and cannot act!");
            return;
        }
        
        if (RollDodge(player.Defense))
        {
            _display.ShowCombatMessage("You dodged the attack!");
        }
        else
        {
            var enemyDmg = Math.Max(1, enemy.Attack - player.Defense);
            var isCrit = RollCrit();
            if (isCrit)
            {
                enemyDmg *= 2;
                _display.ShowCombatMessage("Critical hit!");
            }
            player.TakeDamage(enemyDmg);
            _display.ShowCombatMessage($"{enemy.Name} hits you for {enemyDmg} damage!");

            // Lifesteal (e.g. Vampire Lord)
            if (enemy.LifestealPercent > 0)
            {
                var heal = (int)(enemyDmg * enemy.LifestealPercent);
                if (heal > 0)
                {
                    enemy.HP = Math.Min(enemy.MaxHP, enemy.HP + heal);
                    _display.ShowCombatMessage($"{enemy.Name} drains {heal} HP!");
                }
            }
        }
    }
    
    private void HandleLootAndXP(Player player, Enemy enemy)
    {
        var loot = enemy.LootTable.RollDrop(enemy);
        if (loot.Gold > 0)
        {
            player.AddGold(loot.Gold);
            _display.ShowMessage($"You found {loot.Gold} gold!");
        }
        if (loot.Item != null)
        {
            player.Inventory.Add(loot.Item);
            _display.ShowLootDrop(loot.Item);
        }
        
        player.AddXP(enemy.XPValue);
        _display.ShowMessage($"You gained {enemy.XPValue} XP. (Total: {player.XP})");
        CheckLevelUp(player);
        _events?.RaiseCombatEnded(player, enemy, CombatResult.Won);
        _statusEffects.Clear(player);
        _statusEffects.Clear(enemy);
    }
    
    private void CheckLevelUp(Player player)
    {
        var newLevel = player.XP / 100 + 1;
        if (newLevel > player.Level)
        {
            player.LevelUp();
            _display.ShowMessage($"LEVEL UP! You are now level {player.Level}!");
        }
    }
    
    private bool RollDodge(int defense)
    {
        var dodgeChance = defense / (double)(defense + 20);
        return _rng.NextDouble() < dodgeChance;
    }
    
    private bool RollCrit()
    {
        return _rng.NextDouble() < 0.15;
    }
}

public enum AbilityMenuResult
{
    Cancel,
    Used
}
