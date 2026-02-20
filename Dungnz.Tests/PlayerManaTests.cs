namespace Dungnz.Tests;
using Xunit;
using Dungnz.Models;

public class PlayerManaTests
{
    [Fact]
    public void Player_StartsWithMana30()
    {
        var player = new Player { Name = "TestHero" };
        Assert.Equal(30, player.Mana);
        Assert.Equal(30, player.MaxMana);
    }

    [Fact]
    public void SpendMana_DeductsManaCorrectly()
    {
        var player = new Player { Name = "TestHero" };
        player.SpendMana(10);
        Assert.Equal(20, player.Mana);
    }

    [Fact]
    public void SpendMana_InsufficientMana_ThrowsException()
    {
        var player = new Player { Name = "TestHero" };
        Assert.Throws<InvalidOperationException>(() => player.SpendMana(40));
    }

    [Fact]
    public void SpendMana_NegativeAmount_ThrowsException()
    {
        var player = new Player { Name = "TestHero" };
        Assert.Throws<ArgumentException>(() => player.SpendMana(-5));
    }

    [Fact]
    public void RestoreMana_IncreasesManaCappedAtMax()
    {
        var player = new Player { Name = "TestHero" };
        player.SpendMana(15);
        player.RestoreMana(10);
        Assert.Equal(25, player.Mana);
    }

    [Fact]
    public void RestoreMana_CannotExceedMaxMana()
    {
        var player = new Player { Name = "TestHero" };
        player.RestoreMana(20);
        Assert.Equal(30, player.Mana);
    }

    [Fact]
    public void RestoreMana_NegativeAmount_ThrowsException()
    {
        var player = new Player { Name = "TestHero" };
        Assert.Throws<ArgumentException>(() => player.RestoreMana(-5));
    }

    [Fact]
    public void LevelUp_IncreasesMaxManaBy10()
    {
        var player = new Player { Name = "TestHero" };
        player.LevelUp();
        Assert.Equal(40, player.MaxMana);
    }

    [Fact]
    public void LevelUp_RestoresManaToFull()
    {
        var player = new Player { Name = "TestHero" };
        player.SpendMana(20);
        player.LevelUp();
        Assert.Equal(40, player.Mana);
        Assert.Equal(40, player.MaxMana);
    }

    [Fact]
    public void MultipleLevelUps_ManaScalesCorrectly()
    {
        var player = new Player { Name = "TestHero" };
        for (int i = 0; i < 5; i++)
        {
            player.LevelUp();
        }
        Assert.Equal(80, player.MaxMana);
        Assert.Equal(80, player.Mana);
    }
}
