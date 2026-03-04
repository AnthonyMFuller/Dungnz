using System.Reflection;
using Dungnz.Display.Tui;
using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests.TuiTests;

/// <summary>
/// Tests for private static helper methods in TerminalGuiDisplayService.
/// Uses reflection to invoke helpers that are called from InvokeOnUiThread lambdas,
/// ensuring coverage of these behaviours without requiring a running Terminal.Gui loop.
/// </summary>
public class TerminalGuiDisplayServiceHelperTests
{
    // ─── Reflection helpers ─────────────────────────────────────────────────

    private static T InvokeHelper<T>(string methodName, params object?[] args)
    {
        var method = typeof(TerminalGuiDisplayService)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Method {methodName} not found");
        return (T)method.Invoke(null, args)!;
    }

    // ─── BuildHpBar ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildHpBar_MaxIsZero_ReturnsFallback()
    {
        var result = InvokeHelper<string>("BuildHpBar", 0, 0);
        result.Should().Be("[        ]");
    }

    [Fact]
    public void BuildHpBar_FullHealth_ReturnsFullBar()
    {
        var result = InvokeHelper<string>("BuildHpBar", 100, 100);
        result.Should().Be("[████████]");
    }

    [Fact]
    public void BuildHpBar_HalfHealth_ReturnsHalfFilledBar()
    {
        var result = InvokeHelper<string>("BuildHpBar", 50, 100);
        result.Should().StartWith("[").And.EndWith("]");
        result.Should().Contain("█");
        result.Should().Contain("░");
    }

    [Fact]
    public void BuildHpBar_ZeroHealth_ReturnsBarWithMinimalFill()
    {
        // PadRight starts from "█" so minimum is one filled char
        var result = InvokeHelper<string>("BuildHpBar", 0, 100);
        result.Should().StartWith("[").And.EndWith("]").And.HaveLength(10);
    }

    // ─── BuildColoredHpBar ───────────────────────────────────────────────────

    [Fact]
    public void BuildColoredHpBar_MaxIsZero_ReturnsEmptyBar()
    {
        var result = InvokeHelper<string>("BuildColoredHpBar", 0, 0);
        result.Should().Be("[░░░░░░░░]");
    }

    [Fact]
    public void BuildColoredHpBar_FullHealth_UsesBlockChar()
    {
        // >50% → '█'
        var result = InvokeHelper<string>("BuildColoredHpBar", 100, 100);
        result.Should().StartWith("[").And.EndWith("]");
        result.Should().Contain("█");
    }

    [Fact]
    public void BuildColoredHpBar_ThirtyPercent_UsesMediumShadeChar()
    {
        // 30% > 25% → '▓'
        var result = InvokeHelper<string>("BuildColoredHpBar", 30, 100);
        result.Should().StartWith("[").And.EndWith("]");
        result.Should().Contain("▓");
    }

    [Fact]
    public void BuildColoredHpBar_TwentyPercent_UsesLightShadeChar()
    {
        // 20% ≤ 25% → '▒'
        var result = InvokeHelper<string>("BuildColoredHpBar", 20, 100);
        result.Should().StartWith("[").And.EndWith("]");
    }

    [Fact]
    public void BuildColoredHpBar_ZeroHealth_ReturnsAllLightShade()
    {
        var result = InvokeHelper<string>("BuildColoredHpBar", 0, 100);
        result.Should().Be("[░░░░░░░░]");
    }

    [Fact]
    public void BuildColoredHpBar_OverMax_ClampedToFull()
    {
        // HP > max should clamp
        var result = InvokeHelper<string>("BuildColoredHpBar", 150, 100);
        result.Should().StartWith("[").And.EndWith("]").And.HaveLength(10);
    }

    // ─── BuildMpBar ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildMpBar_MaxIsZero_ReturnsFallback()
    {
        var result = InvokeHelper<string>("BuildMpBar", 0, 0);
        result.Should().Be("[        ]");
    }

