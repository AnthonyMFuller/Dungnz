namespace Dungnz.Engine.Commands;

/// <summary>
/// Handles a single player command, mutating <see cref="CommandContext"/> to reflect the outcome.
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Executes the command with the given <paramref name="argument"/> against the provided <paramref name="context"/>.
    /// </summary>
    void Handle(string argument, CommandContext context);
}
