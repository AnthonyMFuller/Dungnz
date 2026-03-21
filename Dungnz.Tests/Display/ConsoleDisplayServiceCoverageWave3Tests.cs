using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests.Display;

/// <summary>
/// Wave 3 targeted coverage tests for ConsoleDisplayService.
/// Targets uncovered methods to push Dungnz.Display above 80% line coverage.
/// Uses Console.SetOut capture pattern with [Collection("console-output")].
/// </summary>
[Collection("console-output")]
public sealed class ConsoleDisplayServiceCoverageWave3Tests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly TextReader _originalIn;
    private readonly ConsoleDisplayService _svc;

    public ConsoleDisplayServiceCoverageWave3Tests()
    {
        _originalOut = Console.Out;
        _originalIn = Console.In;
        _output = new StringWriter();
        Console.SetOut(_output);
        _svc = new ConsoleDisplayService();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetIn(_originalIn);
        _output.Dispose();
    }

    private string Output => _output.ToString();

    // ── ShowRoom: Room-type color prefixes (lines 47-55) ──────────────────

    [Fact]
    public void ShowRoom_DarkType_WritesDarkPrefix()
    {
        var room = new RoomBuilder().Named("A dark chamber").OfType(RoomType.Dark).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("dark");
    }

    [Fact]
    public void ShowRoom_ScorchedType_WritesScorchedWarning()
    {
        var room = new RoomBuilder().Named("A burned hall").OfType(RoomType.Scorched).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("Scorch");
    }

    [Fact]
    public void ShowRoom_FloodedType_WritesFloodedWarning()
    {
        var room = new RoomBuilder().Named("A wet cave").OfType(RoomType.Flooded).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("water");
    }

    [Fact]
    public void ShowRoom_MossyType_WritesMossyPrefix()
    {
        var room = new RoomBuilder().Named("A mossy room").OfType(RoomType.Mossy).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("moss");
    }

    [Fact]
    public void ShowRoom_AncientType_WritesAncientPrefix()
    {
        var room = new RoomBuilder().Named("An old room").OfType(RoomType.Ancient).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("runes");
    }

    [Fact]
    public void ShowRoom_ForgottenShrineType_WritesShrinePrefix()
    {
        var room = new RoomBuilder().Named("A shrine room").OfType(RoomType.ForgottenShrine).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("shrine");
    }

    [Fact]
    public void ShowRoom_PetrifiedLibraryType_WritesLibraryPrefix()
    {
        var room = new RoomBuilder().Named("A library room").OfType(RoomType.PetrifiedLibrary).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("bookshelves");
    }

    [Fact]
    public void ShowRoom_ContestedArmoryType_WritesArmoryPrefix()
    {
        var room = new RoomBuilder().Named("An armory room").OfType(RoomType.ContestedArmory).Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("Weapon");
    }

    // ── ShowRoom: Environmental hazards (lines 65-73) ─────────────────────

    [Fact]
    public void ShowRoom_LavaSeamHazard_WritesLavaWarning()
    {
        var room = new RoomBuilder().Named("A hot room").Build();
        room.EnvironmentalHazard = RoomHazard.LavaSeam;

        _svc.ShowRoom(room);

        Output.Should().Contain("Lava");
    }

    [Fact]
    public void ShowRoom_CorruptedGroundHazard_WritesCorruptedWarning()
    {
        var room = new RoomBuilder().Named("A corrupted room").Build();
        room.EnvironmentalHazard = RoomHazard.CorruptedGround;

        _svc.ShowRoom(room);

        Output.Should().Contain("dark energy");
    }

    [Fact]
    public void ShowRoom_BlessedClearingHazard_WritesBlessedMessage()
    {
        var room = new RoomBuilder().Named("A blessed room").Build();
        room.EnvironmentalHazard = RoomHazard.BlessedClearing;

        _svc.ShowRoom(room);

        Output.Should().Contain("blessed");
    }

    // ── ShowRoom: Shrine and special rooms (lines 119-142) ────────────────

    [Fact]
    public void ShowRoom_WithShrine_NonForgotten_WritesUseShrine()
    {
        var room = new RoomBuilder().Named("A room with shrine").OfType(RoomType.Standard).WithShrine().Build();

        _svc.ShowRoom(room);

        Output.Should().Contain("USE SHRINE");
    }

    [Fact]
    public void ShowRoom_ForgottenShrineNotUsed_WritesForgottenShrineHint()
    {
        var room = new RoomBuilder().Named("A forgotten shrine").OfType(RoomType.ForgottenShrine).Build();
        room.SpecialRoomUsed = false;

        _svc.ShowRoom(room);

        Output.Should().Contain("forgotten shrine");
    }

    [Fact]
    public void ShowRoom_PetrifiedLibraryNotUsed_WritesLibraryHint()
    {
        var room = new RoomBuilder().Named("A petrified library").OfType(RoomType.PetrifiedLibrary).Build();
        room.SpecialRoomUsed = false;

        _svc.ShowRoom(room);

        Output.Should().Contain("Ancient tomes");
    }

    [Fact]
    public void ShowRoom_ContestedArmoryNotUsed_WritesArmoryHint()
    {
        var room = new RoomBuilder().Named("A contested armory").OfType(RoomType.ContestedArmory).Build();
        room.SpecialRoomUsed = false;

        _svc.ShowRoom(room);

        Output.Should().Contain("USE ARMORY");
    }

    [Fact]
    public void ShowRoom_WithMerchant_WritesShopHint()
    {
        var room = new RoomBuilder().Named("A merchant room").Build();
        room.Merchant = new Merchant();

        _svc.ShowRoom(room);

        Output.Should().Contain("SHOP");
    }

    // ── ShowShop (lines 534-572) ──────────────────────────────────────────

    [Fact]
    public void ShowShop_WithStock_WritesItemNamesAndPrices()
    {
        var sword = new ItemBuilder().Named("Iron Sword").WithDamage(5).WithTier(ItemTier.Common).Build();
        var shield = new ItemBuilder().Named("Wood Shield").WithDefense(3).WithTier(ItemTier.Uncommon).Build();
        var stock = new[] { (sword, 50), (shield, 30) };

        _svc.ShowShop(stock, playerGold: 100);

        Output.Should().Contain("Iron Sword");
        Output.Should().Contain("Wood Shield");
        Output.Should().Contain("100g");
    }

    [Fact]
    public void ShowShop_ExpensiveItem_ShowsRedPrice()
    {
        var item = new ItemBuilder().Named("Legendary Blade").WithDamage(20).WithTier(ItemTier.Legendary).Build();
        var stock = new[] { (item, 999) };

        _svc.ShowShop(stock, playerGold: 10);

        Output.Should().Contain("Legendary Blade");
        Output.Should().Contain("999 gold");
    }

    // ── ShowSellMenu (lines 579-603) ──────────────────────────────────────

    [Fact]
    public void ShowSellMenu_WithItems_WritesItemNamesAndSellPrices()
    {
        var sword = new ItemBuilder().Named("Old Sword").WithDamage(3).WithTier(ItemTier.Common).Build();
        var armor = new ItemBuilder().Named("Rusty Armor").WithDefense(2).WithTier(ItemTier.Common).Build();
        var items = new[] { (sword, 10), (armor, 8) };

        _svc.ShowSellMenu(items, playerGold: 50);

        Output.Should().Contain("SELL ITEMS");
        Output.Should().Contain("Old Sword");
        Output.Should().Contain("Rusty Armor");
        Output.Should().Contain("50g");
    }

    [Fact]
    public void ShowSellMenu_TieredItems_WritesTierColors()
    {
        var rare = new ItemBuilder().Named("Rare Blade").WithDamage(8).WithTier(ItemTier.Rare).Build();
        var epic = new ItemBuilder().Named("Epic Shield").WithDefense(10).WithTier(ItemTier.Epic).Build();
        var items = new[] { (rare, 25), (epic, 50) };

        _svc.ShowSellMenu(items, playerGold: 100);

        Output.Should().Contain("Rare Blade");
        Output.Should().Contain("Epic Shield");
    }

    // ── ShowCraftRecipe (lines 610-637) ───────────────────────────────────

    [Fact]
    public void ShowCraftRecipe_WithIngredients_WritesRecipeCard()
    {
        var result = new ItemBuilder().Named("Iron Sword").WithDamage(8).Build();
        var ingredients = new List<(string ingredient, bool playerHasIt)>
        {
            ("Iron Ore", true),
            ("Wood Plank", false),
            ("Leather Strip", true)
        };

        _svc.ShowCraftRecipe("Iron Sword Recipe", result, ingredients);

        Output.Should().Contain("RECIPE");
        Output.Should().Contain("Iron Sword");
        Output.Should().Contain("Iron Ore");
        Output.Should().Contain("Wood Plank");
        Output.Should().Contain("Leather Strip");
    }

    // ── ShowCommandPrompt with player (lines 878-889) ─────────────────────

    [Fact]
    public void ShowCommandPrompt_WithPlayer_WritesHPBar()
    {
        var player = new PlayerBuilder().WithHP(80).WithMaxHP(100).Build();

        _svc.ShowCommandPrompt(player);

        Output.Should().Contain("HP");
    }

    [Fact]
    public void ShowCommandPrompt_WithPlayerWithMana_WritesMPBar()
    {
        var player = new PlayerBuilder().WithHP(80).WithMaxHP(100).WithMana(20).WithMaxMana(40).Build();

        _svc.ShowCommandPrompt(player);

        Output.Should().Contain("HP");
        Output.Should().Contain("MP");
    }

    // ── ShowIntroNarrative (lines 1181-1196) ──────────────────────────────

    [Fact]
    public void ShowIntroNarrative_WritesLoreAndReturnsFalse()
    {
        Console.SetIn(new StringReader("\n")); // Simulate Enter press

        var result = _svc.ShowIntroNarrative();

        result.Should().BeFalse();
        Output.Should().Contain("Dungnz");
        Output.Should().Contain("descent");
    }

    // ── ShowEnhancedTitle (lines 1158-1175) ───────────────────────────────

    [Fact]
    public void ShowEnhancedTitle_WritesColoredBanner()
    {
        _svc.ShowEnhancedTitle();

        Output.Should().Contain("D  U  N  G  N  Z");
        Output.Should().Contain("Descend If You Dare");
    }

    // ── ShowPrestigeInfo (lines 1201-1221) ────────────────────────────────

    [Fact]
    public void ShowPrestigeInfo_WritesPrestigeLevelAndBonuses()
    {
        var prestige = new PrestigeData
        {
            PrestigeLevel = 3,
            TotalWins = 5,
            TotalRuns = 10,
            BonusStartAttack = 3,
            BonusStartDefense = 2,
            BonusStartHP = 10
        };

        _svc.ShowPrestigeInfo(prestige);

        Output.Should().Contain("PRESTIGE LEVEL 3");
        Output.Should().Contain("Wins: 5");
        Output.Should().Contain("Bonus Attack");
        Output.Should().Contain("Bonus Defense");
        Output.Should().Contain("Bonus HP");
    }

    [Fact]
    public void ShowPrestigeInfo_NoBonuses_OmitsBonusLines()
    {
        var prestige = new PrestigeData
        {
            PrestigeLevel = 1,
            TotalWins = 1,
            TotalRuns = 2,
        };

        _svc.ShowPrestigeInfo(prestige);

        Output.Should().Contain("PRESTIGE LEVEL 1");
        Output.Should().NotContain("Bonus Attack");
    }

    // ── ShowFloorBanner (lines 1403-1423) ─────────────────────────────────

    [Fact]
    public void ShowFloorBanner_LowFloor_WritesLowDanger()
    {
        var variant = DungeonVariant.ForFloor(1);

        _svc.ShowFloorBanner(1, 8, variant);

        Output.Should().Contain("Floor 1 of 8");
        Output.Should().Contain("Low");
    }

    [Fact]
    public void ShowFloorBanner_MidFloor_WritesModerateDanger()
    {
        var variant = DungeonVariant.ForFloor(3);

        _svc.ShowFloorBanner(3, 8, variant);

        Output.Should().Contain("Floor 3 of 8");
        Output.Should().Contain("Moderate");
    }

    [Fact]
    public void ShowFloorBanner_HighFloor_WritesHighDanger()
    {
        var variant = DungeonVariant.ForFloor(5);

        _svc.ShowFloorBanner(5, 8, variant);

        Output.Should().Contain("Floor 5 of 8");
        Output.Should().Contain("High");
    }

    // ── ShowEnemyDetail (lines 1426-1445) ─────────────────────────────────

    [Fact]
    public void ShowEnemyDetail_NormalEnemy_WritesEnemyStats()
    {
        var enemy = new EnemyBuilder().Named("Goblin").WithHP(30).WithAttack(8).WithDefense(3).WithXP(15).Build();

        _svc.ShowEnemyDetail(enemy);

        Output.Should().Contain("GOBLIN");
        Output.Should().Contain("ATK:");
        Output.Should().Contain("DEF:");
        Output.Should().Contain("XP:");
    }

    [Fact]
    public void ShowEnemyDetail_EliteEnemy_WritesEliteBadge()
    {
        var enemy = new EnemyBuilder().Named("Elite Orc").WithHP(80).WithAttack(15).WithDefense(6).WithXP(40).AsElite().Build();

        _svc.ShowEnemyDetail(enemy);

        Output.Should().Contain("ELITE");
    }

    // ── ShowVictory (lines 1448-1466) ─────────────────────────────────────

    [Fact]
    public void ShowVictory_WritesVictoryBannerAndStats()
    {
        var player = new PlayerBuilder().Named("Hero").WithLevel(5).Build();
        var stats = new RunStats { EnemiesDefeated = 20, GoldCollected = 150, ItemsFound = 8, TurnsTaken = 100 };

        _svc.ShowVictory(player, 5, stats);

        Output.Should().Contain("V I C T O R Y");
        Output.Should().Contain("Hero");
        Output.Should().Contain("Level 5");
        Output.Should().Contain("5 floors conquered");
        Output.Should().Contain("20");
        Output.Should().Contain("150");
    }

    [Fact]
    public void ShowVictory_SingleFloor_NoPlural()
    {
        var player = new PlayerBuilder().Named("Solo").WithLevel(1).Build();
        var stats = new RunStats { EnemiesDefeated = 3, GoldCollected = 20, ItemsFound = 2, TurnsTaken = 15 };

        _svc.ShowVictory(player, 1, stats);

        Output.Should().Contain("1 floor conquered");
        Output.Should().NotContain("1 floors");
    }

    // ── ShowGameOver (lines 1469-1487) ────────────────────────────────────

    [Fact]
    public void ShowGameOver_WithKiller_WritesDeathCause()
    {
        var player = new PlayerBuilder().Named("Fallen").WithLevel(3).Build();
        var stats = new RunStats { EnemiesDefeated = 5, FloorsVisited = 2, TurnsTaken = 30 };

        _svc.ShowGameOver(player, "Dragon", stats);

        Output.Should().Contain("G A M E  O V E R");
        Output.Should().Contain("Fallen");
        Output.Should().Contain("Dragon");
    }

    [Fact]
    public void ShowGameOver_NullKiller_WritesUnknownCause()
    {
        var player = new PlayerBuilder().Named("Unknown").WithLevel(1).Build();
        var stats = new RunStats { EnemiesDefeated = 1, FloorsVisited = 1, TurnsTaken = 10 };

        _svc.ShowGameOver(player, null, stats);

        Output.Should().Contain("Cause of death: unknown");
    }

    // ── ShowEnemyArt (lines 1490-1511) ────────────────────────────────────

    [Fact]
    public void ShowEnemyArt_WithArt_WritesArtBox()
    {
        // Use an actual enemy subclass that has art, or construct manually
        var goblin = new Goblin();  // should have AsciiArt set

        _svc.ShowEnemyArt(goblin);

        if (goblin.AsciiArt.Length > 0)
        {
            Output.Should().Contain("┌");
            Output.Should().Contain("└");
        }
    }

    [Fact]
    public void ShowEnemyArt_NoArt_WritesNothing()
    {
        var enemy = new EnemyBuilder().Named("Generic").Build();

        _svc.ShowEnemyArt(enemy);

        Output.Should().BeEmpty();
    }

    // ── ShowCombatStart (lines 1363-1374) ─────────────────────────────────

    [Fact]
    public void ShowCombatStart_WritesEnemyNameAndBanner()
    {
        var enemy = new EnemyBuilder().Named("Dark Knight").Build();

        _svc.ShowCombatStart(enemy);

        Output.Should().Contain("COMBAT BEGINS");
        Output.Should().Contain("Dark Knight");
    }

    // ── ShowCombatEntryFlags (lines 1377-1384) ────────────────────────────

    [Fact]
    public void ShowCombatEntryFlags_EliteEnemy_WritesEliteBadge()
    {
        var enemy = new EnemyBuilder().Named("Elite Orc").AsElite().Build();

        _svc.ShowCombatEntryFlags(enemy);

        Output.Should().Contain("ELITE");
    }

    [Fact]
    public void ShowCombatEntryFlags_NormalEnemy_WritesNothing()
    {
        var enemy = new EnemyBuilder().Named("Goblin").Build();

        _svc.ShowCombatEntryFlags(enemy);

        Output.Should().BeEmpty();
    }

    // ── ShowLevelUpChoice (lines 1387-1400) ───────────────────────────────

    [Fact]
    public void ShowLevelUpChoice_WritesStatOptions()
    {
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).WithAttack(10).WithDefense(5).Build();

        _svc.ShowLevelUpChoice(player);

        Output.Should().Contain("LEVEL UP");
        Output.Should().Contain("[1]");
        Output.Should().Contain("[2]");
        Output.Should().Contain("[3]");
        Output.Should().Contain("+5 Max HP");
        Output.Should().Contain("+2 Attack");
        Output.Should().Contain("+2 Defense");
    }

    // ── ShowCombatHistory (lines 1514-1518) ───────────────────────────────

    [Fact]
    public void ShowCombatHistory_WritesHistoryNotice()
    {
        _svc.ShowCombatHistory();

        Output.Should().Contain("Combat History");
    }

    // ── ShowEquipment (lines 1149-1153) ───────────────────────────────────

    [Fact]
    public void ShowEquipment_WritesEquipmentHeader()
    {
        var player = new PlayerBuilder().Build();

        _svc.ShowEquipment(player);

        Output.Should().Contain("EQUIPMENT");
    }

    // ── ShowItemDetail: coverage for item description word-wrap (lines 506-526) ──

    [Fact]
    public void ShowItemDetail_WithDescription_WordWraps()
    {
        var item = new ItemBuilder()
            .Named("Enchanted Staff")
            .WithDamage(12)
            .WithTier(ItemTier.Epic)
            .WithDescription("A powerful staff forged in the heart of a volcano by ancient dwarven smiths who spent decades perfecting their craft")
            .Build();

        _svc.ShowItemDetail(item);

        Output.Should().Contain("ENCHANTED STAFF");
        Output.Should().Contain("Attack:");
        Output.Should().Contain("ancient");
    }

    [Fact]
    public void ShowItemDetail_Consumable_WritesHealAmount()
    {
        var item = new ItemBuilder()
            .Named("Health Potion")
            .WithHeal(25)
            .WithTier(ItemTier.Common)
            .Build();

        _svc.ShowItemDetail(item);

        Output.Should().Contain("HEALTH POTION");
        Output.Should().Contain("Heal:");
        Output.Should().Contain("25");
    }

    // ── ShowInventoryAndSelect with items (lines 309-319) ─────────────────

    [Fact]
    public void ShowInventoryAndSelect_WithItems_CancelInput_ReturnsNull()
    {
        Console.SetIn(new StringReader("x\n"));
        var player = new PlayerBuilder()
            .WithItem(new ItemBuilder().Named("Sword").WithDamage(5).Build())
            .Build();

        var result = _svc.ShowInventoryAndSelect(player);

        result.Should().BeNull();
    }

    [Fact]
    public void ShowInventoryAndSelect_WithItems_ValidIndex_ReturnsItem()
    {
        Console.SetIn(new StringReader("1\n"));
        var sword = new ItemBuilder().Named("Sword").WithDamage(5).Build();
        var player = new PlayerBuilder().WithItem(sword).Build();

        var result = _svc.ShowInventoryAndSelect(player);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Sword");
    }

    [Fact]
    public void ShowInventoryAndSelect_WithItems_InvalidIndex_ReturnsNull()
    {
        Console.SetIn(new StringReader("99\n"));
        var player = new PlayerBuilder()
            .WithItem(new ItemBuilder().Named("Sword").WithDamage(5).Build())
            .Build();

        var result = _svc.ShowInventoryAndSelect(player);

        result.Should().BeNull();
    }

    // ── ReadSeed (lines 811-823) ──────────────────────────────────────────

    [Fact]
    public void ReadSeed_ValidSeed_ReturnsSeed()
    {
        Console.SetIn(new StringReader("123456\n"));

        var result = _svc.ReadSeed();

        result.Should().Be(123456);
    }

    [Fact]
    public void ReadSeed_CancelInput_ReturnsNull()
    {
        Console.SetIn(new StringReader("cancel\n"));

        var result = _svc.ReadSeed();

        result.Should().BeNull();
    }

    [Fact]
    public void ReadSeed_InvalidThenValid_ReturnsValid()
    {
        Console.SetIn(new StringReader("abc\n654321\n"));

        var result = _svc.ReadSeed();

        result.Should().Be(654321);
    }

    [Fact]
    public void ReadSeed_EmptyInput_ReturnsNull()
    {
        Console.SetIn(new StringReader("\n"));

        var result = _svc.ReadSeed();

        result.Should().BeNull();
    }

    // ── ShowColoredMessage/ShowColoredCombatMessage/ShowColoredStat ───────

    [Fact]
    public void ShowColoredMessage_WritesMessage()
    {
        _svc.ShowColoredMessage("Test message", Systems.ColorCodes.Green);

        Output.Should().Contain("Test message");
    }

    [Fact]
    public void ShowColoredCombatMessage_WritesMessage()
    {
        _svc.ShowColoredCombatMessage("Combat text", Systems.ColorCodes.Red);

        Output.Should().Contain("Combat text");
    }

    [Fact]
    public void ShowColoredStat_WritesLabelAndValue()
    {
        _svc.ShowColoredStat("HP", "100", Systems.ColorCodes.Green);

        Output.Should().Contain("HP");
        Output.Should().Contain("100");
    }

    // ── ShowItemPickup: overweight warning (line 462-463) ─────────────────

    [Fact]
    public void ShowItemPickup_NearCapacity_WritesWeightWarning()
    {
        var item = new ItemBuilder().Named("Heavy Rock").WithDamage(1).Build();

        _svc.ShowItemPickup(item, slotsCurrent: 9, slotsMax: 10, weightCurrent: 85, weightMax: 100);

        Output.Should().Contain("Heavy Rock");
        Output.Should().Contain("nearly full");
    }

    [Fact]
    public void ShowItemPickup_LowCapacity_NoWarning()
    {
        var item = new ItemBuilder().Named("Feather").WithDamage(1).Build();

        _svc.ShowItemPickup(item, slotsCurrent: 2, slotsMax: 10, weightCurrent: 10, weightMax: 100);

        Output.Should().Contain("Feather");
        Output.Should().NotContain("nearly full");
    }

    // ── ShowGoldPickup (lines 440-443) ────────────────────────────────────

    [Fact]
    public void ShowGoldPickup_WritesAmountAndTotal()
    {
        _svc.ShowGoldPickup(25, 100);

        Output.Should().Contain("+25 gold");
        Output.Should().Contain("Total: 100g");
    }

    // ── ShowEquipmentComparison: defense delta path (lines 1133-1142) ─────

    [Fact]
    public void ShowEquipmentComparison_DefenseDelta_WritesComparison()
    {
        var player = new PlayerBuilder().WithAttack(10).WithDefense(5).Build();
        var oldItem = new ItemBuilder().Named("Old Shield").WithDefense(2).Build();
        var newItem = new ItemBuilder().Named("New Shield").WithDefense(5).Build();

        _svc.ShowEquipmentComparison(player, oldItem, newItem);

        Output.Should().Contain("EQUIPMENT COMPARISON");
        Output.Should().Contain("Old Shield");
        Output.Should().Contain("New Shield");
    }

    [Fact]
    public void ShowEquipmentComparison_NullOldItem_WritesNone()
    {
        var player = new PlayerBuilder().WithAttack(10).WithDefense(5).Build();
        var newItem = new ItemBuilder().Named("New Sword").WithDamage(8).Build();

        _svc.ShowEquipmentComparison(player, null, newItem);

        Output.Should().Contain("(none)");
        Output.Should().Contain("New Sword");
    }

    // ── RefreshDisplay (lines 1843-1849) ──────────────────────────────────

    [Fact]
    public void RefreshDisplay_WritesStatsRoomAndMap()
    {
        var player = new PlayerBuilder().Named("Hero").WithHP(80).WithMaxHP(100).Build();
        var room = new RoomBuilder().Named("Main Hall").Build();

        _svc.RefreshDisplay(player, room, 1);

        Output.Should().Contain("Hero");
        Output.Should().Contain("Main Hall");
    }

    // ── ShowSkillTreeMenu (lines 1827-1840) ───────────────────────────────

    [Fact]
    public void ShowSkillTreeMenu_WritesSkillList()
    {
        var player = new PlayerBuilder().WithLevel(3).Build();

        var result = _svc.ShowSkillTreeMenu(player);

        result.Should().BeNull();
        Output.Should().Contain("SKILL TREE");
    }
}