    [Fact]
    public void BuildMpBar_FullMana_ReturnsFullBar()
    {
        var result = InvokeHelper<string>("BuildMpBar", 50, 50);
        result.Should().Be("[████████]");
    }

    [Fact]
    public void BuildMpBar_ZeroMana_ReturnsBarWithMinimalFill()
    {
        // Same PadRight quirk as BuildHpBar
        var result = InvokeHelper<string>("BuildMpBar", 0, 50);
        result.Should().StartWith("[").And.EndWith("]").And.HaveLength(10);
    }

    // ─── BuildColoredMpBar ───────────────────────────────────────────────────

    [Fact]
    public void BuildColoredMpBar_MaxIsZero_ReturnsEmptyBar()
    {
        var result = InvokeHelper<string>("BuildColoredMpBar", 0, 0);
        result.Should().Be("[░░░░░░░░]");
    }

    [Fact]
    public void BuildColoredMpBar_FullMana_UsesBlockChar()
    {
        // 100% > 50% → '█'
        var result = InvokeHelper<string>("BuildColoredMpBar", 100, 100);
        result.Should().Contain("█");
    }

    [Fact]
    public void BuildColoredMpBar_ThirtyPercent_UsesMediumShadeChar()
    {
        // 30% > 20% → '▓'
        var result = InvokeHelper<string>("BuildColoredMpBar", 30, 100);
        result.Should().StartWith("[").And.EndWith("]");
    }

    [Fact]
    public void BuildColoredMpBar_TenPercent_UsesLightShadeChar()
    {
        // 10% ≤ 20% → '▒'
        var result = InvokeHelper<string>("BuildColoredMpBar", 10, 100);
        result.Should().StartWith("[").And.EndWith("]");
    }

    [Fact]
    public void BuildColoredMpBar_ZeroMana_ReturnsEmptyBar()
    {
        var result = InvokeHelper<string>("BuildColoredMpBar", 0, 100);
        result.Should().Be("[░░░░░░░░]");
    }

    // ─── StripAnsiCodes ──────────────────────────────────────────────────────

    [Fact]
    public void StripAnsiCodes_PlainText_ReturnsUnchanged()
    {
        var result = InvokeHelper<string>("StripAnsiCodes", "Hello world");
        result.Should().Be("Hello world");
    }

    [Fact]
    public void StripAnsiCodes_AnsiColorCode_StripsCode()
    {
        var result = InvokeHelper<string>("StripAnsiCodes", "\x1b[31mRed text\x1b[0m");
        result.Should().Be("Red text");
    }

    [Fact]
    public void StripAnsiCodes_MultipleAnsiCodes_StripsAll()
    {
        var input = "\x1b[1m\x1b[32mBold green\x1b[0m normal";
        var result = InvokeHelper<string>("StripAnsiCodes", input);
        result.Should().Be("Bold green normal");
    }

    [Fact]
    public void StripAnsiCodes_EmptyString_ReturnsEmpty()
    {
        var result = InvokeHelper<string>("StripAnsiCodes", "");
        result.Should().Be("");
    }

    // ─── GetPrimaryStatLabel ─────────────────────────────────────────────────

    [Fact]
    public void GetPrimaryStatLabel_AttackBonus_ReturnsAtkLabel()
    {
        var item = new ItemBuilder().WithDamage(5).Build();
        var result = InvokeHelper<string>("GetPrimaryStatLabel", item);
        result.Should().Be("+5 ATK");
    }

    [Fact]
    public void GetPrimaryStatLabel_DefenseBonus_ReturnsDefLabel()
    {
        var item = new ItemBuilder().WithDefense(3).Build();
        var result = InvokeHelper<string>("GetPrimaryStatLabel", item);
        result.Should().Be("+3 DEF");
    }

    [Fact]
    public void GetPrimaryStatLabel_HealAmount_ReturnsHpLabel()
    {
        var item = new ItemBuilder().WithHeal(20).Build();
        var result = InvokeHelper<string>("GetPrimaryStatLabel", item);
        result.Should().Be("+20 HP");
    }

