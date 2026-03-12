namespace Dungnz.Engine.Commands;

/// <summary>
/// Handles the HISTORY command — renders the full combat log scrollback to the Content panel.
/// </summary>
internal sealed class HistoryCommandHandler : CommandHandlerBase
{
    protected override void HandleCore(string argument, CommandContext context)
    {
        context.Display.ShowCombatHistory();
    }
}
