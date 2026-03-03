namespace Dungnz.Engine.Commands;

internal sealed class LookCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Display.ShowRoom(context.CurrentRoom);
    }
}
