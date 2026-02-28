using Dungnz.Models;
using Dungnz.Systems;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Additional SkillTree tests to cover class restrictions, GetDescription, and GetAvailableSkills.</summary>
public class SkillTreeAdditionalCoverageTests
{
    private static Player MakePlayer(int level, PlayerClass cls = PlayerClass.Warrior)
        => new Player { Level = level, Class = cls };

    // ── Class restriction enforcement ────────────────────────────────────────

    [Fact]
    public void TryUnlock_ClassRestricted_WrongClass_Fails()
    {
        var player = MakePlayer(level: 3, cls: PlayerClass.Mage);
        var tree = new SkillTree();

        var result = tree.TryUnlock(player, Skill.IronConstitution);

        result.Should().BeFalse("IronConstitution is Warrior-only");
        tree.IsUnlocked(Skill.IronConstitution).Should().BeFalse();
    }

    [Fact]
    public void TryUnlock_ClassRestricted_CorrectClass_Succeeds()
    {
        var player = MakePlayer(level: 3, cls: PlayerClass.Warrior);
        var tree = new SkillTree();

        var result = tree.TryUnlock(player, Skill.IronConstitution);

        result.Should().BeTrue();
        tree.IsUnlocked(Skill.IronConstitution).Should().BeTrue();
    }

    [Fact]
    public void TryUnlock_ArcaneReservoir_MageClass_AppliesMaxManaBonus()
    {
        var player = MakePlayer(level: 3, cls: PlayerClass.Mage);
        player.MaxMana = 40;
        var tree = new SkillTree();

        tree.TryUnlock(player, Skill.ArcaneReservoir);

        player.MaxMana.Should().Be(60, "ArcaneReservoir adds +20 MaxMana");
    }

    [Fact]
    public void TryUnlock_ArcaneReservoir_WrongClass_Fails()
    {
        var player = MakePlayer(level: 3, cls: PlayerClass.Rogue);
        var tree = new SkillTree();

        var result = tree.TryUnlock(player, Skill.ArcaneReservoir);

        result.Should().BeFalse("ArcaneReservoir is Mage-only");
    }

    [Fact]
    public void TryUnlock_BlessedArmor_Paladin_AppliesDefBonus()
    {
        var player = MakePlayer(level: 3, cls: PlayerClass.Paladin);
        var initialDef = player.Defense;
        var tree = new SkillTree();

        tree.TryUnlock(player, Skill.BlessedArmor);

        player.Defense.Should().Be(initialDef + 3, "BlessedArmor adds +3 Defense");
    }

    [Fact]
    public void TryUnlock_IronConstitution_Warrior_IncreasesMaxHP()
    {
        var player = MakePlayer(level: 3, cls: PlayerClass.Warrior);
        var initialMaxHP = player.MaxHP;
        var tree = new SkillTree();

        tree.TryUnlock(player, Skill.IronConstitution);

        player.MaxHP.Should().Be(initialMaxHP + 15, "IronConstitution adds +15 MaxHP");
    }

    // ── All class passives unlock when class matches ──────────────────────────

    [Theory]
    [InlineData(Skill.QuickReflexes, PlayerClass.Rogue, 3)]
    [InlineData(Skill.Opportunist, PlayerClass.Rogue, 4)]
    [InlineData(Skill.Relentless, PlayerClass.Rogue, 6)]
    [InlineData(Skill.ShadowMaster, PlayerClass.Rogue, 8)]
    public void TryUnlock_RoguePassives_CorrectClass_Succeeds(Skill skill, PlayerClass cls, int level)
    {
        var player = MakePlayer(level: level, cls: cls);
        var tree = new SkillTree();

        tree.TryUnlock(player, skill).Should().BeTrue($"{skill} should unlock for {cls} at level {level}");
    }

    [Theory]
    [InlineData(Skill.UndyingServants, PlayerClass.Necromancer, 3)]
    [InlineData(Skill.VampiricTouch, PlayerClass.Necromancer, 4)]
    [InlineData(Skill.MasterOfDeath, PlayerClass.Necromancer, 6)]
    [InlineData(Skill.LichsBargain, PlayerClass.Necromancer, 8)]
    public void TryUnlock_NecromancerPassives_CorrectClass_Succeeds(Skill skill, PlayerClass cls, int level)
    {
        var player = MakePlayer(level: level, cls: cls);
        var tree = new SkillTree();

        tree.TryUnlock(player, skill).Should().BeTrue($"{skill} should unlock for {cls} at level {level}");
    }

    [Theory]
    [InlineData(Skill.KeenEye, PlayerClass.Ranger, 3)]
    [InlineData(Skill.PackTactics, PlayerClass.Ranger, 4)]
    [InlineData(Skill.TrapMastery, PlayerClass.Ranger, 6)]
    [InlineData(Skill.ApexPredator, PlayerClass.Ranger, 8)]
    public void TryUnlock_RangerPassives_CorrectClass_Succeeds(Skill skill, PlayerClass cls, int level)
    {
        var player = MakePlayer(level: level, cls: cls);
        var tree = new SkillTree();

        tree.TryUnlock(player, skill).Should().BeTrue($"{skill} should unlock for {cls} at level {level}");
    }

    [Theory]
    [InlineData(Skill.AuraOfProtection, PlayerClass.Paladin, 4)]
    [InlineData(Skill.HolyFervor, PlayerClass.Paladin, 6)]
    [InlineData(Skill.MartyrResolve, PlayerClass.Paladin, 8)]
    public void TryUnlock_PaladinPassives_CorrectClass_Succeeds(Skill skill, PlayerClass cls, int level)
    {
        var player = MakePlayer(level: level, cls: cls);
        var tree = new SkillTree();

        tree.TryUnlock(player, skill).Should().BeTrue($"{skill} should unlock for {cls} at level {level}");
    }

