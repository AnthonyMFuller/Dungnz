# Decision: ReadCommandInput() Added to IDisplayService

**Date:** 2026-03-06  
**Agent:** Hill  
**Context:** Fixing bugs #1095 and #1096 in TUI Command panel  
**Status:** Implemented in PR #1097

## Decision

Added `ReadCommandInput()` method to the `IDisplayService` interface to handle command-line input during normal gameplay exploration. This method is responsible for:

1. Pausing any active live render loops (SpectreLayoutDisplayService)
2. Reading a single line of input from the player
3. Resuming the render loop after input is collected
4. Returning the input string (or null)

## Rationale

### The Problem
`GameLoop.RunLoop()` was calling `_input.ReadLine()` directly (which maps to `Console.ReadLine()`), bypassing the display abstraction. This created a race condition with `SpectreLayoutDisplayService`'s Live render loop that continuously redraws the terminal in a background thread. The result: players couldn't type commands in the TUI.

### Why This Solution
All other interactive input (menus, prompts) already used the `PauseAndRun` pattern in `SpectreLayoutDisplayService` to correctly synchronize with the Live loop. Adding `ReadCommandInput()` to the interface extends this pattern to command-line input and fixes the abstraction leak.

### Alternative Considered
- **Keep using `_input.ReadLine()`**: Would require SpectreLayoutDisplayService to expose its own IInputReader that wraps PauseAndRun, creating API fragmentation
- **Remove Live loop**: Would lose the benefits of the live-updating TUI dashboard
- **Make PauseAndRun public**: Would expose internal synchronization mechanism and require GameLoop to know about display implementation details

## Impact

- **GameLoop**: Changed one line (`_input.ReadLine()` → `_display.ReadCommandInput()`)
- **IDisplayService**: New method added to interface contract
- **All implementations**: ConsoleDisplayService and SpectreDisplayService added simple stubs that call Console.ReadLine() (no Live loop to synchronize)
- **SpectreLayoutDisplayService**: New method wraps Console.ReadLine() in PauseAndRun for thread safety
- **Tests**: `_input` field still exists in GameLoop for test mocks that use IInputReader

## Follow-Up
None required. This is a targeted fix that resolves the immediate bug without disrupting existing test infrastructure or other display implementations.