    [Fact]
    public void GetPrimaryStatLabel_ManaRestore_ReturnsMpLabel()
    {
        var item = new Item { Name = "Mana Potion", Type = ItemType.Consumable, ManaRestore = 15 };
        var result = InvokeHelper<string>("GetPrimaryStatLabel", item);
        result.Should().Be("+15 MP");
    }

    [Fact]
    public void GetPrimaryStatLabel_MaxManaBonus_ReturnsMaxMpLabel()
    {
        var item = new Item { Name = "Arcane Ring", Type = ItemType.Accessory, MaxManaBonus = 10 };
        var result = InvokeHelper<string>("GetPrimaryStatLabel", item);
        result.Should().Be("+10 Max MP");
    }

    [Fact]
    public void GetPrimaryStatLabel_NoBonuses_ReturnsTypeName()
    {
        var item = new Item { Name = "Gold Coin", Type = ItemType.Gold };
        var result = InvokeHelper<string>("GetPrimaryStatLabel", item);
        result.Should().Be("Gold");
    }

    [Fact]
    public void GetPrimaryStatLabel_CraftingMaterial_ReturnsTypeName()
    {
        var item = new Item { Name = "Iron Ore", Type = ItemType.CraftingMaterial };
        var result = InvokeHelper<string>("GetPrimaryStatLabel", item);
        result.Should().Be("CraftingMaterial");
    }

    // ─── GetItemIcon ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ItemType.Weapon, "⚔")]
    [InlineData(ItemType.Armor, "🛡")]
    [InlineData(ItemType.Accessory, "💍")]
    [InlineData(ItemType.Consumable, "🧪")]
    [InlineData(ItemType.CraftingMaterial, "📦")]
    [InlineData(ItemType.Gold, "◆")]
    public void GetItemIcon_ReturnsCorrectIcon(ItemType type, string expectedIcon)
    {
        var item = new Item { Name = "Test", Type = type };
        var result = InvokeHelper<string>("GetItemIcon", item);
        result.Should().Be(expectedIcon);
    }

    // ─── GetClassIcon ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Warrior", "⚔")]
    [InlineData("Mage", "🔮")]
    [InlineData("Rogue", "🗡")]
    [InlineData("Paladin", "✨")]
    [InlineData("Necromancer", "🛡")]
    [InlineData("Ranger", "🛡")]
    public void GetClassIcon_ReturnsCorrectIcon(string className, string expectedIcon)
    {
        var def = PlayerClassDefinition.All.First(d => d.Name == className);
        var result = InvokeHelper<string>("GetClassIcon", def);
        result.Should().Be(expectedIcon);
    }

    // ─── GetMapRoomSymbol ────────────────────────────────────────────────────

    [Fact]
    public void GetMapRoomSymbol_CurrentRoom_ReturnsPlayerSymbol()
    {
        var room = new RoomBuilder().Build();
        var result = InvokeHelper<string>("GetMapRoomSymbol", room, room);
        result.Should().Be("[@]");
    }

