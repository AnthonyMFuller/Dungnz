using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;

namespace Dungnz.Tests;

/// <summary>
/// Unit tests for <see cref="MapRenderer"/> — the static BFS-based ASCII map renderer
/// extracted as part of the Avalonia migration Phase 1.
///
/// Tests cover grid generation, connector rendering, current room indicator,
/// plain text vs markup output, room symbol priority chain, and edge cases.
/// </summary>
public class MapRendererTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Creates a room with an optional description and marks it visited.</summary>
    private static Room MakeRoom(string description = "A room", bool visited = true)
    {
        return new Room { Description = description, Visited = visited };
    }

    /// <summary>Links two rooms bidirectionally along the given axis.</summary>
    private static void Link(Room from, Direction dir, Room to)
    {
        from.Exits[dir] = to;
        to.Exits[Opposite(dir)] = from;
    }

    private static Direction Opposite(Direction d) => d switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };

    // ═══════════════════════════════════════════════════════════════════════
    //  Grid Generation
    // ═══════════════════════════════════════════════════════════════════════

    // ── 1. Single room — produces valid output with the current-room marker ─

    [Fact]
    public void BuildPlainTextMap_SingleRoom_ContainsCurrentRoomMarker()
    {
        // Arrange
        var room = MakeRoom("Starting room");

        // Act
        var map = MapRenderer.BuildPlainTextMap(room);

        // Assert
        map.Should().Contain("[@]", because: "the only room is the current room");
    }

    // ── 2. Linear corridor N→S — rooms appear in correct vertical order ─────

    [Fact]
    public void BuildPlainTextMap_LinearCorridor_RoomsInVerticalOrder()
    {
        // Arrange: A (current) ─south→ B ─south→ C
        var a = MakeRoom("Room A");
        var b = MakeRoom("Room B");
        var c = MakeRoom("Room C");
        Link(a, Direction.South, b);
        Link(b, Direction.South, c);

        // Act
        var map = MapRenderer.BuildPlainTextMap(a);

        // Assert — [@] (current) should appear before the other rooms vertically
        var lines = map.Split('\n');
        int currentLine = Array.FindIndex(lines, l => l.Contains("[@]"));
        int lastRoomLine = Array.FindLastIndex(lines, l => l.Contains("[+]") || l.Contains("[?]"));

        currentLine.Should().BeLessThan(lastRoomLine,
            because: "current room is north, linked rooms are south (lower on map)");
    }

    // ── 3. Branching dungeon (T-intersection) — all branches rendered ────────

    [Fact]
    public void BuildPlainTextMap_TIntersection_AllBranchesPresent()
    {
        // Arrange: center (current) with east, west, and south exits
        var center = MakeRoom("Center");
        var east = MakeRoom("East wing");
        var west = MakeRoom("West wing");
        var south = MakeRoom("South wing");
        Link(center, Direction.East, east);
        Link(center, Direction.West, west);
        Link(center, Direction.South, south);

        // Act
        var map = MapRenderer.BuildPlainTextMap(center);

        // Assert — grid section (before legend) contains 1 current + 3 cleared = 4 room symbols
        var grid = GetGridSection(map);
        int roomSymbolCount = CountOccurrences(grid, "[@]") + CountOccurrences(grid, "[+]");
        roomSymbolCount.Should().Be(4, because: "center + 3 branches = 4 rooms in grid");
    }

    // ── 4. Cyclic dungeon — no infinite loop or duplicate cells ──────────────

    [Fact]
    public void BuildPlainTextMap_CyclicRooms_NoInfiniteLoopOrDuplicate()
    {
        // Arrange: A→B→C→A (triangle)
        var a = MakeRoom("Room A");
        var b = MakeRoom("Room B");
        var c = MakeRoom("Room C");
        Link(a, Direction.East, b);
        Link(b, Direction.South, c);
        Link(c, Direction.West, a);

        // Act — should terminate without hanging
        var map = MapRenderer.BuildPlainTextMap(a);

        // Assert — grid section has exactly 3 rooms (1 current + 2 cleared)
        var grid = GetGridSection(map);
        int roomCount = CountOccurrences(grid, "[@]") + CountOccurrences(grid, "[+]");
        roomCount.Should().Be(3, because: "BFS should visit each room exactly once in the grid");
    }

    // ── 5. Larger dungeon — no index-out-of-bounds ──────────────────────────

    [Fact]
    public void BuildPlainTextMap_LargerDungeon_NoException()
    {
        // Arrange: 5×1 east corridor
        var rooms = Enumerable.Range(0, 5).Select(i => MakeRoom($"Room {i}")).ToList();
        for (int i = 0; i < rooms.Count - 1; i++)
            Link(rooms[i], Direction.East, rooms[i + 1]);

        // Act
        var act = () => MapRenderer.BuildPlainTextMap(rooms[0]);

        // Assert
        act.Should().NotThrow(because: "a 5-room dungeon must render without overflow");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Connector Rendering
    // ═══════════════════════════════════════════════════════════════════════

    // ── 6. East exit — horizontal connector ─────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_EastExit_HorizontalConnector()
    {
        // Arrange
        var a = MakeRoom("Room A");
        var b = MakeRoom("Room B");
        Link(a, Direction.East, b);

        // Act
        var map = MapRenderer.BuildPlainTextMap(a);

        // Assert — horizontal connector character should appear between rooms
        map.Should().Contain("─", because: "east exit renders a horizontal connector");
    }

    // ── 7. South exit — vertical connector ──────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_SouthExit_VerticalConnector()
    {
        // Arrange
        var a = MakeRoom("Room A");
        var b = MakeRoom("Room B");
        Link(a, Direction.South, b);

        // Act
        var map = MapRenderer.BuildPlainTextMap(a);

        // Assert
        map.Should().Contain("│", because: "south exit renders a vertical connector");
    }

    // ── 8. Room with no exits — isolated cell, no connectors ────────────────

    [Fact]
    public void BuildPlainTextMap_IsolatedRoom_NoConnectors()
    {
        // Arrange
        var room = MakeRoom("Isolated");

        // Act
        var map = MapRenderer.BuildPlainTextMap(room);

        // Assert
        map.Should().NotContain("─", because: "no east exit means no horizontal connector");
        map.Should().NotContain("│", because: "no south exit means no vertical connector");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Current Room Indicator
    // ═══════════════════════════════════════════════════════════════════════

    // ── 9. Current room is visually marked as [@] ───────────────────────────

    [Fact]
    public void BuildPlainTextMap_CurrentRoom_MarkedAsAtSymbol()
    {
        // Arrange
        var current = MakeRoom("Current");
        var neighbor = MakeRoom("Neighbor");
        Link(current, Direction.East, neighbor);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[@]", because: "the current room is always shown as [@]");
    }

    // ── 10. Non-current visited rooms use [+] marker ────────────────────────

    [Fact]
    public void BuildPlainTextMap_VisitedNonCurrentRoom_MarkedAsCleared()
    {
        // Arrange — neighbor is visited with no enemy/shrine/special
        var current = MakeRoom("Current");
        var neighbor = MakeRoom("Neighbor", visited: true);
        Link(current, Direction.East, neighbor);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[+]", because: "a visited, cleared room uses the [+] symbol");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Plain Text vs Markup
    // ═══════════════════════════════════════════════════════════════════════

    // ── 11. Plain text output contains no Spectre markup tags ────────────────

    [Fact]
    public void BuildPlainTextMap_NoSpectreMarkupTags()
    {
        // Arrange
        var room = MakeRoom("Test room");
        var neighbor = MakeRoom("Neighbor");
        Link(room, Direction.East, neighbor);

        // Act
        var map = MapRenderer.BuildPlainTextMap(room);

        // Assert — strip out the room symbol brackets like [+], [@], [?] etc.,
        // then confirm no remaining [color]...[/] patterns
        var withoutRoomSymbols = System.Text.RegularExpressions.Regex.Replace(map, @"\[[@+?!BESAMTLFD~*^]\]", "");
        withoutRoomSymbols.Should().NotMatchRegex(@"\[[a-z].*?\]",
            because: "plain text output must not contain Spectre [color]...[/] markup");
    }

    // ── 12. Markup output contains Spectre color tags ───────────────────────

    [Fact]
    public void BuildMarkupMap_ContainsSpectreMarkupSyntax()
    {
        // Arrange
        var room = MakeRoom("Test room");

        // Act
        var map = MapRenderer.BuildMarkupMap(room);

        // Assert — must contain at least one Spectre color tag
        map.Should().Contain("[bold yellow]", because: "current room uses [bold yellow] markup");
        map.Should().Contain("[/]", because: "Spectre markup tags must be closed");
    }

    // ── 13. Both methods produce consistent spatial layout ──────────────────

    [Fact]
    public void BothMethods_ProduceConsistentLineCount()
    {
        // Arrange — same dungeon for both
        var center = MakeRoom("Center");
        var east = MakeRoom("East");
        var south = MakeRoom("South");
        Link(center, Direction.East, east);
        Link(center, Direction.South, south);

        // Act
        var plain = MapRenderer.BuildPlainTextMap(center);
        var markup = MapRenderer.BuildMarkupMap(center);

        var plainLines = plain.Split('\n').Length;
        var markupLines = markup.Split('\n').Length;

        // Assert — both should produce the same number of lines
        plainLines.Should().Be(markupLines,
            because: "plain and markup variants use the same BFS grid and layout logic");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Edge Cases
    // ═══════════════════════════════════════════════════════════════════════

    // ── 14. Unvisited neighbor — shown as [?] ───────────────────────────────

    [Fact]
    public void BuildPlainTextMap_UnvisitedNeighbor_ShownAsUnknown()
    {
        // Arrange — neighbor has not been visited
        var current = MakeRoom("Current");
        var unvisited = MakeRoom("Unvisited", visited: false);
        Link(current, Direction.East, unvisited);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[?]", because: "unvisited rooms render as [?]");
    }

    // ── 15. Room with exit to room that has an enemy — shows [!] ────────────

    [Fact]
    public void BuildPlainTextMap_RoomWithEnemy_ShowsExclamation()
    {
        // Arrange
        var current = MakeRoom("Safe room");
        var dangerRoom = MakeRoom("Danger room");
        dangerRoom.Enemy = new EnemyBuilder().WithHP(10).Build();
        Link(current, Direction.East, dangerRoom);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[!]", because: "a visited room with a live enemy renders as [!]");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Room Symbol Priority Chain
    // ═══════════════════════════════════════════════════════════════════════

    // ── 16. Exit room with boss — shows [B] not [E] ─────────────────────────

    [Fact]
    public void BuildPlainTextMap_ExitRoomWithBoss_ShowsBossSymbol()
    {
        // Arrange
        var current = MakeRoom("Start");
        var bossRoom = MakeRoom("Boss room");
        bossRoom.IsExit = true;
        bossRoom.Enemy = new EnemyBuilder().WithHP(100).Build();
        Link(current, Direction.East, bossRoom);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[B]", because: "exit room with live boss shows [B], not [E]");
        map.Should().NotContain("[E]", because: "boss symbol takes priority over exit");
    }

    // ── 17. Exit room without boss — shows [E] ─────────────────────────────

    [Fact]
    public void BuildPlainTextMap_ClearedExitRoom_ShowsExitSymbol()
    {
        // Arrange
        var current = MakeRoom("Start");
        var exitRoom = MakeRoom("Exit");
        exitRoom.IsExit = true;
        exitRoom.Enemy = null; // boss defeated
        Link(current, Direction.East, exitRoom);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[E]", because: "exit room with no live boss shows [E]");
    }

    // ── 18. Shrine room — shows [S] ─────────────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_ShrineRoom_ShowsShrineSymbol()
    {
        // Arrange
        var current = MakeRoom("Start");
        var shrine = MakeRoom("Shrine");
        shrine.HasShrine = true;
        shrine.ShrineUsed = false;
        Link(current, Direction.East, shrine);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[S]", because: "unused shrine renders as [S]");
    }

    // ── 19. Merchant room — shows [M] ───────────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_MerchantRoom_ShowsMerchantSymbol()
    {
        // Arrange
        var current = MakeRoom("Start");
        var shopRoom = MakeRoom("Shop");
        shopRoom.Merchant = new Merchant();
        Link(current, Direction.East, shopRoom);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[M]", because: "room with a merchant renders as [M]");
    }

    // ── 20. Trap room — shows [T] ───────────────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_TrapRoom_ShowsTrapSymbol()
    {
        // Arrange
        var current = MakeRoom("Start");
        var trapRoom = MakeRoom("Trap");
        trapRoom.Type = RoomType.TrapRoom;
        trapRoom.SpecialRoomUsed = false;
        Link(current, Direction.East, trapRoom);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[T]", because: "an unsprung trap room renders as [T]");
    }

    // ── 21. Compass header is present ───────────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_AlwaysShowsCompassHeader()
    {
        // Arrange
        var room = MakeRoom("Solo");

        // Act
        var map = MapRenderer.BuildPlainTextMap(room);

        // Assert
        map.Should().Contain("N", because: "the compass header should show cardinal directions");
        map.Should().Contain("W", because: "the compass header should show W");
        map.Should().Contain("E", because: "the compass header should show E");
        map.Should().Contain("S", because: "the compass header should show S");
    }

    // ── 22. Legend includes "You" label ──────────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_LegendContainsYouLabel()
    {
        // Arrange
        var room = MakeRoom("Solo");

        // Act
        var map = MapRenderer.BuildPlainTextMap(room);

        // Assert
        map.Should().Contain("[@] You", because: "the legend always lists the current-room symbol");
    }

    // ── 23. Markup map legend uses color tags ────────────────────────────────

    [Fact]
    public void BuildMarkupMap_LegendUsesColorTags()
    {
        // Arrange — room with enemy so legend has entries beyond just "You"
        var current = MakeRoom("Start");
        var enemyRoom = MakeRoom("Enemy");
        enemyRoom.Enemy = new EnemyBuilder().WithHP(10).Build();
        Link(current, Direction.East, enemyRoom);

        // Act
        var map = MapRenderer.BuildMarkupMap(current);

        // Assert
        map.Should().Contain("[bold yellow]", because: "markup legend includes colored [@] You entry");
        map.Should().Contain("[red]", because: "enemy room triggers [red] legend entry");
    }

    // ── 24. Entrance room on floor > 1 — shows [^] ─────────────────────────

    [Fact]
    public void BuildPlainTextMap_EntranceOnFloorGreaterThan1_ShowsEntranceSymbol()
    {
        // Arrange
        var current = MakeRoom("Current");
        var entrance = MakeRoom("Entrance");
        entrance.IsEntrance = true;
        Link(current, Direction.North, entrance);

        // Act — floor 2 means entrance is visible
        var map = MapRenderer.BuildPlainTextMap(current, currentFloor: 2);

        // Assert
        map.Should().Contain("[^]", because: "entrance room on floor > 1 renders as [^]");
    }

    // ── 25. Entrance room on floor 1 — does NOT show [^] ───────────────────

    [Fact]
    public void BuildPlainTextMap_EntranceOnFloor1_DoesNotShowEntranceSymbol()
    {
        // Arrange
        var current = MakeRoom("Current");
        var entrance = MakeRoom("Entrance");
        entrance.IsEntrance = true;
        Link(current, Direction.North, entrance);

        // Act — floor 1 means entrance symbol is suppressed
        var map = MapRenderer.BuildPlainTextMap(current, currentFloor: 1);

        // Assert
        map.Should().NotContain("[^]",
            because: "entrance symbol is only shown when currentFloor > 1");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Visibility / Fog of War
    // ═══════════════════════════════════════════════════════════════════════

    // ── 26. Room two hops away from visited — not visible ───────────────────

    [Fact]
    public void BuildPlainTextMap_RoomTwoHopsFromVisited_NotRendered()
    {
        // Arrange: current → unvisited_mid → unvisited_far
        var current = MakeRoom("Current");
        var mid = new Room { Description = "Mid", Visited = false };
        var far = new Room { Description = "Far", Visited = false };
        Link(current, Direction.East, mid);
        Link(mid, Direction.East, far);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert — grid section: mid is neighbor of current (visible as [?]),
        // far is neighbor of unvisited mid (not in knownSet), so hidden.
        // Only count [?] in the grid section (legend also contains "[?] Unknown").
        var grid = GetGridSection(map);
        int unknownCount = CountOccurrences(grid, "[?]");
        unknownCount.Should().Be(1, because: "only mid (direct neighbor of current) is visible as [?]; far is hidden");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Special Room Types
    // ═══════════════════════════════════════════════════════════════════════

    // ── 27. Dark room — shows [D] ───────────────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_DarkRoom_ShowsDarkSymbol()
    {
        // Arrange
        var current = MakeRoom("Start");
        var dark = MakeRoom("Dark");
        dark.Type = RoomType.Dark;
        Link(current, Direction.East, dark);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[D]", because: "a dark room renders as [D]");
    }

    // ── 28. Blessed clearing — shows [*] ────────────────────────────────────

    [Fact]
    public void BuildPlainTextMap_BlessedClearing_ShowsBlessedSymbol()
    {
        // Arrange
        var current = MakeRoom("Start");
        var blessed = MakeRoom("Blessed");
        blessed.EnvironmentalHazard = RoomHazard.BlessedClearing;
        Link(current, Direction.East, blessed);

        // Act
        var map = MapRenderer.BuildPlainTextMap(current);

        // Assert
        map.Should().Contain("[*]", because: "blessed clearing renders as [*]");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Utility
    // ═══════════════════════════════════════════════════════════════════════

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }

    /// <summary>
    /// Returns the grid portion of a map string (everything before the legend).
    /// The legend is separated from the grid by a blank line.
    /// </summary>
    private static string GetGridSection(string map)
    {
        // MapRenderer separates grid from legend with a blank line (AppendLine() after grid)
        var doubleNewline = map.IndexOf("\n\n", StringComparison.Ordinal);
        return doubleNewline >= 0 ? map[..doubleNewline] : map;
    }
}
