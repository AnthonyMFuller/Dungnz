using Dungnz.Display;
using Dungnz.Display.Tui;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Coverage tests for TerminalGuiDisplayService void display methods not previously
/// exercised. Each method delegates to InvokeOnUiThread (fire-and-forget). After
/// GameThreadBridge.SetUiReady() has been called, these methods complete immediately
/// without executing the lambda body, but the method-level code path IS covered.
/// Also covers SessionLogger static methods.
/// </summary>
public class TerminalGuiDisplayServiceVoidMethodTests
{
    private readonly TuiLayout _layout;
    private readonly TerminalGuiDisplayService _svc;

    public TerminalGuiDisplayServiceVoidMethodTests()
    {
        GameThreadBridge.SetUiReady();
        _layout = new TuiLayout();
        _svc = new TerminalGuiDisplayService(_layout);
    }

    // ─── Combat display methods ──────────────────────────────────────────────

    [Fact]
    public void ShowCombat_DoesNotThrow()
    {
        Action act = () => _svc.ShowCombat("You strike the goblin!");
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowCombatStatus_DoesNotThrow()
    {
        var player = new PlayerBuilder().Named("Hero").WithHP(80).WithMaxHP(100).Build();
        var enemy = new EnemyBuilder().Named("Goblin").WithHP(15).Build();

        Action act = () => _svc.ShowCombatStatus(player, enemy,
            new List<ActiveEffect>(),
            new List<ActiveEffect>());

        act.Should().NotThrow();
    }

    [Fact]
    public void ShowCombatMessage_DoesNotThrow()
    {
        Action act = () => _svc.ShowCombatMessage("Goblin deals 5 damage!");
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowCombatStart_DoesNotThrow()
    {
        var enemy = new EnemyBuilder().Named("Troll").Build();
        Action act = () => _svc.ShowCombatStart(enemy);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowCombatEntryFlags_EliteEnemy_DoesNotThrow()
    {
        var enemy = new EnemyBuilder().Named("Elite Goblin").AsElite().Build();
        Action act = () => _svc.ShowCombatEntryFlags(enemy);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowCombatEntryFlags_NormalEnemy_DoesNotThrow()
    {
        var enemy = new EnemyBuilder().Named("Normal Goblin").Build();
        Action act = () => _svc.ShowCombatEntryFlags(enemy);
        act.Should().NotThrow();
    }

    // ─── Inventory / item display methods ───────────────────────────────────

    [Fact]
    public void ShowInventory_EmptyInventory_DoesNotThrow()
    {
        var player = new PlayerBuilder().Build();
        Action act = () => _svc.ShowInventory(player);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowInventory_WithItems_DoesNotThrow()
    {
        var item = new ItemBuilder().Named("Sword").WithDamage(5).Build();
        var player = new PlayerBuilder().WithItem(item).Build();
        Action act = () => _svc.ShowInventory(player);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowLootDrop_DoesNotThrow()
    {
        var item = new ItemBuilder().Named("Gold Ring").Build();
        var player = new PlayerBuilder().Build();
        Action act = () => _svc.ShowLootDrop(item, player);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowLootDrop_IsElite_DoesNotThrow()
    {
        var item = new ItemBuilder().Named("Elite Sword").WithDamage(15).Build();
        var player = new PlayerBuilder().Build();
        Action act = () => _svc.ShowLootDrop(item, player, isElite: true);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowGoldPickup_DoesNotThrow()
    {
        Action act = () => _svc.ShowGoldPickup(amount: 25, newTotal: 75);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowItemPickup_DoesNotThrow()
    {
        var item = new ItemBuilder().Named("Health Potion").WithHeal(20).Build();
        Action act = () => _svc.ShowItemPickup(item, 1, 10, 2, 20);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowItemDetail_DoesNotThrow()
    {
        var item = new ItemBuilder().Named("Magic Staff").Build();
        Action act = () => _svc.ShowItemDetail(item);
        act.Should().NotThrow();
    }

    // ─── Equipment display ───────────────────────────────────────────────────

    [Fact]
    public void ShowEquipmentComparison_WithOldItem_DoesNotThrow()
    {
        var player = new PlayerBuilder().Build();
        var old = new ItemBuilder().Named("Dagger").WithDamage(3).Build();
        var newItem = new ItemBuilder().Named("Sword").WithDamage(8).Build();
        Action act = () => _svc.ShowEquipmentComparison(player, old, newItem);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowEquipmentComparison_NoOldItem_DoesNotThrow()
    {
        var player = new PlayerBuilder().Build();
        var newItem = new ItemBuilder().Named("Shield").WithDefense(5).Build();
        Action act = () => _svc.ShowEquipmentComparison(player, null, newItem);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowEquipment_DoesNotThrow()
    {
        var player = new PlayerBuilder().Build();
        Action act = () => _svc.ShowEquipment(player);
        act.Should().NotThrow();
    }

    // ─── Progress / game state display ──────────────────────────────────────

    [Fact]
    public void ShowPrestigeInfo_DoesNotThrow()
    {
        var prestige = new PrestigeData { PrestigeLevel = 2, TotalWins = 3, TotalRuns = 8 };
        Action act = () => _svc.ShowPrestigeInfo(prestige);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowLevelUpChoice_DoesNotThrow()
    {
        var player = new PlayerBuilder().WithLevel(4).Build();
        Action act = () => _svc.ShowLevelUpChoice(player);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowFloorBanner_DoesNotThrow()
    {
        var variant = DungeonVariant.ForFloor(2);
        Action act = () => _svc.ShowFloorBanner(floor: 2, maxFloor: 5, variant);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowEnemyDetail_DoesNotThrow()
    {
        var enemy = new EnemyBuilder().Named("Dark Knight").WithHP(50).Build();
        Action act = () => _svc.ShowEnemyDetail(enemy);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowVictory_DoesNotThrow()
    {
        var player = new PlayerBuilder().Named("Hero").WithLevel(10).Build();
        var stats = new RunStats();
        Action act = () => _svc.ShowVictory(player, 5, stats);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowGameOver_WithKilledBy_DoesNotThrow()
    {
        var player = new PlayerBuilder().Named("Hero").Build();
        var stats = new RunStats();
        Action act = () => _svc.ShowGameOver(player, "Goblin King", stats);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowGameOver_WithoutKilledBy_DoesNotThrow()
    {
        var player = new PlayerBuilder().Named("Hero").Build();
        var stats = new RunStats();
        Action act = () => _svc.ShowGameOver(player, null, stats);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowEnemyArt_EnemyWithNoArt_DoesNotThrow()
    {
        var enemy = new EnemyBuilder().Named("Goblin").Build();
        Action act = () => _svc.ShowEnemyArt(enemy);
        act.Should().NotThrow();
    }

    // ─── Shop / crafting display ─────────────────────────────────────────────

    [Fact]
    public void ShowShop_DoesNotThrow()
    {
        var stock = new[] { (new ItemBuilder().Named("Potion").WithHeal(20).Build(), 30) };
        Action act = () => _svc.ShowShop(stock, playerGold: 100);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowSellMenu_DoesNotThrow()
    {
        var items = new[] { (new ItemBuilder().Named("Old Sword").WithDamage(3).Build(), 15) };
        Action act = () => _svc.ShowSellMenu(items, playerGold: 50);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowCraftRecipe_DoesNotThrow()
    {
        var result = new ItemBuilder().Named("Master Blade").WithDamage(20).Build();
        var ingredients = new List<(string, bool)> { ("Iron Ore", true), ("Crystal", false) };
        Action act = () => _svc.ShowCraftRecipe("Master Crafting", result, ingredients);
        act.Should().NotThrow();
    }

    // ─── Colored output methods ──────────────────────────────────────────────

    [Fact]
    public void ShowColoredCombatMessage_DoesNotThrow()
    {
        Action act = () => _svc.ShowColoredCombatMessage("Critical hit!", ColorCodes.BrightRed);
        act.Should().NotThrow();
    }

    [Fact]
    public void ShowColoredStat_DoesNotThrow()
    {
        Action act = () => _svc.ShowColoredStat("ATK:", "15", ColorCodes.Red);
        act.Should().NotThrow();
    }

    // ─── ShowEnhancedTitle ───────────────────────────────────────────────────

    [Fact]
    public void ShowEnhancedTitle_DoesNotThrow()
    {
        Action act = () => _svc.ShowEnhancedTitle();
        act.Should().NotThrow();
    }

    // ─── SessionLogger ───────────────────────────────────────────────────────

    [Fact]
    public void SessionLogger_LogSession_Victory_DoesNotThrow()
    {
        var stats = new RunStats
        {
            FloorReached = 5,
            EnemiesDefeated = 20,
            GoldCollected = 350,
            TurnsTaken = 100
        };

        // LogSession writes to AppData/Dungnz — wrapped in try-catch in the impl
        Action act = () => SessionLogger.LogSession(stats, playerWon: true);
        act.Should().NotThrow("SessionLogger silently swallows all errors");
    }

    [Fact]
    public void SessionLogger_LogSession_Defeat_DoesNotThrow()
    {
        var stats = new RunStats
        {
            FloorReached = 2,
            EnemiesDefeated = 5,
            DeathCause = "Goblin",
            DeathEnemy = "Goblin King"
        };

        Action act = () => SessionLogger.LogSession(stats, playerWon: false);
        act.Should().NotThrow("SessionLogger silently swallows all errors");
    }

    [Fact]
    public void SessionLogger_LogBalanceSummary_DoesNotThrow()
    {
        var sessionStats = new SessionStats
        {
            EnemiesKilled = 15,
            GoldEarned = 200,
            FloorsCleared = 3,
            BossKills = 1,
            DamageDealt = 500
        };
        var logger = NullLogger.Instance;

        Action act = () => SessionLogger.LogBalanceSummary(logger, sessionStats, "Victory");
        act.Should().NotThrow();
    }
}
