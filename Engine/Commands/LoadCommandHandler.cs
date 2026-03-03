namespace Dungnz.Engine.Commands;

using Dungnz.Systems;
using Microsoft.Extensions.Logging;

internal sealed class LoadCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            context.TurnConsumed = false;
            context.Display.ShowError("Load which save? Usage: LOAD <name>");
            return;
        }
        try
        {
            var state = SaveSystem.LoadGame(argument);
            context.Player = state.Player;
            context.CurrentRoom = state.CurrentRoom;
            context.CurrentFloor = state.CurrentFloor;
            context.Seed = state.Seed;
            context.RunStart = DateTime.UtcNow;
            context.Rng = context.Seed.HasValue ? new Random(context.Seed.Value) : new Random();
            context.Stats = new RunStats();
            context.SessionStats = new SessionStats();
            context.Display.ShowMessage($"Loaded save '{argument}'.");
            context.Logger.LogInformation("Game loaded from {SaveFile}", argument);
            context.Display.ShowRoom(context.CurrentRoom);
        }
        catch (FileNotFoundException)
        {
            context.TurnConsumed = false;
            context.Display.ShowError($"Save '{argument}' not found.");
        }
        catch (Exception ex)
        {
            context.TurnConsumed = false;
            context.Logger.LogError(ex, "Failed to load save '{SaveFile}'", argument);
            context.Display.ShowError($"Failed to load save: {ex.Message}");
        }
    }
}
