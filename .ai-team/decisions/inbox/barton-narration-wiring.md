# Decision: Narration Call Wiring in CombatEngine

**Date:** 2026-03-08
**Author:** Barton
**Related Issues:** #1271, #1272
**PR:** https://github.com/AnthonyMFuller/Dungnz/pull/1282

---

## Decision 1: Reuse `_combatTurn` for turn gating

**Context:** The task spec suggested adding `_combatTurnNumber`. `_combatTurn` already exists, is incremented at the top of each full turn cycle, and is already shared with `_attackResolver`.

**Decision:** Reuse `_combatTurn`. No new field added.

**Rationale:** Avoids duplicate state that could diverge. The existing field already encodes "full turn number" (player + enemy = 1 increment).

---

## Decision 2: Idle taunt gated on special-attack exclusion booleans

**Context:** The task said "only on normal attack turns (not ability turns, not boss-phase turns)." In `PerformEnemyTurn`, the special-attack paths set local booleans (`isFrostBreath`, `isFlameBreath`, `isTidalSlam`, `wasCharged`) before the unified hit block.

**Decision:** Guard idle taunt with `!isFrostBreath && !isFlameBreath && !isTidalSlam && !wasCharged`.

**Rationale:** These flags are the definitive record of what kind of attack just landed. This is simpler than an explicit "isNormalAttack" flag and stays correct as new special attacks are added (they should get their own bool too).

---

## Decision 3: Desperation placed after stun/freeze early-returns, before AI block

**Context:** The task said "at the START of the enemy's turn." The stun/freeze checks bail out entirely (the enemy truly takes no turn). Desperation should still be blocked if the enemy is stunned.

**Decision:** Place desperation check after the stun/freeze early returns but before the AI action-choice block.

**Rationale:** An enemy that can't act shouldn't be narrated as desperate — the scene is incoherent. Placing it before the AI block ensures it fires regardless of which AI action the enemy eventually takes.

---

## Decision 4: Phase narration fires after PerformPlayerAttack regardless of hit/miss

**Context:** `PerformPlayerAttack` is a void delegation to `_attackResolver`; there is no hit/miss return value at the CombatEngine level. The task said "wherever the player lands a hit."

**Decision:** Wire phase narration immediately after `PerformPlayerAttack(player, enemy)` in the ATTACK branch, unconditionally (no hit/miss guard).

**Rationale:** `GetPhaseAwareAttackNarration` returns atmospheric flavor about the phase of the fight (turn number, HP ratios), not hit-specific flavor. It is meaningful on miss turns too. If this needs to be restricted to hits in future, `PerformPlayerAttack` should return a result type — that's a separate refactor.
