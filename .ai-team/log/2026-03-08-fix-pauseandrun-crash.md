# Session Log: P0 PauseAndRun Crash Fix

**Date:** 2026-03-08  
**Issue:** #1265  
**PR:** #1266  
**Author:** Barton  
**Status:** Merged to master

## Summary

Fixed critical P0 crash in SpectreLayoutDisplayService related to broken `PauseAndRun` method.

## Root Cause

The Spectre.Console library's `DefaultExclusivityMode` holds an exclusivity lock for the entire duration of `Live.Start()`. The `PauseAndRun` method attempted to pause the live display, release the lock, execute user input via `AnsiConsole.Prompt`, then resume. This design was fundamentally incompatible with the Spectre.Console locking model and caused deadlocks/crashes.

## Solution

1. **Removed broken PauseAndRun method** — method was unreliable by design
2. **Removed pause/resume event handlers** — no longer needed without PauseAndRun
3. **Simplified Live loop** — removed unnecessary pause/resume logic
4. **Refactored ShowSkillTreeMenu** — now calls `AnsiConsole.Prompt` directly when not running inside a live display, bypassing the lock issue entirely

## Changes

- `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs` — removed `PauseAndRun`, event handlers, simplified Live loop initialization
- `Dungnz.Systems/PlayerSkillTree.cs` — updated `ShowSkillTreeMenu` to call prompt directly instead of via PauseAndRun

## Testing

- All existing tests pass
- Skill tree menu displays and accepts user input without crashes
- No regression in live display functionality

## Impact

- Resolves P0 crash
- Simplifies display service architecture
- Makes skill tree input handling more predictable
