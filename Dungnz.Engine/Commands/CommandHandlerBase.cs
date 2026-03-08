namespace Dungnz.Engine.Commands;

/// <summary>
/// Base class for command handlers that provides common functionality including
/// automatic room view refresh after execution. Subclasses implement HandleCore
/// and optionally override ShouldRefreshRoom to control the refresh behavior.
/// </summary>
public abstract class CommandHandlerBase : ICommandHandler
{
    /// <summary>
    /// Executes the command using template method pattern:
    /// calls HandleCore, then conditionally refreshes the room view.
    /// </summary>
    public void Handle(string argument, CommandContext context)
    {
        HandleCore(argument, context);
        
        if (ShouldRefreshRoom())
        {
            context.Display.ShowRoom(context.CurrentRoom);
        }
    }

    /// <summary>
    /// Core command logic to be implemented by subclasses.
    /// </summary>
    /// <param name="argument">The argument string provided with the command.</param>
    /// <param name="context">The command execution context.</param>
    protected abstract void HandleCore(string argument, CommandContext context);

    /// <summary>
    /// Determines whether ShowRoom should be called after HandleCore completes.
    /// Default is true. Override to return false for commands like SAVE, LEADERBOARD,
    /// or other informational commands that don't require a room refresh.
    /// </summary>
    /// <returns>True if ShowRoom should be called, false otherwise.</returns>
    protected virtual bool ShouldRefreshRoom() => true;
}
