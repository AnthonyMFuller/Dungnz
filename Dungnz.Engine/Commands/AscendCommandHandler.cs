namespace Dungnz.Engine.Commands;

using Dungnz.Models;
using Dungnz.Systems;

internal sealed class AscendCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (!context.CurrentRoom.IsEntrance)
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You can only ascend from the entrance of this floor.");
            return;
        }

        if (context.CurrentFloor <= 1)
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You are already on the first floor. There is nowhere higher to go.");
            return;
        }

        if (!context.FloorHistory.TryGetValue(context.CurrentFloor - 1, out var previousEntrance))
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You cannot find a way to ascend.");
            return;
        }

        if (context.Player.TempAttackBonus > 0) { context.Player.ModifyAttack(-context.Player.TempAttackBonus); context.Player.TempAttackBonus = 0; }
        if (context.Player.TempDefenseBonus != 0) { context.Player.ModifyDefense(-context.Player.TempDefenseBonus); context.Player.TempDefenseBonus = 0; }
        context.Player.WardingVeilActive = false;

        context.CurrentFloor--;
        context.FloorEntranceRoom = previousEntrance;
        context.CurrentRoom = previousEntrance;
        context.PreviousRoom = null; // Can't go back across floors

        foreach (var line in FloorTransitionNarration.GetAscendSequence(context.CurrentFloor))
            context.Display.ShowMessage(line);
        context.Display.ShowMessage($"You ascend back to floor {context.CurrentFloor}.");

        var ascendVariant = DungeonVariant.ForFloor(context.CurrentFloor);
        context.Display.ShowFloorBanner(context.CurrentFloor, DungeonGenerator.FinalFloor, ascendVariant);
        context.Display.ShowRoom(context.CurrentRoom);
        context.Display.ShowMap(context.CurrentRoom, context.CurrentFloor);
    }
}
