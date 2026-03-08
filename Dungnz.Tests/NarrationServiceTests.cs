namespace Dungnz.Tests;
using Xunit;
using Dungnz.Systems;

public class NarrationServiceTests
{
    [Fact]
    public void Pick_ReturnsItemFromPool()
    {
        var svc = new NarrationService();
        var pool = new[] { "alpha", "beta", "gamma" };
        var result = svc.Pick(pool);
        Assert.Contains(result, pool);
    }

    [Fact]
    public void Pick_WithFormat_ReturnsFormattedString()
    {
        var svc = new NarrationService();
        var pool = new[] { "Hello, {0}!" };
        var result = svc.Pick(pool, "World");
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void PickWeighted_AlwaysReturnsFromPool()
    {
        var svc = new NarrationService();
        var pool = new (string text, int weight)[]
        {
            ("common", 10),
            ("rare", 1),
        };
        for (int i = 0; i < 50; i++)
        {
            var result = svc.PickWeighted(pool);
            Assert.True(result == "common" || result == "rare");
        }
    }

    [Fact]
    public void Chance_ZeroProbability_ReturnsFalse()
    {
        var svc = new NarrationService();
        Assert.False(svc.Chance(0.0));
    }

    [Fact]
    public void Chance_FullProbability_ReturnsTrue()
    {
        var svc = new NarrationService();
        Assert.True(svc.Chance(1.0));
    }

    [Theory]
    [InlineData(RoomNarrationState.FirstVisit)]
    [InlineData(RoomNarrationState.ActiveEnemies)]
    [InlineData(RoomNarrationState.Cleared)]
    [InlineData(RoomNarrationState.Merchant)]
    [InlineData(RoomNarrationState.Shrine)]
    [InlineData(RoomNarrationState.Boss)]
    public void GetRoomEntryNarration_ReturnsNonEmptyString(RoomNarrationState state)
    {
        var svc = new NarrationService();
        var result = svc.GetRoomEntryNarration(state);
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetRoomEntryNarration_ReturnsDifferentLinesOnMultipleCalls()
    {
        var svc = new NarrationService();
        var results = new HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            results.Add(svc.GetRoomEntryNarration(RoomNarrationState.FirstVisit));
        }
        Assert.True(results.Count > 1, "Expected variety in narration picks");
    }
}
