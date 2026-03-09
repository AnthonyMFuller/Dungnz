using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
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
/// <para><strong>Input pattern:</strong> Input-coupled methods use ReadKey-based input when
/// Live is active. AnsiConsole.Prompt() is only used when Live is not running to avoid
/// Spectre.Console exclusivity lock conflicts.</para>
/// </remarks>
[ExcludeFromCodeCoverage]
public partial class SpectreLayoutDisplayService : IDisplayService
{
    private readonly Layout _layout;
    private readonly SpectreLayoutContext _ctx;

    // Event signaling that Live should exit
    private readonly ManualResetEventSlim _liveExitEvent = new(false);

    // Content panel buffer (markup strings)
    private readonly List<string> _contentLines = new();
    private string _contentHeader = "Adventure";
    private Color _contentBorderColor = Color.Blue;
    private const int MaxContentLines = 50;
    
    // NOTE: _pauseDepth was removed along with PauseAndRun method (#1265)

    // Log panel buffer (markup strings)
    private readonly List<string> _logHistory = new();
    private const int MaxLogHistory = 50;
    private const int MaxDisplayedLog = 12;

    // Cached state for auto-refresh
    private Player? _cachedPlayer;
    private Room? _cachedRoom;
    private int _currentFloor = 1;

    // Cached ability cooldown state for combat HUD (Issue #1268)
    private IReadOnlyList<(string name, int turnsRemaining)> _cachedCooldowns = [];

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

