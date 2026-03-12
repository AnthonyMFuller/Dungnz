using Dungnz.Systems;
using FluentAssertions;
using Spectre.Console;

namespace Dungnz.Tests.Narration;

/// <summary>
/// #1373 — Adversarial markup safety tests for <see cref="EnemyNarration"/>.
/// Verifies all public methods handle bracket-containing names without crashing,
/// and all returned strings are valid Spectre markup.
/// </summary>
public class EnemyNarrationAdversarialTests
{
    private static void AssertAllMarkupSafe(string[] lines, string context)
    {
        lines.Should().NotBeNull($"{context}: must not be null");
        lines.Should().NotBeEmpty($"{context}: must have at least one line");
        lines.Should().NotContainNulls($"{context}: must not contain null entries");
        foreach (var line in lines)
        {
            var captured = line;
            var act = () => _ = new Markup(captured);
            act.Should().NotThrow<Exception>($"{context}: '{captured}' is not valid Spectre markup");
        }
    }

    [Theory]
    [InlineData("[HERO]")]
    [InlineData("[bold]")]
    [InlineData("[red]warrior[/red]")]
    [InlineData("[ spaces inside ]")]
    [InlineData("[123]")]
    [InlineData("[BOSS][/BOSS]")]
    [InlineData("[]")]
    public void GetIntros_BracketName_DoesNotThrow_ReturnsMarkupSafeStrings(string name)
    {
        string[]? lines = null;
        var act = () => lines = EnemyNarration.GetIntros(name);
        act.Should().NotThrow();
        AssertAllMarkupSafe(lines!, $"GetIntros(\"{name}\")");
    }

    [Theory]
    [InlineData("[HERO]")]
    [InlineData("[bold]")]
    [InlineData("[red]warrior[/red]")]
    [InlineData("[ spaces inside ]")]
    [InlineData("[123]")]
    [InlineData("[BOSS][/BOSS]")]
    [InlineData("[]")]
    public void GetDeaths_BracketName_DoesNotThrow_ReturnsMarkupSafeStrings(string name)
    {
        string[]? lines = null;
        var act = () => lines = EnemyNarration.GetDeaths(name);
        act.Should().NotThrow();
        AssertAllMarkupSafe(lines!, $"GetDeaths(\"{name}\")");
    }

    [Theory]
    [InlineData("[HERO]")]
    [InlineData("[bold]")]
    [InlineData("[BOSS][/BOSS]")]
    [InlineData("[]")]
    public void GetCritReactions_BracketName_DoesNotThrow_ReturnsMarkupSafeStrings(string name)
    {
        string[]? lines = null;
        var act = () => lines = EnemyNarration.GetCritReactions(name);
        act.Should().NotThrow();
        AssertAllMarkupSafe(lines!, $"GetCritReactions(\"{name}\")");
    }

    [Theory]
    [InlineData("[HERO]")]
    [InlineData("[bold]")]
    [InlineData("[BOSS][/BOSS]")]
    [InlineData("[]")]
    public void GetIdleTaunts_BracketName_DoesNotThrow_ReturnsMarkupSafeStrings(string name)
    {
        string[]? lines = null;
        var act = () => lines = EnemyNarration.GetIdleTaunts(name);
        act.Should().NotThrow();
        AssertAllMarkupSafe(lines!, $"GetIdleTaunts(\"{name}\")");
    }

    [Theory]
    [InlineData("[HERO]")]
    [InlineData("[bold]")]
    [InlineData("[BOSS][/BOSS]")]
    [InlineData("[]")]
    public void GetDesperationLines_BracketName_DoesNotThrow_ReturnsMarkupSafeStrings(string name)
    {
        string[]? lines = null;
        var act = () => lines = EnemyNarration.GetDesperationLines(name);
        act.Should().NotThrow();
        AssertAllMarkupSafe(lines!, $"GetDesperationLines(\"{name}\")");
    }

