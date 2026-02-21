namespace Dungnz.Systems;
using Dungnz.Models;

public enum Skill
{
    PowerStrike,    // +15% damage on attacks
    IronSkin,       // +3 Defense permanently
    Swiftness,      // +0.05 dodge bonus
    ManaFlow,       // +10 max mana + mana regen +5/turn
    BattleHardened, // Take 5% less damage
}

public class SkillTree
{
    private readonly HashSet<Skill> _unlocked = new();

    public bool IsUnlocked(Skill skill) => _unlocked.Contains(skill);

    public IReadOnlyCollection<Skill> UnlockedSkills => _unlocked;

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

    public static string GetDescription(Skill skill) => skill switch {
        Skill.PowerStrike => "+15% attack damage",
        Skill.IronSkin => "+3 Defense (immediate)",
        Skill.Swiftness => "+5% dodge chance",
        Skill.ManaFlow => "+10 max mana, +5 mana/turn",
        Skill.BattleHardened => "Take 5% less damage",
        _ => ""
    };
}