            // Live render loop — wait for exit signal
            while (!_liveExitEvent.IsSet)
            {
                // NOTE: Pause/resume logic removed (#1265) — never call AnsiConsole.Prompt()
                // while Live is active. Use ReadKey-based input instead.
                Thread.Sleep(50); // Yield to avoid busy-waiting
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
    /// Resets all cached state between game runs. Called at start of each Run().
    /// </summary>
    public void Reset()
    {
        _cachedPlayer = null;
        _cachedRoom = null;
        _contentLines.Clear();
        _logHistory.Clear();
        _currentFloor = 1;
        _contentHeader = "Adventure";
        _contentBorderColor = Color.Blue;
    }

    // ── Panel update helpers ──────────────────────────────────────────────────

    private void UpdateMapPanel(string markupContent)
    {
        var panel = new Panel(new Markup(markupContent))
            .Header($"[bold green]Floor {_currentFloor}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green);
        _ctx.UpdatePanel(SpectreLayout.Panels.Map, panel);
    }

    private void UpdateStatsPanel(string markupContent)
    {
        var panel = new Panel(new Markup(markupContent))
            .Header("[bold cyan]Player Stats[/]")
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
        var content = string.Join("\n", _logHistory.TakeLast(MaxDisplayedLog));
        var panel = new Panel(new Markup(content))
            .Header("[bold grey]Message Log[/]")
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
        UpdateMapPanel(BuildMapMarkup(currentRoom, _currentFloor));

    private static string BuildMapMarkup(Room currentRoom, int currentFloor = 1)
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
                sb.Append(GetMapRoomSymbol(r, currentRoom, currentFloor));
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
        bool hasCleared = false, hasUnknown = false, hasEntrance = false;

        foreach (var kv in grid)
        {
            var rL = kv.Value;
            if (rL == currentRoom) continue;
            if (!rL.Visited)                                            { hasUnknown  = true; continue; }
            
            // Skip cleared rooms from legend (they render as [+], not their type symbol)
            bool isCleared = rL.Visited && rL.Enemy?.HP <= 0;
            
            // Check merchant independently so it always appears in legend even when the room also has an alive enemy (#1146)
            if (rL.Merchant != null)                                                           hasMerchant = true;
            if (rL.IsEntrance && currentFloor > 1)                     { hasEntrance = true; continue; }
            if (rL.IsExit && rL.Enemy?.HP > 0)                         { hasBoss     = true; continue; }
            if (rL.IsExit)                                              { hasExit     = true; continue; }
            if (rL.Enemy?.HP > 0)                                       { hasEnemy    = true; continue; }
            if (rL.HasShrine && !rL.ShrineUsed)                        { hasShrine   = true; continue; }
            if (rL.Merchant != null)                                    {                      continue; }
            if (rL.Type == RoomType.TrapRoom && !rL.SpecialRoomUsed)   { hasTrap     = true; continue; }
            if (rL.Type == RoomType.ContestedArmory && !isCleared)     { hasArmory   = true; continue; }
            if (rL.Type == RoomType.PetrifiedLibrary && !isCleared)    { hasLibrary  = true; continue; }
            if (rL.Type == RoomType.ForgottenShrine && !isCleared)     { hasFShrine  = true; continue; }
            if (rL.EnvironmentalHazard == RoomHazard.BlessedClearing)  { hasBlessed  = true; continue; }
            if (rL.EnvironmentalHazard != RoomHazard.None && !isCleared) { hasHazard = true; continue; }
            if (rL.Type == RoomType.Dark && !isCleared)                { hasDark     = true; continue; }
            hasCleared = true;
        }

        if (hasEntrance) entries.Add("[blue][[^]][/] Entrance");
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

    private static string GetMapRoomSymbol(Room r, Room currentRoom, int currentFloor = 1)
    {
        if (r == currentRoom)                                                    return "[bold yellow][[@]][/]";
        if (!r.Visited)                                                          return "[grey][[?]][/]";
        if (r.IsEntrance && currentFloor > 1)                                   return "[blue][[^]][/]";
        if (r.IsExit && r.Enemy != null && r.Enemy.HP > 0)                      return "[bold red][[B]][/]";
        if (r.IsExit)                                                            return "[white][[E]][/]";
        if (r.Enemy != null && r.Enemy.HP > 0)                                  return "[red][[!]][/]";
        if (r.HasShrine && !r.ShrineUsed)                                       return "[cyan][[S]][/]";
        if (r.Merchant != null)                                                  return "[bold green][[M]][/]";
        if (r.Type == RoomType.TrapRoom         && !r.SpecialRoomUsed)          return "[bold red][[T]][/]";
        if (r.Type == RoomType.ContestedArmory  && !r.SpecialRoomUsed)          return "[yellow][[A]][/]";
        if (r.Type == RoomType.PetrifiedLibrary && !r.SpecialRoomUsed)          return "[blue][[L]][/]";
        if (r.Type == RoomType.ForgottenShrine  && !r.SpecialRoomUsed)          return "[cyan][[F]][/]";
        if (r.EnvironmentalHazard == RoomHazard.BlessedClearing)                return "[green][[*]][/]";
        if (r.EnvironmentalHazard != RoomHazard.None)                           return "[red][[~]][/]";
        if (r.Type == RoomType.Dark)                                            return "[grey][[D]][/]";
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

        // Cooldown state display (Issue #1268) — only shown during combat when abilities are tracked
        if (_cachedCooldowns.Count > 0)
        {
            var cdParts = _cachedCooldowns.Select(c =>
                c.turnsRemaining == 0
                    ? $"[green]{Markup.Escape(c.name)}:✅[/]"
                    : $"[grey]{Markup.Escape(c.name)}:[/][yellow]{c.turnsRemaining}t[/]");
            sb.AppendLine($"[dim]CD:[/] {string.Join("  ", cdParts)}");
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

        UpdateStatsPanel(sb.ToString().TrimEnd());
    }

    private void RenderGearPanel(Player player)
    {
        var sb = new StringBuilder();

        void AddSlot(string slotLabel, Item? item, bool isWeapon = false, bool isAccessory = false)
        {
            if (item == null)
            {
                sb.AppendLine($"[grey]{slotLabel}:[/]  [dim](empty)[/]");
                return;
            }
            var tc = TierColor(item.Tier);
            var statParts = new List<string>();
            if (isWeapon)
            {
                if (item.AttackBonus  != 0) statParts.Add($"[red]+{item.AttackBonus} ATK[/]");
                if (item.DodgeBonus   >  0) statParts.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
                if (item.MaxManaBonus >  0) statParts.Add($"[blue]+{item.MaxManaBonus} mana[/]");
            }
            else if (isAccessory)
            {
                if (item.AttackBonus  != 0) statParts.Add($"[red]+{item.AttackBonus} ATK[/]");
                if (item.DefenseBonus != 0) statParts.Add($"[cyan]+{item.DefenseBonus} DEF[/]");
                if (item.StatModifier != 0) statParts.Add($"[green]+{item.StatModifier} HP[/]");
                if (item.DodgeBonus   >  0) statParts.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
            }
            else
            {
                if (item.DefenseBonus != 0) statParts.Add($"[cyan]+{item.DefenseBonus} DEF[/]");
                if (item.DodgeBonus   >  0) statParts.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
                if (item.MaxManaBonus >  0) statParts.Add($"[blue]+{item.MaxManaBonus} mana[/]");
            }
            var statsStr = statParts.Count > 0 ? "  " + string.Join(", ", statParts) : "";
            sb.AppendLine($"[grey]{slotLabel}:[/]  [{tc}]{Markup.Escape(item.Name)}[/]{statsStr}");
        }

        AddSlot("⚔  Weapon",    player.EquippedWeapon,    isWeapon: true);
        AddSlot("💍 Accessory", player.EquippedAccessory, isAccessory: true);
        AddSlot("🪖 Head",      player.EquippedHead);
        AddSlot("🥋 Shoulders", player.EquippedShoulders);
        AddSlot("🦺 Chest",     player.EquippedChest);
        AddSlot("🧤 Hands",     player.EquippedHands);
        AddSlot("👖 Legs",      player.EquippedLegs);
        AddSlot("👟 Feet",      player.EquippedFeet);
        AddSlot("🧥 Back",      player.EquippedBack);
        AddSlot("🔰 Off-Hand",  player.EquippedOffHand);

        var setDesc = SetBonusManager.GetActiveBonusDescription(player);
        if (!string.IsNullOrEmpty(setDesc))
        {
            sb.AppendLine();
            sb.Append($"[yellow]Set Bonus: {Markup.Escape(setDesc)}[/]");
        }

        var panel = new Panel(new Markup(sb.ToString().TrimEnd()))
            .Header("[bold yellow]⚔  Gear[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Gold1);
        _ctx.UpdatePanel(SpectreLayout.Panels.Gear, panel);
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

        if (_ctx.IsLiveActive)
            SetContent(sb.ToString(), "🏰 DUNGNZ", Color.Red);
        else
            AnsiConsole.Write(new Markup(sb.ToString() + "\n\n"));
    }

    /// <inheritdoc/>
    public void ShowRoom(Room room)
    {
        bool isNewRoom = _cachedRoom?.Id != room.Id;
        _cachedRoom = room;
        _cachedCooldowns = []; // clear combat cooldown HUD when re-entering room context (Issue #1268)

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
        if (isNewRoom)
            AppendLog($"Entered {GetRoomDisplayName(room)}");

        // Auto-populate map and stats panels on room entry
        RenderMapPanel(room);
        if (_cachedPlayer != null)
            RenderStatsPanel(_cachedPlayer);
    }

    /// <inheritdoc/>
    public void ShowCombat(string message)
    {
        _contentHeader = "Combat";
        _contentBorderColor = Color.Red;
        _contentLines.Clear();
        AppendContent($"[bold red]═══ {Markup.Escape(message)} ═══[/]");
        AppendLog(message, "combat");
    }

    /// <inheritdoc/>
    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        var sb = new StringBuilder();

        // Player status
        sb.AppendLine($"⚔  [bold]{Markup.Escape(player.Name)}[/]");
        sb.Append($"HP {BuildHpBar(player.HP, player.MaxHP)} {player.HP}/{player.MaxHP}");
        if (player.MaxMana > 0)
            sb.Append($"  MP {BuildMpBar(player.Mana, player.MaxMana)} {player.Mana}/{player.MaxMana}");
        sb.AppendLine();
        if (playerEffects.Count > 0)
        {
            foreach (var e in playerEffects)
            {
                var col = e.IsBuff ? "purple" : "red";
                sb.Append($"[{col}][[{EffectIcon(e.Effect)}{Markup.Escape(e.Effect.ToString())} {e.RemainingTurns}t]][/] ");
            }
            sb.AppendLine();
        }

        sb.AppendLine();

        // Enemy status
        sb.AppendLine($"🐉 [bold]{Markup.Escape(enemy.Name)}[/]");
        sb.Append($"HP {BuildHpBar(enemy.HP, enemy.MaxHP)} {enemy.HP}/{enemy.MaxHP}");
        sb.Append($"  [red]ATK {enemy.Attack}[/]  [cyan]DEF {enemy.Defense}[/]");
        sb.AppendLine();
        if (enemyEffects.Count > 0)
        {
            foreach (var e in enemyEffects)
            {
                var col = e.IsBuff ? "purple" : "red";
                sb.Append($"[{col}][[{EffectIcon(e.Effect)}{Markup.Escape(e.Effect.ToString())} {e.RemainingTurns}t]][/] ");
            }
            sb.AppendLine();
        }

        if (_contentHeader == "⚔  Combat")
        {
            // Append HP status to existing combat history (don't wipe messages)
            AppendContent("[grey]──────────────────[/]");
            foreach (var line in sb.ToString().TrimEnd().Split('\n'))
                AppendContent(line);
        }
        else
        {
            SetContent(sb.ToString().TrimEnd(), "⚔  Combat", Color.Red);
        }
    }

    /// <inheritdoc/>
    public void ShowCombatMessage(string message)
    {
        var converted = ConvertAnsiInlineToSpectre(message);
        AppendContent($"  {converted}");
        AppendLog(StripAnsiCodes(message), "combat");
    }

    /// <inheritdoc/>
    public void ShowPlayerStats(Player player)
    {
        _cachedPlayer = player;
        RenderStatsPanel(player);
        RenderGearPanel(player);
    }

    /// <inheritdoc/>
    public void UpdateCooldownDisplay(IReadOnlyList<(string name, int turnsRemaining)> cooldowns)
    {
        _cachedCooldowns = cooldowns;
        if (_cachedPlayer != null)
            RenderStatsPanel(_cachedPlayer);
    }

    /// <inheritdoc/>
    public void ShowInventory(Player player)
    {
        var sb = new StringBuilder();
        int currentWeight = player.Inventory.Sum(i => i.Weight);
        int maxWeight     = InventoryManager.MaxWeight;
        var wtColor   = currentWeight > maxWeight * 0.95 ? "red" : currentWeight > maxWeight * 0.8 ? "yellow" : "green";
        var slotColor = player.Inventory.Count >= Player.MaxInventorySize ? "red" : "green";

        sb.AppendLine($"[grey]Slots:[/] [{slotColor}]{player.Inventory.Count}/{Player.MaxInventorySize}[/]  [grey]│  Weight:[/] [{wtColor}]{currentWeight}/{maxWeight}[/]");
        sb.AppendLine();

        if (player.Inventory.Count == 0)
        {
            sb.AppendLine("[grey]  (inventory empty)[/]");
        }
        else
        {
            int idx = 1;
            foreach (var group in player.Inventory.GroupBy(i => i.Name))
            {
                var item     = group.First();
                var count    = group.Count();
                var tc       = TierColor(item.Tier);
                var countStr = count > 1 ? $" [grey]×{count}[/]" : "";
                sb.AppendLine($"  {idx,2}. {ItemIcon(item)} [{tc}]{Markup.Escape(item.Name)}[/]{countStr}  [grey]{Markup.Escape(PrimaryStatLabel(item))}[/]");
                idx++;
            }
        }

        sb.AppendLine();
        sb.Append($"[yellow]💰 {player.Gold}g[/]");
        SetContent(sb.ToString().TrimEnd(), "🎒 Inventory", Color.Yellow);
    }

    /// <inheritdoc/>
    public void ShowLootDrop(Item item, Player player, bool isElite = false)
    {
        var tc     = TierColor(item.Tier);
        var header = isElite ? "[bold yellow]✦ ELITE LOOT DROP[/]" : "[bold yellow]✦ LOOT DROP[/]";
        var stat   = PrimaryStatLabel(item);

        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine($"[{tc}]{item.Tier}[/]");
        sb.AppendLine($"{ItemIcon(item)} [{tc}]{Markup.Escape(item.Name)}[/]");
        sb.Append($"[cyan]{Markup.Escape(stat)}[/]  [grey]{item.Weight} wt[/]");

        if (item.AttackBonus > 0 && player.EquippedWeapon != null)
        {
            int delta = item.AttackBonus - player.EquippedWeapon.AttackBonus;
            if (delta > 0) sb.Append($"  [green](+{delta} vs equipped!)[/]");
        }
        else if (item.DefenseBonus > 0 && player.EquippedChest != null)
        {
            int delta = item.DefenseBonus - player.EquippedChest.DefenseBonus;
            if (delta > 0) sb.Append($"  [green](+{delta} vs equipped!)[/]");
        }

        SetContent(sb.ToString().TrimEnd(), "💰 Loot", Color.Gold1);
        AppendLog($"Loot: {item.Name}", "loot");
    }

    /// <inheritdoc/>
    public void ShowGoldPickup(int amount, int newTotal)
    {
        SetContent($"[yellow]💰 +{amount} gold[/]  [grey](Total: {newTotal}g)[/]", "💰 Gold", Color.Gold1);
        AppendLog($"+{amount} gold (total: {newTotal}g)", "loot");
    }

    /// <inheritdoc/>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax)
    {
        var tc        = TierColor(item.Tier);
        var slotColor = slotsCurrent >= slotsMax         ? "red" : slotsCurrent >= slotsMax * 0.95  ? "yellow" : "green";
        var wtColor   = weightCurrent > weightMax * 0.95 ? "red" : weightCurrent > weightMax * 0.8  ? "yellow" : "green";

        var sb = new StringBuilder();
        sb.AppendLine($"{ItemIcon(item)} Picked up: [{tc}]{Markup.Escape(item.Name)}[/]");
        sb.AppendLine($"[grey]{Markup.Escape(PrimaryStatLabel(item))}[/]");
        sb.AppendLine($"[grey]Slots:[/] [{slotColor}]{slotsCurrent}/{slotsMax}[/]  [grey]·  Weight:[/] [{wtColor}]{weightCurrent}/{weightMax}[/]");
        if (weightCurrent > weightMax * 0.8)
            sb.Append("[yellow]⚠ Inventory nearly full![/]");

        SetContent(sb.ToString().TrimEnd(), "📦 Pickup", Color.Green);
        AppendLog($"Picked up: {item.Name}", "loot");
    }

    /// <inheritdoc/>
    public void ShowItemDetail(Item item)
    {
        var tc = TierColor(item.Tier);
        var sb = new StringBuilder();
        sb.AppendLine($"[grey]Type:[/]    {Markup.Escape(item.Type.ToString())}");
        sb.AppendLine($"[grey]Tier:[/]    [{tc}]{item.Tier}[/]");
        sb.AppendLine($"[grey]Weight:[/]  {item.Weight}");
        if (item.AttackBonus  != 0) sb.AppendLine($"[grey]Attack:[/]  [red]+{item.AttackBonus}[/]");
        if (item.DefenseBonus != 0) sb.AppendLine($"[grey]Defense:[/] [cyan]+{item.DefenseBonus}[/]");
        if (item.HealAmount   != 0) sb.AppendLine($"[grey]Heal:[/]    [green]+{item.HealAmount} HP[/]");
        if (item.ManaRestore  != 0) sb.AppendLine($"[grey]Mana:[/]    [blue]+{item.ManaRestore}[/]");
        if (item.MaxManaBonus != 0) sb.AppendLine($"[grey]Max Mana:[/][blue]+{item.MaxManaBonus}[/]");
        if (item.DodgeBonus   >  0) sb.AppendLine($"[grey]Dodge:[/]   +{item.DodgeBonus:P0}");
        if (item.CritChance   >  0) sb.AppendLine($"[grey]Crit:[/]    +{item.CritChance:P0}");
        if (item.HPOnHit      != 0) sb.AppendLine($"[grey]HP on Hit:[/][green]+{item.HPOnHit}[/]");
        if (item.AppliesBleedOnHit) sb.AppendLine("[grey]Special:[/] [red]Bleed on hit[/]");
        if (item.PoisonImmunity)    sb.AppendLine("[grey]Special:[/] [green]Poison immune[/]");
        if (!string.IsNullOrEmpty(item.Description))
        {
            sb.AppendLine();
            sb.Append($"[grey]{Markup.Escape(item.Description)}[/]");
        }

        SetContent(sb.ToString().TrimEnd(), $"{ItemIcon(item)} {Markup.Escape(item.Name)}", Style.Parse(tc).Foreground);
    }

    /// <inheritdoc/>
    public void ShowMessage(string message)
    {
        var clean = StripAnsiCodes(message);
        AppendContent(Markup.Escape(clean));
        AppendLog(clean, "info");
    }

    /// <inheritdoc/>
    public void ShowError(string message)
    {
        var clean = StripAnsiCodes(message);
        AppendContent($"[red]✗ {Markup.Escape(clean)}[/]");
        AppendLog(clean, "error");
    }

    /// <inheritdoc/>
    public void ShowHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[grey]── Navigation ──[/]");
        sb.AppendLine("[yellow]go [[n|s|e|w]][/]  Move   [grey]│[/]  [yellow]look[/]  Redescribe   [grey]│[/]  [yellow]map[/]  Mini-map");
        sb.AppendLine("[yellow]descend[/]  Descend to the next floor   [grey]│[/]  [yellow]ascend[/]  Return to previous floor");
        sb.AppendLine();
        sb.AppendLine("[grey]── Items ──[/]");
        sb.AppendLine("[yellow]take [[item]][/]   [yellow]use [[item]][/]   [yellow]equip [[item]][/]   [yellow]examine [[target]][/]");
        sb.AppendLine("[yellow]inventory[/]   [yellow]equipment[/]   [yellow]craft [[recipe]][/]   [yellow]shop[/]   [yellow]sell[/]");
        sb.AppendLine();
        sb.AppendLine("[grey]── Character ──[/]");
        sb.AppendLine("[yellow]stats[/]   [yellow]skills[/]   [yellow]learn [[skill]][/]");
        sb.AppendLine();
        sb.AppendLine("[grey]── Systems ──[/]");
        sb.AppendLine("[yellow]save [[name]][/]   [yellow]load [[name]][/]   [yellow]listsaves[/]");
        sb.AppendLine("[yellow]prestige[/]   [yellow]leaderboard[/]   [yellow]help[/]   [yellow]quit[/]");
        SetContent(sb.ToString().TrimEnd(), "❓ Help", Color.Yellow);
    }

    /// <inheritdoc/>
    public void ShowCommandPrompt(Player? player = null)
    {
        var promptContent = "[grey]> Command:[/]";
        var panel = new Panel(new Markup(promptContent))
            .Header("[bold yellow]Command[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
        _ctx.UpdatePanel(SpectreLayout.Panels.Input, panel);
    }

    /// <inheritdoc/>
    public void ShowMap(Room currentRoom, int floor = 1)
    {
        _currentFloor = floor;
        _cachedRoom = currentRoom;
        RenderMapPanel(currentRoom);
    }

    /// <inheritdoc/>
    public void ShowColoredMessage(string message, string color)
    {
        var spectreColor = MapAnsiToSpectre(color);
        var clean = StripAnsiCodes(message);
        AppendContent($"[{spectreColor}]{Markup.Escape(clean)}[/]");
        AppendLog(clean, "info");
    }

    /// <inheritdoc/>
    public void ShowColoredCombatMessage(string message, string color)
    {
        var spectreColor = MapAnsiToSpectre(color);
        var clean = StripAnsiCodes(message);
        AppendContent($"  [{spectreColor}]{Markup.Escape(clean)}[/]");
        AppendLog(clean, "combat");
    }

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string valueColor)
    {
        var spectreColor = MapAnsiToSpectre(valueColor);
        AppendContent($"{Markup.Escape(label),-8} [{spectreColor}]{Markup.Escape(value)}[/]");
    }

    /// <inheritdoc/>
    public void ShowEquipment(Player player)
    {
        var sb = new StringBuilder();

        void AddSlot(string slotLabel, Item? item, bool isWeapon = false, bool isAccessory = false)
        {
            if (item == null)
            {
                sb.AppendLine($"[grey]{slotLabel}:[/]  [dim](empty)[/]");
                return;
            }
            var tc = TierColor(item.Tier);
            var statParts = new List<string>();
            if (isWeapon)
            {
                if (item.AttackBonus  != 0) statParts.Add($"[red]+{item.AttackBonus} ATK[/]");
                if (item.DodgeBonus   >  0) statParts.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
                if (item.MaxManaBonus >  0) statParts.Add($"[blue]+{item.MaxManaBonus} mana[/]");
            }
            else if (isAccessory)
            {
                if (item.AttackBonus  != 0) statParts.Add($"[red]+{item.AttackBonus} ATK[/]");
                if (item.DefenseBonus != 0) statParts.Add($"[cyan]+{item.DefenseBonus} DEF[/]");
                if (item.StatModifier != 0) statParts.Add($"[green]+{item.StatModifier} HP[/]");
                if (item.DodgeBonus   >  0) statParts.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
            }
            else
            {
                if (item.DefenseBonus != 0) statParts.Add($"[cyan]+{item.DefenseBonus} DEF[/]");
                if (item.DodgeBonus   >  0) statParts.Add($"[yellow]+{item.DodgeBonus:P0} dodge[/]");
                if (item.MaxManaBonus >  0) statParts.Add($"[blue]+{item.MaxManaBonus} mana[/]");
            }
            var statsStr = statParts.Count > 0 ? "  " + string.Join(", ", statParts) : "";
            sb.AppendLine($"[grey]{slotLabel}:[/]  [{tc}]{Markup.Escape(item.Name)}[/]{statsStr}");
        }

        AddSlot("⚔  Weapon",    player.EquippedWeapon,    isWeapon: true);
        AddSlot("💍 Accessory", player.EquippedAccessory, isAccessory: true);
        AddSlot("🪖 Head",      player.EquippedHead);
        AddSlot("🥋 Shoulders", player.EquippedShoulders);
        AddSlot("🦺 Chest",     player.EquippedChest);
        AddSlot("🧤 Hands",     player.EquippedHands);
        AddSlot("👖 Legs",      player.EquippedLegs);
        AddSlot("👟 Feet",      player.EquippedFeet);
        AddSlot("🧥 Back",      player.EquippedBack);
        AddSlot("🔰 Off-Hand",  player.EquippedOffHand);

        var setDesc = SetBonusManager.GetActiveBonusDescription(player);
        if (!string.IsNullOrEmpty(setDesc))
        {
            sb.AppendLine();
            sb.Append($"[yellow]Set Bonus: {Markup.Escape(setDesc)}[/]");
        }

        SetContent(sb.ToString().TrimEnd(), "⚔  Equipment", Color.Gold1);
    }

    /// <inheritdoc/>
    public void ShowEnhancedTitle() => ShowTitle();

    /// <inheritdoc/>
    public bool ShowIntroNarrative()
    {
        var lore = "The ancient fortress of [bold]Dungnz[/] has stood for a thousand years — a labyrinthine\n"
                 + "tomb carved into the mountain's heart by hands long since turned to dust. Adventurers\n"
                 + "who descend its spiral corridors speak of riches beyond imagination and horrors beyond\n"
                 + "comprehension. The air below reeks of sulfur and old blood. Torches flicker without wind.\n"
                 + "Something vast and patient watches from the deep.\n\n"
                 + "[yellow][[ Press Enter to begin your descent... ]][/]";
        
        if (_ctx.IsLiveActive)
            SetContent(lore, "📜 Lore", Color.Grey);
        else
            AnsiConsole.Write(new Markup(lore + "\n\n"));
        
        return false;
    }

    /// <inheritdoc/>
    public void ShowPrestigeInfo(PrestigeData prestige)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[yellow]⭐ Prestige Level:[/] [bold]{prestige.PrestigeLevel}[/]");
        sb.AppendLine($"[grey]Wins:[/]  {prestige.TotalWins}");
        sb.AppendLine($"[grey]Runs:[/]  {prestige.TotalRuns}");
        if (prestige.BonusStartAttack  > 0) sb.AppendLine($"[green]Bonus Attack:[/]   +{prestige.BonusStartAttack}");
        if (prestige.BonusStartDefense > 0) sb.AppendLine($"[green]Bonus Defense:[/]  +{prestige.BonusStartDefense}");
        if (prestige.BonusStartHP      > 0) sb.AppendLine($"[green]Bonus HP:[/]       +{prestige.BonusStartHP}");
        
        var content = sb.ToString().TrimEnd();
        if (_ctx.IsLiveActive)
            SetContent(content, "⭐ Prestige", Color.Yellow);
        else
            AnsiConsole.Write(new Markup(content + "\n\n"));
    }

    /// <inheritdoc/>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[bold yellow]🏪 Merchant[/]  [grey]Your gold:[/] [yellow]{playerGold}g[/]");
        sb.AppendLine();
        int idx = 1;
        foreach (var (item, price) in stock)
        {
            var tc        = TierColor(item.Tier);
            var canAfford = playerGold >= price;
            var priceStr  = canAfford ? $"[green]{price}g[/]" : $"[grey]{price}g[/]";
            var nameMk    = canAfford ? $"[{tc}]{Markup.Escape(item.Name)}[/]" : $"[grey]{Markup.Escape(item.Name)}[/]";
            sb.AppendLine($"  {idx,2}. {ItemIcon(item)} {nameMk}  [grey]{Markup.Escape(PrimaryStatLabel(item))}[/]  {priceStr}");
            idx++;
        }
        SetContent(sb.ToString().TrimEnd(), "🏪 Shop", Color.Yellow);
    }

    /// <inheritdoc/>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[bold yellow]💰 Sell Items[/]  [grey]Your gold:[/] [yellow]{playerGold}g[/]");
        sb.AppendLine();
        int idx = 1;
        foreach (var (item, sellPrice) in items)
        {
            var tc = TierColor(item.Tier);
            sb.AppendLine($"  {idx,2}. {ItemIcon(item)} [{tc}]{Markup.Escape(item.Name)}[/]  [green]+{sellPrice}g[/]");
            idx++;
        }
        SetContent(sb.ToString().TrimEnd(), "💰 Sell", Color.Green);
    }

