namespace Dungnz.Models;

/// <summary>
/// Skill tree integration: exposes the player's <see cref="SkillTree"/> for passive skill tracking.
/// </summary>
public partial class Player
{
    /// <summary>Gets the player's skill tree, tracking unlocked passive skills and their bonuses.</summary>
    public SkillTree Skills { get; } = new();
}
