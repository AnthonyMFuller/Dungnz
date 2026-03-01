using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Tests.Helpers;

public class FakeDisplayService : IDisplayService
{
    private IInputReader? _input;

    public FakeDisplayService(IInputReader? input = null) { _input = input; }
    public void SetInputReader(IInputReader reader) { _input = reader; }

    public List<string> Messages { get; } = new();
    public List<string> Errors { get; } = new();
    public List<string> CombatMessages { get; } = new();
    public List<string> RawCombatMessages { get; } = new();
    public List<string> AllOutput { get; } = new();

    /// <summary>
    /// Strips ANSI color codes from text to ensure tests check plain text content.
    /// </summary>
    private static string StripAnsi(string text) => ColorCodes.StripAnsiCodes(text);

    public void ShowTitle() { }
    public void ShowCommandPrompt(Player? player = null) { }
    public void ShowHelp() { AllOutput.Add("help"); }
    public void ShowRoom(Room room) { AllOutput.Add($"room:{room.Description}"); }
    public void ShowMap(Room room) { AllOutput.Add($"map:{room.Description}"); }
    public string ReadPlayerName() => "TestPlayer";

    public void ShowMessage(string message)
    {
        var plain = StripAnsi(message);
        Messages.Add(plain);
        AllOutput.Add(plain);
    }

    public void ShowError(string message)
    {
        var plain = StripAnsi(message);
        Errors.Add(plain);
        AllOutput.Add($"ERROR:{plain}");
    }

    public void ShowCombat(string message)
    {
        var plain = StripAnsi(message);
        CombatMessages.Add(plain);
        AllOutput.Add($"combat:{plain}");
    }

    public void ShowCombatMessage(string message)
    {
        var plain = StripAnsi(message);
        RawCombatMessages.Add(message);
        CombatMessages.Add(plain);
        AllOutput.Add($"combatmsg:{plain}");
    }

    public void ShowCombatStatus(Player player, Enemy enemy, 
        IReadOnlyList<ActiveEffect> playerEffects, 
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        AllOutput.Add($"status:{player.HP}/{player.MaxHP} vs {enemy.HP}/{enemy.MaxHP}");
    }

    public void ShowPlayerStats(Player player)
    {
        AllOutput.Add($"stats:{player.Name}");
    }

    public void ShowInventory(Player player)
    {
        AllOutput.Add($"inventory:{player.Inventory.Count}");
    }

    public void ShowLootDrop(Item item, Player player, bool isElite = false)
    {
        AllOutput.Add($"loot:{item.Name}");
    }

