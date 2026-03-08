# Decision: Enemy Intent Telegraph Implementation (#1270)

**Author:** Barton  
**Date:** 2026-03-05  
**PR:** https://github.com/AnthonyMFuller/Dungnz/pull/1280

## Decision

Telegraph special enemy attacks using **Option A** (same-turn warning before the attack resolves), not Option B (prior-turn warning), for all non-boss enemies.

## Rationale

Option B is already implemented for `DungeonBoss` via the `IsCharging` / `ChargeActive` flag pair — it telegraphs one turn early and skips damage on the warn turn. Extending Option B to non-boss enemies (FrostBreath, FlameBreath, TidalSlam) would require new boolean state flags on `Enemy.cs`, which adds model complexity for a cosmetic UX improvement.

Option A adds the warning message at the top of the special-attack turn itself. The player can't dodge the current hit, but they learn the ability exists and its cycle pattern — enabling counter-play for subsequent occurrences.

## Scope of This Decision

`ShowIntentTelegraph()` is intentionally narrow: it only fires for named special attacks (5 currently). Normal melee hits, passive regen, and boss phase triggers are **not** telegraphed — those already have descriptive inline messages.

## Extensibility Note

To add a telegraph for a new special attack, call `ShowIntentTelegraph(enemy, "Ability Name")` immediately before the ability resolves in `PerformEnemyTurn`. Add a matching verb string to the switch expression in `ShowIntentTelegraph` if a custom verb is desired; the generic fallback handles unknown abilities automatically.
