using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Tests.Helpers;

public class FakeDisplayService : IDisplayService
{
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
    public void ShowCommandPrompt() { }
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

    public void ShowCombatStatus(Player player, Enemy enemy)
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

    public void ShowLootDrop(Item item)
    {
        AllOutput.Add($"loot:{item.Name}");
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
}
