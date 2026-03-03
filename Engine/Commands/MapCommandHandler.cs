namespace Dungnz.Engine.Commands;

internal sealed class MapCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Display.ShowMap(context.CurrentRoom, context.CurrentFloor);
    }
}
