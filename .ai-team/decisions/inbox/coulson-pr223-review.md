# PR #223 Review — Barton: ColorizeDamage LastIndexOf fix + README

**Reviewer:** Coulson (Lead)
**Branch:** `squad/220-colorize-damage-fix`
**Verdict:** ✅ APPROVED

## Code Review

### `ReplaceLastOccurrence` helper (CombatEngine.cs)
- Clean `private static` helper using `LastIndexOf` — correct approach
- Null-safe: returns `source` unchanged if `find` not found
- XML doc accurately explains why last-occurrence is the right semantic
- Both call sites updated: normal damage and crit path

### `ColorizeDamage` changes
- `string.Replace` → `ReplaceLastOccurrence` on both code paths (normal + crit)
- Preserves existing colorization logic (BrightRed for damage, Yellow+Bold for crits)
- Fix directly addresses Issue #220: when damage number appears in enemy name, only the trailing (actual damage) occurrence is colorized

### README update
- Accurately documents the `LastIndexOf` behaviour
- Placed in the correct section (Display & Colours)
- Concise, informative

### Build & Tests
- ✅ All 267 tests pass on this branch
- No new warnings introduced

## Decision
Merge to master. Clean, minimal, correct fix.
