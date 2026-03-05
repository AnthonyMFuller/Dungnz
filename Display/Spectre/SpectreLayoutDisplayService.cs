using System.Text;
using System.Text.RegularExpressions;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Dungnz.Display.Spectre;

/// <summary>
/// Spectre.Console Live+Layout implementation of <see cref="IDisplayService"/>.
/// Renders the game to a persistent 5-panel layout using Spectre's Live display.
/// </summary>
/// <remarks>
/// <para><strong>Threading model:</strong> The game thread calls methods on this service.
/// All panel updates call <see cref="SpectreLayoutContext.UpdatePanel"/> which is thread-safe
/// and automatically refreshes the Live display.</para>
/// <para><strong>Input pattern:</strong> Input-coupled methods (menus, prompts) pause the
/// Live display by signaling <see cref="_pauseLiveEvent"/>, run a <see cref="SelectionPrompt{T}"/>,
/// then resume Live. This is acceptable for turn-based games per Anthony's decision.</para>
/// </remarks>
public partial class SpectreLayoutDisplayService : IDisplayService
{
    private readonly Layout _layout;
    private readonly SpectreLayoutContext _ctx;

    // Event signaling that Live should pause for a SelectionPrompt
    private readonly ManualResetEventSlim _pauseLiveEvent = new(false);
    private readonly ManualResetEventSlim _resumeLiveEvent = new(false);
    private readonly ManualResetEventSlim _liveExitEvent = new(false);

    // Content panel buffer (markup strings)
    private readonly List<string> _contentLines = new();
    private string _contentHeader = "📜 Adventure";
    private Color _contentBorderColor = Color.Blue;
    private const int MaxContentLines = 100;

    // Log panel buffer (markup strings)
    private readonly List<string> _logHistory = new();
    private const int MaxLogHistory = 50;

    // Cached state for auto-refresh
    private Player? _cachedPlayer;
    private Room? _cachedRoom;
    private int _currentFloor = 1;

