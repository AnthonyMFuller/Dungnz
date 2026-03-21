# Hill — History (Full archive: history-archive.md)

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Core Context

**Summarized:** All entries prior to Avalonia P5 archived to history-archive.md on 2025-07-18.

**Key Technical Patterns Established:**
- `AsciiArt` on Enemy is `string[]` (not string) — use `string.Join("\n", enemy.AsciiArt)`
- Enemy model has no Description property on base class

---

## Avalonia P5 — Input Panel + ReadCommandInput (2025-07-17)

**PR:** #1405 | **Branch:** `squad/avalonia-p5-input-panel`

### What Was Implemented

Implemented the TCS-based input bridge between the synchronous game thread and the Avalonia UI thread. This is the critical-path item that enables the game to actually accept player commands.

**Files changed (6):**

| File | Change |
|------|--------|
| `AvaloniaInputReader.cs` | **New.** `IInputReader` implementation using `TaskCompletionSource<string?>` to bridge `ReadLine()` to Avalonia TextBox. `ReadKey()` returns null, `IsInteractive` returns false (P6 will enable). |
| `AvaloniaDisplayService.cs` | Replaced `ReadCommandInput() => null` stub with full TCS implementation. Subscribes/unsubscribes to `InputSubmitted` per-call. |
| `InputPanelViewModel.cs` | Added `IsInputEnabled` (observable), `InputSubmitted` event, `Submit()` method. Submit trims text, clears, disables, then fires event. |
| `InputPanel.axaml` | Added `x:Name="CommandInput"`, `IsEnabled="{Binding IsInputEnabled}"` binding. |
| `InputPanel.axaml.cs` | Enter key handling via tunnel `KeyDown` handler. Auto-focus via `PropertyChanged` on `IsEnabledProperty`. |
| `App.axaml.cs` | Replaced `ConsoleInputReader()` temp stub with `AvaloniaInputReader(mainVM.Input)`. |

### Architecture: TCS Pattern

```
Game Thread                          UI Thread
───────────                          ─────────
ReadCommandInput() called
  → creates new TCS
  → dispatches to UI: enable input   → TextBox becomes enabled, focused
  → blocks on TCS.Task.Result
                                     User types "go north", presses Enter
                                     → TCS.TrySetResult("go north")
ReadCommandInput() unblocks ←────────
  → returns "go north"               → TextBox cleared, disabled
```

**Key detail:** `TaskCreationOptions.RunContinuationsAsynchronously` prevents the continuation from running on the UI thread (which would deadlock).

### Design Decisions

1. **Two consumers, one event** — Both `AvaloniaInputReader.ReadLine()` and `AvaloniaDisplayService.ReadCommandInput()` use `InputPanelViewModel.InputSubmitted`. Safe because game thread is single-threaded.
2. **Tunnel handler for Enter** — Used `RoutingStrategies.Tunnel` so the handler fires before the TextBox processes Enter (which would insert a newline in multiline mode).
3. **`IsInteractive = false`** — Forces numbered text prompts instead of arrow-key menus. P6 will flip to `true` when key navigation is ready.
4. **Build error fix** — Initial approach used `GetObservable(IsEnabledProperty)` which was ambiguous between `AvaloniaObjectExtensions` and `InteractiveExtensions`. Switched to `PropertyChanged` event handler.

### Validation

- ✅ `dotnet build Dungnz.slnx --no-incremental` — 0 errors
- ✅ `dotnet test` — 2,154 passed, 0 failed, 4 skipped
- ✅ Only `Dungnz.Display.Avalonia/` files touched

### Next Steps

**P6:** Menu implementations — selection prompts for difficulty, class, combat, inventory, shop menus. Will use overlay dialogs or in-panel selection lists.
**P7-P8:** Remaining IGameInput stubs (shrine menus, skill tree, confirm dialogs, etc.).
- 2025-07-17: Avalonia P5 — TCS-Based Input Bridge (see decisions.md)

---

## Regression Wave 1 — TCS Race Fix + Dead Code Cleanup

**Date:** 2025-07-18
**Work Items:** WI-R02 (P0), WI-R05 (P2)
**Branch:** `squad/regression-wave1-hill`

### WI-R02: TCS Race Condition Fix

