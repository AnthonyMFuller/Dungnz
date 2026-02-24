# Session: 2026-02-24 — Fix 3 Pre-Existing Test Failures

**Requested by:** Anthony

## Summary
Fixed 3 pre-existing test failures that existed before this sprint and were unrelated to prior UI/UX work.

## Failures Fixed

### 1. `ShowLootDrop_ColorizedTier_BoxBordersAlign(Uncommon)` and `(Rare)`
**Issue:** ANSI padding bug in `DisplayService.cs`
- Stat line used raw `{statLine,-36}` format (not ANSI-aware)
- Then appended `• {weight} wt`, making visible line width 48 vs box border 40
- Misalignment caused test failures

**Fix:** Combined stat+weight into single `PadRightVisible(statWithWeight, 36)` call to handle ANSI codes correctly

### 2. `PlayerDeathInCombat_EndsGameLoop`
**Issue:** `FakeDisplayService.ShowGameOver` wrote to `AllOutput` only, not `Messages`
- Test asserted on `Messages`, causing failure

**Fix:** Added `Messages.Add("YOU HAVE FALLEN...")` to the stub method

## Outcome
- PR #311 opened and merged
- All 427 tests now pass on master

## Related Files
- `DisplayService.cs` — ANSI-aware padding fix
- Test fixtures in combat test suite
