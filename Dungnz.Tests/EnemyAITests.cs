using Dungnz.Engine;
using Dungnz.Models;
using FluentAssertions;

namespace Dungnz.Tests;

public class EnemyAITests
{
    // ── CombatContext ───────────────────────────────────────────────

    [Fact]
    public void CombatContext_StoresValues()
    {
        var ctx = new CombatContext(3, 0.75, 2);
        ctx.RoundNumber.Should().Be(3);
        ctx.PlayerHPPercent.Should().BeApproximately(0.75, 0.001);
        ctx.CurrentFloor.Should().Be(2);
    }
}