    private static readonly Regex AnsiEscapePattern = new(@"\x1B\[[0-9;]*m", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreLayoutDisplayService"/> class.
    /// </summary>
    public SpectreLayoutDisplayService()
    {
        _layout = SpectreLayout.Create();
        _ctx = new SpectreLayoutContext(_layout);
    }

    /// <summary>
    /// Starts the Live display loop as a background <see cref="Task"/>.
    /// Returns immediately; the task completes when <see cref="StopLive"/> is called.
    /// </summary>
    public Task StartAsync() => Task.Run(StartLive);

    /// <summary>
    /// Starts the Live display. Call from main thread; blocks until game signals exit.
    /// The game loop runs on a separate thread and calls display methods.
    /// </summary>
    public void StartLive()
    {
        AnsiConsole.Live(_layout).Start(ctx =>
        {
            _ctx.SetContext(ctx);
            ctx.Refresh();

            // Live render loop — wait for exit or pause signals
            while (!_liveExitEvent.IsSet)
            {
                // Check for pause request (SelectionPrompt needs console)
                if (_pauseLiveEvent.IsSet)
                {
                    _pauseLiveEvent.Reset();
                    // Signal that Live is paused and prompt can run
                    _resumeLiveEvent.Wait();
                    _resumeLiveEvent.Reset();
                    ctx.Refresh();
                }
                else
                {
                    Thread.Sleep(50); // Yield to avoid busy-waiting
                }
            }

            _ctx.ClearContext();
        });
    }

    /// <summary>
    /// Signals the Live display to exit. Called when game ends.
    /// </summary>
    public void StopLive()
    {
        _liveExitEvent.Set();
    }

    /// <summary>
    /// Pauses Live, runs a SelectionPrompt, then resumes Live.
    /// </summary>
    private T RunPrompt<T>(Func<T> promptFunc) where T : notnull
    {
        // If Live isn't running, just run the prompt directly
        if (!_ctx.IsLiveActive)
        {
            return promptFunc();
        }

        // Signal pause and wait for Live to acknowledge
        _pauseLiveEvent.Set();
        Thread.Sleep(100); // Give Live loop time to pause

        try
        {
            return promptFunc();
        }
        finally
        {
            // Resume Live
            _resumeLiveEvent.Set();
        }
    }

    /// <summary>
    /// Pauses Live, runs a nullable SelectionPrompt, then resumes Live.
    /// </summary>
    private T? RunNullablePrompt<T>(Func<T?> promptFunc) where T : class
    {
        if (!_ctx.IsLiveActive)
        {
            return promptFunc();
        }

        _pauseLiveEvent.Set();
        Thread.Sleep(100);

        try
        {
            return promptFunc();
        }
        finally
        {
            _resumeLiveEvent.Set();
        }
    }

    // ── Panel update helpers ──────────────────────────────────────────────────

    private void UpdateMapPanel(string markupContent)
    {
        var panel = new Panel(new Markup(markupContent))
            .Header($"[bold green]🗺  Floor {_currentFloor}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green);
        _ctx.UpdatePanel(SpectreLayout.Panels.Map, panel);
    }

    private void UpdateStatsPanel(string markupContent)
    {
        var panel = new Panel(new Markup(markupContent))
            .Header("[bold cyan]⚔  Player Stats[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1);
        _ctx.UpdatePanel(SpectreLayout.Panels.Stats, panel);
    }

    /// <summary>Sets the content panel to the given markup, replacing prior content.</summary>
    private void SetContent(string markupContent, string header = "📜 Adventure", Color? borderColor = null)
    {
        _contentLines.Clear();
        if (!string.IsNullOrEmpty(markupContent))
        {
            foreach (var line in markupContent.Split('\n'))
                _contentLines.Add(line);
        }
        _contentHeader = header;
        _contentBorderColor = borderColor ?? Color.Blue;
        RefreshContentPanel();
    }

    /// <summary>Appends a markup line to the content panel buffer and refreshes.</summary>
    private void AppendContent(string markupLine)
    {
        _contentLines.Add(markupLine);
        if (_contentLines.Count > MaxContentLines)
            _contentLines.RemoveAt(0);
        RefreshContentPanel();
    }

    private void RefreshContentPanel()
    {
        var content = string.Join("\n", _contentLines.TakeLast(50));
        var panel = new Panel(new Markup(content))
            .Header($"[bold white]{_contentHeader}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(_contentBorderColor);
        _ctx.UpdatePanel(SpectreLayout.Panels.Content, panel);
    }

    private void UpdateLogPanel()
    {
        var content = string.Join("\n", _logHistory.TakeLast(MaxLogHistory));
        var panel = new Panel(new Markup(content))
            .Header("[bold grey]📋 Message Log[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey);
        _ctx.UpdatePanel(SpectreLayout.Panels.Log, panel);
    }

    private void AppendLog(string plainMessage, string type = "info")
    {
        var timestamp = DateTime.Now.ToString("HH:mm");
        var (icon, color) = type switch
        {
            "error"  => ("❌", "red"),
            "combat" => ("⚔",  "yellow"),
            "loot"   => ("💰", "green"),
            _        => ("ℹ",  "grey")
        };
        _logHistory.Add($"[grey]{timestamp}[/] {icon} [{color}]{Markup.Escape(plainMessage)}[/]");
        if (_logHistory.Count > MaxLogHistory)
            _logHistory.RemoveAt(0);
        UpdateLogPanel();
    }

    // ── HP/MP urgency bars (Issue #1066) ─────────────────────────────────────

    /// <summary>
    /// Builds a 10-character HP urgency bar.
    /// Green &gt;50%, yellow 25–50%, red &lt;25%.
    /// Format: <c>[color]████████░░[/] current/max HP</c>
    /// </summary>
    private static string BuildHpBar(int current, int max, int width = 10)
    {
        if (max <= 0) return $"[grey]{new string('░', width)}[/]";
        current = Math.Clamp(current, 0, max);
        double pct = (double)current / max;
        int filled = (int)Math.Round(pct * width);
        var color = pct > 0.5 ? "green" : pct >= 0.25 ? "yellow" : "red";
        var bar = new string('█', filled) + new string('░', width - filled);
        return $"[{color}]{bar}[/]";
    }

    /// <summary>
    /// Builds a 10-character MP urgency bar.
    /// Blue &gt;50%, mediumpurple1 25–50%, darkviolet &lt;25%.
    /// </summary>
    private static string BuildMpBar(int current, int max, int width = 10)
    {
        if (max <= 0) return string.Empty;
        current = Math.Clamp(current, 0, max);
        double pct = (double)current / max;
        int filled = (int)Math.Round(pct * width);
        var color = pct > 0.5 ? "blue" : pct >= 0.25 ? "mediumpurple1" : "darkviolet";
        var bar = new string('█', filled) + new string('░', width - filled);
        return $"[{color}]{bar}[/]";
    }

    // ── Map rendering helpers ─────────────────────────────────────────────────

    private void RenderMapPanel(Room currentRoom) =>
        UpdateMapPanel(BuildMapMarkup(currentRoom));

    private static string BuildMapMarkup(Room currentRoom)
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
                    Direction.North => (rx,     ry - 1),
                    Direction.South => (rx,     ry + 1),
                    Direction.East  => (rx + 1, ry),
                    Direction.West  => (rx - 1, ry),
                    _               => (rx,     ry)
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
        if (visible.Count == 0) return "[grey]No map data.[/]";

        int minX = visible.Min(kv => kv.Value.x), maxX = visible.Max(kv => kv.Value.x);
        int minY = visible.Min(kv => kv.Value.y), maxY = visible.Max(kv => kv.Value.y);

        var grid = new Dictionary<(int x, int y), Room>();
        foreach (var (room, pos) in visible)
            grid[pos] = room;

        var sb = new StringBuilder();
        sb.AppendLine("[grey]  N  W✦E  S[/]");

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
                sb.Append(GetMapRoomSymbol(r, currentRoom));
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

        // Legend — only symbols actually in the grid
        sb.AppendLine();
        var entries = new List<string> { "[bold yellow][[@]][/] You" };
        bool hasBoss = false, hasEnemy = false, hasExit = false, hasShrine = false;
        bool hasMerchant = false, hasTrap = false, hasArmory = false, hasLibrary = false;
        bool hasFShrine = false, hasBlessed = false, hasHazard = false, hasDark = false;
        bool hasCleared = false, hasUnknown = false;

        foreach (var kv in grid)
        {
            var rL = kv.Value;
            if (rL == currentRoom) continue;
            if (!rL.Visited)                                            { hasUnknown  = true; continue; }
            if (rL.IsExit && rL.Enemy?.HP > 0)                         { hasBoss     = true; continue; }
            if (rL.IsExit)                                              { hasExit     = true; continue; }
            if (rL.Enemy?.HP > 0)                                       { hasEnemy    = true; continue; }
            if (rL.HasShrine && !rL.ShrineUsed)                        { hasShrine   = true; continue; }
            if (rL.Merchant != null)                                    { hasMerchant = true; continue; }
            if (rL.Type == RoomType.TrapRoom && !rL.SpecialRoomUsed)   { hasTrap     = true; continue; }
            if (rL.Type == RoomType.ContestedArmory)                   { hasArmory   = true; continue; }
            if (rL.Type == RoomType.PetrifiedLibrary)                  { hasLibrary  = true; continue; }
            if (rL.Type == RoomType.ForgottenShrine)                   { hasFShrine  = true; continue; }
            if (rL.EnvironmentalHazard == RoomHazard.BlessedClearing)  { hasBlessed  = true; continue; }
            if (rL.EnvironmentalHazard != RoomHazard.None)             { hasHazard   = true; continue; }
            if (rL.Type == RoomType.Dark)                              { hasDark     = true; continue; }
            hasCleared = true;
        }

        if (hasBoss)     entries.Add("[bold red][[B]][/] Boss");
        if (hasEnemy)    entries.Add("[red][[!]][/] Enemy");
        if (hasExit)     entries.Add("[white][[E]][/] Exit");
        if (hasShrine)   entries.Add("[cyan][[S]][/] Shrine");
        if (hasMerchant) entries.Add("[bold green][[M]][/] Merchant");
        if (hasTrap)     entries.Add("[bold red][[T]][/] Trap");
        if (hasArmory)   entries.Add("[yellow][[A]][/] Armory");
        if (hasLibrary)  entries.Add("[blue][[L]][/] Library");
        if (hasFShrine)  entries.Add("[cyan][[F]][/] Forgotten");
        if (hasBlessed)  entries.Add("[green][[*]][/] Blessed");
        if (hasHazard)   entries.Add("[red][[~]][/] Hazard");
        if (hasDark)     entries.Add("[grey][[D]][/] Dark");
        if (hasCleared)  entries.Add("[white][[+]][/] Cleared");
        if (hasUnknown)  entries.Add("[grey][[?]][/] Unknown");

        int half = (entries.Count + 1) / 2;
        if (entries.Count <= 4)
            sb.Append(string.Join("  ", entries));
        else
        {
            sb.AppendLine(string.Join("  ", entries.Take(half)));
            sb.Append(string.Join("  ", entries.Skip(half)));
        }

        return sb.ToString().TrimEnd();
    }

    private static string GetMapRoomSymbol(Room r, Room currentRoom)
    {
        if (r == currentRoom)                                        return "[bold yellow][[@]][/]";
        if (!r.Visited)                                              return "[grey][[?]][/]";
        if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0)          return "[bold red][[B]][/]";
        if (r.IsExit)                                                return "[white][[E]][/]";
        if (r.Enemy != null && r.Enemy.HP > 0)                      return "[red][[!]][/]";
        if (r.HasShrine && !r.ShrineUsed)                           return "[cyan][[S]][/]";
        if (r.Merchant != null)                                      return "[bold green][[M]][/]";
        if (r.Type == RoomType.TrapRoom && !r.SpecialRoomUsed)      return "[bold red][[T]][/]";
        if (r.Type == RoomType.ContestedArmory)                     return "[yellow][[A]][/]";
        if (r.Type == RoomType.PetrifiedLibrary)                    return "[blue][[L]][/]";
        if (r.Type == RoomType.ForgottenShrine)                     return "[cyan][[F]][/]";
        if (r.EnvironmentalHazard == RoomHazard.BlessedClearing)    return "[green][[*]][/]";
        if (r.EnvironmentalHazard != RoomHazard.None)               return "[red][[~]][/]";
        if (r.Type == RoomType.Dark)                                return "[grey][[D]][/]";
        return "[white][[+]][/]";
    }

    // ── Stats panel rendering ─────────────────────────────────────────────────

    private void RenderStatsPanel(Player player)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[bold]{Markup.Escape(player.Name)}[/]  [grey]Lv {player.Level}[/]  [dim]{Markup.Escape(player.Class.ToString())}[/]");
        sb.AppendLine();

        // HP bar with urgency coloring (Issue #1066)
        var hpBar = BuildHpBar(player.HP, player.MaxHP);
        sb.AppendLine($"HP {hpBar} [bold]{player.HP}/{player.MaxHP}[/]");

        // MP bar with urgency coloring (Issue #1066)
        if (player.MaxMana > 0)
        {
            var mpBar = BuildMpBar(player.Mana, player.MaxMana);
            sb.AppendLine($"MP {mpBar} [bold]{player.Mana}/{player.MaxMana}[/]");
        }

        sb.AppendLine();
        sb.AppendLine($"[red]ATK[/] [bold]{player.Attack}[/]   [cyan]DEF[/] [bold]{player.Defense}[/]");
        sb.AppendLine($"[yellow]Gold[/] {player.Gold}g");
        var xpToNext = 100 * player.Level;
        sb.AppendLine($"[green]XP[/] {player.XP}/{xpToNext}");

        if (player.Class == PlayerClass.Rogue && player.ComboPoints > 0)
        {
            var dots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            sb.AppendLine($"[yellow]✦ Combo[/] {dots}");
        }

        // Quick equipped gear summary
        sb.AppendLine();
        if (player.EquippedWeapon != null)
        {
            var tc = TierColor(player.EquippedWeapon.Tier);
            sb.AppendLine($"[grey]⚔[/]  [{tc}]{Markup.Escape(player.EquippedWeapon.Name)}[/]");
        }
        if (player.EquippedChest != null)
        {
            var tc = TierColor(player.EquippedChest.Tier);
            sb.AppendLine($"[grey]🦺[/] [{tc}]{Markup.Escape(player.EquippedChest.Name)}[/]");
        }

        UpdateStatsPanel(sb.ToString().TrimEnd());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IDisplayService Implementation — Display-only methods (Live update)
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void ShowTitle()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[bold red]  ██████╗ ██╗   ██╗███╗   ██╗ ██████╗ ███╗   ██╗███████╗[/]");
        sb.AppendLine("[bold red]  ██╔══██╗██║   ██║████╗  ██║██╔════╝ ████╗  ██║╚══███╔╝[/]");
        sb.AppendLine("[bold red]  ██║  ██║██║   ██║██╔██╗ ██║██║  ███╗██╔██╗ ██║  ███╔╝ [/]");
        sb.AppendLine("[bold red]  ██║  ██║██║   ██║██║╚██╗██║██║   ██║██║╚██╗██║ ███╔╝  [/]");
        sb.AppendLine("[bold red]  ██████╔╝╚██████╔╝██║ ╚████║╚██████╔╝██║ ╚████║███████╗[/]");
        sb.AppendLine("[bold red]  ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝[/]");
        sb.AppendLine();
        sb.Append("[grey]              A dungeon awaits...[/]");
        SetContent(sb.ToString(), "🏰 DUNGNZ", Color.Red);
    }

    /// <inheritdoc/>
    public void ShowRoom(Room room)
    {
        _cachedRoom = room;

        var sb = new StringBuilder();

        // Room type prefix with color
        var (prefix, prefixColor) = room.Type switch
        {
            RoomType.Dark             => ("🌑 The room is pitch dark. ",                           "red"),
            RoomType.Scorched         => ("🔥 Scorch marks scar the stone. ",                      "yellow"),
            RoomType.Flooded          => ("💧 Ankle-deep water pools here. ",                      "blue"),
            RoomType.Mossy            => ("🌿 Damp moss covers the walls. ",                       "green"),
            RoomType.Ancient          => ("🏛 Ancient runes line the walls. ",                     "cyan"),
            RoomType.ForgottenShrine  => ("✨ Holy light radiates from a forgotten shrine. ",      "cyan"),
            RoomType.PetrifiedLibrary => ("📚 Petrified bookshelves line these ancient walls. ",  "blue"),
            RoomType.ContestedArmory  => ("⚔ Weapon racks gleam dangerously in the dark. ",       "yellow"),
            _                         => (string.Empty, "white")
        };

        if (!string.IsNullOrEmpty(prefix))
            sb.AppendLine($"[{prefixColor}]{Markup.Escape(prefix)}[/]");

        sb.AppendLine(Markup.Escape(room.Description));

        // Environmental hazard
        var envLine = room.EnvironmentalHazard switch
        {
            RoomHazard.LavaSeam        => "[red]🔥 Lava seams crack the floor — each action will burn you.[/]",
            RoomHazard.CorruptedGround => "[grey]💀 The ground pulses with dark energy — it will drain you.[/]",
            RoomHazard.BlessedClearing => "[cyan]✨ A blessed warmth fills this clearing.[/]",
            _                          => null
        };
        if (envLine != null) sb.AppendLine(envLine);

        // Hazard forewarning
        var hazardLine = room.Type switch
        {
            RoomType.Scorched => "[yellow]⚠ The scorched stone radiates heat — take care.[/]",
            RoomType.Flooded  => "[blue]⚠ The water here looks treacherous.[/]",
            RoomType.Dark     => "[grey]⚠ Darkness presses in around you.[/]",
            _                 => null
        };
        if (hazardLine != null) sb.AppendLine(hazardLine);

        // Exits
        if (room.Exits.Count > 0)
        {
            var exitSymbols = new Dictionary<Direction, string>
            {
                [Direction.North] = "↑ North",
                [Direction.South] = "↓ South",
                [Direction.East]  = "→ East",
                [Direction.West]  = "← West"
            };
            var ordered = new[] { Direction.North, Direction.South, Direction.East, Direction.West }
                .Where(d => room.Exits.ContainsKey(d))
                .Select(d => exitSymbols[d]);
            sb.AppendLine($"[yellow]Exits:[/] {string.Join("   ", ordered)}");
        }

        // Enemies
        if (room.Enemy != null)
            sb.AppendLine($"[bold red]⚔ {Markup.Escape(room.Enemy.Name)} is here![/]");

        // Items on floor
        if (room.Items.Count > 0)
        {
            sb.AppendLine("[grey]Items on the ground:[/]");
            foreach (var item in room.Items)
                sb.AppendLine($"  [green]◆ {Markup.Escape(item.Name)}[/] [grey]({Markup.Escape(PrimaryStatLabel(item))})[/]");
        }

        // Special room hints
        if (room.HasShrine && room.Type != RoomType.ForgottenShrine)
            sb.AppendLine("[cyan]✨ A shrine glimmers here. (USE SHRINE)[/]");
        if (room.Type == RoomType.ForgottenShrine && !room.SpecialRoomUsed)
            sb.AppendLine("[cyan]✨ A forgotten shrine stands here. (USE SHRINE)[/]");
        if (room.Type == RoomType.PetrifiedLibrary && !room.SpecialRoomUsed)
            sb.AppendLine("[cyan]📖 Ancient tomes line the walls. Something catches the light...[/]");
        if (room.Type == RoomType.ContestedArmory && !room.SpecialRoomUsed)
            sb.AppendLine("[yellow]⚠ Trapped weapons gleam in the dark. (USE ARMORY to approach)[/]");
        if (room.Merchant != null)
            sb.AppendLine("[yellow]🛒 A merchant awaits. (SHOP)[/]");

        SetContent(sb.ToString().TrimEnd(), GetRoomDisplayName(room), Color.Blue);
        AppendLog($"Entered {GetRoomDisplayName(room)}");

        // Auto-populate map and stats panels on room entry
        RenderMapPanel(room);
        if (_cachedPlayer != null)
            RenderStatsPanel(_cachedPlayer);
    }

    /// <inheritdoc/>
    public void ShowCombat(string message)
    {
        // TODO: Hill — Show combat headline in content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        // TODO: Hill — Render HP/MP bars with urgency coloring in content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowCombatMessage(string message)
    {
        // TODO: Hill — Append combat message to content and log panels
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowPlayerStats(Player player)
    {
        // TODO: Hill — Render full stats to stats panel with HP/MP urgency bars
        _cachedPlayer = player;
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowInventory(Player player)
    {
        // TODO: Hill — Render inventory list to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowLootDrop(Item item, Player player, bool isElite = false)
    {
        // TODO: Hill — Render loot card with tier coloring to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowGoldPickup(int amount, int newTotal)
    {
        // TODO: Hill — Show gold pickup in content and log panels
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax)
    {
        // TODO: Hill — Show item pickup with slot/weight status
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowItemDetail(Item item)
    {
        // TODO: Hill — Render full item stat card to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowMessage(string message)
    {
        // TODO: Hill — Show message in content panel and append to log
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowError(string message)
    {
        // TODO: Hill — Show error in red to content panel and log
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowHelp()
    {
        // TODO: Hill — Render help text to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowCommandPrompt(Player? player = null)
    {
        // TODO: Hill — Update input panel with prompt (no-op in Live mode, prompt handled elsewhere)
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowMap(Room currentRoom, int floor = 1)
    {
        // TODO: Hill — Build ASCII map and render to map panel
        _currentFloor = floor;
        _cachedRoom = currentRoom;
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowColoredMessage(string message, string color)
    {
        // TODO: Hill — Convert ANSI color code to Spectre markup and show message
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowColoredCombatMessage(string message, string color)
    {
        // TODO: Hill — Convert ANSI color code to Spectre markup and show combat message
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string valueColor)
    {
        // TODO: Hill — Render stat with colored value
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowEquipment(Player player)
    {
        // TODO: Hill — Render equipment slots to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowEnhancedTitle()
    {
        // TODO: Hill — Render enhanced ASCII title with Spectre FigletText
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool ShowIntroNarrative()
    {
        // TODO: Hill — Show intro lore text to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowPrestigeInfo(PrestigeData prestige)
    {
        // TODO: Hill — Show prestige card to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        // TODO: Hill — Render shop items to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        // TODO: Hill — Render sell menu to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients)
    {
        // TODO: Hill — Render craft recipe card to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowCombatStart(Enemy enemy)
    {
        // TODO: Hill — Render combat start banner with enemy name
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowCombatEntryFlags(Enemy enemy)
    {
        // TODO: Hill — Show elite/special flags in content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowLevelUpChoice(Player player)
    {
        // TODO: Hill — Render level-up options to content panel (display-only)
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)
    {
        // TODO: Hill — Render floor banner to content panel
        _currentFloor = floor;
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowEnemyDetail(Enemy enemy)
    {
        // TODO: Hill — Render enemy stat card to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowVictory(Player player, int floorsCleared, RunStats stats)
    {
        // TODO: Hill — Render victory screen to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowGameOver(Player player, string? killedBy, RunStats stats)
    {
        // TODO: Hill — Render game over screen to content panel
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ShowEnemyArt(Enemy enemy)
    {
        // TODO: Hill — Render enemy ASCII art to content panel if present
        throw new NotImplementedException();
    }

}

