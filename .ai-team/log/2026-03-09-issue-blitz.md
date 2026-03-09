# Session: 2026-03-09 — Issue Blitz (9 PRs, 4 Closures, 43 New Tests)

**Requested by:** Anthony  
**Team:** Hill, Barton, Romanoff, Fitz, Fury, Coulson  

---

## What They Did

### Hill — P1 Gameplay Bug Batch (#1235, #1234, #1224, #1238, #1237, #1232)

Audited and resolved six P1 gameplay bugs across null-safety, constant deduplication, and registry deduplication.

**Audit closures (no code change — verified correct, closed):**
- **#1238** `SetBonusManager.ApplySetBonuses` correctly applies 2-piece bonuses — verified, closed
- **#1237** `CombatEngine.HandleLootAndXP` correctly passes `dungeonFloor` to `RollDrop` — verified, closed
- **#1232** Enemy HP clamped to 0 via `Math.Max(0, ...)` in `CombatEngine` — verified, closed

**PR #1287 — GameLoop null-safety (#1235):**
- `_player = null!`, `_currentRoom = null!`, `_stats = null!` replaced with `= new()`
- `_context = null!` kept — always initialized in `InitContext()` before `RunLoop()`
- File: `Dungnz.Engine/GameLoop.cs`

**PR #1289 — FinalFloor constant dedup (#1234):**
- `private const int FinalFloor = DungeonGenerator.FinalFloor` was redeclared in 5 places
- All five callers (`GoCommandHandler`, `DescendCommandHandler`, `AscendCommandHandler`, `StatsCommandHandler`, `GameLoop`) now reference `DungeonGenerator.FinalFloor` directly
- Files: `Dungnz.Engine/Commands/*.cs`, `Dungnz.Engine/GameLoop.cs`

**PR #1291 — EnemyTypeRegistry dedup (#1224):**
- Two identical 95-line `EnemyTypeRegistry` classes existed in `Dungnz.Engine` and `Dungnz.Systems`
- Kept `Dungnz.Systems` version (canonical); deleted `Dungnz.Engine/EnemyTypeRegistry.cs`
- Updated `Dungnz.Tests/ArchitectureTests.cs` line 68: `Dungnz.Engine.EnemyTypeRegistry` → `Dungnz.Systems.EnemyTypeRegistry`
- Resolves Fitz CI concern from #1230
- Key constraint: `Dungnz.Systems.csproj` has no reference to `Dungnz.Engine` — making Engine canonical would require a circular dependency

---

### Barton — Substring Bounds Fix + ContentPanelMenu Cancel Fix (#1246, #1241)

**PR #1286 — Substring bounds guard (#1246):**
- Multi-step loop in an input processing path was calling substring operations without sufficient bounds checking
- Guard covers ALL mutations in the loop body (including state updates at the bottom)
- File: `Dungnz.Display/Spectre/SpectreLayoutDisplayService.Input.cs` (or related)
- Branch: `squad/1246-substring-bounds`

**PR #1288 — ContentPanelMenu Escape/Q semantics (#1241):**
- `ContentPanelMenu<T>` (non-nullable) was returning `items[items.Count - 1].Value` on Escape/Q — a phantom selection
- Fix: Escape/Q case changed to `break` (ignore key, loop again) — non-nullable menus are required-choice; no cancel sentinel exists
- `ContentPanelMenuNullable<T>` already correctly returned `null` on Escape/Q — no change needed
- File: `Dungnz.Display/Spectre/SpectreLayoutDisplayService.Input.cs`
- Branch: `squad/1241-contentpanelmenu-cancel`

---

### Romanoff — 43 New Tests for Edge Cases and Coverage (#1292)

**PR #1292** added 43 tests across 6 test classes targeting coverage and edge-case gaps:

