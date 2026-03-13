# Decision: Avalonia P5 — TCS-Based Input Bridge

**Author:** Hill  
**Date:** 2025-07-17  
**PR:** #1405  
**Status:** Proposed  

## Context

Phase 5 of the Avalonia migration requires bridging the game thread (which blocks
waiting for player input) with the Avalonia UI thread (where the TextBox lives).
The game loop is single-threaded and synchronous — `ReadCommandInput()` and
`ReadLine()` must block until the player submits a command.

## Decision

Use `TaskCompletionSource<string?>` with `TaskCreationOptions.RunContinuationsAsynchronously`
to bridge the two threads:

1. **Game thread** creates a TCS and dispatches "enable input" to the UI thread,
   then blocks on `tcs.Task.GetAwaiter().GetResult()`.
2. **UI thread** enables the TextBox, auto-focuses it, and waits for Enter.
3. **On Enter**, the `InputPanelViewModel.Submit()` method fires `InputSubmitted`,
   which calls `TrySetResult()` on the TCS, unblocking the game thread.

`RunContinuationsAsynchronously` prevents the continuation from running on the
UI thread (which would deadlock since the UI thread is dispatching).

### Why not async/await end-to-end?

The entire game loop (`GameLoop.Run`) and all 23 command handlers are synchronous.
Converting to async would require rewriting the entire engine. The TCS pattern
lets us keep the synchronous game thread while cleanly bridging to the async UI.

### Two consumers, one event

Both `AvaloniaInputReader.ReadLine()` and `AvaloniaDisplayService.ReadCommandInput()`
use the same `InputPanelViewModel.InputSubmitted` event. This is safe because:
- The game thread is single-threaded — only one call blocks at a time.
- `AvaloniaInputReader` subscribes in its constructor (persistent).
- `AvaloniaDisplayService` subscribes/unsubscribes per-call (scoped).

## Alternatives Considered

- **`Channel<string>`** — more complex, no advantage for single-request pattern.
- **`AutoResetEvent`** — requires shared mutable string field, less clean.
- **Full async game loop** — massive rewrite, deferred to post-migration.

## Consequences

- The game can now accept typed commands in the Avalonia app.
- P6 (menus) will extend this pattern for selection prompts and key navigation.
- `IsInteractive` returns `false` for now — game uses numbered text prompts
  instead of arrow-key menus. P6 will flip this to `true`.
