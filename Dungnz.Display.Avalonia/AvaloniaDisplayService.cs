using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Threading;
using Dungnz.Display.Avalonia.ViewModels;
using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Display.Avalonia;

/// <summary>
/// Avalonia implementation of <see cref="IDisplayService"/> (both <see cref="IGameDisplay"/> and <see cref="IGameInput"/>).
/// Phase 3: All IGameDisplay output methods implemented with Dispatcher.UIThread.InvokeAsync pattern.
/// Phase 5-8: IGameInput methods will be implemented with async prompt dialogs.
/// </summary>
public class AvaloniaDisplayService : IDisplayService
{
    private readonly MainWindowViewModel _vm;
    
    // Cached state (same pattern as SpectreLayoutDisplayService)
    private Player? _cachedPlayer;
    private Room? _cachedRoom;
    private int _currentFloor = 1;
    private Enemy? _cachedCombatEnemy;
    private IReadOnlyList<ActiveEffect> _cachedEnemyEffects = Array.Empty<ActiveEffect>();
    private IReadOnlyList<(string name, int turnsRemaining)> _cachedCooldowns = [];
    private bool _lowHpWarningIssued;

    public AvaloniaDisplayService(MainWindowViewModel viewModel)
    {
        _vm = viewModel;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IGameDisplay Implementation (Output-only methods)
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void ShowTitle()
    {
        var sb = new StringBuilder();
        sb.AppendLine("  ██████╗ ██╗   ██╗███╗   ██╗ ██████╗ ███╗   ██╗███████╗");
        sb.AppendLine("  ██╔══██╗██║   ██║████╗  ██║██╔════╝ ████╗  ██║╚══███╔╝");
        sb.AppendLine("  ██║  ██║██║   ██║██╔██╗ ██║██║  ███╗██╔██╗ ██║  ███╔╝ ");
        sb.AppendLine("  ██║  ██║██║   ██║██║╚██╗██║██║   ██║██║╚██╗██║ ███╔╝  ");
        sb.AppendLine("  ██████╔╝╚██████╔╝██║ ╚████║╚██████╔╝██║ ╚████║███████╗");
        sb.AppendLine("  ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝");
        sb.AppendLine();
        sb.Append("              A dungeon awaits...");

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString(), "🏰 DUNGNZ"));
    }

    /// <inheritdoc/>
    public void ShowEnhancedTitle()
    {
        ShowTitle();
    }

    /// <inheritdoc/>
    public bool ShowIntroNarrative()
    {
        var narrative = "Long ago, the fortress of Dungnz stood proud against the darkness.\n" +
                        "Now it lies in ruin, its halls crawling with monsters.\n" +
                        "Many have ventured into its depths. Few return.\n\n" +
                        "Will you be the one to conquer it?";
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(narrative, "📜 Lore"));
        return false;
    }

    /// <inheritdoc/>
    public void ShowPrestigeInfo(PrestigeData prestige)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"⭐ Prestige Level: {prestige.PrestigeLevel}");
        sb.AppendLine($"Victories: {prestige.TotalWins}");
        sb.AppendLine($"Total Runs: {prestige.TotalRuns}");
        sb.AppendLine();
        
        if (prestige.PrestigeLevel > 0)
        {
            sb.AppendLine("Active Bonuses:");
            sb.AppendLine($"  +{prestige.PrestigeLevel * 2}% HP");
            sb.AppendLine($"  +{prestige.PrestigeLevel}% ATK/DEF");
            sb.AppendLine($"  +{prestige.PrestigeLevel * 50}g starting gold");
        }
        else
        {
            sb.Append("Complete a full run to unlock prestige bonuses!");
        }

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "⭐ Prestige"));
    }

    /// <inheritdoc/>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"════════════════════════════════");
        sb.AppendLine($"   FLOOR {floor} OF {maxFloor}");
        sb.AppendLine($"   {variant.Name}");
        sb.AppendLine($"════════════════════════════════");
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString(), $"Floor {floor}"));
    }

    /// <inheritdoc/>
    public void ShowRoom(Room room)
    {
        bool isNewRoom = _cachedRoom?.Id != room.Id;
        _cachedRoom = room;
        _cachedCooldowns = [];
        _cachedCombatEnemy = null;
        _cachedEnemyEffects = Array.Empty<ActiveEffect>();

        var sb = new StringBuilder();

        // Room type prefix with decoration
        var prefix = room.Type switch
        {
            RoomType.Dark             => "🌑 The room is pitch dark. ",
            RoomType.Scorched         => "🔥 Scorch marks scar the stone. ",
            RoomType.Flooded          => "💧 Ankle-deep water pools here. ",
            RoomType.Mossy            => "🌿 Damp moss covers the walls. ",
            RoomType.Ancient          => "🏛 Ancient runes line the walls. ",
            RoomType.ForgottenShrine  => "✨ Holy light radiates from a forgotten shrine. ",
            RoomType.PetrifiedLibrary => "📚 Petrified bookshelves line these ancient walls. ",
            RoomType.ContestedArmory  => "⚔ Weapon racks gleam dangerously in the dark. ",
            _                         => string.Empty
        };

        if (!string.IsNullOrEmpty(prefix))
            sb.AppendLine(prefix);

        sb.AppendLine(room.Description);

        // Environmental hazard
        var envLine = room.EnvironmentalHazard switch
        {
            RoomHazard.LavaSeam        => "🔥 Lava seams crack the floor — each action will burn you.",
            RoomHazard.CorruptedGround => "💀 The ground pulses with dark energy — it will drain you.",
            RoomHazard.BlessedClearing => "✨ A blessed warmth fills this clearing.",
            _                          => null
        };
        if (envLine != null) sb.AppendLine(envLine);

        // Hazard forewarning
        var hazardLine = room.Type switch
        {
            RoomType.Scorched => "⚠ The scorched stone radiates heat — take care.",
            RoomType.Flooded  => "⚠ The water here looks treacherous.",
            RoomType.Dark     => "⚠ Darkness presses in around you.",
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
            sb.AppendLine($"Exits: {string.Join("   ", ordered)}");
        }

        // Enemies
        if (room.Enemy != null)
            sb.AppendLine($"⚔ {room.Enemy.Name} is here!");

        // Items on floor
        if (room.Items.Count > 0)
        {
            sb.AppendLine("Items on the ground:");
            foreach (var item in room.Items)
                sb.AppendLine($"  ◆ {item.Name} ({PrimaryStatLabel(item)})");
        }

        // Special room hints
        if (room.HasShrine && room.Type != RoomType.ForgottenShrine)
            sb.AppendLine("✨ A shrine glimmers here. (USE SHRINE)");
        if (room.Type == RoomType.ForgottenShrine && !room.SpecialRoomUsed)
            sb.AppendLine("✨ A forgotten shrine stands here. (USE SHRINE)");
        if (room.Type == RoomType.PetrifiedLibrary && !room.SpecialRoomUsed)
            sb.AppendLine("📖 Ancient tomes line the walls. Something catches the light...");
        if (room.Type == RoomType.ContestedArmory && !room.SpecialRoomUsed)
            sb.AppendLine("⚠ Trapped weapons gleam in the dark. (USE ARMORY to approach)");
        if (room.Merchant != null)
            sb.AppendLine("🛒 A merchant awaits. (SHOP)");

        var roomName = GetRoomDisplayName(room);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.SetContent(sb.ToString().TrimEnd(), roomName);
            _vm.Map.Update(room, _currentFloor);
            if (_cachedPlayer != null)
            {
                _vm.Stats.Update(_cachedPlayer, _cachedCooldowns);
                _vm.Gear.Update(_cachedPlayer);
            }
        });
        
        if (isNewRoom)
            Dispatcher.UIThread.InvokeAsync(() => _vm.Log.AppendLog($"Entered {roomName}", "info"));
    }

    /// <inheritdoc/>
    public void ShowMap(Room currentRoom, int floor = 1)
    {
        _currentFloor = floor;
        _cachedRoom = currentRoom;
        Dispatcher.UIThread.InvokeAsync(() => _vm.Map.Update(currentRoom, floor));
    }

    /// <inheritdoc/>
    public void ShowCombat(string message)
    {
        var cleanMsg = StripAnsi(message);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.Clear();
            _vm.Content.SetContent($"═══ {cleanMsg} ═══", "⚔ Combat");
            _vm.Log.AppendLog(cleanMsg, "combat");
        });
    }

    /// <inheritdoc/>
    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        _cachedPlayer = player;
        _cachedCombatEnemy = enemy;
        _cachedEnemyEffects = enemyEffects;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Stats.UpdateCombat(player, _cachedCooldowns);
            _vm.Gear.ShowEnemyStats(enemy, enemyEffects);
        });

        // Low HP warning
        if (player.MaxHP > 0)
        {
            bool isLowHp = player.HP < player.MaxHP * 0.30;
            if (isLowHp && !_lowHpWarningIssued)
            {
                _lowHpWarningIssued = true;
                Dispatcher.UIThread.InvokeAsync(() => _vm.Log.AppendLog($"Low HP! {player.HP}/{player.MaxHP} — below 30%!", "combat"));
            }
            else if (!isLowHp)
            {
                _lowHpWarningIssued = false;
            }
        }
    }

    /// <inheritdoc/>
    public void ShowCombatMessage(string message)
    {
        var cleanMsg = StripAnsi(message);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.AppendMessage($"  {cleanMsg}");
            _vm.Log.AppendLog(cleanMsg, "combat");
        });
    }

    /// <inheritdoc/>
    public void ShowColoredCombatMessage(string message, string color)
    {
        var coloredMsg = $"{color}{StripAnsi(message)}{ColorCodes.Reset}";
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.AppendMessage($"  {coloredMsg}");
            _vm.Log.AppendLog(coloredMsg, "combat");
        });
    }

    /// <inheritdoc/>
    public void ShowCombatStart(Enemy enemy)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.Clear();
            _vm.Content.AppendMessage("⚔ ─── COMBAT ─── ⚔");
            _vm.Content.AppendMessage($"{enemy.Name}");
        });
    }

    /// <inheritdoc/>
    public void ShowCombatEntryFlags(Enemy enemy)
    {
        if (enemy.IsElite)
            Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage("⭐ ELITE ENEMY"));
        if (enemy is Dungnz.Systems.Enemies.DungeonBoss boss && boss.IsEnraged)
            Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage("⚡ ENRAGED!"));
    }

    /// <inheritdoc/>
    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt == null || enemy.AsciiArt.Length == 0)
            return;
        var art = string.Join("\n", enemy.AsciiArt);
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage(art));
    }

    /// <inheritdoc/>
    public void ShowEnemyDetail(Enemy enemy)
    {
        var hpBar = BuildPlainHpBar(enemy.HP, enemy.MaxHP, 12);
        var sb = new StringBuilder();
        sb.AppendLine($"HP {hpBar} {enemy.HP}/{enemy.MaxHP}");
        sb.AppendLine($"ATK {enemy.Attack}  DEF {enemy.Defense}");
        sb.AppendLine($"XP {enemy.XPValue}");
        if (enemy.IsElite)
            sb.Append("⭐ Elite");
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage(sb.ToString().TrimEnd()));
    }

    /// <inheritdoc/>
    public void ShowCombatHistory()
    {
        // Log panel already shows scrollback — just notify user
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage("Combat history is displayed in the Log panel."));
    }

    /// <inheritdoc/>
    public void ShowPlayerStats(Player player)
    {
        _cachedPlayer = player;
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_cachedCombatEnemy != null)
            {
                // In combat: update both stats and enemy panels
                _vm.Stats.UpdateCombat(player, _cachedCooldowns);
                _vm.Gear.ShowEnemyStats(_cachedCombatEnemy, _cachedEnemyEffects);
            }
            else
            {
                _vm.Stats.Update(player, _cachedCooldowns);
                _vm.Gear.Update(player);
            }
        });
    }

    /// <inheritdoc/>
    public void ShowInventory(Player player)
    {
        var sb = new StringBuilder();
        int currentWeight = player.Inventory.Sum(i => i.Weight);
        int maxWeight     = InventoryManager.MaxWeight;
        
        sb.AppendLine($"Slots: {player.Inventory.Count}/{Player.MaxInventorySize}  │  Weight: {currentWeight}/{maxWeight}");
        sb.AppendLine();

        if (player.Inventory.Count == 0)
        {
            sb.AppendLine("  (inventory empty)");
        }
        else
        {
            int idx = 1;
            foreach (var group in player.Inventory.GroupBy(i => i.Name))
            {
                var item     = group.First();
                var count    = group.Count();
                var countStr = count > 1 ? $" ×{count}" : "";
                sb.AppendLine($"  {idx,2}. {ItemIcon(item)} {item.Name}{countStr}  {PrimaryStatLabel(item)}");
                idx++;
            }
        }

        sb.AppendLine();
        sb.Append($"💰 {player.Gold}g");
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "🎒 Inventory"));
    }

    /// <inheritdoc/>
    public void ShowEquipment(Player player)
    {
        var sb = new StringBuilder();

        void AddSlot(string slotLabel, Item? item)
        {
            if (item == null)
            {
                sb.AppendLine($"{slotLabel}:  (empty)");
                return;
            }
            sb.AppendLine($"{slotLabel}:  {item.Name}  {PrimaryStatLabel(item)}");
        }

        AddSlot("⚔  Weapon",    player.EquippedWeapon);
        AddSlot("💍 Accessory", player.EquippedAccessory);
        AddSlot("🪖 Head",      player.EquippedHead);
        AddSlot("🥋 Shoulders", player.EquippedShoulders);
        AddSlot("🦺 Chest",     player.EquippedChest);
        AddSlot("🧤 Hands",     player.EquippedHands);
        AddSlot("👖 Legs",      player.EquippedLegs);
        AddSlot("👟 Feet",      player.EquippedFeet);
        AddSlot("🧥 Back",      player.EquippedBack);
        AddSlot("🔰 Off-Hand",  player.EquippedOffHand);

        var setDesc = SetBonusManager.GetActiveBonusDescription(player);
        if (!string.IsNullOrEmpty(setDesc))
        {
            sb.AppendLine();
            sb.Append($"Set Bonus: {setDesc}");
        }

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "⚔ Equipment"));
    }

    /// <inheritdoc/>
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"New: {ItemIcon(newItem)} {newItem.Name}");
        sb.AppendLine($"     {PrimaryStatLabel(newItem)}");
        
        if (oldItem != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Old: {ItemIcon(oldItem)} {oldItem.Name}");
            sb.AppendLine($"     {PrimaryStatLabel(oldItem)}");
            sb.AppendLine();
            
            var atkDelta  = newItem.AttackBonus  - oldItem.AttackBonus;
            var defDelta  = newItem.DefenseBonus - oldItem.DefenseBonus;
            var manaDelta = newItem.MaxManaBonus - oldItem.MaxManaBonus;
            
            if (atkDelta != 0)  sb.AppendLine($"ATK:  {FormatDelta(atkDelta)}");
            if (defDelta != 0)  sb.AppendLine($"DEF:  {FormatDelta(defDelta)}");
            if (manaDelta != 0) sb.AppendLine($"Mana: {FormatDelta(manaDelta)}");
        }
        else
        {
            sb.AppendLine();
            sb.Append("New slot — nothing equipped");
        }

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "⚖ Comparison"));
    }

    /// <inheritdoc/>
    public void ShowItemDetail(Item item)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Type:    {item.Type}");
        sb.AppendLine($"Tier:    {item.Tier}");
        sb.AppendLine($"Weight:  {item.Weight}");
        if (item.AttackBonus  != 0) sb.AppendLine($"Attack:  +{item.AttackBonus}");
        if (item.DefenseBonus != 0) sb.AppendLine($"Defense: +{item.DefenseBonus}");
        if (item.HealAmount   != 0) sb.AppendLine($"Heal:    +{item.HealAmount} HP");
        if (item.ManaRestore  != 0) sb.AppendLine($"Mana:    +{item.ManaRestore}");
        if (item.MaxManaBonus != 0) sb.AppendLine($"Max Mana:+{item.MaxManaBonus}");
        if (item.DodgeBonus   >  0) sb.AppendLine($"Dodge:   +{item.DodgeBonus:P0}");
        if (item.CritChance   >  0) sb.AppendLine($"Crit:    +{item.CritChance:P0}");
        if (!string.IsNullOrEmpty(item.Description))
        {
            sb.AppendLine();
            sb.Append(item.Description);
        }

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), $"{ItemIcon(item)} {item.Name}"));
    }

    /// <inheritdoc/>
    public void ShowLootDrop(Item item, Player player, bool isElite = false)
    {
        var header = isElite ? "✦ ELITE LOOT DROP" : "✦ LOOT DROP";
        var stat   = PrimaryStatLabel(item);

        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine($"{item.Tier}");
        sb.AppendLine($"{ItemIcon(item)} {item.Name}");
        sb.AppendLine($"{stat}  {item.Weight} wt");

        var equipped = GetEquippedInSameSlot(item, player);
        if (equipped != null)
        {
            sb.AppendLine();
            sb.AppendLine($"vs {equipped.Name}");
            
            var atkDelta  = item.AttackBonus  - equipped.AttackBonus;
            var defDelta  = item.DefenseBonus - equipped.DefenseBonus;
            
            if (atkDelta != 0) sb.AppendLine($"  ATK: {FormatDelta(atkDelta)}");
            if (defDelta != 0) sb.AppendLine($"  DEF: {FormatDelta(defDelta)}");
        }
        else if (item.IsEquippable)
        {
            sb.AppendLine();
            sb.Append("New slot — nothing equipped");
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.SetContent(sb.ToString().TrimEnd(), "💰 Loot");
            _vm.Log.AppendLog($"Loot: {item.Name}", "loot");
        });
    }

    /// <inheritdoc/>
    public void ShowGoldPickup(int amount, int newTotal)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.SetContent($"💰 +{amount} gold  (Total: {newTotal}g)", "💰 Gold");
            _vm.Log.AppendLog($"+{amount} gold (total: {newTotal}g)", "loot");
        });
    }

    /// <inheritdoc/>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{ItemIcon(item)} Picked up: {item.Name}");
        sb.AppendLine($"{PrimaryStatLabel(item)}");
        sb.AppendLine($"Slots: {slotsCurrent}/{slotsMax}  ·  Weight: {weightCurrent}/{weightMax}");
        if (weightCurrent > weightMax * 0.8)
            sb.Append("⚠ Inventory nearly full!");

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.SetContent(sb.ToString().TrimEnd(), "📦 Pickup");
            _vm.Log.AppendLog($"Picked up: {item.Name}", "loot");
        });
    }

    /// <inheritdoc/>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Your gold: {playerGold}g");
        sb.AppendLine();

        int idx = 1;
        foreach (var (item, price) in stock)
        {
            var afford = playerGold >= price ? "  " : "✗ ";
            sb.AppendLine($"  {idx,2}. {afford}{ItemIcon(item)} {item.Name} — {item.Tier} — {PrimaryStatLabel(item)} — {price}g");
            idx++;
        }

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "🛒 Merchant"));
    }

    /// <inheritdoc/>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Your gold: {playerGold}g");
        sb.AppendLine();

        int idx = 1;
        foreach (var (item, sellPrice) in items)
        {
            sb.AppendLine($"  {idx,2}. {ItemIcon(item)} {item.Name} — {PrimaryStatLabel(item)} — {sellPrice}g");
            idx++;
        }

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "💰 Sell Items"));
    }

    /// <inheritdoc/>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Recipe: {recipeName}");
        sb.AppendLine($"Result: {ItemIcon(result)} {result.Name}");
        sb.AppendLine($"Stats:  {PrimaryStatLabel(result)}");
        sb.AppendLine();
        sb.AppendLine("Ingredients:");
        
        foreach (var (ingredient, hasIt) in ingredients)
        {
            var mark = hasIt ? "✓" : "✗";
            sb.AppendLine($"  {mark} {ingredient}");
        }

        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "⚗ Crafting"));
    }

    /// <inheritdoc/>
    public void ShowMessage(string message)
    {
        var cleanMsg = StripAnsi(message);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.AppendMessage(cleanMsg);
            _vm.Log.AppendLog(cleanMsg, "info");
        });
    }

    /// <inheritdoc/>
    public void ShowError(string message)
    {
        var cleanMsg = StripAnsi(message);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.AppendMessage($"✗ {cleanMsg}");
            _vm.Log.AppendLog(cleanMsg, "error");
        });
    }

    /// <inheritdoc/>
    public void ShowHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("── Navigation ──");
        sb.AppendLine("go [n|s|e|w]  Move   │  look  Redescribe   │  map  Mini-map");
        sb.AppendLine("descend  Descend to the next floor   │  ascend  Return to previous floor");
        sb.AppendLine("return   Fast-travel to the floor entrance   │  back  Step back one room");
        sb.AppendLine();
        sb.AppendLine("── Items ──");
        sb.AppendLine("take [item]   use [item]   equip [item]   examine [target]");
        sb.AppendLine("inventory   equipment   craft [recipe]   shop   sell");
        sb.AppendLine();
        sb.AppendLine("── Character ──");
        sb.AppendLine("stats   skills   learn [skill]");
        sb.AppendLine();
        sb.AppendLine("── Systems ──");
        sb.AppendLine("save [name]   load [name]   listsaves");
        sb.AppendLine("prestige   leaderboard   help   quit");
        sb.AppendLine();
        sb.AppendLine("── Log ──");
        sb.Append("history   Show full combat log scrollback in this panel");
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "❓ Help"));
    }

    /// <inheritdoc/>
    public void ShowCommandPrompt(Player? player = null)
    {
        // Input panel wiring deferred to P5
    }

    /// <inheritdoc/>
    public void ShowColoredMessage(string message, string color)
    {
        var coloredMsg = $"{color}{StripAnsi(message)}{ColorCodes.Reset}";
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Content.AppendMessage(coloredMsg);
            _vm.Log.AppendLog(coloredMsg, "info");
        });
    }

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string valueColor)
    {
        var cleanLabel = StripAnsi(label);
        var cleanValue = StripAnsi(value);
        var line = $"{cleanLabel,-8} {valueColor}{cleanValue}{ColorCodes.Reset}";
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage(line));
    }

    /// <inheritdoc/>
    public void ShowLevelUpChoice(Player player)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🎉 LEVEL UP! Now level {player.Level}");
        sb.AppendLine();
        sb.AppendLine("Choose your stat boost:");
        sb.AppendLine("  1. +10 HP");
        sb.AppendLine("  2. +2 ATK");
        sb.Append("  3. +2 DEF");
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString(), "⭐ Level Up"));
    }

    /// <inheritdoc/>
    public void ShowVictory(Player player, int floorsCleared, RunStats stats)
    {
        var floorWord = floorsCleared == 1 ? "floor" : "floors";
        var sb = new StringBuilder();
        sb.AppendLine("✦  V I C T O R Y  ✦");
        sb.AppendLine();
        sb.AppendLine($"{player.Name}  •  Level {player.Level}  •  {player.Class}");
        sb.AppendLine($"{floorsCleared} {floorWord} conquered");
        sb.AppendLine();
        sb.AppendLine($"Enemies slain:  {stats.EnemiesDefeated}");
        sb.AppendLine($"Gold earned:    {stats.GoldCollected}");
        sb.AppendLine($"Items found:    {stats.ItemsFound}");
        sb.Append($"Turns taken:    {stats.TurnsTaken}");
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "🏆 Victory!"));
    }

    /// <inheritdoc/>
    public void ShowGameOver(Player player, string? killedBy, RunStats stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("☠  RUN ENDED  ☠");
        sb.AppendLine();
        sb.AppendLine($"{player.Name}  •  Level {player.Level}  •  {player.Class}");
        if (!string.IsNullOrEmpty(killedBy))
            sb.AppendLine($"Killed by: {killedBy}");
        sb.AppendLine();
        sb.AppendLine($"Enemies slain:  {stats.EnemiesDefeated}");
        sb.AppendLine($"Floors reached: {stats.FloorsVisited}");
        sb.AppendLine($"Gold earned:    {stats.GoldCollected}");
        sb.AppendLine($"Items found:    {stats.ItemsFound}");
        sb.Append($"Turns taken:    {stats.TurnsTaken}");
        
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(sb.ToString().TrimEnd(), "☠ Game Over"));
    }

    /// <inheritdoc/>
    public void RefreshDisplay(Player player, Room room, int floor)
    {
        _currentFloor = floor;
        _cachedPlayer = player;
        _cachedRoom = room;
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Stats.Update(player, _cachedCooldowns);
            _vm.Map.Update(room, floor);
            _vm.Gear.Update(player);
        });
        
        ShowRoom(room);
    }

    /// <inheritdoc/>
    public void UpdateCooldownDisplay(IReadOnlyList<(string name, int turnsRemaining)> cooldowns)
    {
        _cachedCooldowns = cooldowns;
        if (_cachedPlayer != null)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_cachedCombatEnemy != null)
                    _vm.Stats.UpdateCombat(_cachedPlayer, cooldowns);
                else
                    _vm.Stats.Update(_cachedPlayer, cooldowns);
            });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IGameInput Implementation (Input-coupled methods)
    // ══════════════════════════════════════════════════════════════════════════

    private TaskCompletionSource<string?>? _pendingCommand;

    // ── Core Input Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Blocks the game thread until the player types text and presses Enter in the
    /// Avalonia input panel. Uses <see cref="TaskCompletionSource{T}"/> to bridge
    /// between the game thread and the Avalonia UI thread.
    /// </summary>
    /// <param name="prompt">The prompt string shown next to the text box.</param>
    /// <returns>The raw text the player typed, or <see langword="null"/> if blank.</returns>
    private string? WaitForTextInput(string prompt = "> ")
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCommand = tcs;

        void OnSubmitted(string text)
        {
            var pending = Interlocked.Exchange(ref _pendingCommand, null);
            _vm.Input.InputSubmitted -= OnSubmitted;
            pending?.TrySetResult(text);
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Input.InputSubmitted += OnSubmitted;
            _vm.Input.PromptText = prompt;
            _vm.Input.CommandText = "";
            _vm.Input.IsInputEnabled = true;
        });

        var result = tcs.Task.GetAwaiter().GetResult();

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Input.IsInputEnabled = false;
        });

        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

    /// <summary>
    /// Displays a numbered menu in the content panel, waits for the player to type a
    /// number, validates the input, and returns the corresponding value. Re-prompts on
    /// invalid input. This is the generic menu selection infrastructure for Avalonia.
    /// </summary>
    /// <typeparam name="T">The type of value associated with each menu option.</typeparam>
    /// <param name="header">Header text displayed above the options.</param>
    /// <param name="options">The list of (Label, Value) pairs to display.</param>
    /// <param name="allowCancel">When <see langword="true"/>, entering "0" or empty returns <paramref name="cancelValue"/>.</param>
    /// <param name="cancelValue">The value returned when the player cancels.</param>
    /// <returns>The value corresponding to the player's valid selection.</returns>
    private T SelectFromMenu<T>(string header, IReadOnlyList<(string Label, T Value)> options,
        bool allowCancel = false, T? cancelValue = default)
    {
        // Build the menu text once (it doesn't change between iterations)
        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine();
        for (int i = 0; i < options.Count; i++)
            sb.AppendLine($"  [{i + 1}] {options[i].Label}");
        if (allowCancel)
            sb.AppendLine($"  [0] Cancel");
        var menuText = sb.ToString().TrimEnd();

        // Input loop — re-display menu and re-prompt until valid
        while (true)
        {
            Dispatcher.UIThread.InvokeAsync(() => _vm.Content.SetContent(menuText, header));

            var input = WaitForTextInput("Choice: ");

            // Cancel path
            if (allowCancel && (input == null || input == "0"))
                return cancelValue!;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= options.Count)
                return options[choice - 1].Value;

            // Invalid — log error, loop will re-display menu
            Dispatcher.UIThread.InvokeAsync(() =>
                _vm.Log.AppendLog($"Invalid choice. Enter 1–{options.Count}{(allowCancel ? " or 0 to cancel" : "")}.", "error"));
        }
    }

    // ── ReadCommandInput ──────────────────────────────────────────────────────

    /// <summary>
    /// Blocks the game thread until the player types a command and presses Enter
    /// in the Avalonia input panel.
    /// </summary>
    /// <returns>The trimmed command string, or <see langword="null"/> if blank.</returns>
    public string? ReadCommandInput() => WaitForTextInput("> ");

    // ── Text Entry (Startup) ─────────────────────────────────────────────────

    /// <inheritdoc/>
    public string ReadPlayerName()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
            _vm.Content.SetContent("Enter your name, adventurer:", "✏ Name"));

        var name = WaitForTextInput("Name: ");
        return string.IsNullOrWhiteSpace(name) ? "Hero" : name;
    }

    /// <inheritdoc/>
    public int? ReadSeed()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
            _vm.Content.SetContent(
                "Enter a 6-digit seed (100000–999999)\nor press Enter to cancel.",
                "🌱 Seed"));

        while (true)
        {
            var input = WaitForTextInput("Seed: ");

            if (input == null)
                return null;

            if (input.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                return null;

            if (int.TryParse(input, out var seed) && seed >= 100000 && seed <= 999999)
                return seed;

            Dispatcher.UIThread.InvokeAsync(() =>
                _vm.Log.AppendLog("Invalid seed. Enter a 6-digit number (100000–999999) or press Enter to cancel.", "error"));
        }
    }

    // ── Startup Flow ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public StartupMenuOption ShowStartupMenu(bool hasSaves)
    {
        var options = new List<(string Label, StartupMenuOption Value)>
        {
            ("🗡  New Game", StartupMenuOption.NewGame)
        };

        if (hasSaves)
            options.Add(("📂 Load Save", StartupMenuOption.LoadSave));

        options.Add(("🌱 New Game with Seed", StartupMenuOption.NewGameWithSeed));
        options.Add(("✖  Exit", StartupMenuOption.Exit));

        return SelectFromMenu("What would you like to do?", options);
    }

    /// <inheritdoc/>
    public Difficulty SelectDifficulty()
    {
        var options = new (string Label, Difficulty Value)[]
        {
            ("CASUAL — Weaker enemies · Cheap shops · Start with 50g + 3 potions", Difficulty.Casual),
            ("NORMAL — Balanced challenge · The intended experience · Start with 15g + 1 potion", Difficulty.Normal),
            ("HARD   — Stronger enemies · Scarce rewards · No starting supplies · ☠ Permadeath", Difficulty.Hard),
        };
        return SelectFromMenu("Choose your difficulty:", options);
    }

    /// <inheritdoc/>
    public PlayerClassDefinition SelectClass(PrestigeData? prestige)
    {
        const int baseHP = 100;
        const int baseAttack = 10;
        const int baseDefense = 5;
        const int baseMana = 30;

        // Build class cards in content panel
        var sb = new StringBuilder();
        int number = 1;
        foreach (var def in PlayerClassDefinition.All)
        {
            int hp  = baseHP + def.BonusMaxHP;
            int atk = baseAttack + def.BonusAttack;
            int def_ = baseDefense + def.BonusDefense;
            int mana = baseMana + def.BonusMaxMana;

            sb.AppendLine($"  [{number}] {def.Name.ToUpperInvariant()}");
            sb.AppendLine($"      HP: {hp}  ATK: {atk}  DEF: {def_}  Mana: {mana}");
            sb.AppendLine($"      \"{def.Description}\"");

            if (prestige is { PrestigeLevel: > 0 })
            {
                var extras = new List<string>();
                if (prestige.BonusStartHP > 0) extras.Add($"+{prestige.BonusStartHP} HP");
                if (prestige.BonusStartAttack > 0) extras.Add($"+{prestige.BonusStartAttack} ATK");
                if (prestige.BonusStartDefense > 0) extras.Add($"+{prestige.BonusStartDefense} DEF");
                if (extras.Count > 0)
                    sb.AppendLine($"      Prestige: {string.Join(", ", extras)}");
            }

            sb.AppendLine();
            number++;
        }

        Dispatcher.UIThread.InvokeAsync(() =>
            _vm.Content.SetContent(sb.ToString().TrimEnd(), "⚔ Choose Your Class"));

        // Build selection options
        var selectOptions = PlayerClassDefinition.All
            .Select(d => (d.Name, d))
            .ToArray();

        // Use manual input loop (content already rendered with the cards)
        while (true)
        {
            var input = WaitForTextInput("Class #: ");

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= selectOptions.Length)
                return selectOptions[choice - 1].Item2;

            Dispatcher.UIThread.InvokeAsync(() =>
                _vm.Log.AppendLog($"Invalid choice. Enter 1–{selectOptions.Length}.", "error"));
        }
    }

    /// <inheritdoc/>
    public string? SelectSaveToLoad(string[] saveNames)
    {
        var options = saveNames
            .Select(name => (name, (string?)name))
            .Append(("↩  Back", (string?)null))
            .ToArray();

        return SelectFromMenu("Choose a save to load:", options, allowCancel: false);
    }

    // ── Combat Menus ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
    {
        // Build resource info header
        var info = new StringBuilder();
        info.Append($"Mana: {player.Mana}/{player.MaxMana}");
        if (player.Class == PlayerClass.Rogue)
        {
            var dots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            info.Append($"  ⚡ Combo: {dots}");
        }
        if (player.Class == PlayerClass.Mage && player.IsManaShieldActive)
            info.Append(" [SHIELD ACTIVE]");
        if (player.Class == PlayerClass.Paladin && player.DivineShieldTurnsRemaining > 0)
            info.Append($" [DIVINE SHIELD: {player.DivineShieldTurnsRemaining}T]");

        var options = new (string Label, string Value)[]
        {
            ($"{ColorCodes.Yellow}[A]{ColorCodes.Reset} ⚔  Attack",  "A"),
            ($"{ColorCodes.Yellow}[B]{ColorCodes.Reset} ✨ Ability",  "B"),
            ($"{ColorCodes.Yellow}[F]{ColorCodes.Reset} 🏃 Flee",     "F"),
            ($"{ColorCodes.Yellow}[I]{ColorCodes.Reset} 🧪 Use Item", "I"),
        };

        // Build combined menu text
        var sb = new StringBuilder();
        sb.AppendLine(info.ToString());
        sb.AppendLine();
        for (int i = 0; i < options.Length; i++)
            sb.AppendLine($"  [{i + 1}] {options[i].Label}");

        Dispatcher.UIThread.InvokeAsync(() =>
            _vm.Content.SetContent(sb.ToString().TrimEnd(), "⚔ Combat Action"));

        while (true)
        {
            var input = WaitForTextInput("Action: ");

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= options.Length)
                return options[choice - 1].Value;

            // Also accept letter shortcuts directly
            if (input != null)
            {
                var upper = input.ToUpperInvariant();
                if (upper is "A" or "B" or "F" or "I")
                    return upper;
            }

            Dispatcher.UIThread.InvokeAsync(() =>
                _vm.Log.AppendLog("Invalid choice. Enter 1–4 or A/B/F/I.", "error"));
        }
    }

    /// <inheritdoc/>
    public Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities)
    {
        var sb = new StringBuilder();

        // Show unavailable abilities as info lines (not selectable)
        foreach (var (ability, onCooldown, cooldownTurns, notEnoughMana) in unavailableAbilities)
        {
            if (onCooldown)
                sb.AppendLine($"  {ColorCodes.Gray}○ {ability.Name} — {ColorCodes.Yellow}[COOLDOWN: {cooldownTurns}t]{ColorCodes.Reset}");
            else if (notEnoughMana)
                sb.AppendLine($"  {ColorCodes.Gray}○ {ability.Name} — {ColorCodes.BrightRed}[NEED {ability.ManaCost} MP]{ColorCodes.Reset}");
        }

        sb.AppendLine();

        // Build selectable options
        var availList = availableAbilities.ToList();
        var options = availList
            .Select(a => ($"{a.Name} — {a.Description} (Cost: {a.ManaCost} MP)", (Ability?)a))
            .Append(("↩  Cancel", (Ability?)null))
            .ToArray();

        for (int i = 0; i < options.Length; i++)
            sb.AppendLine($"  [{i + 1}] {options[i].Item1}");

        Dispatcher.UIThread.InvokeAsync(() =>
            _vm.Content.SetContent(sb.ToString().TrimEnd(), "✨ Abilities"));

        while (true)
        {
            var input = WaitForTextInput("Ability #: ");

            // Empty/null = cancel (last option)
            if (input == null)
                return null;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= options.Length)
                return options[choice - 1].Item2;

            Dispatcher.UIThread.InvokeAsync(() =>
                _vm.Log.AppendLog($"Invalid choice. Enter 1–{options.Length}.", "error"));
        }
    }

    /// <inheritdoc/>
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

        return SelectFromMenu("Choose a consumable:", options);
    }

    /// <inheritdoc/>
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2)
    {
        var options = new (string Label, int Value)[]
        {
            (option1, 1),
            (option2, 2),
            ("Leave", 0),
        };
        return SelectFromMenu(header, options);
    }

    // ── Inventory / Equipment Menus ──────────────────────────────────────────

    /// <inheritdoc/>
    public Item? ShowInventoryAndSelect(Player player)
    {
        ShowInventory(player);

        if (player.Inventory.Count == 0)
            return null;

        var options = player.Inventory
            .Select(item => ($"{ItemIcon(item)} {item.Name}  {PrimaryStatLabel(item)}", (Item?)item))
            .Append(("↩  Cancel", (Item?)null))
            .ToArray();

        return SelectFromMenu("Select an item:", options, allowCancel: true);
    }

    /// <inheritdoc/>
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
    {
        var options = equippable
            .Select(item =>
            {
                var icon = ItemIcon(item);
                var stat = PrimaryStatLabel(item);
                return ($"{icon} {item.Name}  [{stat}]", (Item?)item);
            })
            .Append(("↩  Cancel", (Item?)null))
            .ToArray();

        return SelectFromMenu("EQUIP — Choose an item:", options);
    }

    /// <inheritdoc/>
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable)
    {
        var options = usable
            .Select(item =>
            {
                var icon = ItemIcon(item);
                var stat = PrimaryStatLabel(item);
                return ($"{icon} {item.Name}  [{stat}]", (Item?)item);
            })
            .Append(("↩  Cancel", (Item?)null))
            .ToArray();

        return SelectFromMenu("Use which item?", options);
    }

    /// <inheritdoc/>
    public TakeSelection? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems)
    {
        var options = roomItems
            .Select(item =>
            {
                var icon = ItemIcon(item);
                var stat = PrimaryStatLabel(item);
                return ($"{icon} {item.Name}  [{stat}]", (TakeSelection?)new TakeSelection.Single(item));
            })
            .Prepend(("📦 Take All", (TakeSelection?)new TakeSelection.All()))
            .Append(("↩  Cancel", (TakeSelection?)null))
            .ToArray();

        return SelectFromMenu("TAKE — Choose an item:", options);
    }

    // ── Shop / Craft Menus ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        ShowShop(stockList, playerGold);

        var options = stockList
            .Select((s, i) => ($"{ItemIcon(s.item)} {s.item.Name} — {s.price}g", i + 1))
            .Append(("↩  Cancel", 0))
            .ToArray();

        return SelectFromMenu($"Your gold: {playerGold}g — Buy which item?", options);
    }

    /// <inheritdoc/>
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        var itemList = items.ToList();
        ShowSellMenu(itemList, playerGold);

        var options = itemList
            .Select((s, i) => ($"{ItemIcon(s.item)} {s.item.Name} — sell for {s.sellPrice}g", i + 1))
            .Append(("↩  Cancel", 0))
            .ToArray();

        return SelectFromMenu($"Your gold: {playerGold}g — Sell which item?", options);
    }

    /// <inheritdoc/>
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        ShowShop(stockList, playerGold);

        var options = stockList
            .Select((s, i) => ($"{ItemIcon(s.item)} {s.item.Name} — {s.price}g", i + 1))
            .Append(("💰 Sell Items", -1))
            .Append(("Leave", 0))
            .ToArray();

        return SelectFromMenu($"Your gold: {playerGold}g — What would you like to do?", options);
    }

    /// <inheritdoc/>
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes)
    {
        var recipeList = recipes.ToList();

        var options = recipeList
            .Select((r, i) => (
                Label: r.canCraft ? $"✅ {r.recipeName}" : $"❌ {r.recipeName}",
                Value: i + 1))
            .Append(("↩  Cancel", 0))
            .ToArray();

        return SelectFromMenu("CRAFTING — Choose a recipe:", options);
    }

    // ── Progression Menus ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public int ShowLevelUpChoiceAndSelect(Player player)
    {
        var options = new (string Label, int Value)[]
        {
            ($"+5 Max HP     ({player.MaxHP} → {player.MaxHP + 5})", 1),
            ($"+2 Attack     ({player.Attack} → {player.Attack + 2})", 2),
            ($"+2 Defense    ({player.Defense} → {player.Defense + 2})", 3),
        };
        return SelectFromMenu("★ Choose a stat bonus:", options);
    }

    /// <inheritdoc/>
    public Skill? ShowSkillTreeMenu(Player player)
    {
        var allSkills = SkillTree.GetSkillsForClass(player);

        var sb = new StringBuilder();
        sb.AppendLine($"Your level: {player.Level}");
        sb.AppendLine();

        var available = new List<(string Label, Skill? Value)>();

        foreach (var skill in allSkills)
        {
            if (player.Skills.IsUnlocked(skill))
                continue;

            var (minLevel, _) = SkillTree.GetSkillRequirements(skill);
            var desc = SkillTree.GetDescription(skill);

            if (player.Level >= minLevel)
            {
                available.Add(($"{skill}: {desc}", skill));
                sb.AppendLine($"  ★ {skill}: {desc} [Available]");
            }
            else
            {
                sb.AppendLine($"  ○ {skill}: {desc} [Req. Lv{minLevel}]");
            }
        }

        if (available.Count == 0)
        {
            sb.AppendLine();
            sb.Append("No skills available to learn right now.");
            Dispatcher.UIThread.InvokeAsync(() =>
                _vm.Content.SetContent(sb.ToString().TrimEnd(), "📖 Skill Tree"));

            // Show briefly and return null — player can press Enter to dismiss
            WaitForTextInput("Press Enter...");
            return null;
        }

        available.Add(("↩  Cancel", null));

        // Add numbered options to display
        sb.AppendLine();
        sb.AppendLine("Learn a skill:");
        for (int i = 0; i < available.Count; i++)
            sb.AppendLine($"  [{i + 1}] {available[i].Label}");

        Dispatcher.UIThread.InvokeAsync(() =>
            _vm.Content.SetContent(sb.ToString().TrimEnd(), "📖 Skill Tree"));

        while (true)
        {
            var input = WaitForTextInput("Skill #: ");

            if (input == null)
                return null;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= available.Count)
                return available[choice - 1].Value;

            Dispatcher.UIThread.InvokeAsync(() =>
                _vm.Log.AppendLog($"Invalid choice. Enter 1–{available.Count}.", "error"));
        }
    }

    // ── Special Room Menus ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75)
    {
        var options = new (string Label, int Value)[]
        {
            ($"Heal fully        — {healCost}g  (Your gold: {playerGold}g)", 1),
            ($"Bless             — {blessCost}g  (+2 ATK/DEF permanently)", 2),
            ($"Fortify           — {fortifyCost}g  (MaxHP +10, permanent)", 3),
            ($"Meditate          — {meditateCost}g  (MaxMana +10, permanent)", 4),
            ("Leave", 0),
        };
        return SelectFromMenu("✨ Shrine Menu:", options);
    }

    /// <inheritdoc/>
    public int ShowForgottenShrineMenuAndSelect()
    {
        var options = new (string Label, int Value)[]
        {
            ("Holy Strength   — +5 ATK (lasts until next floor)", 1),
            ("Sacred Ground   — Auto-heal at shrines", 2),
            ("Warding Veil    — 20% chance to deflect enemy attacks this floor", 3),
            ("Leave", 0),
        };
        return SelectFromMenu("🕯 Forgotten Shrine — choose a blessing:", options);
    }

    /// <inheritdoc/>
    public int ShowContestedArmoryMenuAndSelect(int playerDefense)
    {
        var options = new (string Label, int Value)[]
        {
            ($"Careful approach — disarm traps (requires DEF > 12, yours: {playerDefense})", 1),
            ("Reckless grab   — take what you can (15-30 damage)", 2),
            ("Leave", 0),
        };
        return SelectFromMenu("⚔ Contested Armory — how do you approach?", options);
    }

    // ── Utility Menus ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool ShowConfirmMenu(string prompt)
    {
        var options = new (string Label, bool Value)[]
        {
            ("Yes", true),
            ("No", false),
        };
        return SelectFromMenu(prompt, options);
    }

    // ── Private static helpers ────────────────────────────────────────────────

    private static string StripAnsi(string text) =>
        Regex.Replace(text, @"\x1b\[[0-9;]*m", "");

    private static string BuildPlainHpBar(int current, int max, int width = 10)
    {
        if (max <= 0) return new string('░', width);
        current = Math.Clamp(current, 0, max);
        int filled = (int)Math.Round((double)current / max * width);
        return new string('█', filled) + new string('░', width - filled);
    }

    private static string ItemTypeIcon(ItemType type) => type switch
    {
        ItemType.Weapon           => "⚔",
        ItemType.Armor            => "🦺",
        ItemType.Consumable       => "🧪",
        ItemType.Accessory        => "💍",
        ItemType.CraftingMaterial => "⚗",
        _                         => "•"
    };

    private static string SlotIcon(ArmorSlot slot) => slot switch
    {
        ArmorSlot.Head      => "🪖",
        ArmorSlot.Shoulders => "🥋",
        ArmorSlot.Chest     => "🦺",
        ArmorSlot.Hands     => "🧤",
        ArmorSlot.Legs      => "👖",
        ArmorSlot.Feet      => "👟",
        ArmorSlot.Back      => "🧥",
        ArmorSlot.OffHand   => "🔰",
        _                   => "🦺",
    };

    private static string ItemIcon(Item item) =>
        item.Type == ItemType.Armor ? SlotIcon(item.Slot) : ItemTypeIcon(item.Type);

    private static string PrimaryStatLabel(Item item)
    {
        if (item.AttackBonus  != 0) return $"Attack +{item.AttackBonus}";
        if (item.DefenseBonus != 0) return $"Defense +{item.DefenseBonus}";
        if (item.HealAmount   != 0) return $"Heals {item.HealAmount} HP";
        if (item.ManaRestore  != 0) return $"Mana +{item.ManaRestore}";
        if (item.MaxManaBonus != 0) return $"Max Mana +{item.MaxManaBonus}";
        return item.Type.ToString();
    }

    private static string GetRoomDisplayName(Room room) => room.Type switch
    {
        RoomType.Standard         => "Dungeon Room",
        RoomType.Dark             => "Dark Chamber",
        RoomType.Scorched         => "Scorched Hall",
        RoomType.Flooded          => "Flooded Passage",
        RoomType.Mossy            => "Mossy Alcove",
        RoomType.Ancient          => "Ancient Hall",
        RoomType.ForgottenShrine  => "Forgotten Shrine",
        RoomType.PetrifiedLibrary => "Petrified Library",
        RoomType.ContestedArmory  => "Contested Armory",
        RoomType.TrapRoom         => "Trap Room",
        _                         => "Dungeon Room",
    };

    private static Item? GetEquippedInSameSlot(Item candidate, Player player) =>
        candidate.Type switch
        {
            ItemType.Weapon    => player.EquippedWeapon,
            ItemType.Accessory => player.EquippedAccessory,
            ItemType.Armor     => candidate.Slot switch
            {
                ArmorSlot.Head      => player.EquippedHead,
                ArmorSlot.Shoulders => player.EquippedShoulders,
                ArmorSlot.Chest     => player.EquippedChest,
                ArmorSlot.Hands     => player.EquippedHands,
                ArmorSlot.Legs      => player.EquippedLegs,
                ArmorSlot.Feet      => player.EquippedFeet,
                ArmorSlot.Back      => player.EquippedBack,
                ArmorSlot.OffHand   => player.EquippedOffHand,
                _                   => player.EquippedChest,
            },
            _ => null,
        };

    private static string FormatDelta(int delta) =>
        delta > 0 ? $"+{delta}" : delta.ToString();
}
