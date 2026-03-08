namespace Dungnz.Engine.Commands;

internal sealed class HelpCommandHandler : CommandHandlerBase
{
    protected override void HandleCore(string argument, CommandContext context)
    {
        context.Display.ShowHelp();
    }
}
