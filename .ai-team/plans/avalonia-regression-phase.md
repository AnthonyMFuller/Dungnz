# Avalonia Regression Testing & Bug Fixing Phase

**Author:** Coulson (Lead Architect)
**Date:** 2026-03-14
**Status:** Draft — Pending Boss Approval
**Scope:** Stabilize P0–P5 Avalonia work; raise coverage above 80% gate; fix bugs introduced by migration

---

## 1. Executive Summary

The Avalonia migration has completed phases P0–P5 and P9 (console compat verification). Before proceeding to P6 (menu input — the highest-risk phase), we need a stabilization checkpoint:

- **1,555 lines** of new Avalonia code with **zero** test coverage
- **Race condition** in the TCS input bridge (AvaloniaInputReader + AvaloniaDisplayService)
- **MapRenderer** extracted to Models with no dedicated tests
- **Dungnz.Display** at 74.62% and **Dungnz.Models** at 78.79% — both below the 80% CI gate
- Unused `AvaloniaAppBuilder.cs` file lingering from P2 scaffold

This phase produces no new features. It exists to ensure the console game is unbroken, the Avalonia code completed so far is correct, and we have a safety net before the high-risk P6 work begins.

---

## 2. Regression Risk Assessment

### 2.1 IDisplayService Split (P0) — Risk: LOW ✅

**What changed:** `IDisplayService` was split into `IGameDisplay` (47 output methods) and `IGameInput` (31 input methods). `IDisplayService` now extends both.

**Why risk is low:**
- All existing consumers still depend on `IDisplayService` — no call site changes needed
- All 6 implementations (3 production, 2 test doubles, 1 Avalonia) implement `IDisplayService` directly
- The split was additive (new sub-interfaces), not destructive (no method moves between types)

**Residual risk:**
- Any new code that takes `IGameDisplay` or `IGameInput` specifically (instead of `IDisplayService`) could be missing methods from the other half
- Test doubles implement `IDisplayService`, so they automatically satisfy both sub-interfaces — but only if the interface definitions stay in sync

**Verification needed:** Interface conformance test (WI-R01)

### 2.2 MapRenderer Extraction (P1) — Risk: MEDIUM ⚠️

**What changed:** `MapRenderer` was extracted as a static class in `Dungnz.Models/MapRenderer.cs` with two variants:
- `BuildMarkupMap()` — Spectre markup (used by SpectreLayoutDisplayService)
- `BuildPlainTextMap()` — plain text (used by Avalonia MapPanelViewModel)

**Risk factors:**
- **Zero dedicated tests** — coverage is only incidental through display service smoke tests
- BFS grid algorithm has edge cases: isolated rooms, cycles, dead ends, single-room dungeons
- Box-drawing characters (─, │, ✦) could break on certain terminals
- Markup escaping in `BuildMarkupMap()` could inject Spectre syntax errors

**Verification needed:** Comprehensive MapRenderer unit tests (WI-R03)

### 2.3 Avalonia Project Structure (P2–P3) — Risk: LOW

**What changed:** `Dungnz.Display.Avalonia` project added as a separate executable with:
- 6 ViewModels (Map, Stats, Content, Gear, Log, Input panels)
- `AvaloniaDisplayService` implementing all 47 IGameDisplay methods
- All 31 IGameInput methods present as stubs (hardcoded defaults)
- `App.axaml.cs` bootstrap with game loop on background thread

**Risk factors:**
- Unused `AvaloniaAppBuilder.cs` still in project (dead code)
- `App.axaml.cs` hardcodes seed 12345 and Warrior class (expected — stub for P2, but should not leak to production)
- Avalonia project references `Dungnz.Display` (for ConsoleInputReader TEMP usage) — this cross-reference should be temporary

**Verification needed:** Build validation, dead code cleanup (WI-R05, WI-R08)

### 2.4 Input Bridge — TCS Pattern (P5) — Risk: HIGH 🔴

