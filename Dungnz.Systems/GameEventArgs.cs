namespace Dungnz.Systems;

using Dungnz.Models;

/// <summary>
/// Provides event data for the <see cref="GameEvents.OnCombatEnded"/> event,
/// carrying the player, the enemy, and the outcome of the finished encounter.
/// </summary>
public class CombatEndedEventArgs : EventArgs
{
    /// <summary>The player who participated in the combat.</summary>
    public Player Player { get; }

    /// <summary>The enemy that was fought in the encounter.</summary>
    public Enemy Enemy { get; }

    /// <summary>The result of the combat, indicating whether the player won, died, or fled.</summary>
    public CombatResult Result { get; }

    /// <summary>
    /// Initialises a new instance with the combat participants and outcome.
    /// </summary>
    /// <param name="player">The player involved in the combat.</param>
    /// <param name="enemy">The enemy involved in the combat.</param>
    /// <param name="result">The outcome of the combat.</param>
    public CombatEndedEventArgs(Player player, Enemy enemy, CombatResult result)
    {
        Player = player;
        Enemy = enemy;
        Result = result;
    }
}

/// <summary>
/// Provides event data for the <see cref="GameEvents.OnItemPicked"/> event,
/// carrying the player, the item collected, and the room it came from.
/// </summary>
public class ItemPickedEventArgs : EventArgs
{
    /// <summary>The player who picked up the item.</summary>
    public Player Player { get; }

    /// <summary>The item that was collected by the player.</summary>
    public Item Item { get; }

    /// <summary>The room from which the item was taken.</summary>
    public Room Room { get; }

    /// <summary>
    /// Initialises a new instance with the player, item, and source room.
    /// </summary>
    /// <param name="player">The player picking up the item.</param>
    /// <param name="item">The item being collected.</param>
    /// <param name="room">The room the item was found in.</param>
    public ItemPickedEventArgs(Player player, Item item, Room room)
    {
        Player = player;
        Item = item;
        Room = room;
    }
}

/// <summary>
/// Provides event data for the <see cref="GameEvents.OnLevelUp"/> event,
/// recording the player and the level transition that just occurred.
/// </summary>
public class LevelUpEventArgs : EventArgs
{
    /// <summary>The player whose level just increased.</summary>
    public Player Player { get; }

    /// <summary>The player's new level after the increase.</summary>
    public int NewLevel { get; }

    /// <summary>The player's level immediately before the increase.</summary>
    public int OldLevel { get; }

    /// <summary>
    /// Initialises a new instance with the player and both level values.
    /// </summary>
    /// <param name="player">The player who levelled up.</param>
    /// <param name="oldLevel">The level before the increase.</param>
    /// <param name="newLevel">The level after the increase.</param>
    public LevelUpEventArgs(Player player, int oldLevel, int newLevel)
    {
        Player = player;
        OldLevel = oldLevel;
        NewLevel = newLevel;
    }
}

/// <summary>
/// Provides event data for the <see cref="GameEvents.OnRoomEntered"/> event,
/// identifying the player, the room they moved into, and where they came from.
/// </summary>
public class RoomEnteredEventArgs : EventArgs
{
    /// <summary>The player who entered the room.</summary>
    public Player Player { get; }

    /// <summary>The room the player has just moved into.</summary>
    public Room Room { get; }

    /// <summary>
    /// The room the player was in before this move, or <see langword="null"/> if this is
    /// the first room entered at the start of a new game.
    /// </summary>
    public Room? PreviousRoom { get; }

    /// <summary>
    /// Initialises a new instance with the player, destination room, and optional origin room.
    /// </summary>
    /// <param name="player">The player who moved.</param>
    /// <param name="room">The room being entered.</param>
    /// <param name="previousRoom">The room the player came from, or <see langword="null"/> at game start.</param>
    public RoomEnteredEventArgs(Player player, Room room, Room? previousRoom)
    {
        Player = player;
        Room = room;
        PreviousRoom = previousRoom;
    }
}

/// <summary>Provides event data for the <see cref="GameEvents.OnAchievementUnlocked"/> event.</summary>
public class AchievementUnlockedEventArgs : EventArgs
{
    /// <summary>
    /// Initialises a new instance with the achievement name and description.
    /// </summary>
    /// <param name="achievementName">The name of the achievement.</param>
    /// <param name="achievementDescription">The description of the achievement.</param>
    public AchievementUnlockedEventArgs(string achievementName, string achievementDescription)
    {
        Name = achievementName;
        Description = achievementDescription;
    }
    /// <summary>The name of the unlocked achievement.</summary>
    public string Name { get; }
    /// <summary>The description of the unlocked achievement.</summary>
    public string Description { get; }
}
