namespace Dungnz.Engine.Commands;

using Dungnz.Models;

internal sealed class SkillsCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        var skillToLearn = context.Display.ShowSkillTreeMenu(context.Player);
        if (skillToLearn.HasValue)
        {
            HandleLearnSpecificSkill(skillToLearn.Value, context);
        }
        else
        {
            context.Display.ShowRoom(context.CurrentRoom);
        }
        context.TurnConsumed = false;
    }

    internal void HandleLearnSpecificSkill(Skill skill, CommandContext context)
    {
        var success = context.Player.Skills.TryUnlock(context.Player, skill);
        if (success)
            context.Display.ShowMessage($"You learned {skill}!");
        else
            context.Display.ShowMessage($"Cannot learn {skill} right now.");
        context.TurnConsumed = false;
    }
}

internal sealed class LearnCommandHandler : ICommandHandler
{
    public void Handle(string argument, CommandContext context)
    {
        if (!Enum.TryParse<Skill>(argument, ignoreCase: true, out var skill))
        {
            context.TurnConsumed = false;
            context.Display.ShowError($"Unknown skill: {argument}");
            return;
        }
        new SkillsCommandHandler().HandleLearnSpecificSkill(skill, context);
    }
}