    [Theory]
    [InlineData("Goblin")]
    [InlineData("Skeleton")]
    [InlineData("Troll")]
    [InlineData("Dark Knight")]
    [InlineData("Goblin Shaman")]
    [InlineData("Stone Golem")]
    [InlineData("Wraith")]
    [InlineData("Vampire Lord")]
    [InlineData("Mimic")]
    [InlineData("Giant Rat")]
    [InlineData("Cursed Zombie")]
    [InlineData("Blood Hound")]
    [InlineData("Iron Guard")]
    [InlineData("Night Stalker")]
    [InlineData("Bone Archer")]
    [InlineData("Mana Leech")]
    [InlineData("Plague Bear")]
    [InlineData("Crypt Priest")]
    [InlineData("Shield Breaker")]
    [InlineData("Dark Sorcerer")]
    [InlineData("Siege Ogre")]
    [InlineData("Blade Dancer")]
    [InlineData("Shadow Imp")]
    [InlineData("Carrion Crawler")]
    [InlineData("Chaos Knight")]
    [InlineData("Frost Wyvern")]
    [InlineData("Malachar the Undying")]
    [InlineData("Lich King")]
    [InlineData("Archlich Sovereign")]
    [InlineData("Abyssal Leviathan")]
    [InlineData("Infernal Dragon")]
    public void GetDeaths_AllKnownEnemyTypes_ReturnNonNullNonEmptyMarkupSafeLines(string enemyName)
    {
        var lines = EnemyNarration.GetDeaths(enemyName);
        lines.Should().NotBeNull().And.NotBeEmpty();
        lines.Should().NotContainNulls();
        foreach (var line in lines)
        {
            var captured = line;
            var act = () => _ = new Markup(captured);
            act.Should().NotThrow<Exception>($"Death line for \"{enemyName}\": '{captured}'");
        }
    }

    [Theory]
    [InlineData("Goblin")]
    [InlineData("Skeleton")]
    [InlineData("Troll")]
    [InlineData("Dark Knight")]
    [InlineData("Goblin Shaman")]
    [InlineData("Stone Golem")]
    [InlineData("Wraith")]
    [InlineData("Vampire Lord")]
    [InlineData("Mimic")]
    public void GetIntros_KnownEnemyTypes_ReturnMarkupSafeLines(string enemyName)
    {
        var lines = EnemyNarration.GetIntros(enemyName);
        lines.Should().NotBeNull().And.NotBeEmpty();
        foreach (var line in lines)
        {
            var captured = line;
            var act = () => _ = new Markup(captured);
            act.Should().NotThrow<Exception>($"Intro line for \"{enemyName}\": '{captured}'");
        }
    }

    [Fact]
    public void GetIntros_UnknownEnemy_DefaultStringIsMarkupSafe()
    {
        var lines = EnemyNarration.GetIntros("ThisEnemyDoesNotExist_9f3bc");
        lines.Should().NotBeNull().And.NotBeEmpty();
        foreach (var line in lines) { var c = line; var a = () => _ = new Markup(c); a.Should().NotThrow<Exception>(); }
    }

    [Fact]
    public void GetDeaths_UnknownEnemy_DefaultStringIsMarkupSafe()
    {
        var lines = EnemyNarration.GetDeaths("ThisEnemyDoesNotExist_9f3bc");
        lines.Should().NotBeNull().And.NotBeEmpty();
        foreach (var line in lines) { var c = line; var a = () => _ = new Markup(c); a.Should().NotThrow<Exception>(); }
    }

    [Fact]
    public void GetCritReactions_UnknownEnemy_DefaultStringIsMarkupSafe()
    {
        var lines = EnemyNarration.GetCritReactions("ThisEnemyDoesNotExist_9f3bc");
        lines.Should().NotBeNull().And.NotBeEmpty();
        foreach (var line in lines) { var c = line; var a = () => _ = new Markup(c); a.Should().NotThrow<Exception>(); }
    }

    [Fact]
    public void GetIdleTaunts_UnknownEnemy_DefaultStringIsMarkupSafe()
    {
        var lines = EnemyNarration.GetIdleTaunts("ThisEnemyDoesNotExist_9f3bc");
        lines.Should().NotBeNull().And.NotBeEmpty();
        foreach (var line in lines) { var c = line; var a = () => _ = new Markup(c); a.Should().NotThrow<Exception>(); }
    }

    [Fact]
    public void GetDesperationLines_UnknownEnemy_DefaultStringIsMarkupSafe()
    {
        var lines = EnemyNarration.GetDesperationLines("ThisEnemyDoesNotExist_9f3bc");
        lines.Should().NotBeNull().And.NotBeEmpty();
        foreach (var line in lines) { var c = line; var a = () => _ = new Markup(c); a.Should().NotThrow<Exception>(); }
    }

    [Fact]
    public void AllMethods_EmptyStringName_DoNotThrow()
    {
        var act = () =>
        {
            EnemyNarration.GetIntros(string.Empty);
            EnemyNarration.GetDeaths(string.Empty);
            EnemyNarration.GetCritReactions(string.Empty);
            EnemyNarration.GetIdleTaunts(string.Empty);
            EnemyNarration.GetDesperationLines(string.Empty);
        };
        act.Should().NotThrow();
    }
}
