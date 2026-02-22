namespace Dungnz.Models;
using Dungnz.Systems;

public partial class Player
{
    /// <summary>Gets the player's skill tree, tracking unlocked passive skills and their bonuses.</summary>
    public SkillTree Skills { get; } = new();
}
