# Romanoff — History

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Learnings

### 2026-02-28: PR Review — Bug Hunt Fix Session (#625, #626)

**PRs Reviewed:**
- PR #625 — Barton's systems fixes (6 bugs: #611–#616)
- PR #626 — Hill's display/engine fixes (13 bugs: #604–#610, #618–#624)

**Review Findings:**

**PR #625 — APPROVED** (684/684 tests)
- #611: PerformEnemyTurn correctly called before `continue` in Cancel branch; death check present ✓
- #612: `pool.Count > 0` guard prevents crash; empty pool returns null item + gold gracefully ✓
- #613: All 3 enemy DoT effects (Poison/Bleed/Burn) now use `Math.Max(0, HP - N)` ✓
- #614: `SelfHealEveryTurns - 1` reset with decrement-first pattern produces correct N-turn interval ✓
- #615: `SpendMana(manaLost)` called with pre-validated mana amount ✓
- #616: `CheckLevelUp` moved before XP display; post-level threshold shown correctly ✓

**PR #626 — APPROVED** (684/684 tests; one ShieldBash flake observed, confirmed pre-existing)
- Display branch built on top of systems branch (2 commits above master)
- VisualWidth() correctly adds 1 extra col per ⚔/⭐ (only double-width BMP emoji in codebase) ✓
- HandleUse no-effect paths: _turnConsumed=false, "Nothing happened." message ✓
- _stats = new RunStats() after successful load, not in catch ✓
- ConsoleMenuNavigator try/finally restores cursor; SelectFromMenu delegates to it ✓
- Scrolling menu: maxVisible=WindowHeight-4, ↑/↓ indicators, boundary wrap correct ✓
- Escape/X are no-ops in ConsoleMenuNavigator (menu stays open, user must press Enter) ✓
- SelectFromMenu delegates to _navigator.Select(); dead inline loop removed ✓
- ShowLevelUpChoice padding corrected (W-13, W-25) ✓

