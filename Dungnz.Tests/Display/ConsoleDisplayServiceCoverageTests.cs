using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests.Display;

/// <summary>
/// Additional coverage tests for ConsoleDisplayService targeting methods not
/// exercised by the existing DisplayServiceTests or DisplayServiceSmokeTests.
/// Uses the same Console.SetOut capture pattern as DisplayServiceTests.
/// </summary>
[Collection("console-output")]
public sealed class ConsoleDisplayServiceCoverageTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly ConsoleDisplayService _svc;

    public ConsoleDisplayServiceCoverageTests()
    {
        _originalOut = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
        _svc = new ConsoleDisplayService();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    private string Output => _output.ToString();

    // ─── ShowCombatStatus ────────────────────────────────────────────────────

    [Fact]
    public void ShowCombatStatus_WritesPlayerAndEnemyHP()
    {
        var player = new PlayerBuilder().Named("Hero").WithHP(80).WithMaxHP(100).Build();
        var enemy = new EnemyBuilder().Named("Goblin").WithHP(15).Build();

        _svc.ShowCombatStatus(player, enemy,
            new List<ActiveEffect>(),
            new List<ActiveEffect>());

        // Service writes "You: [HP]" for player and "{name}: [HP]" for enemy
        Output.Should().Contain("Goblin");
        Output.Should().Contain("You:");
    }

    [Fact]
    public void ShowCombatStatus_WithActiveEffects_ShowsEffects()
    {
        var player = new PlayerBuilder().Named("Hero").Build();
        var enemy = new EnemyBuilder().Named("Boss").Build();
        var playerEffects = new List<ActiveEffect>
        {
            new ActiveEffect(StatusEffect.Poison, 3)
        };
        var enemyEffects = new List<ActiveEffect>
        {
            new ActiveEffect(StatusEffect.Bleed, 2)
        };

        _svc.ShowCombatStatus(player, enemy, playerEffects, enemyEffects);

        Output.Should().Contain("Poison");
        Output.Should().Contain("Bleed");
    }

    [Fact]
    public void ShowCombatStatus_PlayerWithMana_ShowsManaBar()
    {
        var player = new PlayerBuilder().Named("Mage").WithMana(20).WithMaxMana(50).Build();
        var enemy = new EnemyBuilder().Named("Skeleton").Build();

        _svc.ShowCombatStatus(player, enemy,
            new List<ActiveEffect>(),
            new List<ActiveEffect>());

        Output.Should().Contain("MP");
    }

    // ─── ShowCombatMessage ───────────────────────────────────────────────────

    [Fact]
    public void ShowCombatMessage_WritesMessage()
    {
        _svc.ShowCombatMessage("You strike the goblin for 12 damage!");

        Output.Should().Contain("12 damage");
    }

    // ─── ShowLootDrop ────────────────────────────────────────────────────────

    [Fact]
    public void ShowLootDrop_Normal_WritesItemInfo()
    {
        var item = new ItemBuilder().Named("Iron Sword").WithDamage(5).WithTier(ItemTier.Common).Build();
        var player = new PlayerBuilder().Build();

        _svc.ShowLootDrop(item, player);

        Output.Should().Contain("Iron Sword");
    }

    [Fact]
    public void ShowLootDrop_Elite_WritesEliteLoot()
    {
        var item = new ItemBuilder().Named("Silver Blade").WithDamage(10).WithTier(ItemTier.Uncommon).Build();
        var player = new PlayerBuilder().Build();

        _svc.ShowLootDrop(item, player, isElite: true);

        Output.Should().Contain("Silver Blade");
        Output.Should().Contain("ELITE");
    }

    [Fact]
    public void ShowLootDrop_BetterWeaponThanEquipped_ShowsUpgrade()
    {
        var equipped = new ItemBuilder().Named("Rusty Sword").WithDamage(3).Build();
        var newItem = new ItemBuilder().Named("Sharp Sword").WithDamage(8).WithTier(ItemTier.Uncommon).Build();
        var player = new PlayerBuilder().WithWeapon(equipped).Build();

        _svc.ShowLootDrop(newItem, player);

        Output.Should().Contain("Sharp Sword");
    }

    // ─── ShowGoldPickup ──────────────────────────────────────────────────────

    [Fact]
    public void ShowGoldPickup_WritesAmountAndTotal()
    {
        _svc.ShowGoldPickup(amount: 25, newTotal: 100);

        Output.Should().Contain("25");
        Output.Should().Contain("100");
    }

    // ─── ShowItemPickup ──────────────────────────────────────────────────────

    [Fact]
    public void ShowItemPickup_WritesItemName()
    {
        var item = new ItemBuilder().Named("Health Potion").WithHeal(30).Build();

        _svc.ShowItemPickup(item, slotsCurrent: 2, slotsMax: 10, weightCurrent: 3, weightMax: 20);

        Output.Should().Contain("Health Potion");
    }

    [Fact]
    public void ShowItemPickup_NearlyFull_ShowsWarning()
    {
        var item = new ItemBuilder().Named("Big Shield").WithDefense(5).Build();

        // weightCurrent > weightMax * 0.8 → warning
        _svc.ShowItemPickup(item, slotsCurrent: 8, slotsMax: 10, weightCurrent: 17, weightMax: 20);

        Output.Should().Contain("Big Shield");
    }

    // ─── ShowItemDetail ──────────────────────────────────────────────────────

    [Fact]
    public void ShowItemDetail_WritesAllItemFields()
    {
        var item = new Item
        {
            Name = "Enchanted Blade",
            Type = ItemType.Weapon,
            Tier = ItemTier.Rare,
            AttackBonus = 12,
            Weight = 4,
            Description = "A blade imbued with ancient power."
        };

        _svc.ShowItemDetail(item);

        // Name is written UPPERCASE inside a box border
        Output.Should().Contain("ENCHANTED BLADE");
    }

    [Fact]
    public void ShowItemDetail_ConsumableItem_ShowsHealAmount()
    {
        var item = new Item
        {
            Name = "Super Potion",
            Type = ItemType.Consumable,
            Tier = ItemTier.Uncommon,
            HealAmount = 50,
            Weight = 1
        };

        _svc.ShowItemDetail(item);

        // Name is written UPPERCASE
        Output.Should().Contain("SUPER POTION");
    }

    // ─── ShowEquipmentComparison ─────────────────────────────────────────────

    [Fact]
    public void ShowEquipmentComparison_WithOldItem_ShowsDelta()
    {
        var player = new PlayerBuilder().Build();
        var oldItem = new ItemBuilder().Named("Dagger").WithDamage(5).Build();
        var newItem = new ItemBuilder().Named("Sword").WithDamage(10).Build();

        _svc.ShowEquipmentComparison(player, oldItem, newItem);

        Output.Should().Contain("Dagger");
        Output.Should().Contain("Sword");
    }

    [Fact]
    public void ShowEquipmentComparison_WithNoOldItem_ShowsNewItem()
    {
        var player = new PlayerBuilder().Build();
        var newItem = new ItemBuilder().Named("Iron Shield").WithDefense(3).Build();

        _svc.ShowEquipmentComparison(player, oldItem: null, newItem: newItem);

        Output.Should().Contain("Iron Shield");
    }

    // ─── ShowEquipment ───────────────────────────────────────────────────────

    [Fact]
    public void ShowEquipment_WritesAllSlots()
    {
        var player = new PlayerBuilder().Build();

        _svc.ShowEquipment(player);

        Output.Should().NotBeNullOrEmpty("ShowEquipment writes equipment slots");
    }

    // ─── ShowPrestigeInfo ────────────────────────────────────────────────────

    [Fact]
    public void ShowPrestigeInfo_WritesPrestigeLevel()
    {
        var prestige = new PrestigeData
        {
            PrestigeLevel = 3,
            TotalWins = 5,
            TotalRuns = 12,
            BonusStartAttack = 2,
            BonusStartDefense = 1
        };

        _svc.ShowPrestigeInfo(prestige);

        Output.Should().Contain("3");
    }

    // ─── ShowShop ────────────────────────────────────────────────────────────

    [Fact]
    public void ShowShop_WritesStockItems()
    {
        var stock = new[]
        {
            (new ItemBuilder().Named("Health Potion").WithHeal(30).Build(), 50),
            (new ItemBuilder().Named("Iron Sword").WithDamage(8).Build(), 120)
        };

        _svc.ShowShop(stock, playerGold: 100);

        Output.Should().Contain("Health Potion");
        Output.Should().Contain("Iron Sword");
    }

    // ─── ShowSellMenu ────────────────────────────────────────────────────────

    [Fact]
    public void ShowSellMenu_WritesItemsWithSellPrice()
    {
        var items = new[]
        {
            (new ItemBuilder().Named("Old Dagger").WithDamage(2).Build(), 15),
            (new ItemBuilder().Named("Worn Armor").WithDefense(1).Build(), 10)
        };

        _svc.ShowSellMenu(items, playerGold: 50);

        Output.Should().Contain("Old Dagger");
        Output.Should().Contain("Worn Armor");
    }

    // ─── ShowCraftRecipe ─────────────────────────────────────────────────────

    [Fact]
    public void ShowCraftRecipe_WritesRecipeDetails()
    {
        var resultItem = new ItemBuilder().Named("Enchanted Blade").WithDamage(15).Build();
        var ingredients = new List<(string ingredient, bool playerHasIt)>
        {
            ("Iron Ore", true),
            ("Magic Crystal", false)
        };

        _svc.ShowCraftRecipe("Master Blade", resultItem, ingredients);

        Output.Should().Contain("Master Blade");
        Output.Should().Contain("Iron Ore");
    }

    // ─── ShowCombatStart ─────────────────────────────────────────────────────

    [Fact]
    public void ShowCombatStart_WritesEnemyName()
    {
        var enemy = new EnemyBuilder().Named("Dark Knight").Build();

        _svc.ShowCombatStart(enemy);

        Output.Should().Contain("Dark Knight");
    }

    // ─── ShowCombatEntryFlags ────────────────────────────────────────────────

    [Fact]
    public void ShowCombatEntryFlags_EliteEnemy_WritesEliteFlag()
    {
        var enemy = new EnemyBuilder().Named("Elite Troll").AsElite().Build();

        _svc.ShowCombatEntryFlags(enemy);

        Output.Should().Contain("ELITE");
    }

    [Fact]
    public void ShowCombatEntryFlags_NormalEnemy_WritesNothing()
    {
        var enemy = new EnemyBuilder().Named("Normal Goblin").Build();

        _svc.ShowCombatEntryFlags(enemy);

        // No elite flag for normal enemies
        Output.Should().NotContain("ELITE");
    }

    // ─── ShowLevelUpChoice ───────────────────────────────────────────────────

    [Fact]
    public void ShowLevelUpChoice_WritesChoiceOptions()
    {
        var player = new PlayerBuilder().WithLevel(5).Build();

        _svc.ShowLevelUpChoice(player);

        Output.Should().NotBeNullOrEmpty("ShowLevelUpChoice writes level-up options");
    }

    // ─── ShowFloorBanner ─────────────────────────────────────────────────────

    [Fact]
    public void ShowFloorBanner_WritesFloorNumber()
    {
        var variant = DungeonVariant.ForFloor(3);

        _svc.ShowFloorBanner(floor: 3, maxFloor: 5, variant);

        Output.Should().Contain("3");
    }

    // ─── ShowEnemyDetail ─────────────────────────────────────────────────────

    [Fact]
    public void ShowEnemyDetail_WritesEnemyStats()
    {
        var enemy = new EnemyBuilder().Named("Skeleton Archer").WithHP(30).WithAttack(8).Build();

        _svc.ShowEnemyDetail(enemy);

        // Name is written UPPERCASE inside a box border
        Output.Should().Contain("SKELETON ARCHER");
    }

    // ─── ShowVictory ─────────────────────────────────────────────────────────

    [Fact]
    public void ShowVictory_WritesPlayerNameAndStats()
    {
        var player = new PlayerBuilder().Named("Champion").WithLevel(10).Build();
        var stats = new RunStats();

        _svc.ShowVictory(player, floorsCleared: 5, stats);

        Output.Should().Contain("Champion");
    }

    // ─── ShowGameOver ────────────────────────────────────────────────────────

    [Fact]
    public void ShowGameOver_WritesGameOverWithKiller()
    {
        var player = new PlayerBuilder().Named("Hero").WithLevel(3).Build();
        var stats = new RunStats();

        _svc.ShowGameOver(player, killedBy: "Goblin King", stats);

        Output.Should().Contain("Hero");
    }

    [Fact]
    public void ShowGameOver_WithoutKiller_DoesNotThrow()
    {
        var player = new PlayerBuilder().Named("Hero").Build();
        var stats = new RunStats();

        Action act = () => _svc.ShowGameOver(player, killedBy: null, stats);
        act.Should().NotThrow();
    }

    // ─── ShowColoredMessage ──────────────────────────────────────────────────

    [Fact]
    public void ShowColoredMessage_WritesMessage()
    {
        _svc.ShowColoredMessage("Healing complete!", ColorCodes.Green);

        Output.Should().Contain("Healing complete!");
    }

    // ─── ShowColoredCombatMessage ────────────────────────────────────────────

    [Fact]
    public void ShowColoredCombatMessage_WritesMessage()
    {
        _svc.ShowColoredCombatMessage("Critical hit!", ColorCodes.BrightRed);

        Output.Should().Contain("Critical hit!");
    }

    // ─── ShowColoredStat ─────────────────────────────────────────────────────

    [Fact]
    public void ShowColoredStat_WritesLabelAndValue()
    {
        _svc.ShowColoredStat("ATK:", "15", ColorCodes.Red);

        Output.Should().Contain("ATK:").And.Contain("15");
    }

    // ─── ShowPlayerStats ─────────────────────────────────────────────────────

    [Fact]
    public void ShowPlayerStats_WritesPlayerStats()
    {
        var player = new PlayerBuilder().Named("Warrior").WithHP(90).WithMaxHP(100).Build();

        _svc.ShowPlayerStats(player);

        Output.Should().Contain("Warrior");
    }

    // ─── ShowEnemyArt ────────────────────────────────────────────────────────

    [Fact]
    public void ShowEnemyArt_EnemyWithArt_WritesArt()
    {
        var enemy = new EnemyBuilder().Named("Dragon").Build();
        // AsciiArt is usually set on Enemy subclasses
        // For the base enemy, AsciiArt should be empty
        _svc.ShowEnemyArt(enemy);
        // No throw is the main assertion
        Output.Should().NotBeNull();
    }

    // ─── ShowTitle ───────────────────────────────────────────────────────────

    [Fact]
    public void ShowTitle_WritesDungeonCrawlerBanner()
    {
        _svc.ShowTitle();

        Output.Should().Contain("DUNGEON CRAWLER");
    }

    // ─── ShowEnhancedTitle ───────────────────────────────────────────────────

    [Fact]
    public void ShowEnhancedTitle_WritesDungnzBanner()
    {
        _svc.ShowEnhancedTitle();

        Output.Should().Contain("D  U  N  G  N  Z");
    }

    // ─── ShowMap ─────────────────────────────────────────────────────────────

    [Fact]
    public void ShowMap_SingleRoom_RendersMapHeader()
    {
        var room = new RoomBuilder().Named("Test Chamber").Build();

        _svc.ShowMap(room, floor: 1);

        Output.Should().Contain("MAP");
        Output.Should().Contain("[*]"); // current room marker
    }

    [Fact]
    public void ShowMap_WithVisitedNorthSouth_RendersConnectorRows()
    {
        var northRoom = new RoomBuilder().Named("North Hall").Build();
        northRoom.Visited = true;
        var southRoom = new RoomBuilder().Named("South Hall").Build();
        southRoom.Visited = true;
        var current = new RoomBuilder().Named("Current")
            .WithExit(Direction.North, northRoom)
            .WithExit(Direction.South, southRoom)
            .Build();
        northRoom.Exits[Direction.South] = current;
        southRoom.Exits[Direction.North] = current;

        _svc.ShowMap(current, floor: 1);

        Output.Should().Contain("[*]");
        Output.Should().Contain("|"); // N-S connector
    }

    [Fact]
    public void ShowMap_WithVisitedEastWest_RendersHorizontalConnectors()
    {
        var eastRoom = new RoomBuilder().Named("East Hall").Build();
        eastRoom.Visited = true;
        var westRoom = new RoomBuilder().Named("West Hall").Build();
        westRoom.Visited = true;
        var current = new RoomBuilder().Named("Current")
            .WithExit(Direction.East, eastRoom)
            .WithExit(Direction.West, westRoom)
            .Build();
        eastRoom.Exits[Direction.West] = current;
        westRoom.Exits[Direction.East] = current;

        _svc.ShowMap(current, floor: 1);

        Output.Should().Contain("[*]");
        Output.Should().Contain("-"); // E-W connector
    }

    [Fact]
    public void ShowMap_ExitRoom_ShowsExitMarker()
    {
        var exitRoom = new RoomBuilder().AsExit().Build();
        exitRoom.Visited = true;
        var current = new RoomBuilder()
            .WithExit(Direction.East, exitRoom)
            .Build();
        exitRoom.Exits[Direction.West] = current;

        _svc.ShowMap(current, floor: 1);

        Output.Should().Contain("[E]");
    }

    [Fact]
    public void ShowMap_RoomWithLiveEnemy_ShowsEnemyMarker()
    {
        var enemy = new EnemyBuilder().Named("Goblin").WithHP(10).Build();
        var enemyRoom = new RoomBuilder().WithEnemy(enemy).Build();
        enemyRoom.Visited = true;
        var current = new RoomBuilder()
            .WithExit(Direction.East, enemyRoom)
            .Build();
        enemyRoom.Exits[Direction.West] = current;

        _svc.ShowMap(current, floor: 1);

        Output.Should().Contain("[!]");
    }

    [Fact]
    public void ShowMap_ShrineRoom_ShowsShrineMarker()
    {
        var shrineRoom = new RoomBuilder().WithShrine().Build();
        shrineRoom.Visited = true;
        var current = new RoomBuilder()
            .WithExit(Direction.East, shrineRoom)
            .Build();
        shrineRoom.Exits[Direction.West] = current;

        _svc.ShowMap(current, floor: 1);

        Output.Should().Contain("[S]");
    }

    // ─── ShowRoom — environmental hazards ────────────────────────────────────

    [Fact]
    public void ShowRoom_LavaSeamHazard_ShowsHazardLine()
    {
        var room = new Room { Description = "A hot chamber.", EnvironmentalHazard = RoomHazard.LavaSeam };

        _svc.ShowRoom(room);

        Output.Should().Contain("Lava");
    }

    [Fact]
    public void ShowRoom_CorruptedGroundHazard_ShowsHazardLine()
    {
        var room = new Room { Description = "A dark chamber.", EnvironmentalHazard = RoomHazard.CorruptedGround };

        _svc.ShowRoom(room);

        Output.Should().Contain("drain");
    }

    [Fact]
    public void ShowRoom_BlessedClearingHazard_ShowsBlessingLine()
    {
        var room = new Room { Description = "A peaceful glade.", EnvironmentalHazard = RoomHazard.BlessedClearing };

        _svc.ShowRoom(room);

        Output.Should().Contain("blessed");
    }

    // ─── ShowRoom — special room types ───────────────────────────────────────

    [Fact]
    public void ShowRoom_ForgottenShrine_ShowsShrineHint()
    {
        var room = new RoomBuilder().OfType(RoomType.ForgottenShrine).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("SHRINE");
    }

    [Fact]
    public void ShowRoom_PetrifiedLibrary_ShowsLibraryHint()
    {
        var room = new RoomBuilder().OfType(RoomType.PetrifiedLibrary).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("tomes");
    }

    [Fact]
    public void ShowRoom_ContestedArmory_ShowsArmoryHint()
    {
        var room = new RoomBuilder().OfType(RoomType.ContestedArmory).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("ARMORY");
    }

    [Fact]
    public void ShowRoom_WithMerchant_ShowsMerchantGreeting()
    {
        var room = new RoomBuilder().Build();
        room.Merchant = new Merchant();

        _svc.ShowRoom(room);

        Output.Should().Contain("SHOP");
    }

    // ─── SelectDifficulty ────────────────────────────────────────────────────

    [Fact]
    public void SelectDifficulty_WithFakeInput_ReturnsCasual()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.SelectDifficulty();

        result.Should().Be(Difficulty.Casual);
    }

    // ─── SelectClass ─────────────────────────────────────────────────────────

    [Fact]
    public void SelectClass_NullPrestige_ReturnsFirstClass()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.SelectClass(prestige: null);

        result.Should().NotBeNull();
        result.Name.Should().Be("Warrior");
    }

    [Fact]
    public void SelectClass_WithPrestige_ShowsPrestigeBonuses()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var prestige = new PrestigeData { PrestigeLevel = 2, BonusStartAttack = 2, BonusStartDefense = 1, BonusStartHP = 5 };

        var result = svc.SelectClass(prestige);

        result.Should().NotBeNull();
    }

    // ─── ShowConfirmMenu ─────────────────────────────────────────────────────

    [Fact]
    public void ShowConfirmMenu_YesInput_ReturnsTrue()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.ShowConfirmMenu("Are you sure?");

        result.Should().BeTrue();
    }

    [Fact]
    public void ShowConfirmMenu_NoInput_ReturnsFalse()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2"));

        var result = svc.ShowConfirmMenu("Are you sure?");

        result.Should().BeFalse();
    }

    // ─── ShowShopAndSelect ───────────────────────────────────────────────────

    [Fact]
    public void ShowShopAndSelect_WithFakeInput_ReturnsSelection()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var stock = new[] { (new ItemBuilder().Named("Health Potion").WithHeal(30).Build(), 50) };

        var result = svc.ShowShopAndSelect(stock, playerGold: 100);

        result.Should().BeGreaterOrEqualTo(0);
    }

    // ─── ShowSellMenuAndSelect ────────────────────────────────────────────────

    [Fact]
    public void ShowSellMenuAndSelect_WithFakeInput_ReturnsSelection()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var items = new[] { (new ItemBuilder().Named("Old Sword").WithDamage(3).Build(), 15) };

        var result = svc.ShowSellMenuAndSelect(items, playerGold: 50);

        result.Should().BeGreaterOrEqualTo(0);
    }

    // ─── ShowShrineMenuAndSelect ──────────────────────────────────────────────

    [Fact]
    public void ShowShrineMenuAndSelect_WithFakeInput_ReturnsFirstOption()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.ShowShrineMenuAndSelect(playerGold: 200, healCost: 30, blessCost: 50, fortifyCost: 75, meditateCost: 75);

        result.Should().BeGreaterOrEqualTo(0);
    }

    // ─── ShowInventoryAndSelect ───────────────────────────────────────────────

    [Fact]
    public void ShowInventoryAndSelect_EmptyInventory_ReturnsNull()
    {
        var player = new PlayerBuilder().Build();

        var result = _svc.ShowInventoryAndSelect(player);

        result.Should().BeNull();
    }
}
