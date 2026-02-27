using Dungnz.Engine;
using Dungnz.Tests.Helpers;
using Xunit;

namespace Dungnz.Tests;

public class FakeMenuNavigatorTests
{
    [Fact]
    public void Select_ReturnsQueuedIndex()
    {
        var nav = new FakeMenuNavigator().EnqueueSelection(1);
        var options = new List<MenuOption<string>>
        {
            new("Option A", "a"),
            new("Option B", "b"),
            new("Option C", "c"),
        };
        var result = nav.Select(options);
        Assert.Equal("b", result);
    }

    [Fact]
    public void Select_DefaultsToIndex0_WhenQueueEmpty()
    {
        var nav = new FakeMenuNavigator();
        var options = new List<MenuOption<int>>
        {
            new("First", 10),
            new("Second", 20),
        };
        var result = nav.Select(options);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Confirm_ReturnsTrueWhenQueued()
    {
        var nav = new FakeMenuNavigator().EnqueueConfirm(true);
        Assert.True(nav.Confirm("Are you sure?"));
    }

    [Fact]
    public void Confirm_DefaultsFalseWhenQueueEmpty()
    {
        var nav = new FakeMenuNavigator();
        Assert.False(nav.Confirm("Are you sure?"));
    }

    [Fact]
    public void Select_ThrowsOnOutOfRangeIndex()
    {
        var nav = new FakeMenuNavigator().EnqueueSelection(5); // only 2 options
        var options = new List<MenuOption<string>>
        {
            new("A", "a"),
            new("B", "b"),
        };
        Assert.Throws<InvalidOperationException>(() => nav.Select(options));
    }
}