    /// <inheritdoc/>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients)
    {
        var tc = TierColor(result.Tier);
        var sb = new StringBuilder();
        sb.AppendLine($"[grey]Result:[/]  {ItemIcon(result)} [{tc}]{Markup.Escape(result.Name)}[/]");
        sb.AppendLine($"[grey]Stats:[/]   [cyan]{Markup.Escape(PrimaryStatLabel(result))}[/]");
        sb.AppendLine();
        sb.AppendLine("[grey]Ingredients:[/]");
        foreach (var (ingredient, hasIt) in ingredients)
        {
            var check    = hasIt ? "[green]✅[/]" : "[red]❌[/]";
            var ingColor = hasIt ? "white" : "grey";
            sb.AppendLine($"  {check} [{ingColor}]{Markup.Escape(ingredient)}[/]");
        }
        SetContent(sb.ToString().TrimEnd(), $"🔨 {Markup.Escape(recipeName)}", Color.Yellow);
    }

    /// <inheritdoc/>
    public void ShowCombatStart(Enemy enemy)
    {
        _contentLines.Clear();
        _contentHeader = "⚔  Combat";
        _contentBorderColor = Color.Red;
        AppendContent("[bold red]⚔ ─── COMBAT ─── ⚔[/]");
        AppendContent($"[red]Enemy: {Markup.Escape(enemy.Name)}[/]");
        AppendLog($"Combat started: {enemy.Name}", "combat");
    }

