# Avalonia Migration Regression Audit

**Author:** Romanoff (QA Engineer)  
**Date:** 2026-07-22  
**Scope:** P0–P5 Avalonia migration test coverage audit  
**Baseline:** 2,154 tests passing, 0 failures on master  
**Coverage:** Dungnz.Display 74.62%, Dungnz.Models 78.79% (both below 80% gate)

---

## Executive Summary

The Avalonia migration through P5 has **zero automated test coverage**. The test project (`Dungnz.Tests.csproj`) does not even reference `Dungnz.Display.Avalonia`, making it impossible to test any Avalonia code. The interface split (IGameDisplay/IGameInput) is functionally complete but has zero dedicated tests verifying the split itself. MapRenderer (357 lines of BFS algorithm extracted in P1) has zero unit tests. The TCS input bridge pattern has race condition risks and zero tests.

Both coverage gates (Display 74.62%, Models 78.79%) are below the 80% CI threshold. The primary culprits are: MapRenderer (357 untested lines in Models), and incomplete coverage of ConsoleDisplayService (1,849 lines with ~57 of 62 public methods tested in Display).

---

## Findings by Risk Level

### 🔴 CRITICAL — Ship-Blocking Issues

#### C1. Test Project Does Not Reference Avalonia Project

**What:** `Dungnz.Tests.csproj` has no `<ProjectReference>` to `Dungnz.Display.Avalonia`.  
**Impact:** Zero Avalonia code is testable. All ViewModels, AvaloniaDisplayService, and AvaloniaInputReader are invisible to the test suite.  
**Evidence:** `Dungnz.Tests.csproj` references only: Dungnz.Models, Dungnz.Data, Dungnz.Systems, Dungnz.Display, Dungnz.Engine.  
**Note:** `Dungnz.Display.Avalonia.csproj` already has `InternalsVisibleTo("Dungnz.Tests")` — the Avalonia project is ready for testing, but the test project isn't wired.  
**Fix:** Add `<ProjectReference Include="..\Dungnz.Display.Avalonia\Dungnz.Display.Avalonia.csproj" />` to the test project. This will pull in the Avalonia NuGet packages (Avalonia 11.3.2, CommunityToolkit.Mvvm 8.4.0) into the test compilation, but ViewModel tests don't need a running UI.

#### C2. MapRenderer Has Zero Unit Tests (357 Lines, BFS Algorithm)

**What:** `Dungnz.Models/MapRenderer.cs` — static class with `BuildMarkupMap()`, `BuildPlainTextMap()`, and BFS grid algorithm — has zero dedicated tests.  
**Impact:** The core map rendering logic has no regression safety net. Any change to room symbol priority, legend generation, or BFS traversal could silently break the map.  
**Evidence:** `grep -rn "MapRenderer" Dungnz.Tests/ → 0 results`  
**Risk:** BFS algorithm correctness, room symbol priority chain (16 conditions), legend entry filtering, connector rendering, and edge cases (single room, no exits, cycles) are all untested.  
**Coverage Impact:** 357 lines in Dungnz.Models with 0% coverage directly contributes to the 78.79% figure.

#### C3. AvaloniaInputReader TCS Pattern Has Thread-Safety Risks