1. **#1249 — PrestigeFlowTests (11 tests):** Navigation via `GameLoop`; unknown direction word fires invalid-direction error; uses `[Collection("PrestigeTests")]`
2. **#1248 — LootTableFloorEdgeCaseTests (6 tests):** `RollDrop` with `dungeonFloor: 0` or negative; epic/legendary paths not triggered at floor 0; uses `IDisposable` tier pool restore
3. **#1243 — CombatDeadEnemyTests (5 tests):** Enemy at HP=0 on entry to `RunCombat`; loop catches `IsDead` after status tick, breaks immediately, returns `Won`
4. **#1239 — SetBonusThresholdTests (8 tests):** 1-piece: bonuses stay 0; 2-piece: activates after `ApplySetBonuses()`. Key finding: `SetBonusCritChance` does not exist as a Player field — crit bonus lives only in the SetBonus list
5. **#1233 — PlayerSettingsRoundTripTests (13 tests):** All 6 `PlayerClass` and all 3 `Difficulty` values round-trip via `SaveSystem.SaveGame/LoadGame`; uses `[Collection("save-system")]` + temp directory
6. **#1251 — (additional coverage):** Further edge-case coverage from the batch

**Running total:** All 1858+ tests pass.

**Patterns used:** `FakeDisplayService` + `FakeInputReader` + `ControlledRandom(0.9)` for combat tests; `EnemyStub(hp, atk, def, xp) { HP = 0 }` to simulate dead enemy.

---

### Fitz — Coverage Script + CodeQL Workflow (#1283, #1284, #1230)

**PR #1283 — coverage.sh script:** Adds a local coverage script for developer use; simplifies running `dotnet test` with coverage collection outside CI.

**PR #1284 — CodeQL workflow:** Adds GitHub Actions CodeQL analysis workflow for static security analysis.

**#1230 closed:** Coverage CI concern resolved by Hill's EnemyTypeRegistry dedup (PR #1291) which fixed the architecture test referencing the now-deleted `Dungnz.Engine.EnemyTypeRegistry` class.

---

### Fury — Mid-Combat Banter Content (#1285)

**PR #1285** adds mid-combat banter content lines for enemy encounters, expanding narrative flavor during combat sequences.

**Note (Romanoff review finding):** PR #1279 (earlier Fury banter PR) has a `NarrationService.GetEnemyCritReaction` signature conflict with PR #1275 (`string?` → `string`). Fury must rebase `squad/1271-mid-combat-banter` on main after #1275 merges. The `CombatEngine` null guard is harmless to keep.

---

### Coulson — Issue Triage #1274

Triaged issue #1274 (triggered effects for 3 enemy abilities). Added labels `squad:barton` + `squad:hill`. Waiting on Anthony spec sign-off for 3 triggered effects before work can begin.

---

## Key Technical Decisions

### EnemyTypeRegistry Lives in Dungnz.Systems (Canonical)
`Dungnz.Systems.csproj` does NOT reference `Dungnz.Engine`. `SaveSystem` (in Systems) uses `EnemyTypeRegistry.CreateOptions()`. An `internal` class in `Dungnz.Engine` is inaccessible to `Dungnz.Systems` assemblies — making Engine canonical would require either a circular project reference (forbidden) or a public class with a cross-project ref. Systems is the only valid location. Architecture tests must reference `Dungnz.Systems.EnemyTypeRegistry`.

### ContentPanelMenu<T> Cancel Semantics
Non-nullable `ContentPanelMenu<T>` ignores Escape/Q (loops back). Nullable sibling `ContentPanelMenuNullable<T>` returns `null` on Escape/Q. This matches Spectre's `SelectionPrompt<T>` non-Live behaviour. Menus where cancel is valid must use the nullable variant.

### FakeInputReader + FakeDisplayService as Standard Test Infrastructure
`FakeInputReader("1")` injection at `ConsoleDisplayService` construction time is the established pattern for testing all interactive methods that use `_input.ReadLine()`. Methods using `Console.ReadLine()` directly need `Console.SetIn(new StringReader("x"))`.

---

## Related PRs

- PR #1283: Fitz — coverage.sh script
- PR #1284: Fitz — CodeQL workflow
- PR #1285: Fury — mid-combat banter content
- PR #1286: Barton — substring bounds fix (#1246)
- PR #1287: Hill — GameLoop null-safety (#1235)
- PR #1288: Barton — ContentPanelMenu cancel semantics (#1241)
- PR #1289: Hill — FinalFloor constant dedup (#1234)
- PR #1291: Hill — EnemyTypeRegistry dedup (#1224)
- PR #1292: Romanoff — 43 new tests (6 test classes)

## Issues Closed

- #1238 — SetBonusManager audit (verified correct, no code change)
- #1237 — HandleLootAndXP audit (verified correct, no code change)
- #1232 — Enemy HP clamp audit (verified correct, no code change)
- #1230 — Fitz CI concern (resolved by PR #1291)