**Key observations:**
- Both PRs merged successfully via squash merge (#625 first, then #626)
- Display branch included systems fixes as first commit — squash diff correctly excludes the already-merged systems changes
- GitHub cannot approve own PRs — used `gh pr comment` for review documentation

**Tests Written (PR #627):**
- `AbilityCancel_EnemyTurnStillRuns_PlayerTakesDamage` (#611) — enemy attacks after ability cancel
- `RollDrop_EmptyTierPools_DoesNotThrow_ReturnsGoldOnly` (#612) — empty pool guarded with try/finally cleanup
- `Poison_EnemyAtOneHP_ClampsToZeroNotNegative` (#613) — HP floors at 0
- `Bleed_EnemyAtOneHP_ClampsToZeroNotNegative` (#613)
- `Burn_EnemyAtOneHP_ClampsToZeroNotNegative` (#613)
- #622 (Escape no-op): not unit-testable — ConsoleMenuNavigator requires live console I/O

**Test count:** 684 baseline → 689 with new tests

**Patterns learned:**
- ControlledRandom(defaultDouble: X) must be chosen carefully — 0.1 is below the 0.15 crit threshold and causes enemy to always crit; use 0.9 for deterministic non-crit tests
- LootTable.SetTierPools is static — tests mutating it must restore pools in try/finally
- ShieldBash test is order-sensitive (flaky under parallel test execution) — confirmed pre-existing

### 2026-02-27: Deep Bug Hunt Session
**Scope:** Full source review — DisplayService, ConsoleMenuNavigator, GameLoop, CombatEngine, Models
**Tests baseline:** 684 tests passing, build succeeds with 35 warnings (XML doc only)

**Bugs Found (8 total):**
1. #617 — HandleLoad does not reset _stats; inflated end-of-run summaries after save/load
2. #618 — ShowEnemyDetail HP line overflows box right border by 9 chars (formula uses W-14, should be W-23)
3. #619 — ShowEnemyDetail name line is 2 chars short of box border (W-4 should be W-2)
4. #620 — ShowCombatStart banner overflows 44-char border by 2 when sword emoji renders double-width
5. #621 — ShowLootDrop weapon loot card name line overflows by 1 (sword icon BMP .Length=1 vs visual=2)
6. #622 — SelectFromMenu Escape/X silently selects last option even in menus without a Cancel option
7. #623 — IMenuNavigator injected into ConsoleDisplayService but never used (_navigator field is dead)
8. #624 — ShowLevelUpChoice box padding wrong (dead method, but on IDisplayService interface)

**Categories:**
- State management: 1 bug
- Box border alignment: 4 bugs (3 in ShowEnemyDetail/ShowCombatStart/ShowLootDrop + 1 dead method)
- Menu navigation: 2 bugs (Escape behavior, unused navigator dependency)

**Patterns Identified:**
- `icon.Length` ≠ visual column width for BMP emoji like U+2694 sword (1 C# char, 2 visual cols)
- Surrogate pair emoji (.Length=2) happen to match their ~2 visual col width and work correctly
- `PadRightVisible`/`StripAnsiCodes` do NOT account for emoji visual width — only ANSI codes stripped
- Box padding formulas need careful manual counting; hpBar width (10) was omitted from ShowEnemyDetail
- `_navigator` injection architecture is incomplete — SelectFromMenu is a reimplementation

**Fix Assignments (suggested):** Hill (display fixes), Barton (save/load stats), architecture clarification on navigator

### 2026-02-20: WI-10 Code Review (Feature-Complete Quality Pass)
**Reviewed:** All 23 source files (Program.cs, Models, Engine, Systems, Display)
**Scope:** Bug hunting, logic errors, architectural violations, edge cases

**Issues Found (7 total):**
1. Program.cs:8-9 — Direct Console.Write/ReadLine violating DisplayService-only architecture
2. GameLoop.cs:30 — Direct Console.Write("> ") prompt violation
3. CombatEngine.cs:22 — Direct Console.Write("[A]ttack or [F]lee? ") prompt violation
4. GameLoop.cs:123-130 — Dead enemies not cleared from room after combat (HP=0 but Enemy!=null persists)
5. GameLoop.cs:109 — Win condition checked dead boss via HP<=0 instead of Enemy==null
6. Architectural violation: Multiple classes bypassing DisplayService for output
7. Logic bug: Dead enemy objects remain in rooms, could cause re-combat or confusion

**Fixes Applied (all 7 fixed):**
- Added `ShowCommandPrompt()` to DisplayService
- Added `ShowCombatPrompt()` to DisplayService  
- Added `ReadPlayerName()` to DisplayService
- Updated Program.cs to use DisplayService.ReadPlayerName()
- Updated GameLoop to use DisplayService.ShowCommandPrompt()
- Updated CombatEngine to use DisplayService.ShowCombatPrompt()
- Fixed GameLoop to set `_currentRoom.Enemy = null` after `CombatResult.Won`
- Simplified win condition to check `_currentRoom.Enemy == null` (no dead enemy object edge case)

**Patterns to Watch:**
- Console I/O violations: Always route through DisplayService
- Dead enemy cleanup: Any system that mutates enemy state must handle lifecycle
- Null vs zero HP: Prefer explicit null for "defeated" state over HP<=0 checks
- Edge case: What happens if player flees combat then returns? (Currently enemy HP persists — acceptable behavior)

**Verdict:** APPROVED
- All architectural violations corrected
- Logic bugs fixed (dead enemy cleanup)
- No null safety issues found (LootTable = null! is safe — always initialized in enemy constructors)
- Edge cases handled: empty inventory USE, GO with no arg, TAKE with no items, combat with dead enemy
- Code is ready to ship

### 2026-02-20: Retrospective Ceremony & v2 Planning Decisions

**Team Update:** Retrospective ceremony identified 3 refactoring decisions for v2:

1. **DisplayService Interface Extraction** — Extract IDisplayService interface to enable mock test implementations. Romanoff will build test harness on this interface.

2. **Player Encapsulation Refactor** — Hill refactoring Player model with validation. Improves state safety and enables future extensions (save/load, analytics).

3. **Test Infrastructure Required** — Romanoff leads test infrastructure buildout: xUnit/NUnit harness, injected Random for deterministic combat/loot testing, integration with CI/CD. Blocks v2 feature work. Effort: 1-2 sprints.

**Participants:** Coulson (facilitator), Hill, Barton, Romanoff

**Impact:** Romanoff owns test infrastructure as blocking work for v2. High priority: regression risk without tests is unacceptable for future changes.

### 2026-02-20: v2 Testing Strategy Proposal

**Authored comprehensive testing strategy for v2:**

**Framework:** xUnit 2.6+ selected over NUnit (better async, cleaner isolation, industry standard)

**Project Structure:** Dungnz.Tests project with Unit/, Integration/, Fixtures/, TestHelpers/ organization

**Mocking Strategy:**
- IDisplayService extraction required (Hill owns) → enables headless testing with Moq
- Random injection into CombatEngine/LootTable → enables deterministic testing with seeded Random
- FluentAssertions for readable test assertions

**Coverage Strategy (Must-Have Tests):**
- CombatEngine unit tests: player kills enemy, enemy kills player, flee success/failure, flee failure death, level-up mid-combat, minimum damage rule, loot distribution
- LootTable unit tests: deterministic drops, first-match-wins, gold ranges, empty loot table
- InventoryManager unit tests: TakeItem, UseItem (consumable/weapon/armor), empty inventory edge cases
- CommandParser unit tests: all commands, shortcuts, case insensitivity, argument parsing
- Player unit tests (post-encapsulation): TakeDamage, Heal, ModifyStat validation, state constraints
- DungeonGenerator integration: 5x4 grid, connectivity (BFS), spawn/exit rooms, boss placement, bidirectional exits
- GameLoop integration: combat triggers, dead enemy cleanup, exit blocking, win/lose conditions

**Manual Testing Only:** DisplayService formatting (visual validation more efficient than brittle assertions), end-to-end gameplay balance

**Edge Case Inventory (v1 Learnings + v2 Risks):**
- Dead enemy cleanup regression test (room.Enemy == null after Won)
- Flee penalty death edge case (CombatResult.PlayerDied if flee damage kills)
- Flee-and-return behavior (enemy HP persists — accepted, but must consider for save/load)
- Item stacking exploit monitoring (infinite weapon/armor equips)
- Save/load JSON deserialization safety (if added): validate HP <= MaxHP, no negative stats
- Player encapsulation validation: TakeDamage/Heal input validation, stat caps enforced

**Quality Gates:**
- Build gates: 0 test failures allowed, 85-100% coverage on high-risk systems (combat/loot/inventory/player)
- Manual gates: Architecture review (no Console.Write, no hardcoded Random, Player encapsulation), edge case validation, playtest validation
- Rejection criteria: Test failures, coverage drops, architectural violations, edge case regressions, null refs, soft-lock bugs

**C# Testing Patterns:**
- Arrange-Act-Assert structure (always)
- Theory + InlineData for parameterized tests (e.g., CommandParser input variations)
- Moq IDisplayService verification for critical messages ("LEVEL UP!", "You defeated")
- System.Text.Json fixtures for save/load testing (if feature added)
- Deterministic Random pattern: `new Random(42)` in tests, `rng ?? new Random()` in production
- Builder pattern for test data factories (PlayerBuilder, EnemyBuilder)
- FluentAssertions for readability: `result.Should().Be(CombatResult.Won)`

**Timeline Estimate:** 1.5-2 sprints
- Phase 1: Infrastructure (IDisplayService extraction, Random injection, test project setup) — 1 week
- Phase 2: Unit tests (CombatEngine, LootTable, InventoryManager, CommandParser) — 1 week
- Phase 3: Integration tests (DungeonGenerator, GameLoop) — 3-4 days
- Phase 4: Regression suite (WI-10 edge cases) — 2 days
- Phase 5: CI/CD integration (GitHub Actions, dotnet test) — 1 day

**Blocking Dependencies:**
- Hill: IDisplayService interface extraction (1-2 hours)
- Barton: CombatEngine/LootTable Random injection (2-3 hours)
- Hill: Player encapsulation refactor (2-3 hours)

**Patterns Identified:**
- Dependency injection for testability: inject Random, IDisplayService, future IFileSystem/IClock
- Interface extraction for external dependencies: Console → IDisplayService, File → IFileSystem
- Optional parameter pattern for production defaults: `Random? rng = null` → `_rng = rng ?? new Random()`

**File:** `.ai-team/decisions/inbox/romanoff-v2-testing-strategy.md` (18KB comprehensive strategy document)

## 📌 Team Update (2026-02-20): Phase Gates and Testing Infrastructure
**From Scribe** — Team decisions merged; phase dependencies confirmed:
- **Phase Structure:** Coulson's phase gates linked to Romanoff's test coverage thresholds. Testing work (Phase 1) unblocks architecture work (Phase 2) which unblocks features (Phase 3).
- **Infrastructure Priority:** IDisplayService extraction (Hill + Coulson) and Random injection (Coulson + Hill) are prerequisites for test harness. Romanoff can begin xUnit setup once these interfaces/injections in place.
- **Coverage Targets:** 70%+ code coverage required Phase 1→2 gate. CombatEngine, LootTable, InventoryManager = 100% coverage must-haves per testing strategy.

**Impact on Romanoff:** Confirmed xUnit + Moq + FluentAssertions stack (vs NUnit). Mocking strategy (IDisplayService + seeded Random) documented and agreed across team.

📌 Team update (2026-02-20): CI Quality Gate Infrastructure established — 70% code coverage threshold enforced by GitHub Actions. Test project framework fixed (net10.0 → net9.0).

### 2026-02-20: v3 Quality Planning - Test Coverage Gaps & Strategy

**Scope:** Post-v2 test inventory audit; identify coverage gaps; plan v3 testing infrastructure for character classes, shops, crafting systems.

**v2 Test Inventory:**
- **Test Classes:** 13 total (139 test methods)
- **Coverage by System:**
  - ✅ **Full coverage:** CombatEngine (12), CommandParser (14), DisplayService (17), EnemyFactory (17), GameLoop (20), InventoryManager (8), LootTable (8), DungeonGenerator (10), AbilityManager (20), PlayerManaTests (10)
  - ⚠️ **Partial coverage:** Player (7 tests, basic only), EnemyTests (5, stub only)
  - ❌ **Zero coverage:** AchievementSystem (96 LOC), SaveSystem (178 LOC), StatusEffectManager (84 LOC), EnemyConfig, ItemConfig, GameEvents, RunStats, Enemies (9 types × 29-62 LOC)

**Critical Coverage Gaps (v2):**
1. **StatusEffectManager:** No unit tests for poison/bleed/burn/stun/regen/slow application, duration tracking, immunity handling (Stone Golem), turn-start processing, effect removal. Risk: Combat balance broken by untested effect logic.
2. **AchievementSystem:** No tests for unlock conditions, persistence (JSON save), Glass Cannon/Untouchable/Hoarder/EliteHunter/SpeedRunner logic. Risk: Silent achievement unlock failures; save corruption.
3. **SaveSystem:** No tests for GameState serialization, room graph reconstruction, deserialization safety (HP validation, stat bounds, null safety). Risk: Save/load data loss; soft-locks on corrupted saves.
4. **Equipment System:** Player equipment slots (weapon/armor/accessory) exist but no tests for swap logic, stat bonuses, or unequip edge cases. Risk: Stat exploitation (equip multiple weapons).
5. **Enemy Variants (Elite 5% & Config Scaling):** Elite enemy generation logic untested; config-driven stat scaling untested. Risk: Balance breakage via config edits.

**Quality Risks for v3 Features:**
1. **Character Classes:** No base class for inheritance testing; type casting untested in combat/loot systems. Need polymorphic test patterns.
2. **Shops & Crafting:** New persistent state (inventory qty tracking, recipe manager); no SaveSystem integration tests yet. Need transaction semantics testing.
3. **New Systems:** Player encapsulation must be validated (no public setters); mutable shared state (config, events) risks race conditions in multithreaded logging. Need immutability/isolation tests.

**Recommended v3 Test Strategy:**

**Tier 1 - Foundation (High Risk, Must Test):**
- StatusEffectManager unit tests (all 6 effects, immunity, duration, removal)
- Equipment system unit tests (equip/unequip, stat bonuses, conflict detection)
- SaveSystem integration tests (serialize/deserialize, file safety, data validation)
- AchievementSystem unit tests (all 5 achievement conditions, unlock logic, persistence)

**Tier 2 - Infrastructure (New Systems):**
- Abstract base class pattern tests (for character classes)
- Shop system unit tests (buy/sell logic, inventory qty, gold validation)
- Crafting recipe validation tests (ingredient checks, output generation)
- Config hotload safety tests (no state mutation during reload)

**Tier 3 - Hardening (Edge Cases & Defensive Coding):**
- Boundary tests: negative HP in save, invalid stat values, circular room exits
- Input validation: negative gold, invalid equipment slot indices, recipe name injection
- Concurrency safety: GameEvents thread safety, shared config read/write locks
- Null safety: SaveSystem deserialization failure modes, missing achievements list

**Tier 4 - Integration (v2 Regressions):**
- Dead enemy cleanup (regression from WI-10)
- Flee-and-return combat state persistence
- Status effect interaction during combat (apply → combat → remove cycle)
- Level-up during combat + status effect application ordering

**Test Infrastructure for v3:**
- **Async support:** Xunit + async/await for file I/O tests (SaveSystem)
- **JSON fixtures:** System.Text.Json deserializer safety patterns; corrupt JSON handling
- **Time-based tests:** Duration-based effect removal (turn counter vs wall-clock time)
- **Factory patterns:** CharacterClassBuilder, ShopStateBuilder, RecipeBuilder for test data
- **Config injection:** IGameConfig interface for mocking config values

**Edge Case Inventory (v2 + v3 Risks):**
- Dead enemy cleanup (GP@WI-10)
- Flee-and-return HP persistence (by design, but test it)
- Save file corruption recovery (silent fail vs graceful error)
- Empty shop (no recipes, no items) edge case
- Inventory overflow on pickup (max items, max qty per slot)
- Negative stat calculations (Defense > Attack scenario)
- Status effect duration = 0 at apply time
- Elite enemy spawn without base enemy type defined in config

**Quality Gates (v3):**
- **Build gates:** 80%+ coverage on high-risk systems (StatusEffects, Equipment, SaveSystem, Achievements). No test failures block merge.
- **Manual gates:** Code review for SaveSystem file I/O (handles missing directory, permission errors); Equipment stat bonuses validated against balance spreadsheet; Config hotload tested with live game state.
- **Regression gates:** All WI-10 edge case tests pass; dead enemy cleanup regression test included in CI.
- **Rejection criteria:** Coverage drop, new system without tests, untested edge case causing crash/balance issue, SaveSystem deserialization failure.

**Effort Estimate:**
- StatusEffectManager tests: 4-6 hours (all 6 effects × multiple scenarios)
- SaveSystem tests: 6-8 hours (file I/O, deserialization, validation)
- AchievementSystem tests: 3-4 hours (5 conditions, persistence)
- Equipment system tests: 3-4 hours (equip/unequip, bonuses)
- v3 infrastructure (class hierarchy, shop, crafting): 10-12 hours across phases
- **Total v3 testing work estimate:** 26-34 hours across v3 phases

**Files written:**
- `.ai-team/decisions/inbox/romanoff-v3-planning.md` — test strategy recommendations

### 2026-02-20: Systems/ Pre-v3 Bug Hunt — 7 Bugs Identified

**Scope:** SaveSystem, AchievementSystem, RunStats, config loaders — persistence layer review for state corruption, double-unlock, unset fields, and loading failures.

**Review Method:**
- Static code analysis (no test execution)
- LINQ usage audit (SaveSystem.ListSaves sort order)
- Data flow tracing (RunStats field updates)
- Deserialization safety audit (SaveSystem validation, Player state bounds)
- Edge case analysis (status effects on dead entities, config directory missing)

**Bugs Found (6 valid, 1 retracted):**

**Critical:**
1. **SaveSystem.ListSaves() — OrderByDescending Sort Bug** (SaveSystem.cs:146)
   - `OrderByDescending(File.GetLastWriteTime)` sorts filename strings, not timestamps
   - Impact: Save file list mis-sorted if filenames don't match write-time order alphabetically
   - Fix: Apply `OrderByDescending` to full paths, not filenames

2. **RunStats.DamageDealt/DamageTaken Never Updated** (RunStats.cs:18,21 + CombatEngine.cs)
   - Fields declared but never incremented during combat
   - Impact: "Untouchable" achievement always unlocks (DamageTaken defaults to 0), stat display broken
   - Fix: Inject RunStats into CombatEngine, track damage in PerformPlayerAttack/PerformEnemyTurn

**High:**
3. **SaveSystem.LoadGame() — No Player State Validation** (SaveSystem.cs:79-132)
   - Deserialized Player bypasses encapsulation: HP > MaxHP, negative stats, Level < 1 all allowed
   - Impact: Save corruption/exploit editing breaks combat math, achievement integrity
   - Fix: Validate HP <= MaxHP, stats > 0, Level >= 1 after deserialization

**Medium:**
4. **ItemConfig/EnemyConfig.Load() — No Directory Handling** (ItemConfig.cs:64, EnemyConfig.cs:55)
   - Check `File.Exists()` but don't handle missing parent directory
   - Impact: `DirectoryNotFoundException` instead of clearer `FileNotFoundException` if Data/ missing
   - Fix: Improve error message to mention directory requirement

5. **StatusEffectManager.ProcessTurnStart() — No Dead Entity Check** (StatusEffectManager.cs:46-79)
   - Applies status effects to entities with HP <= 0, shows "effect wore off" on corpses
   - Impact: Cosmetic bug (death messages on dead entities); potential logic error if called post-death
   - Fix: Guard clause to skip processing if target HP <= 0

**Low:**
6. **SaveSystem — Redundant .ToList() on Items** (SaveSystem.cs:105)
   - `Items = roomData.Items.ToList()` creates defensive copy when Items is already a List<Item>
   - Impact: None (defensive copy is actually good practice); micro-optimization opportunity only
   - Fix: None required; defensive copy prevents shared-reference bugs

**Retracted:**
- **AchievementSystem.Evaluate() Double-Unlock** — False alarm. `LoadUnlocked() + savedNames.Contains()` correctly prevents double-unlocking. No bug exists.

**Critical Patterns Identified:**

1. **LINQ Ordering Anti-Pattern:** `collection.OrderByDescending(File.GetLastWriteTime)` when `collection` is strings, not paths. Must pass full path to lambda for file-based sorts.

2. **Deserialization Trust Violation:** Loading JSON directly into domain models without validation bypasses encapsulation. Always validate external data:
   - Bounds checks (HP <= MaxHP)
   - Positive constraints (Level >= 1, MaxHP > 0)
   - Logical invariants (Mana <= MaxMana)

3. **Event-Driven Stat Tracking Gap:** RunStats fields exist but no event hooks to populate them. GameLoop → CombatEngine → RunStats dependency chain broken. Options:
   - **Dependency Injection:** Pass RunStats to CombatEngine (invasive but correct)
   - **Event Subscription:** GameLoop subscribes to damage events (looser coupling, harder to test)
   - **Post-Combat Aggregation:** CombatEngine returns damage summary (requires API change)

4. **Status Effect Lifecycle Management:** StatusEffectManager doesn't check entity liveness before applying effects. Defensive pattern: guard clause at method entry (`if (HP <= 0) return`).

5. **Config Loader Error Handling:** ItemConfig/EnemyConfig don't match SaveSystem's defensive directory handling. SaveSystem creates missing directories; config loaders assume they exist. Inconsistent error UX.

**Blocking Issues for v3:**
- **BUG-4 (RunStats tracking):** Blocks achievement system integrity — "Untouchable" exploit renders achievement meaningless
- **BUG-2 (SaveSystem validation):** Blocks safe save/load — corruption or exploit editing breaks game state
- **BUG-1 (ListSaves sort):** Blocks quality-of-life — "most recent save" UI misleading

**Test Coverage Gaps Exposed:**
- No SaveSystem round-trip validation tests (corrupt JSON → InvalidDataException)
- No RunStats integration tests (combat → verify damage counters incremented)
- No SaveSystem.ListSaves() temporal ordering tests
- No StatusEffectManager death-check tests (poison damage on 1 HP entity)
- No config loader error message tests (missing directory vs missing file)

**Files Written:**
- `.ai-team/decisions/inbox/romanoff-systems-bugs.md` — comprehensive bug report with severity, reproduction, fix recommendations

**Key File Paths (Systems/):**
- `SaveSystem.cs` — JSON save/load, room graph serialization, LINQ sort bug
- `AchievementSystem.cs` — Achievement unlock logic (CORRECT, no bugs found)
- `RunStats.cs` — Stat tracking fields (DamageDealt/Taken never updated)
- `ItemConfig.cs`, `EnemyConfig.cs` — Config loaders (directory handling weak)
- `StatusEffectManager.cs` — Status effect tick processing (no dead-entity guard)
- `GameEvents.cs` — Event hub (no events for damage tracking)

**Architecture Observation:**
- **Persistence Layer Weak Points:** SaveSystem has no validation layer; trusts deserialized data implicitly. Missing "hydration validation" step between JSON → Model.
- **Telemetry Gap:** RunStats declared as data DTO but no instrumentation to populate it. Event-driven architecture (GameEvents) exists but underutilized for analytics.
- **Config vs Save Asymmetry:** SaveSystem defensively creates directories; config loaders assume existence. Inconsistent defensive coding standards.

### 2026-02-20: Pre-v3 Bug Hunt Session — Systems Quality Findings

📌 **Team update (2026-02-20):** Pre-v3 comprehensive bug hunt identified 47 critical issues. Systems audit found 7 critical issues in SaveSystem, RunStats, AchievementSystem, and StatusEffectManager:

**Critical Blockers (must fix before v3):**
1. **SaveSystem.ListSaves() Sort Error (CRITICAL):** OrderByDescending sorts filename strings, not timestamps — save list shows wrong "most recent" order
2. **SaveSystem Validation Missing (HIGH):** LoadGame() deserializes without validation — HP can exceed MaxHP, stats go negative, achievement exploits possible (Glass Cannon)
3. **RunStats Damage Tracking (CRITICAL):** DamageDealt/DamageTaken fields never incremented — "Untouchable" achievement always unlocks because DamageTaken defaults to 0, stat display shows zeros

**Medium Issues:**
- Config loaders missing directory error handling
- StatusEffectManager processes effects on dead entities (cosmetic but defensive coding violation)

**Recommended Fixes:** Inject RunStats into CombatEngine (fix damage tracking), add LoadGame() validation (prevent save corruption), correct OrderByDescending usage (UI quality-of-life).

— decided by Romanoff (from Systems Pre-v3 Bug Hunt)

### 2026-02-20: Tests for Issues #220 and #221

**Task:** Write tests for two follow-up issues from PR #218 ahead of Barton/Hill's fix merges.

**Deliverables:**
- Branch: `squad/220-221-tests`
- PR #225: test: edge cases for ColorizeDamage and equipment comparison alignment

**New Test Files:**
- `Dungnz.Tests/ColorizeDamageTests.cs` — 2 tests for `ColorizeDamage()` (private, tested via `FakeDisplayService.RawCombatMessages`)
- `Dungnz.Tests/ShowEquipmentComparisonAlignmentTests.cs` — 2 tests for `ConsoleDisplayService.ShowEquipmentComparison()` (Console captured via `IDisposable` pattern)

**Infrastructure Change:**
- Added `RawCombatMessages` list to `FakeDisplayService` — stores un-stripped (ANSI-intact) combat messages alongside the existing stripped `CombatMessages` list. Additive change, no existing tests affected.

**Test Results at time of commit:**
- `ColorizeDamage` tests: 2/2 PASS — Barton's fix (PR #220, `ReplaceLastOccurrence` via `LastIndexOf`) is already merged
- `ShowEquipmentComparison` alignment tests: 0/2 PASS (expected) — Hill's fix for item-name-row padding (Current/New lines are 40 chars, should be 41) is NOT yet merged. Tests will auto-pass once that PR lands.

**Discoveries Made:**
1. Barton's ColorizeDamage fix was already in the codebase — `ReplaceLastOccurrence` using `LastIndexOf` already present in `Engine/CombatEngine.cs`. Tests confirm and lock in the behavior.
2. The alignment bug in `ShowEquipmentComparison` was more nuanced than described:
   - The **stat rows** (Attack/Defense) were ALREADY correctly padded using `StripAnsiCodes`-aware dynamic padding — Hill's fix for those is already merged too
   - The **item name rows** (Current/New) use `{name,-27}` with a fixed 1-char shortfall — these render as 40-char lines against a 41-char border
   - My tests correctly detect the item-name row misalignment and will pass once that's fixed

**Key Pattern:** `FakeDisplayService.RawCombatMessages` is now available for any future tests that need to inspect ANSI formatting rather than plain text.

### 2026-02-22: Intro QA Assessment & Testability Planning

**Task:** Assess intro improvements from quality and testability standpoint. Review Program.cs intro logic, evaluate testing implications, identify edge cases, recommend architecture.

**Findings:**

**Critical Issues (Current Architecture):**
1. **Program.cs intro logic is untestable** — All game startup (lines 7-82) is top-level script with hardcoded `Console.ReadLine()` calls. Cannot provide test input or validate behavior without console I/O.
2. **No input validation** — Empty names, invalid difficulty/class selections silently fall back to defaults without error messages. Edge cases unknown (special chars, whitespace, overflow).
3. **Prestige loading has no error handling** — Side effect loaded globally; if prestige save corrupted, crash with no recovery.
4. **Seed generation unrecorded** — Random seed generated but not captured for verification; hard to test determinism contracts.
5. **Player creation is inline** — Stat modifications (class bonuses, prestige bonuses) done at script level; cannot test player creation in isolation.

**Edge Cases Identified (23 edge cases across 4 input types):**
- **Player name:** Empty, whitespace-only, 100+ chars, newlines/null chars
- **Difficulty:** Empty input, invalid selections (4+), negative values
- **Class:** Empty input, invalid selections (4+), floating-point input
- **Seed:** Overflow (int.MaxValue), negative seeds, very large numbers, non-numeric input
- **Player creation:** Class bonus making stat negative, prestige loading failures, MaxHP clamping edge cases

**Testing Infrastructure Assessment:**
- ✅ **FakeDisplayService** exists (Helpers/) — tracks all output in lists; can be reused for intro testing
- ✅ **Console capture pattern** documented (DisplayServiceTests.cs) — IDisposable + StringWriter for console output validation
- ✅ **Test pattern established** — Arrange-Act-Assert, FluentAssertions, Theory+InlineData for parameterized tests
- ⚠️ **FakeInputReader** — Must enhance or create to queue test input for intro sequence

**Architectural Recommendation:**

**Extract `IntroSequence` class** (testable orchestrator):
- Constructor: Inject `IDisplayService`, `IInputReader`, `IPrestigeProvider`
- Public method `Run(out int seed) → Player` — Orchestrates all intro steps, returns initialized player
- Private methods: `ValidateName()`, `SelectSeed()`, `SelectDifficulty()`, `SelectClass()`, `CreatePlayer()`
- Enables: Unit testing of each step independently, FakeDisplayService mocking, test input injection, validation testing

**New IDisplayService Methods (testability):**
- Add return-value methods: `string SelectSeed()`, `Difficulty SelectDifficulty()`, `PlayerClass SelectClass()`
- Separates "show menu" (void) from "get valid choice" (return)
- Decouples display testing from logic testing

**Recommended Test Coverage (Tier 1 = must-write):**
- 24+ test cases: name validation (4), seed selection (4), difficulty selection (4), class selection (4), player creation (5), integration (3)
- Theory+InlineData for parameterized edge cases
- Integration test: full intro flow with fake input → verify player returned with correct stats

**Quality Gate Recommendations:**
- BLOCKING: All Tier 1 tests pass (24 cases), no Console.ReadLine in intro logic, FakeDisplayService captures all output
- MERGE GATE: Tier 2 edge cases covered, silent fallbacks documented, player bounds-checking verified
- REJECT: Test failures, prestige load crash, stat validation skipped, seed overflow unhandled

**Timeline Impact:**
- Refactoring IntroSequence: 2-3 hours (Hill/Barton)
- Writing Tier 1 tests: 4-6 hours (Romanoff)
- Code review + final tests: 1 hour
- **Total: 7-10 hours** before new intro features can be safely added

**Files for Creation:**
- `Engine/IntroSequence.cs` — Orchestrator class (new)
- `Dungnz.Tests/IntroSequenceTests.cs` — Test harness (24+ tests)
- Update `Display/IDisplayService.cs` — Add 3 return-value methods
- Update `Display/ConsoleDisplayService.cs` — Implement new methods
- Refactor `Program.cs` (lines 7-82) — Replace with `new IntroSequence().Run()`

**Key Learning:**
**Top-level script is the enemy of testability.** Any complex logic (selection menus, validation, player creation) must be extracted to a class with injected dependencies. This is not over-engineering; it's the minimum required for regression safety as the intro grows.

**Documentation:** `.ai-team/decisions/inbox/romanoff-intro-qa-notes.md` (21KB comprehensive assessment with edge case inventory, test matrix, and architecture diagrams)

---

## 2026-02-22: Team Decision Merge

📌 **Team update:** Intro QA strategy, edge case testing framework, and quality assurance patterns — decided by Romanoff (via comprehensive intro QA assessment). Decisions merged into `.ai-team/decisions.md` by Scribe.

📌 Team update (2026-02-22): Process alignment protocol established — all code changes require a branch and PR before any commits. See decisions.md for full protocol. No exceptions.

---

### 2026-02-22: Phase 2.1–2.4 Proactive Tests — TierDisplayTests.cs

**Task:** Write proactive tests for Phase 2.1–2.4 (loot-display-phase2 branch) before Hill's code lands.

**File Written:** `Dungnz.Tests/TierDisplayTests.cs`

**Test Count:** 16 tests total (11 pass on master today; 5 intentionally fail pending Phase 2.1/2.3)

**Tests by category:**

| Test | Status on master | Requires |
|---|---|---|
| `ShowLootDrop_CommonItem_DoesNotContainBrightCyan` | ✅ PASS | baseline |
| `ShowLootDrop_CommonItem_ItemNameNotPrecededByGreen` | ✅ PASS | baseline |
| `ShowLootDrop_UncommonItem_ItemNamePrecededByGreen` | ❌ FAIL | Phase 2.1 |
| `ShowLootDrop_RareItem_OutputContainsBrightCyan` | ❌ FAIL | Phase 2.1 |
| `ShowLootDrop_RareItem_ItemNamePrecededByBrightCyan` | ❌ FAIL | Phase 2.1 |
| `ShowInventory_CommonItem_NoBrightCyanInOutput` | ✅ PASS | baseline |
| `ShowInventory_RareItem_OutputContainsBrightCyan` | ❌ FAIL | Phase 2.1/2.3 |
| `ShowInventory_UncommonItem_ItemNamePrecededByGreen` | ❌ FAIL | Phase 2.1/2.3 |
| `FakeDisplayService_ShowLootDrop_RareItem_RecordsItemName` | ✅ PASS | baseline |
| `FakeDisplayService_ShowInventory_RareItem_RecordsInventoryCount` | ✅ PASS | baseline |
| `ShowLootDrop_EmptyItemName_DoesNotThrow` | ✅ PASS | edge case |
| `ShowLootDrop_NullItemName_DoesNotThrow` | ✅ PASS | edge case |
| `ShowInventory_EmptyInventory_DoesNotThrow` | ✅ PASS | edge case |
| `ShowLootDrop_AllTiers_DoNotThrow` (Theory ×3) | ✅ PASS | edge case |

**Phase 2.2 (ShowShop) and Phase 2.4 (ShowCraftRecipe):** Fully written but commented out — methods not yet on `IDisplayService`. Uncomment and adjust signatures when Hill's interface changes land.

**Key Technical Notes:**
- `BrightCyan` is not yet in `ColorCodes.cs`. Tests use local const `BrightCyanAnsi = "\u001b[96m"` (standard ANSI bright cyan). Once Hill adds `ColorCodes.BrightCyan`, update the constant to reference it.
- Tests check `{color}{itemName}` pattern specifically (not just "output contains green") to distinguish tier color from other greens (equipped [E] tag, stat values, etc.)
- `FakeDisplayService` strips ANSI — useless for color assertions. All ANSI checks must use `ConsoleDisplayService` with `Console.SetOut(StringWriter)` capture pattern.

## Learnings

### Test patterns for color/tier behavior

1. **Use ConsoleDisplayService + Console capture for ANSI assertions** — `FakeDisplayService` strips all ANSI codes before recording. For any test that needs to assert color codes (e.g., tier color wrapping item names), redirect `Console.Out` to a `StringWriter` and use `new ConsoleDisplayService()` directly. Use `[Collection("console-output")]` to prevent parallel runs from competing on stdout.

2. **Test the specific `{color}{itemName}` pattern, not just `{color}` presence** — Many existing display methods already emit `ColorCodes.Green` for equipped tags, health values, etc. Asserting `output.Contains(ColorCodes.Green)` is a weak signal. Asserting `output.Contains($"{ColorCodes.Green}ItemName")` confirms the colorization is specifically wrapping the intended content.

3. **Define a local constant for not-yet-added color codes** — When a color code doesn't exist yet in `ColorCodes.cs`, use a local `private const string BrightCyanAnsi = "\u001b[96m"` in the test class. Add a comment to replace with `ColorCodes.BrightCyan` once Hill adds it. This lets tests compile and run without waiting for the constant to be defined.

4. **Proactive test failure count = Phase gate signal** — The 5 failing tests in `TierDisplayTests.cs` are an exact checklist for Phase 2.1/2.3. When Hill's PR is reviewed, running `dotnet test --filter TierDisplay` should show exactly those 5 transitioning to green. If more tests fail, something regressed; if fewer, the implementation is incomplete.

5. **Comment-in pattern for interface-gated tests** — When tests depend on interface methods that don't exist yet (ShowShop, ShowCraftRecipe), wrap the entire test class in `/* ... */` with a header comment `// Requires Phase 2.x: MethodName`. This preserves the test logic in version control without breaking compilation. Add a TODO listing the expected method signature so Hill can align implementation.


### 2026-02-20: Phase 3 Proactive Tests — Looting UX Polish

**Task:** Write proactive tests targeting Phase 3 features before Hill's PR lands on feature/loot-display-phase3.
**File:** `Dungnz.Tests/Phase3LootPolishTests.cs` (17 tests, all passing)

**Branch status:** feature/loot-display-phase3 did not exist when task began. Hill had already merged Phase 3 production code (ConsoleDisplayService, IDisplayService) into the current branch. The test project had pre-existing build failures from the signature change; fixed TierDisplayTests.cs:390 (FluentAssertions named-arg error) so the project compiled cleanly.

**Tests written (by section):**

**3.1 Consumable Grouping (4 tests):**
- `ShowInventory_ThreeIdenticalPotions_ShowsTimesThreeMultiplier` — 3 same-name items → output contains `×3`
- `ShowInventory_DifferentNamedItems_StaySeparate` — different names don't group, no `×2`
- `ShowInventory_SinglePotion_ShowsNoMultiplier` — single item, no `×` badge
- `ShowInventory_EmptyInventory_ShowsNoGroupingArtifacts` — empty inventory, no stray `×`, shows "(empty)"

**3.2 Elite Loot Callout (5 tests):**
- `ShowLootDrop_IsEliteTrue_OutputContainsEliteLootDrop` — isElite:true → "ELITE LOOT DROP"
- `ShowLootDrop_IsEliteFalse_OutputContainsLootDropButNotElite` — isElite:false → "LOOT DROP", no "ELITE"
- `ShowLootDrop_UncommonItem_OutputContainsUncommonBadge` — tier badge shows "Uncommon"
- `ShowLootDrop_RareItem_OutputContainsRareBadge` — tier badge shows "Rare"
- `ShowLootDrop_CommonItem_OutputContainsCommonBadge` — tier badge shows "Common"

**3.3 Weight Warning (4 tests):**
- `ShowItemPickup_At85PercentWeight_ShowsWeightWarning` — 42/50 weight → ⚠ + "nearly full"
- `ShowItemPickup_At79PercentWeight_ShowsNoWeightWarning` — 39/50 weight → no ⚠
- `ShowItemPickup_AtExactly80PercentWeight_ShowsWeightWarning` — 40/50 weight (exactly 80%) → no ⚠ (strict `>` boundary, not `>=`)
- `ShowItemPickup_AtJustOver80PercentWeight_ShowsWeightWarning` — 41/50 weight (82%) → ⚠

**3.4 New Best Indicator (4 tests):**
- `ShowLootDrop_NewWeaponBetterThanEquipped_ShowsPositiveDelta` — Attack +5 drop vs +2 equipped → "+3 vs equipped"
- `ShowLootDrop_NewWeaponSameAsEquipped_ShowsNoVsEquipped` — Attack +5 drop vs +5 equipped → no "vs equipped"
- `ShowLootDrop_NewWeaponWeakerThanEquipped_ShowsNoVsEquipped` — Attack +3 drop vs +5 equipped → no "vs equipped"
- `ShowLootDrop_PlayerHasNoWeaponEquipped_ShowsNoVsEquipped` — no weapon equipped → no "vs equipped"

**Key implementation observations:**
- `ShowInventory` groups by `GroupBy(i => i.Name)`, shows `×{count}` only when count > 1
- `ShowLootDrop(Item, Player, bool isElite)` already in production; uses `ColorCodes.Yellow` for elite header, tier switch for badge
- `ShowItemPickup` uses strict `>` (not `>=`) for the 80% weight boundary — exactly 80% does NOT trigger warning
- "vs equipped" indicator: only when `AttackBonus > 0 && EquippedWeapon != null && delta > 0`

**Patterns learned:**
- Boundary test important: the `>0.8` vs `>=0.8` distinction at exactly-80% is a common off-by-one risk; documented both sides in tests
- `EquippedWeapon` can be set directly (`player.EquippedWeapon = equippedSword`) without going through `Equip()` — valid for unit tests
- All 17 tests pass against existing production code with no mocking needed

---

## Issue #316 — ShowEnemyArt Display and Combat Integration Tests

**Branch:** `squad/316-ascii-art-tests`  
**PR:** #321  
**File:** `Dungnz.Tests/Display/ShowEnemyArtTests.cs`

### Tests written (4 total)

1. **`ShowEnemyArt_EmptyAsciiArt_DoesNotAddEntryToAllOutput`** — `AsciiArt = []` → no `"enemy_art:"` entry in `AllOutput`
2. **`ShowEnemyArt_WithAsciiArt_AddsJoinedEntryToAllOutput`** — `AsciiArt = ["line1", "line2"]` → `"enemy_art:line1|line2"` in `AllOutput`
3. **`AllEnemyArtLines_AreAtMost34CharactersLong`** — loads `Data/enemy-stats.json` via `EnemyConfig.Load`, checks every art line ≤ 34 chars across all 23 enemies
4. **`CombatEngine_RunCombat_CallsShowEnemyArt`** — integration test: `RunCombat` triggers `ShowEnemyArt`, confirmed via `FakeDisplayService.AllOutput` containing `"enemy_art:"` prefix

### Learnings

- **`AsciiArt` has `protected set`** on `Enemy` base class — cannot set from outside. Solution: define a private inner `ArtEnemy : Enemy` test subclass that accepts art lines in its constructor and assigns `AsciiArt` directly (works because it's a derived class).
- **`Enemy_Stub` is defined locally** inside `CombatEngineTests.cs`, not in `Helpers/`. Each test file needing a concrete enemy should define its own local subclass.
- **JSON path pattern** for `EnemyConfig.Load` in tests: `Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "enemy-stats.json")` — same as `EnemyFactoryFixture`.
- **`FakeDisplayService.ShowEnemyArt`** already guards empty art with a null/length check, so the empty-art test naturally passes without any changes to production code.
- **Result:** 427 existing + 4 new = 431 tests, all passing.

### 2026-02-24: PR #366 Phase 6 Class Ability Test Audit

**PR:** #366 `squad/class-abilities` → 845 lines, 57 tests in `Phase6ClassAbilityTests.cs`, refactored `AbilityManagerTests.cs`

**Scope:** Review test quality for WI-22 through WI-27 (class restrictions, warrior/mage/rogue abilities, passives, integration tests)

**Quality Assessment:**
- ✅ **AAA Structure:** All tests follow Arrange-Act-Assert pattern consistently
- ✅ **Edge Cases Covered:**
  - Class restriction filtering (wrong class cannot see other classes' abilities)
  - HP preservation gates (RecklessBlow/ArcaneSacrifice preserve min 1 HP)
  - Combo point cap (5 max) and requirements (Flurry ≥1 CP, Assassinate ≥3 CP)
  - Execute thresholds (Meteor <20%, Assassinate ≤30%) with boss immunity via `IsImmuneToEffects`
  - Last Stand HP gate (fails >40%, succeeds ≤40% with mana refund on failure)
  - Conditional damage (Backstab 1.5x base, 2.5x when enemy has Slow/Stun/Bleed)
  - Fortify heal gate (≤50% HP heals, >50% no heal)
  - ManaShield toggle on/off
  - Mana refunds on failed abilities (Last Stand, Flurry, Assassinate)
  - Passive skill class restrictions (3 tests: warrior/mage/rogue cannot unlock other classes' passives)
- ✅ **No Trivial Tests:** All assertions verify real behavior with meaningful test data
- ✅ **Integration Tests:** 3 full combat flows (Warrior ShieldBash, Mage ArcaneBolt, Rogue combo chain to Assassinate)

**AbilityManagerTests Refactor:**
- Updated from generic Phase 1-5 abilities (PowerStrike/DefensiveStance/PoisonDart/SecondWind) to Warrior-specific Phase 6 abilities (ShieldBash/BattleCry/Fortify/RecklessBlow/LastStand)
- Coverage NOT weakened — same test patterns applied to new ability system
- All tests updated to use `PlayerClass.Warrior` and class-specific abilities

**Missing Coverage:** None identified for Phase 6 scope. All WI-22 through WI-27 requirements covered.

**Verdict:** APPROVED — no concerns, recommend merge

**Test Count:** 431 existing + 57 new Phase 6 tests = 488 tests
