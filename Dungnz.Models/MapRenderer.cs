using System.Text;

namespace Dungnz.Models;

/// <summary>
/// Static utility class for rendering ASCII dungeon maps using BFS-based spatial layout.
/// Provides both Spectre markup and plain text variants for use by different display implementations.
/// </summary>
public static class MapRenderer
{
    /// <summary>
    /// Builds an ASCII dungeon map with Spectre.Console markup tags for colors.
    /// Uses BFS to assign grid coordinates to all reachable rooms and renders
    /// box-drawing connectors and a dynamic legend.
    /// </summary>
    /// <param name="currentRoom">The player's current room (renders as [@]).</param>
    /// <param name="currentFloor">The current dungeon floor number (used to determine entrance visibility).</param>
    /// <returns>Multi-line string with Spectre markup tags (e.g., [red]text[/]).</returns>
    public static string BuildMarkupMap(Room currentRoom, int currentFloor = 1)
    {
        var (grid, minX, maxX, minY, maxY, visible) = BuildMapGrid(currentRoom, currentFloor);
        if (grid.Count == 0) return "[grey]No map data.[/]";

        var sb = new StringBuilder();
        sb.AppendLine("[grey]  N  W✦E  S[/]");

        // Render grid with room symbols and connectors
        for (int y = minY; y <= maxY; y++)
        {
            sb.Append("  ");
            for (int x = minX; x <= maxX; x++)
            {
                if (!grid.TryGetValue((x, y), out var r))
                {
                    sb.Append(x < maxX ? "    " : "   ");
                    continue;
                }
                sb.Append(GetMapRoomSymbolMarkup(r, currentRoom, currentFloor));
                if (x < maxX)
                {
                    bool east = r.Exits.ContainsKey(Direction.East) && grid.ContainsKey((x + 1, y));
                    sb.Append(east ? "─" : " ");
                }
            }
            sb.AppendLine();
            if (y < maxY)
            {
                sb.Append("  ");
                for (int x = minX; x <= maxX; x++)
                {
                    bool south = grid.TryGetValue((x, y), out var rS)
                        && rS.Exits.ContainsKey(Direction.South)
                        && grid.ContainsKey((x, y + 1));
                    sb.Append(south ? " │ " : "   ");
                    if (x < maxX) sb.Append(" ");
                }
                sb.AppendLine();
            }
        }

        // Legend with markup colors
        sb.AppendLine();
        sb.Append(BuildLegendMarkup(grid, currentRoom, currentFloor));

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds an ASCII dungeon map with plain text (no markup tags).
    /// Identical logic to <see cref="BuildMarkupMap"/> but returns uncolored text
    /// suitable for non-Spectre display implementations (e.g., Avalonia).
    /// </summary>
    /// <param name="currentRoom">The player's current room (renders as [@]).</param>
    /// <param name="currentFloor">The current dungeon floor number (used to determine entrance visibility).</param>
    /// <returns>Multi-line plain text string without color markup.</returns>
    public static string BuildPlainTextMap(Room currentRoom, int currentFloor = 1)
    {
        var (grid, minX, maxX, minY, maxY, visible) = BuildMapGrid(currentRoom, currentFloor);
        if (grid.Count == 0) return "No map data.";

        var sb = new StringBuilder();
        sb.AppendLine("  N  W✦E  S");

        // Render grid with room symbols and connectors
        for (int y = minY; y <= maxY; y++)
        {
            sb.Append("  ");
            for (int x = minX; x <= maxX; x++)
            {
                if (!grid.TryGetValue((x, y), out var r))
                {
                    sb.Append(x < maxX ? "    " : "   ");
                    continue;
                }
                sb.Append(GetMapRoomSymbolPlain(r, currentRoom, currentFloor));
                if (x < maxX)
                {
                    bool east = r.Exits.ContainsKey(Direction.East) && grid.ContainsKey((x + 1, y));
                    sb.Append(east ? "─" : " ");
                }
            }
            sb.AppendLine();
            if (y < maxY)
            {
                sb.Append("  ");
                for (int x = minX; x <= maxX; x++)
                {
                    bool south = grid.TryGetValue((x, y), out var rS)
                        && rS.Exits.ContainsKey(Direction.South)
                        && grid.ContainsKey((x, y + 1));
                    sb.Append(south ? " │ " : "   ");
                    if (x < maxX) sb.Append(" ");
                }
                sb.AppendLine();
            }
        }

        // Legend with plain text
        sb.AppendLine();
        sb.Append(BuildLegendPlain(grid, currentRoom, currentFloor));

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds the map legend with Spectre markup tags.
    /// Only includes symbols that are actually present in the current grid.
    /// </summary>
    private static string BuildLegendMarkup(Dictionary<(int x, int y), Room> grid, Room currentRoom, int currentFloor)
    {
        var entries = new List<string> { "[bold yellow][[@]][/] You" };
        bool hasBoss = false, hasEnemy = false, hasExit = false, hasShrine = false;
        bool hasMerchant = false, hasTrap = false, hasArmory = false, hasLibrary = false;
        bool hasFShrine = false, hasBlessed = false, hasHazard = false, hasDark = false;
        bool hasCleared = false, hasUnknown = false, hasEntrance = false;

        foreach (var kv in grid)
        {
            var rL = kv.Value;
            if (rL == currentRoom) continue;
            if (!rL.Visited) { hasUnknown = true; continue; }

            // Check merchant independently so it always appears in legend even when the room also has an alive enemy (#1146)
            if (rL.Merchant != null) hasMerchant = true;
            if (rL.IsEntrance && currentFloor > 1) { hasEntrance = true; continue; }
            if (rL.IsExit && rL.Enemy?.HP > 0) { hasBoss = true; continue; }
            if (rL.IsExit) { hasExit = true; continue; }
            if (rL.Enemy?.HP > 0) { hasEnemy = true; continue; }
            if (rL.HasShrine && !rL.ShrineUsed) { hasShrine = true; continue; }
            if (rL.Merchant != null) { continue; }
            if (rL.Type == RoomType.TrapRoom && !rL.SpecialRoomUsed) { hasTrap = true; continue; }
            if (rL.Type == RoomType.ContestedArmory && !rL.SpecialRoomUsed) { hasArmory = true; continue; }
            if (rL.Type == RoomType.PetrifiedLibrary && !rL.SpecialRoomUsed) { hasLibrary = true; continue; }
            if (rL.Type == RoomType.ForgottenShrine && !rL.SpecialRoomUsed) { hasFShrine = true; continue; }
            if (rL.EnvironmentalHazard == RoomHazard.BlessedClearing) { hasBlessed = true; continue; }
            if (rL.EnvironmentalHazard != RoomHazard.None && !rL.SpecialRoomUsed) { hasHazard = true; continue; }
            if (rL.Type == RoomType.Dark && !rL.SpecialRoomUsed) { hasDark = true; continue; }
            hasCleared = true;
        }

        if (hasEntrance) entries.Add("[blue][[^]][/] Entrance");
        if (hasBoss) entries.Add("[bold red][[B]][/] Boss");
        if (hasEnemy) entries.Add("[red][[!]][/] Enemy");
        if (hasExit) entries.Add("[white][[E]][/] Exit");
        if (hasShrine) entries.Add("[cyan][[S]][/] Shrine");
        if (hasMerchant) entries.Add("[bold green][[M]][/] Merchant");
        if (hasTrap) entries.Add("[bold red][[T]][/] Trap");
        if (hasArmory) entries.Add("[yellow][[A]][/] Armory");
        if (hasLibrary) entries.Add("[blue][[L]][/] Library");
        if (hasFShrine) entries.Add("[cyan][[F]][/] Forgotten");
        if (hasBlessed) entries.Add("[green][[*]][/] Blessed");
        if (hasHazard) entries.Add("[red][[~]][/] Hazard");
        if (hasDark) entries.Add("[grey][[D]][/] Dark");
        if (hasCleared) entries.Add("[white][[+]][/] Cleared");
        if (hasUnknown) entries.Add("[grey][[?]][/] Unknown");

        int half = (entries.Count + 1) / 2;
        if (entries.Count <= 4)
            return string.Join("  ", entries);
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join("  ", entries.Take(half)));
            sb.Append(string.Join("  ", entries.Skip(half)));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Builds the map legend with plain text (no markup).
    /// Only includes symbols that are actually present in the current grid.
    /// </summary>
    private static string BuildLegendPlain(Dictionary<(int x, int y), Room> grid, Room currentRoom, int currentFloor)
    {
        var entries = new List<string> { "[@] You" };
        bool hasBoss = false, hasEnemy = false, hasExit = false, hasShrine = false;
        bool hasMerchant = false, hasTrap = false, hasArmory = false, hasLibrary = false;
        bool hasFShrine = false, hasBlessed = false, hasHazard = false, hasDark = false;
        bool hasCleared = false, hasUnknown = false, hasEntrance = false;

        foreach (var kv in grid)
        {
            var rL = kv.Value;
            if (rL == currentRoom) continue;
            if (!rL.Visited) { hasUnknown = true; continue; }

            if (rL.Merchant != null) hasMerchant = true;
            if (rL.IsEntrance && currentFloor > 1) { hasEntrance = true; continue; }
            if (rL.IsExit && rL.Enemy?.HP > 0) { hasBoss = true; continue; }
            if (rL.IsExit) { hasExit = true; continue; }
            if (rL.Enemy?.HP > 0) { hasEnemy = true; continue; }
            if (rL.HasShrine && !rL.ShrineUsed) { hasShrine = true; continue; }
            if (rL.Merchant != null) { continue; }
            if (rL.Type == RoomType.TrapRoom && !rL.SpecialRoomUsed) { hasTrap = true; continue; }
            if (rL.Type == RoomType.ContestedArmory && !rL.SpecialRoomUsed) { hasArmory = true; continue; }
            if (rL.Type == RoomType.PetrifiedLibrary && !rL.SpecialRoomUsed) { hasLibrary = true; continue; }
            if (rL.Type == RoomType.ForgottenShrine && !rL.SpecialRoomUsed) { hasFShrine = true; continue; }
            if (rL.EnvironmentalHazard == RoomHazard.BlessedClearing) { hasBlessed = true; continue; }
            if (rL.EnvironmentalHazard != RoomHazard.None && !rL.SpecialRoomUsed) { hasHazard = true; continue; }
            if (rL.Type == RoomType.Dark && !rL.SpecialRoomUsed) { hasDark = true; continue; }
            hasCleared = true;
        }

        if (hasEntrance) entries.Add("[^] Entrance");
        if (hasBoss) entries.Add("[B] Boss");
        if (hasEnemy) entries.Add("[!] Enemy");
        if (hasExit) entries.Add("[E] Exit");
        if (hasShrine) entries.Add("[S] Shrine");
        if (hasMerchant) entries.Add("[M] Merchant");
        if (hasTrap) entries.Add("[T] Trap");
        if (hasArmory) entries.Add("[A] Armory");
        if (hasLibrary) entries.Add("[L] Library");
        if (hasFShrine) entries.Add("[F] Forgotten");
        if (hasBlessed) entries.Add("[*] Blessed");
        if (hasHazard) entries.Add("[~] Hazard");
        if (hasDark) entries.Add("[D] Dark");
        if (hasCleared) entries.Add("[+] Cleared");
        if (hasUnknown) entries.Add("[?] Unknown");

        int half = (entries.Count + 1) / 2;
        if (entries.Count <= 4)
            return string.Join("  ", entries);
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join("  ", entries.Take(half)));
            sb.Append(string.Join("  ", entries.Skip(half)));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Core map grid building logic shared by both markup and plain text variants.
    /// Uses BFS to assign (x,y) coordinates to all reachable rooms and filters
    /// to only show visited rooms and their immediate neighbors.
    /// </summary>
    /// <returns>Tuple of (grid, minX, maxX, minY, maxY, visibleList).</returns>
    private static (Dictionary<(int x, int y), Room> grid, int minX, int maxX, int minY, int maxY, List<KeyValuePair<Room, (int x, int y)>> visible)
        BuildMapGrid(Room currentRoom, int currentFloor)
    {
        // BFS to assign (x, y) coordinates to every reachable room
        var positions = new Dictionary<Room, (int x, int y)>();
        var queue = new Queue<Room>();
        positions[currentRoom] = (0, 0);
        queue.Enqueue(currentRoom);

        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            var (rx, ry) = positions[room];
            foreach (var (dir, neighbour) in room.Exits)
            {
                if (positions.ContainsKey(neighbour)) continue;
                var (nx, ny) = dir switch
                {
                    Direction.North => (rx, ry - 1),
                    Direction.South => (rx, ry + 1),
                    Direction.East => (rx + 1, ry),
                    Direction.West => (rx - 1, ry),
                    _ => (rx, ry)
                };
                positions[neighbour] = (nx, ny);
                queue.Enqueue(neighbour);
            }
        }

        // Visible rooms: visited, current, or adjacent to a visited/current room
        var knownSet = new HashSet<Room>(positions.Keys.Where(r => r.Visited || r == currentRoom));
        foreach (var known in knownSet.ToList())
            foreach (var (_, neighbour) in known.Exits)
                if (positions.ContainsKey(neighbour))
                    knownSet.Add(neighbour);

        var visible = positions.Where(kv => knownSet.Contains(kv.Key)).ToList();
        if (visible.Count == 0)
        {
            return (new Dictionary<(int x, int y), Room>(), 0, 0, 0, 0, visible);
        }

        int minX = visible.Min(kv => kv.Value.x), maxX = visible.Max(kv => kv.Value.x);
        int minY = visible.Min(kv => kv.Value.y), maxY = visible.Max(kv => kv.Value.y);

        var grid = new Dictionary<(int x, int y), Room>();
        foreach (var (room, pos) in visible)
            grid[pos] = room;

        return (grid, minX, maxX, minY, maxY, visible);
    }

    /// <summary>
    /// Returns the Spectre markup symbol for a room on the map.
    /// Evaluates room state (current/visited/enemy/shrine/etc.) in priority order.
    /// </summary>
    private static string GetMapRoomSymbolMarkup(Room r, Room currentRoom, int currentFloor = 1)
    {
        if (r == currentRoom) return "[bold yellow][[@]][/]";
        if (!r.Visited) return "[grey][[?]][/]";
        if (r.IsEntrance && currentFloor > 1) return "[blue][[^]][/]";
        if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0) return "[bold red][[B]][/]";
        if (r.IsExit) return "[white][[E]][/]";
        if (r.Enemy != null && r.Enemy.HP > 0) return "[red][[!]][/]";
        if (r.HasShrine && !r.ShrineUsed) return "[cyan][[S]][/]";
        if (r.Merchant != null) return "[bold green][[M]][/]";
        if (r.Type == RoomType.TrapRoom && !r.SpecialRoomUsed) return "[bold red][[T]][/]";
        if (r.Type == RoomType.ContestedArmory && !r.SpecialRoomUsed) return "[yellow][[A]][/]";
        if (r.Type == RoomType.PetrifiedLibrary && !r.SpecialRoomUsed) return "[blue][[L]][/]";
        if (r.Type == RoomType.ForgottenShrine && !r.SpecialRoomUsed) return "[cyan][[F]][/]";
        if (r.EnvironmentalHazard == RoomHazard.BlessedClearing) return "[green][[*]][/]";
        if (r.EnvironmentalHazard != RoomHazard.None) return "[red][[~]][/]";
        if (r.Type == RoomType.Dark) return "[grey][[D]][/]";
        return "[white][[+]][/]";
    }

