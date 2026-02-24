namespace Dungnz.Systems;

using Dungnz.Models;

/// <summary>
/// Central event hub that broadcasts significant game occurrences — combat outcomes, item pickups,
/// level-ups, and room transitions — to any registered subscribers. Decouples game systems
/// from each other by letting them react to events without direct references.
/// </summary>
public class GameEvents
{
    /// <summary>Raised when a combat encounter ends, carrying the player, enemy, and combat outcome.</summary>
    public event EventHandler<CombatEndedEventArgs>? OnCombatEnded;

    /// <summary>Raised when the player picks up an item from a room.</summary>
    public event EventHandler<ItemPickedEventArgs>? OnItemPicked;

    /// <summary>Raised when the player gains enough experience to advance to a new level.</summary>
    public event EventHandler<LevelUpEventArgs>? OnLevelUp;

    /// <summary>Raised each time the player moves into a new room.</summary>
    public event EventHandler<RoomEnteredEventArgs>? OnRoomEntered;

    /// <summary>Raised when the player unlocks an achievement during a run.</summary>
    public event EventHandler<AchievementUnlockedEventArgs>? OnAchievementUnlocked;

    /// <summary>
    /// Fires the <see cref="OnCombatEnded"/> event with the provided combat participants and result.
    /// </summary>
    /// <param name="player">The player who participated in the combat.</param>
    /// <param name="enemy">The enemy that was fought.</param>
    /// <param name="result">The outcome of the combat encounter.</param>
    public void RaiseCombatEnded(Player player, Enemy enemy, CombatResult result)
    {
        OnCombatEnded?.Invoke(this, new CombatEndedEventArgs(player, enemy, result));
    }

    /// <summary>
    /// Fires the <see cref="OnItemPicked"/> event when the player collects an item.
    /// </summary>
    /// <param name="player">The player who picked up the item.</param>
    /// <param name="item">The item that was collected.</param>
    /// <param name="room">The room from which the item was taken.</param>
    public void RaiseItemPicked(Player player, Item item, Room room)
    {
        OnItemPicked?.Invoke(this, new ItemPickedEventArgs(player, item, room));
    }

    /// <summary>
    /// Fires the <see cref="OnLevelUp"/> event when the player's level increases.
    /// </summary>
    /// <param name="player">The player who levelled up.</param>
    /// <param name="oldLevel">The player's level before the increase.</param>
    /// <param name="newLevel">The player's level after the increase.</param>
    public void RaiseLevelUp(Player player, int oldLevel, int newLevel)
    {
        OnLevelUp?.Invoke(this, new LevelUpEventArgs(player, oldLevel, newLevel));
    }

    /// <summary>
    /// Fires the <see cref="OnRoomEntered"/> event when the player moves into a room.
    /// </summary>
    /// <param name="player">The player who entered the room.</param>
    /// <param name="room">The room that was entered.</param>
    /// <param name="previousRoom">The room the player came from, or <see langword="null"/> at the start of the game.</param>
    public void RaiseRoomEntered(Player player, Room room, Room? previousRoom)
    {
        OnRoomEntered?.Invoke(this, new RoomEnteredEventArgs(player, room, previousRoom));
    }

    /// <summary>
    /// Fires the <see cref="OnAchievementUnlocked"/> event when an achievement is unlocked.
    /// </summary>
    /// <param name="name">The name of the achievement.</param>
    /// <param name="description">The description of the achievement.</param>
    public void RaiseAchievementUnlocked(string name, string description)
    {
        OnAchievementUnlocked?.Invoke(this, new AchievementUnlockedEventArgs(name, description));
    }
}
