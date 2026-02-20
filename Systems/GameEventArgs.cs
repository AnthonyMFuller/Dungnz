namespace Dungnz.Systems;

using Dungnz.Models;

public class CombatEndedEventArgs : EventArgs
{
    public Player Player { get; }
    public Enemy Enemy { get; }
    public CombatResult Result { get; }

    public CombatEndedEventArgs(Player player, Enemy enemy, CombatResult result)
    {
        Player = player;
        Enemy = enemy;
        Result = result;
    }
}

public class ItemPickedEventArgs : EventArgs
{
    public Player Player { get; }
    public Item Item { get; }
    public Room Room { get; }

    public ItemPickedEventArgs(Player player, Item item, Room room)
    {
        Player = player;
        Item = item;
        Room = room;
    }
}

public class LevelUpEventArgs : EventArgs
{
    public Player Player { get; }
    public int NewLevel { get; }
    public int OldLevel { get; }

    public LevelUpEventArgs(Player player, int oldLevel, int newLevel)
    {
        Player = player;
        OldLevel = oldLevel;
        NewLevel = newLevel;
    }
}

public class RoomEnteredEventArgs : EventArgs
{
    public Player Player { get; }
    public Room Room { get; }
    public Room? PreviousRoom { get; }

    public RoomEnteredEventArgs(Player player, Room room, Room? previousRoom)
    {
        Player = player;
        Room = room;
        PreviousRoom = previousRoom;
    }
}
