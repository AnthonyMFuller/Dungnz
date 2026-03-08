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
        context.Display.ShowMap(context.CurrentRoom, context.CurrentFloor);
    }
}
