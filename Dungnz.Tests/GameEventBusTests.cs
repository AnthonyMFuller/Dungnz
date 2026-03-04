using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using FluentAssertions;

namespace Dungnz.Tests;

public class GameEventBusTests
{
    [Fact]
    public void Publish_InvokesSubscribedHandler()
    {
        var bus = new GameEventBus();
        OnRoomEntered? received = null;
        bus.Subscribe<OnRoomEntered>(e => received = e);

        var player = new Player { Name = "Test" };
        var room = new Room { Description = "test room" };
        bus.Publish(new OnRoomEntered(player, room, null));

        received.Should().NotBeNull();
        received!.Player.Name.Should().Be("Test");
    }

    [Fact]
    public void Publish_DoesNothing_WithNoSubscribers()
    {
        var bus = new GameEventBus();
        var act = () => bus.Publish(new OnRoomEntered(new Player(), new Room(), null));
        act.Should().NotThrow();
    }

    [Fact]
    public void Subscribe_MultipleHandlers_AllInvoked()
    {
        var bus = new GameEventBus();
        int count = 0;
        bus.Subscribe<OnPlayerDamaged>(_ => count++);
        bus.Subscribe<OnPlayerDamaged>(_ => count++);

        bus.Publish(new OnPlayerDamaged(new Player(), 10, "test"));

        count.Should().Be(2);
    }

    [Fact]
    public void Publish_OnlyInvokesMatchingType()
    {
        var bus = new GameEventBus();
        bool roomFired = false;
        bool damagedFired = false;
        bus.Subscribe<OnRoomEntered>(_ => roomFired = true);
        bus.Subscribe<OnPlayerDamaged>(_ => damagedFired = true);

        bus.Publish(new OnPlayerDamaged(new Player(), 5, "test"));

        roomFired.Should().BeFalse();
        damagedFired.Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllHandlers()
    {
        var bus = new GameEventBus();
        bus.Subscribe<OnRoomEntered>(_ => { });
        bus.HandlerCount.Should().Be(1);

        bus.Clear();
        bus.HandlerCount.Should().Be(0);
    }

    [Fact]
    public void OnCombatEnd_CarriesCorrectData()
    {
        var bus = new GameEventBus();
        OnCombatEnd? received = null;
        bus.Subscribe<OnCombatEnd>(e => received = e);

        var player = new Player { Name = "Hero" };
        var enemy = new Goblin();
        bus.Publish(new OnCombatEnd(player, enemy, CombatResult.Won));

        received.Should().NotBeNull();
        received!.Result.Should().Be(CombatResult.Won);
        received.Enemy.Should().BeOfType<Goblin>();
    }
}
