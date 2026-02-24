using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Tests.Helpers;

public class TestDisplayService : IDisplayService
{
    public List<string> Messages { get; } = new();
    public List<string> Errors { get; } = new();
    public List<string> CombatMessages { get; } = new();
    public List<string> AllOutput { get; } = new();

    /// <summary>
    /// Strips ANSI color codes from text to ensure tests check plain text content.
    /// </summary>
    private static string StripAnsi(string text) => ColorCodes.StripAnsiCodes(text);

    public void ShowTitle() { }
    public void ShowCommandPrompt(Player? player = null) { }
    public void ShowHelp() => AllOutput.Add("help");
    public void ShowRoom(Room room) => AllOutput.Add($"room:{room.Description}");
    public void ShowMap(Room room) { }

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

    public void ShowGoldPickup(int amount, int newTotal) { }
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax) { }
    public void ShowItemDetail(Item item) { }

    public string ReadPlayerName()
    {
        return "TestPlayer";
    }

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
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold) { AllOutput.Add($"sell:{playerGold}g"); }
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients) { AllOutput.Add($"recipe:{recipeName}"); }
    
    public void ShowCombatStart(Enemy enemy) { AllOutput.Add($"combat_start:{enemy.Name}"); }
    public void ShowCombatEntryFlags(Enemy enemy) { AllOutput.Add($"combat_flags:{enemy.Name}"); }
    public void ShowLevelUpChoice(Player player) { AllOutput.Add($"levelup_choice:{player.Level}"); }
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant) { AllOutput.Add($"floor_banner:{floor}/{maxFloor}"); }
    public void ShowEnemyDetail(Enemy enemy) { AllOutput.Add($"enemy_detail:{enemy.Name}"); }
    public void ShowVictory(Player player, int floorsCleared, RunStats stats) { AllOutput.Add($"victory:{player.Name}"); }
    public void ShowGameOver(Player player, string? killedBy, RunStats stats) { AllOutput.Add($"gameover:{killedBy ?? "unknown"}"); }
    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt != null && enemy.AsciiArt.Length > 0)
            AllOutput.Add($"enemy_art:{string.Join("|", enemy.AsciiArt)}");
    }
}
