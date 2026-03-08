### 2026-03-06: QA Review — Bug Hunt Sprint PRs (1 Approved, 3 Blocked)

**Reviewer:** Romanoff  
**PRs Reviewed:** #1255, #1259, #1260, #1261  
**Outcome:** 1 approved, 3 blocked with required fixes

---

## Summary

Reviewed 4 PRs produced by the pre-v3 bug hunt sprint. Found critical quality issues in 3 of 4 PRs:

**PR #1260 (EnemyAI + CommandHandlerBase) — APPROVED ✅**
- Clean implementation, all issues correctly addressed
- 38 enemy types now have AI (2 specialized, 36 default)
- CommandHandlerBase provides architectural foundation
- Build passes, 1759 tests pass
- Ready to merge (branch protection prevents direct merge — requires admin)

**PR #1255 (DevOps fixes) — BLOCKED ❌**
- Critical: 3 files completely emptied instead of updated
  - `scripts/coverage.sh` (23 lines deleted)
  - `.github/workflows/squad-stryker.yml` (53 lines deleted)  
  - `Dungnz.Tests/ArchitectureTests.cs` (76 lines deleted)
- Files should be UPDATED, not deleted
- Also includes undocumented AttackResolver/SetBonusManager changes
- Build passes, tests pass, but deletions are blockers

**PR #1259 (SetBonusManager fixes) — INCOMPLETE ❌**
- Claims to close 4 issues, only fixes 2
- Missing: MaxHP/MaxMana application (#1242), CritChanceBonus in RollCrit (#1253)
- Build passes, tests pass, but work is incomplete

**PR #1261 (Missing tests) — BLOCKED ❌**
- Same file deletion issues as #1255
- Includes unrelated changes (AttackResolver, SetBonusManager, EnemyTypeRegistry)
- Tests themselves are good (+16 tests), but file deletions block merge
- Build passes, 1775 tests pass

---

## Root Cause Analysis

### File Deletion Pattern (PRs #1255, #1261)
Three critical files were emptied in 2 separate PRs. This suggests a Git merge conflict resolution issue where conflicts were resolved by selecting "delete entire file" instead of merging changes.

**Impact:** Removes local dev scripts, CI workflows, and architectural safety tests.

**Fix:** Git workflow training — document proper conflict resolution, never select "delete entire file."

### Scope Creep Without Documentation (PRs #1255, #1261)
Both PRs include AttackResolver/SetBonusManager changes not mentioned in titles, bodies, or linked issues. These changes belong in PR #1259 but were duplicated elsewhere.

**Impact:** PR metadata can't be trusted, reviewer time wasted tracking down undocumented changes.

**Fix:** PR template with checklist: "No unrelated changes", "All linked issues actually fixed."

### Incomplete Work Claimed Complete (PR #1259)
PR claims to close 4 issues but only contains fixes for 2 issues. Either work was lost in merge conflicts or PR was opened prematurely.

**Impact:** Breaks trust in PR metadata, issues incorrectly marked as resolved.

**Fix:** Pre-merge checklist: verify every linked issue is actually addressed by the diff.

---

## Required Actions

### For PR #1255
1. Restore `scripts/coverage.sh` with 70% threshold (not deleted)
2. Restore `.github/workflows/squad-stryker.yml` with `dotnet tool restore` (not deleted)
3. Restore `Dungnz.Tests/ArchitectureTests.cs` with `Dungnz.Systems.EnemyTypeRegistry` reference (not deleted)
4. Document AttackResolver/SetBonusManager changes in PR body OR move to PR #1259

### For PR #1259
1. Add MaxHP/MaxMana application to player stats (lines 231-232 in SetBonusManager.cs)
2. Add CritChanceBonus to RollCrit calculation in AttackResolver.cs
3. Verify all 4 issues (#1240, #1242, #1253, #1254) are actually fixed

### For PR #1261
1. Restore `scripts/coverage.sh`, `squad-stryker.yml`, `ArchitectureTests.cs` (same as #1255)
2. Remove unrelated AttackResolver/SetBonusManager/EnemyTypeRegistry changes
3. Consider deepening test coverage (many tests are trivial one-liners)

### For PR #1260
1. Merge when branch protection allows (requires admin/approval workflow)
2. Issues #1225 and #1226 will auto-close on merge

---

## Process Improvements Recommended

### Git Workflow
- Document merge conflict resolution best practices
- Never resolve conflicts by deleting entire files
- Use `git diff master...HEAD --name-status` to verify no unintended deletions

### PR Quality Gate
- Pre-submit checklist: "No files deleted unless intentional", "All linked issues fixed", "No unrelated changes"
- Minimum 3 assertions per test method (or explicit waiver comment)
- Diff review before opening PR to catch scope creep

### Branch Protection
- Require QA approval before merge (current setup allows self-merge)
- Consider requiring 2 approvals for PRs that touch critical paths (CI workflows, architecture tests)

---

## Sprint Velocity vs Quality

**Velocity:** 4 PRs opened, 38 files changed, 16 new tests  
**Quality:** 3 of 4 PRs blocked, critical file deletions in 2 PRs, incomplete work in 1 PR

**Conclusion:** High velocity sprint, but quality control insufficient. Recommend slower pace with stronger pre-merge review next sprint.

---

**Review complete.** PR #1260 ready to merge. PRs #1255, #1259, #1261 require rework.

— Romanoff, QA