**What changed:** `AvaloniaInputReader` and `AvaloniaDisplayService.ReadCommandInput()` use `TaskCompletionSource<string?>` to bridge between the game thread (blocking) and UI thread (event-driven).

**Race condition identified:**

```csharp
// AvaloniaInputReader.cs:66-71
private void OnInputSubmitted(string text)
{
    var pending = _pendingLine;     // UI thread reads field
    _pendingLine = null;            // UI thread clears field
    pending?.TrySetResult(text);    // Game thread may be writing _pendingLine concurrently
}
```

Both `_pendingLine` (in AvaloniaInputReader) and `_pendingCommand` (in AvaloniaDisplayService) are accessed from two threads without synchronization. The window is small but real:

1. Game thread calls `ReadLine()`, sets `_pendingLine = tcs`
2. UI thread fires `OnInputSubmitted`, reads `_pendingLine`, sets to null
3. If game thread calls `ReadLine()` again before step 2 completes, new TCS overwrites old one — **lost input**

**Fix:** Use `Interlocked.Exchange(ref _pendingLine, null)` in the event handler.

**Same pattern exists in:**
- `AvaloniaDisplayService.ReadCommandInput()` lines 765-771 (`_pendingCommand` field)

**Verification needed:** Thread safety fix + concurrent input tests (WI-R02)

### 2.5 Console Regression — Risk: LOW ✅

**Evidence:** All 2,154 tests pass. P9 (ConsoleDisplayService compatibility verification) is complete. SpectreLayoutDisplayService was not modified during Avalonia work.

**Residual risk:**
- If any shared code (Models, Engine, Systems) was touched for Avalonia compatibility, side effects could exist
- MapRenderer extraction changed where the class lives — any missed import would cause compile error (would have been caught by CI)

**Verification needed:** Full console-mode smoke test, end-to-end game simulation (WI-R06)

---

## 3. Work Items

### Priority Key
- **P0** — Blocks correctness (fix before any other work)
- **P1** — Required for coverage gate / CI health
- **P2** — Improves confidence but not blocking

### Size Key
- **S** — < 2 hours
- **M** — 2–4 hours
- **L** — 4–8 hours

---

### WI-R01: Interface Conformance Tests
**Priority:** P1 | **Size:** S | **Agent:** Romanoff

Validate that `IGameDisplay` and `IGameInput` compose correctly into `IDisplayService` and that all implementations satisfy the full surface area.

**Tests to write:**
1. Reflection test: every method in `IGameDisplay` + `IGameInput` exists on `IDisplayService`
2. Reflection test: `IDisplayService` has no methods not in `IGameDisplay` or `IGameInput`
3. Compile-time conformance: verify `FakeDisplayService` and `TestDisplayService` implement all 78 methods (47 + 31) — this is already true but should be an explicit assertion
4. Architecture test: no Engine/Systems code should depend on `IGameDisplay` or `IGameInput` directly (only `IDisplayService`) — enforces the split doesn't fragment consumers

**Acceptance criteria:**
- [ ] 4+ new tests in `Dungnz.Tests/Architecture/`
- [ ] All pass on current codebase
- [ ] Tests would catch: missing method on sub-interface, consumer depending on wrong interface type

**File:** `Dungnz.Tests/Architecture/InterfaceSplitTests.cs`

---

### WI-R02: Fix TCS Race Condition in Input Bridge
**Priority:** P0 | **Size:** S | **Agent:** Hill

Fix unsynchronized access to `_pendingLine` and `_pendingCommand` fields.

**Changes required:**
1. `AvaloniaInputReader.cs` — change `OnInputSubmitted`:
   ```csharp
   private void OnInputSubmitted(string text)
   {
       var pending = Interlocked.Exchange(ref _pendingLine, null);
       pending?.TrySetResult(text);
   }
   ```
