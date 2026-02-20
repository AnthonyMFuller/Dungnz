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
    private readonly StatusEffectManager? _statusManager;
    
    public CombatEngine(IDisplayService display, IInputReader? input = null, Random? rng = null, GameEvents? events = null, StatusEffectManager? statusManager = null)
    {
        _display = display;
        _input = input ?? new ConsoleInputReader();
        _rng = rng ?? new Random();
        _events = events;
        _statusManager = statusManager;
    }
    
    public CombatResult RunCombat(Player player, Enemy enemy)
    {
        _display.ShowCombat($"A {enemy.Name} attacks!");
        bool isFirstTurn = true;
        
        while (true)
        {
            // Mimic ambush: enemy attacks first on first turn
            if (isFirstTurn && enemy.SpecialAbility == SpecialAbility.Ambush)
            {
                _display.ShowCombatMessage($"{enemy.Name} ambushes you!");
                if (!RollDodge(player.Defense))
                {
                    var ambushDmg = Math.Max(1, enemy.Attack - player.Defense);
                    player.TakeDamage(ambushDmg);
                    _display.ShowCombatMessage($"{enemy.Name} hits you for {ambushDmg} damage!");
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                }
                else
                {
                    _display.ShowCombatMessage("You dodged the ambush!");
                }
            }
            isFirstTurn = false;
            _display.ShowCombatStatus(player, enemy);
            _display.ShowCombatPrompt();
            var choice = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
            
            if (choice == "F" || choice == "FLEE")
            {
                if (_rng.NextDouble() < 0.5)
                {
                    _display.ShowMessage("You fled successfully!");
                    return CombatResult.Fled;
                }
                else
                {
                    _display.ShowMessage("You failed to flee!");
                    var fleeDmg = Math.Max(1, enemy.Attack - player.Defense);
                    player.TakeDamage(fleeDmg);
                    _display.ShowCombatMessage($"{enemy.Name} hits you for {fleeDmg} damage!");
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                    continue;
                }
            }
            
            // Player attack phase - check for Wraith enhanced dodge
            bool enemyDodged = false;
            if (enemy.SpecialAbility == SpecialAbility.EnhancedDodge && _rng.NextDouble() < enemy.SpecialValue)
            {
                enemyDodged = true;
                _display.ShowCombatMessage($"{enemy.Name} phased through your attack!");
            }
            else if (RollDodge(enemy.Defense))
            {
                enemyDodged = true;
                _display.ShowCombatMessage($"{enemy.Name} dodged your attack!");
            }
            
            if (!enemyDodged)
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
            }
            
            if (enemy.HP <= 0)
            {
                _display.ShowCombat($"You defeated the {enemy.Name}!");
                
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
                return CombatResult.Won;
            }
            
            // Enemy attack phase
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
                
                // Lifesteal: heal enemy after successful hit
                if (enemy.SpecialAbility == SpecialAbility.Lifesteal)
                {
                    var healAmount = (int)(enemyDmg * enemy.SpecialValue);
                    enemy.HP = Math.Min(enemy.MaxHP, enemy.HP + healAmount);
                    _display.ShowCombatMessage($"{enemy.Name} drains {healAmount} HP from you!");
                }
                
                // Poison Dart: apply poison on successful hit
                if (enemy.SpecialAbility == SpecialAbility.PoisonDart && _statusManager != null)
                {
                    _statusManager.Apply(player, StatusEffect.Poison, 3);
                    _display.ShowCombatMessage($"{enemy.Name}'s poison dart infects you!");
                }
            }
            
            if (player.HP <= 0) return CombatResult.PlayerDied;
        }
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
