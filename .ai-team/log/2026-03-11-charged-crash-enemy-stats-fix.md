# Session: 2026-03-11 — CHARGED Markup Crash & Enemy Stats Never Shown

**Requested by:** Anthony  
**Team:** Barton, Romanoff  

---

## What They Did

### Barton — Bug Investigation and Fixes

Investigated and fixed two bugs in `Dungnz.Display/SpectreLayoutDisplayService.cs`.

**Bug 1 — #1324: `[CHARGED]` markup crash**  
Spectre.Console interprets square brackets as markup tags. The literal string `[CHARGED]` was being passed directly to Spectre's rendering pipeline, causing a parse exception at runtime whenever the Warrior/Mage/Paladin/Ranger momentum bar reached the charged threshold. Fixed by escaping both occurrences: `[CHARGED]` → `[[CHARGED]]`. Two places changed in `SpectreLayoutDisplayService.cs`.

**Bug 2 — #1325: Enemy stats never shown in combat**  
`ShowCombatStatus` was rendering the enemy stats panel but the player cache was never populated — `_cachedPlayer` was `null` on the first call, causing the display to skip the stats render silently. Fixed by adding `_cachedPlayer = player` at the top of `ShowCombatStatus`, ensuring subsequent panel refreshes have a valid player reference.

Both fixes committed on branch `fix/charged-crash-enemy-stats-1324-1325`. PR #1326 opened for review.

### Romanoff — Review

Reviewing PR #1326.

---

## Key Technical Decisions

- **Spectre escape convention:** Square brackets used in game strings that are passed to Spectre.Console must be doubled (`[[` / `]]`) to be treated as literals, not markup tags. This applies to any status label or resource name rendered via Spectre's markup pipeline.
- **`_cachedPlayer` contract:** `ShowCombatStatus` is now responsible for updating `_cachedPlayer` on every call. All other methods that depend on `_cachedPlayer` for panel refreshes rely on this being populated before any combat-UI call resolves.

---

## Build & Test

- **Build:** 0 errors, 0 warnings  
- **Tests:** 1898 passed, 4 skipped  

---

## Related PRs

- PR #1326: Fix CHARGED markup crash and enemy stats never shown (#1324, #1325)
