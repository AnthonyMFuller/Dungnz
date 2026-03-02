# 2026-03-02 Session: Startup Menu Implementation

**Requested by:** Anthony (Boss)

## Feature
Startup menu with New Game / Load Save / Enter Seed / Exit options

## GitHub Issues Created
- #835 — Startup menu UI
- #836 — Load save functionality
- #837 — Seed entry
- #838 — Tests

## Team Work Summary

### Architecture (Coulson)
Designed the startup menu system:
- **StartupMenuOption** enum (NewGame, LoadSave, NewGameWithSeed, Exit)
- **StartupResult** discriminated union (NewGame, LoadedGame, ExitGame)
- **StartupOrchestrator** class — coordinates menu flow
- Three new **IDisplayService** methods:
  - `ShowStartupMenu(bool hasSaves)`
  - `SelectSaveToLoad(string[] saveNames)`
  - `int? ReadSeed()`
- **GameLoop.Run(GameState)** overload for loading saves
- **Program.cs** rewire to use orchestrator

### Implementation (Hill)
Implemented display layer and orchestrator per Coulson's design on **PR #840**:
- **New files:**
  - `Engine/StartupMenuOption.cs` — Enum
  - `Engine/StartupResult.cs` — Discriminated union records
  - `Engine/StartupOrchestrator.cs` — Main orchestrator class
- **Modified files:**
  - `Display/IDisplayService.cs` — Added 3 new methods
  - `Display/SpectreDisplayService.cs` — Implemented 3 methods
  - `Display/DisplayService.cs` (ConsoleDisplayService) — Implemented 3 methods
  - `Engine/IntroSequence.cs` — Added optional `showTitle` parameter
- **Design adherence:** 100% per Coulson's design doc
- **Build:** ✅ Passes, PR #840 merged to master

### GameLoop & Program (Barton)
Implemented game loop changes and Program.cs rewire:
- **GameLoop.cs:**
  - Added `Run(GameState)` overload — restores player, room, floor, seed from saved state
  - Extracted `RunLoop()` method — shared command loop for both entry paths
- **Program.cs:**
  - Integrated `StartupOrchestrator` call
  - Pattern-matched on `StartupResult` to branch:
    - `NewGame` → dungeon generation + `Run(Player, Room)`
    - `LoadedGame` → restore and `Run(GameState)`
    - `ExitGame` → exit application
- **PR #839:** Closed as superseded — Hill included identical changes in PR #840
- **Design adherence:** 100% per Coulson's design

### Tests (Romanoff)
Writing unit tests on **PR #838** (in progress):
- `StartupOrchestratorTests.cs` — Menu flow, save load, seed entry, cancel paths
- `TestDisplayService` — Stubs for 3 new methods
- Coverage: orchestrator with `TestDisplayService`, GameLoop loaded-state overload, seed validation edge cases

## Build Status
✅ **Master at commit 10ee689: Build passing**
- PR #840 merged successfully
- All architecture and UI layer complete
- Tests in progress on PR #838

## Decision Documents
- `coulson-startup-menu-architecture.md` — Design spec
- `barton-startup-gameloop.md` — GameLoop implementation
- `hill-startup-menu-implementation.md` — Display layer implementation
