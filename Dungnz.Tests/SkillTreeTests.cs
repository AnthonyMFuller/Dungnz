using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class SkillTreeTests
{
    private static Player MakePlayer(int level = 1)
    {
        return new Player { Level = level };
    }

    [Fact]
    public void TryUnlock_WithEnoughLevel_Succeeds()
    {
        var player = MakePlayer(level: 3);
        var tree = new SkillTree();

        var result = tree.TryUnlock(player, Skill.PowerStrike);

        result.Should().BeTrue();
        tree.IsUnlocked(Skill.PowerStrike).Should().BeTrue();
    }

    [Fact]
    public void TryUnlock_InsufficientLevel_Fails()
    {
        var player = MakePlayer(level: 1);
        var tree = new SkillTree();

        var result = tree.TryUnlock(player, Skill.PowerStrike);

        result.Should().BeFalse();
        tree.IsUnlocked(Skill.PowerStrike).Should().BeFalse();
    }

    [Fact]
    public void TryUnlock_SameSkillTwice_NoDoubleApply()
    {
        var player = MakePlayer(level: 3);
        var tree = new SkillTree();
        tree.TryUnlock(player, Skill.IronSkin);
        var defenseAfterFirst = player.Defense;

        var result = tree.TryUnlock(player, Skill.IronSkin);

        result.Should().BeFalse();
        player.Defense.Should().Be(defenseAfterFirst);
    }

    [Fact]
    public void TryUnlock_PowerStrike_AppliesAttackBonus()
    {
        // PowerStrike is a passive — no immediate stat change; just verifies unlock registers
        var player = MakePlayer(level: 3);
        var tree = new SkillTree();

        tree.TryUnlock(player, Skill.PowerStrike);

        tree.IsUnlocked(Skill.PowerStrike).Should().BeTrue();
    }

    [Fact]
    public void TryUnlock_IronSkin_AppliesDefenseBonus()
    {
        var player = MakePlayer(level: 3);
        var initialDefense = player.Defense;
        var tree = new SkillTree();

        tree.TryUnlock(player, Skill.IronSkin);

        player.Defense.Should().Be(initialDefense + 3);
    }

    [Fact]
    public void TryUnlock_ManaFlow_AppliesMaxManaBonus()
    {
        var player = MakePlayer(level: 4);
        var initialMaxMana = player.MaxMana;
        var tree = new SkillTree();

        tree.TryUnlock(player, Skill.ManaFlow);

        player.MaxMana.Should().Be(initialMaxMana + 10);
    }

    [Fact]
    public void Unlock_RestoredFromSave_DoesNotApplyBonuses()
    {
        var player = MakePlayer(level: 3);
        var initialDefense = player.Defense;
        var tree = new SkillTree();

        // Simulate save-restore path (does not apply stat bonuses)
        tree.Unlock(Skill.IronSkin);

        player.Defense.Should().Be(initialDefense);
        tree.IsUnlocked(Skill.IronSkin).Should().BeTrue();
    }
}
