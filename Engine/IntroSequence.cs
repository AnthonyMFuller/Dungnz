using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;

namespace Dungnz.Engine;

/// <summary>
/// Orchestrates the full game introduction sequence, gathering player configuration
/// (name, class, difficulty) and returning a configured Player ready to begin the dungeon run.
/// </summary>
public class IntroSequence
{
    private readonly IDisplayService _display;
    private readonly IInputReader _input;

    /// <summary>
    /// Constructs a new IntroSequence with the given display and input services.
    /// </summary>
    /// <param name="display">The display service for rendering the intro screens.</param>
    /// <param name="input">The input reader for player input (currently unused as display service handles input).</param>
    public IntroSequence(IDisplayService display, IInputReader input)
    {
        _display = display;
        _input = input;
    }

    /// <summary>
    /// Runs the complete intro flow: title → lore → prestige (if any) → name → class → difficulty.
    /// Returns a fully configured Player, the auto-generated run seed, and the chosen difficulty.
    /// </summary>
    public (Player player, int seed, Difficulty difficulty) Run(PrestigeData? prestige = null)
    {
        prestige ??= new PrestigeData();

        _display.ShowEnhancedTitle();
        _display.ShowIntroNarrative();

        if (prestige.PrestigeLevel > 0)
            _display.ShowPrestigeInfo(prestige);

        var name = _display.ReadPlayerName();
        var classDef = _display.SelectClass(prestige);
        var difficulty = _display.SelectDifficulty();

        var player = BuildPlayer(name, classDef, prestige);
        var seed = new Random().Next(100000, 999999);

        return (player, seed, difficulty);
    }

    private static Player BuildPlayer(string name, PlayerClassDefinition classDef, PrestigeData prestige)
    {
        var player = new Player { Name = name };
        player.Class = classDef.Class;
        player.Attack += classDef.BonusAttack;
        player.Defense = Math.Max(0, player.Defense + classDef.BonusDefense);
        player.MaxHP = Math.Max(1, player.MaxHP + classDef.BonusMaxHP);
        player.HP = player.MaxHP;
        player.MaxMana = Math.Max(0, player.MaxMana + classDef.BonusMaxMana);
        player.Mana = player.MaxMana;

        if (classDef.Class == PlayerClass.Rogue)
            player.ClassDodgeBonus = 0.10f;

        if (prestige.PrestigeLevel > 0)
        {
            player.Attack += prestige.BonusStartAttack;
            player.Defense += prestige.BonusStartDefense;
            player.MaxHP += prestige.BonusStartHP;
            player.HP = player.MaxHP;
        }

        return player;
    }
}
