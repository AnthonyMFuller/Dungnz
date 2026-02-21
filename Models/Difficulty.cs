namespace Dungnz.Models;

/// <summary>
/// Represents the three selectable difficulty levels offered to the player at game start.
/// </summary>
public enum Difficulty
{
    /// <summary>Reduced enemy power with improved loot and gold rewards, suitable for new players.</summary>
    Casual,

    /// <summary>Balanced stats representing the intended base experience.</summary>
    Normal,

    /// <summary>Stronger enemies with diminished loot and gold rewards for a greater challenge.</summary>
    Hard
}

/// <summary>
/// Holds the numeric multipliers and flags that describe how a chosen <see cref="Difficulty"/>
/// modifies the generated dungeon and its rewards.
/// </summary>
public class DifficultySettings
{
    /// <summary>
    /// Gets the multiplier applied to enemy stat scaling when creating enemies via
    /// <c>EnemyFactory.CreateScaled</c>. Values below 1.0 make enemies weaker;
    /// values above 1.0 make them stronger.
    /// </summary>
    public float EnemyStatMultiplier { get; init; }

    /// <summary>
    /// Gets the multiplier applied to item-drop rates from enemy loot tables.
    /// Values above 1.0 increase drop frequency.
    /// </summary>
    public float LootDropMultiplier { get; init; }

    /// <summary>
    /// Gets the multiplier applied to gold amounts awarded after combat.
    /// Values above 1.0 yield more gold per kill.
    /// </summary>
    public float GoldMultiplier { get; init; }

    /// <summary>
    /// Gets whether the run uses permadeath rules (death ends the game with no retry).
    /// </summary>
    public bool Permadeath { get; init; }

    /// <summary>
    /// Returns the pre-configured <see cref="DifficultySettings"/> for the given
    /// <paramref name="difficulty"/> level.
    /// </summary>
    /// <param name="difficulty">The difficulty level to retrieve settings for.</param>
    /// <returns>
    /// A <see cref="DifficultySettings"/> instance populated with the multipliers
    /// appropriate for <paramref name="difficulty"/>.
    /// </returns>
    public static DifficultySettings For(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Casual => new() { EnemyStatMultiplier = 0.7f, LootDropMultiplier = 1.5f, GoldMultiplier = 1.5f },
        Difficulty.Hard   => new() { EnemyStatMultiplier = 1.3f, LootDropMultiplier = 0.7f, GoldMultiplier = 0.7f },
        _                 => new() { EnemyStatMultiplier = 1.0f, LootDropMultiplier = 1.0f, GoldMultiplier = 1.0f }
    };
}
