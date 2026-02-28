using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>Tests for GameEvents, all EventArgs classes, and HealthChangedEventArgs.</summary>
public class GameEventsCoverageTests
{
    // ── CombatEndedEventArgs ─────────────────────────────────────────────────

    [Fact]
    public void CombatEndedEventArgs_Properties_MatchCtorArgs()
    {
        var player = new Player { Name = "Hero" };
        var enemy = new Goblin();
        var args = new CombatEndedEventArgs(player, enemy, CombatResult.Won);

        args.Player.Should().BeSameAs(player);
        args.Enemy.Should().BeSameAs(enemy);
        args.Result.Should().Be(CombatResult.Won);
    }

    [Fact]
    public void CombatEndedEventArgs_PlayerDied_Result()
    {
        var player = new Player { Name = "Hero" };
        var enemy = new Goblin();
        var args = new CombatEndedEventArgs(player, enemy, CombatResult.PlayerDied);
        args.Result.Should().Be(CombatResult.PlayerDied);
    }

    [Fact]
    public void CombatEndedEventArgs_Fled_Result()
    {
        var player = new Player { Name = "Hero" };
        var enemy = new Goblin();
        var args = new CombatEndedEventArgs(player, enemy, CombatResult.Fled);
        args.Result.Should().Be(CombatResult.Fled);
    }

    // ── ItemPickedEventArgs ──────────────────────────────────────────────────

    [Fact]
    public void ItemPickedEventArgs_Properties_MatchCtorArgs()
    {
        var player = new Player { Name = "Hero" };
        var item = new Item { Name = "Sword" };
        var room = new Room { Description = "A dark room" };
        var args = new ItemPickedEventArgs(player, item, room);

        args.Player.Should().BeSameAs(player);
        args.Item.Should().BeSameAs(item);
        args.Room.Should().BeSameAs(room);
    }

    // ── LevelUpEventArgs ─────────────────────────────────────────────────────

    [Fact]
    public void LevelUpEventArgs_Properties_MatchCtorArgs()
    {
        var player = new Player { Name = "Hero" };
        var args = new LevelUpEventArgs(player, oldLevel: 3, newLevel: 4);

        args.Player.Should().BeSameAs(player);
        args.OldLevel.Should().Be(3);
        args.NewLevel.Should().Be(4);
    }

    // ── RoomEnteredEventArgs ─────────────────────────────────────────────────

    [Fact]
    public void RoomEnteredEventArgs_Properties_MatchCtorArgs()
    {
        var player = new Player { Name = "Hero" };
        var room = new Room { Description = "New room" };
        var prev = new Room { Description = "Previous room" };
        var args = new RoomEnteredEventArgs(player, room, prev);

        args.Player.Should().BeSameAs(player);
        args.Room.Should().BeSameAs(room);
        args.PreviousRoom.Should().BeSameAs(prev);
    }

    [Fact]
    public void RoomEnteredEventArgs_NullPreviousRoom_AllowedAtGameStart()
    {
        var player = new Player { Name = "Hero" };
        var room = new Room { Description = "Start room" };
        var args = new RoomEnteredEventArgs(player, room, null);

        args.PreviousRoom.Should().BeNull();
    }

    // ── AchievementUnlockedEventArgs ─────────────────────────────────────────

    [Fact]
    public void AchievementUnlockedEventArgs_Properties_MatchCtorArgs()
    {
        var args = new AchievementUnlockedEventArgs("First Blood", "Killed your first enemy");

        args.Name.Should().Be("First Blood");
        args.Description.Should().Be("Killed your first enemy");
    }

    // ── HealthChangedEventArgs ────────────────────────────────────────────────

    [Fact]
    public void HealthChangedEventArgs_Properties_MatchCtorArgs()
    {
        var args = new HealthChangedEventArgs(100, 75);

        args.OldHP.Should().Be(100);
        args.NewHP.Should().Be(75);
    }

    // ── GameEvents ────────────────────────────────────────────────────────────

    [Fact]
    public void GameEvents_RaiseCombatEnded_InvokesSubscribers()
    {
        var events = new GameEvents();
        CombatEndedEventArgs? received = null;
        events.OnCombatEnded += (_, e) => received = e;

        var player = new Player { Name = "Hero" };
        var enemy = new Goblin();
        events.RaiseCombatEnded(player, enemy, CombatResult.Won);

        received.Should().NotBeNull();
        received!.Result.Should().Be(CombatResult.Won);
    }

    [Fact]
    public void GameEvents_RaiseCombatEnded_NoSubscribers_DoesNotThrow()
    {
        var events = new GameEvents();
        var player = new Player { Name = "Hero" };
        var enemy = new Goblin();
        var act = () => events.RaiseCombatEnded(player, enemy, CombatResult.Won);
        act.Should().NotThrow();
    }

