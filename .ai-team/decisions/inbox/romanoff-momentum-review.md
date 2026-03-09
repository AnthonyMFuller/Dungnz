# Decision: Momentum Test Strategy — Post-Combat State Limitations

**By:** Romanoff  
**Date:** 2026-03-10  
**Context:** PR #1294 review — activating momentum tests after #1293 and #1295 merged  
**Issue:** #1274

---

## 1. Post-Won Momentum is Always Zero

**Finding:** `CombatEngine.HandleLootAndXP()` calls `_statusEffectApplicator.ResetCombatEffects(player, enemy)`, which calls `player.ResetCombatPassives()`, which calls `Momentum?.Reset()`. After any Won combat, `player.Momentum.Current == 0`.

**Decision:** Never assert `Momentum.Current > 0` on a `CombatResult.Won` result in tests. Use one of:
1. `CombatResult.PlayerDied` path — no cleanup, momentum preserved
2. `display.CombatMessages` inspection for threshold messages ("Momentum unleashed", "Momentum charged")
3. `player.Momentum.Maximum` assertion — immutable after initialization, survives Won

---

## 2. Cannot Pre-Charge Momentum Before RunCombat

**Finding:** `CombatEngine.InitPlayerMomentum(player)` is called at the START of every `RunCombat()`, creating a fresh `MomentumResource` for the player's class. Any `player.Momentum.Add()` calls before `RunCombat()` are immediately overwritten.

**Decision:** WI-D tests that need pre-charged momentum must either:
- Run enough combat turns to charge naturally (Warrior: 3 rounds min at 2 WI-C per round)
- Assert on display messages instead of calling Consume() directly

Affects: Mage_ArcaneCharged_ZeroManaCost, Ranger_TakingDamage_ResetsFocus (both deferred).

---

## 3. Ranger Focus 0-Damage Tests: Blocked by Min-Damage-1

**Finding:** The minimum damage rule (`Math.Max(1, attack - defense)`) means there is no defense value that makes enemy regular attacks deal 0 HP damage. Ranger Focus increments only on TRULY 0-HP-damage enemy turns (stun skip, DivineShield, ManaShield full absorb). Ranger has none of these.

**Decision:** Ranger_TakingNoDamage and Ranger_TakingDamage_ResetsFocus are skipped until a Ranger-compatible 0-damage scenario exists (e.g., a Freeze mechanic that Ranger can apply).

---

## 4. Mage Ability Tests: Menu Navigation Complexity

**Finding:** Mage ability use requires navigating the CombatEngine input menu to slot 2 (Use Ability), then navigating an ability submenu. `FakeInputReader` raw tokens ("A", "F", "2") work for top-level choices only; ability submenu selection requires additional tokens that vary by class loadout and are undocumented.

**Decision:** Mage_CastingAbility and Mage_ArcaneCharged deferred until FakeMenuNavigator supports the ability submenu flow or until the input sequence is documented in test helpers.
