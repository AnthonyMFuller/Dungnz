# Barton Systems Review — 2026-02-20

**Reviewer:** Barton (Systems Dev)
**Scope:** All owned systems — CombatEngine, StatusEffectManager, AbilityManager, InventoryManager, LootTable, Player models, enemy files

---

## Findings Summary

Six bugs filed. Listed by severity.

| # | Issue | File | Severity |
|---|-------|------|----------|
| #611 | Ability menu cancel skips enemy turn | CombatEngine.cs ~L490 | **Critical** |
| #612 | LootTable.RollDrop crashes (empty tier pool) | LootTable.cs ~L193 | **Critical** |
| #613 | Enemy HP goes negative from DoT (no floor clamp) | StatusEffectManager.cs ~L80 | Moderate |
| #614 | CryptPriest heals every 3 turns not 2 (off-by-one) | CombatEngine.cs ~L933 | Moderate |
| #615 | ManaShield uses direct `Mana -=` instead of SpendMana() | CombatEngine.cs ~L1197 | Minor |
| #616 | XP progress display shows stale threshold on level-up | CombatEngine.cs ~L1482 | Minor |

---

## Critical Issues

### #611 — Ability Cancel Free Turn
Player opens `[B]ability`, cancels with `[C]`, loop `continue`s to top without calling `PerformEnemyTurn`. Player can stall any fight indefinitely. Fix: call `PerformEnemyTurn` before `continue` in the Cancel branch.

### #612 — LootTable Empty Pool Crash
`SetTierPools` can receive empty (non-null) lists. `??` operator doesn't fall back to hardcoded lists in that case. `_rng.Next(0)` throws `ArgumentOutOfRangeException`. `RollTier` and `RollArmorTier` have the guard; `RollDrop`'s tiered item block does not. Fix: add `if (pool.Count == 0) return new LootResult { Gold = gold };` before the `pool[_rng.Next(pool.Count)]` call.

---

## Areas Reviewed as Clean

- Damage formula (`Math.Max(1, atk - def)`) — no negative damage possible
- HP overflow on level-up — `LevelUp()` properly handles `MaxHP` then clamps `HP`
- Max level cap — enforced at 20 in `CheckLevelUp` while loop condition
- Player death check — DoT player death correctly detected before enemy acts (#210 fix in place)
- Gold underflow — `SpendGold` throws `InvalidOperationException` before deducting, shop checks balance first
- Flee mechanic — 50% chance, failure grants enemy free turn (correct)
- Inventory null/empty usage — `UseItem` returns `NotFound` gracefully
- Equip validation — `EquipItem` throws if item not in inventory
- XP level-up formula — `player.XP / 100 + 1 > player.Level` is mathematically equivalent to `player.XP >= 100 * player.Level` (correct)
- Status effect application/expiry — `ProcessTurnStart` correctly decrements and removes expired effects
- Stun handling — pre-ProcessTurnStart stun capture (Fix #167) in place
- LootTable explicit drop pool — correctly iterates, no crash risk since `_drops` is typed `List<(Item, double)>`

