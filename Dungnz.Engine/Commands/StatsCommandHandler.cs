namespace Dungnz.Engine.Commands;

internal sealed class StatsCommandHandler : CommandHandlerBase
{
    private const int FinalFloor = DungeonGenerator.FinalFloor;

    protected override void HandleCore(string argument, CommandContext context)
    {
        context.Display.ShowPlayerStats(context.Player);
        context.Display.ShowMessage($"Floor: {context.CurrentFloor} / {FinalFloor}");
    }
}
