using System.Text;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Terminal.Gui;

namespace Dungnz.Display.Tui;

/// <summary>
/// Terminal.Gui implementation of <see cref="IDisplayService"/> that renders
/// game output to the TUI split-screen layout.
/// </summary>
public sealed class TerminalGuiDisplayService : IDisplayService
{
    private readonly TuiLayout _layout;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalGuiDisplayService"/> class.
    /// </summary>
    /// <param name="layout">The TUI layout to render to.</param>
    public TerminalGuiDisplayService(TuiLayout layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    /// <inheritdoc/>
    public void ShowTitle()
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            _layout.SetContent("═══════════════════════════════════════\n");
            _layout.AppendContent("        D  U  N  G  N  Z\n");
            _layout.AppendContent("═══════════════════════════════════════\n");
            _layout.AppendContent("      A dungeon awaits...\n\n");
        });
    }

    /// <inheritdoc/>
    public void ShowRoom(Room room)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine();

            // Room type prefix
            var prefix = room.Type switch
            {
                RoomType.Dark => "🌑 The room is pitch dark. ",
                RoomType.Scorched => "🔥 Scorch marks scar the stone. ",
                RoomType.Flooded => "💧 Ankle-deep water pools here. ",
                RoomType.Mossy => "🌿 Damp moss covers the walls. ",
                RoomType.Ancient => "🏛 Ancient runes line the walls. ",
                RoomType.ForgottenShrine => "✨ Holy light radiates from a forgotten shrine. ",
                RoomType.PetrifiedLibrary => "📚 Petrified bookshelves line these ancient walls. ",
                RoomType.ContestedArmory => "⚔ Weapon racks gleam dangerously in the dark. ",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(prefix))
                sb.AppendLine(prefix);

            sb.AppendLine(room.Description);

            // Environmental hazard
            var hazard = room.EnvironmentalHazard switch
            {
                RoomHazard.LavaSeam => "🔥 Lava seams crack the floor — each action will burn you.",
                RoomHazard.CorruptedGround => "💀 The ground pulses with dark energy — it will drain you with every action.",
                RoomHazard.BlessedClearing => "✨ A blessed warmth fills this clearing.",
                _ => null
            };
            if (hazard != null) sb.AppendLine(hazard);

            // Exits
            if (room.Exits.Count > 0)
            {
                var exitSymbols = new Dictionary<Direction, string>
                {
                    [Direction.North] = "↑ North",
                    [Direction.South] = "↓ South",
                    [Direction.East] = "→ East",
                    [Direction.West] = "← West"
                };
                var ordered = new[] { Direction.North, Direction.South, Direction.East, Direction.West }
                    .Where(d => room.Exits.ContainsKey(d))
                    .Select(d => exitSymbols[d]);
                sb.AppendLine($"Exits: {string.Join("   ", ordered)}");
            }

            // Enemies
            if (room.Enemy != null)
                sb.AppendLine($"⚔ {room.Enemy.Name} is here!");

            // Items
            if (room.Items.Count > 0)
            {
                sb.AppendLine("Items on the ground:");
                foreach (var item in room.Items)
                    sb.AppendLine($"  ◆ {item.Name} ({GetPrimaryStatLabel(item)})");
            }

            // Special rooms
            if (room.HasShrine && room.Type != RoomType.ForgottenShrine)
                sb.AppendLine("✨ A shrine glimmers here. (USE SHRINE)");
            if (room.Type == RoomType.ForgottenShrine && !room.SpecialRoomUsed)
                sb.AppendLine("✨ A forgotten shrine stands here, radiating holy energy. (USE SHRINE)");
            if (room.Type == RoomType.PetrifiedLibrary && !room.SpecialRoomUsed)
                sb.AppendLine("📖 Ancient tomes line the walls. Something catches the light as you enter...");
            if (room.Type == RoomType.ContestedArmory && !room.SpecialRoomUsed)
                sb.AppendLine("⚠ Trapped weapons gleam in the dark. (USE ARMORY to approach)");
            if (room.Merchant != null)
                sb.AppendLine("🛒 A merchant awaits. (SHOP)");

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowCombat(string message)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            _layout.AppendContent($"\n═══ {message} ═══\n");
            _layout.AppendLog(message, "combat");
        });
    }

    /// <inheritdoc/>
    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine();

            // Player status
            sb.AppendLine($"⚔  {player.Name}");
            sb.AppendLine($"HP: {BuildHpBar(player.HP, player.MaxHP)} {player.HP}/{player.MaxHP}");
            if (player.MaxMana > 0)
                sb.AppendLine($"MP: {BuildMpBar(player.Mana, player.MaxMana)} {player.Mana}/{player.MaxMana}");
            if (playerEffects.Count > 0)
            {
                var effects = string.Join(" ", playerEffects.Select(e => $"[{e.Effect} {e.RemainingTurns}t]"));
                sb.AppendLine($"Effects: {effects}");
            }

            sb.AppendLine();

            // Enemy status
            sb.AppendLine($"🐉 {enemy.Name}");
            sb.AppendLine($"HP: {BuildHpBar(enemy.HP, enemy.MaxHP)} {enemy.HP}/{enemy.MaxHP}");
            if (enemyEffects.Count > 0)
            {
                var effects = string.Join(" ", enemyEffects.Select(e => $"[{e.Effect} {e.RemainingTurns}t]"));
                sb.AppendLine($"Effects: {effects}");
            }

            _layout.AppendContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowCombatMessage(string message)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var cleaned = StripAnsiCodes(message);
            _layout.AppendContent($"  {cleaned}\n");
            _layout.AppendLog(cleaned, "combat");
        });
    }

    /// <inheritdoc/>
    public void ShowPlayerStats(Player player)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"⚔ {player.Name}");
            sb.AppendLine($"Class: {player.Class}");
            var xpToNext = 100 * player.Level;
            sb.AppendLine($"Level: {player.Level}");
            sb.AppendLine($"XP: {player.XP}/{xpToNext}");
            sb.AppendLine();
            
            // Colored HP bar
            var hpBar = BuildColoredHpBar(player.HP, player.MaxHP);
            sb.AppendLine($"HP: {hpBar}");
            sb.AppendLine($"    {player.HP}/{player.MaxHP}");
            
            // Colored MP bar
            if (player.MaxMana > 0)
            {
                var mpBar = BuildColoredMpBar(player.Mana, player.MaxMana);
                sb.AppendLine($"MP: {mpBar}");
                sb.AppendLine($"    {player.Mana}/{player.MaxMana}");
            }
            
            sb.AppendLine();
            sb.AppendLine($"ATK: {player.Attack}");
            sb.AppendLine($"DEF: {player.Defense}");
            sb.AppendLine($"Gold: {player.Gold}g");
            sb.AppendLine();

            // Equipment summary
            sb.AppendLine("Equipment:");
            if (player.EquippedWeapon != null)
                sb.AppendLine($"  ⚔ {player.EquippedWeapon.Name}");
            if (player.EquippedChest != null)
                sb.AppendLine($"  🛡 {player.EquippedChest.Name}");
            if (player.EquippedHead != null)
                sb.AppendLine($"  🪖 {player.EquippedHead.Name}");
            if (player.EquippedHands != null)
                sb.AppendLine($"  🧤 {player.EquippedHands.Name}");
            if (player.EquippedFeet != null)
                sb.AppendLine($"  👢 {player.EquippedFeet.Name}");
            if (player.EquippedAccessory != null)
                sb.AppendLine($"  💍 {player.EquippedAccessory.Name}");

            _layout.SetStats(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowInventory(Player player)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Inventory ({player.Inventory.Count}/{Player.MaxInventorySize} slots)");
            sb.AppendLine();

            if (player.Inventory.Count == 0)
            {
                sb.AppendLine("  (empty)");
            }
            else
            {
                foreach (var item in player.Inventory)
                    sb.AppendLine($"  {GetItemIcon(item)} {item.Name} ({GetPrimaryStatLabel(item)})");
            }

            _layout.AppendContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public Item? ShowInventoryAndSelect(Player player)
    {
        if (player.Inventory.Count == 0)
            return null;

        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = player.Inventory.Select((Item i) => (i.Name, (Item?)i)).ToList();
            var dialog = new TuiMenuDialog<Item?>("Inspect an item", options, null);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public void ShowLootDrop(Item item, Player player, bool isElite = false)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var header = isElite ? "✦ ELITE LOOT DROP" : "✦ LOOT DROP";
            var stat = GetPrimaryStatLabel(item);

            var sb = new StringBuilder();
            sb.AppendLine(header);
            sb.AppendLine($"{item.Tier}");
            sb.AppendLine($"{GetItemIcon(item)} {item.Name}");
            sb.AppendLine($"{stat}  {item.Weight} wt");

            // Upgrade hint
            if (item.AttackBonus > 0 && player.EquippedWeapon != null)
            {
                int delta = item.AttackBonus - player.EquippedWeapon.AttackBonus;
                if (delta > 0) sb.AppendLine($"(+{delta} vs equipped!)");
            }
            else if (item.DefenseBonus > 0 && player.EquippedChest != null)
            {
                int delta = item.DefenseBonus - player.EquippedChest.DefenseBonus;
                if (delta > 0) sb.AppendLine($"(+{delta} vs equipped!)");
            }

            _layout.AppendContent(sb.ToString() + "\n");
            _layout.AppendLog($"Loot: {item.Name}", "loot");
        });
    }

    /// <inheritdoc/>
    public void ShowGoldPickup(int amount, int newTotal)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var message = $"💰 +{amount} gold  (Total: {newTotal}g)";
            _layout.AppendContent($"  {message}\n");
            _layout.AppendLog(message, "loot");
        });
    }

    /// <inheritdoc/>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var message = $"{GetItemIcon(item)} Picked up: {item.Name}  ({GetPrimaryStatLabel(item)})";
            _layout.AppendContent($"  {message}\n");
            _layout.AppendContent($"  Slots: {slotsCurrent}/{slotsMax}  ·  Weight: {weightCurrent}/{weightMax}\n");
            _layout.AppendLog(message, "loot");

            if (weightCurrent > weightMax * 0.8)
                _layout.AppendContent("  ⚠ Inventory nearly full!\n");
        });
    }

    /// <inheritdoc/>
    public void ShowItemDetail(Item item)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetItemIcon(item)} {item.Name}");
            sb.AppendLine();
            sb.AppendLine($"Type:    {item.Type}");
            sb.AppendLine($"Tier:    {item.Tier}");
            sb.AppendLine($"Weight:  {item.Weight}");
            if (item.AttackBonus != 0) sb.AppendLine($"Attack:  +{item.AttackBonus}");
            if (item.DefenseBonus != 0) sb.AppendLine($"Defense: +{item.DefenseBonus}");
            if (item.HealAmount != 0) sb.AppendLine($"Heal:    +{item.HealAmount} HP");
            if (item.ManaRestore != 0) sb.AppendLine($"Mana:    +{item.ManaRestore}");
            if (item.MaxManaBonus != 0) sb.AppendLine($"Max Mana: +{item.MaxManaBonus}");
            if (item.DodgeBonus > 0) sb.AppendLine($"Dodge:   +{item.DodgeBonus:P0}");
            if (item.CritChance > 0) sb.AppendLine($"Crit:    +{item.CritChance:P0}");
            if (item.HPOnHit != 0) sb.AppendLine($"HP on Hit: +{item.HPOnHit}");
            if (item.AppliesBleedOnHit) sb.AppendLine("Special: Bleed on hit");
            if (item.PoisonImmunity) sb.AppendLine("Special: Poison immune");
            if (!string.IsNullOrEmpty(item.Description))
            {
                sb.AppendLine();
                sb.AppendLine(item.Description);
            }

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowMessage(string message)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var cleaned = StripAnsiCodes(message);
            _layout.AppendContent(cleaned + "\n");
            _layout.AppendLog(cleaned, "info");
        });
    }

    /// <inheritdoc/>
    public void ShowError(string message)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var cleaned = StripAnsiCodes(message);
            _layout.AppendContent($"❌ {cleaned}\n");
            _layout.AppendLog(cleaned, "error");
        });
    }

    /// <inheritdoc/>
    public void ShowHelp()
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("             COMMANDS");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("MOVEMENT:");
            sb.AppendLine("  GO <direction> / NORTH / SOUTH / EAST / WEST");
            sb.AppendLine("  MAP — Show dungeon map");
            sb.AppendLine();
            sb.AppendLine("COMBAT:");
            sb.AppendLine("  ATTACK — Attack enemy");
            sb.AppendLine("  FLEE — Attempt to flee combat");
            sb.AppendLine();
            sb.AppendLine("INVENTORY:");
            sb.AppendLine("  INVENTORY / INV — Show inventory");
            sb.AppendLine("  TAKE <item> / TAKE ALL — Pick up items");
            sb.AppendLine("  DROP <item> — Drop item");
            sb.AppendLine("  EQUIP <item> — Equip item");
            sb.AppendLine("  USE <item> — Use consumable");
            sb.AppendLine("  EXAMINE <item> — View item details");
            sb.AppendLine();
            sb.AppendLine("INTERACTION:");
            sb.AppendLine("  SHOP — Browse merchant");
            sb.AppendLine("  USE SHRINE — Interact with shrine");
            sb.AppendLine("  DESCEND — Go to next floor");
            sb.AppendLine();
            sb.AppendLine("OTHER:");
            sb.AppendLine("  STATS — Show player stats");
            sb.AppendLine("  SKILLS — View skill tree");
            sb.AppendLine("  SAVE — Save game");
            sb.AppendLine("  HELP — Show this help");
            sb.AppendLine("  QUIT — Exit game");
            sb.AppendLine("═══════════════════════════════════════");

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowCommandPrompt(Player? player = null)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            _layout.CommandInput.SetFocus();
        });
    }

    /// <inheritdoc/>
    public void ShowMap(Room currentRoom, int floor = 1)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var map = BuildAsciiMap(currentRoom);
            _layout.SetMap($"Floor {floor}\n\n{map}");
        });
    }

    /// <inheritdoc/>
    public string ReadPlayerName()
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var dialog = new Dialog("Enter your name")
            {
                Width = 50,
                Height = 8
            };

            var nameField = new TextField("")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 1
            };

            var okButton = new Button("OK")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(nameField) + 1
            };
            okButton.Clicked += () => Application.RequestStop();

            dialog.Add(new Label("Enter your character's name:") { X = 1, Y = 0 });
            dialog.Add(nameField);
            dialog.Add(okButton);

            nameField.SetFocus();
            Application.Run(dialog);

            return string.IsNullOrWhiteSpace(nameField.Text.ToString()) ? "Hero" : nameField.Text.ToString()!;
        });
    }

    /// <inheritdoc/>
    public void ShowColoredMessage(string message, string color)
    {
        // Terminal.Gui doesn't support inline ANSI colors in TextView
        // Just show the message without color
        ShowMessage(message);
    }

    /// <inheritdoc/>
    public void ShowColoredCombatMessage(string message, string color)
    {
        ShowCombatMessage(message);
    }

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string valueColor)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            _layout.AppendContent($"{label} {value}\n");
        });
    }

    /// <inheritdoc/>
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ Equipment Comparison ═══");
            sb.AppendLine();

            if (oldItem != null)
            {
                sb.AppendLine($"Currently Equipped: {oldItem.Name}");
                sb.AppendLine($"  ATK: {oldItem.AttackBonus}  DEF: {oldItem.DefenseBonus}");
            }
            else
            {
                sb.AppendLine("Currently Equipped: (none)");
            }

            sb.AppendLine();
            sb.AppendLine($"New Item: {newItem.Name}");
            sb.AppendLine($"  ATK: {newItem.AttackBonus}  DEF: {newItem.DefenseBonus}");

            if (oldItem != null)
            {
                int atkDelta = newItem.AttackBonus - oldItem.AttackBonus;
                int defDelta = newItem.DefenseBonus - oldItem.DefenseBonus;

                sb.AppendLine();
                sb.AppendLine("Change:");
                if (atkDelta > 0) sb.AppendLine($"  ATK: +{atkDelta}");
                else if (atkDelta < 0) sb.AppendLine($"  ATK: {atkDelta}");
                if (defDelta > 0) sb.AppendLine($"  DEF: +{defDelta}");
                else if (defDelta < 0) sb.AppendLine($"  DEF: {defDelta}");
            }

            _layout.AppendContent(sb.ToString() + "\n");
        });
    }

    /// <inheritdoc/>
    public void ShowEquipment(Player player)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("            EQUIPMENT");
            sb.AppendLine("═══════════════════════════════════════");

            sb.AppendLine($"Weapon:    {player.EquippedWeapon?.Name ?? "(none)"}");
            sb.AppendLine($"Chest:     {player.EquippedChest?.Name ?? "(none)"}");
            sb.AppendLine($"Head:      {player.EquippedHead?.Name ?? "(none)"}");
            sb.AppendLine($"Hands:     {player.EquippedHands?.Name ?? "(none)"}");
            sb.AppendLine($"Feet:      {player.EquippedFeet?.Name ?? "(none)"}");
            sb.AppendLine($"Accessory: {player.EquippedAccessory?.Name ?? "(none)"}");

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowEnhancedTitle()
    {
        ShowTitle();
    }

    /// <inheritdoc/>
    public bool ShowIntroNarrative()
    {
        GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var dialog = new Dialog("Lore")
            {
                Width = Dim.Percent(80),
                Height = Dim.Percent(50)
            };

            var lore = "The ancient fortress of Dungnz has stood for a thousand years — a labyrinthine\n"
                     + "tomb carved into the mountain's heart by hands long since turned to dust. Adventurers\n"
                     + "who descend its spiral corridors speak of riches beyond imagination and horrors beyond\n"
                     + "comprehension. The air below reeks of sulfur and old blood. Torches flicker without wind.\n"
                     + "Something vast and patient watches from the deep.\n\n"
                     + "Press OK to begin your descent...";

            var textView = new TextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 3,
                ReadOnly = true,
                Text = lore
            };

            var okButton = new Button("OK")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(textView) + 1
            };
            okButton.Clicked += () => Application.RequestStop();

            dialog.Add(textView);
            dialog.Add(okButton);

            Application.Run(dialog);
        });

        return false;
    }

    /// <inheritdoc/>
    public void ShowPrestigeInfo(PrestigeData prestige)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("            PRESTIGE");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"⭐ Level: {prestige.PrestigeLevel}");
            sb.AppendLine($"Wins:     {prestige.TotalWins}");
            sb.AppendLine($"Runs:     {prestige.TotalRuns}");
            if (prestige.BonusStartAttack > 0) sb.AppendLine($"Bonus Attack:  +{prestige.BonusStartAttack}");
            if (prestige.BonusStartDefense > 0) sb.AppendLine($"Bonus Defense: +{prestige.BonusStartDefense}");
            if (prestige.BonusStartHP > 0) sb.AppendLine($"Bonus HP:      +{prestige.BonusStartHP}");

            _layout.AppendContent(sb.ToString() + "\n");
        });
    }

    /// <inheritdoc/>
    public Difficulty SelectDifficulty()
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new[]
            {
                ("CASUAL — Weaker enemies · Cheap shops · Start with 50g + 3 potions", Difficulty.Casual),
                ("NORMAL — Balanced challenge · The intended experience · Start with 15g + 1 potion", Difficulty.Normal),
                ("HARD — Stronger enemies · Scarce rewards · No starting supplies · ☠ Permadeath", Difficulty.Hard)
            };

            var dialog = new TuiMenuDialog<Difficulty>("Choose your difficulty", options, Difficulty.Normal);
            var result = dialog.ShowAndGetResult();
            return result;
        });
    }

    /// <inheritdoc/>
    public PlayerClassDefinition SelectClass(PrestigeData? prestige)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = PlayerClassDefinition.All.Select(def =>
            {
                var presBonus = prestige != null && prestige.PrestigeLevel > 0
                    ? $" (+{prestige.BonusStartHP} HP, +{prestige.BonusStartAttack} ATK, +{prestige.BonusStartDefense} DEF prestige)"
                    : "";
                var label = $"{GetClassIcon(def)} {def.Name} — {def.Description}{presBonus}";
                return (label, def);
            });

            var dialog = new TuiMenuDialog<PlayerClassDefinition>("Choose your class", options);
            return dialog.ShowAndGetResult() ?? PlayerClassDefinition.All[0];
        });
    }

    /// <inheritdoc/>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🏪 Merchant  Your gold: {playerGold}g");
            sb.AppendLine();

            int idx = 1;
            foreach (var (item, price) in stock)
            {
                var affordable = price <= playerGold ? "✓" : "✗";
                sb.AppendLine($"{idx}. {affordable} {item.Name} — {item.Tier} — {GetPrimaryStatLabel(item)} — {price}g");
                idx++;
            }

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var stockList = stock.ToList();
            var options = stockList.Select((s, idx) =>
            {
                var affordable = s.price <= playerGold ? "✓" : "✗";
                var label = $"{affordable} {s.item.Name} — {s.item.Tier} — {GetPrimaryStatLabel(s.item)} — {s.price}g";
                return (label, idx + 1);
            });

            var dialog = new TuiMenuDialog<int>($"🏪 Merchant (Gold: {playerGold}g)", options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"💰 Sell Items  Your gold: {playerGold}g");
            sb.AppendLine();

            int idx = 1;
            foreach (var (item, sellPrice) in items)
            {
                sb.AppendLine($"{idx}. {item.Name} — {sellPrice}g");
                idx++;
            }

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var itemsList = items.ToList();
            var options = itemsList.Select((s, idx) =>
            {
                var label = $"{s.item.Name} — {s.sellPrice}g";
                return (label, idx + 1);
            });

            var dialog = new TuiMenuDialog<int>($"💰 Sell Items (Gold: {playerGold}g)", options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"═══ {recipeName} ═══");
            sb.AppendLine();
            sb.AppendLine($"Result: {result.Name}");
            sb.AppendLine($"  {GetPrimaryStatLabel(result)}");
            sb.AppendLine();
            sb.AppendLine("Ingredients:");
            foreach (var (ingredient, playerHasIt) in ingredients)
            {
                var check = playerHasIt ? "✅" : "❌";
                sb.AppendLine($"  {check} {ingredient}");
            }

            _layout.AppendContent(sb.ToString() + "\n");
        });
    }

    /// <inheritdoc/>
    public void ShowCombatStart(Enemy enemy)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            _layout.SetContent($"\n═══ COMBAT: {enemy.Name} ═══\n\n");
            _layout.AppendLog($"Combat started: {enemy.Name}");
        });
    }

    /// <inheritdoc/>
    public void ShowCombatEntryFlags(Enemy enemy)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            if (enemy.IsElite)
                _layout.AppendContent("⭐ ELITE ENEMY\n");
        });
    }

    /// <inheritdoc/>
    public void ShowLevelUpChoice(Player player)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🎉 LEVEL UP! You are now level {player.Level}");
            sb.AppendLine();
            sb.AppendLine("Choose a stat to increase:");
            sb.AppendLine("1. +5 Max HP");
            sb.AppendLine("2. +2 Attack");
            sb.AppendLine("3. +2 Defense");

            _layout.AppendContent(sb.ToString() + "\n");
        });
    }

    /// <inheritdoc/>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"         FLOOR {floor} / {maxFloor}");
            sb.AppendLine($"         {variant}");
            sb.AppendLine("═══════════════════════════════════════");

            _layout.SetContent(sb.ToString() + "\n");
            _layout.AppendLog($"Entered Floor {floor}");
        });
    }

    /// <inheritdoc/>
    public void ShowEnemyDetail(Enemy enemy)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"═══ {enemy.Name} ═══");
            sb.AppendLine();
            sb.AppendLine($"HP: {enemy.HP}/{enemy.MaxHP}");
            sb.AppendLine($"ATK: {enemy.Attack}  DEF: {enemy.Defense}");
            if (enemy.IsElite) sb.AppendLine("⭐ ELITE");

            _layout.AppendContent(sb.ToString() + "\n");
        });
    }

    /// <inheritdoc/>
    public void ShowVictory(Player player, int floorsCleared, RunStats stats)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("           🎉 VICTORY! 🎉");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Hero: {player.Name}");
            sb.AppendLine($"Class: {player.Class}");
            sb.AppendLine($"Final Level: {player.Level}");
            sb.AppendLine($"Floors Cleared: {floorsCleared}");
            sb.AppendLine();
            sb.AppendLine("Run Statistics:");
            sb.AppendLine($"  Enemies Defeated: {stats.EnemiesDefeated}");
            sb.AppendLine($"  Items Found:      {stats.ItemsFound}");
            sb.AppendLine($"  Gold Collected:   {stats.GoldCollected}");
            sb.AppendLine();
            sb.AppendLine("You have conquered the dungeon!");

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowGameOver(Player player, string? killedBy, RunStats stats)
    {
        GameThreadBridge.InvokeOnUiThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("           ☠ GAME OVER ☠");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Hero: {player.Name}");
            sb.AppendLine($"Class: {player.Class}");
            sb.AppendLine($"Final Level: {player.Level}");
            if (!string.IsNullOrEmpty(killedBy))
                sb.AppendLine($"Killed by: {killedBy}");
            sb.AppendLine();
            sb.AppendLine("Run Statistics:");
            sb.AppendLine($"  Enemies Defeated: {stats.EnemiesDefeated}");
            sb.AppendLine($"  Items Found:      {stats.ItemsFound}");
            sb.AppendLine($"  Gold Collected:   {stats.GoldCollected}");

            _layout.SetContent(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt.Length > 0)
        {
            GameThreadBridge.InvokeOnUiThread(() =>
            {
                _layout.AppendContent($"\n{string.Join("\n", enemy.AsciiArt)}\n\n");
            });
        }
    }

    /// <inheritdoc/>
    public int ShowLevelUpChoiceAndSelect(Player player)
    {
        ShowLevelUpChoice(player);

        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new[]
            {
                ("+5 Max HP", 1),
                ("+2 Attack", 2),
                ("+2 Defense", 3)
            };

            var dialog = new TuiMenuDialog<int>("Level Up!", options, 1);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new[]
            {
                ("⚔ Attack", "A"),
                ("✨ Ability", "B"),
                ("🏃 Flee", "F")
            };

            var dialog = new TuiMenuDialog<string>("Combat Action", options, "A");
            return dialog.ShowAndGetResult() ?? "A";
        });
    }

    /// <inheritdoc/>
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var recipesList = recipes.ToList();
            var options = recipesList.Select((r, idx) =>
            {
                var check = r.canCraft ? "✓" : "✗";
                var label = $"{check} {r.recipeName}";
                return (label, idx + 1);
            });

            var dialog = new TuiMenuDialog<int>("Crafting", options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new[]
            {
                ($"💚 Heal ({healCost}g)", 1),
                ($"✨ Blessing ({blessCost}g)", 2),
                ($"🛡 Fortify ({fortifyCost}g)", 3),
                ($"🧘 Meditate ({meditateCost}g)", 4),
                ("← Leave", 0)
            };

            var dialog = new TuiMenuDialog<int>($"✨ Shrine (Gold: {playerGold}g)", options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var stockList = stock.ToList();
            var options = new List<(string Label, int Value)>();

            for (int i = 0; i < stockList.Count; i++)
            {
                var (item, price) = stockList[i];
                var affordable = price <= playerGold ? "✓" : "✗";
                var label = $"{affordable} {item.Name} — {item.Tier} — {GetPrimaryStatLabel(item)} — {price}g";
                options.Add((label, i + 1));
            }

            options.Add(("💰 Sell Items", -1));
            options.Add(("← Leave", 0));

            var dialog = new TuiMenuDialog<int>($"🏪 Merchant (Gold: {playerGold}g)", options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public bool ShowConfirmMenu(string prompt)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            return TuiMenuDialog.ShowConfirm(prompt);
        });
    }

    /// <inheritdoc/>
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new[]
            {
                (option1, 1),
                (option2, 2),
                ("← Leave", 0)
            };

            var dialog = new TuiMenuDialog<int>(header, options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public int ShowForgottenShrineMenuAndSelect()
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new[]
            {
                ("💚 Prayer of Restoration (Full heal + cure ailments)", 1),
                ("⚔ Prayer of Might (Permanent +3 ATK)", 2),
                ("🛡 Prayer of Resilience (Permanent +3 DEF)", 3),
                ("← Leave", 0)
            };

            var dialog = new TuiMenuDialog<int>("✨ Forgotten Shrine", options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public int ShowContestedArmoryMenuAndSelect(int playerDefense)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new[]
            {
                ($"⚡ Rush in (Take {Math.Max(10 - playerDefense, 1)} damage, get rare weapon)", 1),
                ("🛡 Careful approach (Take 3 damage, get common weapon)", 2),
                ("← Leave", 0)
            };

            var dialog = new TuiMenuDialog<int>("⚔ Contested Armory", options, 0);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new List<(string Label, Ability? Value)>();

            // Add available abilities
            foreach (var ability in availableAbilities)
            {
                var label = $"✓ {ability.Name} (MP: {ability.ManaCost}) — {ability.Description}";
                options.Add((label, ability));
            }

            // Add unavailable abilities as info (cannot be selected)
            foreach (var (ability, onCooldown, cooldownTurns, notEnoughMana) in unavailableAbilities)
            {
                var reason = onCooldown ? $"Cooldown: {cooldownTurns}t" : "Not enough mana";
                var label = $"✗ {ability.Name} ({reason})";
                // We can't make these unselectable in the simple dialog, so just mark them
                options.Add((label, null)); // Will return null if selected
            }

            options.Add(("← Cancel", null));

            var dialog = new TuiMenuDialog<Ability?>("✨ Abilities", options, null);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables)
    {
        if (consumables.Count == 0)
            return null;

        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = consumables.Select(i => (i.Name, (Item?)i));
            var dialog = new TuiMenuDialog<Item?>("Use Item", options, null);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
    {
        if (equippable.Count == 0)
            return null;

        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = equippable.Select(i => ($"{i.Name} ({GetPrimaryStatLabel(i)})", (Item?)i));
            var dialog = new TuiMenuDialog<Item?>("Equip Item", options, null);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable)
    {
        if (usable.Count == 0)
            return null;

        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = usable.Select(i => ($"{i.Name} ({GetPrimaryStatLabel(i)})", (Item?)i));
            var dialog = new TuiMenuDialog<Item?>("Use Item", options, null);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public TakeSelection? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems)
    {
        if (roomItems.Count == 0)
            return null;

        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new List<(string Label, TakeSelection? Value)>
            {
                ("Take All", new TakeSelection.All())
            };

            foreach (var item in roomItems)
            {
                options.Add(($"{item.Name} ({GetPrimaryStatLabel(item)})", new TakeSelection.Single(item)));
            }

            options.Add(("← Cancel", null));

            var dialog = new TuiMenuDialog<TakeSelection?>("Take Items", options, null);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public StartupMenuOption ShowStartupMenu(bool hasSaves)
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = new List<(string Label, StartupMenuOption Value)>
            {
                ("🗡  New Game", StartupMenuOption.NewGame)
            };

            if (hasSaves)
                options.Add(("📂 Load Save", StartupMenuOption.LoadSave));

            options.Add(("🌱 New Game with Seed", StartupMenuOption.NewGameWithSeed));
            options.Add(("🚪 Exit", StartupMenuOption.Exit));

            var dialog = new TuiMenuDialog<StartupMenuOption>("DUNGNZ", options, StartupMenuOption.Exit);
            var result = dialog.ShowAndGetResult();
            return result;
        });
    }

    /// <inheritdoc/>
    public string? SelectSaveToLoad(string[] saveNames)
    {
        if (saveNames.Length == 0)
            return null;

        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            var options = saveNames.Select(s => (s, (string?)s));
            var dialog = new TuiMenuDialog<string?>("Load Save", options, null);
            return dialog.ShowAndGetResult();
        });
    }

    /// <inheritdoc/>
    public int? ReadSeed()
    {
        return GameThreadBridge.InvokeOnUiThreadAndWait(() =>
        {
            int? result = null;
            
            var dialog = new Dialog("Enter Seed")
            {
                Width = 50,
                Height = 10
            };

            var seedField = new TextField("")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 1
            };

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 5,
                Y = Pos.Bottom(seedField) + 1
            };
            okButton.Clicked += () =>
            {
                var text = seedField.Text.ToString();
                if (!string.IsNullOrWhiteSpace(text) &&
                    int.TryParse(text, out int seed) &&
                    seed >= 100000 && seed <= 999999)
                {
                    result = seed;
                }
                Application.RequestStop();
            };

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 5,
                Y = Pos.Bottom(seedField) + 1
            };
            cancelButton.Clicked += () =>
            {
                Application.RequestStop();
            };

            dialog.Add(new Label("Enter a 6-digit seed (100000-999999):") { X = 1, Y = 0 });
            dialog.Add(seedField);
            dialog.Add(okButton);
            dialog.Add(cancelButton);

            seedField.SetFocus();
            Application.Run(dialog);

            return result;
        });
    }

    /// <inheritdoc/>
    public Skill? ShowSkillTreeMenu(Player player)
    {
        // For the TUI implementation, we'll use a simplified skill selection
        // The full skill tree would require more complex UI components
        // For now, return null (no skill selected)
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Helper methods
    // ═══════════════════════════════════════════════════════════════

    private static string BuildHpBar(int current, int max)
    {
        if (max == 0) return "[        ]";
        int filled = (int)((double)current / max * 8);
        filled = Math.Max(0, Math.Min(8, filled));
        return $"[{"█".PadRight(filled, '█').PadRight(8, '░')}]";
    }

    private static string BuildColoredHpBar(int current, int max)
    {
        if (max == 0) return "[░░░░░░░░]";
        int filled = (int)((double)current / max * 8);
        filled = Math.Max(0, Math.Min(8, filled));
        
        // Use visual indicators based on health percentage
        var percent = (double)current / max;
        var barChar = percent switch
        {
            > 0.50 => "█", // Green zone
            > 0.25 => "▓", // Yellow zone
            _ => "▒"       // Red zone
        };
        
        var bar = new string('█', filled).PadRight(8, '░');
        return $"[{bar}]";
    }

    private static string BuildMpBar(int current, int max)
    {
        if (max == 0) return "[        ]";
        int filled = (int)((double)current / max * 8);
        filled = Math.Max(0, Math.Min(8, filled));
        return $"[{"█".PadRight(filled, '█').PadRight(8, '░')}]";
    }

    private static string BuildColoredMpBar(int current, int max)
    {
        if (max == 0) return "[░░░░░░░░]";
        int filled = (int)((double)current / max * 8);
        filled = Math.Max(0, Math.Min(8, filled));
        
        var bar = new string('█', filled).PadRight(8, '░');
        return $"[{bar}]";
    }

    private static string StripAnsiCodes(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, @"\x1b\[[0-9;]*m", string.Empty);
    }

    private static string GetPrimaryStatLabel(Item item)
    {
        if (item.AttackBonus > 0) return $"+{item.AttackBonus} ATK";
        if (item.DefenseBonus > 0) return $"+{item.DefenseBonus} DEF";
        if (item.HealAmount > 0) return $"+{item.HealAmount} HP";
        if (item.ManaRestore > 0) return $"+{item.ManaRestore} MP";
        if (item.MaxManaBonus > 0) return $"+{item.MaxManaBonus} Max MP";
        return item.Type.ToString();
    }

    private static string GetItemIcon(Item item) => item.Type switch
    {
        ItemType.Weapon => "⚔",
        ItemType.Armor => "🛡",
        ItemType.Accessory => "💍",
        ItemType.Consumable => "🧪",
        ItemType.CraftingMaterial => "📦",
        _ => "◆"
    };

    private static string GetClassIcon(PlayerClassDefinition def) => def.Name switch
    {
        "Warrior" => "⚔",
        "Mage" => "🔮",
        "Rogue" => "🗡",
        "Paladin" => "✨",
        _ => "🛡"
    };

    private static string BuildAsciiMap(Room currentRoom)
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
                    Direction.North => (rx, ry - 1),
                    Direction.South => (rx, ry + 1),
                    Direction.East => (rx + 1, ry),
                    Direction.West => (rx - 1, ry),
                    _ => (rx, ry)
                };
                positions[neighbour] = (nx, ny);
                queue.Enqueue(neighbour);
            }
        }

        // Rooms the player has seen: visited, current, or adjacent to a visited/current room
        var knownSet = new HashSet<Room>(positions.Keys.Where(r => r.Visited || r == currentRoom));
        foreach (var known in knownSet.ToList())
        {
            foreach (var (_, neighbour) in known.Exits)
            {
                if (positions.ContainsKey(neighbour))
                    knownSet.Add(neighbour);
            }
        }

        var visiblePositions = positions.Where(kv => knownSet.Contains(kv.Key)).ToList();

        if (visiblePositions.Count == 0)
        {
            return "No map data available.";
        }

        int minX = visiblePositions.Min(kv => kv.Value.x);
        int maxX = visiblePositions.Max(kv => kv.Value.x);
        int minY = visiblePositions.Min(kv => kv.Value.y);
        int maxY = visiblePositions.Max(kv => kv.Value.y);

        var grid = new Dictionary<(int x, int y), Room>();
        foreach (var (room, pos) in visiblePositions)
            grid[pos] = room;

        var sb = new StringBuilder();
        sb.AppendLine("    N");
        sb.AppendLine("  W ✦ E");
        sb.AppendLine("    S");
        sb.AppendLine();

        for (int y = minY; y <= maxY; y++)
        {
            sb.Append(" ");
            for (int x = minX; x <= maxX; x++)
            {
                if (!grid.TryGetValue((x, y), out var r))
                {
                    sb.Append(x < maxX ? "    " : "   ");
                    continue;
                }

                string symbol = GetMapRoomSymbol(r, currentRoom);
                sb.Append(symbol);

                if (x < maxX)
                {
                    bool hasConnector = r.Exits.ContainsKey(Direction.East) && grid.ContainsKey((x + 1, y));
                    sb.Append(hasConnector ? "─" : " ");
                }
            }
            sb.AppendLine();

            if (y < maxY)
            {
                sb.Append(" ");
                for (int x = minX; x <= maxX; x++)
                {
                    bool hasSouth = grid.TryGetValue((x, y), out var rS)
                        && rS.Exits.ContainsKey(Direction.South)
                        && grid.ContainsKey((x, y + 1));
                    sb.Append(hasSouth ? " │ " : "   ");
                    if (x < maxX) sb.Append(" ");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("Legend:");
        sb.AppendLine("[@] You    [?] Unknown");
        sb.AppendLine("[E] Exit   [!] Enemy");
        sb.AppendLine("[S] Shrine [M] Merchant");
        sb.AppendLine("[+] Cleared");

        return sb.ToString();
    }

    private static string GetMapRoomSymbol(Room r, Room currentRoom)
    {
        if (r == currentRoom) return "[@]";
        if (!r.Visited) return "[?]";
        if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0) return "[B]";
        if (r.IsExit) return "[E]";
        if (r.Enemy != null && r.Enemy.HP > 0) return "[!]";
        if (r.HasShrine && !r.ShrineUsed) return "[S]";
        if (r.Merchant != null) return "[M]";
        if (r.Type == RoomType.TrapRoom && !r.SpecialRoomUsed) return "[T]";
        if (r.Type == RoomType.ContestedArmory) return "[A]";
        if (r.Type == RoomType.PetrifiedLibrary) return "[L]";
        if (r.Type == RoomType.ForgottenShrine) return "[F]";
        if (r.EnvironmentalHazard == RoomHazard.BlessedClearing) return "[*]";
        if (r.EnvironmentalHazard != RoomHazard.None) return "[~]";
        if (r.Type == RoomType.Dark) return "[D]";
        return "[+]";
    }
}
