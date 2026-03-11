# Session: 2026-03-11 — Enemy Stats Root Cause Fixed — Gear Panel

**Requested by:** Anthony  
**Team:** Hill, Coulson  

---

## What They Did

### Hill — Root Cause Analysis & Fix

Identified the true root cause of the long-standing "enemy stats never visible in combat" bug. All prior fix rounds (#1324–#1326) verified that rendering code *ran*, but not that the output was *visible*.

**Root cause — #1328:**  
The Stats panel is ~20% of screen height, yielding approximately 8 visible rows. `RenderCombatStatsPanel` was generating 14–19 lines (player stats block + enemy stats block), placing the enemy section permanently below the fold regardless of correctness of rendering logic. The issue was structural — the target panel was too small to surface enemy data.

**Fix implemented:**  
- Extracted `RenderEnemyStatsPanel` in `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs`
- Enemy data (name, HP bar, ATK/DEF, regen, badges, active effects) now rendered into the **Gear panel** (50% height, 30% width) rather than Stats
- Gear panel shows `🐉 Enemy` header with red border during combat
- Fixed `ShowPlayerStats`: was unconditionally calling `RenderGearPanel`, overwriting the enemy panel during combat on level-up events. Now only calls `RenderGearPanel` when not in combat (`_cachedCombatEnemy == null`)
- Post-combat gear restore: `ShowRoom` already clears `_cachedCombatEnemy` and calls `RenderGearPanel` — no additional change needed

**Change stats:** `SpectreLayoutDisplayService.cs` — 24 insertions, 7 deletions  
**Commit:** `df0fb67`

### Coulson — PR Review

Reviewed PR #1329. Confirmed fix is structurally sound: enemy display in Gear panel is the correct approach given the Stats panel's fixed height constraint. Approved and merged to master.

---

## Key Technical Decisions

**Enemy stats belong in the Gear panel, not Stats panel.**  
Stats panel is ~20% screen height (~8 rows); enemy data requires 14–19 rows. Routing enemy data to the Gear panel (50% height) is the correct long-term solution. The Gear panel is contextually repurposed during combat with a `🐉 Enemy` header and red border, restoring to gear display post-combat via `ShowRoom`.

**`ShowPlayerStats` must guard against overwriting combat state.**  
Any method that calls `RenderGearPanel` must check `_cachedCombatEnemy` first. Level-up events triggered during combat previously clobbered the enemy panel silently. Guard pattern: `if (_cachedCombatEnemy == null) RenderGearPanel(...)`.

**Post-combat restore is implicit via `ShowRoom`.**  
`ShowRoom` clears `_cachedCombatEnemy` and calls `RenderGearPanel` as part of its standard flow. No explicit restore step is needed in combat teardown — this was already correct prior to this fix.

---

## Build & Test

- **Build:** 0 errors, 0 warnings  
- **Tests:** 1898 passed, 4 skipped (pre-existing)  
- **Master:** Clean and up to date post-merge  

---

## Related Issues & PRs

- Issue #1328: Enemy stats never visible — root cause (Stats panel below-the-fold overflow)
- PR #1329: Fix enemy stats never visible — render in Gear panel (merged to master)
