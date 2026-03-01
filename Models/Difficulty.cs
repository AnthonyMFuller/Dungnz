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
    /// Gets the multiplier applied to player outgoing damage.
    /// Values above 1.0 make the player hit harder; values below 1.0 reduce player damage.
    /// </summary>
    public float PlayerDamageMultiplier { get; init; }

    /// <summary>
    /// Gets the multiplier applied to enemy outgoing damage.
    /// Values below 1.0 make enemies hit softer; values above 1.0 increase enemy damage.
    /// </summary>
    public float EnemyDamageMultiplier { get; init; }

    /// <summary>
    /// Gets the multiplier applied to all healing effects received by the player.
    /// Values above 1.0 improve healing effectiveness.
    /// </summary>
    public float HealingMultiplier { get; init; }

    /// <summary>
    /// Gets the multiplier applied to merchant buy prices.
    /// Values below 1.0 make items cheaper; values above 1.0 make them more expensive.
    /// </summary>
    public float MerchantPriceMultiplier { get; init; }

    /// <summary>
    /// Gets the multiplier applied to experience points gained from defeating enemies.
    /// Values above 1.0 increase XP gains independently of enemy stat scaling.
    /// </summary>
    public float XPMultiplier { get; init; }

    /// <summary>
    /// Gets the amount of gold the player begins the game with.
    /// </summary>
    public int StartingGold { get; init; }

    /// <summary>
    /// Gets the number of free Health Potions the player receives at game start.
    /// </summary>
    public int StartingPotions { get; init; }

    /// <summary>
    /// Gets the multiplier applied to shrine spawn rates in the dungeon.
    /// The base shrine spawn rate is 15%, and values above 1.0 increase spawn frequency.
    /// </summary>
    public float ShrineSpawnMultiplier { get; init; }

    /// <summary>
    /// Gets the multiplier applied to merchant spawn rates in the dungeon.
    /// The base merchant spawn rate is 20%, and values above 1.0 increase spawn frequency.
    /// </summary>
    public float MerchantSpawnMultiplier { get; init; }

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
        Difficulty.Casual => new()
        {
            EnemyStatMultiplier = 0.65f,
            EnemyDamageMultiplier = 0.70f,
            PlayerDamageMultiplier = 1.20f,
            LootDropMultiplier = 1.60f,
            GoldMultiplier = 1.80f,
            HealingMultiplier = 1.50f,
            MerchantPriceMultiplier = 0.65f,
            XPMultiplier = 1.40f,
            StartingGold = 50,
            StartingPotions = 3,
            ShrineSpawnMultiplier = 1.50f,
            MerchantSpawnMultiplier = 1.40f,
            Permadeath = false
        },
        Difficulty.Hard => new()
        {
            EnemyStatMultiplier = 1.35f,
            EnemyDamageMultiplier = 1.25f,
            PlayerDamageMultiplier = 0.90f,
            LootDropMultiplier = 0.65f,
            GoldMultiplier = 0.60f,
            HealingMultiplier = 0.75f,
            MerchantPriceMultiplier = 1.40f,
            XPMultiplier = 0.80f,
            StartingGold = 0,
            StartingPotions = 0,
            ShrineSpawnMultiplier = 0.70f,
            MerchantSpawnMultiplier = 0.70f,
            Permadeath = true
        },
        _ => new()
        {
            EnemyStatMultiplier = 1.00f,
            EnemyDamageMultiplier = 1.00f,
            PlayerDamageMultiplier = 1.00f,
            LootDropMultiplier = 1.00f,
            GoldMultiplier = 1.00f,
            HealingMultiplier = 1.00f,
            MerchantPriceMultiplier = 1.00f,
            XPMultiplier = 1.00f,
            StartingGold = 15,
            StartingPotions = 1,
            ShrineSpawnMultiplier = 1.00f,
            MerchantSpawnMultiplier = 1.00f,
            Permadeath = false
        }
    };
}
