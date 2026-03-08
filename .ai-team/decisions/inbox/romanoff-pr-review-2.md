# Romanoff PR Review Round 2 — Blocking Issues

**Date:** 2026-03-08
**Author:** Romanoff
**Status:** For team awareness

---

## PR #1279 — Fury: Mid-Combat Banter (squad/1271-mid-combat-banter)

### ❌ Blocking: NarrationService.GetEnemyCritReaction signature conflict

**Problem:** PR #1279 has `GetEnemyCritReaction` returning `string?` (nullable). PR #1275 (now approved, merging to main) changes the method to return `string` (non-null, with `_defaultCritReaction` fallback in `EnemyNarration.GetCritReactions()`).

**Impact:** When #1279 tries to merge after #1275 lands, there will be a merge conflict in `NarrationService.cs`.

**Resolution:** Fury must rebase `squad/1271-mid-combat-banter` on main after #1275 merges. The CombatEngine null guard (`if (!string.IsNullOrEmpty(critReaction))`) is harmless to keep even after the signature change.

**Owner:** Fury
**Depends on:** #1275 merge

---

## PR #1280 — Barton: Enemy Intent Telegraph (squad/1270-enemy-intent-telegraph)

### ✅ No blocking issues

All checks pass. Same rebase dependency as #1279 (same base branches, same files modified). Barton should rebase after #1275 merges before CI can run cleanly.

**Owner:** Barton
**Depends on:** #1275 merge

---

## Coverage Gate — Dungnz.Display

### Context (resolved in PR #1277)

`Dungnz.Display` was at 50.57% line coverage vs. the 70% threshold. The Spectre TUI classes are not measured by coverlet (they require live terminal infrastructure). The actual gap was in `ConsoleDisplayService` at 50% — the class IS exercised in tests but many methods were untested.

### Resolution

Added 38 targeted tests to `ConsoleDisplayServiceCoverageTests.cs`:
- `ShowTitle`, `ShowEnhancedTitle` (title rendering paths)
- `ShowMap` with 6 scenarios (BFS renderer, room symbols, connectors)
- `ShowRoom` with environmental hazards and special room types
- `SelectDifficulty`, `SelectClass` (with and without prestige) — `SelectClass` alone was 0/78 coverage
- Interactive methods via `FakeInputReader` injection: `ShowConfirmMenu`, `ShowShopAndSelect`, `ShowSellMenuAndSelect`, `ShowShrineMenuAndSelect`
- `ShowInventoryAndSelect` (empty inventory path)

**Result:** `Dungnz.Display` 50.57% → 74.09%. All 1815 tests pass. Gate cleared.

### Key Learning

`SelectClass` (78 sequence points, 0% covered) was the highest-value single target. It calls `StatBar` (which was also 0% covered) — one test covered both. When hunting coverage gaps, look for large uncovered methods first.

---

## Standard: FakeInputReader injection pattern for ConsoleDisplayService

All interactive `ConsoleDisplayService` methods that use `_input.ReadLine()` can be tested by injecting `new FakeInputReader("1")` at construction time. Methods that use `Console.ReadLine()` directly (e.g., `ShowInventoryAndSelect`) need `Console.SetIn(new StringReader("x"))` or must be tested via empty-input paths.
