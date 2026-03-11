namespace Dungnz.Engine.Commands;

using Dungnz.Models;

internal sealed class StatsCommandHandler : CommandHandlerBase
{
    protected override void HandleCore(string argument, CommandContext context)
    {
        context.Display.ShowPlayerStats(context.Player);
        context.Display.ShowMessage($"Floor: {context.CurrentFloor} / {GameConstants.FinalFloor}");
    }
}
