using Dungnz.Models;
using Dungnz.Systems;
using Spectre.Console;

namespace Dungnz.Display;

/// <summary>
/// Spectre.Console-backed implementation of IDisplayService.
/// Replaces hand-rolled ANSI with Spectre widgets.
/// </summary>
public sealed class SpectreDisplayService : IDisplayService
{
    /// <inheritdoc/>
    public void ShowTitle()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("DUNGNZ").Color(Color.Red));
        AnsiConsole.Write(new Rule("[grey]A dungeon awaits...[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowRoom(Room room)
    {
        AnsiConsole.WriteLine();

        // Build room panel content
        var sb = new System.Text.StringBuilder();

        // Room type prefix with color
        var (prefix, prefixColor) = room.Type switch
        {
            RoomType.Dark            => ("🌑 The room is pitch dark. ",                           "red"),
            RoomType.Scorched        => ("🔥 Scorch marks scar the stone. ",                      "yellow"),
            RoomType.Flooded         => ("💧 Ankle-deep water pools here. ",                      "yellow"),
            RoomType.Mossy           => ("🌿 Damp moss covers the walls. ",                       "green"),
            RoomType.Ancient         => ("🏛 Ancient runes line the walls. ",                     "cyan"),
            RoomType.ForgottenShrine => ("✨ Holy light radiates from a forgotten shrine. ",      "cyan"),
            RoomType.PetrifiedLibrary=> ("📚 Petrified bookshelves line these ancient walls. ",  "cyan"),
            RoomType.ContestedArmory => ("⚔ Weapon racks gleam dangerously in the dark. ",       "yellow"),
            _                        => (string.Empty, "white")
        };

        if (!string.IsNullOrEmpty(prefix))
            sb.AppendLine($"[{prefixColor}]{Markup.Escape(prefix)}[/]");

        sb.AppendLine(Markup.Escape(room.Description));

        // Environmental hazard
        var envLine = room.EnvironmentalHazard switch
        {
            RoomHazard.LavaSeam        => "[red]🔥 Lava seams crack the floor — each action will burn you.[/]",
            RoomHazard.CorruptedGround => "[grey]💀 The ground pulses with dark energy — it will drain you with every action.[/]",
            RoomHazard.BlessedClearing => "[cyan]✨ A blessed warmth fills this clearing.[/]",
            _                          => null
        };
        if (envLine != null) sb.AppendLine(envLine);

        // Hazard forewarning
        var hazardLine = room.Type switch
        {
            RoomType.Scorched => "[yellow]⚠ The scorched stone radiates heat — take care.[/]",
            RoomType.Flooded  => "[cyan]⚠ The water here looks treacherous.[/]",
            RoomType.Dark     => "[grey]⚠ Darkness presses in around you.[/]",
            _                 => null
        };
        if (hazardLine != null) sb.AppendLine(hazardLine);

        // Exits
        if (room.Exits.Count > 0)
        {
            var exitSymbols = new Dictionary<Direction, string>
            {
                [Direction.North] = "↑ North",
                [Direction.South] = "↓ South",
                [Direction.East]  = "→ East",
                [Direction.West]  = "← West"
            };
            var ordered = new[] { Direction.North, Direction.South, Direction.East, Direction.West }
                .Where(d => room.Exits.ContainsKey(d))
                .Select(d => exitSymbols[d]);
            sb.AppendLine($"[yellow]Exits:[/] {string.Join("   ", ordered)}");
        }

        // Enemies
        if (room.Enemy != null)
            sb.AppendLine($"[bold red]⚔ {Markup.Escape(room.Enemy.Name)} is here![/]");

        // Items on floor
        if (room.Items.Count > 0)
        {
            sb.AppendLine("[grey]Items on the ground:[/]");
            foreach (var item in room.Items)
                sb.AppendLine($"  [green]◆ {Markup.Escape(item.Name)}[/] [grey]({Markup.Escape(PrimaryStatLabel(item))})[/]");
        }

        // Shrine hints
        if (room.HasShrine && room.Type != RoomType.ForgottenShrine)
        {
            var atm = ShrineNarration.Presence[Random.Shared.Next(ShrineNarration.Presence.Length)];
            sb.AppendLine($"[cyan]{Markup.Escape(atm)}[/]");
            sb.AppendLine("[cyan]✨ A shrine glimmers here. (USE SHRINE)[/]");
        }
        if (room.Type == RoomType.ForgottenShrine && !room.SpecialRoomUsed)
            sb.AppendLine("[cyan]✨ A forgotten shrine stands here, radiating holy energy. (USE SHRINE)[/]");
        if (room.Type == RoomType.PetrifiedLibrary && !room.SpecialRoomUsed)
            sb.AppendLine("[cyan]📖 Ancient tomes line the walls. Something catches the light as you enter...[/]");
        if (room.Type == RoomType.ContestedArmory && !room.SpecialRoomUsed)
            sb.AppendLine("[yellow]⚠ Trapped weapons gleam in the dark. (USE ARMORY to approach)[/]");
        if (room.Merchant != null)
        {
            var greeting = MerchantNarration.Greetings[Random.Shared.Next(MerchantNarration.Greetings.Length)];
            sb.AppendLine($"[yellow]{Markup.Escape(greeting)}[/]");
            sb.AppendLine("[yellow]🛒 A merchant awaits. (SHOP)[/]");
        }

        var panel = new Panel(new Markup(sb.ToString().TrimEnd()))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader($"[bold cyan]{Markup.Escape(room.Description.Length > 0 ? GetRoomDisplayName(room) : "Room")}[/]"),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private static string GetRoomDisplayName(Room room) => room.Type switch
    {
        RoomType.Dark             => "Dark Room",
        RoomType.Mossy            => "Mossy Chamber",
        RoomType.Flooded          => "Flooded Chamber",
        RoomType.Scorched         => "Scorched Hall",
        RoomType.Ancient          => "Ancient Chamber",
        RoomType.ForgottenShrine  => "Forgotten Shrine",
        RoomType.PetrifiedLibrary => "Petrified Library",
        RoomType.ContestedArmory  => "Contested Armory",
        RoomType.TrapRoom         => "Trap Room",
        _                         => "Chamber"
    };

    /// <inheritdoc/>
    public void ShowCombat(string message) =>
        AnsiConsole.Write(new Rule($"[bold red]{Markup.Escape(message)}[/]"));

    /// <inheritdoc/>
    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        AnsiConsole.WriteLine();

        var table = new Table().NoBorder().Expand();
        table.AddColumn(new TableColumn("").NoWrap());
        table.AddColumn(new TableColumn("").NoWrap());

        // Player cell
        var playerCell = new System.Text.StringBuilder();
        playerCell.Append($"⚔  [bold]{Markup.Escape(player.Name)}[/]");
        playerCell.AppendLine();
        playerCell.Append($"HP: {BuildHpBar(player.HP, player.MaxHP)} {player.HP}/{player.MaxHP}");
        if (player.MaxMana > 0)
        {
            playerCell.AppendLine();
            playerCell.Append($"MP: [blue]{BuildBar(player.Mana, player.MaxMana)}[/] {player.Mana}/{player.MaxMana}");
        }
        if (playerEffects.Count > 0)
        {
            playerCell.AppendLine();
            foreach (var e in playerEffects)
            {
                var color = e.IsBuff ? "purple" : "red";
                playerCell.Append($"[{color}][[{EffectIcon(e.Effect)}{Markup.Escape(e.Effect.ToString())} {e.RemainingTurns}t]][/] ");
            }
        }

        // Enemy cell
        var enemyCell = new System.Text.StringBuilder();
        enemyCell.Append($"🐉 [bold]{Markup.Escape(enemy.Name)}[/]");
        enemyCell.AppendLine();
        enemyCell.Append($"HP: {BuildHpBar(enemy.HP, enemy.MaxHP)} {enemy.HP}/{enemy.MaxHP}");
        if (enemyEffects.Count > 0)
        {
            enemyCell.AppendLine();
            foreach (var e in enemyEffects)
            {
                var color = e.IsBuff ? "purple" : "red";
                enemyCell.Append($"[{color}][[{EffectIcon(e.Effect)}{Markup.Escape(e.Effect.ToString())} {e.RemainingTurns}t]][/] ");
            }
        }

        table.AddRow(new Markup(playerCell.ToString()), new Markup(enemyCell.ToString()));
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowCombatMessage(string message) =>
        AnsiConsole.MarkupLine($"  [white]{Markup.Escape(message)}[/]");

    /// <inheritdoc/>
    public void ShowPlayerStats(Player player)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold grey]Stat[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Value[/]"));

        table.AddRow("Name",   Markup.Escape(player.Name));
        table.AddRow("Level",  player.Level.ToString());
        table.AddRow("Class",  Markup.Escape(player.Class.ToString()));

        double hpPct = player.MaxHP > 0 ? (double)player.HP / player.MaxHP : 0;
        var hpColor  = hpPct > 0.5 ? "green" : hpPct >= 0.25 ? "yellow" : "red";
        table.AddRow("HP",     $"[{hpColor}]{player.HP}/{player.MaxHP}[/]");

        if (player.MaxMana > 0)
        {
            double mpPct = (double)player.Mana / player.MaxMana;
            var mpColor  = mpPct > 0.5 ? "blue" : mpPct >= 0.25 ? "cyan" : "grey";
            table.AddRow("MP",  $"[{mpColor}]{player.Mana}/{player.MaxMana}[/]");
        }

        table.AddRow("Attack",  $"[red]{player.Attack}[/]");
        table.AddRow("Defense", $"[cyan]{player.Defense}[/]");
        table.AddRow("Gold",    $"[yellow]{player.Gold}g[/]");
        var xpToNext = 100 * player.Level;
        table.AddRow("XP",     $"[green]{player.XP}/{xpToNext}[/]");

        if (player.Class == PlayerClass.Rogue && player.ComboPoints > 0)
        {
            var dots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            table.AddRow("⚡ Combo", $"[yellow]{dots}[/]");
        }

        var classDef = PlayerClassDefinition.All.FirstOrDefault(c => c.Class == player.Class);
        if (classDef != null && !string.IsNullOrEmpty(classDef.TraitDescription))
            table.AddRow("Trait", Markup.Escape(classDef.TraitDescription));

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowInventory(Player player)
    {
        AnsiConsole.WriteLine();

        if (player.Inventory.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]  (inventory empty)[/]");
            AnsiConsole.WriteLine();
            return;
        }

        int currentWeight = player.Inventory.Sum(i => i.Weight);
        int maxWeight     = Systems.InventoryManager.MaxWeight;
        var wtColor       = currentWeight > maxWeight * 0.95 ? "red"
                          : currentWeight > maxWeight * 0.80 ? "yellow" : "green";
        var slotColor     = player.Inventory.Count >= Player.MaxInventorySize ? "red" : "green";

        AnsiConsole.MarkupLine($"[grey]Slots:[/] [{slotColor}]{player.Inventory.Count}/{Player.MaxInventorySize}[/]  [grey]│  Weight:[/] [{wtColor}]{currentWeight}/{maxWeight}[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold grey]#[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Name[/]"))
            .AddColumn(new TableColumn("[bold grey]Type[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Tier[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]E[/]").NoWrap());

        int idx = 1;
        foreach (var group in player.Inventory.GroupBy(i => i.Name))
        {
            var item      = group.First();
            var count     = group.Count();
            var isEquipped = item == player.EquippedWeapon
                          || item == player.EquippedAccessory
                          || player.AllEquippedArmor.Contains(item);
            var tc        = TierColor(item.Tier);
            var nameMk    = $"[{tc}]{Markup.Escape(item.Name)}[/]" + (count > 1 ? $" [grey]×{count}[/]" : "");
            var equip     = isEquipped ? "[green]✓[/]" : "";
            table.AddRow(idx.ToString(), nameMk, Markup.Escape(item.Type.ToString()), $"[{tc}]{item.Tier}[/]", equip);
            idx++;
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"  [yellow]💰 {player.Gold}g[/]");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowLootDrop(Item item, Player player, bool isElite = false)
    {
        var tc        = TierColor(item.Tier);
        var header    = isElite ? "[bold yellow]✦ ELITE LOOT DROP[/]" : "[bold yellow]✦ LOOT DROP[/]";
        var stat      = PrimaryStatLabel(item);

        // Build optional upgrade hint
        string statLine = $"[cyan]{Markup.Escape(stat)}[/]";
        if (item.AttackBonus > 0 && player.EquippedWeapon != null)
        {
            int delta = item.AttackBonus - player.EquippedWeapon.AttackBonus;
            if (delta > 0) statLine += $"  [green](+{delta} vs equipped!)[/]";
        }
        else if (item.DefenseBonus > 0 && player.EquippedChest != null)
        {
            int delta = item.DefenseBonus - player.EquippedChest.DefenseBonus;
            if (delta > 0) statLine += $"  [green](+{delta} vs equipped!)[/]";
        }

        var content = new Markup(
            $"{header}\n" +
            $"[{tc}]{item.Tier}[/]\n" +
            $"{ItemTypeIcon(item.Type)} [{tc}]{Markup.Escape(item.Name)}[/]\n" +
            $"{statLine}  [grey]{item.Weight} wt[/]");

        AnsiConsole.Write(new Panel(content)
        {
            Border    = BoxBorder.Rounded,
            BorderStyle = Style.Parse(tc),
        });
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowGoldPickup(int amount, int newTotal) =>
        AnsiConsole.MarkupLine($"  [yellow]💰 +{amount} gold[/]  [grey](Total: {newTotal}g)[/]");

    /// <inheritdoc/>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax)
    {
        var tc = TierColor(item.Tier);
        AnsiConsole.MarkupLine($"  {ItemTypeIcon(item.Type)} Picked up: [{tc}]{Markup.Escape(item.Name)}[/]  [grey]({Markup.Escape(PrimaryStatLabel(item))})[/]");

        var slotColor = slotsCurrent >= slotsMax         ? "red"
                      : slotsCurrent >= slotsMax * 0.95  ? "yellow" : "green";
        var wtColor   = weightCurrent > weightMax * 0.95 ? "red"
                      : weightCurrent > weightMax * 0.80 ? "yellow" : "green";
        AnsiConsole.MarkupLine($"  [grey]Slots:[/] [{slotColor}]{slotsCurrent}/{slotsMax}[/]  [grey]·  Weight:[/] [{wtColor}]{weightCurrent}/{weightMax}[/]");

        if (weightCurrent > weightMax * 0.8)
            AnsiConsole.MarkupLine($"  [yellow]⚠ Inventory nearly full![/]");
    }

    /// <inheritdoc/>
    public void ShowItemDetail(Item item)
    {
        var tc = TierColor(item.Tier);
        var stats = new System.Text.StringBuilder();
        stats.AppendLine($"[grey]Type:[/]    {Markup.Escape(item.Type.ToString())}");
        stats.AppendLine($"[grey]Tier:[/]    [{tc}]{item.Tier}[/]");
        stats.AppendLine($"[grey]Weight:[/]  {item.Weight}");
        if (item.AttackBonus  != 0) stats.AppendLine($"[grey]Attack:[/]  [red]+{item.AttackBonus}[/]");
        if (item.DefenseBonus != 0) stats.AppendLine($"[grey]Defense:[/] [cyan]+{item.DefenseBonus}[/]");
        if (item.HealAmount   != 0) stats.AppendLine($"[grey]Heal:[/]    [green]+{item.HealAmount} HP[/]");
        if (item.ManaRestore  != 0) stats.AppendLine($"[grey]Mana:[/]    [blue]+{item.ManaRestore}[/]");
        if (item.MaxManaBonus != 0) stats.AppendLine($"[grey]Max Mana:[/][blue]+{item.MaxManaBonus}[/]");
        if (item.DodgeBonus   >  0) stats.AppendLine($"[grey]Dodge:[/]   +{item.DodgeBonus:P0}");
        if (item.CritChance   >  0) stats.AppendLine($"[grey]Crit:[/]    +{item.CritChance:P0}");
        if (item.HPOnHit      != 0) stats.AppendLine($"[grey]HP on Hit:[/][green]+{item.HPOnHit}[/]");
        if (item.AppliesBleedOnHit) stats.AppendLine("[grey]Special:[/] [red]Bleed on hit[/]");
        if (item.PoisonImmunity)    stats.AppendLine("[grey]Special:[/] [green]Poison immune[/]");
        if (!string.IsNullOrEmpty(item.Description))
        {
            stats.AppendLine();
            stats.Append($"[grey]{Markup.Escape(item.Description)}[/]");
        }

        var panel = new Panel(new Markup(stats.ToString().TrimEnd()))
        {
            Border      = BoxBorder.Rounded,
            BorderStyle = Style.Parse(tc),
            Header      = new PanelHeader($"[{tc} bold]{ItemTypeIcon(item.Type)} {Markup.Escape(item.Name)}[/]"),
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowMessage(string message) =>
        AnsiConsole.MarkupLine(Markup.Escape(message));

    /// <inheritdoc/>
    public void ShowError(string message) =>
        AnsiConsole.MarkupLine($"[red]✗ {Markup.Escape(message)}[/]");

    /// <inheritdoc/>
    public void ShowHelp()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn(new TableColumn("[bold yellow]Command[/]").NoWrap())
            .AddColumn(new TableColumn("[bold yellow]Description[/]"));

        table.AddRow("[grey]── Navigation ──[/]", "");
        table.AddRow("go [north|south|east|west]", "Move in a direction  [grey](aliases: n s e w)[/]");
        table.AddRow("look",       "Re-describe the current room");
        table.AddRow("map",        "Show ASCII mini-map of discovered rooms");
        table.AddRow("descend",    "Descend to the next floor (at cleared exit)");
        table.AddRow("[grey]── Items ──[/]", "");
        table.AddRow("examine [target]", "Inspect an enemy, room item, or inventory item");
        table.AddRow("take [item]",      "Pick up an item from the floor");
        table.AddRow("use [item]",       "Use a consumable [grey](e.g. USE POTION, USE SHRINE)[/]");
        table.AddRow("inventory",        "List carried items");
        table.AddRow("equipment",        "Show equipped gear");
        table.AddRow("equip [item]",     "Equip a weapon, armour, or accessory");
        table.AddRow("unequip [item]",   "Unequip an item back to inventory");
        table.AddRow("craft [recipe]",   "Craft an item [grey](CRAFT alone lists recipes)[/]");
        table.AddRow("shop",             "Browse the merchant (if one is present)");
        table.AddRow("sell",             "Sell items to the merchant (if one is present)");
        table.AddRow("[grey]── Character ──[/]", "");
        table.AddRow("stats",            "Show player stats and current floor");
        table.AddRow("skills",           "Show skill tree");
        table.AddRow("learn [skill]",    "Unlock a skill");
        table.AddRow("[grey]── Systems ──[/]", "");
        table.AddRow("save [name]",      "Save the game");
        table.AddRow("load [name]",      "Load a saved game");
        table.AddRow("listsaves",        "List available save files");
        table.AddRow("prestige",         "Show prestige level and bonuses");
        table.AddRow("leaderboard",      "Show top run history");
        table.AddRow("help",             "Show this help");
        table.AddRow("quit",             "Exit the game");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowCommandPrompt(Player? player = null)
    {
        if (player == null)
        {
            AnsiConsole.Markup("[grey]>[/] ");
            return;
        }
        var hpBar = BuildHpBar(player.HP, player.MaxHP, 6);
        AnsiConsole.Markup($"[grey][[/]{hpBar} {player.HP}/{player.MaxHP} HP");
        if (player.MaxMana > 0)
            AnsiConsole.Markup($" [grey]│[/] [blue]{BuildBar(player.Mana, player.MaxMana, 4)}[/] {player.Mana}/{player.MaxMana} MP");
        AnsiConsole.Markup("[grey]][/] [grey]>[/] ");
    }

    /// <inheritdoc/>
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

        var visiblePositions = positions
            .Where(kv => kv.Key.Visited || kv.Key == currentRoom)
            .ToList();

        if (visiblePositions.Count == 0) return;

        int minX = visiblePositions.Min(kv => kv.Value.x);
        int maxX = visiblePositions.Max(kv => kv.Value.x);
        int minY = visiblePositions.Min(kv => kv.Value.y);
        int maxY = visiblePositions.Max(kv => kv.Value.y);

        var grid = new Dictionary<(int x, int y), Room>();
        foreach (var (room, pos) in visiblePositions)
            grid[pos] = room;

        var mapSb = new System.Text.StringBuilder();
        mapSb.AppendLine("[bold white]═══ MAP ═══[/]   N");
        mapSb.AppendLine("              ↑");

        for (int y = minY; y <= maxY; y++)
        {
            mapSb.Append("  ");
            for (int x = minX; x <= maxX; x++)
            {
                if (!grid.TryGetValue((x, y), out var r))
                {
                    mapSb.Append(x < maxX ? "    " : "   ");
                    continue;
                }

                string symbol = GetMapRoomSymbol(r, currentRoom);
                mapSb.Append(symbol);

                if (x < maxX)
                {
                    bool hasConnector = r.Exits.ContainsKey(Direction.East) && grid.ContainsKey((x + 1, y));
                    mapSb.Append(hasConnector ? "-" : " ");
                }
            }
            mapSb.AppendLine();

            if (y < maxY)
            {
                mapSb.Append("  ");
                for (int x = minX; x <= maxX; x++)
                {
                    bool hasSouth = grid.TryGetValue((x, y), out var rS)
                        && rS.Exits.ContainsKey(Direction.South)
                        && grid.ContainsKey((x, y + 1));
                    mapSb.Append(hasSouth ? " | " : "   ");
                    if (x < maxX) mapSb.Append(" ");
                }
                mapSb.AppendLine();
            }
        }

        mapSb.AppendLine();
        mapSb.AppendLine("[bold yellow][[*]][/] You    [white][[E]][/] Exit    [red][[!]][/] Enemy    [cyan][[S]][/] Shrine    [white][[+]][/] Cleared    [yellow][[B]][/] Boss");

        AnsiConsole.Write(new Panel(new Markup(mapSb.ToString().TrimEnd()))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader("[bold white]Mini-Map[/]"),
        });
        AnsiConsole.WriteLine();
    }

    private static string GetMapRoomSymbol(Room r, Room currentRoom)
    {
        if (r == currentRoom)                               return "[bold yellow][[@]][/]";
        if (!r.Visited)                                     return "[grey][[?]][/]";
        if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0) return "[bold red][[B]][/]";
        if (r.IsExit)                                       return "[white][[E]][/]";
        if (r.Enemy != null && r.Enemy.HP > 0)              return "[red][[!]][/]";
        if (r.HasShrine && !r.ShrineUsed)                   return "[cyan][[S]][/]";
        return "[white][[+]][/]";
    }

    /// <inheritdoc/>
    public string ReadPlayerName() =>
        AnsiConsole.Prompt(
            new TextPrompt<string>("[bold yellow]Enter your name, hero:[/]")
                .Validate(name => !string.IsNullOrWhiteSpace(name)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Name cannot be empty[/]")));

    /// <inheritdoc/>
    public void ShowColoredMessage(string message, string color)
    {
        // Map legacy ANSI color codes to Spectre color names where recognisable; fallback to white
        var spectreColor = MapAnsiToSpectre(color);
        AnsiConsole.MarkupLine($"[{spectreColor}]{Markup.Escape(message)}[/]");
    }

    /// <inheritdoc/>
    public void ShowColoredCombatMessage(string message, string color)
    {
        var spectreColor = MapAnsiToSpectre(color);
        AnsiConsole.MarkupLine($"  [{spectreColor}]{Markup.Escape(message)}[/]");
    }

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string color)
    {
        var spectreColor = MapAnsiToSpectre(color);
        AnsiConsole.MarkupLine($"{Markup.Escape(label),-8} [{spectreColor}]{Markup.Escape(value)}[/]");
    }

    private static string MapAnsiToSpectre(string ansiCode) => ansiCode switch
    {
        var c when c == Systems.ColorCodes.Red       || c == Systems.ColorCodes.BrightRed  => "red",
        var c when c == Systems.ColorCodes.Green                                           => "green",
        var c when c == Systems.ColorCodes.Yellow                                          => "yellow",
        var c when c == Systems.ColorCodes.Cyan                                            => "cyan",
        var c when c == Systems.ColorCodes.Gray                                            => "grey",
        var c when c == Systems.ColorCodes.Blue                                            => "blue",
        var c when c == Systems.ColorCodes.BrightWhite                                    => "white",
        _ => "white"
    };

    /// <inheritdoc/>
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Title("[bold yellow]Equipment Comparison[/]")
            .AddColumn(new TableColumn("[bold grey]Stat[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Current[/]").Centered())
            .AddColumn(new TableColumn("[bold grey]New[/]").Centered())
            .AddColumn(new TableColumn("[bold grey]Delta[/]").Centered());

        // Item names row
        var oldName = oldItem != null ? $"[grey]{Markup.Escape(oldItem.Name)}[/]" : "[grey](none)[/]";
        var tcNew   = TierColor(newItem.Tier);
        table.AddRow("[grey]Item[/]", oldName, $"[{tcNew}]{Markup.Escape(newItem.Name)}[/]", "");

        // Stat delta helper
        void AddStatRow(string label, int oldVal, int newVal)
        {
            int delta = newVal - oldVal;
            var deltaStr = delta == 0 ? "[grey]—[/]"
                         : delta > 0  ? $"[green]+{delta}[/]"
                         : $"[red]{delta}[/]";
            table.AddRow(label, oldVal.ToString(), newVal.ToString(), deltaStr);
        }

        AddStatRow("Attack",   oldItem?.AttackBonus  ?? 0, newItem.AttackBonus);
        AddStatRow("Defense",  oldItem?.DefenseBonus ?? 0, newItem.DefenseBonus);
        if (newItem.HealAmount   != 0 || (oldItem?.HealAmount ?? 0) != 0)
            AddStatRow("Heal HP",  oldItem?.HealAmount   ?? 0, newItem.HealAmount);
        if (newItem.MaxManaBonus != 0 || (oldItem?.MaxManaBonus ?? 0) != 0)
            AddStatRow("Max Mana", oldItem?.MaxManaBonus ?? 0, newItem.MaxManaBonus);
        if (newItem.StatModifier != 0 || (oldItem?.StatModifier ?? 0) != 0)
            AddStatRow("HP Mod",   oldItem?.StatModifier ?? 0, newItem.StatModifier);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowEquipment(Player player)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Gold1)
            .Title("[bold gold1]⚔ EQUIPMENT[/]")
            .AddColumn(new TableColumn("[bold grey]Slot[/]").NoWrap().LeftAligned())
            .AddColumn(new TableColumn("[bold grey]Item[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold grey]Stats[/]").LeftAligned());

        void AddSlot(string slotLabel, Item? item, bool isWeapon = false, bool isAccessory = false)
        {
            if (item == null)
            {
                table.AddRow(slotLabel, "[grey](empty)[/]", "[grey]—[/]");
                return;
            }

            var tc = TierColor(item.Tier);
            var name = $"[{tc}]{Markup.Escape(item.Name)}[/]";
            var stats = new System.Collections.Generic.List<string>();

            if (isWeapon)
            {
                if (item.AttackBonus  != 0) stats.Add($"[red]+{item.AttackBonus} ATK[/]");
                if (item.DodgeBonus   > 0)  stats.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
                if (item.MaxManaBonus > 0)  stats.Add($"[blue]+{item.MaxManaBonus} mana[/]");
                if (item.PoisonImmunity)    stats.Add("[green]poison immune[/]");
            }
            else if (isAccessory)
            {
                if (item.AttackBonus  != 0) stats.Add($"[red]+{item.AttackBonus} ATK[/]");
                if (item.DefenseBonus != 0) stats.Add($"[cyan]+{item.DefenseBonus} DEF[/]");
                if (item.StatModifier != 0) stats.Add($"[green]+{item.StatModifier} HP[/]");
                if (item.DodgeBonus   > 0)  stats.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
                if (item.MaxManaBonus > 0)  stats.Add($"[blue]+{item.MaxManaBonus} mana[/]");
            }
            else // armor
            {
                if (item.DefenseBonus != 0) stats.Add($"[cyan]+{item.DefenseBonus} DEF[/]");
                if (item.DodgeBonus   > 0)  stats.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
                if (item.MaxManaBonus > 0)  stats.Add($"[blue]+{item.MaxManaBonus} mana[/]");
                if (item.PoisonImmunity)    stats.Add("[green]poison immune[/]");
            }

            var statsStr = stats.Count > 0 ? string.Join(", ", stats) : "[grey]—[/]";
            table.AddRow(slotLabel, name, statsStr);
        }

        AddSlot("⚔  Weapon",    player.EquippedWeapon,    isWeapon: true);
        AddSlot("💍 Accessory", player.EquippedAccessory, isAccessory: true);
        AddSlot("🪖 Head",      player.EquippedHead);
        AddSlot("🥋 Shoulders", player.EquippedShoulders);
        AddSlot("🛡 Chest",     player.EquippedChest);
        AddSlot("🧤 Hands",     player.EquippedHands);
        AddSlot("👖 Legs",      player.EquippedLegs);
        AddSlot("👟 Feet",      player.EquippedFeet);
        AddSlot("🧥 Back",      player.EquippedBack);
        AddSlot("⛨  Off-Hand",  player.EquippedOffHand);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);

        var setDesc = SetBonusManager.GetActiveBonusDescription(player);
        if (!string.IsNullOrEmpty(setDesc))
        {
            var setPanel = new Panel(new Markup($"[yellow]{Markup.Escape(setDesc)}[/]"))
            {
                Border = BoxBorder.Rounded,
                Header = new PanelHeader("[bold yellow]Set Bonuses[/]"),
            };
            AnsiConsole.Write(setPanel);
        }

        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowEnhancedTitle()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("DUNGNZ").Color(Color.Red));
        AnsiConsole.Write(new Rule("[grey]D  U  N  G  N  Z[/]").RuleStyle("cyan"));
        AnsiConsole.MarkupLine("[grey]         Descend If You Dare[/]");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public bool ShowIntroNarrative()
    {
        var lore = "The ancient fortress of [bold]Dungnz[/] has stood for a thousand years — a labyrinthine\n"
                 + "tomb carved into the mountain's heart by hands long since turned to dust. Adventurers\n"
                 + "who descend its spiral corridors speak of riches beyond imagination and horrors beyond\n"
                 + "comprehension. The air below reeks of sulfur and old blood. Torches flicker without wind.\n"
                 + "Something vast and patient watches from the deep.";
        var panel = new Panel(new Markup($"[grey]{lore}[/]"))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader("[grey]Lore[/]"),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine("[yellow][[ Press Enter to begin your descent... ]][/]");
        Console.ReadLine();
        AnsiConsole.WriteLine();
        return false;
    }

    /// <inheritdoc/>
    public void ShowPrestigeInfo(PrestigeData prestige)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn(new TableColumn("[yellow]Prestige[/]"))
            .AddColumn(new TableColumn("[yellow]Value[/]"));

        table.AddRow("⭐ Level",   $"[yellow]{prestige.PrestigeLevel}[/]");
        table.AddRow("Wins",       prestige.TotalWins.ToString());
        table.AddRow("Runs",       prestige.TotalRuns.ToString());
        if (prestige.BonusStartAttack  > 0) table.AddRow("Bonus Attack",   $"[green]+{prestige.BonusStartAttack}[/]");
        if (prestige.BonusStartDefense > 0) table.AddRow("Bonus Defense",  $"[green]+{prestige.BonusStartDefense}[/]");
        if (prestige.BonusStartHP      > 0) table.AddRow("Bonus HP",       $"[green]+{prestige.BonusStartHP}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public Difficulty SelectDifficulty() =>
        PromptFromMenu("[bold yellow]Choose your difficulty:[/]",
            new (string, Difficulty)[]
            {
                ("[green]CASUAL[/]     Weaker enemies · Cheap shops · Start with 50g + 3 potions", Difficulty.Casual),
                ("[yellow]NORMAL[/]     Balanced challenge · The intended experience · Start with 15g + 1 potion", Difficulty.Normal),
                ("[red]HARD[/]       Stronger enemies · Scarce rewards · No starting supplies · ☠ Permadeath", Difficulty.Hard),
            });

    /// <inheritdoc/>
    public PlayerClassDefinition SelectClass(PrestigeData? prestige)
    {
        var choices = PlayerClassDefinition.All.Select(def =>
        {
            var presBonus = prestige != null && prestige.PrestigeLevel > 0
                ? $" [yellow](+{prestige.BonusStartHP} HP, +{prestige.BonusStartAttack} ATK, +{prestige.BonusStartDefense} DEF prestige)[/]"
                : "";
            var label = $"{ClassIcon(def)} [bold]{Markup.Escape(def.Name),-12}[/] — {Markup.Escape(def.Description)}{presBonus}";
            return (label, def);
        });
        return PromptFromMenu("[bold yellow]Choose your class:[/]", choices);
    }

    /// <inheritdoc/>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [bold yellow]🏪 Merchant[/]  [grey]Your gold:[/] [yellow]{playerGold}g[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn(new TableColumn("[bold grey]#[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Name[/]"))
            .AddColumn(new TableColumn("[bold grey]Type[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Tier[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Cost[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Stats[/]"));

        int idx = 1;
        foreach (var (item, price) in stock)
        {
            var tc         = TierColor(item.Tier);
            var canAfford  = playerGold >= price;
            var priceStr   = canAfford ? $"[green]{price}g[/]" : $"[grey]{price}g[/]";
            var nameMk     = canAfford ? $"[{tc}]{Markup.Escape(item.Name)}[/]"
                                       : $"[grey]{Markup.Escape(item.Name)}[/]";
            table.AddRow(
                idx.ToString(),
                nameMk,
                Markup.Escape(item.Type.ToString()),
                $"[{tc}]{item.Tier}[/]",
                priceStr,
                $"[grey]{Markup.Escape(PrimaryStatLabel(item))}[/]");
            idx++;
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        var options = stockList
            .Select((s, i) => (
                $"{ItemTypeIcon(s.item.Type)} {Markup.Escape(s.item.Name)}  [grey]{Markup.Escape(PrimaryStatLabel(s.item))}[/]  [yellow]{s.price}g[/]",
                i + 1))
            .Append(("[grey]Cancel[/]", 0));
        return PromptFromMenu("[bold yellow]Buy an item:[/]", options);
    }

    /// <inheritdoc/>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [bold yellow]💰 Sell Items[/]  [grey]Your gold:[/] [yellow]{playerGold}g[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold grey]#[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Name[/]"))
            .AddColumn(new TableColumn("[bold grey]Tier[/]").NoWrap())
            .AddColumn(new TableColumn("[bold grey]Sell Price[/]").NoWrap());

        int idx = 1;
        foreach (var (item, price) in items)
        {
            var tc = TierColor(item.Tier);
            table.AddRow(idx.ToString(), $"[{tc}]{Markup.Escape(item.Name)}[/]", $"[{tc}]{item.Tier}[/]", $"[green]+{price}g[/]");
            idx++;
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        var itemList = items.ToList();
        var options = itemList
            .Select((s, i) => (
                $"{ItemTypeIcon(s.item.Type)} {Markup.Escape(s.item.Name)}  [grey]{Markup.Escape(PrimaryStatLabel(s.item))}[/]  [green]+{s.sellPrice}g[/]",
                i + 1))
            .Append(("[grey]Cancel[/]", 0));
        return PromptFromMenu("[bold yellow]Sell an item:[/]", options);
    }

    /// <inheritdoc/>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients)
    {
        var tc = TierColor(result.Tier);
        var content = new System.Text.StringBuilder();
        content.AppendLine($"[grey]Result:[/]  {ItemTypeIcon(result.Type)} [{tc}]{Markup.Escape(result.Name)}[/]  [grey]({Markup.Escape(PrimaryStatLabel(result))})[/]");
        content.AppendLine($"[grey]Stats:[/]   [cyan]{Markup.Escape(PrimaryStatLabel(result))}[/]");
        content.AppendLine();
        content.AppendLine("[grey]Ingredients:[/]");
        foreach (var (ingredient, hasIt) in ingredients)
        {
            var check     = hasIt ? "[green]✅[/]" : "[red]❌[/]";
            var ingColor  = hasIt ? "white" : "grey";
            content.AppendLine($"  {check} [{ingColor}]{Markup.Escape(ingredient)}[/]");
        }

        var panel = new Panel(new Markup(content.ToString().TrimEnd()))
        {
            Border      = BoxBorder.Rounded,
            BorderStyle = Style.Parse("yellow"),
            Header      = new PanelHeader($"[yellow bold]🔨 {Markup.Escape(recipeName)}[/]"),
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowCombatStart(Enemy enemy)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule($"[bold red]⚔  COMBAT BEGINS  ⚔[/]");
        rule.Style = Style.Parse("red");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine($"[bold red]  {Markup.Escape(enemy.Name)}[/]");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowCombatEntryFlags(Enemy enemy)
    {
        if (enemy.IsElite)
            AnsiConsole.MarkupLine("  [yellow]⭐ ELITE — enhanced stats and loot[/]");
        if (enemy is Dungnz.Systems.Enemies.DungeonBoss boss && boss.IsEnraged)
            AnsiConsole.MarkupLine("  [bold red]⚡ ENRAGED[/]");
    }

    /// <inheritdoc/>
    public void ShowLevelUpChoice(Player player)
    {
        AnsiConsole.WriteLine();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn(new TableColumn("[bold yellow]#[/]").NoWrap())
            .AddColumn(new TableColumn("[bold yellow]Bonus[/]"))
            .AddColumn(new TableColumn("[bold yellow]Value[/]").NoWrap());

        table.AddRow("[yellow]1[/]", "+5 Max HP",   $"[grey]{player.MaxHP} → {player.MaxHP + 5}[/]");
        table.AddRow("[yellow]2[/]", "+2 Attack",   $"[grey]{player.Attack} → {player.Attack + 2}[/]");
        table.AddRow("[yellow]3[/]", "+2 Defense",  $"[grey]{player.Defense} → {player.Defense + 2}[/]");

        AnsiConsole.MarkupLine("[bold white]★ LEVEL UP! Choose a stat bonus:[/]");
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)
    {
        var threatColor = floor <= 2 ? "green" : floor <= 4 ? "yellow" : "red";
        var threat = floor <= 2 ? "Low" : floor <= 4 ? "Moderate" : "High";

        var content = $"[{threatColor}]Floor {floor} of {maxFloor}[/]\n"
                    + $"[white]{Markup.Escape(variant.Name)}[/]\n"
                    + $"[{threatColor}]⚠ Danger: {threat}[/]";

        var rule = new Rule($"[bold {threatColor}]Floor {floor} — {Markup.Escape(variant.Name)}[/]");
        rule.Style = Style.Parse(threatColor);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine($"  [{threatColor}]⚠ Danger: {threat}[/]");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowEnemyDetail(Enemy enemy)
    {
        var nameColor = enemy.IsElite ? "yellow" : "red";
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"HP:      {BuildHpBar(enemy.HP, enemy.MaxHP)} {enemy.HP}/{enemy.MaxHP}");
        summary.AppendLine($"[red]ATK:     {enemy.Attack}[/]");
        summary.AppendLine($"[cyan]DEF:     {enemy.Defense}[/]");
        summary.Append($"[green]XP:      {enemy.XPValue}[/]");
        if (enemy.IsElite) summary.Append("  [yellow]⭐ ELITE[/]");

        var panel = new Panel(new Markup(summary.ToString()))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader($"[bold {nameColor}]{Markup.Escape(enemy.Name.ToUpperInvariant())}[/]"),
        };
        AnsiConsole.Write(panel);
    }

    /// <inheritdoc/>
    public void ShowVictory(Player player, int floorsCleared, RunStats stats)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("VICTORY").Color(Color.Gold1));
        var floorWord = floorsCleared == 1 ? "floor" : "floors";
        var summary = $"[bold]{Markup.Escape(player.Name)}[/]  •  Level {player.Level}\n"
                    + $"[yellow]{floorsCleared} {floorWord} conquered[/]\n\n"
                    + $"[grey]Enemies slain:  {stats.EnemiesDefeated}\n"
                    + $"Gold earned:    {stats.GoldCollected}\n"
                    + $"Items found:    {stats.ItemsFound}\n"
                    + $"Turns taken:    {stats.TurnsTaken}[/]";
        var panel = new Panel(new Markup(summary))
        {
            Border = BoxBorder.Heavy,
            Header = new PanelHeader("[bold yellow]✦  V I C T O R Y  ✦[/]"),
        };
        panel.BorderColor(Color.Gold1);
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowGameOver(Player player, string? killedBy, RunStats stats)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("GAME OVER").Color(Color.DarkRed));
        var deathLine = killedBy != null ? $"Killed by: {Markup.Escape(killedBy)}" : "Cause of death: unknown";
        var summary = $"[bold]{Markup.Escape(player.Name)}[/]  •  Level {player.Level}\n"
                    + $"[red]{deathLine}[/]\n"
                    + $"[grey]Enemies slain:  {stats.EnemiesDefeated}\n"
                    + $"Floors reached: {stats.FloorsVisited}\n"
                    + $"Turns survived: {stats.TurnsTaken}[/]";
        var panel = new Panel(new Markup(summary))
        {
            Border = BoxBorder.Heavy,
            Header = new PanelHeader("[bold red]☠  RUN ENDED  ☠[/]"),
        };
        panel.BorderColor(Color.DarkRed);
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt == null || enemy.AsciiArt.Length == 0)
            return;

        var artText = string.Join("\n", enemy.AsciiArt.Select(l => Markup.Escape(l)));
        var artColor = enemy.IsElite ? "yellow" : "red";
        var panel = new Panel($"[{artColor}]{artText}[/]")
        {
            Header = new PanelHeader($"[bold red]{Markup.Escape(enemy.Name)}[/]"),
            Border = BoxBorder.Rounded,
        };
        AnsiConsole.Write(panel);
    }

    /// <inheritdoc/>
    public int ShowLevelUpChoiceAndSelect(Player player) =>
        PromptFromMenu("[bold yellow]★ Choose a stat bonus:[/]",
            new (string, int)[]
            {
                ($"+5 Max HP     [grey]({player.MaxHP} → {player.MaxHP + 5})[/]", 1),
                ($"+2 Attack     [grey]({player.Attack} → {player.Attack + 2})[/]", 2),
                ($"+2 Defense    [grey]({player.Defense} → {player.Defense + 2})[/]", 3),
            });

    /// <inheritdoc/>
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
    {
        var ctx = new System.Text.StringBuilder($"Mana: {player.Mana}/{player.MaxMana}");
        if (player.Class == PlayerClass.Rogue)
        {
            var dots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            ctx.Append($"  ⚡ Combo: {dots}");
        }
        if (player.Class == PlayerClass.Mage && player.IsManaShieldActive)
            ctx.Append(" [SHIELD ACTIVE]");
        if (player.Class == PlayerClass.Paladin && player.DivineShieldTurnsRemaining > 0)
            ctx.Append($" [DIVINE SHIELD: {player.DivineShieldTurnsRemaining}T]");
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(ctx.ToString())}[/]");
        return PromptFromMenu("[bold yellow]Choose your action:[/]",
            new (string, string)[]
            {
                ("⚔  Attack",  "A"),
                ("✨ Ability", "B"),
                ("🏃 Flee",    "F"),
                ("🧪 Use Item","I"),
            });
    }

    /// <inheritdoc/>
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes)
    {
        var recipeList = recipes.ToList();
        var options = recipeList
            .Select((r, i) => (
                r.canCraft
                    ? $"[green]✅ {Markup.Escape(r.recipeName)}[/]"
                    : $"[red]❌ {Markup.Escape(r.recipeName)}[/]",
                i + 1))
            .Append(("[grey]↩  Cancel[/]", 0));
        return PromptFromMenu("[bold yellow]=== CRAFTING — Choose a recipe ===[/]", options);
    }

    /// <inheritdoc/>
    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75) =>
        PromptFromMenu("[bold yellow]✨ [[Shrine Menu]][/]",
            new (string, int)[]
            {
                ($"Heal fully        — [yellow]{healCost}g[/]  [grey](Your gold: {playerGold}g)[/]", 1),
                ($"Bless             — [yellow]{blessCost}g[/]  [grey](+2 ATK/DEF permanently)[/]", 2),
                ($"Fortify           — [yellow]{fortifyCost}g[/]  [grey](MaxHP +10, permanent)[/]", 3),
                ($"Meditate          — [yellow]{meditateCost}g[/]  [grey](MaxMana +10, permanent)[/]", 4),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        var options = stockList
            .Select((s, i) => (
                $"{ItemTypeIcon(s.item.Type)} {Markup.Escape(s.item.Name)}  [grey]{Markup.Escape(PrimaryStatLabel(s.item))}[/]  [yellow]{s.price}g[/]",
                i + 1))
            .Append(("[yellow]💰 Sell Items[/]", -1))
            .Append(("[grey]Leave[/]", 0));
        return PromptFromMenu("[bold yellow]Merchant — what would you like?[/]", options);
    }

    /// <inheritdoc/>
    public bool ShowConfirmMenu(string prompt) =>
        PromptFromMenu($"[bold yellow]{Markup.Escape(prompt)}[/]",
            new (string, bool)[]
            {
                ("[green]Yes[/]", true),
                ("[grey]No[/]", false),
            });

    /// <inheritdoc/>
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2) =>
        PromptFromMenu($"[bold yellow]{Markup.Escape(header)}[/]",
            new (string, int)[]
            {
                (Markup.Escape(option1), 1),
                (Markup.Escape(option2), 2),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public int ShowForgottenShrineMenuAndSelect() =>
        PromptFromMenu("[bold yellow]🕯 [[Forgotten Shrine]] — choose a blessing:[/]",
            new (string, int)[]
            {
                ("Holy Strength   — [grey]+5 ATK (lasts until next floor)[/]", 1),
                ("Sacred Ground   — [grey]Auto-heal at shrines[/]", 2),
                ("Warding Veil    — [grey]20% chance to deflect enemy attacks this floor[/]", 3),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public int ShowContestedArmoryMenuAndSelect(int playerDefense) =>
        PromptFromMenu("[bold yellow]⚔ [[Contested Armory]] — how do you approach?[/]",
            new (string, int)[]
            {
                ($"Careful approach — [grey]disarm traps (requires DEF > 12, yours: {playerDefense})[/]", 1),
                ("Reckless grab   — [grey]take what you can (15-30 damage)[/]", 2),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities)
    {
        foreach (var (ability, onCooldown, cooldownTurns, notEnoughMana) in unavailableAbilities)
        {
            if (onCooldown)
                AnsiConsole.MarkupLine($"  [grey]○ {Markup.Escape(ability.Name)} — Cooldown: {cooldownTurns} turns (Cost: {ability.ManaCost} MP)[/]");
            else if (notEnoughMana)
                AnsiConsole.MarkupLine($"  [red]○ {Markup.Escape(ability.Name)} — Need {ability.ManaCost} MP (Cost: {ability.ManaCost} MP)[/]");
        }
        var availList = availableAbilities.ToList();
        var options = availList
            .Select(a => (
                $"{Markup.Escape(a.Name)} — [grey]{Markup.Escape(a.Description)} (Cost: {a.ManaCost} MP)[/]",
                (Ability?)a))
            .Append(("[grey]Cancel[/]", (Ability?)null));
        return PromptFromMenu("[bold yellow]=== Abilities ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables)
    {
        var options = consumables
            .Select(item =>
            {
                var manaStr = item.ManaRestore > 0 ? $" [blue]+{item.ManaRestore} MP[/]" : "";
                return ($"🧪 {Markup.Escape(item.Name)} [green](+{item.HealAmount} HP)[/]{manaStr}", (Item?)item);
            })
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== USE ITEM — Choose a consumable ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
    {
        var options = equippable
            .Select(item => (
                $"{ItemTypeIcon(item.Type)} {Markup.Escape(item.Name)}  [grey][[{Markup.Escape(PrimaryStatLabel(item))}]][/]",
                (Item?)item))
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== EQUIP — Choose an item ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable)
    {
        var options = usable
            .Select(item => (
                $"{ItemTypeIcon(item.Type)} {Markup.Escape(item.Name)}  [grey][[{Markup.Escape(PrimaryStatLabel(item))}]][/]",
                (Item?)item))
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== Use which item? ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems)
    {
        var sentinel = new Item { Name = "__TAKE_ALL__" };
        var options = roomItems
            .Select(item => (
                $"{ItemTypeIcon(item.Type)} {Markup.Escape(item.Name)}  [grey][[{Markup.Escape(PrimaryStatLabel(item))}]][/]",
                (Item?)item))
            .Prepend(("[yellow]📦 Take All[/]", (Item?)sentinel))
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== TAKE — Choose an item ===[/]", options);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string TierColor(ItemTier tier) => tier switch
    {
        ItemTier.Uncommon  => "green",
        ItemTier.Rare      => "blue",
        ItemTier.Epic      => "purple",
        ItemTier.Legendary => "gold1",
        _                  => "grey",
    };

    private static T PromptFromMenu<T>(string title, IEnumerable<(string Label, T Value)> options)
    {
        var optList = options.ToList();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<(string Label, T Value)>()
                .Title(title)
                .UseConverter(o => o.Label)
                .AddChoices(optList));
        return selected.Value;
    }

    private static string ItemTypeIcon(ItemType type) => type switch
    {
        ItemType.Weapon           => "⚔",
        ItemType.Armor            => "🛡",
        ItemType.Consumable       => "🧪",
        ItemType.Accessory        => "💍",
        ItemType.CraftingMaterial => "⚗",
        _                         => "•"
    };

    private static string PrimaryStatLabel(Item item)
    {
        if (item.AttackBonus  != 0) return $"Attack +{item.AttackBonus}";
        if (item.DefenseBonus != 0) return $"Defense +{item.DefenseBonus}";
        if (item.HealAmount   != 0) return $"Heals {item.HealAmount} HP";
        if (item.ManaRestore  != 0) return $"Mana +{item.ManaRestore}";
        if (item.MaxManaBonus != 0) return $"Max Mana +{item.MaxManaBonus}";
        if (item.DodgeBonus   >  0) return $"Dodge +{item.DodgeBonus:P0}";
        if (item.StatModifier != 0) return $"HP +{item.StatModifier}";
        return item.Type.ToString();
    }

    private static string ClassIcon(PlayerClassDefinition def) => def.Class switch
    {
        PlayerClass.Warrior     => "⚔",
        PlayerClass.Mage        => "🔮",
        PlayerClass.Rogue       => "🗡",
        PlayerClass.Paladin     => "🛡",
        PlayerClass.Necromancer => "💀",
        PlayerClass.Ranger      => "🏹",
        _                       => "•"
    };

    private static string BuildBar(int current, int max, int width = 10)
    {
        current = Math.Clamp(current, 0, Math.Max(max, 1));
        int filled = max > 0 ? (int)Math.Round((double)current / max * width) : 0;
        return new string('█', filled) + new string('░', width - filled);
    }

    private static string BuildHpBar(int current, int max, int width = 10)
    {
        var bar = BuildBar(current, max, width);
        double pct = max > 0 ? (double)current / max : 0;
        var color = pct > 0.5 ? "green" : pct >= 0.25 ? "yellow" : "red";
        return $"[{color}]{bar}[/]";
    }

    private static string EffectIcon(StatusEffect effect) => effect switch
    {
        StatusEffect.Poison    => "☠",
        StatusEffect.Bleed     => "🩸",
        StatusEffect.Stun      => "⚡",
        StatusEffect.Regen     => "✨",
        StatusEffect.Fortified => "🛡",
        StatusEffect.Weakened  => "💀",
        StatusEffect.Slow      => ">",
        StatusEffect.BattleCry => "!",
        StatusEffect.Burn      => "*",
        StatusEffect.Freeze    => "~",
        StatusEffect.Silence   => "X",
        StatusEffect.Curse     => "@",
        _                      => "●"
    };
}
