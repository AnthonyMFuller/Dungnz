namespace Dungnz.Engine.Commands;

using Dungnz.Systems;
using Microsoft.Extensions.Logging;

internal sealed class SaveCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            context.TurnConsumed = false;
            context.Display.ShowError("Save as what? Usage: SAVE <name>");
            return;
        }
        SaveSystem.SaveGame(new GameState(context.Player, context.CurrentRoom, context.CurrentFloor, context.Seed, context.DifficultyLevel), argument);
        context.Logger.LogInformation("Game saved to {SaveFile}", argument);
        context.Display.ShowMessage($"Game saved as '{argument}'.");
    }
}
