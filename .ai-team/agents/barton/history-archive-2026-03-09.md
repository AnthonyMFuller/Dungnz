# Barton — History Archive (pre-2026-02)

*Archived 2026-03-09. Contains entries older than 3 months.*

---

## 2025-01-30: Bug Hunt Fixes — PR #625

**Task:** Fix 6 game systems bugs (#611–#616) identified in the deep systems review.
**Branch:** squad/bug-hunt-systems-fixes
**PR:** #625 — All 6 bugs fixed in a single commit.

**Fixes Applied:**

| Issue | Fix |
|-------|-----|
| #611 | Ability menu Cancel now calls PerformEnemyTurn — exploit closed |
| #612 | `pool.Count > 0` guard added before `_rng.Next(pool.Count)` in LootTable tiered drop |
| #613 | All enemy DoT assignments (Poison/Bleed/Burn) now use `Math.Max(0, HP - dmg)` |
| #614 | CryptPriest cooldown reset changed to `SelfHealEveryTurns - 1` for decrement-first pattern |
| #615 | `player.Mana -= manaLost` replaced with `player.SpendMana(manaLost)` in ManaShield handler |
| #616 | `CheckLevelUp` moved before XP progress message so threshold reflects new level |

**Files Changed:**
- `Engine/CombatEngine.cs` (#611, #614, #615, #616)
- `Systems/StatusEffectManager.cs` (#613)
- `Models/LootTable.cs` (#612)

**Test Results:** 684/684 passed ✅
