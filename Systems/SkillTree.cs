namespace Dungnz.Systems;
using Dungnz.Models;

/// <summary>Passive skills that can be unlocked by a player once they reach the required level.</summary>
public enum Skill
{
    /// <summary>Increases the player's attack damage by 15% on each hit.</summary>
    PowerStrike,    // +15% damage on attacks
    /// <summary>Permanently adds 3 to the player's defense stat when unlocked.</summary>
    IronSkin,       // +3 Defense permanently
    /// <summary>Adds a 5% flat bonus to the player's dodge chance.</summary>
    Swiftness,      // +0.05 dodge bonus
    /// <summary>Increases maximum mana by 10 and grants 5 mana regeneration per turn.</summary>
    ManaFlow,       // +10 max mana + mana regen +5/turn
    /// <summary>Reduces all incoming damage by 5%.</summary>
    BattleHardened, // Take 5% less damage
}

/// <summary>
/// Tracks which passive skills the player has unlocked and applies their stat bonuses.
/// Skills are gated by minimum player level and can only be unlocked once per run.
/// </summary>
public class SkillTree
{
    private readonly HashSet<Skill> _unlocked = new();

    /// <summary>Returns <see langword="true"/> if the specified skill has been unlocked.</summary>
    /// <param name="skill">The skill to query.</param>
    public bool IsUnlocked(Skill skill) => _unlocked.Contains(skill);

    /// <summary>
    /// Directly marks <paramref name="skill"/> as unlocked without a level check or re-applying
    /// stat bonuses. Used by the save system to restore persisted skill state on load.
    /// </summary>
    /// <param name="skill">The skill to mark as learned.</param>
    public void Unlock(Skill skill) => _unlocked.Add(skill);

    /// <summary>Gets a read-only collection of all skills that have been unlocked.</summary>
    public IReadOnlyCollection<Skill> UnlockedSkills => _unlocked;

    /// <summary>
    /// Attempts to unlock the specified skill for the player. Fails if the skill is already
    /// unlocked or the player has not yet reached the required level.
    /// </summary>
    /// <param name="player">The player attempting to unlock the skill.</param>
    /// <param name="skill">The skill to unlock.</param>
    /// <returns><see langword="true"/> if the skill was successfully unlocked; otherwise <see langword="false"/>.</returns>
    public bool TryUnlock(Player player, Skill skill)
    {
        if (_unlocked.Contains(skill)) return false;
        var minLevel = skill switch {
            Skill.PowerStrike => 3,
            Skill.IronSkin => 3,
            Skill.Swiftness => 5,
            Skill.ManaFlow => 4,
            Skill.BattleHardened => 6,
            _ => 1
        };
        if (player.Level < minLevel) return false;
        _unlocked.Add(skill);
        ApplySkillBonus(player, skill);
        return true;
    }

    private void ApplySkillBonus(Player player, Skill skill)
    {
        switch (skill)
        {
            case Skill.IronSkin:
                player.ModifyDefense(3);
                break;
            case Skill.ManaFlow:
                player.MaxMana += 10;
                player.Mana = Math.Min(player.Mana + 10, player.MaxMana);
                break;
        }
        // PowerStrike, Swiftness, BattleHardened are passive — applied in combat
    }

    /// <summary>
    /// Returns a short human-readable description of the passive bonus granted by the given skill.
    /// </summary>
    /// <param name="skill">The skill to describe.</param>
    /// <returns>A string describing the skill's effect.</returns>
    public static string GetDescription(Skill skill) => skill switch {
        Skill.PowerStrike => "+15% attack damage",
        Skill.IronSkin => "+3 Defense (immediate)",
        Skill.Swiftness => "+5% dodge chance",
        Skill.ManaFlow => "+10 max mana, +5 mana/turn",
        Skill.BattleHardened => "Take 5% less damage",
        _ => ""
    };
}
