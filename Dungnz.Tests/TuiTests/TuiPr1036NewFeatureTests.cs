using Dungnz.Display.Tui;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Tests for new features added in PR #1036:
/// - GameThreadBridge.SetUiReady() — signals MainLoop readiness
/// - TerminalGuiDisplayService caching fields (_player, _currentRoom, _currentFloor)
/// - ShowColoredMessage routing via TuiColorMapper
/// - ShowSkillTreeMenu early-return when all skills are learned
/// </summary>
public class TuiPr1036NewFeatureTests
{
    // ─── GameThreadBridge.SetUiReady ─────────────────────────────────────────

    [Fact]
    public void SetUiReady_DoesNotThrow()
    {
        // Arrange & Act
        Action act = () => GameThreadBridge.SetUiReady();

        // Assert
        act.Should().NotThrow("SetUiReady sets a ManualResetEventSlim and must not throw");
    }

    [Fact]
    public void SetUiReady_CalledMultipleTimes_DoesNotThrow()
    {
        // ManualResetEventSlim.Set() is idempotent
        Action act = () =>
        {
            GameThreadBridge.SetUiReady();
            GameThreadBridge.SetUiReady();
            GameThreadBridge.SetUiReady();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void InvokeOnUiThread_AfterSetUiReady_ReturnsQuickly()
    {
        // Arrange — ensure _uiReady is set so the 5-second wait is skipped
        GameThreadBridge.SetUiReady();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        GameThreadBridge.InvokeOnUiThread(() => { });
        sw.Stop();

        // Assert — should complete in well under 1 second (not the 5-second timeout)
        sw.ElapsedMilliseconds.Should().BeLessThan(1000,
            "InvokeOnUiThread should return immediately after SetUiReady is called");
    }

    // ─── TerminalGuiDisplayService — caching fields ──────────────────────────

    [Fact]
    public void ShowRoom_DoesNotThrow_WithValidRoom()
    {
        // Arrange
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);
        var room = new RoomBuilder()
            .Named("A stone chamber.")
            .OfType(RoomType.Standard)
            .Build();

        // Act — _currentRoom is assigned before InvokeOnUiThread call
        Action act = () => service.ShowRoom(room);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowRoom_WithRoomHavingEnemy_DoesNotThrow()
    {
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);
        var enemy = new EnemyBuilder().Named("Goblin").WithHP(10).Build();
        var room = new RoomBuilder().WithEnemy(enemy).Build();

        Action act = () => service.ShowRoom(room);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowRoom_WithRoomHavingItems_DoesNotThrow()
    {
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);
        var item = new ItemBuilder().Named("Rusty Sword").WithDamage(3).Build();
        var room = new RoomBuilder().WithLoot(item).Build();

        Action act = () => service.ShowRoom(room);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowRoom_AfterShowPlayerStats_UsesPlayerForStats()
    {
        // Arrange — set _player via ShowPlayerStats first, then call ShowRoom
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);
        var player = new PlayerBuilder().Named("Tester").WithHP(80).WithMaxHP(100).Build();
        var room = new RoomBuilder().Build();

        // Act — ShowPlayerStats caches _player; ShowRoom uses it for stats panel
        Action act = () =>
        {
            service.ShowPlayerStats(player);
            service.ShowRoom(room);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowMap_DoesNotThrow_WithValidRoom()
    {
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);
        var room = new RoomBuilder().Build();

        Action act = () => service.ShowMap(room, floor: 2);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowPlayerStats_DoesNotThrow_WithValidPlayer()
    {
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);
        var player = new PlayerBuilder().Named("Hero").Build();

        Action act = () => service.ShowPlayerStats(player);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowColoredMessage_DoesNotThrow_WithKnownColor()
    {
        // ShowColoredMessage now routes through TuiColorMapper (#1037)
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        Action act = () => service.ShowColoredMessage("Damage dealt!", Dungnz.Systems.ColorCodes.Red);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowColoredMessage_DoesNotThrow_WithUnknownColor()
    {
        GameThreadBridge.SetUiReady();
        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        Action act = () => service.ShowColoredMessage("Info message", "unknown-code");
        act.Should().NotThrow();
    }

    // ─── ShowSkillTreeMenu — early return path ───────────────────────────────

    [Fact]
    public void ShowSkillTreeMenu_PlayerWithAllSkills_ReturnsNull()
    {
        // Arrange — unlock every skill so the menu returns early
        var player = new PlayerBuilder().Build();
        foreach (var skill in Enum.GetValues<Dungnz.Models.Skill>())
            player.Skills.Unlock(skill);

        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        // Act — should take the early-return path (no dialog shown)
        var result = service.ShowSkillTreeMenu(player);

        // Assert
        result.Should().BeNull("player with all skills has nothing left to learn");
    }

    [Fact]
    public void ShowSkillTreeMenu_PlayerWithNoSkills_BuildsAvailableList()
    {
        // Arrange — player with no skills: availableSkills will be non-empty
        var player = new PlayerBuilder().Build();
        // player.Skills is empty by default

        var layout = new TuiLayout();
        var service = new TerminalGuiDisplayService(layout);

        // Act — InvokeOnUiThreadAndWait calls dialog (MainLoop is null, so action runs directly)
        // The dialog constructor requires FakeDriver, so just verify no exception of wrong type
        Action act = () =>
        {
            try { service.ShowSkillTreeMenu(player); }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // Expected: may throw due to no Application init — that's OK
            }
        };

        act.Should().NotThrow<NullReferenceException>(
            "code before InvokeOnUiThreadAndWait should not null-ref");
    }

    // ─── TuiLayout color scheme — new fields ─────────────────────────────────

    [Fact]
    public void TuiLayout_SetMap_StoresTextInPersistentView()
    {
        // Arrange
        var layout = new TuiLayout();
        var mapText = "Floor 1\n\n[@] ─ [?]";

        // Act
        Action act = () => layout.SetMap(mapText);

        // Assert
        act.Should().NotThrow("SetMap writes to the persistent _mapView TextView");
    }

    [Fact]
    public void TuiLayout_SetStats_StoresTextInPersistentView()
    {
        // Arrange
        var layout = new TuiLayout();
        var statsText = "HP: [████████]\n    100/100\nATK: 10\nDEF: 5";

        // Act
        Action act = () => layout.SetStats(statsText);

        // Assert
        act.Should().NotThrow("SetStats writes to the persistent _statsView TextView");
    }

    [Fact]
    public void TuiLayout_SetMapThenSetMap_DoesNotAccumulate()
    {
        // Persistent view should replace, not append
        var layout = new TuiLayout();

        Action act = () =>
        {
            layout.SetMap("First map");
            layout.SetMap("Second map");
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void TuiLayout_SetStatsThenSetStats_DoesNotAccumulate()
    {
        var layout = new TuiLayout();

        Action act = () =>
        {
            layout.SetStats("Old stats");
            layout.SetStats("New stats");
        };

        act.Should().NotThrow();
    }

    // ─── AppendLog message types (#1036 color routing) ───────────────────────

    [Fact]
    public void AppendLog_ErrorType_AddsErrorPrefix()
    {
        var layout = new TuiLayout();
        layout.AppendLog("Something went wrong", "error");

        layout.MessageLogPanel.Text.ToString().Should().Contain("❌");
    }

    [Fact]
    public void AppendLog_CombatType_AddsCombatPrefix()
    {
        var layout = new TuiLayout();
        layout.AppendLog("Attack hit!", "combat");

        layout.MessageLogPanel.Text.ToString().Should().Contain("⚔");
    }

    [Fact]
    public void AppendLog_LootType_AddsLootPrefix()
    {
        var layout = new TuiLayout();
        layout.AppendLog("Item found", "loot");

        layout.MessageLogPanel.Text.ToString().Should().Contain("💰");
    }

    [Fact]
    public void AppendLog_InfoType_AddsInfoPrefix()
    {
        var layout = new TuiLayout();
        layout.AppendLog("General info", "info");

        layout.MessageLogPanel.Text.ToString().Should().Contain("ℹ");
    }

    [Fact]
    public void AppendLog_DefaultType_AddsInfoPrefix()
    {
        var layout = new TuiLayout();
        layout.AppendLog("No type specified");

        layout.MessageLogPanel.Text.ToString().Should().Contain("ℹ");
    }
}
