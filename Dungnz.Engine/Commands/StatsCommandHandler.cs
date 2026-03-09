namespace Dungnz.Engine.Commands;

internal sealed class StatsCommandHandler : CommandHandlerBase
{
    protected override void HandleCore(string argument, CommandContext context)
    {
        context.Display.ShowPlayerStats(context.Player);
        context.Display.ShowMessage($"Floor: {context.CurrentFloor} / {DungeonGenerator.FinalFloor}");
    }
}