2. `AvaloniaDisplayService.cs` — change `ReadCommandInput` event handler:
   ```csharp
   void OnSubmitted(string text)
   {
       var pending = Interlocked.Exchange(ref _pendingCommand, null);
       _vm.Input.InputSubmitted -= OnSubmitted;
       pending?.TrySetResult(text);
   }
   ```
3. Add `using System.Threading;` if not already present

**Acceptance criteria:**
- [ ] Both files use `Interlocked.Exchange` for TCS field access
- [ ] No direct assignment to `_pendingLine` or `_pendingCommand` in event handlers
- [ ] Build succeeds (Avalonia project compiles)
- [ ] Existing 2,154 tests still pass

---

### WI-R03: MapRenderer Unit Tests
**Priority:** P1 | **Size:** M | **Agent:** Romanoff

`MapRenderer` was extracted in P1 with zero tests. Both `BuildMarkupMap()` and `BuildPlainTextMap()` need coverage.

**Tests to write:**

*Grid generation:*
1. Single room — produces valid grid with one cell
2. Linear corridor (N→S chain) — rooms appear in correct order
3. Branching dungeon (T-intersection) — all branches rendered
4. Cycle detection — rooms visited twice don't duplicate in grid
5. Large dungeon (20+ rooms) — no index-out-of-bounds

*Connector rendering:*
6. East exit — horizontal connector (`─`) between adjacent cells
7. South exit — vertical connector (`│`) between cells
8. Room with no exits — isolated cell, no connectors

*Current room indicator:*
9. Current room marked with `✦` or equivalent
10. Non-current rooms use standard marker

*Plain text vs markup:*
11. `BuildPlainTextMap()` output contains no Spectre markup tags (`[`, `]`)
12. `BuildMarkupMap()` output contains valid Spectre color markup
13. Both methods produce same spatial layout for same dungeon

*Edge cases:*
14. Null room — throws or returns empty gracefully
15. Room with exits to ungenerated rooms (null neighbor) — no crash

**Acceptance criteria:**
- [ ] 15+ tests in `Dungnz.Tests/MapRendererTests.cs`
- [ ] All pass
- [ ] Dungnz.Models coverage increases toward 80%

---

### WI-R04: ViewModel Unit Tests (Headless)
**Priority:** P1 | **Size:** M | **Agent:** Romanoff

The 6 Avalonia ViewModels (461 lines total) contain testable logic that does NOT require Avalonia UI. CommunityToolkit.Mvvm's `ObservableObject` works without a running application.

**Test approach:** Reference `Dungnz.Display.Avalonia` from `Dungnz.Tests`. ViewModels use `[ObservableProperty]` which generates standard `INotifyPropertyChanged` — testable with plain C#. Do NOT instantiate Avalonia `Application` or `Dispatcher`.

**Tests to write:**

*ContentPanelViewModel:*
1. `AppendMessage` adds to `ContentLines`
2. `AppendMessage` trims at `MaxContentLines` (50)
3. `SetContent` replaces all lines
4. `Clear` empties collection

*StatsPanelViewModel:*
5. `Update` produces text containing HP bar, player name, level
6. `UpdateCombat` includes combat-specific formatting
7. HP bar renders correct proportions (100/100 = full, 50/100 = half)

*GearPanelViewModel:*
8. `Update` lists equipped items
9. `ShowEnemyStats` displays enemy name, HP, attack, defense
10. Player with no equipment — shows "Empty" slots

*LogPanelViewModel:*
11. `AppendLog` adds timestamped entry
12. Log trims at history buffer limit (50)
13. Type-based icons: "combat" → ⚔️, "error" → ❌, "loot" → 💎

*MapPanelViewModel:*
14. `Update` calls `MapRenderer.BuildPlainTextMap` and stores result
15. `CurrentFloor` property updates

*InputPanelViewModel:*
16. `Submit` fires `InputSubmitted` event with trimmed text
17. `Submit` clears `CommandText` and disables input
18. Empty submit fires event with empty string

