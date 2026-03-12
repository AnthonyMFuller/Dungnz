namespace Dungnz.Engine.Commands;

using Dungnz.Systems;

/// <summary>
/// Handles the <c>RETURN</c> command, fast-travelling the player to the entrance room
/// of the current floor without requiring manual backtracking through every room.
/// </summary>
/// <remarks>
/// The command is blocked while an enemy is alive in the current room.
/// If the player is already at the floor entrance the command is a no-op.
/// Otherwise the player is teleported to <see cref="CommandContext.FloorEntranceRoom"/>
/// and the room is redisplayed as though the player had just walked in.
/// </remarks>
internal sealed class ReturnCommandHandler : ICommandHandler
{
    /// <inheritdoc/>
    public void Handle(string argument, CommandContext context)
    {
        // Block during active combat
        if (context.CurrentRoom.Enemy is { IsDead: false })
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You cannot retreat while in combat.");
            return;
        }

        // Guard: no tracked entrance (shouldn't happen during normal play)
        if (context.FloorEntranceRoom == null)
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You cannot find the floor entrance from here.");
            return;
        }

        // Already at the entrance
        if (context.CurrentRoom == context.FloorEntranceRoom)
        {
            context.TurnConsumed = false;
            context.Display.ShowMessage("You are already at the floor entrance.");
            return;
        }

        // Teleport to floor entrance
        context.PreviousRoom = context.CurrentRoom;
        context.CurrentRoom = context.FloorEntranceRoom;
        context.CurrentRoom.Visited = true;

        if (context.CurrentRoom.State == Models.RoomState.Fresh)
            context.CurrentRoom.State = Models.RoomState.Revisited;

        context.Display.ShowMessage("You retrace your steps to the floor entrance.");
        context.Display.ShowRoom(context.CurrentRoom);

        var narrationState = DetermineRoomNarrationState(context.CurrentRoom);
        var narrationLine = context.Narration.GetRoomEntryNarration(narrationState);
        if (!string.IsNullOrEmpty(narrationLine))
            context.Display.ShowMessage(narrationLine);

        context.Display.ShowMap(context.CurrentRoom, context.CurrentFloor);
    }

    private static RoomNarrationState DetermineRoomNarrationState(Models.Room room)
    {
        if (room.Merchant != null)
            return RoomNarrationState.Merchant;

        if (room.HasShrine)
            return RoomNarrationState.Shrine;

        if (room.Enemy != null && IsBossEnemy(room.Enemy))
            return RoomNarrationState.Boss;

        if (room.IsCleared)
            return RoomNarrationState.Cleared;

        if (room.Enemy != null && !room.Enemy.IsDead)
            return room.WasVisited ? RoomNarrationState.ActiveEnemies : RoomNarrationState.FirstVisit;

        return room.WasVisited ? RoomNarrationState.Cleared : RoomNarrationState.FirstVisit;
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
