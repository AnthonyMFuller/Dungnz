# Terminal.Gui Migration Architecture

**Date:** 2025-07-21
**Author:** Coulson (Lead)
**Status:** Approved — ready for implementation
**Requested by:** Anthony (Boss)

---

## Executive Summary

Migrate Dungnz's display layer from Spectre.Console to Terminal.Gui v2, enabling a
split-screen TUI with persistent map, stats, combat log, and command input panels.
The existing Spectre.Console implementation remains fully functional via a `--tui`
feature flag — zero risk to current gameplay.

---

## Architectural Decisions

### AD-1: Dual-Thread Model (Game Thread + UI Thread)

**Decision:** Run Terminal.Gui's `Application.Run()` on the main thread and the game
logic (`StartupOrchestrator` → `GameLoop` → `CombatEngine`) on a background thread.

**Rationale:**
- Terminal.Gui requires `Application.Run()` to own the main thread (it's an event loop)
- The existing `GameLoop.RunLoop()` is a blocking `while(true)` loop that calls
  `_input.ReadLine()` — it CANNOT run on the UI thread without deadlocking
- Background thread lets GameLoop, CombatEngine, and all command handlers remain
  100% unchanged — they still call `IDisplayService` methods synchronously
- Display methods marshal to the UI thread via `Application.Invoke()`
- Input methods block the game thread via `TaskCompletionSource<T>` or
  `BlockingCollection<T>` until the user provides input through the TUI

**Alternatives rejected:**
- Converting GameLoop to async/event-driven: Would require rewriting GameLoop,
  CombatEngine, all 20+ command handlers, and IntroSequence — massive risk, 3x effort
- Running Terminal.Gui on a background thread: Terminal.Gui explicitly requires the
  main thread for signal handling and terminal control

### AD-2: Feature Flag with `--tui` CLI Argument

**Decision:** Add a `--tui` command-line flag to Program.cs. Default behavior remains
Spectre.Console. When `--tui` is passed, Terminal.Gui is used instead.

**Rationale:**
- Zero risk to existing players — default path is unchanged
- Easy rollback: remove the flag and the `Display/Tui/` directory
- Enables incremental development: TUI can be partially implemented and tested
  while the game remains playable via Spectre
- CI/CD can test both paths independently

**Implementation:**
```csharp
var useTui = args.Contains("--tui");

if (useTui)
{
    Application.Init();
    var layout = new TuiLayout();
    IDisplayService display = new TerminalGuiDisplayService(layout);
    IInputReader input = new TerminalGuiInputReader(layout);

    var gameThread = new Thread(() => RunGame(display, input, args))
    {
        IsBackground = true,
        Name = "GameLogic"
    };
    gameThread.Start();
    Application.Run(layout.MainWindow);
    Application.Shutdown();
}
else
{
    // Existing Spectre.Console path — UNCHANGED
    IDisplayService display = new SpectreDisplayService();
    IInputReader input = new ConsoleInputReader();
    RunGame(display, input, args);
}
```

### AD-3: New Files Only — No Modifications to Existing Display Code

**Decision:** All Terminal.Gui code lives in `Display/Tui/` as new files.
`SpectreDisplayService.cs`, `DisplayService.cs`, `IDisplayService.cs`, and
`IInputReader.cs` are NOT modified.

**Rationale:**
- Additive changes only — every PR leaves the game working on master
- IDisplayService is the abstraction boundary; Terminal.Gui is just another implementation
- If the migration fails, delete `Display/Tui/` and the `--tui` flag — done

### AD-4: Thread-Safe UI Marshaling Pattern

**Decision:** All `IDisplayService` methods in `TerminalGuiDisplayService` use
`Application.Invoke()` to marshal work to the UI thread.

**Pattern for pure output methods:**
```csharp
public void ShowMessage(string message)
{
    Application.Invoke(() =>
    {
        _layout.ContentPanel.AppendText(message);
        _layout.LogPanel.AppendLine(message);
    });
}
```

**Pattern for input-coupled methods:**
```csharp
public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
{
    var tcs = new TaskCompletionSource<string>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    Application.Invoke(() =>
    {
        var dialog = new TuiMenuDialog<string>(
            "Combat",
            new[] {
                ("⚔ Attack", "A"),
                ("✨ Ability", "B"),
                ("🏃 Flee", "F")
            });
        dialog.OnSelected += result => tcs.SetResult(result);
        dialog.OnCancelled += () => tcs.SetResult("A"); // default
        Application.Run(dialog);
    });

    return tcs.Task.GetAwaiter().GetResult(); // blocks game thread
}
```

### AD-5: Split-Screen Layout

**Decision:** Four-panel layout with persistent sidebar.

```
┌─────────────────────────┬──────────────────┐
│                         │   Player Stats   │
│     Dungeon Map         │  HP: ██████ 80%  │
│     (ASCII/BFS)         │  MP: ████   60%  │
│                         │  ATK: 15 DEF: 8  │
│     [@] ─── [?]         │  Gold: 250       │
│      |                  │  Floor: 3/5      │
│     [M] ─── [E]         │  Class: Warrior  │
│                         ├──────────────────┤
│                         │   Equipment      │
│                         │  ⚔ Iron Sword    │
│                         │  🛡 Chain Mail   │
├─────────────────────────┴──────────────────┤
│              Main Content                   │
│  You enter a dark, mossy chamber.           │
│  Exits: North, East                         │
│  A Goblin lurks in the shadows!             │
├─────────────────────────────────────────────┤
│              Message Log                    │
│  > You attack the Goblin for 12 damage.     │
│  > The Goblin strikes back for 5 damage.    │
│  > You found a Steel Sword!                 │
├─────────────────────────────────────────────┤
│ > _                                         │
└─────────────────────────────────────────────┘
```

**Panel responsibilities:**
- **Map Panel** (top-left, ~60% width): ASCII dungeon map, auto-updates on room change
- **Stats Panel** (top-right, ~40% width): Player HP/MP bars, stats, gold, floor info
- **Equipment Sub-panel** (below stats): Currently equipped items
- **Content Panel** (middle, full width): Room descriptions, combat text, loot cards,
  victory/game-over screens — the main narrative area
- **Message Log** (below content, full width): Scrollable history of all messages,
  color-coded by type
- **Command Input** (bottom, full width): Text field for player commands, Enter to submit

### AD-6: Input-Coupled Method Strategy

**Decision:** Input-coupled methods use Terminal.Gui modal dialogs (`Dialog` subclass)
that overlay the main layout. The game thread blocks via `TaskCompletionSource<T>`
until the user makes a selection.

**The 19 input-coupled methods and their TUI equivalents:**

| IDisplayService Method | TUI Implementation |
|---|---|
| `ReadPlayerName()` | Text input dialog |
| `ReadSeed()` | Numeric input dialog |
| `SelectDifficulty()` | List dialog (3 options) |
| `SelectClass(prestige)` | List dialog with stat preview |
| `ShowStartupMenu(hasSaves)` | List dialog (New/Load/Seed/Exit) |
| `SelectSaveToLoad(saves)` | List dialog |
| `ShowConfirmMenu(prompt)` | Yes/No dialog |
| `ShowCombatMenuAndSelect(player, enemy)` | List dialog (Attack/Ability/Flee) |
| `ShowAbilityMenuAndSelect(...)` | List dialog with cooldown info |
| `ShowCombatItemMenuAndSelect(consumables)` | List dialog |
| `ShowInventoryAndSelect(player)` | List dialog |
| `ShowEquipMenuAndSelect(equippable)` | List dialog |
| `ShowUseMenuAndSelect(usable)` | List dialog |
| `ShowTakeMenuAndSelect(roomItems)` | List dialog |
| `ShowShopAndSelect / ShowShopWithSellAndSelect` | List dialog with prices |
| `ShowSellMenuAndSelect(items, gold)` | List dialog with sell prices |
| `ShowLevelUpChoiceAndSelect(player)` | List dialog (HP/ATK/DEF) |
| `ShowCraftMenuAndSelect(recipes)` | List dialog with availability |
| `ShowShrineMenuAndSelect(...)` | List dialog with costs |
| `ShowForgottenShrineMenuAndSelect()` | List dialog |
| `ShowContestedArmoryMenuAndSelect(def)` | List dialog |
| `ShowTrapChoiceAndSelect(...)` | List dialog |
| `ShowSkillTreeMenu(player)` | List dialog with skill info |

All use the same `TuiMenuDialog<T>` base with customizable rendering.

---

## File Structure

```
Display/
├── IDisplayService.cs          (UNCHANGED)
├── DisplayService.cs           (UNCHANGED)
├── SpectreDisplayService.cs    (UNCHANGED)
└── Tui/
    ├── TerminalGuiDisplayService.cs   (implements IDisplayService)
    ├── TerminalGuiInputReader.cs      (implements IInputReader)
    ├── TuiLayout.cs                   (main Window + panel arrangement)
    ├── TuiMenuDialog.cs               (reusable modal selection dialog)
    └── Panels/
        ├── MapPanel.cs                (dungeon map rendering)
        ├── StatsPanel.cs              (player stats + equipment)
        ├── ContentPanel.cs            (main narrative/display area)
        └── MessageLogPanel.cs         (scrollable message history)
```

---

## Threading Model

```
Main Thread                          Game Thread
───────────                          ───────────
Application.Init()
                                     StartupOrchestrator.Run()
Application.Run(layout)              │
  │                                  ├── display.ShowTitle()
  │ ◄─── Application.Invoke() ──────┤     → marshals to UI thread
  │      updates Map panel           │
  │                                  ├── display.SelectDifficulty()
  │ ◄─── Application.Invoke() ──────┤     → shows modal dialog
  │      shows Dialog                │     → blocks on TCS
  │      user selects "Hard"         │
  │      tcs.SetResult(Hard) ───────►│     → unblocks, returns Hard
  │                                  │
  │                                  ├── GameLoop.Run()
  │                                  │   └── while(true)
  │                                  │       ├── display.ShowCommandPrompt()
  │                                  │       │     → updates input field focus
  │                                  │       ├── input.ReadLine()
  │                                  │       │     → blocks on BlockingCollection
  │ user types "go north" + Enter    │       │
  │ collection.Add("go north") ─────►│       │     → unblocks, returns "go north"
  │                                  │       ├── handler.Handle("north", ctx)
  │                                  │       │   ├── display.ShowRoom(room)
  │ ◄─── Application.Invoke() ──────│       │   │     → updates Content + Map
  │                                  │       │   └── display.ShowCombatStart(enemy)
  │ ◄─── Application.Invoke() ──────│       │         → updates Content
  │                                  │       └── (loop continues)
  │                                  │
  │                                  └── (game ends)
  │ ◄─── Application.RequestStop() ─┘
Application.Shutdown()
```

---

## Rollback Strategy

1. **Feature flag:** `--tui` is opt-in. Default path is Spectre.Console, unchanged.
2. **Additive code:** All TUI code lives in `Display/Tui/`. No existing files modified
   except `Program.cs` (which gets a small `if (useTui)` branch) and `Dungnz.csproj`
   (which gets the Terminal.Gui NuGet reference).
3. **To rollback:** Remove `Display/Tui/` directory, revert the 2 modified files. Done.
4. **Zero regression risk:** The Spectre.Console path is never touched during this work.
   Every PR should pass CI with the default (non-TUI) path.

---

## Implementation Order

1. TG-01: Project setup (NuGet + flag + directory)
2. TG-02: TUI layout scaffold
3. TG-03: Thread bridge + TerminalGuiInputReader
4. TG-04: Pure output methods
5. TG-05: Menu dialog system
6. TG-06: Input-coupled methods
7. TG-07 through TG-10: Panel implementations (parallelizable)
8. TG-11: Wire Program.cs dual-path startup
9. TG-12: Integration testing
10. TG-13: Documentation

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Thread deadlock between game and UI threads | Medium | High | Use `TaskCreationOptions.RunContinuationsAsynchronously`; never call `Application.Invoke()` synchronously from UI thread |
| Terminal.Gui v2 API instability | Low | Medium | Pin to specific version; wrap all TG calls in our own panel classes |
| Unicode/emoji rendering differences | Medium | Low | Terminal.Gui supports Unicode; test on common terminals (xterm, Windows Terminal, iTerm2) |
| Modal dialogs blocking UI updates | Low | Medium | Dialogs are transient; background panels update when dialog closes |
| Performance with large maps | Low | Low | Map panel only renders visible portion; BFS already handles this |

---

## Dependencies

- **Terminal.Gui v2** (NuGet: `Terminal.Gui >= 2.0.0`)
- **.NET 10.0** (already in use)
- **No changes to:** IDisplayService, IInputReader, IMenuNavigator, GameLoop, CombatEngine, any command handlers, any models, any systems
