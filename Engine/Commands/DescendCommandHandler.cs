namespace Dungnz.Engine.Commands;

using Dungnz.Models;
using Dungnz.Systems;

internal sealed class DescendCommandHandler : ICommandHandler
{
    private const int FinalFloor = DungeonGenerator.FinalFloor;

    public void Handle(string argument, CommandContext context)
    {
        if (!context.CurrentRoom.IsExit || context.CurrentRoom.Enemy != null)
        {
            context.TurnConsumed = false;
            context.Display.ShowError("You can only descend at a cleared exit room.");
            return;
        }

        if (context.CurrentFloor >= FinalFloor)
        {
            context.Stats.FinalLevel = context.Player.Level;
            context.Stats.TimeElapsed = DateTime.UtcNow - context.RunStart;
            context.Display.ShowVictory(context.Player, context.CurrentFloor, context.Stats);
            context.Display.ShowMessage($"Difficulty: {context.GetDifficultyName()}");
            if (context.Seed.HasValue) context.Display.ShowMessage($"Run seed: {context.Seed.Value}");
            context.Stats.Display(context.Display.ShowMessage);
            context.RecordRunEnd(true, null);
            context.GameOver = true;
            return;
        }

        context.CurrentFloor++;
        context.Stats.FloorsVisited = context.CurrentFloor;
        if (context.Player.TempAttackBonus > 0) { context.Player.ModifyAttack(-context.Player.TempAttackBonus); context.Player.TempAttackBonus = 0; }
        if (context.Player.TempDefenseBonus != 0) { context.Player.ModifyDefense(-context.Player.TempDefenseBonus); context.Player.TempDefenseBonus = 0; }
        context.Player.WardingVeilActive = false;
        foreach (var line in FloorTransitionNarration.GetSequence(context.CurrentFloor))
            context.Display.ShowMessage(line);
        context.Display.ShowMessage($"You descend deeper into the dungeon... Floor {context.CurrentFloor}");

        float floorMult = 1.0f + (context.CurrentFloor - 1) * 0.5f;
        var floorSeed = context.Seed.HasValue ? context.Seed.Value + context.CurrentFloor : (int?)null;
        var gen = new DungeonGenerator(floorSeed, context.AllItems);
        var (newStart, _) = gen.Generate(playerLevel: context.Player.Level, floorMultiplier: floorMult, difficulty: context.Difficulty, floor: context.CurrentFloor);
        context.CurrentRoom = newStart;
        context.CurrentRoom.Visited = true;
        var descendVariant = DungeonVariant.ForFloor(context.CurrentFloor);
        context.Display.ShowFloorBanner(context.CurrentFloor, FinalFloor, descendVariant);
        context.Display.ShowMessage(descendVariant.EntryMessage);
        context.Display.ShowRoom(context.CurrentRoom);
        context.Display.ShowMap(context.CurrentRoom, context.CurrentFloor);
    }
}