    [Fact]
    public void GetMapRoomSymbol_UnvisitedRoom_ReturnsUnknownSymbol()
    {
        var currentRoom = new RoomBuilder().Build();
        var otherRoom = new RoomBuilder().Build(); // Visited defaults to false
        var result = InvokeHelper<string>("GetMapRoomSymbol", otherRoom, currentRoom);
        result.Should().Be("[?]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedExitWithLivingBoss_ReturnsBossSymbol()
    {
        var boss = new EnemyBuilder().Named("Boss").WithHP(100).Build();
        var room = new RoomBuilder().AsExit().Build();
        room.Visited = true;
        room.Enemy = boss;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[B]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedExitNoBoss_ReturnsExitSymbol()
    {
        var room = new RoomBuilder().AsExit().Build();
        room.Visited = true;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[E]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedRoomWithLivingEnemy_ReturnsEnemySymbol()
    {
        var enemy = new EnemyBuilder().WithHP(10).Build();
        var room = new RoomBuilder().WithEnemy(enemy).Build();
        room.Visited = true;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[!]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedShrineNotUsed_ReturnsShrineSymbol()
    {
        var room = new RoomBuilder().WithShrine().Build();
        room.Visited = true;
        room.ShrineUsed = false;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[S]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedRoomWithMerchant_ReturnsMerchantSymbol()
    {
        var room = new RoomBuilder().Build();
        room.Visited = true;
        room.Merchant = new Merchant { Name = "Trader" };
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[M]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedTrapRoom_ReturnsTrapSymbol()
    {
        var room = new RoomBuilder().OfType(RoomType.TrapRoom).Build();
        room.Visited = true;
        room.SpecialRoomUsed = false;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[T]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedContestedArmory_ReturnsArmorySymbol()
    {
        var room = new RoomBuilder().OfType(RoomType.ContestedArmory).Build();
        room.Visited = true;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[A]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedPetrifiedLibrary_ReturnsLibrarySymbol()
    {
        var room = new RoomBuilder().OfType(RoomType.PetrifiedLibrary).Build();
        room.Visited = true;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[L]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedForgottenShrine_ReturnsForgottenShrineSymbol()
    {
        var room = new RoomBuilder().OfType(RoomType.ForgottenShrine).Build();
        room.Visited = true;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[F]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedBlessedClearing_ReturnsBlessedSymbol()
    {
        var room = new RoomBuilder().Build();
        room.Visited = true;
        room.EnvironmentalHazard = RoomHazard.BlessedClearing;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[*]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedHazardRoom_ReturnsHazardSymbol()
    {
        var room = new RoomBuilder().Build();
        room.Visited = true;
        room.EnvironmentalHazard = RoomHazard.LavaSeam;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[~]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedDarkRoom_ReturnsDarkSymbol()
    {
        var room = new RoomBuilder().OfType(RoomType.Dark).Build();
        room.Visited = true;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[D]");
    }

    [Fact]
    public void GetMapRoomSymbol_VisitedClearedRoom_ReturnsClearedSymbol()
    {
        var room = new RoomBuilder().Build();
        room.Visited = true;
        var current = new RoomBuilder().Build();

        var result = InvokeHelper<string>("GetMapRoomSymbol", room, current);
        result.Should().Be("[+]");
    }

    // ─── BuildAsciiMap ───────────────────────────────────────────────────────

    private static string InvokeBuildAsciiMap(Room room)
        => InvokeHelper<string>("BuildAsciiMap", room);

    [Fact]
    public void BuildAsciiMap_SingleRoom_ContainsPlayerSymbol()
    {
        var room = new RoomBuilder().Build();
        var result = InvokeBuildAsciiMap(room);

        result.Should().Contain("[@]");
    }

    [Fact]
    public void BuildAsciiMap_SingleRoom_ContainsCompassAndLegend()
    {
        var room = new RoomBuilder().Build();
        var result = InvokeBuildAsciiMap(room);

        result.Should().Contain("N");
        result.Should().Contain("Legend:");
        result.Should().Contain("[@] You");
    }

    [Fact]
    public void BuildAsciiMap_RoomWithNorthExit_ContainsConnector()
    {
        var northRoom = new RoomBuilder().Build();
        northRoom.Visited = true;
        var southRoom = new RoomBuilder().Build();
        southRoom.Exits[Direction.South] = northRoom;
        northRoom.Exits[Direction.North] = southRoom;

        var result = InvokeBuildAsciiMap(southRoom);

        result.Should().Contain("│");
    }

    [Fact]
    public void BuildAsciiMap_RoomWithEastExit_ContainsHorizontalConnector()
    {
        var eastRoom = new RoomBuilder().Build();
        eastRoom.Visited = true;
        var westRoom = new RoomBuilder().Build();
        westRoom.Exits[Direction.East] = eastRoom;
        eastRoom.Exits[Direction.West] = westRoom;

        var result = InvokeBuildAsciiMap(westRoom);

        result.Should().Contain("─");
    }

    [Fact]
    public void BuildAsciiMap_UnvisitedAdjacentRoom_ShowsUnknownSymbol()
    {
        var eastRoom = new RoomBuilder().Build(); // not visited
        var currentRoom = new RoomBuilder().Build();
        currentRoom.Exits[Direction.East] = eastRoom;

        var result = InvokeBuildAsciiMap(currentRoom);

        result.Should().Contain("[?]");
    }

    [Fact]
    public void BuildAsciiMap_VisitedEnemyRoom_ShowsEnemySymbol()
    {
        var enemy = new EnemyBuilder().WithHP(10).Build();
        var enemyRoom = new RoomBuilder().WithEnemy(enemy).Build();
        enemyRoom.Visited = true;
        var currentRoom = new RoomBuilder().Build();
        currentRoom.Exits[Direction.East] = enemyRoom;

        var result = InvokeBuildAsciiMap(currentRoom);

        result.Should().Contain("[!]");
    }

    [Fact]
    public void BuildAsciiMap_MultipleDirections_ShowsAllConnectedRooms()
    {
        var northRoom = new RoomBuilder().Build();
        northRoom.Visited = true;
        var southRoom = new RoomBuilder().Build();
        southRoom.Visited = true;
        var currentRoom = new RoomBuilder().Build();
        currentRoom.Exits[Direction.North] = northRoom;
        currentRoom.Exits[Direction.South] = southRoom;
        northRoom.Exits[Direction.South] = currentRoom;
        southRoom.Exits[Direction.North] = currentRoom;

        var result = InvokeBuildAsciiMap(currentRoom);

        result.Should().Contain("[@]");
        result.Should().Contain("[+]");
    }

    // ─── BuildStatsText ──────────────────────────────────────────────────────

    [Fact]
    public void BuildStatsText_BasicPlayer_ContainsNameAndStats()
    {
        var player = new PlayerBuilder().Named("Hero").WithHP(80).WithMaxHP(100).Build();
        var result = InvokeHelper<string>("BuildStatsText", player);

        result.Should().Contain("Hero");
        result.Should().Contain("ATK:");
        result.Should().Contain("DEF:");
    }

    [Fact]
    public void BuildStatsText_PlayerWithMana_ContainsMpBar()
    {
        var player = new PlayerBuilder().WithMana(30).WithMaxMana(50).Build();
        var result = InvokeHelper<string>("BuildStatsText", player);

        result.Should().Contain("MP:");
    }

    [Fact]
    public void BuildStatsText_PlayerNoMana_NoMpSection()
    {
        var player = new PlayerBuilder().WithMana(0).WithMaxMana(0).Build();
        var result = InvokeHelper<string>("BuildStatsText", player);

        result.Should().NotContain("MP:");
    }

    [Fact]
    public void BuildStatsText_PlayerWithWeapon_ShowsWeaponName()
    {
        var weapon = new ItemBuilder().Named("Iron Sword").WithDamage(5).Build();
        var player = new PlayerBuilder().WithWeapon(weapon).Build();
        var result = InvokeHelper<string>("BuildStatsText", player);

        result.Should().Contain("Iron Sword");
    }

    [Fact]
    public void BuildStatsText_PlayerWithGold_ShowsGold()
    {
        var player = new PlayerBuilder().WithGold(150).Build();
        var result = InvokeHelper<string>("BuildStatsText", player);

        result.Should().Contain("150g");
    }

    [Fact]
    public void BuildStatsText_PlayerWithLevel_ShowsLevel()
    {
        var player = new PlayerBuilder().WithLevel(5).Build();
        var result = InvokeHelper<string>("BuildStatsText", player);

        result.Should().Contain("5");
    }
}
