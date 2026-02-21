using Dungnz.Models;

namespace Dungnz.Display;

/// <summary>
/// Concrete <see cref="IDisplayService"/> that writes all game output to the standard
/// system console using Unicode box-drawing characters and emoji for visual clarity,
/// and reads player input via <see cref="Console.ReadLine"/>.
/// </summary>
public class ConsoleDisplayService : IDisplayService
{
    /// <summary>
    /// Clears the terminal and prints the game's ASCII-art title banner.
    /// </summary>
    public void ShowTitle()
    {
        Console.Clear();
        Console.WriteLine("╔═══════════════════════════════════════╗");
        Console.WriteLine("║         DUNGEON CRAWLER               ║");
        Console.WriteLine("║      A Text-Based Adventure           ║");
        Console.WriteLine("╚═══════════════════════════════════════╝");
        Console.WriteLine();
    }

    /// <summary>
    /// Writes the room description, available exits, any live enemy warning, and a
    /// list of items on the floor to the console.
    /// </summary>
    /// <param name="room">The room to describe.</param>
    public void ShowRoom(Room room)
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

    /// <summary>
    /// Prints a combat headline prefixed with a sword emoji (e.g. "⚔ A Goblin attacks!").
    /// </summary>
    /// <param name="message">The headline text to display.</param>
    public void ShowCombat(string message)
    {
        Console.WriteLine($"⚔ {message}");
    }

    /// <summary>
    /// Prints a one-line HP status comparison in the format
    /// "[You: X/Y HP] vs [EnemyName: X/Y HP]".
    /// </summary>
    /// <param name="player">The player whose HP is shown on the left side.</param>
    /// <param name="enemy">The enemy whose HP is shown on the right side.</param>
    public void ShowCombatStatus(Player player, Enemy enemy)
    {
        Console.WriteLine();
        Console.WriteLine($"[You: {player.HP}/{player.MaxHP} HP] vs [{enemy.Name}: {enemy.HP}/{enemy.MaxHP} HP]");
        Console.WriteLine();
    }

    /// <summary>
    /// Prints a single indented line of combat narrative text (hit/miss/dodge/crit/effect messages).
    /// </summary>
    /// <param name="message">The narrative line to display.</param>
    public void ShowCombatMessage(string message)
    {
        Console.WriteLine($"  {message}");
    }

    /// <summary>
    /// Renders a formatted "PLAYER STATS" block showing name, HP, attack, defense,
    /// gold, XP, and level.
    /// </summary>
    /// <param name="player">The player whose stats are displayed.</param>
    public void ShowPlayerStats(Player player)
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

    /// <summary>
    /// Renders the player's inventory as a bulleted list with item-type annotations,
    /// or "(empty)" when the inventory contains no items.
    /// </summary>
    /// <param name="player">The player whose inventory is displayed.</param>
    public void ShowInventory(Player player)
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

    /// <summary>
    /// Announces that a defeated enemy dropped an item, prefixed with a star glyph.
    /// </summary>
    /// <param name="item">The item that was dropped.</param>
    public void ShowLootDrop(Item item)
    {
        Console.WriteLine($"✦ Dropped: {item.Name}");
    }

    /// <summary>
    /// Writes a plain informational line to the console with no special prefix or formatting.
    /// </summary>
    /// <param name="message">The text to display.</param>
    public void ShowMessage(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes an error or warning line prefixed with "✗" to visually distinguish it
    /// from regular game output.
    /// </summary>
    /// <param name="message">The error description to display.</param>
    public void ShowError(string message)
    {
        Console.WriteLine($"✗ {message}");
    }

    /// <summary>
    /// Prints a "COMMANDS" reference block listing every available verb and its purpose
    /// so the player can discover what actions are available during exploration.
    /// </summary>
    public void ShowHelp()
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

    /// <summary>
    /// Writes the standard "&gt; " input prompt without a trailing newline, signalling
    /// to the player that they should type an exploration command.
    /// </summary>
    public void ShowCommandPrompt()
    {
        Console.Write("> ");
    }

    /// <summary>
    /// Writes the combat action prompt "[A]ttack or [F]lee?" without a trailing newline,
    /// indicating to the player that they must choose a combat action.
    /// </summary>
    public void ShowCombatPrompt()
    {
        Console.Write("[A]ttack or [F]lee? ");
    }

    /// <summary>
    /// Prompts the player to enter their adventurer name at game start and returns it.
    /// Falls back to "Hero" if the player presses Enter without typing anything.
    /// </summary>
    /// <returns>The name entered by the player, or "Hero" if the input was empty.</returns>
    public string ReadPlayerName()
    {
        Console.Write("Enter your name, adventurer: ");
        return Console.ReadLine() ?? "Hero";
    }
}
