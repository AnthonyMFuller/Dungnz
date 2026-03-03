namespace Dungnz.Engine.Commands;

using Dungnz.Systems;

internal sealed class ListSavesCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        var saves = SaveSystem.ListSaves();
        if (saves.Length == 0)
        {
            context.Display.ShowMessage("No saved games found.");
            return;
        }
        context.Display.ShowMessage("=== Saved Games ===");
        foreach (var s in saves)
            context.Display.ShowMessage($"  {s}");
    }
}
