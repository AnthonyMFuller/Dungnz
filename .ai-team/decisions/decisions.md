# Architectural Decisions Log

> Single source of truth for all project-level technical decisions.  
> Maintained by Scribe. Newest decisions at top.

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
