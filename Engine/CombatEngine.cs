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
    
    public CombatEngine(IDisplayService display, IInputReader? input = null, Random? rng = null, GameEvents? events = null)
    {
        _display = display;
        _input = input ?? new ConsoleInputReader();
        _rng = rng ?? new Random();
        _events = events;
    }
    
    public CombatResult RunCombat(Player player, Enemy enemy)
    {
        _display.ShowCombat($"A {enemy.Name} attacks!");
        
        while (true)
        {
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
            
            var playerDmg = Math.Max(1, player.Attack - enemy.Defense);
            enemy.HP -= playerDmg;
            _display.ShowCombatMessage($"You hit {enemy.Name} for {playerDmg} damage!");
            
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
            
            var enemyDmg = Math.Max(1, enemy.Attack - player.Defense);
            player.TakeDamage(enemyDmg);
            _display.ShowCombatMessage($"{enemy.Name} hits you for {enemyDmg} damage!");
            
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
}
