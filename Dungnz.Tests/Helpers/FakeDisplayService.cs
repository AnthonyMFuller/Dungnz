using Dungnz.Display;
using Dungnz.Models;

namespace Dungnz.Tests.Helpers;

public class FakeDisplayService : IDisplayService
{
    public List<string> Messages { get; } = new();
    public List<string> Errors { get; } = new();
    public List<string> CombatMessages { get; } = new();
    public List<string> AllOutput { get; } = new();

    public void ShowTitle() { }
    public void ShowCommandPrompt() { }
    public void ShowCombatPrompt() { }
    public void ShowHelp() { AllOutput.Add("help"); }
    public void ShowRoom(Room room) { AllOutput.Add($"room:{room.Description}"); }
    public void ShowMap(Room room) { AllOutput.Add($"map:{room.Description}"); }
    public string ReadPlayerName() => "TestPlayer";

    public void ShowMessage(string message)
    {
        Messages.Add(message);
        AllOutput.Add(message);
    }

    public void ShowError(string message)
    {
        Errors.Add(message);
        AllOutput.Add($"ERROR:{message}");
    }

    public void ShowCombat(string message)
    {
        CombatMessages.Add(message);
        AllOutput.Add($"combat:{message}");
    }

    public void ShowCombatMessage(string message)
    {
        CombatMessages.Add(message);
        AllOutput.Add($"combatmsg:{message}");
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
}
