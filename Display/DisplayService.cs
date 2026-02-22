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
    /// Writes the room description with color-coded room type prefixes, available exits,
    /// any live enemy warning, and a list of items on the floor to the console.
    /// </summary>
    /// <param name="room">The room to describe.</param>
    public void ShowRoom(Room room)
    {
        Console.WriteLine();
        
        // Color-code room type prefix based on danger level
        var (prefix, color) = room.Type switch
        {
            RoomType.Dark => ("🌑 The room is pitch dark. ", Systems.ColorCodes.Red),
            RoomType.Scorched => ("🔥 Scorch marks scar the stone. ", Systems.ColorCodes.Yellow),
            RoomType.Flooded => ("💧 Ankle-deep water pools here. ", Systems.ColorCodes.Yellow),
            RoomType.Mossy => ("🌿 Damp moss covers the walls. ", Systems.ColorCodes.Green),
            RoomType.Ancient => ("🏛 Ancient runes line the walls. ", Systems.ColorCodes.Cyan),
            _ => (string.Empty, Systems.ColorCodes.Reset)
        };
        
        if (!string.IsNullOrEmpty(prefix))
            Console.Write($"{color}{prefix}{Systems.ColorCodes.Reset}");
        
        Console.WriteLine(room.Description);
        Console.WriteLine();

        if (room.Exits.Count > 0)
        {
            Console.Write("Exits: ");
            Console.WriteLine(string.Join(", ", room.Exits.Keys));
        }

        if (room.Enemy != null)
        {
            Console.WriteLine($"{Systems.ColorCodes.BrightRed}{Systems.ColorCodes.Bold}⚠ {room.Enemy.Name} is here!{Systems.ColorCodes.Reset}");
        }

        if (room.Items.Count > 0)
        {
            Console.Write("Items: ");
            var itemNames = room.Items.Select(i => $"{Systems.ColorCodes.Yellow}{i.Name}{Systems.ColorCodes.Reset}");
            Console.WriteLine(string.Join(", ", itemNames));
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
    /// Prints a one-line HP status comparison with color-coded HP values and mana display.
    /// </summary>
    /// <param name="player">The player whose HP is shown on the left side.</param>
    /// <param name="enemy">The enemy whose HP is shown on the right side.</param>
    public void ShowCombatStatus(Player player, Enemy enemy)
    {
        Console.WriteLine();
        
        var playerHpColor = Systems.ColorCodes.HealthColor(player.HP, player.MaxHP);
        var enemyHpColor = Systems.ColorCodes.HealthColor(enemy.HP, enemy.MaxHP);
        
        Console.Write($"[You: {playerHpColor}{player.HP}/{player.MaxHP}{Systems.ColorCodes.Reset} HP");
        
        // Add mana display if player has mana
        if (player.MaxMana > 0)
        {
            var manaColor = Systems.ColorCodes.ManaColor(player.Mana, player.MaxMana);
            Console.Write($" │ {manaColor}{player.Mana}/{player.MaxMana}{Systems.ColorCodes.Reset} MP");
        }
        
        Console.WriteLine($"] vs [{enemy.Name}: {enemyHpColor}{enemy.HP}/{enemy.MaxHP}{Systems.ColorCodes.Reset} HP]");
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
    /// gold, XP, and level with color-coded values.
    /// </summary>
    /// <param name="player">The player whose stats are displayed.</param>
    public void ShowPlayerStats(Player player)
    {
        Console.WriteLine();
        Console.WriteLine("═══ PLAYER STATS ═══");
        Console.WriteLine($"Name:    {player.Name}");
        
        // HP with threshold-based coloring
        var hpColor = Systems.ColorCodes.HealthColor(player.HP, player.MaxHP);
        Console.WriteLine($"HP:      {hpColor}{player.HP}/{player.MaxHP}{Systems.ColorCodes.Reset}");
        
        // Mana with threshold-based coloring
        var manaColor = Systems.ColorCodes.ManaColor(player.Mana, player.MaxMana);
        Console.WriteLine($"💧 Mana: {manaColor}{player.Mana}/{player.MaxMana}{Systems.ColorCodes.Reset}");
        
        // Attack in bright red
        Console.WriteLine($"Attack:  {Systems.ColorCodes.BrightRed}{player.Attack}{Systems.ColorCodes.Reset}");
        
        // Defense in cyan
        Console.WriteLine($"Defense: {Systems.ColorCodes.Cyan}{player.Defense}{Systems.ColorCodes.Reset}");
        
        // Gold in yellow
        Console.WriteLine($"Gold:    {Systems.ColorCodes.Yellow}{player.Gold}{Systems.ColorCodes.Reset}");
        
        // XP in green
        Console.WriteLine($"XP:      {Systems.ColorCodes.Green}{player.XP}{Systems.ColorCodes.Reset}");
        
        Console.WriteLine($"Level:   {player.Level}");
        var classDef = PlayerClassDefinition.All.FirstOrDefault(c => c.Class == player.Class);
        if (classDef != null && !string.IsNullOrEmpty(classDef.TraitDescription))
            Console.WriteLine($"Trait:   {classDef.TraitDescription}");
        Console.WriteLine();
    }

    /// <summary>
    /// Renders the player's inventory as a bulleted list with item-type annotations,
    /// weight tracking, and capacity display.
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
            // Calculate inventory metrics
            int currentWeight = player.Inventory.Sum(i => i.Weight);
            int maxWeight = Systems.InventoryManager.MaxWeight;
            int maxSlots = Player.MaxInventorySize;
            int usedSlots = player.Inventory.Count;
            
            // Show capacity header with color coding
            var weightColor = Systems.ColorCodes.WeightColor(currentWeight, maxWeight);
            var slotsColor = usedSlots >= maxSlots ? Systems.ColorCodes.Red : Systems.ColorCodes.Green;
            
            Console.Write("Slots: ");
            Console.Write($"{slotsColor}{usedSlots}/{maxSlots}{Systems.ColorCodes.Reset}");
            Console.Write(" │ Weight: ");
            Console.WriteLine($"{weightColor}{currentWeight}/{maxWeight}{Systems.ColorCodes.Reset}");
            Console.WriteLine();
            
            foreach (var item in player.Inventory)
            {
                Console.Write($"  • {item.Name} ({item.Type})");
                Console.WriteLine($" {Systems.ColorCodes.Gray}[{item.Weight} wt]{Systems.ColorCodes.Reset}");
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
    /// Prints the full list of available player commands, grouped by category.
    /// </summary>
    public void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("═══ COMMANDS ═══");
        Console.WriteLine();
        Console.WriteLine("  Navigation");
        Console.WriteLine("    go [north|south|east|west]  Move in a direction  (aliases: n s e w)");
        Console.WriteLine("    look                         Re-describe the current room");
        Console.WriteLine("    map                          Show ASCII mini-map of discovered rooms");
        Console.WriteLine("    descend                      Descend to the next floor (at cleared exit)");
        Console.WriteLine();
        Console.WriteLine("  Items");
        Console.WriteLine("    examine [target]             Inspect an enemy, room item, or inventory item");
        Console.WriteLine("    take [item]                  Pick up an item from the floor");
        Console.WriteLine("    use [item]                   Use a consumable (e.g. USE POTION, USE SHRINE)");
        Console.WriteLine("    inventory                    List carried items");
        Console.WriteLine("    equipment                    Show equipped gear");
        Console.WriteLine("    equip [item]                 Equip a weapon, armour, or accessory");
        Console.WriteLine("    unequip [item]               Unequip an item back to inventory");
        Console.WriteLine("    craft [recipe]               Craft an item (CRAFT alone lists recipes)");
        Console.WriteLine("    shop                         Browse the merchant (if one is present)");
        Console.WriteLine();
        Console.WriteLine("  Character");
        Console.WriteLine("    stats                        Show player stats and current floor");
        Console.WriteLine("    skills                       Show skill tree");
        Console.WriteLine("    learn [skill]                Unlock a skill");
        Console.WriteLine();
        Console.WriteLine("  Systems");
        Console.WriteLine("    save [name]                  Save the game");
        Console.WriteLine("    load [name]                  Load a saved game");
        Console.WriteLine("    listsaves                    List available save files");
        Console.WriteLine("    prestige                     Show prestige level and bonuses");
        Console.WriteLine("    leaderboard                  Show top run history");
        Console.WriteLine("    help                         Show this help");
        Console.WriteLine("    quit                         Exit the game");
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
    /// Renders an ASCII mini-map by performing a BFS from <paramref name="currentRoom"/>
    /// to infer every reachable room's grid coordinates (current room = 0,0;
    /// North = y−1, South = y+1, East = x+1, West = x−1), then drawing a labelled
    /// grid with a compass rose and symbol legend.
    /// </summary>
    /// <param name="currentRoom">
    /// The room the player currently occupies, placed at origin (0,0) on the map.
    /// </param>
    public void ShowMap(Room currentRoom)
    {
        // BFS to assign (x, y) coordinates to every reachable room
        var positions = new Dictionary<Room, (int x, int y)>();
        var queue = new Queue<Room>();
        positions[currentRoom] = (0, 0);
        queue.Enqueue(currentRoom);

        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            var (rx, ry) = positions[room];

            foreach (var (dir, neighbour) in room.Exits)
            {
                if (positions.ContainsKey(neighbour)) continue;

                var (nx, ny) = dir switch
                {
                    Direction.North => (rx,     ry - 1),
                    Direction.South => (rx,     ry + 1),
                    Direction.East  => (rx + 1, ry),
                    Direction.West  => (rx - 1, ry),
                    _               => (rx,     ry)
                };

                positions[neighbour] = (nx, ny);
                queue.Enqueue(neighbour);
            }
        }

        // Determine grid bounds
        int minX = positions.Values.Min(p => p.x);
        int maxX = positions.Values.Max(p => p.x);
        int minY = positions.Values.Min(p => p.y);
        int maxY = positions.Values.Max(p => p.y);

        // Build lookup: coordinate → room
        var grid = new Dictionary<(int x, int y), Room>();
        foreach (var (room, pos) in positions)
            grid[pos] = room;

        // Render
        Console.WriteLine();
        Console.WriteLine("═══ MAP ═══   N");
        Console.WriteLine("              ↑");

        for (int y = minY; y <= maxY; y++)
        {
            Console.Write("  ");
            for (int x = minX; x <= maxX; x++)
            {
                if (!grid.TryGetValue((x, y), out var r))
                {
                    Console.Write("    ");
                    continue;
                }

                string symbol;
                if (r == currentRoom)
                    symbol = "[*]";
                else if (!r.Visited)
                    symbol = "[ ]";
                else if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0)
                    symbol = "[B]";
                else if (r.IsExit)
                    symbol = "[E]";
                else if (r.Enemy != null && r.Enemy.HP > 0)
                    symbol = "[!]";
                else if (r.HasShrine && !r.ShrineUsed)
                    symbol = "[S]";
                else
                    symbol = "[+]";

                Console.Write(symbol + " ");
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("Legend: [*] You  [B] Boss  [E] Exit  [!] Enemy  [S] Shrine  [+] Cleared  [ ] Unknown");
        Console.WriteLine();
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

    /// <summary>
    /// Displays a message with the specified ANSI color applied.
    /// </summary>
    /// <param name="message">The message text to display.</param>
    /// <param name="color">The ANSI color code to apply.</param>
    public void ShowColoredMessage(string message, string color)
    {
        Console.WriteLine($"{color}{message}{Systems.ColorCodes.Reset}");
    }

    /// <summary>
    /// Displays a combat message with the specified ANSI color applied, using
    /// the standard combat message indentation (2 spaces).
    /// </summary>
    /// <param name="message">The combat message text to display.</param>
    /// <param name="color">The ANSI color code to apply.</param>
    public void ShowColoredCombatMessage(string message, string color)
    {
        Console.WriteLine($"  {color}{message}{Systems.ColorCodes.Reset}");
    }

    /// <summary>
    /// Displays a stat label and value pair where the value is colorized.
    /// </summary>
    /// <param name="label">The stat label (e.g. "HP:", "Mana:").</param>
    /// <param name="value">The stat value to display.</param>
    /// <param name="valueColor">The ANSI color code to apply to the value.</param>
    public void ShowColoredStat(string label, string value, string valueColor)
    {
        Console.WriteLine($"{label,-8} {valueColor}{value}{Systems.ColorCodes.Reset}");
    }

    /// <summary>
    /// Displays a side-by-side comparison of equipment showing before/after stats
    /// with color-coded deltas.
    /// </summary>
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem)
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════╗");
        Console.WriteLine("║       EQUIPMENT COMPARISON            ║");
        Console.WriteLine("╠═══════════════════════════════════════╣");
        
        // Current item
        Console.Write("║ Current:  ");
        if (oldItem != null)
            Console.Write($"{oldItem.Name,-27}");
        else
            Console.Write($"{"(none)",-27}");
        Console.WriteLine("║");
        
        // New item
        Console.WriteLine($"║ New:      {newItem.Name,-27}║");
        Console.WriteLine("╠═══════════════════════════════════════╣");
        
        // Calculate deltas
        int oldAttack = oldItem?.AttackBonus ?? 0;
        int oldDefense = oldItem?.DefenseBonus ?? 0;
        int newAttack = newItem.AttackBonus;
        int newDefense = newItem.DefenseBonus;
        int attackDelta = newAttack - oldAttack;
        int defenseDelta = newDefense - oldDefense;
        
        // Show attack
        Console.Write("║ Attack:   ");
        Console.Write($"{player.Attack - oldAttack} → {player.Attack - oldAttack + attackDelta}");
        if (attackDelta != 0)
        {
            var deltaColor = attackDelta > 0 ? Systems.ColorCodes.Green : Systems.ColorCodes.Red;
            var deltaSign = attackDelta > 0 ? "+" : "";
            Console.Write($" {deltaColor}({deltaSign}{attackDelta}){Systems.ColorCodes.Reset}");
        }
        Console.WriteLine($"{"",20}║");
        
        // Show defense
        Console.Write("║ Defense:  ");
        Console.Write($"{player.Defense - oldDefense} → {player.Defense - oldDefense + defenseDelta}");
        if (defenseDelta != 0)
        {
            var deltaColor = defenseDelta > 0 ? Systems.ColorCodes.Green : Systems.ColorCodes.Red;
            var deltaSign = defenseDelta > 0 ? "+" : "";
            Console.Write($" {deltaColor}({deltaSign}{defenseDelta}){Systems.ColorCodes.Reset}");
        }
        Console.WriteLine($"{"",20}║");
        
        Console.WriteLine("╚═══════════════════════════════════════╝");
        Console.WriteLine();
    }
}
