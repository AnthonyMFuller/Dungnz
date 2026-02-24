using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests.Display;

/// <summary>
/// Tests for ShowEnemyArt: FakeDisplayService recording, art content constraints,
/// and CombatEngine integration.
/// </summary>
public class ShowEnemyArtTests
{
    // Concrete test enemy allowing AsciiArt injection via constructor
    private class ArtEnemy : Enemy
    {
        public ArtEnemy(string[] art)
        {
            Name = "TestArtEnemy";
            HP = MaxHP = 1;
            Attack = 1;
            Defense = 0;
            XPValue = 0;
            LootTable = new LootTable(minGold: 0, maxGold: 0);
            AsciiArt = art;
        }
    }

    [Fact]
    public void ShowEnemyArt_EmptyAsciiArt_DoesNotAddEntryToAllOutput()
    {
        var display = new FakeDisplayService();
        var enemy = new ArtEnemy([]);

        display.ShowEnemyArt(enemy);

        display.AllOutput.Should().NotContain(s => s.StartsWith("enemy_art:"));
    }

    [Fact]
    public void ShowEnemyArt_WithAsciiArt_AddsJoinedEntryToAllOutput()
    {
        var display = new FakeDisplayService();
        var enemy = new ArtEnemy(["line1", "line2"]);

        display.ShowEnemyArt(enemy);

        display.AllOutput.Should().Contain("enemy_art:line1|line2");
    }

    [Fact]
    public void AllEnemyArtLines_AreAtMost34CharactersLong()
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "enemy-stats.json");
        var config = EnemyConfig.Load(dataPath);

        var violations = config
            .SelectMany(kvp => kvp.Value.AsciiArt.Select(line => (kvp.Key, line)))
            .Where(x => x.line.Length > 34)
            .ToList();

        violations.Should().BeEmpty(
            because: "all ASCII art lines must fit within the 34-character display limit, " +
                     "but found violations: " + string.Join(", ", violations.Select(v => $"[{v.Key}] \"{v.line}\" ({v.line.Length})")));
    }

    [Fact]
    public void CombatEngine_RunCombat_CallsShowEnemyArt()
    {
        var player = new Player { HP = 100, MaxHP = 100, Attack = 50, Defense = 5 };
        var enemy = new ArtEnemy(["art_line1", "art_line2"]);
        var display = new FakeDisplayService();
        var input = new FakeInputReader("A");
        var engine = new CombatEngine(display, input, new ControlledRandom());

        engine.RunCombat(player, enemy);

        display.AllOutput.Should().Contain(s => s.StartsWith("enemy_art:"),
            because: "CombatEngine.RunCombat should call ShowEnemyArt after ShowCombatStart");
    }
}