    /// <summary>
    /// Returns the plain text symbol for a room on the map (no markup).
    /// Evaluates room state (current/visited/enemy/shrine/etc.) in priority order.
    /// </summary>
    private static string GetMapRoomSymbolPlain(Room r, Room currentRoom, int currentFloor = 1)
    {
        if (r == currentRoom) return "[@]";
        if (!r.Visited) return "[?]";
        if (r.IsEntrance && currentFloor > 1) return "[^]";
        if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0) return "[B]";
        if (r.IsExit) return "[E]";
        if (r.Enemy != null && r.Enemy.HP > 0) return "[!]";
        if (r.HasShrine && !r.ShrineUsed) return "[S]";
        if (r.Merchant != null) return "[M]";
        if (r.Type == RoomType.TrapRoom && !r.SpecialRoomUsed) return "[T]";
        if (r.Type == RoomType.ContestedArmory && !r.SpecialRoomUsed) return "[A]";
        if (r.Type == RoomType.PetrifiedLibrary && !r.SpecialRoomUsed) return "[L]";
        if (r.Type == RoomType.ForgottenShrine && !r.SpecialRoomUsed) return "[F]";
        if (r.EnvironmentalHazard == RoomHazard.BlessedClearing) return "[*]";
        if (r.EnvironmentalHazard != RoomHazard.None) return "[~]";
        if (r.Type == RoomType.Dark) return "[D]";
        return "[+]";
    }
}
