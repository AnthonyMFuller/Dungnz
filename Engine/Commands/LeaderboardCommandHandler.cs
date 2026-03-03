namespace Dungnz.Engine.Commands;

using Dungnz.Systems;

internal sealed class LeaderboardCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        context.Display.ShowMessage("=== TOP RUNS ===");
        var top = RunStats.GetTopRuns(5);
        if (top.Count == 0)
        {
            context.Display.ShowMessage("No completed runs yet. Be the first!");
            return;
        }
        for (int i = 0; i < top.Count; i++)
        {
            var r = top[i];
            var won = r.Won ? "✅" : "💀";
            context.Display.ShowMessage($"#{i + 1} {won} Level {r.FinalLevel} | {r.EnemiesDefeated} enemies | {r.GoldCollected}g");
        }
    }
}
