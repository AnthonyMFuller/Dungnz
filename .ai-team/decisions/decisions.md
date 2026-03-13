# Architectural Decisions Log

> Single source of truth for all project-level technical decisions.  
> Maintained by Scribe. Newest decisions at top.

---

# Avalonia P5 — TCS-Based Input Bridge

**Date:** 2025-07-17  
**Architect/Author:** Hill  
**Issues:** —  
**PRs:** #1405  

---

## Context

Phase 5 of the Avalonia migration requires bridging the game thread (which blocks waiting for player input) with the Avalonia UI thread (where the TextBox lives). The game loop is single-threaded and synchronous — `ReadCommandInput()` and `ReadLine()` must block until the player submits a command.

## Decision

Use `TaskCompletionSource<string?>` with `TaskCreationOptions.RunContinuationsAsynchronously` to bridge the two threads:

1. **Game thread** creates a TCS and dispatches "enable input" to the UI thread, then blocks on `tcs.Task.GetAwaiter().GetResult()`.
2. **UI thread** enables the TextBox, auto-focuses it, and waits for Enter.
3. **On Enter**, the `InputPanelViewModel.Submit()` method fires `InputSubmitted`, which calls `TrySetResult()` on the TCS, unblocking the game thread.

`RunContinuationsAsynchronously` prevents the continuation from running on the UI thread (which would deadlock since the UI thread is dispatching).

### Why not async/await end-to-end?

The entire game loop (`GameLoop.Run`) and all 23 command handlers are synchronous. Converting to async would require rewriting the entire engine. The TCS pattern lets us keep the synchronous game thread while cleanly bridging to the async UI.

### Two consumers, one event

Both `AvaloniaInputReader.ReadLine()` and `AvaloniaDisplayService.ReadCommandInput()` use the same `InputPanelViewModel.InputSubmitted` event. This is safe because:
- The game thread is single-threaded — only one call blocks at a time.
- `AvaloniaInputReader` subscribes in its constructor (persistent).
- `AvaloniaDisplayService` subscribes/unsubscribes per-call (scoped).

## Rationale

- TCS with `RunContinuationsAsynchronously` is the minimal-overhead pattern for single-request async-to-sync bridging
- Avoids `Channel<string>` complexity for what is always a one-shot request/response
- Keeps the synchronous game loop intact — no engine-wide async rewrite required
- Single `InputSubmitted` event safely serves both consumers because the game thread is single-threaded

## Alternatives Considered

- **`Channel<string>`** — more complex, no advantage for single-request pattern.
- **`AutoResetEvent`** — requires shared mutable string field, less clean.
- **Full async game loop** — massive rewrite, deferred to post-migration.

## Related Files

- `Dungnz.Display.Avalonia/AvaloniaDisplayService.cs` — `ReadCommandInput()` TCS bridge
- `Dungnz.Display.Avalonia/AvaloniaInputReader.cs` — `ReadLine()` TCS bridge
- `Dungnz.Display.Avalonia/ViewModels/InputPanelViewModel.cs` — `InputSubmitted` event + `Submit()`
- `Dungnz.Display.Avalonia/Views/Panels/InputPanel.axaml` — TextBox + Enter binding
- `Dungnz.Display.Avalonia/Views/Panels/InputPanel.axaml.cs` — code-behind focus handling

---

# Avalonia P3 Output Panel Architecture

**Date:** 2025-01-12  
**Architect/Author:** Hill  
**Issues:** —  
**PRs:** #1403  

---

## Context

Avalonia GUI migration Phase 3 required implementing all 31 `IGameDisplay` output methods in `AvaloniaDisplayService`. The core challenge was safely marshalling updates from the game engine's background thread to the Avalonia UI thread without introducing latency, deadlocks, or unnecessary complexity. A secondary decision was whether to implement rich styled output immediately or defer to a later phase.

## Decision

All `IGameDisplay` output methods in `AvaloniaDisplayService` use fire-and-forget `Dispatcher.UIThread.InvokeAsync()` to marshal from the game thread (background) to the UI thread (main). No blocking, no synchronous cross-thread calls. P3 outputs plain text only; color/style/rich UI deferred to P9+ polish phase. Cached state mirrors `SpectreLayoutDisplayService` for combat panel-switching behavior.

## Rationale

- The game engine never needs return values from output methods (they're void), so blocking is unnecessary
- Fire-and-forget is simpler and more performant than Spectre's PauseAndRun pattern
- Avalonia's dispatcher queue automatically serializes UI updates
- Plain text unblocks P4–P8 implementation (input methods, menus) and allows all 31 output methods to be validated independently of styling
- Mirroring Spectre's cached state enables the same combat panel-switching behavior (Gear panel shows enemy in combat, player gear in exploration)

## Alternatives Considered

**Blocking with `Dispatcher.UIThread.Invoke()` (synchronous):**
- Would block game thread until UI updates complete
- Introduces latency and potential deadlock risk
- No benefit since game engine doesn't need return values
- Rejected: fire-and-forget is simpler and safer

## Related Files

- `Dungnz.Display.Avalonia/AvaloniaDisplayService.cs` — All 31 output methods
- `Dungnz.Display.Avalonia/ViewModels/*.cs` — Panel update methods
- `Dungnz.Display.Avalonia/App.axaml.cs` — Wire MainWindowViewModel to AvaloniaDisplayService
