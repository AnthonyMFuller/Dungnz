# PR #225 Review — Romanoff: Edge-case tests for #220 and #221

**Reviewer:** Coulson (Lead)
**Branch:** `squad/220-221-tests`
**Verdict:** ✅ APPROVED (with note on alignment test failures)

## Rebase Performed
Branch rebased onto master (which now includes PR #224 fix for #219/#221/#222). Redundant fix commits dropped cleanly. Force-pushed.

## ColorizeDamage Tests (ColorizeDamageTests.cs) — ✅ PASS (2/2)

### Test quality
- Proper xUnit `[Fact]` tests with clear Arrange-Act-Assert structure
- `ColorizeDamage_NormalCase_OnlyColorizesDamageNumber` — baseline: single occurrence, correct colorization
- `ColorizeDamage_EdgeCase_OnlyLastOccurrenceIsColorized_WhenDamageAppearsInEnemyName` — the #220 edge case: enemy named "5" dealing 5 damage, verifies only the trailing "5" is colorized
- `CountColorized` helper is clean and reusable
- Uses `RawCombatMessages` (new property on FakeDisplayService) to inspect ANSI-intact output — good design

### FakeDisplayService change
- Added `RawCombatMessages` list that stores messages before ANSI stripping
- Minimal, non-breaking addition — existing `CombatMessages` (stripped) still works for all other tests

## ShowEquipmentComparison Alignment Tests (ShowEquipmentComparisonAlignmentTests.cs) — ❌ FAIL (2/2)

### Test quality — tests are CORRECT
- `ShowEquipmentComparison_AllBoxLines_HaveConsistentVisualWidth_WhenDeltasAreColoured` — verifies every `║`-prefixed line matches border width after ANSI stripping
- `ShowEquipmentComparison_RightBorderChar_AppearsAtConsistentColumn_WhenOnlyAttackChanges` — mixed case: one delta row, one plain row
- Both use `IDisposable` pattern with `StringWriter` console capture — clean
- `BoxWidth` helper correctly derives expected width from the `╔═══╗` border line

### Why they fail
The tests correctly detect a **remaining alignment bug**: master's #221 fix (PR #224) corrected the Attack/Defense delta rows but did NOT fix the non-delta content rows:
- `║ Current:  {name,-27}║` → produces 40-char lines
- `║ New:      {name,-27}║` → produces 40-char lines
- Border `╔═══...═══╗` → 41 chars

The padding should be `,-28` (not `,-27`) to match the 41-char border width. This is a **production code bug**, not a test bug.

### Action needed
**Follow-up required:** Fix `ShowEquipmentComparison` non-delta rows to use correct padding (`,-28`). File as follow-up to #221 or have Barton fix before merge.

## Test Results Summary
- ColorizeDamage tests: 2/2 PASS ✅
- Alignment tests: 2/2 FAIL ❌ (correct tests, incomplete production fix)
- All other tests: 269/269 PASS ✅

## Decision
Tests are well-structured, correctly written, and exercise the right edge cases. Approve the test code. The alignment test failures are a signal that the #221 fix needs a follow-up patch to the `Current:`/`New:` rows in `ShowEquipmentComparison`. **Do not merge until alignment fix lands** (or tests will break CI).
