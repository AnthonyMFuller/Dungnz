namespace Dungnz.Engine.Commands;

/// <summary>
/// Handles the "back" command, returning the player to the previous room they came from.
/// </summary>
internal sealed class BackCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (context.PreviousRoom == null)
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You can't go back from here.");
            return;
        }

        // Store current room as the new previous room before moving
        var temp = context.CurrentRoom;
        context.CurrentRoom = context.PreviousRoom;
        context.PreviousRoom = temp;

        // Mark the room as visited
        context.CurrentRoom.Visited = true;

        // Update room state for revisited rooms
        if (context.CurrentRoom.State == Models.RoomState.Fresh)
        {
            context.CurrentRoom.State = Models.RoomState.Revisited;
        }

        // Display the room
        context.Display.ShowRoom(context.CurrentRoom);
        
        // Display dynamic room entry narration based on room state
        var narrationState = DetermineRoomNarrationState(context.CurrentRoom);
        var narrationLine = context.Narration.GetRoomEntryNarration(narrationState);
        if (!string.IsNullOrEmpty(narrationLine))
        {
            context.Display.ShowMessage(narrationLine);
        }
        
        context.Display.ShowMap(context.CurrentRoom, context.CurrentFloor);
    }

    private static Systems.RoomNarrationState DetermineRoomNarrationState(Models.Room room)
    {
        // Priority order: Merchant > Shrine > Boss > Cleared > ActiveEnemies > FirstVisit
        
        if (room.Merchant != null)
            return Systems.RoomNarrationState.Merchant;
        
        if (room.HasShrine)
            return Systems.RoomNarrationState.Shrine;
        
        // Check if this is a boss room
        if (room.Enemy != null && IsBossEnemy(room.Enemy))
            return Systems.RoomNarrationState.Boss;
        
        // Check if room is cleared
        if (room.IsCleared)
            return Systems.RoomNarrationState.Cleared;
        
        // Check for active enemies
        if (room.Enemy != null && !room.Enemy.IsDead)
        {
            return room.WasVisited ? Systems.RoomNarrationState.ActiveEnemies : Systems.RoomNarrationState.FirstVisit;
        }
        
        // Default to FirstVisit for fresh rooms
        return room.WasVisited ? Systems.RoomNarrationState.ActiveEnemies : Systems.RoomNarrationState.FirstVisit;
    }

    private static bool IsBossEnemy(Models.Enemy enemy)
    {
        var enemyType = enemy.GetType();
        return enemyType.Name is "DungeonBoss" 
            or "ArchlichSovereign" 
            or "AbyssalLeviathan" 
            or "InfernalDragon";
    }
}