    /// <inheritdoc/>
    public void ShowCombatEntryFlags(Enemy enemy)
    {
        if (enemy.IsElite)
            AppendContent("[yellow]⭐ ELITE — enhanced stats and loot[/]");
        if (enemy is Dungnz.Systems.Enemies.DungeonBoss boss && boss.IsEnraged)
            AppendContent("[bold red]⚡ ENRAGED[/]");
    }

    /// <inheritdoc/>
    public void ShowLevelUpChoice(Player player)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[bold yellow]🎉 LEVEL UP! You are now level {player.Level}[/]");
        sb.AppendLine();
        sb.AppendLine("[grey]Choose a stat to increase:[/]");
        sb.AppendLine($"  [yellow]1.[/] +5 Max HP    [grey]({player.MaxHP} → {player.MaxHP + 5})[/]");
        sb.AppendLine($"  [yellow]2.[/] +2 Attack    [grey]({player.Attack} → {player.Attack + 2})[/]");
        sb.Append($"  [yellow]3.[/] +2 Defense   [grey]({player.Defense} → {player.Defense + 2})[/]");
        SetContent(sb.ToString().TrimEnd(), "🎉 Level Up!", Color.Yellow);
    }

    /// <inheritdoc/>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)
    {
        _currentFloor = floor;
        var threatColor = floor <= 2 ? "green" : floor <= 4 ? "yellow" : "red";
        var threat      = floor <= 2 ? "Low"   : floor <= 4 ? "Moderate" : "High";
        var sb = new StringBuilder();
        sb.AppendLine($"[{threatColor} bold]FLOOR {floor} OF {maxFloor}[/]");
        sb.AppendLine($"[white]{Markup.Escape(variant.Name)}[/]");
        sb.Append($"[{threatColor}]⚠ Danger: {threat}[/]");
        SetContent(sb.ToString().TrimEnd(), $"⬇  Floor {floor}", Color.Cyan1);
        AppendLog($"Entered Floor {floor} — {variant.Name}");
        if (_cachedRoom != null) RenderMapPanel(_cachedRoom);
    }

    /// <inheritdoc/>
    public void ShowEnemyDetail(Enemy enemy)
    {
        var nameColor = enemy.IsElite ? "yellow" : "red";
        var sb = new StringBuilder();
        sb.AppendLine($"HP  {BuildHpBar(enemy.HP, enemy.MaxHP)} {enemy.HP}/{enemy.MaxHP}");
        sb.AppendLine($"[red]ATK: {enemy.Attack}[/]   [cyan]DEF: {enemy.Defense}[/]");
        sb.Append($"[green]XP: {enemy.XPValue}[/]");
        if (enemy.IsElite) sb.Append("  [yellow]⭐ ELITE[/]");
        SetContent(sb.ToString().TrimEnd(), $"[{nameColor}]{Markup.Escape(enemy.Name.ToUpperInvariant())}[/]", Color.Red);
    }

    /// <inheritdoc/>
    public void ShowVictory(Player player, int floorsCleared, RunStats stats)
    {
        var floorWord = floorsCleared == 1 ? "floor" : "floors";
        var sb = new StringBuilder();
        sb.AppendLine("[bold gold1]✦  V I C T O R Y  ✦[/]");
        sb.AppendLine();
        sb.AppendLine($"[bold]{Markup.Escape(player.Name)}[/]  •  Level {player.Level}  •  {Markup.Escape(player.Class.ToString())}");
        sb.AppendLine($"[yellow]{floorsCleared} {floorWord} conquered[/]");
        sb.AppendLine();
        sb.AppendLine($"[grey]Enemies slain:[/]  {stats.EnemiesDefeated}");
        sb.AppendLine($"[grey]Gold earned:[/]    {stats.GoldCollected}");
        sb.AppendLine($"[grey]Items found:[/]    {stats.ItemsFound}");
        sb.Append($"[grey]Turns taken:[/]    {stats.TurnsTaken}");
        SetContent(sb.ToString().TrimEnd(), "🏆 Victory!", Color.Gold1);
    }

    /// <inheritdoc/>
    public void ShowGameOver(Player player, string? killedBy, RunStats stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[bold red]☠  RUN ENDED  ☠[/]");
        sb.AppendLine();
        sb.AppendLine($"[bold]{Markup.Escape(player.Name)}[/]  •  Level {player.Level}  •  {Markup.Escape(player.Class.ToString())}");
        if (!string.IsNullOrEmpty(killedBy))
            sb.AppendLine($"[red]Killed by: {Markup.Escape(killedBy)}[/]");
        sb.AppendLine();
        sb.AppendLine($"[grey]Enemies slain:[/]  {stats.EnemiesDefeated}");
        sb.AppendLine($"[grey]Floors reached:[/] {stats.FloorsVisited}");
        sb.AppendLine($"[grey]Gold earned:[/]    {stats.GoldCollected}");
        sb.AppendLine($"[grey]Items found:[/]    {stats.ItemsFound}");
        sb.Append($"[grey]Turns taken:[/]    {stats.TurnsTaken}");
        SetContent(sb.ToString().TrimEnd(), "☠  Game Over", Color.DarkRed);
    }

    /// <inheritdoc/>
    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt == null || enemy.AsciiArt.Length == 0)
            return;
        var artColor = enemy.IsElite ? "yellow" : "red";
        var art = string.Join("\n", enemy.AsciiArt.Select(l => Markup.Escape(l)));
        AppendContent($"[{artColor}]{art}[/]");
    }

    /// <inheritdoc/>
    public void RefreshDisplay(Player player, Room room, int floor)
    {
        _currentFloor = floor;  // set before ShowRoom so map header uses correct floor
        ShowPlayerStats(player);
        ShowRoom(room);         // ShowRoom already calls RenderMapPanel
    }

    // ── Private static helpers ────────────────────────────────────────────────

    private static string ItemTypeIcon(ItemType type) => type switch
    {
        ItemType.Weapon           => "⚔",
        ItemType.Armor            => "🦺",
        ItemType.Consumable       => "🧪",
        ItemType.Accessory        => "💍",
        ItemType.CraftingMaterial => "⚗",
        _                         => "•"
    };

    private static string SlotIcon(ArmorSlot slot) => slot switch
    {
        ArmorSlot.Head      => "🪖",
        ArmorSlot.Shoulders => "🥋",
        ArmorSlot.Chest     => "🦺",
        ArmorSlot.Hands     => "🧤",
        ArmorSlot.Legs      => "👖",
        ArmorSlot.Feet      => "👟",
        ArmorSlot.Back      => "🧥",
        ArmorSlot.OffHand   => "🔰",
        _                   => "🦺",
    };

    private static string ItemIcon(Item item) =>
        item.Type == ItemType.Armor ? SlotIcon(item.Slot) : ItemTypeIcon(item.Type);

    private static string EffectIcon(StatusEffect effect) => effect switch
    {
        StatusEffect.Poison    => "☠",
        StatusEffect.Bleed     => "🩸",
        StatusEffect.Stun      => "⚡",
        StatusEffect.Regen     => "✨",
        StatusEffect.Fortified => "🛡",
        StatusEffect.Weakened  => "💀",
        StatusEffect.Slow      => ">",
        StatusEffect.BattleCry => "!",
        StatusEffect.Burn      => "*",
        StatusEffect.Freeze    => "~",
        StatusEffect.Silence   => "X",
        StatusEffect.Curse     => "@",
        _                      => "●"
    };

    private static string MapAnsiToSpectre(string ansiCode) => ansiCode switch
    {
        var c when c == ColorCodes.Red       || c == ColorCodes.BrightRed   => "red",
        var c when c == ColorCodes.Green                                    => "green",
        var c when c == ColorCodes.Yellow                                   => "yellow",
        var c when c == ColorCodes.Cyan                                     => "cyan",
        var c when c == ColorCodes.Gray                                     => "grey",
        var c when c == ColorCodes.Blue                                     => "blue",
        var c when c == ColorCodes.BrightWhite                              => "white",
        _                                                                   => "white"
    };

    private static string StripAnsiCodes(string input) =>
        AnsiEscapePattern.Replace(input, string.Empty);

    private static string ConvertAnsiInlineToSpectre(string input)
    {
        var matches = AnsiEscapePattern.Matches(input);
        if (matches.Count == 0)
            return Markup.Escape(input);

        var result = new StringBuilder();
        var lastIndex = 0;
        var isBold = false;
        var currentColor = "";
        var isTagOpen = false;

        foreach (Match match in matches)
        {
            // Guard: skip matches that overlap an already-processed region.
            // On well-formed input this never fires, but malformed or
            // overlapping ANSI sequences could produce match.Index < lastIndex,
            // which would make the Substring length negative and throw.
            if (match.Index < lastIndex)
                continue;

            // Append text before this match (escaped)
            if (match.Index > lastIndex)
            {
                var plainText = input.Substring(lastIndex, match.Index - lastIndex);
                
                // If we have bold or color accumulated, open tag before text
                if ((isBold || !string.IsNullOrEmpty(currentColor)) && !string.IsNullOrEmpty(plainText))
                {
                    result.Append('[');
                    if (isBold)
                        result.Append("bold ");
                    if (!string.IsNullOrEmpty(currentColor))
                        result.Append(currentColor);
                    result.Append(']');
                    isTagOpen = true;
                }
                
                result.Append(Markup.Escape(plainText));
            }

            // Process ANSI code
            var code = match.Value;
            if (code == "\u001b[0m") // Reset
            {
                if (isTagOpen)
                {
                    result.Append("[/]");
                    isTagOpen = false;
                }
                isBold = false;
                currentColor = "";
            }
            else if (code == "\u001b[1m") // Bold
            {
                isBold = true;
            }
            else // Color code
            {
                currentColor = code switch
                {
                    "\u001b[91m" => "red",
                    "\u001b[32m" => "green",
                    "\u001b[33m" => "yellow",
                    "\u001b[36m" => "cyan",
                    "\u001b[37m" => "grey",
                    "\u001b[34m" => "blue",
                    "\u001b[97m" => "white",
                    _ => ""
                };
            }

            lastIndex = match.Index + match.Length;
        }

        // Append remaining text
        if (lastIndex < input.Length)
        {
            var plainText = input.Substring(lastIndex);
            if ((isBold || !string.IsNullOrEmpty(currentColor)) && !string.IsNullOrEmpty(plainText))
            {
                result.Append('[');
                if (isBold)
                    result.Append("bold ");
                if (!string.IsNullOrEmpty(currentColor))
                    result.Append(currentColor);
                result.Append(']');
                isTagOpen = true;
            }
            result.Append(Markup.Escape(plainText));
        }

        // Close tag if still open
        if (isTagOpen)
            result.Append("[/]");

        return result.ToString();
    }
}


