# Display Layer Architecture Review — Issues Filed

**Date:** 2026-03-04  
**Author:** Coulson (Lead)  
**Requested by:** Anthony (Boss)

## Summary

Performed deep architecture review of the Spectre display layer. Identified critical bugs in the `PauseAndRun` mechanism that breaks ALL menu interactions when Live display is active.

## Issues Filed

| Issue # | Priority | Title |
|---------|----------|-------|
| #1107 | **P0** | All menus crash with InvalidOperationException — PauseAndRun + SelectionPrompt conflict with Live exclusivity mode |
| #1108 | P1 | Content panel not refreshed after menu returns |
| #1109 | P2 | Race condition between pause/resume events in Live loop |
| #1110 | P2 | _pauseDepth nesting logic doesn't fully solve nested menu deadlock |

## Root Cause Analysis

The core issue is that Spectre.Console's `AnsiConsole.Live().Start()` acquires `DefaultExclusivityMode._running = 1` for the **entire duration** of the callback, not just during refresh operations. This means:

1. Live display runs in a callback that holds exclusivity lock indefinitely
2. `PauseAndRun` pauses the Live loop (makes it block on `_resumeLiveEvent.Wait()`)
3. But the exclusivity lock is still held by the outer `Start()` callback
4. `SelectionPrompt` tries to acquire exclusivity → throws `InvalidOperationException`

**Approximately 20 menu methods are affected**, making the game essentially unplayable when Live display is active.

## Recommended Fix Strategy

Replace `SelectionPrompt` with a custom ReadKey-based menu rendered in the content panel:

1. Render menu options as a list in the Content panel
2. Use `▶` or highlight to show selected item
3. Arrow keys (Up/Down) navigate the selection
4. Enter confirms, Escape cancels
5. Use `AnsiConsole.Console.Input.ReadKey(intercept: true)` which does NOT require exclusivity

This pattern is already proven in `ReadCommandInput()` (lines 430-466 of SpectreLayoutDisplayService.Input.cs).

## Files Reviewed

- `Display/Spectre/SpectreLayoutDisplayService.cs` — Live loop, panel rendering
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs` — All menu methods, PauseAndRun mechanism
- `Display/Spectre/SpectreLayoutContext.cs` — Thread-safe context wrapper
- `Display/Spectre/SpectreLayout.cs` — 6-panel layout structure
- `Engine/GameLoop.cs` — Command handlers that invoke menus

## Work Assignment Recommendation

**Assignee:** Hill (C# Dev)  
**Estimated effort:** 4-6 hours for P0 fix, +1 hour for P1 content restoration

The fix requires creating a generic `ShowMenuAndSelect<T>()` method that:
1. Accepts a list of `(string label, T value)` options
2. Renders them in the content panel with selection indicator
3. Handles keyboard navigation via ReadKey
4. Returns selected value or default on cancel
5. Restores previous content state after completion

All 20+ existing menu methods can then delegate to this single implementation.
