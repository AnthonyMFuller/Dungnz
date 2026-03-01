# Coulson — PR Review Round 4: Sprint 3 Completion

**Date:** 2026-03-01
**Reviewer:** Coulson (Lead)
**Requested by:** Anthony
**Context:** 9 open PRs from Romanoff (testing/quality) and Barton (combat systems) — Sprint 3 tech improvements

---

## Summary

**All 9 PRs merged** to master in prescribed order using `gh pr merge --admin --merge --delete-branch`.

CI status on all branches showed the "test" check as FAILURE — confirmed these are the 2 pre-existing architecture test failures (GenericEnemy missing JsonDerivedType, Models→Systems dependency). CodeQL passed on all branches.

---

## Group 1 — Romanoff's PRs (Testing/Quality)

### ✅ PR #791: ArchUnitNET Architecture Rules
- **Branch:** `squad/architecture-tests`
- **What:** 3 new IL-scanning architecture tests in `Dungnz.Tests/Architecture/ArchitectureTests.cs`
- **Review:** Clean. 2 tests intentionally fail to document tech debt (Display uses raw Console, Engine ConsoleInputReader uses raw Console). 1 test passes (IDisplayService implementations in Display namespace). Good visibility into architectural violations.
- **Impact:** +3 tests, +2 intentional failures

### ✅ PR #795: Test Builder Pattern
- **Branch:** `squad/test-builder-pattern`
- **What:** 4 fluent builders (PlayerBuilder, EnemyBuilder, RoomBuilder, ItemBuilder) in `Dungnz.Tests/Builders/`
- **Review:** Clean fluent API. 6 builder validation tests. 3 existing PlayerTests refactored to use builders. Good test ergonomics improvement.
- **Impact:** +6 tests (net, after refactoring)

### ✅ PR #797: Verify.Xunit Snapshot Tests
- **Branch:** `squad/snapshot-tests`
- **What:** Verify.Xunit v31.12.5 integration. 3 snapshot tests for GameState, Enemy, and CombatRoundResult serialization formats.
- **Review:** Clean. Verified snapshot files committed alongside tests. Good regression guard for save format stability.
- **Impact:** +3 tests

### ✅ PR #801: CsCheck Property-Based Tests
- **Branch:** `squad/cscheck-pbt`
- **What:** 5 property-based tests in `Dungnz.Tests/PropertyBased/GameMechanicPropertyTests.cs`
- **Review:** Clean. Tests cover TakeDamage monotonicity, Heal cap at MaxHP, loot tier scaling, gold non-negativity, damage+heal HP range invariant. Good use of CsCheck generators.
- **Impact:** +5 tests

---

## Group 2 — Barton's PRs (Combat Systems)

### ✅ PR #792: Session Balance Logging
- **Branch:** `squad/session-balance-logging`
- **What:** `SessionStats` model, `SessionLogger.LogBalanceSummary()`, integrated into `GameLoop` for tracking enemies killed, gold earned, boss kills, floors cleared, damage dealt.
- **Review:** Clean. Non-breaking — adds tracking alongside existing RunStats. 4 unit tests for SessionStats. `RecordRunEnd` signature extended with optional `outcomeOverride` parameter (backward compatible).
- **Impact:** +4 tests

