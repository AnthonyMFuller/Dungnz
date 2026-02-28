# Barton — Bug Hunt Systems Fixes

**Date:** 2025-01-30
**PR:** #625 — fix: game systems bug hunt fixes
**Branch:** squad/bug-hunt-systems-fixes

## Bugs Fixed

### #611 — Ability menu cancel exploit
**File:** Engine/CombatEngine.cs
**Fix:** When `AbilityMenuResult.Cancel` is returned from `HandleAbilityMenu`, the code now calls `PerformEnemyTurn` before continuing, matching the turn-consuming behavior of all other player actions. Players can no longer stall indefinitely by canceling the ability menu.

### #612 — LootTable.RollDrop crash on empty tier pool
**File:** Models/LootTable.cs
**Fix:** Added `if (pool.Count > 0)` guard before `_rng.Next(pool.Count)` in the tiered drop block. Empty tier pools are now silently skipped (same as null pools), matching how epic/legendary pools were already guarded.

### #613 — Enemy HP goes negative from DoT damage
**File:** Systems/StatusEffectManager.cs
**Fix:** Replaced direct `e.HP -= N` with `e.HP = Math.Max(0, e.HP - N)` for all three DoT effects: Poison (3), Bleed (5), and Burn (8). Enemy HP now floors at 0, consistent with `TakeDamage()` used for players.

### #614 — CryptPriest self-heal interval off-by-one
**File:** Engine/CombatEngine.cs
**Fix:** Changed cooldown reset from `enemy.SelfHealEveryTurns` to `enemy.SelfHealEveryTurns - 1`. The decrement-first pattern (decrement → check 0 → heal → reset) requires resetting to N-1 to produce a heal every N turns.

### #615 — ManaShield bypasses SpendMana API
**File:** Engine/CombatEngine.cs
**Fix:** Replaced `player.Mana -= manaLost` with `player.SpendMana(manaLost)`. The pre-check `player.Mana >= manaLost` guarantees SpendMana returns true. The else-branch that sets `player.Mana = 0` is separate and not changed.

### #616 — XP progress shows stale threshold after level-up
**File:** Engine/CombatEngine.cs
**Fix:** Moved `CheckLevelUp(player)` to run BEFORE computing `xpToNext` and showing the XP progress message. The message now reflects the post-level-up threshold, eliminating contradictory "109/100 to next level" displays.

## Test Results
- Build: ✅ Clean (0 errors, pre-existing warnings only)
- Tests: ✅ 684/684 passed
