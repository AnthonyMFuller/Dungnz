using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Engine;

/// <summary>
/// Discriminated union representing the outcome of the startup menu flow.
/// Program.cs pattern-matches on these to decide how to launch the game.
/// </summary>
public abstract record StartupResult
{
    private StartupResult() { }

    /// <summary>Player completed intro sequence (new game or new game with seed).</summary>
    public sealed record NewGame(
        Player Player,
        int Seed,
        Difficulty Difficulty
    ) : StartupResult;

    /// <summary>Player selected an existing save to resume.</summary>
    public sealed record LoadedGame(
        GameState State
    ) : StartupResult;

    /// <summary>Player chose to exit the application.</summary>
    public sealed record ExitGame : StartupResult;
}
