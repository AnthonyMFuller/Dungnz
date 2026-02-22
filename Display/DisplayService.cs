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
        
        ShowColoredStat("HP:", $"{player.HP}/{player.MaxHP}", Systems.ColorCodes.HealthColor(player.HP, player.MaxHP));
        ShowColoredStat("💧 Mana:", $"{player.Mana}/{player.MaxMana}", Systems.ColorCodes.ManaColor(player.Mana, player.MaxMana));
        ShowColoredStat("Attack:", $"{player.Attack}", Systems.ColorCodes.BrightRed);
        ShowColoredStat("Defense:", $"{player.Defense}", Systems.ColorCodes.Cyan);
        ShowColoredStat("Gold:", $"{player.Gold}", Systems.ColorCodes.Yellow);
        ShowColoredStat("XP:", $"{player.XP}", Systems.ColorCodes.Green);
        
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
            Console.Write($"{oldItem.Name,-28}");
        else
            Console.Write($"{"(none)",-28}");
        Console.WriteLine("║");
        
        // New item
        Console.WriteLine($"║ New:      {newItem.Name,-28}║");
        Console.WriteLine("╠═══════════════════════════════════════╣");
        
        // Calculate deltas
        int oldAttack = oldItem?.AttackBonus ?? 0;
        int oldDefense = oldItem?.DefenseBonus ?? 0;
        int newAttack = newItem.AttackBonus;
        int newDefense = newItem.DefenseBonus;
        int attackDelta = newAttack - oldAttack;
        int defenseDelta = newDefense - oldDefense;
        
        // Show attack
        const string attackPrefix = "║ Attack:   ";
        const string defensePrefix = "║ Defense:  ";
        const int innerWidth = 39; // box inner width (between the two ║ chars)

        var attackContent = $"{player.Attack - oldAttack} → {player.Attack - oldAttack + attackDelta}";
        if (attackDelta != 0)
        {
            var deltaColor = attackDelta > 0 ? Systems.ColorCodes.Green : Systems.ColorCodes.Red;
            var deltaSign = attackDelta > 0 ? "+" : "";
            attackContent += $" {deltaColor}({deltaSign}{attackDelta}){Systems.ColorCodes.Reset}";
        }
        var attackVisibleLen = attackPrefix.Length - 1 + Systems.ColorCodes.StripAnsiCodes(attackContent).Length;
        Console.WriteLine(attackPrefix + attackContent + new string(' ', innerWidth - attackVisibleLen) + "║");

        // Show defense
        var defenseContent = $"{player.Defense - oldDefense} → {player.Defense - oldDefense + defenseDelta}";
        if (defenseDelta != 0)
        {
            var deltaColor = defenseDelta > 0 ? Systems.ColorCodes.Green : Systems.ColorCodes.Red;
            var deltaSign = defenseDelta > 0 ? "+" : "";
            defenseContent += $" {deltaColor}({deltaSign}{defenseDelta}){Systems.ColorCodes.Reset}";
        }
        var defenseVisibleLen = defensePrefix.Length - 1 + Systems.ColorCodes.StripAnsiCodes(defenseContent).Length;
        Console.WriteLine(defensePrefix + defenseContent + new string(' ', innerWidth - defenseVisibleLen) + "║");
        
        Console.WriteLine("╚═══════════════════════════════════════╝");
        Console.WriteLine();
    }

    /// <summary>
    /// Renders the enhanced ASCII art title screen with colors.
    /// </summary>
    public void ShowEnhancedTitle()
    {
        Console.Clear();
        var cyan = Systems.ColorCodes.Cyan;
        var yellow = Systems.ColorCodes.Yellow;
        var reset = Systems.ColorCodes.Reset;

        Console.WriteLine($"{cyan}╔══════════════════════════════════════╗{reset}");
        Console.WriteLine($"{cyan}║{reset}    {cyan}▓▓{reset}  {yellow}╔═╗ ╦ ╦ ╔╗╔ ╔═╗ ╔╗╔ ╔═╗{reset}  {cyan}▓▓{reset}    {cyan}║{reset}");
        Console.WriteLine($"{cyan}║{reset}    {cyan}▓▓{reset}  {yellow}║ ║ ║ ║ ║║║ ║ ╦ ║║║ ╔═╝{reset}  {cyan}▓▓{reset}    {cyan}║{reset}");
        Console.WriteLine($"{cyan}║{reset}    {cyan}▓▓{reset}  {yellow}╚═╝ ╚═╝ ╝╚╝ ╚═╝ ╝╚╝ ╚═╝{reset}  {cyan}▓▓{reset}    {cyan}║{reset}");
        Console.WriteLine($"{cyan}║{reset}                                      {cyan}║{reset}");
        Console.WriteLine($"{cyan}║{reset}         {cyan}D  U  N  G  N  Z{reset}             {cyan}║{reset}");
        Console.WriteLine($"{cyan}║{reset}    {cyan}─────────────────────────────{reset}     {cyan}║{reset}");
        Console.WriteLine($"{cyan}║{reset}       {yellow}Descend If You Dare{reset}            {cyan}║{reset}");
        Console.WriteLine($"{cyan}╚══════════════════════════════════════╝{reset}");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays the atmospheric lore introduction paragraph. Returns false (never skipped).
    /// </summary>
    public bool ShowIntroNarrative()
    {
        var gray = Systems.ColorCodes.Gray;
        var yellow = Systems.ColorCodes.Yellow;
        var reset = Systems.ColorCodes.Reset;

        Console.WriteLine($"{gray}The ancient fortress of Dungnz has stood for a thousand years — a labyrinthine{reset}");
        Console.WriteLine($"{gray}tomb carved into the mountain's heart by hands long since turned to dust. Adventurers{reset}");
        Console.WriteLine($"{gray}who descend its spiral corridors speak of riches beyond imagination and horrors beyond{reset}");
        Console.WriteLine($"{gray}comprehension. The air below reeks of sulfur and old blood. Torches flicker without wind.{reset}");
        Console.WriteLine($"{gray}Something vast and patient watches from the deep.{reset}");
        Console.WriteLine();
        Console.WriteLine($"{yellow}[ Press Enter to begin your descent... ]{reset}");
        Console.ReadLine();
        Console.WriteLine();
        return false;
    }

    /// <summary>
    /// Displays prestige level card. Only called when prestige.PrestigeLevel > 0.
    /// </summary>
    public void ShowPrestigeInfo(Systems.PrestigeData prestige)
    {
        var yellow = Systems.ColorCodes.Yellow;
        var reset = Systems.ColorCodes.Reset;

        Console.WriteLine($"{yellow}╔═══════════════════════════════╗{reset}");
        Console.WriteLine($"{yellow}║{reset}  {yellow}⭐ PRESTIGE LEVEL {prestige.PrestigeLevel,-10}{reset} {yellow}║{reset}");
        Console.WriteLine($"{yellow}║{reset}  Wins: {prestige.TotalWins,-3} Runs: {prestige.TotalRuns,-10} {yellow}║{reset}");
        
        if (prestige.BonusStartAttack > 0)
            Console.WriteLine($"{yellow}║{reset}  Bonus Attack:   +{prestige.BonusStartAttack,-11} {yellow}║{reset}");
        if (prestige.BonusStartDefense > 0)
            Console.WriteLine($"{yellow}║{reset}  Bonus Defense:  +{prestige.BonusStartDefense,-11} {yellow}║{reset}");
        if (prestige.BonusStartHP > 0)
            Console.WriteLine($"{yellow}║{reset}  Bonus HP:       +{prestige.BonusStartHP,-11} {yellow}║{reset}");
        
        Console.WriteLine($"{yellow}╚═══════════════════════════════╝{reset}");
        Console.WriteLine();
    }

    /// <summary>
    /// Shows colored difficulty cards with mechanical context and returns the player's validated choice.
    /// </summary>
    public Difficulty SelectDifficulty()
    {
        var green = Systems.ColorCodes.Green;
        var yellow = Systems.ColorCodes.Yellow;
        var red = Systems.ColorCodes.Red;
        var reset = Systems.ColorCodes.Reset;

        Console.WriteLine("Choose your difficulty:");
        Console.WriteLine();
        Console.WriteLine($"  {green}[1] CASUAL{reset}     (Enemy Power ×0.7 | Loot ×1.5 | Gold ×1.5)");
        Console.WriteLine($"  {yellow}[2] NORMAL{reset}     (Enemy Power ×1.0 | Balanced)");
        Console.WriteLine($"  {red}[3] HARD{reset}       (Enemy Power ×1.3 | Loot ×0.7 | Gold ×0.7)");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim() ?? "";
            
            switch (input)
            {
                case "1": return Difficulty.Casual;
                case "2": return Difficulty.Normal;
                case "3": return Difficulty.Hard;
                default:
                    Console.WriteLine($"{Systems.ColorCodes.Red}Invalid choice. Please enter 1, 2, or 3.{reset}");
                    break;
            }
        }
    }

    /// <summary>
    /// Shows class cards with ASCII stat bars and inline prestige bonuses, returns the player's validated choice.
    /// </summary>
    public PlayerClassDefinition SelectClass(Systems.PrestigeData? prestige)
    {
        var cyan = Systems.ColorCodes.Cyan;
        var yellow = Systems.ColorCodes.Yellow;
        var gray = Systems.ColorCodes.Gray;
        var reset = Systems.ColorCodes.Reset;

        Console.WriteLine("Choose your class:");
        Console.WriteLine();

        // Base stats (from Player defaults)
        const int baseHP = 100;
        const int baseAttack = 10;
        const int baseDefense = 5;
        const int baseMana = 30;

        var classes = new[] {
            (def: PlayerClassDefinition.Warrior, icon: "⚔", number: 1),
            (def: PlayerClassDefinition.Mage, icon: "🔮", number: 2),
            (def: PlayerClassDefinition.Rogue, icon: "🗡", number: 3)
        };

        foreach (var (def, icon, number) in classes)
        {
            // Calculate effective stats
            int effectiveHP = baseHP + def.BonusMaxHP;
            int effectiveAttack = baseAttack + def.BonusAttack;
            int effectiveDefense = baseDefense + def.BonusDefense;
            int effectiveMana = baseMana + def.BonusMaxMana;

            // Calculate prestige-boosted stats if applicable
            string hpDisplay, atkDisplay, defDisplay;
            if (prestige != null && prestige.PrestigeLevel > 0)
            {
                int prestigeHP = effectiveHP + prestige.BonusStartHP;
                int prestigeAtk = effectiveAttack + prestige.BonusStartAttack;
                int prestigeDef = effectiveDefense + prestige.BonusStartDefense;

                hpDisplay = prestige.BonusStartHP > 0 
                    ? $"{effectiveHP} → {yellow}{prestigeHP}{reset} (+{prestige.BonusStartHP} prestige)"
                    : effectiveHP.ToString();
                atkDisplay = prestige.BonusStartAttack > 0
                    ? $"{effectiveAttack} → {yellow}{prestigeAtk}{reset} (+{prestige.BonusStartAttack} prestige)"
                    : effectiveAttack.ToString();
                defDisplay = prestige.BonusStartDefense > 0
                    ? $"{effectiveDefense} → {yellow}{prestigeDef}{reset} (+{prestige.BonusStartDefense} prestige)"
                    : effectiveDefense.ToString();
            }
            else
            {
                hpDisplay = effectiveHP.ToString();
                atkDisplay = effectiveAttack.ToString();
                defDisplay = effectiveDefense.ToString();
            }

            // Stat bars
            string hpBar = StatBar(effectiveHP, 120);
            string atkBar = StatBar(effectiveAttack, 13);
            string defBar = StatBar(effectiveDefense, 7);
            string manaBar = StatBar(effectiveMana, 60);

            const int boxInner = 48;
            Console.WriteLine($"{cyan}┌────────────────────────────────────────────────┐{reset}");
            Console.WriteLine($"{cyan}│{reset} [{number}] {icon}  {def.Name.ToUpper(),-39} {cyan}│{reset}");
            
            // HP line with ANSI-aware padding (clamped to handle prestige overflow-safe)
            var hpLine = $" HP:      {hpBar}  {hpDisplay}";
            var hpVisibleLen = Systems.ColorCodes.StripAnsiCodes(hpLine).Length;
            Console.WriteLine($"{cyan}│{reset}{hpLine}{new string(' ', Math.Max(0, boxInner - hpVisibleLen))}{cyan}│{reset}");
            
            // Attack line with ANSI-aware padding
            var atkLine = $" Attack:  {atkBar}  {atkDisplay}";
            var atkVisibleLen = Systems.ColorCodes.StripAnsiCodes(atkLine).Length;
            Console.WriteLine($"{cyan}│{reset}{atkLine}{new string(' ', Math.Max(0, boxInner - atkVisibleLen))}{cyan}│{reset}");
            
            // Defense line with ANSI-aware padding
            var defLine = $" Defense: {defBar}  {defDisplay}";
            var defVisibleLen = Systems.ColorCodes.StripAnsiCodes(defLine).Length;
            Console.WriteLine($"{cyan}│{reset}{defLine}{new string(' ', Math.Max(0, boxInner - defVisibleLen))}{cyan}│{reset}");
            
            Console.WriteLine($"{cyan}│{reset} Mana:    {manaBar}  {effectiveMana,-25} {cyan}│{reset}");
            Console.WriteLine($"{cyan}│{reset} Trait: {def.TraitDescription,-39} {cyan}│{reset}");
            Console.WriteLine($"{cyan}│{reset} {gray}\"{def.Description}\"{reset}{new string(' ', Math.Max(0, 46 - def.Description.Length))}{cyan}│{reset}");
            Console.WriteLine($"{cyan}└────────────────────────────────────────────────┘{reset}");
            Console.WriteLine();
        }

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim() ?? "";
            
            switch (input)
            {
                case "1": return PlayerClassDefinition.Warrior;
                case "2": return PlayerClassDefinition.Mage;
                case "3": return PlayerClassDefinition.Rogue;
                default:
                    Console.WriteLine($"{Systems.ColorCodes.Red}Invalid choice. Please enter 1, 2, or 3.{reset}");
                    break;
            }
        }
    }

    /// <summary>
    /// Creates an ASCII stat bar visualization.
    /// </summary>
    private static string StatBar(int value, int max, int width = 10)
    {
        var filled = Math.Clamp((int)Math.Round((double)value / max * width), 0, width);
        return new string('█', filled) + new string('░', width - filled);
    }
}
