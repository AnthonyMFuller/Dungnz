namespace Dungnz.Engine.Commands;

internal sealed class MapCommandHandler : CommandHandlerBase
{
    protected override void HandleCore(string argument, CommandContext context)
    {
        context.Display.ShowMap(context.CurrentRoom, context.CurrentFloor);
    }
}