    [Theory]
    [InlineData(Skill.SpellWeaver, PlayerClass.Mage, 4)]
    [InlineData(Skill.LeyConduit, PlayerClass.Mage, 6)]
    [InlineData(Skill.Overcharge, PlayerClass.Mage, 8)]
    public void TryUnlock_MagePassives_CorrectClass_Succeeds(Skill skill, PlayerClass cls, int level)
    {
        var player = MakePlayer(level: level, cls: cls);
        var tree = new SkillTree();

        tree.TryUnlock(player, skill).Should().BeTrue($"{skill} should unlock for {cls} at level {level}");
    }

    [Theory]
    [InlineData(Skill.UndyingWill, PlayerClass.Warrior, 5)]
    [InlineData(Skill.BerserkersEdge, PlayerClass.Warrior, 6)]
    [InlineData(Skill.Unbreakable, PlayerClass.Warrior, 8)]
    public void TryUnlock_WarriorPassives_CorrectClass_Succeeds(Skill skill, PlayerClass cls, int level)
    {
        var player = MakePlayer(level: level, cls: cls);
        var tree = new SkillTree();

        tree.TryUnlock(player, skill).Should().BeTrue($"{skill} should unlock for {cls} at level {level}");
    }

    // ── Universal skills ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(Skill.Swiftness, 5)]
    [InlineData(Skill.BattleHardened, 6)]
    public void TryUnlock_UniversalSkill_AnyClass_Succeeds(Skill skill, int level)
    {
        var player = MakePlayer(level: level, cls: PlayerClass.Rogue);
        var tree = new SkillTree();

        tree.TryUnlock(player, skill).Should().BeTrue($"{skill} is universal at level {level}");
    }

    // ── GetDescription ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Skill.PowerStrike)]
    [InlineData(Skill.IronSkin)]
    [InlineData(Skill.Swiftness)]
    [InlineData(Skill.ManaFlow)]
    [InlineData(Skill.BattleHardened)]
    [InlineData(Skill.IronConstitution)]
    [InlineData(Skill.UndyingWill)]
    [InlineData(Skill.BerserkersEdge)]
    [InlineData(Skill.Unbreakable)]
    [InlineData(Skill.ArcaneReservoir)]
    [InlineData(Skill.SpellWeaver)]
    [InlineData(Skill.LeyConduit)]
    [InlineData(Skill.Overcharge)]
    [InlineData(Skill.QuickReflexes)]
    [InlineData(Skill.Opportunist)]
    [InlineData(Skill.Relentless)]
    [InlineData(Skill.ShadowMaster)]
    [InlineData(Skill.BlessedArmor)]
    [InlineData(Skill.AuraOfProtection)]
    [InlineData(Skill.HolyFervor)]
    [InlineData(Skill.MartyrResolve)]
    [InlineData(Skill.UndyingServants)]
    [InlineData(Skill.VampiricTouch)]
    [InlineData(Skill.MasterOfDeath)]
    [InlineData(Skill.LichsBargain)]
    [InlineData(Skill.KeenEye)]
    [InlineData(Skill.PackTactics)]
    [InlineData(Skill.TrapMastery)]
    [InlineData(Skill.ApexPredator)]
    public void GetDescription_KnownSkill_ReturnsNonEmpty(Skill skill)
    {
        var desc = SkillTree.GetDescription(skill);
        desc.Should().NotBeNullOrEmpty($"{skill} should have a description");
    }

    [Fact]
    public void GetDescription_UnknownSkill_ReturnsEmpty()
    {
        var desc = SkillTree.GetDescription((Skill)9999);
        desc.Should().BeEmpty();
    }

    // ── GetAvailableSkills ────────────────────────────────────────────────────

    [Fact]
    public void GetAvailableSkills_Level1Warrior_ReturnsNoSkills()
    {
        var player = MakePlayer(level: 1, cls: PlayerClass.Warrior);
        var skills = SkillTree.GetAvailableSkills(player);
        // All skills require at least level 3
        skills.Should().BeEmpty("no skills are available at level 1");
    }

    [Fact]
    public void GetAvailableSkills_Level3Warrior_IncludesWarriorAndUniversalSkills()
    {
        var player = MakePlayer(level: 3, cls: PlayerClass.Warrior);
        var skills = SkillTree.GetAvailableSkills(player);

        skills.Should().Contain(Skill.PowerStrike, "universal level-3 skill");
        skills.Should().Contain(Skill.IronSkin, "universal level-3 skill");
        skills.Should().Contain(Skill.IronConstitution, "Warrior level-3 skill");
        skills.Should().NotContain(Skill.ArcaneReservoir, "Mage-only skill");
    }

    [Fact]
    public void GetAvailableSkills_Level10Rogue_IncludesAllRogueAndUniversalSkills()
    {
        var player = MakePlayer(level: 10, cls: PlayerClass.Rogue);
        var skills = SkillTree.GetAvailableSkills(player);

        skills.Should().Contain(Skill.QuickReflexes);
        skills.Should().Contain(Skill.ShadowMaster);
        skills.Should().Contain(Skill.BattleHardened);
        skills.Should().NotContain(Skill.IronConstitution, "Warrior-only");
    }

    [Fact]
    public void UnlockSkill_Persists_AcrossMultipleCalls()
    {
        var player = MakePlayer(level: 10, cls: PlayerClass.Warrior);
        var tree = new SkillTree();

        tree.Unlock(Skill.PowerStrike);
        tree.Unlock(Skill.IronSkin);

        tree.UnlockedSkills.Should().Contain(Skill.PowerStrike);
        tree.UnlockedSkills.Should().Contain(Skill.IronSkin);
        tree.UnlockedSkills.Should().HaveCount(2);
    }
}