**Dependency note:** The test project will need a `<ProjectReference>` to `Dungnz.Display.Avalonia`. However, if Avalonia NuGet packages cause build issues in the test project (AXAML source generator problem), create a separate `Dungnz.Display.Avalonia.Tests` project instead. Coulson to assess at implementation time.

**Acceptance criteria:**
- [ ] 18+ ViewModel tests
- [ ] All pass without Avalonia Application/Dispatcher running
- [ ] If separate test project needed, it's added to `.slnx` and CI

---

### WI-R05: Dead Code Cleanup
**Priority:** P2 | **Size:** S | **Agent:** Hill

Remove or repurpose files that are no longer needed after the P2 architecture change.

**Items:**
1. Delete `Dungnz.Display.Avalonia/AvaloniaAppBuilder.cs` — replaced by `Program.cs` + `App.axaml.cs`; only contains a `Run()` stub
2. Verify no other files reference `AvaloniaAppBuilder` — should be zero references
3. Audit `App.axaml.cs` TODOs — mark them as intentional stubs (not forgotten code) with phase references

**Acceptance criteria:**
- [ ] `AvaloniaAppBuilder.cs` deleted
- [ ] `dotnet build` succeeds
- [ ] All 2,154+ tests pass

---

### WI-R06: Console-Mode End-to-End Regression Suite
**Priority:** P1 | **Size:** L | **Agent:** Romanoff

Create a dedicated regression test that exercises the full console game loop using `FakeDisplayService` + `FakeInputReader` to verify no Avalonia work broke the console path.

**Scenarios to cover:**

*Core gameplay loop:*
1. New game → room entry → navigation (N/S/E/W) → arrive at new room
2. Combat encounter → attack → enemy dies → loot drops → pick up item
3. Inventory management → equip weapon → stats change → unequip
4. Shop interaction → buy item → gold decreases → sell item → gold increases
5. Level up → choose stat → stat increases
6. Game over → death screen displayed → combat stats shown
7. Victory → floor cleared → victory screen displayed

*Display method coverage:*
8. All 37 IGameDisplay void methods called at least once during a full game simulation
9. All output captured in `FakeDisplayService.AllOutput` — no `null` entries, no empty strings where content expected
10. `ShowMap` called with valid room — output is non-empty

*Startup flow:*
11. `ShowStartupMenu` → New Game → `SelectDifficulty` → `SelectClass` → game begins
12. `ShowStartupMenu` → Load Game → `SelectSaveToLoad` → (no saves → back to menu)

**Implementation pattern:**
```csharp
[Collection("regression")]
public class ConsoleRegressionTests
{
    [Fact]
    public void FullGameLoop_ConsoleMode_NoExceptions()
    {
        var display = new FakeDisplayService();
        var input = new FakeInputReader("n", "n", "A", "A", "A", "quit");
        // ... wire up GameLoop, CombatEngine, etc.
        // Assert no exceptions, display.AllOutput has expected entries
    }
}
```

**Acceptance criteria:**
- [ ] 12+ regression scenarios
- [ ] All pass
- [ ] Covers startup → combat → inventory → shop → level-up → end-game
- [ ] Documents what each test proves (regression-specific, not general feature test)

---

### WI-R07: Raise Dungnz.Display and Dungnz.Models Above 80%
**Priority:** P1 | **Size:** L | **Agent:** Romanoff

Current coverage per module:

| Module | Line | Target | Delta |
|--------|------|--------|-------|
| Dungnz.Display | 74.62% | 80% | +5.38% |
| Dungnz.Models | 78.79% | 80% | +1.21% |
| Dungnz.Engine | 71.34% | 80% | +8.66% |

