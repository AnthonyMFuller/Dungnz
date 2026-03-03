namespace Dungnz.Engine.Commands;

using Dungnz.Systems;

internal sealed class PrestigeCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        var data = PrestigeSystem.Load();
        context.Display.ShowMessage("=== PRESTIGE STATUS ===");
        context.Display.ShowMessage($"Prestige Level: {data.PrestigeLevel}");
        context.Display.ShowMessage($"Total Wins: {data.TotalWins} | Total Runs: {data.TotalRuns}");
        if (data.PrestigeLevel > 0)
            context.Display.ShowMessage($"Bonuses: +{data.BonusStartAttack} Attack, +{data.BonusStartDefense} Defense, +{data.BonusStartHP} Max HP");
        else
            context.Display.ShowMessage("Win 3 runs to earn your first Prestige level!");
    }
}
