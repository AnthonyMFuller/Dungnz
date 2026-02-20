namespace Dungnz.Systems;

using Dungnz.Models;

public class GameEvents
{
    public event EventHandler<CombatEndedEventArgs>? OnCombatEnded;
    public event EventHandler<ItemPickedEventArgs>? OnItemPicked;
    public event EventHandler<LevelUpEventArgs>? OnLevelUp;
    public event EventHandler<RoomEnteredEventArgs>? OnRoomEntered;

    public void RaiseCombatEnded(Player player, Enemy enemy, CombatResult result)
    {
        OnCombatEnded?.Invoke(this, new CombatEndedEventArgs(player, enemy, result));
    }

    public void RaiseItemPicked(Player player, Item item, Room room)
    {
        OnItemPicked?.Invoke(this, new ItemPickedEventArgs(player, item, room));
    }

    public void RaiseLevelUp(Player player, int oldLevel, int newLevel)
    {
        OnLevelUp?.Invoke(this, new LevelUpEventArgs(player, oldLevel, newLevel));
    }

    public void RaiseRoomEntered(Player player, Room room, Room? previousRoom)
    {
        OnRoomEntered?.Invoke(this, new RoomEnteredEventArgs(player, room, previousRoom));
    }
}
