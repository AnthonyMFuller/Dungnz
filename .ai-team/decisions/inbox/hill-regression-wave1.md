# Decision: Regression Wave 1 — TCS Race Fix + Dead Code Cleanup

**Date:** 2025-07-18  
**Author:** Hill  
**Status:** Implemented  
**Work Items:** WI-R02 (P0), WI-R05 (P2)  
**Branch:** `squad/regression-wave1-hill`

## Context

During the Avalonia regression audit (led by Romanoff, plan by Coulson), two issues were identified in the Avalonia display layer:

1. **WI-R02 (P0):** `AvaloniaInputReader.OnInputSubmitted()` and `AvaloniaDisplayService.ReadCommandInput()` use `TaskCompletionSource<string?>` to bridge UI thread → game thread input. The event handlers read and null the TCS field in two separate statements — a TOCTOU race condition. If the UI thread fires between the game thread's field write and the event handler's field read, the handler may read stale data or null.

2. **WI-R05 (P2):** `AvaloniaAppBuilder.cs` is dead code left over from the P2 scaffold phase. It was replaced by `Program.cs` + `App.axaml.cs` but never deleted. Additionally, `App.axaml.cs` had TODO comments missing phase reference annotations.

## Decision

### WI-R02: Use `Interlocked.Exchange` for Atomic TCS Field Access

Replace the non-atomic read-then-null pattern:
```csharp
// BEFORE (race-prone)
var pending = _pendingLine;
_pendingLine = null;
pending?.TrySetResult(text);

// AFTER (atomic)
var pending = Interlocked.Exchange(ref _pendingLine, null);
pending?.TrySetResult(text);
```

**Why `Interlocked.Exchange` and not `lock`?**  
- Lock-free is sufficient here: only one game thread writes the field, only one UI handler reads it
- `Interlocked.Exchange` is a single atomic operation that reads + clears in one step
- No contention overhead, no deadlock risk with Avalonia's Dispatcher

**Why leave game-thread writes as-is?**  
- `_pendingLine = tcs;` on the game thread is a simple field write
- The game thread always writes before the UI handler can fire (the TCS is created, assigned, then the Dispatcher enables input — the user can't submit before input is enabled)
- Making this also use `Interlocked` would add complexity without benefit

### WI-R05: Delete Dead Scaffold, Annotate TODOs

- Delete `AvaloniaAppBuilder.cs` — zero code references, confirmed by grep
- Annotate remaining TODO in `App.axaml.cs` with `(P3-P8)` phase reference so it's clearly intentional

## Consequences

- **Thread safety:** Both TCS bridge points are now race-free
- **Maintenance:** Dead code removed, reducing confusion for new contributors
- **Test impact:** None — changes are in the Avalonia project which has no unit test coverage yet (it's a separate executable). All 2,154 existing tests pass unchanged.
- **Future work:** P6 key navigation and P3-P8 startup flow are unaffected by these changes