**Problem:** `_pendingLine` in `AvaloniaInputReader` and `_pendingCommand` in `AvaloniaDisplayService` were read-then-nulled in event handlers without synchronization. If the UI thread fires the event handler while the game thread is mid-write to the TCS field, the handler could read a stale/null value — a classic TOCTOU race.

**Fix:** Replaced the non-atomic read-then-null pattern with `Interlocked.Exchange(ref field, null)` in both event handlers. This atomically reads and clears the field in one operation, eliminating the race window. Game-thread writes (`_pendingLine = tcs;`) left as-is — only one game thread writes and it always completes before the UI handler can fire.

**Files changed:**
- `Dungnz.Display.Avalonia/AvaloniaInputReader.cs` — `OnInputSubmitted()` now uses `Interlocked.Exchange`
- `Dungnz.Display.Avalonia/AvaloniaDisplayService.cs` — `ReadCommandInput()` local `OnSubmitted` now uses `Interlocked.Exchange`
- Added `using System.Threading;` to both files

### WI-R05: Dead Code Cleanup

- Deleted `Dungnz.Display.Avalonia/AvaloniaAppBuilder.cs` — P2 scaffold replaced by `Program.cs` + `App.axaml.cs`
- Verified zero code references to `AvaloniaAppBuilder` (only doc/plan references remain)
- Annotated `App.axaml.cs` TODO with phase reference: `TODO(P3-P8)`

### Validation

- ✅ `dotnet build Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj` — 0 errors
- ✅ `dotnet test` — 2,154 passed, 0 failed, 4 skipped
- ✅ Only `Dungnz.Display.Avalonia/` files touched (no README update needed)

---

## Regression Wave 2 (2026-03-21)

### WI-R09: Avalonia Smoke Test Checklist

**Issue:** #1419
**Branch:** `squad/regression-wave2-hill`

Created `docs/avalonia-smoke-test-checklist.md` — a 12-scenario manual smoke test checklist for the Avalonia GUI frontend.

**Checklist covers:**
1. Application launch (window opens, 6-panel layout)
2. Map panel renders ASCII dungeon map
3. Stats panel shows player name, HP bar, level
4. Content panel shows room description
5. Gear panel shows equipment slots
6. Log panel accumulates timestamped messages
7. Input panel accepts typing
8. Command submission via Enter
9. Movement command (directional)
10. Quit command
11. Window close via X button
12. Window resize / reflow

**Audit findings — hardcoded values in `App.axaml.cs`:**
- `Difficulty.Normal` (line 49) — should be player-selectable
- Seed `12345` (line 59) — should be random or player-entered
- Player name `"Adventurer"` (line 62) — should be player-entered
- Player class `PlayerClass.Warrior` (line 63) — should be player-selectable
- All intentional P2 scaffolding; full startup flow planned for P3–P8

**Headless verification attempt:**
- ✅ Build succeeds (0 errors)
- ✅ App launches, logs "Dungnz Avalonia GUI starting..."
- ⚠️ Cannot interactively test — headless session, killed by timeout (exit 124)
- Conclusion: App initializes correctly; interactive testing requires desktop environment

---

## Avalonia P6–P8 — Menu Input Infrastructure + All 23 Stubs (2025-07-18)

**PR:** #1434 | **Branch:** `squad/1428-avalonia-menu-infrastructure`
**Issues:** #1428, #1429, #1430, #1431, #1432, #1433

Replaced all 23 hardcoded input stubs in `AvaloniaDisplayService.cs` with real text-based numbered menu implementations. Updated `App.axaml.cs` to use `StartupOrchestrator` instead of hardcoded startup values.

**Key additions:**
- `WaitForTextInput(prompt)` — Reusable TCS bridge pattern
- `SelectFromMenu<T>()` — Generic numbered menu infrastructure
- 23 method implementations across Startup, Combat, Inventory, Economy, Progression, Special Rooms, and Utility categories
- `App.axaml.cs` startup flow via `StartupOrchestrator.Run()`

**Design decisions:** Text-based menus (not arrow-key), combat letter shortcuts (A/B/F/I), inline stat cards for class selection, dual-mode skill tree, `ShowShopWithSellAndSelect` returns -1 for sell.

✅ Build: 0 errors | Tests: 2,351 passed, 0 failed, 4 skipped

Full session log: `.ai-team/log/2025-07-18-avalonia-p6-p8-menu-infrastructure.md`
- 2025-07-18: Avalonia P6–P8 — Menu Input Infrastructure (see decisions.md)