/// <summary>
/// Tests for menu-selection methods on ConsoleDisplayService.
/// These methods use FakeInputReader and the SelectFromMenu fallback.
/// </summary>
[Collection("console-output")]
public sealed class ConsoleDisplayServiceMenuSelectTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;

    public ConsoleDisplayServiceMenuSelectTests()
    {
        _originalOut = Console.Out;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    private string Output => _output.ToString();

    // ── ShowCombatMenuAndSelect (lines 1603-1627) ─────────────────────────

    [Fact]
    public void ShowCombatMenuAndSelect_AttackInput_ReturnsA()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).WithMana(30).WithMaxMana(30).Build();
        var enemy = new EnemyBuilder().Named("Goblin").Build();

        var result = svc.ShowCombatMenuAndSelect(player, enemy);

        result.Should().Be("A");
        Output.Should().Contain("Mana:");
    }

    [Fact]
    public void ShowCombatMenuAndSelect_RoguePlayer_ShowsComboPoints()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var player = new PlayerBuilder()
            .WithHP(100).WithMaxHP(100).WithMana(30).WithMaxMana(30)
            .WithClass(PlayerClass.Rogue)
            .Build();
        var enemy = new EnemyBuilder().Named("Goblin").Build();

        var result = svc.ShowCombatMenuAndSelect(player, enemy);

        result.Should().Be("A");
        Output.Should().Contain("Combo");
    }

    [Fact]
    public void ShowCombatMenuAndSelect_FleeInput_ReturnsF()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3"));
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).WithMana(30).WithMaxMana(30).Build();
        var enemy = new EnemyBuilder().Named("Goblin").Build();

        var result = svc.ShowCombatMenuAndSelect(player, enemy);

        result.Should().Be("F");
    }

    [Fact]
    public void ShowCombatMenuAndSelect_UseItemInput_ReturnsI()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("4"));
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).WithMana(30).WithMaxMana(30).Build();
        var enemy = new EnemyBuilder().Named("Goblin").Build();

        var result = svc.ShowCombatMenuAndSelect(player, enemy);

        result.Should().Be("I");
    }

    // ── ShowCraftMenuAndSelect (lines 1635-1647) ──────────────────────────

    [Fact]
    public void ShowCraftMenuAndSelect_SelectRecipe_ReturnsIndex()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var recipes = new[] { ("Iron Sword", true), ("Gold Ring", false) };

        var result = svc.ShowCraftMenuAndSelect(recipes);

        result.Should().Be(1);
        Output.Should().Contain("CRAFTING");
    }

    [Fact]
    public void ShowCraftMenuAndSelect_Cancel_ReturnsZero()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3")); // cancel = last option
        var recipes = new[] { ("Iron Sword", true), ("Gold Ring", false) };

        var result = svc.ShowCraftMenuAndSelect(recipes);

        result.Should().Be(0);
    }

    // ── ShowAbilityMenuAndSelect (lines 1697-1718) ────────────────────────

    [Fact]
    public void ShowAbilityMenuAndSelect_WithAvailableAbility_ReturnsAbility()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var ability = new Ability("Fireball", "Launches a fireball", 15, 2, 1, AbilityType.PowerStrike);
        var unavailable = new (Ability, bool, int, bool)[]
        {
            (new Ability("Shield", "Block", 10, 3, 1, AbilityType.DefensiveStance), true, 2, false)
        };

        var result = svc.ShowAbilityMenuAndSelect(unavailable, new[] { ability });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Fireball");
    }

    [Fact]
    public void ShowAbilityMenuAndSelect_Cancel_ReturnsNull()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2"));
        var ability = new Ability("Fireball", "Launches a fireball", 15, 2, 1, AbilityType.PowerStrike);

        var result = svc.ShowAbilityMenuAndSelect(
            Array.Empty<(Ability, bool, int, bool)>(),
            new[] { ability });

        result.Should().BeNull();
    }

    [Fact]
    public void ShowAbilityMenuAndSelect_UnavailableDueToMana_ShowsNeedMP()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1")); // select the available one
        var available = new Ability("Slash", "A quick slash", 5, 1, 1, AbilityType.PowerStrike);
        var unavailable = new (Ability, bool, int, bool)[]
        {
            (new Ability("Meteor", "Huge damage", 50, 5, 1, AbilityType.PowerStrike), false, 0, true)
        };

        var result = svc.ShowAbilityMenuAndSelect(unavailable, new[] { available });

        Output.Should().Contain("Need");
        Output.Should().Contain("MP");
    }

    // ── ShowEquipMenuAndSelect (lines 1738-1750) ──────────────────────────

    [Fact]
    public void ShowEquipMenuAndSelect_SelectsItem()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var sword = new ItemBuilder().Named("Iron Sword").WithDamage(5).Build();

        var result = svc.ShowEquipMenuAndSelect(new[] { sword });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Iron Sword");
    }

    [Fact]
    public void ShowEquipMenuAndSelect_Cancel_ReturnsNull()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2"));
        var sword = new ItemBuilder().Named("Iron Sword").WithDamage(5).Build();

        var result = svc.ShowEquipMenuAndSelect(new[] { sword });

        result.Should().BeNull();
    }

    // ── ShowUseMenuAndSelect (lines 1753-1765) ────────────────────────────

    [Fact]
    public void ShowUseMenuAndSelect_SelectsItem()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var potion = new ItemBuilder().Named("Health Potion").WithHeal(25).Build();

        var result = svc.ShowUseMenuAndSelect(new[] { potion });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Health Potion");
    }

    [Fact]
    public void ShowUseMenuAndSelect_Cancel_ReturnsNull()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2"));
        var potion = new ItemBuilder().Named("Health Potion").WithHeal(25).Build();

        var result = svc.ShowUseMenuAndSelect(new[] { potion });

        result.Should().BeNull();
    }

    // ── ShowTakeMenuAndSelect (lines 1768-1781) ───────────────────────────

    [Fact]
    public void ShowTakeMenuAndSelect_SelectsSingleItem()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2")); // Skip "Take All" (1st), select first item (2nd)
        var sword = new ItemBuilder().Named("Floor Sword").WithDamage(3).Build();

        var result = svc.ShowTakeMenuAndSelect(new[] { sword });

        result.Should().NotBeNull();
        result.Should().BeOfType<TakeSelection.Single>();
    }

    [Fact]
    public void ShowTakeMenuAndSelect_TakeAll_ReturnsTakeAll()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1")); // "Take All" is first
        var sword = new ItemBuilder().Named("Floor Sword").WithDamage(3).Build();

        var result = svc.ShowTakeMenuAndSelect(new[] { sword });

        result.Should().NotBeNull();
        result.Should().BeOfType<TakeSelection.All>();
    }

    [Fact]
    public void ShowTakeMenuAndSelect_Cancel_ReturnsNull()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3")); // Cancel = last option
        var sword = new ItemBuilder().Named("Floor Sword").WithDamage(3).Build();

        var result = svc.ShowTakeMenuAndSelect(new[] { sword });

        result.Should().BeNull();
    }

    // ── ShowStartupMenu (lines 1784-1798) ─────────────────────────────────

    [Fact]
    public void ShowStartupMenu_NewGame_NoSaves()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.ShowStartupMenu(hasSaves: false);

        result.Should().Be(StartupMenuOption.NewGame);
    }

    [Fact]
    public void ShowStartupMenu_NewGame_WithSaves()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.ShowStartupMenu(hasSaves: true);

        result.Should().Be(StartupMenuOption.NewGame);
    }

    [Fact]
    public void ShowStartupMenu_LoadSave_WithSaves()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2"));

        var result = svc.ShowStartupMenu(hasSaves: true);

        result.Should().Be(StartupMenuOption.LoadSave);
    }

    // ── SelectSaveToLoad (lines 1801-1808) ────────────────────────────────

    [Fact]
    public void SelectSaveToLoad_SelectsFirstSave()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.SelectSaveToLoad(new[] { "save1", "save2" });

        result.Should().Be("save1");
    }

    [Fact]
    public void SelectSaveToLoad_Cancel_ReturnsNull()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3")); // Back = last option
        
        var result = svc.SelectSaveToLoad(new[] { "save1", "save2" });

        result.Should().BeNull();
    }

    // ── ShowLevelUpChoiceAndSelect (lines 1585-1594) ──────────────────────

    [Fact]
    public void ShowLevelUpChoiceAndSelect_SelectsHP()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).WithAttack(10).WithDefense(5).Build();

        var result = svc.ShowLevelUpChoiceAndSelect(player);

        result.Should().Be(1);
    }

    [Fact]
    public void ShowLevelUpChoiceAndSelect_SelectsDefense()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3"));
        var player = new PlayerBuilder().WithHP(100).WithMaxHP(100).WithAttack(10).WithDefense(5).Build();

        var result = svc.ShowLevelUpChoiceAndSelect(player);

        result.Should().Be(3);
    }

    // ── ShowCombatItemMenuAndSelect (lines 1724-1735) ─────────────────────

    [Fact]
    public void ShowCombatItemMenuAndSelect_SelectsPotion()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));
        var potion = new ItemBuilder().Named("Healing Potion").WithHeal(30).Build();

        var result = svc.ShowCombatItemMenuAndSelect(new[] { potion });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Healing Potion");
    }

    [Fact]
    public void ShowCombatItemMenuAndSelect_Cancel_ReturnsNull()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2"));
        var potion = new ItemBuilder().Named("Healing Potion").WithHeal(30).Build();

        var result = svc.ShowCombatItemMenuAndSelect(new[] { potion });

        result.Should().BeNull();
    }

    // ── ShowTrapChoiceAndSelect (lines 1652-1661) ─────────────────────────

    [Fact]
    public void ShowTrapChoiceAndSelect_SelectsFirstOption()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.ShowTrapChoiceAndSelect("A trap!", "Disarm", "Dodge");

        result.Should().Be(1);
    }

    [Fact]
    public void ShowTrapChoiceAndSelect_SelectsLeave()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3"));

        var result = svc.ShowTrapChoiceAndSelect("A trap!", "Disarm", "Dodge");

        result.Should().Be(0);
    }

    // ── ShowForgottenShrineMenuAndSelect (lines 1666-1676) ────────────────

    [Fact]
    public void ShowForgottenShrineMenuAndSelect_SelectsHolyStrength()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.ShowForgottenShrineMenuAndSelect();

        result.Should().Be(1);
    }

    [Fact]
    public void ShowForgottenShrineMenuAndSelect_SelectsLeave()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("4"));

        var result = svc.ShowForgottenShrineMenuAndSelect();

        result.Should().Be(0);
    }

    // ── ShowContestedArmoryMenuAndSelect (lines 1681-1690) ────────────────

    [Fact]
    public void ShowContestedArmoryMenuAndSelect_SelectsCareful()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.ShowContestedArmoryMenuAndSelect(15);

        result.Should().Be(1);
        Output.Should().Contain("DEF > 12");
        Output.Should().Contain("15");
    }

    [Fact]
    public void ShowContestedArmoryMenuAndSelect_SelectsLeave()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3"));

        var result = svc.ShowContestedArmoryMenuAndSelect(5);

        result.Should().Be(0);
    }

    // ── ShowShopWithSellAndSelect (lines 682-692) ─────────────────────────

    [Fact]
    public void ShowShopWithSellAndSelect_SelectsSellOption()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("2")); // stock(1) + sell(2) + leave(3)
        var item = new ItemBuilder().Named("Sword").WithDamage(5).Build();

        var result = svc.ShowShopWithSellAndSelect(new[] { (item, 50) }, 100);

        result.Should().Be(-1); // Sell option
    }

    [Fact]
    public void ShowShopWithSellAndSelect_SelectsLeave()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3")); // stock(1) + sell(2) + leave(3)
        var item = new ItemBuilder().Named("Sword").WithDamage(5).Build();

        var result = svc.ShowShopWithSellAndSelect(new[] { (item, 50) }, 100);

        result.Should().Be(0);
    }

    // ── SelectDifficulty (lines 1226-1240) ────────────────────────────────

    [Fact]
    public void SelectDifficulty_CasualInput_ReturnsCasual()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.SelectDifficulty();

        result.Should().Be(Difficulty.Casual);
    }

    [Fact]
    public void SelectDifficulty_HardInput_ReturnsHard()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("3"));

        var result = svc.SelectDifficulty();

        result.Should().Be(Difficulty.Hard);
    }

    // ── SelectClass (without prestige — lines 1299-1304) ──────────────────

    [Fact]
    public void SelectClass_NullPrestige_WritesBaseStats()
    {
        var svc = new ConsoleDisplayService(new FakeInputReader("1"));

        var result = svc.SelectClass(null);

        result.Should().NotBeNull();
        result.Name.Should().Be("Warrior");
    }
}