    public void ShowGoldPickup(int amount, int newTotal) { AllOutput.Add($"gold:+{amount}:total:{newTotal}"); }
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax) { AllOutput.Add($"pickup:{item.Name}:{slotsCurrent}/{slotsMax}:{weightCurrent}/{weightMax}"); }
    public void ShowItemDetail(Item item) { Messages.Add($"{item.Name}: {item.Description}"); }

    public void ShowColoredMessage(string message, string color)
    {
        var plain = StripAnsi(message);
        Messages.Add(plain);
        AllOutput.Add(plain);
    }

    public void ShowColoredCombatMessage(string message, string color)
    {
        var plain = StripAnsi(message);
        CombatMessages.Add(plain);
        AllOutput.Add($"combatmsg:{plain}");
    }

    public void ShowColoredStat(string label, string value, string valueColor)
    {
        var plain = StripAnsi(value);
        AllOutput.Add($"{label}{plain}");
    }

    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem)
    {
        AllOutput.Add($"equipment_compare:{oldItem?.Name ?? "none"}->{newItem.Name}");
    }

    public bool ShowEnhancedTitleCalled { get; private set; }
    public bool ShowIntroNarrativeCalled { get; private set; }
    public PrestigeData? LastPrestigeInfo { get; private set; }

    // For SelectDifficulty - configurable return value
    public Difficulty SelectDifficultyResult { get; set; } = Difficulty.Normal;
    // For SelectClass - configurable return value
    public PlayerClassDefinition SelectClassResult { get; set; } = PlayerClassDefinition.Warrior;

    public void ShowEnhancedTitle() { ShowEnhancedTitleCalled = true; }
    public bool ShowIntroNarrative() { ShowIntroNarrativeCalled = true; return false; }
    public void ShowPrestigeInfo(PrestigeData prestige) { LastPrestigeInfo = prestige; }
    public Difficulty SelectDifficulty() => SelectDifficultyResult;
    public PlayerClassDefinition SelectClass(PrestigeData? prestige) => SelectClassResult;
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold) { AllOutput.Add($"shop:{playerGold}g"); }
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold) { AllOutput.Add($"shop_select:{playerGold}g"); return 0; }
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold) { AllOutput.Add($"sell:{playerGold}g"); }
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        AllOutput.Add($"sell_select:{playerGold}g");
        if (_input is not null)
        {
            var line = _input.ReadLine()?.Trim() ?? "";
            if (int.TryParse(line, out int n) && n >= 0) return n;
        }
        return 0;
    }
    public int ShowLevelUpChoiceAndSelect(Player player)
    {
        AllOutput.Add($"levelup_select:{player.Level}");
        if (_input is not null)
        {
            if (int.TryParse(_input.ReadLine()?.Trim(), out int choice)) return choice;
            return 1;
        }
        return 1;
    }
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy) { AllOutput.Add($"combat_menu:{enemy.Name}"); return _input?.ReadLine()?.Trim().ToUpperInvariant() ?? "A"; }
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes) { AllOutput.Add("craft_menu"); return 0; }
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients) { AllOutput.Add($"recipe:{recipeName}"); }
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2) { AllOutput.Add("trap_choice"); return 0; }
    public int ShowForgottenShrineMenuAndSelect() { AllOutput.Add("shrine_menu"); return 0; }
    public int ShowContestedArmoryMenuAndSelect(int playerDefense) { AllOutput.Add("armory_menu"); return 0; }
    public int ShowShrineMenuAndSelect(int playerGold) 
    { 
        AllOutput.Add("shrine_regular"); 
        if (_input is not null)
        {
            var line = _input.ReadLine()?.Trim() ?? "";
            if (int.TryParse(line, out int n) && n >= 0 && n <= 4) return n;
        }
        return 0; 
    }
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        AllOutput.Add($"shop_with_sell:{playerGold}g");
        if (_input is not null)
        {
            var line = _input.ReadLine()?.Trim() ?? "";
            if (int.TryParse(line, out int n)) return n;
        }
        return 0;
    }
    public bool ShowConfirmMenu(string prompt)
    {
        AllOutput.Add($"confirm:{prompt}");
        if (_input is not null)
        {
            var line = _input.ReadLine()?.Trim().ToUpperInvariant() ?? "";
            if (line == "Y" || line == "YES" || line == "1") return true;
        }
        return false;
    }
    public Ability? ShowAbilityMenuAndSelect(IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities, IEnumerable<Ability> availableAbilities) 
    { 
        AllOutput.Add("ability_menu");
        if (_input is not null)
        {
            var available = availableAbilities.ToList();
            var line = _input.ReadLine()?.Trim() ?? "";
            if (line.Equals("C", StringComparison.OrdinalIgnoreCase) || line.Equals("CANCEL", StringComparison.OrdinalIgnoreCase))
                return null;
            if (int.TryParse(line, out int idx) && idx >= 1 && idx <= available.Count)
                return available[idx - 1];
        }
        return null; 
    }

    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables)
    {
        AllOutput.Add("combat_item_menu");
        _input?.ReadLine();
        return consumables.FirstOrDefault();
    }

    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
    {
        AllOutput.Add("equip_menu");
        if (_input != null)
        {
            var line = _input.ReadLine()?.Trim() ?? "";
            var available = equippable.ToList();
            if (int.TryParse(line, out int idx) && idx >= 1 && idx <= available.Count)
                return available[idx - 1];
        }
        return null;
    }

    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable)
    {
        AllOutput.Add("use_menu");
        if (_input != null)
        {
            var line = _input.ReadLine()?.Trim() ?? "";
            var available = usable.ToList();
            if (int.TryParse(line, out int idx) && idx >= 1 && idx <= available.Count)
                return available[idx - 1];
        }
        return null;
    }
    
    public void ShowCombatStart(Enemy enemy) { AllOutput.Add($"combat_start:{enemy.Name}"); }
    public void ShowCombatEntryFlags(Enemy enemy) { AllOutput.Add($"combat_flags:{enemy.Name}"); }
    public void ShowLevelUpChoice(Player player) { AllOutput.Add($"levelup_choice:{player.Level}"); }
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant) { AllOutput.Add($"floor_banner:{floor}/{maxFloor}"); }
    public void ShowEnemyDetail(Enemy enemy) { AllOutput.Add($"enemy_detail:{enemy.Name}"); }
    public void ShowVictory(Player player, int floorsCleared, RunStats stats) { AllOutput.Add($"victory:{player.Name}"); }
    public void ShowGameOver(Player player, string? killedBy, RunStats stats)
    {
        var msg = killedBy != null ? $"YOU HAVE FALLEN — defeated by {killedBy}" : "YOU HAVE FALLEN";
        Messages.Add(msg);
        AllOutput.Add($"gameover:{killedBy ?? "unknown"}");
    }

    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt != null && enemy.AsciiArt.Length > 0)
            AllOutput.Add($"enemy_art:{string.Join("|", enemy.AsciiArt)}");
    }
}