    [Fact]
    public void GameEvents_RaiseItemPicked_InvokesSubscribers()
    {
        var events = new GameEvents();
        ItemPickedEventArgs? received = null;
        events.OnItemPicked += (_, e) => received = e;

        var player = new Player { Name = "Hero" };
        var item = new Item { Name = "Sword" };
        var room = new Room { Description = "Room" };
        events.RaiseItemPicked(player, item, room);

        received.Should().NotBeNull();
        received!.Item.Name.Should().Be("Sword");
    }

    [Fact]
    public void GameEvents_RaiseItemPicked_NoSubscribers_DoesNotThrow()
    {
        var events = new GameEvents();
        var player = new Player { Name = "Hero" };
        var item = new Item { Name = "Sword" };
        var room = new Room { Description = "Room" };
        var act = () => events.RaiseItemPicked(player, item, room);
        act.Should().NotThrow();
    }

    [Fact]
    public void GameEvents_RaiseLevelUp_InvokesSubscribers()
    {
        var events = new GameEvents();
        LevelUpEventArgs? received = null;
        events.OnLevelUp += (_, e) => received = e;

        var player = new Player { Name = "Hero" };
        events.RaiseLevelUp(player, 2, 3);

        received.Should().NotBeNull();
        received!.OldLevel.Should().Be(2);
        received!.NewLevel.Should().Be(3);
    }

    [Fact]
    public void GameEvents_RaiseLevelUp_NoSubscribers_DoesNotThrow()
    {
        var events = new GameEvents();
        var player = new Player { Name = "Hero" };
        var act = () => events.RaiseLevelUp(player, 1, 2);
        act.Should().NotThrow();
    }

    [Fact]
    public void GameEvents_RaiseRoomEntered_InvokesSubscribers()
    {
        var events = new GameEvents();
        RoomEnteredEventArgs? received = null;
        events.OnRoomEntered += (_, e) => received = e;

        var player = new Player { Name = "Hero" };
        var room = new Room { Description = "Room" };
        events.RaiseRoomEntered(player, room, null);

        received.Should().NotBeNull();
        received!.PreviousRoom.Should().BeNull();
    }

    [Fact]
    public void GameEvents_RaiseRoomEntered_NoSubscribers_DoesNotThrow()
    {
        var events = new GameEvents();
        var player = new Player { Name = "Hero" };
        var room = new Room { Description = "Room" };
        var act = () => events.RaiseRoomEntered(player, room, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void GameEvents_RaiseAchievementUnlocked_InvokesSubscribers()
    {
        var events = new GameEvents();
        AchievementUnlockedEventArgs? received = null;
        events.OnAchievementUnlocked += (_, e) => received = e;

        events.RaiseAchievementUnlocked("First Blood", "Killed your first enemy");

        received.Should().NotBeNull();
        received!.Name.Should().Be("First Blood");
    }

    [Fact]
    public void GameEvents_RaiseAchievementUnlocked_NoSubscribers_DoesNotThrow()
    {
        var events = new GameEvents();
        var act = () => events.RaiseAchievementUnlocked("Achievement", "Description");
        act.Should().NotThrow();
    }

    // ── HealthChangedEventArgs.Delta ──────────────────────────────────────────

    [Fact]
    public void HealthChangedEventArgs_Delta_PositiveWhenHealed()
    {
        var args = new HealthChangedEventArgs(50, 80);
        args.Delta.Should().Be(30, "Delta = NewHP - OldHP = 80 - 50 = 30");
        args.OldHP.Should().Be(50);
        args.NewHP.Should().Be(80);
    }

    [Fact]
    public void HealthChangedEventArgs_Delta_NegativeWhenDamaged()
    {
        var args = new HealthChangedEventArgs(80, 50);
        args.Delta.Should().Be(-30, "Delta = 50 - 80 = -30");
    }

    // ── ActiveEffect.IsBuff ───────────────────────────────────────────────────

    [Fact]
    public void ActiveEffect_IsBuff_TrueForNonDebuff()
    {
        var buff = new ActiveEffect(StatusEffect.Fortified, 2);
        buff.IsBuff.Should().BeTrue("Fortified is not a debuff");
        buff.IsDebuff.Should().BeFalse();
    }

    [Fact]
    public void ActiveEffect_IsBuff_FalseForDebuff()
    {
        var debuff = new ActiveEffect(StatusEffect.Poison, 2);
        debuff.IsBuff.Should().BeFalse("Poison is a debuff");
        debuff.IsDebuff.Should().BeTrue();
    }
}