**Strategy for Dungnz.Display (+5.38%):**
- Add tests for uncovered `ConsoleDisplayService` methods (smoke tests cover only 5 of 37)
- Add snapshot tests for remaining `BuildXxxPanelMarkup` methods in SpectreLayoutDisplayService
- Focus on `ShowEquipmentComparison`, `ShowShop`, `ShowCraftRecipe`, `ShowVictory`, `ShowGameOver` — these are complex formatting methods likely uncovered

**Strategy for Dungnz.Models (+1.21%):**
- MapRenderer tests (WI-R03) will contribute significantly
- Add tests for any uncovered model methods: `Player.TakeDamage` edge cases, `Enemy` subclass behaviors, `Room` navigation

**Strategy for Dungnz.Engine (+8.66%):**
- This is the hardest — CombatEngine is 1,709 lines and is the god class
- Focus on uncovered command handlers and edge-case combat paths
- Do NOT attempt CombatEngine decomposition in this phase (that's a separate tech debt item)

**Acceptance criteria:**
- [ ] Dungnz.Display ≥ 80% line coverage
- [ ] Dungnz.Models ≥ 80% line coverage
- [ ] Dungnz.Engine improved (target: ≥ 75%, stretch: 80%)
- [ ] Total project coverage ≥ 83% (current 83.34% — don't regress)

---

### WI-R08: CI Build Validation for Avalonia
**Priority:** P2 | **Size:** S | **Agent:** Fitz

Ensure the Avalonia project builds cleanly in CI and doesn't break the coverage gate.

**Changes:**
1. Verify `dotnet build` in CI builds the Avalonia project (it's in `.slnx` — should already work)
2. Add a build-only step that explicitly builds `Dungnz.Display.Avalonia` to catch compilation errors early:
   ```yaml
   - name: Build Avalonia project
     run: dotnet build Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj --no-restore
   ```
3. Confirm Avalonia NuGet packages restore on Ubuntu CI runner (no Windows-only deps)
4. Coverlet currently doesn't measure Avalonia code (test project doesn't reference it) — this is correct for now (Avalonia code is tested separately via WI-R04)

**Acceptance criteria:**
- [ ] CI builds both executables without errors
- [ ] Avalonia build failure breaks CI (not silently ignored)
- [ ] Coverage gate still passes

---

### WI-R09: Avalonia Smoke Test Checklist (Manual)
**Priority:** P2 | **Size:** S | **Agent:** Hill

Some Avalonia functionality cannot be tested in headless CI. Create a manual test checklist and verify it.

**Manual test scenarios:**

| # | Scenario | Expected Result | Pass? |
|---|----------|----------------|-------|
| 1 | `dotnet run --project Dungnz.Display.Avalonia` | Window opens with 6 panels | |
| 2 | Map panel shows ASCII dungeon map | Room grid with connectors visible | |
| 3 | Stats panel shows player name, HP bar, level | "Adventurer" with HP 100/100 | |
| 4 | Content panel shows room description | Non-empty text about starting room | |
| 5 | Gear panel shows equipment slots | Weapon/armor slot display | |
| 6 | Log panel accumulates messages | At least 2-3 entries after startup | |
| 7 | Input panel accepts typing | Text appears in input box | |
| 8 | Press Enter in input panel | Command submitted, game responds | |
| 9 | Type "n" + Enter | Player moves north (if exit exists) | |
| 10 | Type "quit" + Enter | Window closes cleanly | |
| 11 | Close window via X button | Process exits cleanly (no hang) | |
| 12 | Window resize | Panels reflow, no crash | |

**Acceptance criteria:**
- [ ] Checklist saved as `docs/avalonia-smoke-test-checklist.md`
- [ ] All 12 scenarios verified manually
- [ ] Any failures filed as GitHub issues with `avalonia` label

---

## 4. Dependency Graph

```
WI-R02 (TCS fix)          ─── no deps, do first
WI-R05 (dead code)         ─── no deps, do first
    │
    ▼
WI-R01 (interface tests)   ─── no deps (can parallel with R02/R05)
WI-R03 (MapRenderer tests) ─── no deps (can parallel with R02/R05)
    │
    ▼
WI-R04 (ViewModel tests)   ─── depends on R02 (TCS fix must be in before testing input VM)
WI-R06 (console regression) ─── depends on R02 (fix must be in so console path is baseline)
    │
    ▼
WI-R07 (coverage push)     ─── depends on R01, R03, R06 (those tests count toward coverage)
    │
    ▼
WI-R08 (CI validation)     ─── depends on R04 (if separate test project, CI needs update)
WI-R09 (manual smoke)      ─── depends on R02, R05 (fixes applied before manual test)
```

**Recommended execution order:**

| Wave | Items | Agents | Duration |
|------|-------|--------|----------|
| 1 | WI-R02, WI-R05 | Hill | 1–2h |
| 1 | WI-R01, WI-R03 | Romanoff | 3–4h |
| 2 | WI-R04, WI-R06 | Romanoff | 6–8h |
| 2 | WI-R09 | Hill | 1h |
| 3 | WI-R07 | Romanoff | 4–6h |
| 3 | WI-R08 | Fitz | 1–2h |

**Total estimated effort:** 16–23 hours
**Critical path:** R02 → R04 → R07 (Hill fixes, Romanoff tests, Romanoff coverage)

---

## 5. Coverage Gap Analysis

### 5.1 What the Avalonia Work Added (Untested)

| File | Lines | Tests | Gap |
|------|-------|-------|-----|
| `AvaloniaDisplayService.cs` | 910 | 0 | Full — but covered by WI-R04 (ViewModels test the logic; service is thin delegation) |
| `AvaloniaInputReader.cs` | 72 | 0 | Full — WI-R04 covers InputPanelViewModel; TCS fix in WI-R02 |
| `ViewModels/*.cs` (6 files) | 461 | 0 | Full — WI-R04 |
| `MapRenderer.cs` | 357 | 0 | Full — WI-R03 |
| `IGameDisplay.cs` | ~300 | 0 | Interface only — WI-R01 conformance test |
| `IGameInput.cs` | ~180 | 0 | Interface only — WI-R01 conformance test |
| `App.axaml.cs` | 83 | 0 | Bootstrap — excluded from coverage (Program.cs pattern) |
| `Program.cs` | 29 | 0 | Entry point — excluded from coverage |
| **Total untested** | **~2,392** | **0** | |

### 5.2 What Changed in Existing Code

The P0 split was additive — `IDisplayService` now extends two sub-interfaces but the implementations didn't change. No existing production code was modified for P1–P5 (MapRenderer was a new extraction, not a refactor of existing code).

**This means:** Existing test coverage was not invalidated. The risk is from **new** code, not **changed** code.

### 5.3 Module Coverage Targets

| Module | Current | Target | Strategy |
|--------|---------|--------|----------|
| Dungnz.Data | 100% | 100% | Maintain |
| Dungnz.Display | 74.62% | 80%+ | WI-R07: smoke tests for uncovered display methods |
| Dungnz.Engine | 71.34% | 75%+ | WI-R07: command handler + combat edge cases |
| Dungnz.Models | 78.79% | 80%+ | WI-R03 (MapRenderer) + model edge cases |
| Dungnz.Systems | 93.88% | 93%+ | Maintain |
| **Total** | **83.34%** | **84%+** | Combined effect of all WIs |

Note: `Dungnz.Display.Avalonia` is NOT included in Coverlet coverage because the test project doesn't reference it. ViewModel tests (WI-R04) will either add this reference or use a separate test project. Either way, Avalonia code coverage is tracked separately from the 80% gate — the gate applies to the 5 core modules only.

---

## 6. What Can Be Tested in Headless CI

### ✅ Testable Without UI

| Component | How | WI |
|-----------|-----|-----|
| ViewModels (all 6) | Direct instantiation, call methods, assert properties | WI-R04 |
| MapRenderer | Static methods, no dependencies | WI-R03 |
| IGameDisplay/IGameInput interface conformance | Reflection | WI-R01 |
| Console game loop (end-to-end) | FakeDisplayService + FakeInputReader | WI-R06 |
| AvaloniaDisplayService (output methods) | Mock MainWindowViewModel, call methods, assert VM state | WI-R04 |
| InputPanelViewModel.Submit() | Direct call, assert event fires | WI-R04 |

### ❌ Requires Manual Testing (or Avalonia Headless Package)

| Component | Why | WI |
|-----------|-----|-----|
| Window rendering (6-panel layout) | Requires Avalonia visual tree | WI-R09 |
| Keyboard input in TextBox | Requires Avalonia input system | WI-R09 |
| Window close / process exit | Requires desktop lifetime | WI-R09 |
| Dispatcher.UIThread marshaling | Requires running Avalonia Application | WI-R09 |
| Actual TCS blocking behavior | Requires two real threads with Avalonia event loop | WI-R09 |

**Future option:** Avalonia provides `Avalonia.Headless` package for automated UI testing. This is OUT OF SCOPE for this phase but should be evaluated for P10 (integration testing).

---

## 7. Exit Criteria

This phase is complete when:

- [ ] All race conditions fixed (WI-R02)
- [ ] Dead code removed (WI-R05)
- [ ] Interface conformance tests pass (WI-R01)
- [ ] MapRenderer has 15+ tests (WI-R03)
- [ ] ViewModels have 18+ tests (WI-R04)
- [ ] Console regression suite has 12+ scenarios (WI-R06)
- [ ] Dungnz.Display ≥ 80% line coverage (WI-R07)
- [ ] Dungnz.Models ≥ 80% line coverage (WI-R07)
- [ ] CI builds Avalonia project explicitly (WI-R08)
- [ ] Manual smoke test checklist completed (WI-R09)
- [ ] All 2,154+ tests still pass (regression gate)
- [ ] No new P0 bugs introduced

**Then and only then** do we proceed to P6 (menu input methods).

---

## 8. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Avalonia NuGet packages in test project trigger AXAML generator | WI-R04 blocked | Use separate `Dungnz.Display.Avalonia.Tests` project |
| MapRenderer tests reveal BFS bugs | Map rendering broken for some dungeon shapes | Fix in WI-R03 scope (test-then-fix) |
| Console regression tests find existing bugs unrelated to Avalonia | Scope creep | File as separate issues, don't fix in this phase |
| Coverage push for Dungnz.Engine is hard (CombatEngine god class) | Engine stays below 80% | Accept 75% target for Engine; decomposition is separate tech debt |
| Manual smoke test reveals Avalonia rendering bugs | Blocks P6 | File issues, prioritize based on severity |

---

## Appendix A: Files Changed by Avalonia Work (P0–P5)

**New files (Dungnz.Models):**
- `IGameDisplay.cs` — 47-method output interface
- `IGameInput.cs` — 31-method input interface
- `MapRenderer.cs` — BFS map renderer (extracted from display layer)

**Modified files (Dungnz.Models):**
- `IDisplayService.cs` — now extends `IGameDisplay, IGameInput` (was standalone)

**New files (Dungnz.Display.Avalonia — entire project):**
- `Program.cs`, `App.axaml`, `App.axaml.cs`
- `AvaloniaAppBuilder.cs` (dead code — WI-R05 deletes)
- `AvaloniaDisplayService.cs` (910 lines)
- `AvaloniaInputReader.cs` (72 lines)
- `Controls/AsciiMapControl.cs`
- `Converters/TierColorConverter.cs`
- `ViewModels/` (6 files, 461 lines)
- `Views/` (7 files)

**Unchanged:** All existing production code in Dungnz.Display, Dungnz.Engine, Dungnz.Systems, Dungnz.Data.
