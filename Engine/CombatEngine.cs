namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Display;

public class CombatEngine : ICombatEngine
{
    private readonly DisplayService _display;
    private readonly IInputReader _input;
    private readonly Random _rng;
    
    public CombatEngine(DisplayService display, IInputReader? input = null, Random? rng = null)
    {
        _display = display;
        _input = input ?? new ConsoleInputReader();
        _rng = rng ?? new Random();
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
                    player.HP -= fleeDmg;
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
                    player.Gold += loot.Gold;
                    _display.ShowMessage($"You found {loot.Gold} gold!");
                }
                if (loot.Item != null)
                {
                    player.Inventory.Add(loot.Item);
                    _display.ShowLootDrop(loot.Item);
                }
                
                player.XP += enemy.XPValue;
                _display.ShowMessage($"You gained {enemy.XPValue} XP. (Total: {player.XP})");
                CheckLevelUp(player);
                return CombatResult.Won;
            }
            
            var enemyDmg = Math.Max(1, enemy.Attack - player.Defense);
            player.HP -= enemyDmg;
            _display.ShowCombatMessage($"{enemy.Name} hits you for {enemyDmg} damage!");
            
            if (player.HP <= 0) return CombatResult.PlayerDied;
        }
    }
    
    private void CheckLevelUp(Player player)
    {
        var newLevel = player.XP / 100 + 1;
        if (newLevel > player.Level)
        {
            player.Level = newLevel;
            player.Attack += 2;
            player.Defense += 1;
            player.MaxHP += 10;
            player.HP = player.MaxHP;
            _display.ShowMessage($"LEVEL UP! You are now level {player.Level}!");
        }
    }
}
