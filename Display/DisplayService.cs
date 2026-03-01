using Dungnz.Engine;
using Dungnz.Models;

namespace Dungnz.Display;

/// <summary>
/// Concrete <see cref="IDisplayService"/> that writes all game output to the standard
/// system console using Unicode box-drawing characters and emoji for visual clarity,
/// and reads player input via <see cref="Console.ReadLine"/>.
/// </summary>
public class ConsoleDisplayService : IDisplayService
{
    private readonly IInputReader _input;
    // TODO: wire up navigator call-sites (issue #586)
    private readonly IMenuNavigator _navigator;

    /// <summary>Initialises the display service with the given input and menu navigator.</summary>
    public ConsoleDisplayService(IInputReader? input = null, IMenuNavigator? navigator = null)
    {
        _input = input ?? new ConsoleInputReader();
        _navigator = navigator ?? new ConsoleMenuNavigator();
    }

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
            RoomType.ForgottenShrine => ("✨ Holy light radiates from a forgotten shrine. ", Systems.ColorCodes.Cyan),
            RoomType.PetrifiedLibrary => ("📚 Petrified bookshelves line these ancient walls. ", Systems.ColorCodes.Cyan),
            RoomType.ContestedArmory => ("⚔ Weapon racks gleam dangerously in the dark. ", Systems.ColorCodes.Yellow),
            _ => (string.Empty, Systems.ColorCodes.Reset)
        };
        
        if (!string.IsNullOrEmpty(prefix))
            Console.Write($"{color}{prefix}{Systems.ColorCodes.Reset}");
        
        Console.WriteLine(room.Description);
        Console.WriteLine();

        // Environmental hazard indicator
        var envHazardWarning = room.EnvironmentalHazard switch
        {
            RoomHazard.LavaSeam        => $"{Systems.ColorCodes.Red}🔥 Lava seams crack the floor — each action will burn you.{Systems.ColorCodes.Reset}",
            RoomHazard.CorruptedGround => $"{Systems.ColorCodes.Gray}💀 The ground pulses with dark energy — it will drain you with every action.{Systems.ColorCodes.Reset}",
            RoomHazard.BlessedClearing => $"{Systems.ColorCodes.Cyan}✨ A blessed warmth fills this clearing.{Systems.ColorCodes.Reset}",
            _                          => null
        };
        if (envHazardWarning != null)
            Console.WriteLine(envHazardWarning);

        // Hazard forewarning
        var hazardWarning = room.Type switch
        {
            RoomType.Scorched => $"{Systems.ColorCodes.Yellow}⚠ The scorched stone radiates heat — take care.{Systems.ColorCodes.Reset}",
            RoomType.Flooded  => $"{Systems.ColorCodes.Cyan}⚠ The water here looks treacherous.{Systems.ColorCodes.Reset}",
            RoomType.Dark     => $"{Systems.ColorCodes.Gray}⚠ Darkness presses in around you.{Systems.ColorCodes.Reset}",
            _                 => null
        };
        if (hazardWarning != null)
            Console.WriteLine(hazardWarning);

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
                .Select(d => exitSymbols[d])
                .ToList();
            Console.WriteLine($"Exits: {string.Join("   ", ordered)}");
        }

        if (room.Enemy != null)
        {
            Console.WriteLine($"{Systems.ColorCodes.BrightRed}{Systems.ColorCodes.Bold}⚠ {room.Enemy.Name} is here!{Systems.ColorCodes.Reset}");
        }

        if (room.Items.Count > 0)
        {
            Console.WriteLine("Items on the ground:");
            foreach (var i in room.Items)
            {
                var icon = ItemTypeIcon(i.Type);
                var stat = PrimaryStatLabel(i);
                Console.WriteLine($"  {icon} {ColorizeItemName(i)} {Systems.ColorCodes.Gray}({stat}){Systems.ColorCodes.Reset}");
            }
        }

        // Contextual hints
        if (room.HasShrine && room.Type != RoomType.ForgottenShrine)
        {
            var shrineAtmosphere = Systems.ShrineNarration.Presence[Random.Shared.Next(Systems.ShrineNarration.Presence.Length)];
            Console.WriteLine($"{Systems.ColorCodes.Cyan}{shrineAtmosphere}{Systems.ColorCodes.Reset}");
            Console.WriteLine($"{Systems.ColorCodes.Cyan}✨ A shrine glimmers here. (USE SHRINE){Systems.ColorCodes.Reset}");
        }
        if (room.Type == RoomType.ForgottenShrine && !room.SpecialRoomUsed)
        {
            Console.WriteLine($"{Systems.ColorCodes.Cyan}✨ A forgotten shrine stands here, radiating holy energy. (USE SHRINE){Systems.ColorCodes.Reset}");
        }
        if (room.Type == RoomType.PetrifiedLibrary && !room.SpecialRoomUsed)
        {
            Console.WriteLine($"{Systems.ColorCodes.Cyan}📖 Ancient tomes line the walls. Something catches the light as you enter...{Systems.ColorCodes.Reset}");
        }
        if (room.Type == RoomType.ContestedArmory && !room.SpecialRoomUsed)
        {
            Console.WriteLine($"{Systems.ColorCodes.Yellow}⚠ Trapped weapons gleam in the dark. (USE ARMORY to approach){Systems.ColorCodes.Reset}");
        }
        if (room.Merchant != null)
        {
            var greeting = Systems.MerchantNarration.Greetings[Random.Shared.Next(Systems.MerchantNarration.Greetings.Length)];
            Console.WriteLine($"{Systems.ColorCodes.Yellow}{greeting}{Systems.ColorCodes.Reset}");
            Console.WriteLine($"{Systems.ColorCodes.Yellow}🛒 A merchant awaits. (SHOP){Systems.ColorCodes.Reset}");
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
    public void ShowCombatStatus(Player player, Enemy enemy, 
        IReadOnlyList<ActiveEffect> playerEffects, 
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        Console.WriteLine();
        
        // Player row
        var hpBar = RenderBar(player.HP, player.MaxHP, 8, Systems.ColorCodes.HealthColor(player.HP, player.MaxHP));
        Console.Write($"You: {hpBar} {Systems.ColorCodes.HealthColor(player.HP, player.MaxHP)}{player.HP}/{player.MaxHP}{Systems.ColorCodes.Reset} HP");
        if (player.MaxMana > 0)
        {
            var mpBar = RenderBar(player.Mana, player.MaxMana, 6, Systems.ColorCodes.ManaColor(player.Mana, player.MaxMana));
            Console.Write($"  {mpBar} {Systems.ColorCodes.ManaColor(player.Mana, player.MaxMana)}{player.Mana}/{player.MaxMana}{Systems.ColorCodes.Reset} MP");
        }
        // Active player effects
        if (playerEffects.Count > 0)
        {
            Console.Write("  ");
            foreach (var e in playerEffects)
                Console.Write($"{Systems.ColorCodes.Yellow}[{EffectIcon(e.Effect)} {e.Effect} {e.RemainingTurns}t]{Systems.ColorCodes.Reset} ");
        }
        Console.WriteLine();
        
        // Enemy row
        var enemyHpBar = RenderBar(enemy.HP, enemy.MaxHP, 8, Systems.ColorCodes.HealthColor(enemy.HP, enemy.MaxHP));
        Console.Write($"{enemy.Name}: {enemyHpBar} {Systems.ColorCodes.HealthColor(enemy.HP, enemy.MaxHP)}{enemy.HP}/{enemy.MaxHP}{Systems.ColorCodes.Reset} HP");
        // Active enemy effects
        if (enemyEffects.Count > 0)
        {
            Console.Write("  ");
            foreach (var e in enemyEffects)
                Console.Write($"{Systems.ColorCodes.Red}[{EffectIcon(e.Effect)} {e.Effect} {e.RemainingTurns}t]{Systems.ColorCodes.Reset} ");
        }
        Console.WriteLine();
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
        
        // Rogue: Show Combo Points
        if (player.Class == PlayerClass.Rogue)
        {
            var comboDots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            ShowColoredStat("⚡ Combo:", comboDots, Systems.ColorCodes.Yellow);
        }
        
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
            
            foreach (var group in player.Inventory.GroupBy(i => i.Name))
            {
                var item  = group.First();
                var count = group.Count();
                var icon  = ItemTypeIcon(item.Type);
                var isEquipped = item == player.EquippedWeapon
                              || item == player.EquippedAccessory
                              || player.AllEquippedArmor.Contains(item);
                var equippedTag = isEquipped
                    ? $" {Systems.ColorCodes.Green}[E]{Systems.ColorCodes.Reset}"
                    : string.Empty;
                var countTag    = count > 1 ? $" ×{count}" : string.Empty;
                var statLabel   = PrimaryStatLabel(item);
                var nameField   = $"  {icon} {ColorizeItemName(item)}{equippedTag}{countTag}";
                var statColored = $"{Systems.ColorCodes.Cyan}{statLabel}{Systems.ColorCodes.Reset}";
                var wtEach      = count > 1 ? $"[{item.Weight} wt each]" : $"[{item.Weight} wt]";
                Console.WriteLine($"{PadRightVisible(nameField, 32)}{PadRightVisible(statColored, 22)}{Systems.ColorCodes.Gray}{wtEach}{Systems.ColorCodes.Reset}");
            }
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Renders a box-drawn loot drop card with type icon, item name, primary stat, and weight.
    /// </summary>
    public void ShowLootDrop(Item item, Player player, bool isElite = false)
    {
        var icon = ItemTypeIcon(item.Type);
        var stat = PrimaryStatLabel(item);
        var namePad = new string(' ', Math.Max(0, 35 - VisualWidth(icon) - TruncateName(item.Name).Length));
        var header = isElite ? $"✦ {Systems.ColorCodes.Yellow}ELITE LOOT DROP{Systems.ColorCodes.Reset}" : "✦ LOOT DROP";
        var tierLabel = item.Tier switch
        {
            ItemTier.Uncommon  => $"[{Systems.ColorCodes.Green}Uncommon{Systems.ColorCodes.Reset}]",
            ItemTier.Rare      => $"[{Systems.ColorCodes.BrightCyan}Rare{Systems.ColorCodes.Reset}]",
            ItemTier.Epic      => $"[{Systems.ColorCodes.Magenta}Epic{Systems.ColorCodes.Reset}]",
            ItemTier.Legendary => $"[{Systems.ColorCodes.Yellow}Legendary{Systems.ColorCodes.Reset}]",
            _                  => "[Common]"
        };
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine($"║  {PadRightVisible(header, 36)}║");
        Console.WriteLine($"║  {PadRightVisible(tierLabel, 36)}║");
        Console.WriteLine($"║  {icon} {ColorizeItemName(item)}{namePad}║");

        // Build stat line with optional "new best" indicator
        string statLine = stat;
        if (item.AttackBonus > 0 && player.EquippedWeapon != null)
        {
            int delta = item.AttackBonus - player.EquippedWeapon.AttackBonus;
            if (delta > 0)
                statLine += $"  {Systems.ColorCodes.Green}(+{delta} vs equipped!){Systems.ColorCodes.Reset}";
        }
        else if (item.DefenseBonus > 0 && player.EquippedChest != null)
        {
            int delta = item.DefenseBonus - player.EquippedChest.DefenseBonus;
            if (delta > 0)
                statLine += $"  {Systems.ColorCodes.Green}(+{delta} vs equipped!){Systems.ColorCodes.Reset}";
        }
        else if (item.Type == ItemType.Accessory && player.EquippedAccessory != null)
        {
            // Compare all relevant stats for accessories
            var deltas = new List<string>();
            if (item.StatModifier > player.EquippedAccessory.StatModifier)
                deltas.Add($"+{item.StatModifier - player.EquippedAccessory.StatModifier} HP");
            if (item.AttackBonus > player.EquippedAccessory.AttackBonus)
                deltas.Add($"+{item.AttackBonus - player.EquippedAccessory.AttackBonus} ATK");
            if (item.DefenseBonus > player.EquippedAccessory.DefenseBonus)
                deltas.Add($"+{item.DefenseBonus - player.EquippedAccessory.DefenseBonus} DEF");
            
            if (deltas.Count > 0)
                statLine += $"  {Systems.ColorCodes.Green}({string.Join(", ", deltas)} vs equipped!){Systems.ColorCodes.Reset}";
        }
        var statWithWeight = $"{Systems.ColorCodes.Cyan}{statLine}{Systems.ColorCodes.Reset} • {item.Weight} wt";
        Console.WriteLine($"║  {PadRightVisible(statWithWeight, 36)}║");
        Console.WriteLine("╚══════════════════════════════════════╝");
    }

    /// <summary>
    /// Displays a gold pickup notification with running total.
    /// </summary>
    public void ShowGoldPickup(int amount, int newTotal)
    {
        Console.WriteLine($"  💰 {Systems.ColorCodes.Yellow}+{amount} gold{Systems.ColorCodes.Reset}  (Total: {newTotal}g)");
    }

    /// <summary>
    /// Displays a pickup confirmation line with slot/weight usage.
    /// </summary>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax)
    {
        var icon = ItemTypeIcon(item.Type);
        var stat = PrimaryStatLabel(item);
        Console.WriteLine($"  {icon} Picked up: {ColorizeItemName(item)}  {Systems.ColorCodes.Cyan}({stat}){Systems.ColorCodes.Reset}");
        var slotsRatio = (double)slotsCurrent / slotsMax;
        var wtRatio    = (double)weightCurrent / weightMax;
        var slotsColor = slotsRatio > 0.95 ? Systems.ColorCodes.Red
                       : slotsRatio > 0.80 ? Systems.ColorCodes.Yellow
                       : Systems.ColorCodes.Green;
        var wtColor    = wtRatio > 0.95 ? Systems.ColorCodes.Red
                       : wtRatio > 0.80 ? Systems.ColorCodes.Yellow
                       : Systems.ColorCodes.Green;
        Console.WriteLine($"  Slots: {slotsColor}{slotsCurrent}/{slotsMax}{Systems.ColorCodes.Reset}  •  Weight: {wtColor}{weightCurrent}/{weightMax}{Systems.ColorCodes.Reset}");
        if (weightCurrent > weightMax * 0.8)
            Console.WriteLine($"  {Systems.ColorCodes.Yellow}⚠ Inventory weight: {weightCurrent}/{weightMax} — nearly full!{Systems.ColorCodes.Reset}");
    }

    /// <summary>
    /// Renders a full stat card for an item (EXAMINE command).
    /// </summary>
    public void ShowItemDetail(Item item)
    {
        const int W = 36;
        var border     = new string('═', W);
        var icon       = ItemTypeIcon(item.Type);
        var titleName  = TruncateName(item.Name).ToUpperInvariant();
        var titleColor = item.Tier switch
        {
            ItemTier.Uncommon  => Systems.ColorCodes.Green,
            ItemTier.Rare      => Systems.ColorCodes.BrightCyan,
            ItemTier.Epic      => Systems.ColorCodes.Magenta,
            ItemTier.Legendary => Systems.ColorCodes.Yellow,
            _                  => Systems.ColorCodes.BrightWhite
        };
        var titlePlain = $"  {icon} {titleName}";
        var titlePad   = new string(' ', Math.Max(0, W - titlePlain.Length));
        Console.WriteLine($"╔{border}╗");
        Console.WriteLine($"║  {icon} {titleColor}{titleName}{Systems.ColorCodes.Reset}{titlePad}║");
        Console.WriteLine($"╠{border}╣");
        Console.WriteLine($"║  {"Type:",-10}{item.Type.ToString().PadRight(W - 12)}║");
        if (item.AttackBonus != 0)
            Console.WriteLine($"║  {"Attack:",-10}{Systems.ColorCodes.Red}+{item.AttackBonus}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 12 - (item.AttackBonus.ToString().Length + 1)))}║");
        if (item.DefenseBonus != 0)
            Console.WriteLine($"║  {"Defense:",-10}{Systems.ColorCodes.Cyan}+{item.DefenseBonus}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 12 - (item.DefenseBonus.ToString().Length + 1)))}║");
        if (item.HealAmount != 0)
            Console.WriteLine($"║  {"Heal:",-10}{Systems.ColorCodes.Green}+{item.HealAmount} HP{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 16 - item.HealAmount.ToString().Length))}║");
        if (item.ManaRestore != 0)
            Console.WriteLine($"║  {"Mana:",-10}{Systems.ColorCodes.Blue}+{item.ManaRestore}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 12 - (item.ManaRestore.ToString().Length + 1)))}║");
        if (item.MaxManaBonus != 0)
            Console.WriteLine($"║  {"Max Mana:",-10}{Systems.ColorCodes.Blue}+{item.MaxManaBonus}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 12 - (item.MaxManaBonus.ToString().Length + 1)))}║");
        if (item.DodgeBonus > 0)
            Console.WriteLine($"║  {"Dodge:",-10}+{item.DodgeBonus:P0}{new string(' ', Math.Max(0, W - 12 - $"+{item.DodgeBonus:P0}".Length))}║");
        Console.WriteLine($"║  {"Weight:",-10}{item.Weight}{new string(' ', Math.Max(0, W - 11 - item.Weight.ToString().Length))}║");
        if (item.AppliesBleedOnHit)
            Console.WriteLine($"║  {"Bleed:",-10}{Systems.ColorCodes.BrightRed}On Hit{Systems.ColorCodes.Reset}{new string(' ', W - 16)}║");
        if (item.PoisonImmunity)
            Console.WriteLine($"║  {"Poison:",-10}Immune{new string(' ', W - 16)}║");
        if (!string.IsNullOrEmpty(item.Description))
        {
            Console.WriteLine($"╠{border}╣");
            // Word-wrap description to fit box width
            var words = item.Description.Split(' ');
            var line  = "  ";
            foreach (var word in words)
            {
                if (line.Length + word.Length + 1 > W)
                {
                    Console.WriteLine($"║{line.PadRight(W)}║");
                    line = "  " + word;
                }
                else
                {
                    line += (line == "  " ? "" : " ") + word;
                }
            }
            if (line.Trim().Length > 0)
                Console.WriteLine($"║{line.PadRight(W)}║");
        }
        Console.WriteLine($"╚{border}╝");
    }

    /// <summary>
    /// Renders a box-drawn card for each shop item showing type icon, tier-colored name,
    /// tier badge, primary stat, weight, and price (green if affordable, red if not).
    /// </summary>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        const int Inner = 40;
        var border = new string('═', Inner);
        Console.WriteLine();
        Console.WriteLine($"Your gold: {Systems.ColorCodes.Yellow}{playerGold}g{Systems.ColorCodes.Reset}");
        Console.WriteLine();

        int idx = 1;
        foreach (var (item, price) in stock)
        {
            var icon       = ItemTypeIcon(item.Type);
            var tierBadge  = $"[{item.Tier}]";
            var tierColor  = item.Tier switch
            {
                ItemTier.Uncommon  => Systems.ColorCodes.Green,
                ItemTier.Rare      => Systems.ColorCodes.BrightCyan,
                ItemTier.Epic      => Systems.ColorCodes.Magenta,
                ItemTier.Legendary => Systems.ColorCodes.Yellow,
                _                  => Systems.ColorCodes.BrightWhite
            };
            var priceColor = playerGold >= price ? Systems.ColorCodes.Green : Systems.ColorCodes.Red;
            var stat       = PrimaryStatLabel(item);

            // ANSI-safe padding: compute lengths from plain (uncolored) strings
            var l1Lead  = $"  [{idx}] {icon} ";
            var pad1    = new string(' ', Math.Max(0, Inner - l1Lead.Length - TruncateName(item.Name).Length - tierBadge.Length - 2));
            var l2Lead  = $"  {stat}  •  {item.Weight} wt";
            var priceStr = $"{price} gold";
            // "💰 " → U+1F4B0 is a surrogate pair (2 C# chars) + space = 3 chars
            var pad2    = new string(' ', Math.Max(1, Inner - l2Lead.Length - 3 - priceStr.Length - 2));

            Console.WriteLine($"╔{border}╗");
            Console.WriteLine($"║{l1Lead}{ColorizeItemName(item)}{pad1}{tierColor}{tierBadge}{Systems.ColorCodes.Reset}  ║");
            Console.WriteLine($"║{l2Lead}{pad2}💰 {priceColor}{priceStr}{Systems.ColorCodes.Reset}  ║");
            Console.WriteLine($"╚{border}╝");
            idx++;
        }
        Console.WriteLine("[#] Buy  [X] Leave");
    }

    /// <summary>
    /// Renders a numbered list of items the player can sell, showing tier-colored names
    /// and green sell prices, with a header displaying current gold.
    /// </summary>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        const int Inner = 40;
        var itemList = items.ToList();
        Console.WriteLine();
        Console.WriteLine($"=== SELL ITEMS ===  Your gold: {Systems.ColorCodes.Yellow}{playerGold}g{Systems.ColorCodes.Reset}");
        Console.WriteLine(new string('─', Inner));

        for (int i = 0; i < itemList.Count; i++)
        {
            var (item, price) = itemList[i];
            var tierColor = item.Tier switch
            {
                ItemTier.Uncommon  => Systems.ColorCodes.Green,
                ItemTier.Rare      => Systems.ColorCodes.BrightCyan,
                ItemTier.Epic      => Systems.ColorCodes.Magenta,
                ItemTier.Legendary => Systems.ColorCodes.Yellow,
                _                  => Systems.ColorCodes.BrightWhite
            };
            Console.WriteLine($"  [{i + 1}] {tierColor}{TruncateName(item.Name)}{Systems.ColorCodes.Reset}  — sell for {Systems.ColorCodes.Green}{price}g{Systems.ColorCodes.Reset}");
        }

        Console.WriteLine(new string('─', Inner));
        Console.WriteLine($"  {Systems.ColorCodes.Gray}[#] Sell item  [X] Cancel{Systems.ColorCodes.Reset}");
        Console.WriteLine();
    }

    /// <summary>
    /// Renders a box-drawn recipe card showing the result item's stats and each ingredient
    /// with a ✅ (player has it) or ❌ (missing) availability indicator.
    /// </summary>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients)
    {
        const int W = 40;
        var icon      = ItemTypeIcon(result.Type);
        var stat      = PrimaryStatLabel(result);

        // Plain-text lengths for ANSI-safe padding
        var hdrPlain    = $"  \U0001F528 RECIPE: {recipeName}";  // 🔨 = U+1F528, surrogate pair
        var resultPlain = $"  Result: {icon} {TruncateName(result.Name)}";
        var statPlain   = $"  Stats:  {stat}";
        var ingHeader   = "  Ingredients:";

        Console.WriteLine($"╔{new string('═', W)}╗");
        Console.WriteLine($"║{hdrPlain}{new string(' ', Math.Max(0, W - hdrPlain.Length))}║");
        Console.WriteLine($"╠{new string('═', W)}╣");
        Console.WriteLine($"║  Result: {icon} {ColorizeItemName(result)}{new string(' ', Math.Max(0, W - resultPlain.Length))}║");
        Console.WriteLine($"║  Stats:  {Systems.ColorCodes.Cyan}{stat}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - statPlain.Length))}║");
        Console.WriteLine($"╠{new string('═', W)}╣");
        Console.WriteLine($"║{ingHeader}{new string(' ', Math.Max(0, W - ingHeader.Length))}║");
        foreach (var (ingredient, hasIt) in ingredients)
        {
            // ✅ = U+2705, ❌ = U+274C — both BMP (1 C# char each), visually ~2 cols wide
            var checkIcon  = hasIt ? "✅" : "❌";
            var checkColor = hasIt ? Systems.ColorCodes.Green : Systems.ColorCodes.Red;
            // "    {emoji} {ingredient}" — 4 spaces + emoji(2 visual) + space(1) + ingredient = keep ingredient at 33
            Console.WriteLine($"║    {checkColor}{checkIcon}{Systems.ColorCodes.Reset} {ingredient,-33}║");
        }
        Console.WriteLine($"╚{new string('═', W)}╝");
    }

    /// <summary>Shows shop inventory and returns the selected item index (1-based), or 0 to cancel.</summary>
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        ShowShop(stockList, playerGold);
        var options = stockList
            .Select((s, i) => ($"[{i + 1}] {TruncateName(s.item.Name)}  {s.price}g", i + 1))
            .Append(("Cancel", 0))
            .ToList();
        return SelectFromMenu(options.AsReadOnly(), _input);
    }

    /// <summary>Shows sell menu and returns the selected item index (1-based), or 0 to cancel.</summary>
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        var itemList = items.ToList();
        ShowSellMenu(itemList, playerGold);
        var options = itemList
            .Select((s, i) => ($"[{i + 1}] {TruncateName(s.item.Name)}  {s.sellPrice}g", i + 1))
            .Append(("Cancel", 0))
            .ToList();
        return SelectFromMenu(options.AsReadOnly(), _input);
    }

    /// <summary>Presents the Shrine blessing choices as an arrow-key menu and returns 1–4 or 0 (leave).</summary>
    public int ShowShrineMenuAndSelect(int playerGold)
    {
        var options = new (string Label, int Value)[]
        {
            ($"Heal fully        — 30g  (Your gold: {playerGold}g)", 1),
            ("Bless             — 50g  (+2 ATK/DEF permanently)", 2),
            ("Fortify           — 75g  (MaxHP +10, permanent)", 3),
            ("Meditate          — 75g  (MaxMana +10, permanent)", 4),
            ("Leave", 0),
        };
        return SelectFromMenu(options.AsReadOnly(), _input, "✨ [Shrine Menu]");
    }

    /// <summary>
    /// Presents the shop menu with merchant stock, a Sell Items option, and Leave.
    /// Returns the selected item index (1-based for buying), -1 for Sell, or 0 to Leave.
    /// </summary>
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        ShowShop(stockList, playerGold);   // Use existing box-drawing render
        var options = stockList
            .Select((s, i) => ($"{TruncateName(s.item.Name)}  {s.price}g", i + 1))
            .Append(("💰 Sell Items", -1))
            .Append(("Leave", 0))
            .ToArray();
        return SelectFromMenu(options.AsReadOnly(), _input);
    }

    /// <summary>Presents a Yes/No confirmation menu. Returns true if Yes selected.</summary>
    public bool ShowConfirmMenu(string prompt)
    {
        var options = new (string Label, bool Value)[]
        {
            ("Yes", true),
            ("No", false),
        };
        return SelectFromMenu(options.AsReadOnly(), _input, prompt);
    }


    // ── helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Presents a labeled list of choices to the player and returns the selected value.
    /// Falls back to numbered ReadLine input when <see cref="IInputReader.IsInteractive"/> is false.
    /// For interactive sessions, delegates to the injected <see cref="IMenuNavigator"/>.
    /// </summary>
    private T SelectFromMenu<T>(
        IReadOnlyList<(string Label, T Value)> options,
        IInputReader input,
        string? header = null)
    {
        if (header != null)
        {
            Console.WriteLine(header);
            Console.WriteLine();
        }

        if (!input.IsInteractive)
        {
            // Text-mode fallback for tests and redirected input.
            for (int i = 0; i < options.Count; i++)
                Console.WriteLine($"  [{i + 1}] {options[i].Label}");
            Console.WriteLine();

            while (true)
            {
                Console.Write("> ");
                var line = input.ReadLine()?.Trim() ?? "";
                if (int.TryParse(line, out int n) && n >= 1 && n <= options.Count)
                    return options[n - 1].Value;
                Console.WriteLine($"{Systems.ColorCodes.Red}Invalid choice. Please enter 1–{options.Count}.{Systems.ColorCodes.Reset}");
            }
        }

        // Delegate to injected IMenuNavigator for interactive arrow-key navigation.
        var menuOptions = options
            .Select(o => new MenuOption<T>(o.Label, o.Value))
            .ToList()
            .AsReadOnly();
        return _navigator.Select(menuOptions);
    }

    private static string ItemTypeIcon(ItemType type) => type switch
    {
        ItemType.Weapon     => "⚔",
        ItemType.Armor      => "🛡",
        ItemType.Consumable => "🧪",
        ItemType.Accessory  => "💍",
        _                   => "•"
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

    /// <summary>
    /// Returns the item's name wrapped in the ANSI color appropriate for its tier:
    /// BrightWhite (Common), Green (Uncommon), BrightCyan (Rare).
    /// Truncates the name via TruncateName before applying color codes.
    /// </summary>
    private static string ColorizeItemName(Item item)
    {
        var fullName = BuildFullItemName(item);
        return Systems.ColorCodes.ColorizeItemName(TruncateName(fullName), item.Tier);
    }

    /// <summary>Builds the full display name of an item by combining Prefix, base Name, and Suffix.</summary>
    private static string BuildFullItemName(Item item)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(item.Prefix)) parts.Add(item.Prefix.Trim());
        parts.Add(item.Name);
        if (!string.IsNullOrWhiteSpace(item.Suffix)) parts.Add(item.Suffix.Trim());
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Safety guard for the display layer: returns the name as-is if 30 chars or fewer,
    /// otherwise truncates to the first 27 characters and appends "...".
    /// </summary>
    private static string TruncateName(string? name, int maxLength = 30)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= maxLength)
            return name ?? string.Empty;
        return name[..27] + "...";
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
        Console.WriteLine("    sell                         Sell items to the merchant (if one is present)");
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
    public void ShowCommandPrompt(Player? player = null)
    {
        if (player == null)
        {
            Console.Write("> ");
            return;
        }
        
        var hpBar = RenderBar(player.HP, player.MaxHP, 6, Systems.ColorCodes.HealthColor(player.HP, player.MaxHP));
        var hpText = $"{player.HP}/{player.MaxHP}";
        
        Console.Write($"[{hpBar} {Systems.ColorCodes.HealthColor(player.HP, player.MaxHP)}{hpText}{Systems.ColorCodes.Reset} HP");
        
        if (player.MaxMana > 0)
        {
            var mpBar = RenderBar(player.Mana, player.MaxMana, 4, Systems.ColorCodes.ManaColor(player.Mana, player.MaxMana));
            Console.Write($" │ {mpBar} {Systems.ColorCodes.ManaColor(player.Mana, player.MaxMana)}{player.Mana}/{player.MaxMana}{Systems.ColorCodes.Reset} MP");
        }
        
        Console.Write($"] {Systems.ColorCodes.Gray}>{Systems.ColorCodes.Reset} ");
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

        // Fog of war: only render visited rooms (and the current room)
        var visiblePositions = positions
            .Where(kv => kv.Key.Visited || kv.Key == currentRoom)
            .ToList();

        if (visiblePositions.Count == 0) return;

        int minX = visiblePositions.Min(kv => kv.Value.x);
        int maxX = visiblePositions.Max(kv => kv.Value.x);
        int minY = visiblePositions.Min(kv => kv.Value.y);
        int maxY = visiblePositions.Max(kv => kv.Value.y);

        // Build lookup: coordinate → visible room
        var grid = new Dictionary<(int x, int y), Room>();
        foreach (var (room, pos) in visiblePositions)
            grid[pos] = room;

        // Render
        Console.WriteLine();
        Console.WriteLine("═══ MAP ═══   N");
        Console.WriteLine("              ↑");

        for (int y = minY; y <= maxY; y++)
        {
            // === Room row ===
            Console.Write("  ");
            for (int x = minX; x <= maxX; x++)
            {
                if (!grid.TryGetValue((x, y), out var r))
                {
                    Console.Write(x < maxX ? "    " : "   ");
                    continue;
                }

                string symbol = GetRoomSymbol(r, currentRoom);

                string color = (r == currentRoom)
                    ? Systems.ColorCodes.Bold + Systems.ColorCodes.BrightWhite
                    : (r.Enemy != null && r.Enemy.HP > 0)
                        ? Systems.ColorCodes.Red
                        : Systems.ColorCodes.GetRoomTypeColor(r.Type);

                Console.Write($"{color}{symbol}{Systems.ColorCodes.Reset}");

                if (x < maxX)
                {
                    bool hasConnector = r.Exits.ContainsKey(Direction.East)
                        && grid.ContainsKey((x + 1, y));
                    Console.Write(hasConnector ? "-" : " ");
                }
            }
            Console.WriteLine();

            // === Connector row (between this row and next) ===
            if (y < maxY)
            {
                Console.Write("  ");
                for (int x = minX; x <= maxX; x++)
                {
                    bool hasSouth = grid.TryGetValue((x, y), out var rS)
                        && rS.Exits.ContainsKey(Direction.South)
                        && grid.ContainsKey((x, y + 1));

                    Console.Write(hasSouth ? " | " : "   ");
                    if (x < maxX) Console.Write(" ");
                }
                Console.WriteLine();
            }
        }

        // === Color-coded legend ===
        Console.WriteLine();
        Console.WriteLine("Legend:");
        Console.Write($"  {Systems.ColorCodes.Bold}{Systems.ColorCodes.BrightWhite}[*]{Systems.ColorCodes.Reset} You       ");
        Console.Write($"[E] Exit       ");
        Console.Write($"{Systems.ColorCodes.Red}[!] Enemy{Systems.ColorCodes.Reset}      ");
        Console.Write($"[S] Shrine     ");
        Console.WriteLine($"[+] Cleared");
        Console.Write($"  [B] Boss      ");
        Console.Write($"- Corridor (E/W)    ");
        Console.WriteLine($"| Corridor (N/S)");
        Console.Write("  Room types: ");
        string[] typeNames  = { "Standard", "Dark",     "Mossy",   "Flooded", "Scorched", "Ancient", "Shrine", "Library", "Armory" };
        RoomType[] types    = { RoomType.Standard, RoomType.Dark, RoomType.Mossy, RoomType.Flooded, RoomType.Scorched, RoomType.Ancient, RoomType.ForgottenShrine, RoomType.PetrifiedLibrary, RoomType.ContestedArmory };
        for (int i = 0; i < typeNames.Length; i++)
        {
            string c = Systems.ColorCodes.GetRoomTypeColor(types[i]);
            Console.Write($"{c}{typeNames[i]}{Systems.ColorCodes.Reset}");
            if (i < typeNames.Length - 1) Console.Write("  ");
        }
        Console.WriteLine();
        Console.WriteLine();
    }

    private static string GetRoomSymbol(Room r, Room currentRoom)
    {
        if (r == currentRoom)                              return "[*]";
        if (!r.Visited)                                    return $"{Systems.ColorCodes.Gray}[?]{Systems.ColorCodes.Reset}";
        if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0) return "[B]";
        if (r.IsExit)                                      return "[E]";
        if (r.Enemy != null && r.Enemy.HP > 0)             return "[!]";
        if (r.HasShrine && !r.ShrineUsed)                  return "[S]";
        return "[+]";
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
            Console.Write($"{TruncateName(oldItem.Name),-28}");
        else
            Console.Write($"{"(none)",-28}");
        Console.WriteLine("║");
        
        // New item
        Console.WriteLine($"║ New:      {TruncateName(newItem.Name),-28}║");
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
        var lvlLabel = $"⭐ PRESTIGE LEVEL {prestige.PrestigeLevel}";
        var lvlPad = new string(' ', Math.Max(0, 28 - VisualWidth(lvlLabel)));
        Console.WriteLine($"{yellow}║{reset}  {yellow}{lvlLabel}{lvlPad}{reset} {yellow}║{reset}");
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

        var options = new (string Label, Difficulty Value)[]
        {
            ($"{green}CASUAL{reset}     (Enemy Power ×0.7 | Loot ×1.5 | Gold ×1.5)", Difficulty.Casual),
            ($"{yellow}NORMAL{reset}     (Enemy Power ×1.0 | Balanced)",               Difficulty.Normal),
            ($"{red}HARD{reset}       (Enemy Power ×1.3 | Loot ×0.7 | Gold ×0.7)",   Difficulty.Hard),
        };
        return SelectFromMenu(options, _input, "Choose your difficulty:");
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

        // Base stats (from Player defaults)
        const int baseHP = 100;
        const int baseAttack = 10;
        const int baseDefense = 5;
        const int baseMana = 30;

        var classes = new[] {
            (def: PlayerClassDefinition.Warrior,    icon: "⚔",  number: 1),
            (def: PlayerClassDefinition.Mage,       icon: "🔮", number: 2),
            (def: PlayerClassDefinition.Rogue,      icon: "🗡",  number: 3),
            (def: PlayerClassDefinition.Paladin,    icon: "🛡",  number: 4),
            (def: PlayerClassDefinition.Necromancer,icon: "💀", number: 5),
            (def: PlayerClassDefinition.Ranger,     icon: "🏹", number: 6)
        };

        foreach (var (def, icon, number) in classes)
        {
            // Calculate effective stats
            int effectiveHP = baseHP + def.BonusMaxHP;
            int effectiveAttack = baseAttack + def.BonusAttack;
            int effectiveDefense = baseDefense + def.BonusDefense;
            int effectiveMana = baseMana + def.BonusMaxMana;

            // Stat colors
            string hpColor   = Systems.ColorCodes.HealthColor(effectiveHP, 120);
            string atkColor  = Systems.ColorCodes.BrightRed;
            string defColor  = Systems.ColorCodes.Cyan;
            string manaColor = Systems.ColorCodes.Blue;

            // Calculate prestige-boosted stats if applicable
            string hpDisplay, atkDisplay, defDisplay;
            if (prestige != null && prestige.PrestigeLevel > 0)
            {
                int prestigeHP = effectiveHP + prestige.BonusStartHP;
                int prestigeAtk = effectiveAttack + prestige.BonusStartAttack;
                int prestigeDef = effectiveDefense + prestige.BonusStartDefense;

                hpDisplay = prestige.BonusStartHP > 0 
                    ? $"{hpColor}{effectiveHP}{reset} → {yellow}{prestigeHP}{reset} (+{prestige.BonusStartHP} prestige)"
                    : $"{hpColor}{effectiveHP}{reset}";
                atkDisplay = prestige.BonusStartAttack > 0
                    ? $"{atkColor}{effectiveAttack}{reset} → {yellow}{prestigeAtk}{reset} (+{prestige.BonusStartAttack} prestige)"
                    : $"{atkColor}{effectiveAttack}{reset}";
                defDisplay = prestige.BonusStartDefense > 0
                    ? $"{defColor}{effectiveDefense}{reset} → {yellow}{prestigeDef}{reset} (+{prestige.BonusStartDefense} prestige)"
                    : $"{defColor}{effectiveDefense}{reset}";
            }
            else
            {
                hpDisplay  = $"{hpColor}{effectiveHP}{reset}";
                atkDisplay = $"{atkColor}{effectiveAttack}{reset}";
                defDisplay = $"{defColor}{effectiveDefense}{reset}";
            }

            // Stat bars (colored)
            string hpBar   = StatBar(effectiveHP,     120, fillColor: hpColor,   resetColor: reset);
            string atkBar  = StatBar(effectiveAttack,  13, fillColor: atkColor,  resetColor: reset);
            string defBar  = StatBar(effectiveDefense,  7, fillColor: defColor,  resetColor: reset);
            string manaBar = StatBar(effectiveMana,    60, fillColor: manaColor, resetColor: reset);

            const int boxInner = 48;
            Console.WriteLine($"{cyan}┌────────────────────────────────────────────────┐{reset}");
            int nameColWidth = 39 - (icon.Length - 1); // adjust padding based on actual icon char width
            Console.WriteLine($"{cyan}│{reset} [{number}] {icon}  {def.Name.ToUpper().PadRight(nameColWidth)} {cyan}│{reset}");
            
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
            
            var manaLine = $" Mana:    {manaBar}  {manaColor}{effectiveMana}{reset}";
            var manaVisibleLen = Systems.ColorCodes.StripAnsiCodes(manaLine).Length;
            Console.WriteLine($"{cyan}│{reset}{manaLine}{new string(' ', Math.Max(0, boxInner - manaVisibleLen))}{cyan}│{reset}");
            Console.WriteLine($"{cyan}│{reset} Passive: {def.TraitDescription,-37} {cyan}│{reset}");
            Console.WriteLine($"{cyan}│{reset} {gray}\"{def.Description}\"{reset}{new string(' ', Math.Max(0, 45 - def.Description.Length))}{cyan}│{reset}");
            Console.WriteLine($"{cyan}└────────────────────────────────────────────────┘{reset}");
            Console.WriteLine();
        }

        var selectOptions = new (string Label, PlayerClassDefinition Value)[]
        {
            ("Warrior",         PlayerClassDefinition.Warrior),
            ("Mage",            PlayerClassDefinition.Mage),
            ("Rogue",           PlayerClassDefinition.Rogue),
            ("Paladin",         PlayerClassDefinition.Paladin),
            ("Necromancer",     PlayerClassDefinition.Necromancer),
            ("Ranger",          PlayerClassDefinition.Ranger),
        };
        return SelectFromMenu(selectOptions, _input, "Choose your class:");
    }

    /// <summary>
    /// Creates an ASCII stat bar visualization with optional ANSI fill color.
    /// </summary>
    private static string StatBar(int value, int max, int width = 10, string fillColor = "", string resetColor = "")
    {
        var filled = Math.Clamp((int)Math.Round((double)value / max * width), 0, width);
        return $"{fillColor}{new string('█', filled)}{resetColor}{new string('░', width - filled)}";
    }

    /// <summary>Stub implementation — displays combat start banner.</summary>
    public void ShowCombatStart(Enemy enemy)
    {
        var line = new string('═', 44);
        Console.WriteLine();
        Console.WriteLine($"{Systems.ColorCodes.BrightRed}{line}{Systems.ColorCodes.Reset}");
        var banner = "  ⚔  COMBAT BEGINS  ⚔";
        var pad = new string(' ', Math.Max(0, 44 - VisualWidth(banner)));
        Console.WriteLine($"{Systems.ColorCodes.BrightRed}{Systems.ColorCodes.Bold}{banner}{pad}{Systems.ColorCodes.Reset}");
        Console.WriteLine($"{Systems.ColorCodes.BrightRed}  {enemy.Name}{Systems.ColorCodes.Reset}");
        Console.WriteLine($"{Systems.ColorCodes.BrightRed}{line}{Systems.ColorCodes.Reset}");
        Console.WriteLine();
    }
    
    /// <summary>Stub implementation — displays combat entry flags.</summary>
    public void ShowCombatEntryFlags(Enemy enemy)
    {
        if (enemy.IsElite)
            Console.WriteLine($"  {Systems.ColorCodes.Yellow}⭐ ELITE — enhanced stats and loot{Systems.ColorCodes.Reset}");
        
        if (enemy is Systems.Enemies.DungeonBoss boss && boss.IsEnraged)
            Console.WriteLine($"  {Systems.ColorCodes.BrightRed}{Systems.ColorCodes.Bold}⚡ ENRAGED{Systems.ColorCodes.Reset}");
    }
    
    /// <summary>Stub implementation — displays level-up choice menu.</summary>
    public void ShowLevelUpChoice(Player player)
    {
        const int W = 38;
        var border = new string('═', W);
        Console.WriteLine();
        Console.WriteLine($"╔{border}╗");
        Console.WriteLine($"║  {Systems.ColorCodes.Bold}{Systems.ColorCodes.BrightWhite}★ LEVEL UP!{Systems.ColorCodes.Reset}{new string(' ', W - 13)}║");
        Console.WriteLine($"╠{border}╣");
        Console.WriteLine($"║  {Systems.ColorCodes.Yellow}[1]{Systems.ColorCodes.Reset} +5 Max HP     {Systems.ColorCodes.Gray}({player.MaxHP} → {player.MaxHP + 5}){Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 25 - (player.MaxHP + 5).ToString().Length - player.MaxHP.ToString().Length))}║");
        Console.WriteLine($"║  {Systems.ColorCodes.Yellow}[2]{Systems.ColorCodes.Reset} +2 Attack     {Systems.ColorCodes.Gray}({player.Attack} → {player.Attack + 2}){Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 25 - (player.Attack + 2).ToString().Length - player.Attack.ToString().Length))}║");
        Console.WriteLine($"║  {Systems.ColorCodes.Yellow}[3]{Systems.ColorCodes.Reset} +2 Defense    {Systems.ColorCodes.Gray}({player.Defense} → {player.Defense + 2}){Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 25 - (player.Defense + 2).ToString().Length - player.Defense.ToString().Length))}║");
        Console.WriteLine($"╚{border}╝");
        Console.WriteLine();
    }
    
    /// <summary>Stub implementation — displays floor banner.</summary>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)
    {
        var threatColor = floor <= 2 ? Systems.ColorCodes.Green 
                        : floor <= 4 ? Systems.ColorCodes.Yellow 
                        : Systems.ColorCodes.BrightRed;
        var threat = floor <= 2 ? "Low" : floor <= 4 ? "Moderate" : "High";
        
        const int W = 40;
        var border = new string('═', W);
        var floorLine = $"Floor {floor} of {maxFloor}";
        var variantLine = variant.Name;
        var threatLine = $"⚠ Danger: {threat}";
        
        Console.WriteLine();
        Console.WriteLine($"╔{border}╗");
        Console.WriteLine($"║  {threatColor}{floorLine.PadRight(W - 4)}{Systems.ColorCodes.Reset}  ║");
        Console.WriteLine($"║  {Systems.ColorCodes.BrightWhite}{variantLine.PadRight(W - 4)}{Systems.ColorCodes.Reset}  ║");
        Console.WriteLine($"║  {threatColor}{threatLine.PadRight(W - 4)}{Systems.ColorCodes.Reset}  ║");
        Console.WriteLine($"╚{border}╝");
        Console.WriteLine();
    }
    
    /// <summary>Stub implementation — displays enemy detail card.</summary>
    public void ShowEnemyDetail(Enemy enemy)
    {
        const int W = 36;
        var border = new string('═', W);
        var nameUpper = enemy.Name.ToUpperInvariant();
        var nameColor = enemy.IsElite ? Systems.ColorCodes.Yellow : Systems.ColorCodes.BrightRed;
        var hpBar = RenderBar(enemy.HP, enemy.MaxHP, 10, Systems.ColorCodes.HealthColor(enemy.HP, enemy.MaxHP));
        var eliteTag = enemy.IsElite ? $" {Systems.ColorCodes.Yellow}⭐ ELITE{Systems.ColorCodes.Reset}" : "";
        
        Console.WriteLine($"╔{border}╗");
        Console.WriteLine($"║  {nameColor}{nameUpper}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 2 - nameUpper.Length))}║");
        Console.WriteLine($"╠{border}╣");
        Console.WriteLine($"║  HP:      {hpBar} {enemy.HP}/{enemy.MaxHP}{new string(' ', Math.Max(0, W - 23 - enemy.HP.ToString().Length - enemy.MaxHP.ToString().Length))}║");
        Console.WriteLine($"║  ATK:     {Systems.ColorCodes.BrightRed}{enemy.Attack}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 11 - enemy.Attack.ToString().Length))}║");
        Console.WriteLine($"║  DEF:     {Systems.ColorCodes.Cyan}{enemy.Defense}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 11 - enemy.Defense.ToString().Length))}║");
        Console.WriteLine($"║  XP:      {Systems.ColorCodes.Green}{enemy.XPValue}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 11 - enemy.XPValue.ToString().Length))}║");
        if (!string.IsNullOrEmpty(eliteTag))
            Console.WriteLine($"║  {eliteTag}{new string(' ', Math.Max(0, W - 12))}║");
        Console.WriteLine($"╚{border}╝");
    }
    
    /// <summary>Stub implementation — displays victory screen.</summary>
    public void ShowVictory(Player player, int floorsCleared, Systems.RunStats stats)
    {
        const int W = 42;
        var border = new string('═', W);
        
        Console.WriteLine();
        Console.WriteLine($"╔{border}╗");
        Console.WriteLine($"║  {Systems.ColorCodes.Yellow}{Systems.ColorCodes.Bold}✦  V I C T O R Y  ✦{Systems.ColorCodes.Reset}{new string(' ', W - 22)}║");
        Console.WriteLine($"╠{border}╣");
        Console.WriteLine($"║  {player.Name}  •  Level {player.Level}{new string(' ', Math.Max(0, W - 4 - player.Name.Length - 10 - player.Level.ToString().Length))}║");
        Console.WriteLine($"║  {floorsCleared} floor{(floorsCleared != 1 ? "s" : "")} conquered{new string(' ', Math.Max(0, W - 4 - 11 - floorsCleared.ToString().Length - (floorsCleared != 1 ? 1 : 0)))}║");
        Console.WriteLine($"╠{border}╣");
        Console.WriteLine($"║  Enemies slain:  {Systems.ColorCodes.BrightRed}{stats.EnemiesDefeated}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 20 - stats.EnemiesDefeated.ToString().Length))}║");
        Console.WriteLine($"║  Gold earned:    {Systems.ColorCodes.Yellow}{stats.GoldCollected}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 20 - stats.GoldCollected.ToString().Length))}║");
        Console.WriteLine($"║  Items found:    {Systems.ColorCodes.Cyan}{stats.ItemsFound}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 20 - stats.ItemsFound.ToString().Length))}║");
        Console.WriteLine($"║  Turns taken:    {Systems.ColorCodes.Gray}{stats.TurnsTaken}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 20 - stats.TurnsTaken.ToString().Length))}║");
        Console.WriteLine($"╚{border}╝");
        Console.WriteLine();
    }
    
    /// <summary>Stub implementation — displays game over screen.</summary>
    public void ShowGameOver(Player player, string? killedBy, Systems.RunStats stats)
    {
        const int W = 42;
        var border = new string('═', W);
        var deathLine = killedBy != null ? $"Killed by: {killedBy}" : "Cause of death: unknown";
        
        Console.WriteLine();
        Console.WriteLine($"╔{border}╗");
        Console.WriteLine($"║  {Systems.ColorCodes.BrightRed}{Systems.ColorCodes.Bold}☠  G A M E  O V E R  ☠{Systems.ColorCodes.Reset}{new string(' ', W - 24)}║");
        Console.WriteLine($"╠{border}╣");
        Console.WriteLine($"║  {player.Name}  •  Level {player.Level}{new string(' ', Math.Max(0, W - 4 - player.Name.Length - 10 - player.Level.ToString().Length))}║");
        Console.WriteLine($"║  {Systems.ColorCodes.Red}{deathLine}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 4 - deathLine.Length))}║");
        Console.WriteLine($"╠{border}╣");
        Console.WriteLine($"║  Enemies slain:  {Systems.ColorCodes.BrightRed}{stats.EnemiesDefeated}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 20 - stats.EnemiesDefeated.ToString().Length))}║");
        Console.WriteLine($"║  Floors reached: {Systems.ColorCodes.Cyan}{stats.FloorsVisited}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 20 - stats.FloorsVisited.ToString().Length))}║");
        Console.WriteLine($"║  Turns survived: {Systems.ColorCodes.Gray}{stats.TurnsTaken}{Systems.ColorCodes.Reset}{new string(' ', Math.Max(0, W - 20 - stats.TurnsTaken.ToString().Length))}║");
        Console.WriteLine($"╚{border}╝");
        Console.WriteLine();
    }

    /// <summary>Renders the enemy's ASCII art in a styled box, if art is present.</summary>
    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt == null || enemy.AsciiArt.Length == 0)
            return;

        const int innerWidth = 36;
        var boxBorder = new string('─', innerWidth + 2);
        var artColor = enemy.IsElite ? Systems.ColorCodes.Yellow
            : enemy.MaxHP >= 100 ? Systems.ColorCodes.BrightRed
            : enemy.MaxHP >= 60  ? Systems.ColorCodes.BrightRed
            : enemy is Systems.Enemies.Wraith or Systems.Enemies.GoblinShaman ? Systems.ColorCodes.Cyan
            : enemy is Systems.Enemies.Skeleton ? Systems.ColorCodes.Gray
            : Systems.ColorCodes.BrightRed;

        Console.WriteLine($"┌{boxBorder}┐");
        foreach (var line in enemy.AsciiArt)
        {
            var colored = $"{artColor}{line}{Systems.ColorCodes.Reset}";
            Console.WriteLine($"│ {PadRightVisible(colored, innerWidth)} │");
        }
        Console.WriteLine($"└{boxBorder}┘");
    }

    private static string RenderBar(int current, int max, int width, string fillColor, string emptyColor = Systems.ColorCodes.Gray)
    {
        current = Math.Clamp(current, 0, max);
        if (max <= 0) return emptyColor + new string('░', width) + Systems.ColorCodes.Reset;
        int fillCount = (int)Math.Round((double)current / max * width);
        fillCount = Math.Clamp(fillCount, 0, width);
        return fillColor   + new string('█', fillCount)          + Systems.ColorCodes.Reset
             + emptyColor  + new string('░', width - fillCount)  + Systems.ColorCodes.Reset;
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

    private static int VisibleLength(string s)
        => Systems.ColorCodes.StripAnsiCodes(s).Length;

    /// <summary>
    /// Wide BMP characters (U+0000–U+FFFF) that render as 2 terminal columns.
    /// Surrogate-pair emoji (U+10000+) have .Length == 2 and need no adjustment.
    /// </summary>
    private static readonly HashSet<char> _wideBmpChars =
    [
        '⭐', // U+2B50 WHITE MEDIUM STAR
        '⚔',  // U+2694 CROSSED SWORDS
        '⚡',  // U+26A1 HIGH VOLTAGE SIGN
        '✅',  // U+2705 WHITE HEAVY CHECK MARK
        '❌',  // U+274C CROSS MARK
        '✨',  // U+2728 SPARKLES
        '☠',  // U+2620 SKULL AND CROSSBONES
    ];

    /// <summary>
    /// Returns the visual column width of a string, accounting for BMP emoji that
    /// render as 2 columns in modern terminals. Surrogate-pair emoji (U+10000+)
    /// already contribute 2 to <see cref="string.Length"/> and need no adjustment.
    /// </summary>
    private static int VisualWidth(string s)
        => s.Length + s.Count(c => _wideBmpChars.Contains(c));

    private static string PadRightVisible(string s, int totalWidth)
        => s + new string(' ', Math.Max(0, totalWidth - VisibleLength(s)));

    private static string PadLeftVisible(string s, int totalWidth)
        => new string(' ', Math.Max(0, totalWidth - VisibleLength(s))) + s;

    // ── WI-6: Level-up stat choice ─────────────────────────────────────────────

    /// <summary>
    /// Arrow-key navigable level-up stat choice. Returns 1 (+5 MaxHP), 2 (+2 Attack), or 3 (+2 Defense).
    /// Falls back to numbered text input when ReadKey is unavailable.
    /// </summary>
    public int ShowLevelUpChoiceAndSelect(Player player)
    {
        var options = new (string Label, int Value)[]
        {
            ($"+5 Max HP     {Systems.ColorCodes.Gray}({player.MaxHP} → {player.MaxHP + 5}){Systems.ColorCodes.Reset}", 1),
            ($"+2 Attack     {Systems.ColorCodes.Gray}({player.Attack} → {player.Attack + 2}){Systems.ColorCodes.Reset}", 2),
            ($"+2 Defense    {Systems.ColorCodes.Gray}({player.Defense} → {player.Defense + 2}){Systems.ColorCodes.Reset}", 3),
        };
        return SelectFromMenu(options, _input, "★ Choose a stat bonus:");
    }

    // ── WI-7: Combat action menu ───────────────────────────────────────────────

    /// <summary>
    /// Arrow-key navigable combat action menu. Returns "A" (Attack), "B" (Ability), or "F" (Flee).
    /// Shows a resource context line above the menu. Falls back to numbered text input when ReadKey
    /// is unavailable.
    /// </summary>
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
    {
        // Show class-specific resource line so the player can assess ability costs
        var ctx = new System.Text.StringBuilder($"Mana: {player.Mana}/{player.MaxMana}");
        if (player.Class == Models.PlayerClass.Rogue)
        {
            var dots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            ctx.Append($"  ⚡ Combo: {dots}");
        }
        if (player.Class == Models.PlayerClass.Mage && player.IsManaShieldActive)
            ctx.Append(" [SHIELD ACTIVE]");
        if (player.Class == Models.PlayerClass.Paladin && player.DivineShieldTurnsRemaining > 0)
            ctx.Append($" [DIVINE SHIELD: {player.DivineShieldTurnsRemaining}T]");
        Console.WriteLine(ctx.ToString());
        Console.WriteLine();

        var options = new (string Label, string Value)[]
        {
            ("⚔  Attack",  "A"),
            ("✨ Ability",  "B"),
            ("🏃 Flee",     "F"),
            ("🧪 Use Item", "I"),
        };
        return SelectFromMenu(options, _input);
    }

    // ── WI-8: Crafting recipe selection ───────────────────────────────────────

    /// <summary>
    /// Arrow-key navigable crafting recipe selection menu. Returns 1-based recipe index, or 0 for cancel.
    /// Falls back to numbered text input when ReadKey is unavailable.
    /// </summary>
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes)
    {
        var recipeList = recipes.ToList();
        var options = recipeList
            .Select((r, i) => (
                Label: r.canCraft
                    ? $"{Systems.ColorCodes.Green}✅ {r.recipeName}{Systems.ColorCodes.Reset}"
                    : $"{Systems.ColorCodes.Red}❌ {r.recipeName}{Systems.ColorCodes.Reset}",
                Value: i + 1))
            .Append(("↩  Cancel", 0))
            .ToArray();
        return SelectFromMenu(options, _input, "=== CRAFTING — Choose a recipe ===");
    }

    /// <summary>
    /// Displays a choice between two trap options and a leave option.
    /// </summary>
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2)
    {
        var options = new (string Label, int Value)[]
        {
            (option1, 1),
            (option2, 2),
            ("Leave", 0),
        };
        return SelectFromMenu(options.AsReadOnly(), _input, header);
    }

    /// <summary>
    /// Displays the Forgotten Shrine menu with blessing options.
    /// </summary>
    public int ShowForgottenShrineMenuAndSelect()
    {
        var options = new (string Label, int Value)[]
        {
            ("Holy Strength   — +5 ATK (lasts until next floor)", 1),
            ("Sacred Ground   — Auto-heal at shrines", 2),
            ("Warding Veil    — 20% chance to deflect enemy attacks this floor", 3),
            ("Leave", 0),
        };
        return SelectFromMenu(options.AsReadOnly(), _input, "🕯 [Forgotten Shrine] — choose a blessing:");
    }

    /// <summary>
    /// Displays the Contested Armory menu with approach options based on player defense.
    /// </summary>
    public int ShowContestedArmoryMenuAndSelect(int playerDefense)
    {
        var options = new (string Label, int Value)[]
        {
            ($"Careful approach — disarm traps (requires DEF > 12, yours: {playerDefense})", 1),
            ("Reckless grab   — take what you can (15-30 damage)", 2),
            ("Leave", 0),
        };
        return SelectFromMenu(options.AsReadOnly(), _input, "⚔ [Contested Armory] — how do you approach?");
    }

    /// <summary>
    /// Presents the ability menu with available abilities as selectable options
    /// and unavailable abilities shown as informational lines above.
    /// Returns the selected Ability, or null if the player cancels.
    /// </summary>
    public Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities)
    {
        // Show unavailable abilities as info lines (not selectable)
        foreach (var (ability, onCooldown, cooldownTurns, notEnoughMana) in unavailableAbilities)
        {
            if (onCooldown)
                ShowColoredMessage($"  ○ {ability.Name} — Cooldown: {cooldownTurns} turns (Cost: {ability.ManaCost} MP)", Systems.ColorCodes.Gray);
            else if (notEnoughMana)
                ShowColoredMessage($"  ○ {ability.Name} — Need {ability.ManaCost} MP (Cost: {ability.ManaCost} MP)", Systems.ColorCodes.Red);
        }

        // Build selectable options from available abilities + Cancel
        var availList = availableAbilities.ToList();
        var options = availList
            .Select(a => ($"{a.Name} — {a.Description} (Cost: {a.ManaCost} MP)", (Ability?)a))
            .Append(("Cancel", (Ability?)null))
            .ToArray();

        return SelectFromMenu(options, _input, "=== Abilities ===");
    }

    /// <summary>
    /// Arrow-key navigable consumable item selection menu during combat.
    /// Returns the selected Item, or null if the player cancels.
    /// </summary>
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables)
    {
        var options = consumables
            .Select(item =>
            {
                var manaStr = item.ManaRestore > 0 ? $" +{item.ManaRestore} MP" : "";
                return ($"🧪 {item.Name} (+{item.HealAmount} HP{manaStr})", (Item?)item);
            })
            .Append(("↩  Cancel", (Item?)null))
            .ToArray();
        return SelectFromMenu(options, _input, "=== USE ITEM — Choose a consumable ===");
    }

    /// <inheritdoc />
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
    {
        var options = equippable
            .Select(item =>
            {
                var icon = ItemTypeIcon(item.Type);
                var stat = PrimaryStatLabel(item);
                return ($"{icon} {item.Name}  [{stat}]", (Item?)item);
            })
            .Append(("↩  Cancel", (Item?)null))
            .ToArray();
        return SelectFromMenu(options, _input, "=== EQUIP — Choose an item ===");
    }
}
