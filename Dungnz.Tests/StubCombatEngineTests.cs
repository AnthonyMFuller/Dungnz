using Dungnz.Engine;
using Dungnz.Models;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

public class StubCombatEngineTests
{
    [Fact]
    public void RunCombat_ReturnsWon_AndSetsEnemyHpToZero()
    {
        var stub = new StubCombatEngine();
        var player = new Player();
        var enemy = new Enemy_Stub(50, 10, 5, 20);

        var result = stub.RunCombat(player, enemy);

        result.Should().Be(CombatResult.Won);
        enemy.HP.Should().Be(0);
    }
}