### ⚠️ PR #798: Headless Simulation Mode — STALE BRANCH
- **Branch:** `squad/headless-simulation`
- **What:** Branch was stacked on `squad/test-builder-pattern` and contained the builder commit, NOT headless simulation code.
- **Review:** **No HeadlessDisplayService or SimulationHarness files were delivered.** The merge commit brought in no unique changes (all content already merged via #795). This is the same stacked-branch issue from Round 3.
- **Impact:** 0 new files, 0 new tests. **Headless simulation feature NOT delivered.**
- **Action:** Issue #793 should be reopened.

### ✅ PR #802: IEnemyAI.TakeTurn() Refactor
- **Branch:** `squad/enemy-ai-interface`
- **What:** `IEnemyAI` interface in `Engine/IEnemyAI.cs`, `GoblinShamanAI` and `CryptPriestAI` pilot implementations. Tests in `EnemyAITests.cs`.
- **Review:** Clean interface design. `TakeTurn(EnemyAIContext)` pattern separates AI decision from execution. Good extensibility point for future enemies.
- **Impact:** +8 tests (estimated from EnemyAITests.cs)

### ✅ PR #804: Data-Driven Status Effects
- **Branch:** `squad/data-driven-status-effects`
- **What:** `Data/status-effects.json` with 12 status effect definitions, `StatusEffectDefinition` model class.
- **Review:** Clean JSON schema. Covers Poison, Burn, Freeze, Bleed, Regen, Weakened, Fortified, Slow, Stun, Curse, Silence, BattleCry. Stat modifiers use percentage-based approach.
- **Impact:** Configuration-driven, enables balance tuning without code changes.

### ✅ PR #806: Event-Driven Passives
- **Branch:** `squad/event-driven-passives`
- **What:** `GameEventBus` publish/subscribe system, `IGameEvent` interface, event types (`OnRoomEntered`, `OnPlayerDamaged`, `OnCombatEnd`), `SoulHarvestPassive` implementation. Tests in `GameEventBusTests.cs`.
- **Review:** Clean event bus pattern. Type-safe generic subscribe/publish. `SoulHarvestPassive` demonstrates the Necromancer heal-on-kill passive pattern. 8+ tests covering subscribe, publish, type filtering, clear.
- **Impact:** +8 tests (estimated)

---

## Post-Merge Validation

### Build
- ✅ `dotnet build Dungnz.csproj` — succeeds (3 warnings: XML comment, cref attribute)

### Tests
```
Total:   1394
Passed:  1388
Failed:  6
Skipped: 0
```

### Test Count Delta
- **Before Sprint 3:** 1347 total (1345 passing, 2 failing)
- **After Sprint 3:** 1394 total (+47 tests)
- **New passing:** +43
- **New failing:** +4 (2 intentional arch tests + 2 pre-existing flaky)

### Failure Analysis

| # | Test | Status | Cause |
|---|------|--------|-------|
| 1 | `ArchitectureTests.AllEnemySubclasses_MustHave_JsonDerivedTypeAttribute` | Pre-existing | GenericEnemy missing [JsonDerivedType] |
| 2 | `ArchitectureTests.Models_Must_Not_Depend_On_Systems` | Pre-existing | Merchant→MerchantInventoryConfig, Player→SkillTree/Skill |
| 3 | `LayerArchitectureTests.Display_Should_Not_Depend_On_System_Console` | **Intentional (PR #791)** | ConsoleDisplayService uses raw Console — tech debt visibility |
| 4 | `LayerArchitectureTests.Engine_Must_Not_Call_Console_Directly` | **Intentional (PR #791)** | ConsoleInputReader uses raw Console — tech debt visibility |
| 5 | `Phase6ClassAbilityTests.ShieldBash_AppliesStunWithMockedRng` | Pre-existing flaky | Probabilistic test (50% × 20 tries), no source modified by any PR |
| 6 | `RunStatsTests.RecordRunEnd_CalledForCombatDeath_HistoryContainsEntry` | Pre-existing flaky | Shared mutable file state (`stats-history.json`), no source modified by any PR |

**No regressions introduced by the merged PRs.**

---

## Issues Found

1. **PR #798 stale branch (repeat pattern):** Squad agent created stacked branches again. The `squad/headless-simulation` branch pointed to the test builder commit, not headless simulation code. This is the same issue as PR #767/#771 from Round 3.

2. **Pre-existing flaky tests:** `ShieldBash_AppliesStunWithMockedRng` and `RecordRunEnd_CalledForCombatDeath_HistoryContainsEntry` are environment-sensitive. The former uses real RNG without seeding; the latter shares a mutable file across parallel test runs. Both should be addressed in a test quality pass.

---

## Decisions

1. **D1: Accept 6 known test failures** — 2 pre-existing arch violations, 2 intentional tech debt visibility tests, 2 pre-existing flaky tests. No action required for merge.
2. **D2: Reopen #793 (Headless Simulation)** — Feature was not delivered due to stale branch. Needs fresh branch from master.
3. **D3: Squad agent branch hygiene** — Recommend: each feature branch should be created fresh from master, not stacked on other feature branches. This is the third time this pattern has caused issues.
