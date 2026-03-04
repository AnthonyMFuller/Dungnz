using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Tests for Player HP boundary conditions (#944):
/// healing beyond MaxHP, damage below 0, exact death threshold, resurrection from 0 HP.
/// </summary>
public class PlayerHPBoundaryTests
{
    // ── Healing beyond MaxHP ──────────────────────────────────────────────────

    [Fact]
    public void Heal_BeyondMaxHP_ClampsToMaxHP()
    {
        var player = new PlayerBuilder().WithHP(90).WithMaxHP(100).Build();

        player.Heal(50);

        player.HP.Should().Be(100);
    }

    [Fact]
    public void Heal_AtMaxHP_DoesNotExceed()
    {
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).Build();

        player.Heal(10);

        player.HP.Should().Be(100);
    }

    [Fact]
    public void Heal_ZeroAmount_DoesNotChangeHP()
    {
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).Build();

        player.Heal(0);

        player.HP.Should().Be(50);
    }

    [Fact]
    public void Heal_NegativeAmount_ThrowsArgumentException()
    {
        var player = new Player();

        var act = () => player.Heal(-5);

        act.Should().Throw<ArgumentException>();
    }

    // ── Damage below 0 ───────────────────────────────────────────────────────

    [Fact]
    public void TakeDamage_ExceedsHP_ClampsToZero()
    {
        var player = new PlayerBuilder().WithHP(10).WithMaxHP(100).Build();

        player.TakeDamage(50);

        player.HP.Should().Be(0);
    }

    [Fact]
    public void TakeDamage_ExactHP_ReachesZero()
    {
        var player = new PlayerBuilder().WithHP(25).WithMaxHP(100).Build();

        player.TakeDamage(25);

        player.HP.Should().Be(0);
    }

    [Fact]
    public void TakeDamage_ZeroAmount_DoesNotChangeHP()
    {
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).Build();

        player.TakeDamage(0);

        player.HP.Should().Be(50);
    }

    [Fact]
    public void TakeDamage_NegativeAmount_ThrowsArgumentException()
    {
        var player = new Player();

        var act = () => player.TakeDamage(-5);

        act.Should().Throw<ArgumentException>();
    }

    // ── Exact death threshold ─────────────────────────────────────────────────

    [Fact]
    public void TakeDamage_ToExactlyZero_IsDeadState()
    {
        var player = new PlayerBuilder().WithHP(1).WithMaxHP(100).Build();

        player.TakeDamage(1);

        player.HP.Should().Be(0);
    }

    [Fact]
    public void TakeDamage_From1HP_LargeHit_ClampsToZero()
    {
        var player = new PlayerBuilder().WithHP(1).WithMaxHP(100).Build();

        player.TakeDamage(9999);

        player.HP.Should().Be(0);
    }

    // ── Resurrection from 0 HP ────────────────────────────────────────────────

    [Fact]
    public void Heal_FromZeroHP_RestoresHP()
    {
        var player = new PlayerBuilder().WithHP(0).WithMaxHP(100).Build();

        player.Heal(30);

        player.HP.Should().Be(30);
    }

    [Fact]
    public void SetHPDirect_FromZero_RestoresHP()
    {
        var player = new PlayerBuilder().WithHP(0).WithMaxHP(100).Build();

        player.SetHPDirect(50);

        player.HP.Should().Be(50);
    }

    [Fact]
    public void SetHPDirect_ClampsToMaxHP()
    {
        var player = new PlayerBuilder().WithHP(0).WithMaxHP(100).Build();

        player.SetHPDirect(200);

        player.HP.Should().Be(100);
    }

    [Fact]
    public void SetHPDirect_ClampsToZero()
    {
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).Build();

        player.SetHPDirect(-10);

        player.HP.Should().Be(0);
    }

    // ── HealthChanged event ───────────────────────────────────────────────────

    [Fact]
    public void TakeDamage_FiresOnHealthChanged()
    {
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).Build();
        HealthChangedEventArgs? captured = null;
        player.OnHealthChanged += (_, e) => captured = e;

        player.TakeDamage(30);

        captured.Should().NotBeNull();
        captured!.OldHP.Should().Be(100);
        captured.NewHP.Should().Be(70);
        captured.Delta.Should().Be(-30);
    }

    [Fact]
    public void Heal_FiresOnHealthChanged()
    {
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).Build();
        HealthChangedEventArgs? captured = null;
        player.OnHealthChanged += (_, e) => captured = e;

        player.Heal(20);

        captured.Should().NotBeNull();
        captured!.OldHP.Should().Be(50);
        captured.NewHP.Should().Be(70);
    }

    [Fact]
    public void Heal_AtMaxHP_DoesNotFireEvent()
    {
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).Build();
        bool fired = false;
        player.OnHealthChanged += (_, _) => fired = true;

        player.Heal(10);

        fired.Should().BeFalse();
    }

    [Fact]
    public void TakeDamage_ZeroAmount_DoesNotFireEvent()
    {
        var player = new PlayerBuilder().WithHP(50).WithMaxHP(100).Build();
        bool fired = false;
        player.OnHealthChanged += (_, _) => fired = true;

        player.TakeDamage(0);

        fired.Should().BeFalse();
    }

    // ── FortifyMaxHP ──────────────────────────────────────────────────────────

    [Fact]
    public void FortifyMaxHP_IncreasesMaxAndHeals()
    {
        var player = new PlayerBuilder().WithHP(80).WithMaxHP(100).Build();

        player.FortifyMaxHP(20);

        player.MaxHP.Should().Be(120);
        player.HP.Should().Be(100); // 80 + 20
    }

    [Fact]
    public void FortifyMaxHP_DoesNotExceedNewMax()
    {
        var player = new PlayerBuilder().WithHP(95).WithMaxHP(100).Build();

        player.FortifyMaxHP(10);

        player.MaxHP.Should().Be(110);
        player.HP.Should().Be(105); // 95 + 10
    }
}
