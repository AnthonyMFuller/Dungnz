using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Engine;

/// <summary>
/// Coordinates the pre-game startup menu flow. Asks the player what they want
/// to do (new game, load, seed, exit) and returns a StartupResult that Program.cs
/// uses to decide how to initialise the game loop.
/// </summary>
public class StartupOrchestrator
{
    private readonly IDisplayService _display;
    private readonly IInputReader _input;
    private readonly PrestigeData _prestige;

    /// <summary>
    /// Constructs a new StartupOrchestrator with the given display, input, and prestige data.
    /// </summary>
    public StartupOrchestrator(IDisplayService display, IInputReader input, PrestigeData prestige)
    {
        _display = display;
        _input = input;
        _prestige = prestige;
    }

    /// <summary>
    /// Runs the startup menu loop. Returns when the player has made a valid
    /// selection that produces a StartupResult. May loop internally if the
    /// player cancels out of a sub-menu (e.g. backs out of save list).
    /// </summary>
    public StartupResult Run()
    {
        _display.ShowEnhancedTitle();

        while (true)
        {
            var saves = SaveSystem.ListSaves();
            var choice = _display.ShowStartupMenu(hasSaves: saves.Length > 0);

            switch (choice)
            {
                case StartupMenuOption.NewGame:
                    return RunNewGame(seed: null);

                case StartupMenuOption.LoadSave:
                    var result = RunLoadSave(saves);
                    if (result != null) return result;
                    break;

                case StartupMenuOption.NewGameWithSeed:
                    var seedResult = RunNewGameWithSeed();
                    if (seedResult != null) return seedResult;
                    break;

                case StartupMenuOption.Exit:
                    return new StartupResult.ExitGame();
            }
        }
    }

    private StartupResult.NewGame RunNewGame(int? seed)
    {
        var intro = new IntroSequence(_display, _input);
        var (player, actualSeed, difficulty) = intro.Run(_prestige, showTitle: false);

        if (seed.HasValue)
            actualSeed = seed.Value;

        return new StartupResult.NewGame(player, actualSeed, difficulty);
    }

    private StartupResult.LoadedGame? RunLoadSave(string[] saves)
    {
        var selected = _display.SelectSaveToLoad(saves);
        if (selected == null) return null;

        try
        {
            var state = SaveSystem.LoadGame(selected);
            return new StartupResult.LoadedGame(state);
        }
        catch (Exception ex)
        {
            _display.ShowError(ex.Message);
            return null;
        }
    }

    private StartupResult.NewGame? RunNewGameWithSeed()
    {
        var seed = _display.ReadSeed();
        if (seed == null) return null;

        return RunNewGame(seed: seed.Value);
    }
}
