namespace Dungnz.Engine.Commands;

internal sealed class HelpCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Display.ShowHelp();
    }
}
