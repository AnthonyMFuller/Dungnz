using System.Text;
using System.Text.RegularExpressions;
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
        // For plain text, ignore color
        ShowCombatMessage(message);
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
        // For plain text, ignore color
        ShowMessage(message);
    }

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string valueColor)
    {
        var cleanLabel = StripAnsi(label);
        var cleanValue = StripAnsi(value);
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage($"{cleanLabel,-8} {cleanValue}"));
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

    // TODO: P5-P8 implementation
    private TaskCompletionSource<string?>? _pendingCommand;

    /// <summary>
    /// Blocks the game thread until the player types a command and presses Enter
    /// in the Avalonia input panel. Uses <see cref="TaskCompletionSource{T}"/> to
    /// bridge between the game thread and the Avalonia UI thread.
    /// </summary>
    /// <returns>The trimmed command string, or <see langword="null"/> if blank.</returns>
    public string? ReadCommandInput()
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCommand = tcs;

        void OnSubmitted(string text)
        {
            var pending = _pendingCommand;
            _pendingCommand = null;
            _vm.Input.InputSubmitted -= OnSubmitted;
            pending?.TrySetResult(text);
        }

        // Enable input on UI thread
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Input.InputSubmitted += OnSubmitted;
            _vm.Input.PromptText = "> ";
            _vm.Input.CommandText = "";
            _vm.Input.IsInputEnabled = true;
        });

        // Block game thread until the player submits
        var result = tcs.Task.GetAwaiter().GetResult();

        // Ensure input is disabled after submission
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Input.IsInputEnabled = false;
        });

        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }
    public string ReadPlayerName() => "Player";
    public int? ReadSeed() => null;

    public StartupMenuOption ShowStartupMenu(bool hasSaves) => StartupMenuOption.NewGame;
    public Difficulty SelectDifficulty() => Difficulty.Normal;
    public PlayerClassDefinition SelectClass(PrestigeData? prestige) => PlayerClassDefinition.Warrior;
    public string? SelectSaveToLoad(string[] saveNames) => null;

    public string ShowCombatMenuAndSelect(Player player, Enemy enemy) => "a";
    public Ability? ShowAbilityMenuAndSelect(IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities, IEnumerable<Ability> availableAbilities) => null;
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables) => null;
    public int ShowLevelUpChoiceAndSelect(Player player) => 1;

    public Item? ShowInventoryAndSelect(Player player) => null;
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable) => null;
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable) => null;
    public TakeSelection? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems) => null;

    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold) => 0;
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold) => 0;
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold) => 0;
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes) => 0;

    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75) => 0;
    public bool ShowConfirmMenu(string prompt) => false;
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2) => 1;
    public int ShowForgottenShrineMenuAndSelect() => 0;
    public int ShowContestedArmoryMenuAndSelect(int playerDefense) => 0;

    public Skill? ShowSkillTreeMenu(Player player) => null;

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
