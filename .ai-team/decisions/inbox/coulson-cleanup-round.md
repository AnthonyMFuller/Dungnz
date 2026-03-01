# Coulson — Triage + Dependabot Cleanup Round

**Date:** 2026-03-01
**Agent:** Coulson (Lead)
**Requested by:** Anthony

## Issues Triaged

### Issue #755: HP Encapsulation
**Status:** Already closed. Resolved by PR #789 (merged 2026-03-01) which completed HP encapsulation — private setter, `SetHPDirect` for internal mutations, `[JsonInclude]` for serialization.

### Issue #766: Stryker Manifest
**Status:** Already closed. Resolved by PR #785 (merged 2026-03-01) which added `.config/dotnet-tools.json` to pin Stryker version.

### Issue #745: ANSI Escape Codes in ShowMessage
**Status:** Closed. Bug already fixed in codebase — `Engine/GameLoop.cs` lines 549 and 578 now use `_display.ShowError()` instead of embedding raw `ColorCodes.Red`/`ColorCodes.Reset` in `ShowMessage()`.

## Dependabot PRs — Decisions

### D1: Merge xunit.runner.visualstudio 3.1.5 (#788)
**Decision:** Merged. Minor patch bump, no breaking changes.

### D2: Merge Microsoft.NET.Test.Sdk 18.3.0 (#787)
**Decision:** Merged. Major version bump but builds cleanly and all tests pass (same pre-existing failures as master). No API breakage observed.

### D3: Close FluentAssertions 8.8.0 (#786)
**Decision:** Closed without merge. FluentAssertions v6→v8 is a major version bump with significant breaking API changes (assertion syntax changes, removed methods, `BeEquivalentTo` behavior changes). Test suite uses FluentAssertions extensively — this needs a dedicated migration effort, not a Dependabot auto-merge.

**Action Item:** Open issue to track FluentAssertions v8 migration as planned work.

### D4: Merge actions/setup-dotnet v5 (#784)
**Decision:** Merged after rebase. GitHub Actions workflow change only; no code impact.

### D5: Merge actions/github-script v8 (#783)
**Decision:** Merged after rebase. GitHub Actions workflow change only; no code impact.

### D6: Merge actions/upload-artifact v7 (#782)
**Decision:** Merged after rebase. GitHub Actions workflow change only; no code impact.

## Pre-existing Test Failures (Not Addressed Here)

5 pre-existing failures on master — these are tracked separately:
1. `ArchitectureTests.Models_Must_Not_Depend_On_Systems` — Merchant→MerchantInventoryConfig, Player→SkillTree/Skill
2. `ArchitectureTests.AllEnemySubclasses_MustHave_JsonDerivedTypeAttribute` — GenericEnemy missing attribute
3. `ArchitectureTests.Engine_Must_Not_Depend_On_Data` (2 violations)
4. `RunStatsTests.RecordRunEnd_CalledForTrapDeath_HistoryContainsEntry`

## Final State
- **Master tests:** 1394 total, 1389 passed, 5 pre-existing failures
- **Issues closed:** 3 (#755, #766, #745)
- **PRs merged:** 5 (#788, #787, #784, #783, #782)
- **PRs closed:** 1 (#786 — FluentAssertions major version)
- **No regressions introduced.**
