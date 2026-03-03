namespace Dungnz.Engine.Commands;

internal sealed class StatsCommandHandler : ICommandHandler
{
    private const int FinalFloor = 8;

    public void Handle(string argument, CommandContext context)
    {
        context.Display.ShowPlayerStats(context.Player);
        context.Display.ShowMessage($"Floor: {context.CurrentFloor} / {FinalFloor}");
    }
}