**What:** `AvaloniaInputReader.ReadLine()` creates a `TaskCompletionSource<string?>`, stores it in `_pendingLine`, then blocks with `tcs.Task.GetAwaiter().GetResult()`. The event handler `OnInputSubmitted` reads `_pendingLine`, nulls it, then calls `TrySetResult`.  
**Risks Identified:**
1. **Race on `_pendingLine`:** If `OnInputSubmitted` fires before `_pendingLine` is assigned (Dispatcher.UIThread.InvokeAsync is async — event subscription + enable happen on UI thread after `_pendingLine` is set on game thread), this specific race appears safe because `_pendingLine` is set before the UI thread invoke. However, if two `ReadLine()` calls overlap (shouldn't happen in single-game-thread model, but no guard exists), the second would overwrite `_pendingLine` and orphan the first TCS.
2. **No cancellation support:** If the game needs to abort (window close, timeout), the TCS blocks forever. No `CancellationToken` is threaded through.
3. **No timeout:** A hung UI or unresponsive input panel means a permanently blocked game thread.
4. **`AvaloniaDisplayService.ReadCommandInput()` has the same pattern** but uses `_pendingCommand` and a local event handler — the local handler captures `_pendingCommand` by closure before it's nulled, which is correct but fragile.

**Evidence:** `AvaloniaInputReader.cs` lines 38-55, `AvaloniaDisplayService.cs` lines 762-797.

#### C4. Interface Split (IGameDisplay/IGameInput) Has Zero Dedicated Tests

**What:** P0 split `IDisplayService` into `IGameDisplay` (38 methods) + `IGameInput` (24 methods). `IDisplayService` inherits both for backward compatibility. No test verifies:
- That implementations satisfy the split interfaces independently
- That code depending on `IGameDisplay` alone compiles and works
- That the method partitioning is correct (no methods in wrong interface)

**Evidence:** `grep -rn "IGameDisplay\|IGameInput" Dungnz.Tests/ → 0 results`  
**Impact:** If a method is accidentally moved to the wrong sub-interface, or a new method is added to the wrong one, no test catches it.

---

### 🟠 HIGH — Significant Coverage Gaps

#### H1. AvaloniaDisplayService (910 lines) — Zero Tests

**What:** `AvaloniaDisplayService.cs` implements all 62 IDisplayService methods. The IGameDisplay output methods (38) use `Dispatcher.UIThread.InvokeAsync` for all UI updates. The IGameInput methods (23) return hardcoded defaults (stubs). Only `ReadCommandInput()` has real TCS logic.  
**Testable Without UI:**
- `StripAnsi()` — regex-based ANSI removal (private static)
- `BuildPlainHpBar()` — HP bar calculation (private static)
- `ItemTypeIcon()`, `SlotIcon()`, `ItemIcon()` — icon mapping (private static)
- `PrimaryStatLabel()` — stat label selection (private static)
- `GetRoomDisplayName()` — room type mapping (private static)
- `GetEquippedInSameSlot()` — equipment slot matching (private static)
- `FormatDelta()` — number formatting (private static)

**Hard to Test (Dispatcher dependency):** All 38 IGameDisplay methods wrap logic in `Dispatcher.UIThread.InvokeAsync`.  
**Note:** Static helpers are `private` — testing requires either making them `internal` or testing through public API with mocked ViewModels.

#### H2. Avalonia ViewModels (461 lines total) — Zero Tests

| ViewModel | Lines | Testable Logic |
|-----------|-------|----------------|
| GearPanelViewModel | 169 | BuildGearText, BuildEnemyStatsText, BuildPlainHpBar, EffectIcon |
| StatsPanelViewModel | 101 | BuildPlayerStatsText, BuildPlainHpBar, BuildPlainMpBar, class conditionals |
| LogPanelViewModel | 62 | AppendLog, ClassifyCombatLogIcon, history buffer management |
| ContentPanelViewModel | 50 | AppendMessage (50-line cap), SetContent (string split), Clear |
| InputPanelViewModel | 37 | Submit (trim, clear, disable, event raise) |
| MapPanelViewModel | 26 | Update (delegates to MapRenderer) |
| MainWindowViewModel | 16 | Composition (trivial) |

**All ViewModels are highly testable** — they use `ObservableObject` from CommunityToolkit.Mvvm, have no Dispatcher dependencies, and contain pure computational logic. This is the easiest win.

#### H3. Dungnz.Display Coverage at 74.62% — Analysis

**Root causes (estimated):**
1. **`[ExcludeFromCodeCoverage]`** on 4 files: SpectreDisplayService (1,662 lines), SpectreLayoutDisplayService (1,315 lines), SpectreLayout (109 lines), SpectreLayoutContext (98 lines). These are excluded from the denominator, so they don't drag coverage *down* but also don't contribute.
2. **ConsoleDisplayService.cs (1,849 lines):** Has 57 test methods covering ~57 of 62 public methods. Likely ~5 methods with low branch coverage:
   - `ShowRoom()` — heavily branched (room types, hazards, environmental prefixes)
   - `ShowEquipmentComparison()` — complex delta logic
   - `SelectClass()` — interactive class selection
   - `SelectDifficulty()` — interactive difficulty selection
   - `ReadSeed()` — seed input parsing
3. **AnsiMarkupConverter.cs (126 lines):** Has 11 tests but likely missing edge cases (256-color, RGB codes).

#### H4. Dungnz.Models Coverage at 78.79% — Analysis

**Root causes (estimated):**
1. **MapRenderer.cs (357 lines):** 0% coverage. This is the single biggest contributor to the gap.
2. **PlayerCombat.cs (361 lines):** No dedicated unit tests. Tested only indirectly through integration tests. Complex equipment slot logic.
3. **PlayerStats.cs (323 lines):** No dedicated unit tests. Tested indirectly through PlayerTests.
4. **IGameDisplay.cs (308 lines) / IGameInput.cs (179 lines):** Interface files — these contribute to line count but have no executable code (only method signatures + XML docs). They should be excluded from coverage.
5. **PlayerSkillHelpers.cs (91 lines):** Flagged in gap tracking as untested.
6. **Difficulty.cs (160 lines):** DifficultySettings calculation methods need coverage.

---

### 🟡 MEDIUM — Quality Improvements

#### M1. FakeDisplayService Does Not Implement Split Interfaces Separately

**What:** `FakeDisplayService` implements `IDisplayService` (combined). There's no `FakeGameDisplay` or `FakeGameInput` for testing code that depends on the narrower interfaces.  
**Impact:** Can't write tests that verify a class works with only `IGameDisplay` injection.

#### M2. Stub IGameInput Methods in AvaloniaDisplayService Are Untested

**What:** 22 IGameInput methods return hardcoded defaults (`null`, `0`, `false`, `"a"`, `StartupMenuOption.NewGame`, etc.). While these are stubs for P6-P8, they are the current running code.  
**Risk:** If a stub returns a value that causes an unexpected code path (e.g., `ShowCombatMenuAndSelect` returns `"a"` which is valid attack — fine. But `ShowShopAndSelect` returns `0` which may be treated as a valid shop index or as cancel).

#### M3. AvaloniaDisplayService Cached State Not Validated

**What:** `AvaloniaDisplayService` caches `_cachedPlayer`, `_cachedRoom`, `_cachedCombatEnemy`, `_cachedEnemyEffects`, `_cachedCooldowns`. These are written from the game thread (inside Dispatcher.InvokeAsync lambdas) and read from subsequent calls.  
**Risk:** No null guards on cached state when used in subsequent method calls. If `ShowCombatStatus` is called before `ShowRoom`, `_cachedRoom` is null.

#### M4. Duplicate BuildPlainHpBar Implementations

**What:** `BuildPlainHpBar()` exists in three places:
1. `AvaloniaDisplayService.cs` (private static)
2. `GearPanelViewModel.cs` (private static)
3. `StatsPanelViewModel.cs` (private static)

All three are identical (same algorithm, same parameters). This is a DRY violation with maintenance risk.

#### M5. LogPanelViewModel Icon Classification Lacks Tests

**What:** `ClassifyCombatLogIcon()` uses string pattern matching (`Contains("Critical")`, `Contains("Healed")`, etc.) to assign emoji icons. If combat message wording changes, icons silently break.

---

### 🟢 LOW — Polish Items

#### L1. AsciiMapControl.cs and TierColorConverter.cs Are Stubs

**What:** Both are empty stub classes. No logic to test.  
**Impact:** None until P6-P8 implements them.

#### L2. InputPanel.axaml.cs Code-Behind Untestable Without UI

**What:** `OnCommandInputKeyDown` handler requires Avalonia UI context. Can't unit test keyboard routing.  
**Impact:** Low — the actual Submit() call is testable through InputPanelViewModel.

#### L3. App.axaml.cs Orchestration Code Untestable

**What:** `OnFrameworkInitializationCompleted()` is the composition root — creates all services and wires them. Not unit-testable.  
**Impact:** Low — integration testing would cover this, but not practical in CI.

---

## Specific Test Recommendations

### Priority 1 — Reach 80% Coverage (Estimated: 35-45 new tests)

#### 1A. MapRendererTests.cs (NEW — Dungnz.Tests/Display/)

Target: Cover `BuildPlainTextMap`, `BuildMarkupMap`, and BFS algorithm.

| Test Method | What It Verifies |
|-------------|-----------------|
| `BuildPlainTextMap_SingleRoom_ShowsCurrentPlayerSymbol` | `[@]` appears for current room |
| `BuildPlainTextMap_TwoRoomsNorthSouth_ShowsVerticalConnector` | `│` connector between rooms |
| `BuildPlainTextMap_TwoRoomsEastWest_ShowsHorizontalConnector` | `─` connector between rooms |
| `BuildPlainTextMap_EmptyGrid_ReturnsNoMapData` | Edge case: null or disconnected room |
| `BuildPlainTextMap_UnvisitedRoom_ShowsQuestionMark` | `[?]` for fog of war |
| `BuildPlainTextMap_EnemyRoom_ShowsExclamation` | `[!]` for rooms with alive enemies |
| `BuildPlainTextMap_BossRoom_ShowsBSymbol` | `[B]` for exit room with live boss |
| `BuildPlainTextMap_ClearedBossRoom_ShowsExit` | `[E]` after boss defeated |
| `BuildPlainTextMap_ShrineRoom_ShowsSSymbol` | `[S]` for active shrine |
| `BuildPlainTextMap_MerchantRoom_ShowsMSymbol` | `[M]` for merchant |
| `BuildPlainTextMap_SpecialRoomTypes_ShowsCorrectSymbols` | `[T]`, `[A]`, `[L]`, `[F]` |
| `BuildPlainTextMap_Legend_ContainsOnlyVisibleTypes` | Dynamic legend only includes present types |
| `BuildPlainTextMap_LargeGrid_RendersAllVisibleRooms` | 5+ room BFS layout |
| `BuildMarkupMap_SingleRoom_ContainsSpectreMarkup` | `[bold yellow]` markup present |
| `BuildMarkupMap_EmptyGrid_ReturnsNoMapDataMarkup` | `[grey]No map data.[/]` |

**Estimated tests:** 15  
**Coverage impact:** +357 lines in Dungnz.Models → ~+8% on Models project

#### 1B. ConsoleDisplayServiceGapTests.cs (NEW or extend existing)

Fill remaining 5 untested methods:

| Test Method | What It Verifies |
|-------------|-----------------|
| `ShowRoom_DarkRoom_ShowsDarkPrefix` | Room type prefix logic |
| `ShowRoom_HazardRoom_ShowsHazardWarning` | Environmental hazard display |
| `ShowRoom_WithFloorItems_ShowsItemList` | Floor item rendering |
| `ShowEquipmentComparison_UpgradeItem_ShowsPositiveDelta` | Delta calculation |
| `ShowEquipmentComparison_DowngradeItem_ShowsNegativeDelta` | Negative delta |
| `ReadSeed_ValidNumber_ReturnsParsedInt` | Seed parsing happy path |
| `ReadSeed_NonNumeric_ReturnsNull` | Seed parsing edge case |

**Estimated tests:** 7  
**Coverage impact:** ~+2-3% on Display project

### Priority 2 — Avalonia ViewModel Tests (Estimated: 30-40 new tests)

**Prerequisite:** Add Avalonia project reference to test project. Will also need `CommunityToolkit.Mvvm` in test compilation (transitive dependency from Avalonia project).

#### 2A. StatsPanelViewModelTests.cs (NEW)

| Test Method | What It Verifies |
|-------------|-----------------|
| `Update_Warrior_ShowsNameLevelClass` | Header formatting |
| `Update_WithMana_ShowsMpBar` | MP bar present when MaxMana > 0 |
| `Update_ZeroMana_HidesMpBar` | MP bar absent when MaxMana = 0 |
| `Update_WithCooldowns_ShowsCooldownLine` | CD formatting with turn counts |
| `Update_ReadyCooldown_ShowsCheckmark` | `✅` when turnsRemaining = 0 |
| `Update_Rogue_ShowsComboPoints` | `●○` combo point visualization |
| `Update_Warrior_ChargedFury_ShowsChargedLabel` | `[CHARGED]` suffix |
| `BuildPlainHpBar_FullHP_AllFilled` | `██████████` |
| `BuildPlainHpBar_HalfHP_HalfFilled` | `█████░░░░░` |
| `BuildPlainHpBar_ZeroHP_AllEmpty` | `░░░░░░░░░░` |
| `BuildPlainHpBar_ZeroMax_AllEmpty` | Edge case: max <= 0 |

**Estimated tests:** 11

#### 2B. GearPanelViewModelTests.cs (NEW)

| Test Method | What It Verifies |
|-------------|-----------------|
| `Update_NoGear_ShowsAllEmpty` | 10 "(empty)" slots |
| `Update_WeaponEquipped_ShowsAttackStat` | `+ATK` for weapon slot |
| `Update_ArmorEquipped_ShowsDefenseStat` | `+DEF` for armor |
| `Update_WithSetBonus_ShowsBonusDescription` | Set bonus line present |
| `ShowEnemyStats_BasicEnemy_ShowsHpBar` | HP bar and stats |
| `ShowEnemyStats_BossEnemy_ShowsPhaseInfo` | Phase number display |
| `ShowEnemyStats_EnragedBoss_ShowsEnragedBadge` | `⚡ ENRAGED` badge |
| `ShowEnemyStats_EliteEnemy_ShowsEliteBadge` | `⭐ Elite` badge |
| `ShowEnemyStats_WithEffects_ShowsEffectIcons` | Effect icon rendering |
| `EffectIcon_AllEffects_ReturnCorrectEmoji` | 13-case switch coverage |
| `BuildPlainHpBar_Clamping_NeverExceedsMax` | Overflow prevention |

**Estimated tests:** 11

#### 2C. LogPanelViewModelTests.cs (NEW)

| Test Method | What It Verifies |
|-------------|-----------------|
| `AppendLog_InfoType_ShowsInfoIcon` | `ℹ` icon for info |
| `AppendLog_ErrorType_ShowsErrorIcon` | `❌` icon for error |
| `AppendLog_LootType_ShowsLootIcon` | `💰` icon for loot |
| `AppendLog_CombatCritical_ShowsCritIcon` | `💥` for "Critical" |
| `AppendLog_CombatHeal_ShowsHealIcon` | `💚` for "Healed" |
| `AppendLog_CombatPoison_ShowsPoisonIcon` | `☠` for "Poison" |
| `AppendLog_CombatBurn_ShowsBurnIcon` | `🔥` for "Burn" |
| `AppendLog_ExceedsMaxHistory_DropsOldest` | Buffer limit at 50 |
| `AppendLog_DisplayLimit_ShowsLast12` | Display truncation |
| `AppendLog_IncludesTimestamp` | `HH:mm` prefix |

**Estimated tests:** 10

#### 2D. ContentPanelViewModelTests.cs (NEW)

| Test Method | What It Verifies |
|-------------|-----------------|
| `AppendMessage_UnderLimit_AddsLine` | Basic append |
| `AppendMessage_AtLimit_RemovesOldest` | 50-line cap enforcement |
| `SetContent_MultiLine_SplitsOnNewline` | `\n` splitting |
| `SetContent_Empty_ClearsLines` | Empty content handling |
| `SetContent_SetsHeader` | Header text updated |
| `Clear_RemovesAllLines` | Collection cleared |

**Estimated tests:** 6

#### 2E. InputPanelViewModelTests.cs (NEW)

| Test Method | What It Verifies |
|-------------|-----------------|
| `Submit_RaisesInputSubmittedEvent` | Event firing |
| `Submit_TrimsWhitespace` | Trimming behavior |
| `Submit_ClearsCommandText` | Text cleared after submit |
| `Submit_DisablesInput` | IsInputEnabled = false |
| `Submit_NoSubscriber_DoesNotThrow` | Null event safety |

**Estimated tests:** 5

### Priority 3 — Interface Split & TCS Tests (Estimated: 10-15 new tests)

#### 3A. InterfaceSplitTests.cs (NEW — Dungnz.Tests/Architecture/)

| Test Method | What It Verifies |
|-------------|-----------------|
| `IDisplayService_InheritsBothInterfaces` | Compilation check |
| `ConsoleDisplayService_ImplementsIGameDisplay` | Cast succeeds |
| `ConsoleDisplayService_ImplementsIGameInput` | Cast succeeds |
| `AvaloniaDisplayService_ImplementsIGameDisplay` | Cast succeeds |
| `AvaloniaDisplayService_ImplementsIGameInput` | Cast succeeds |
| `IGameDisplay_HasNoInputMethods` | Architecture rule: no ReadX/SelectX/ShowXAndSelect |
| `IGameInput_HasNoOutputOnlyMethods` | Architecture rule: no ShowX without return value |

**Estimated tests:** 7

#### 3B. AvaloniaInputReaderTests.cs (NEW)

Testing the TCS pattern without a live UI:

| Test Method | What It Verifies |
|-------------|-----------------|
| `ReadLine_SubmitText_ReturnsText` | Happy path: simulate InputSubmitted event |
| `ReadLine_EmptySubmit_ReturnsNull` | Blank input → null |
| `ReadLine_WhitespaceSubmit_ReturnsNull` | Whitespace → null |
| `ReadLine_TrimsResult` | Leading/trailing spaces stripped |
| `ReadKey_ReturnsNull` | Stub behavior |
| `IsInteractive_ReturnsFalse` | Stub behavior |
| `Constructor_NullInputVM_Throws` | Null guard |

**Note:** Testing `ReadLine()` requires running the TCS resolution on a separate thread (since `ReadLine` blocks). Pattern:
```csharp
var vm = new InputPanelViewModel();
var reader = new AvaloniaInputReader(vm);
// Must simulate Dispatcher — use SynchronizationContext or test on threadpool
var readTask = Task.Run(() => reader.ReadLine());
await Task.Delay(50);
vm.InputSubmitted?.Invoke("attack");
var result = await readTask;
result.Should().Be("attack");
```

**Challenge:** `Dispatcher.UIThread.InvokeAsync` will throw if no Avalonia application is running. Tests may need to mock or bypass the Dispatcher. Options:
1. Extract Dispatcher usage behind an interface (clean but requires refactoring)
2. Use Avalonia's headless platform for testing (`Avalonia.Headless`)
3. Make the UI-thread invocation overridable (template method pattern)

**Estimated tests:** 7

---

## Coverage Impact Estimates

### Dungnz.Models: 78.79% → 80%+

| Action | Lines Covered | Estimated Impact |
|--------|--------------|-----------------|
| MapRendererTests (15 tests) | +300 of 357 lines | +~6.7% |
| PlayerSkillHelpers tests (existing gap) | +75 of 91 lines | +~1.7% |
| **Total** | +375 lines | **~+8.4% → 87%+** |

### Dungnz.Display: 74.62% → 80%+

| Action | Lines Covered | Estimated Impact |
|--------|--------------|-----------------|
| ConsoleDisplayService gap tests (7 tests) | +100 of 1849 lines | +~1.7% |
| AnsiMarkupConverter edge cases (3 tests) | +15 of 126 lines | +~0.25% |
| LayoutConstants validation (existing) | +10 of 38 lines | +~0.17% |
| **Total** | +125 lines | **~+2.1% → 76.7%** |

**⚠️ WARNING:** Reaching 80% in Dungnz.Display may require additional strategies:
- Remove `[ExcludeFromCodeCoverage]` from `SpectreLayoutDisplayService` panel-building methods that are already tested via PanelHeightRegressionTests and PanelMarkupSnapshotTests (they call `BuildPlayerStatsPanelMarkup` and `BuildGearPanelMarkup` which ARE internal seam methods)
- Or: Move more testable logic out of excluded classes into utility methods

### Dungnz.Display.Avalonia: 0% → Not Currently Measured

The Avalonia project is a separate assembly. Coverage collection needs to be configured to include it. Estimated ~55 tests would cover the ViewModel layer and static helpers.

---

## Test Count Summary

| Category | New Tests | Priority |
|----------|-----------|----------|
| MapRendererTests | 15 | P1 (coverage gate) |
| ConsoleDisplayService gaps | 7 | P1 (coverage gate) |
| StatsPanelViewModelTests | 11 | P2 (Avalonia quality) |
| GearPanelViewModelTests | 11 | P2 (Avalonia quality) |
| LogPanelViewModelTests | 10 | P2 (Avalonia quality) |
| ContentPanelViewModelTests | 6 | P2 (Avalonia quality) |
| InputPanelViewModelTests | 5 | P2 (Avalonia quality) |
| InterfaceSplitTests | 7 | P3 (architecture) |
| AvaloniaInputReaderTests | 7 | P3 (thread safety) |
| **Total** | **79** | |

**Projected test count:** 2,154 → 2,233

---

## Open Questions for Coulson / Anthony

1. **Avalonia headless testing:** Should we add `Avalonia.Headless` NuGet to the test project for Dispatcher-dependent tests, or refactor AvaloniaDisplayService to make Dispatcher calls overridable?
2. **ExcludeFromCodeCoverage on SpectreLayoutDisplayService:** The internal seam methods (`BuildPlayerStatsPanelMarkup`, `BuildGearPanelMarkup`) are already tested via PanelHeightRegressionTests. Should we remove the class-level exclusion and add method-level exclusions only for truly untestable methods?
3. **Coverage threshold per-project:** Should the 80% gate apply to Dungnz.Display.Avalonia separately, or is it only on the main game projects?
4. **TCS cancellation:** Should we add CancellationToken support to AvaloniaInputReader before shipping? The current implementation blocks forever if the UI hangs.
