using Dungnz.Models;

namespace Dungnz.Display;

public class DisplayService
{
    public virtual void ShowTitle()
    {
        Console.Clear();
        Console.WriteLine("╔═══════════════════════════════════════╗");
        Console.WriteLine("║         DUNGEON CRAWLER               ║");
        Console.WriteLine("║      A Text-Based Adventure           ║");
        Console.WriteLine("╚═══════════════════════════════════════╝");
        Console.WriteLine();
    }

    public virtual void ShowRoom(Room room)
    {
        Console.WriteLine();
        Console.WriteLine(room.Description);
        Console.WriteLine();

        if (room.Exits.Count > 0)
        {
            Console.Write("Exits: ");
            Console.WriteLine(string.Join(", ", room.Exits.Keys));
        }

        if (room.Enemy != null)
        {
            Console.WriteLine($"⚠ {room.Enemy.Name} is here!");
        }

        if (room.Items.Count > 0)
        {
            Console.WriteLine($"Items: {string.Join(", ", room.Items.Select(i => i.Name))}");
        }

        Console.WriteLine();
    }

    public virtual void ShowCombat(string message)
    {
        Console.WriteLine($"⚔ {message}");
    }

    public virtual void ShowCombatStatus(Player player, Enemy enemy)
    {
        Console.WriteLine();
        Console.WriteLine($"[You: {player.HP}/{player.MaxHP} HP] vs [{enemy.Name}: {enemy.HP}/{enemy.MaxHP} HP]");
        Console.WriteLine();
    }

    public virtual void ShowCombatMessage(string message)
    {
        Console.WriteLine($"  {message}");
    }

    public virtual void ShowPlayerStats(Player player)
    {
        Console.WriteLine();
        Console.WriteLine("═══ PLAYER STATS ═══");
        Console.WriteLine($"Name:    {player.Name}");
        Console.WriteLine($"HP:      {player.HP}/{player.MaxHP}");
        Console.WriteLine($"Attack:  {player.Attack}");
        Console.WriteLine($"Defense: {player.Defense}");
        Console.WriteLine($"Gold:    {player.Gold}");
        Console.WriteLine($"XP:      {player.XP}");
        Console.WriteLine($"Level:   {player.Level}");
        Console.WriteLine();
    }

    public virtual void ShowInventory(Player player)
    {
        Console.WriteLine();
        Console.WriteLine("═══ INVENTORY ═══");
        
        if (player.Inventory.Count == 0)
        {
            Console.WriteLine("  (empty)");
        }
        else
        {
            foreach (var item in player.Inventory)
            {
                Console.WriteLine($"  • {item.Name} ({item.Type})");
            }
        }
        
        Console.WriteLine();
    }

    public virtual void ShowLootDrop(Item item)
    {
        Console.WriteLine($"✦ Dropped: {item.Name}");
    }

    public virtual void ShowMessage(string message)
    {
        Console.WriteLine(message);
    }

    public virtual void ShowError(string message)
    {
        Console.WriteLine($"✗ {message}");
    }

    public virtual void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("═══ COMMANDS ═══");
        Console.WriteLine("  move [north|south|east|west] - Move in a direction");
        Console.WriteLine("  look                          - Examine current room");
        Console.WriteLine("  stats                         - Show player stats");
        Console.WriteLine("  inventory                     - Show inventory");
        Console.WriteLine("  take [item]                   - Pick up an item");
        Console.WriteLine("  use [item]                    - Use an item");
        Console.WriteLine("  attack                        - Attack enemy in room");
        Console.WriteLine("  flee                          - Attempt to flee combat");
        Console.WriteLine("  help                          - Show this help");
        Console.WriteLine("  quit                          - Exit game");
        Console.WriteLine();
    }

    public virtual void ShowCommandPrompt()
    {
        Console.Write("> ");
    }

    public virtual void ShowCombatPrompt()
    {
        Console.Write("[A]ttack or [F]lee? ");
    }

    public string ReadPlayerName()
    {
        Console.Write("Enter your name, adventurer: ");
        return Console.ReadLine() ?? "Hero";
    }
}
