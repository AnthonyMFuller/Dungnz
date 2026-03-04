# TUI Architecture: Terminal.Gui Integration

This document describes the Terminal.Gui TUI implementation, including the dual-thread model, display coordination, and file structure.

## Overview

The TUI mode provides a split-screen interface powered by [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) v1.19. It runs on the main thread while the game loop executes on a background thread, coordinated through the **GameThreadBridge**.

## Dual-Thread Model

```
Main Thread (Terminal.Gui event loop):
  ┌─────────────────────────────────┐
  │ Application.Run(MainWindow)     │
  │ ├─ Map Panel (BrightGreen/Black)│
  │ ├─ Stats Panel (BrightCyan/Black│
  │ ├─ Content Area (White/Blue)    │
  │ ├─ Message Log (White/Black)    │
  │ └─ Command Input (Yellow/Black) │
  └──────────┬──────────────────────┘
             │ Application.MainLoop.Invoke()
             │ (all UI updates marshalled here)
Background Thread (game loop):
  ┌──────────▼──────────────────────┐
  │ GameLoop / StartupOrchestrator  │
  │ ├─ Command processing           │
  │ ├─ Combat/logic execution       │
  │ └─ Display calls → bridge       │
  └─────────────────────────────────┘
```

### Communication

- **Background → Main:** `GameThreadBridge.InvokeOnUiThread(action)` — calls `Application.MainLoop.Invoke(action)`
- **Background → Main (blocking):** `GameThreadBridge.InvokeOnUiThreadAndWait(action/func)` — uses `TaskCompletionSource` to wait for result
- **Main → Background:** Commands posted via `bridge.PostCommand(string)`, consumed via `bridge.WaitForCommand()` (blocking `BlockingCollection<string>`)
- **UI readiness sync:** `GameThreadBridge.SetUiReady()` is called from `MainWindow.Loaded`. Background thread early calls wait up to 5 s via `ManualResetEventSlim` before giving up.

## GameThreadBridge

`Display/Tui/GameThreadBridge.cs`

```csharp
// Background → Main: fire-and-forget
GameThreadBridge.InvokeOnUiThread(() => { /* UI work */ });

// Background → Main: blocking with return value
var result = GameThreadBridge.InvokeOnUiThreadAndWait(() => dialog.ShowAndGetResult());

// Main → Background: post a command
bridge.PostCommand("go north");

// Background: block waiting for next command
string? cmd = bridge.WaitForCommand();

// Program.cs: signal UI is ready (called from MainWindow.Loaded)
GameThreadBridge.SetUiReady();
```

**Key properties:**
- Uses `BlockingCollection<string>` for command queue (thread-safe, no locks needed)
- `InvokeOnUiThreadAndWait` uses `TaskCompletionSource` — no spinwait, no polling
- `InvokeOnUiThread` waits up to 5 s for `_uiReady` before calling `MainLoop.Invoke`

## Panel Layout

The TUI is divided into five regions (all with high-contrast ColorSchemes):

| Panel | Position | Color Scheme | Content |
|-------|----------|--------------|---------|
| Map | Top-left 60% | BrightGreen on Black | ASCII dungeon map with `[@]` player marker |
| Stats | Top-right 40% | BrightCyan on Black | HP/MP bars, ATK/DEF/Gold, equipment |
| Adventure | Middle 50% | White on Blue | Room description, combat text, inventory |
| Message Log | Lower 15% | White on Black | Timestamped log with ⚔/💰/❌/ℹ prefixes |
| Command | Bottom fill | BrightYellow on Black | Text input field |

### Auto-population (#1038/#1039)

`TerminalGuiDisplayService.ShowRoom(room)` automatically refreshes both the Map and Stats panels on every room entry. No explicit `MAP` or `STATS` command needed.

- `ShowMap(room, floor)` and `ShowPlayerStats(player)` cache their parameters.
- `ShowRoom` reads the cached floor and player to call both renderers.

## File Structure

```
Display/Tui/
├── GameThreadBridge.cs
│   └─ Thread sync: BlockingCollection commands, ManualResetEventSlim UI-ready,
│      InvokeOnUiThread / InvokeOnUiThreadAndWait
│
├── TerminalGuiDisplayService.cs
│   └─ IDisplayService implementation — all display methods marshal via GameThreadBridge
│      ├─ ShowRoom() — renders room + auto-refreshes map and stats
│      ├─ ShowMap() / ShowPlayerStats() — update persistent TextViews
│      ├─ ShowColoredMessage/CombatMessage() — routes to log with typed prefix
│      └─ ShowSkillTreeMenu() — TuiMenuDialog<Skill?> selection
│
├── TuiLayout.cs
│   └─ Panel definitions, ColorSchemes, persistent TextViews for map/stats
│      ├─ SetMap(text) — updates _mapView.Text in place (no destroy/recreate)
│      └─ SetStats(text) — updates _statsView.Text in place
│
├── TuiColorMapper.cs
│   └─ Maps ANSI color codes → Terminal.Gui Color values
│      Used by ShowColoredMessage to pick log message type
│
├── TuiMenuDialog.cs
│   └─ Generic interactive menu: TuiMenuDialog<T>(title, options, default)
│      ShowAndGetResult() blocks on UI thread, returns selected value
│
└── TerminalGuiInputReader.cs
    └─ IInputReader — ReadLine() blocks on bridge.WaitForCommand()
```

## Initialization Sequence

```
Program.cs (--tui flag):
  1. Application.Init()
  2. new TuiLayout()              — ColorSchemes applied, persistent TextViews created
  3. new GameThreadBridge()
  4. new TerminalGuiInputReader(bridge)
  5. new TerminalGuiDisplayService(layout)
  6. layout.MainWindow.Loaded += () => GameThreadBridge.SetUiReady()
  7. Task.Run(() => game loop)    — background thread; waits for _uiReady before first Invoke
  8. Application.Run(layout.MainWindow)   — blocks main thread; sets _uiReady via Loaded
  9. Application.Shutdown()
```

## Known Limitations

- **No inline color in TextViews:** Terminal.Gui v1 TextViews don't support ANSI sequences. Color distinction is achieved through panel-level ColorSchemes and message log prefixes.
- **Terminal Size:** Minimum 100×30 recommended.
- **Unicode Support:** Requires UTF-8 terminal.

