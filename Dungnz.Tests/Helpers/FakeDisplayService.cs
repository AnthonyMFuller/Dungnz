using Dungnz.Display;
using Dungnz.Models;

namespace Dungnz.Tests.Helpers;

public class FakeDisplayService : DisplayService
{
    public List<string> Messages { get; } = new();
    public List<string> Errors { get; } = new();
    public List<string> CombatMessages { get; } = new();
    public List<string> AllOutput { get; } = new();

    public override void ShowTitle() { }
    public override void ShowCommandPrompt() { }
    public override void ShowCombatPrompt() { }
    public override void ShowHelp() { AllOutput.Add("help"); }
    public override void ShowRoom(Room room) { AllOutput.Add($"room:{room.Description}"); }

    public override void ShowMessage(string message)
    {
        Messages.Add(message);
        AllOutput.Add(message);
    }

    public override void ShowError(string message)
    {
        Errors.Add(message);
        AllOutput.Add($"ERROR:{message}");
    }

    public override void ShowCombat(string message)
    {
        CombatMessages.Add(message);
        AllOutput.Add($"combat:{message}");
    }

    public override void ShowCombatMessage(string message)
    {
        CombatMessages.Add(message);
        AllOutput.Add($"combatmsg:{message}");
    }

    public override void ShowCombatStatus(Player player, Enemy enemy)
    {
        AllOutput.Add($"status:{player.HP}/{player.MaxHP} vs {enemy.HP}/{enemy.MaxHP}");
    }

    public override void ShowPlayerStats(Player player)
    {
        AllOutput.Add($"stats:{player.Name}");
    }

    public override void ShowInventory(Player player)
    {
        AllOutput.Add($"inventory:{player.Inventory.Count}");
    }

    public override void ShowLootDrop(Item item)
    {
        AllOutput.Add($"loot:{item.Name}");
    }
}
