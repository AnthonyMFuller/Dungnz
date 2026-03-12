# Romanoff — History (Recent Activity)

**Full archive:** `history-archive-2026-03-11.md`

---

## Compressed Index — February 2026

- 2026-02-20: WI-10 code review (feature-complete quality pass)
- 2026-02-20: v2 testing strategy proposal; retrospective ceremony
- 2026-02-20: v3 quality planning — test coverage gaps & strategy
- 2026-02-20: Pre-v3 bug hunt — 7 bugs identified in Systems/
- 2026-02-22: Intro QA assessment & testability planning
- 2026-02-22: Phase 2.1–2.4 proactive tests — TierDisplayTests.cs
- 2026-02-22: Phase 3 proactive tests — looting UX polish
- 2026-02-24: PR #366 phase 6 class ability test audit
- 2026-02-24: ShowEnemyArt display and combat integration tests
- 2026-02-27: Deep bug hunt session; sell fix PR opened
- 2026-02-28: PR review — bug hunt fix session (#625, #626)
- 2026-02-28: Coverage uplift to 80%; CraftingMaterial regression tests (#671)
- 2026-02-28: Difficulty balance test review

## Compressed Index — March 2026 (Early)

- 2026-03-01: TakeCommandTests — TAKE command enhancements
- 2026-03-01: Fix CryptPriest heal timing test (PR #752)
- 2026-03-02: Inspect & Compare feature tests (squad/846-inspect-compare-tests)
- 2026-03-03: HelpDisplayRegressionTests for HELP markup crash prevention (#870, PR #886)
- 2026-03-03: DisplayServiceSmokeTests for markup rendering (#875)
- 2026-03-03: Comprehensive test coverage gap analysis
- 2026-03-03: Batch test coverage for 6 issues (#944, #947, #948, #949, #950, #943)
- 2026-03-04: Deep test coverage gap analysis (beyond filed issues)
- 2026-03-04: Merchant sell/shop flow test coverage gap analysis (BUGs A–D)
- 2026-03-06: Deep TUI code audit (Spectre.Console display implementation)
- 2026-03-06: Full display layer bug audit — P0 confirmed + 17 additional bugs
- 2026-03-06: Merchant menu bug fix tests (#1157, #1158, #1156, #1159)
- 2026-03-06: PR review session — 4 PRs from bug hunt sprint
- 2026-03-08: Combat baseline tests (#1273)
- 2026-03-09: PR review session — cleanup & display fixes
- 2026-03-10: Edge case coverage batch (#1233, #1239, #1243, #1248, #1249, #1251)
- 2026-03-10: Momentum resource test coverage (WI-F, #1274)
- 2026-03-10: Momentum system PR review and merge (#1293, #1294, #1295 / #1274)

---

## 2026-03-11 — PR #1340 Review / #1345 Merge (Issues #1336, #1337)

PR #1340 (`squad/1336-bracket-markup-sweep`) was contaminated by an older version of Hill's
FinalFloor commit. GitHub reported `mergeable: CONFLICTING` — squash self-heal did not apply.
Romanoff extracted the unique content (Barton's history additions + decision inbox file),
corrected a spec inaccuracy (Map panel height ~5 → ~8 lines per `LayoutConstants.MapPanelHeight`),
created `squad/1340-clean-docs-sweep` from master HEAD, and merged #1345.

**Process rule established:** Verify `gh pr view --json mergeable,mergeStateStatus` before
assuming squash self-heal. If `CONFLICTING`, extract unique content to a clean branch.

Build: ✅ | Tests: 1909 passing, 0 failed.  
(see decisions.md: "PR #1340 Review — Branch Contamination Handling")

---

## 2026-03-11 — PR #1344 — PanelHeightRegressionTests Review and Merge (Issue #1333)

Reviewed and merged `squad/1333-panel-height-regression-tests` — final retro action item.
4 tests in `Dungnz.Tests/Display/PanelHeightRegressionTests.cs` via `InternalsVisibleTo` seam
calling `BuildPlayerStatsPanelMarkup` directly. All height assertions reference
`LayoutConstants.StatsPanelHeight`. GearPanel test deferred as `// TODO:` only.
All 9 retro action items now complete.

**Outstanding follow-up:** Hill to extract `BuildGearPanelMarkup` as `internal static` →
Romanoff/Barton to add `GearPanelLineCount_IsWithinGearPanelHeight` in a follow-up PR.

Build: ✅ | Tests: 1913 passing, 0 failed.  
(see decisions.md: "PR #1344 — PanelHeightRegressionTests Approved and Merged")

---

## 2026-03-11 — Integration Test Expansion (Issue #1383, PR #1391)

Expanded integration test coverage from ~37 to 119+ scenarios. Added 7 new test class files
to `Dungnz.Tests/`:

| File | Tests |
|------|-------|
| `LootPipelineIntegrationTests.cs` | 13 |
| `SetBonusIntegrationTests.cs` | 13 |
| `SaveLoadIntegrationTests.cs` | 10 |
| `StatusEffectLifecycleIntegrationTests.cs` | 10 |
| `ShopShrineIntegrationTests.cs` | 12 |
| `CombatScenarioIntegrationTests.cs` | 12 |
| `NavigationInventoryIntegrationTests.cs` | 12 |

Also added `CombatColors.cs` (sourced from commit `9e49e96`) to fix a pre-existing build break
in `SpectreLayoutDisplayService.cs` introduced by the `squad/1375-enemy-ai` branch.

**Branch chaos encountered:** Multiple branches on same commit + stash corruption by concurrent
sessions. Recovered via Python file writes + immediate commit + `git reflog`.

Suite result: 1913 → 1995 passing, 0 failed, 4 skipped.
Branch: `squad/1383-integration-test-expansion` | PR #1391 — merged ✅