/// <summary>
/// Additional AnsiMarkupConverter tests to cover the trailing-text-with-active-color
/// code path (lines 103-122 of AnsiMarkupConverter.cs).
/// </summary>
public sealed class AnsiMarkupConverterCoverageTests
{
    [Fact]
    public void ConvertAnsiInlineToSpectre_TrailingTextWithColor_NoReset_ProducesClosedTag()
    {
        // Color code with text but no trailing reset — exercises the "lastIndex < input.Length" branch
        var input = "\x1b[32mgreen text";
        var result = Dungnz.Display.Spectre.AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        result.Should().Contain("[green]");
        result.Should().Contain("[/]");
        result.Should().Contain("green text");
    }

    [Fact]
    public void ConvertAnsiInlineToSpectre_BoldTrailingText_NoReset_ProducesClosedTag()
    {
        // Bold code with trailing text but no trailing reset
        var input = "\x1b[1mbold text without reset";
        var result = Dungnz.Display.Spectre.AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        result.Should().Contain("[bold]");
        result.Should().Contain("[/]");
        result.Should().Contain("bold text without reset");
    }

    [Fact]
    public void ConvertAnsiInlineToSpectre_BoldColorTrailingText_NoReset_ProducesClosedTag()
    {
        // Both bold and color active with trailing text, no reset
        var input = "\x1b[1m\x1b[33myellow bold text";
        var result = Dungnz.Display.Spectre.AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        result.Should().Contain("[bold yellow]");
        result.Should().Contain("[/]");
        result.Should().Contain("yellow bold text");
    }

    [Fact]
    public void ConvertAnsiInlineToSpectre_ColorThenTextThenColorNoReset_ClosesAll()
    {
        // Color, text, new color, text — no final reset. Exercises isTagOpen close-then-reopen
        var input = "\x1b[32mfirst\x1b[91msecond";
        var result = Dungnz.Display.Spectre.AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        result.Should().Contain("[green]first[/]");
        result.Should().Contain("[red]second[/]");
    }

    [Theory]
    [InlineData("\x1b[36m", "cyan")]
    [InlineData("\x1b[37m", "grey")]
    [InlineData("\x1b[34m", "blue")]
    [InlineData("\x1b[97m", "white")]
    public void ConvertAnsiInlineToSpectre_AllMappedColors_ProduceCorrectMarkup(string ansiCode, string expectedColor)
    {
        var input = $"{ansiCode}text\x1b[0m";
        var result = Dungnz.Display.Spectre.AnsiMarkupConverter.ConvertAnsiInlineToSpectre(input);

        result.Should().Contain($"[{expectedColor}]text[/]");
    }
}
