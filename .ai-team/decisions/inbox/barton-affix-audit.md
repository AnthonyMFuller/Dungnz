# Decision: All 5 Unwired Affix Properties Implemented (not removed)

**Author:** Barton
**Date:** 2026-03-03
**Issue:** #871

## Decision
All 5 previously-dead affix properties (`EnemyDefReduction`, `HolyDamageVsUndead`, `BlockChanceBonus`, `ReviveCooldownBonus`, `PeriodicDmgBonus`) are now fully wired. None were removed from loot tables.

## Rationale
The combat system had sufficient hooks for all 5 to be implemented cleanly:
- `EnemyDefReduction` and `HolyDamageVsUndead` fit naturally into the player damage calculation section
- `BlockChanceBonus` slots cleanly next to the existing dodge check
- `PeriodicDmgBonus` fits in `OnTurnStart` (same place as `belt_regen`)
- `ReviveCooldownBonus` extended the existing `phoenix_revive` passive — required a new run-level flag `PhoenixExtraChargeUsed` on Player

## Impact for other agents
- **Romanoff (Tester):** New fields on `Item` and `Player` are testable. Key scenarios: (1) enemy DEF reduction clamped to 0 when reduction > enemy.Defense, (2) holy damage only fires when `enemy.IsUndead = true`, (3) block is independent of dodge (both can exist), (4) phoenix extra charge consumes `PhoenixExtraChargeUsed` not `PhoenixUsedThisRun`.
- **Hill:** No Player model structural changes — only new auto-properties on `PlayerCombat.cs` partial class. `RecalculateDerivedBonuses()` now sums all 5 new item fields from equipped gear.
