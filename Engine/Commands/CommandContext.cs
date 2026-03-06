namespace Dungnz.Engine.Commands;

using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;

/// <summary>
/// Carries all mutable game state and service references needed by
/// <see cref="ICommandHandler"/> implementations during a single command dispatch.
/// </summary>
public class CommandContext
{
    /// <summary>The active player character.</summary>
    public required Player Player { get; set; }
    /// <summary>The room the player currently occupies.</summary>
    public required Room CurrentRoom { get; set; }
    /// <summary>The random-number generator for the current run.</summary>
    public required Random Rng { get; set; }
    /// <summary>Statistics accumulated during the current run.</summary>
    public required RunStats Stats { get; set; }
    /// <summary>Balance-tracking statistics for the current session.</summary>
    public required SessionStats SessionStats { get; set; }
    /// <summary>UTC timestamp at which the current run began.</summary>
    public required DateTime RunStart { get; set; }
    /// <summary>The display service used to render all game output.</summary>
    public required IDisplayService Display { get; set; }
    /// <summary>The combat engine invoked for enemy encounters.</summary>
    public required ICombatEngine Combat { get; set; }
    /// <summary>Manages equipping and unequipping items.</summary>
    public required EquipmentManager Equipment { get; set; }
    /// <summary>Manages adding items to and removing them from the player inventory.</summary>
    public required InventoryManager InventoryManager { get; set; }
    /// <summary>Provides atmospheric narrative text picks.</summary>
    public required NarrationService Narration { get; set; }
    /// <summary>Evaluates and tracks achievement unlocks.</summary>
    public required AchievementSystem Achievements { get; set; }
    /// <summary>The full item catalog used for shop and loot generation.</summary>
    public required IReadOnlyList<Item> AllItems { get; set; }
    /// <summary>Difficulty multipliers for the current run.</summary>
    public required DifficultySettings Difficulty { get; set; }
    /// <summary>The selected difficulty level enum for the current run.</summary>
    public Difficulty DifficultyLevel { get; set; } = Models.Difficulty.Normal;
    /// <summary>Structured logger for game events.</summary>
    public required ILogger Logger { get; set; }
    /// <summary>Optional event bus for broadcasting game events.</summary>
    public GameEvents? Events { get; set; }
    /// <summary>Optional menu navigator override.</summary>
    public IMenuNavigator? Navigator { get; set; }
    /// <summary>Optional RNG seed used for reproducible dungeon layouts.</summary>
    public int? Seed { get; set; }
    /// <summary>The floor the player is currently on.</summary>
    public int CurrentFloor { get; set; }

    /// <summary>Maps floor number → entrance room of that floor, enabling ascension back to previous floors.</summary>
    public Dictionary<int, Room> FloorHistory { get; set; } = new();

    /// <summary>The entrance room of the current floor (grid[0,0] equivalent). Set whenever a new floor is entered.</summary>
    public Room? FloorEntranceRoom { get; set; }

    /// <summary>Set to <see langword="false"/> by a handler when its action should not count as a turn.</summary>
    public bool TurnConsumed { get; set; }
    /// <summary>Set to <see langword="true"/> by a handler when the run has ended.</summary>
    public bool GameOver { get; set; }

    /// <summary>Delegate that ends the run due to the player's death (cause is the killer name).</summary>
    public required Action<string> ExitRun { get; set; }
    /// <summary>Delegate that records the run outcome to history and evaluates achievements.</summary>
    public required Action<bool, string?> RecordRunEnd { get; set; }
    /// <summary>Returns the item currently equipped in the slot that the candidate item would occupy.</summary>
    public required Func<Player, Item?, Item?> GetCurrentlyEquippedForItem { get; set; }
    /// <summary>Returns a display-friendly label for the current difficulty setting.</summary>
    public required Func<string> GetDifficultyName { get; set; }

    /// <summary>Invokes the shrine interaction logic owned by <see cref="GameLoop"/>.</summary>
    public required Action HandleShrine { get; set; }
    /// <summary>Invokes the contested armory interaction logic owned by <see cref="GameLoop"/>.</summary>
    public required Action HandleContestedArmory { get; set; }
    /// <summary>Invokes the petrified library event logic owned by <see cref="GameLoop"/>.</summary>
    public required Action HandlePetrifiedLibrary { get; set; }
    /// <summary>Invokes the trap room event logic owned by <see cref="GameLoop"/>.</summary>
    public required Action HandleTrapRoom { get; set; }
}
