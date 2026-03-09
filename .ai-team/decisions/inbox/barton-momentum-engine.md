# Decision: Momentum WI-C + WI-D implementation approach

**By:** Barton  
**Date:** 2026-03-10  
**PR:** #1295  
**Issue:** #1274

---

## 1. Consume() added to MomentumResource

**Decision:** Added `bool Consume()` to `MomentumResource` — atomically checks `IsCharged` and resets if true, returns the result.

**Rationale:** Romanoff's skipped unit tests (`squad/1274-momentum-tests`) explicitly test `Consume()`. It also provides a cleaner API for WI-D code: `if (player.Momentum?.Consume() ?? false)` is one expression vs two (`IsCharged` check + `Reset()`).

---

## 2. Momentum initialized at combat start (CombatEngine), not at class-select

**Decision:** `InitPlayerMomentum(Player)` is called in `CombatEngine.RunCombat()` at combat start, creating a fresh `MomentumResource` per class each combat. Not wired into `PlayerClassDefinition`.

**Rationale:** Hill's model comment said "Initialized by CombatEngine on first combat start". The `Player.Momentum` property has a public setter for this purpose. Romanoff's `MomentumResourcePlayerInitTests` are still skipped — they test "wired to PlayerClassDefinition" (WI-B path) and do not block this approach.

**Consequence:** `player.Momentum` is `null` before the first combat. Any code testing Momentum state before a combat begins should call `InitPlayerMomentum()` or set `player.Momentum` directly (as Romanoff's integration tests do via `player.Momentum!.Add(5)`).

---

## 3. Ranger Focus: HP-before/after tracking at call sites

**Decision:** Ranger Focus Add(1) is implemented via `AddRangerFocusIfNoDamage(player, hpBefore)` helper at all 5 main-loop `PerformEnemyTurn` call sites. `hpBefore` is captured before each call, compared after.

**Rationale:** `PerformEnemyTurn` has 15+ early-return paths. Changing its return type from `void` to `bool` (to carry "did damage" info) would be a larger refactor. The HP comparison is semantically correct: any path that deals HP damage (via `TakeDamage`) will reduce HP; any 0-damage path (dodge, DivineShield absorb, ManaShield full absorb, stun skip, block) leaves HP unchanged.

**Edge case:** ManaShield PARTIAL absorb still reaches `TakeDamage` with the remainder — correctly triggers Ranger Reset and does NOT add Focus.

---

## 4. Paladin "Holy Smite" = HolyStrike; LayOnHands = heal component

**Decision:** The spec's "Holy Smite's heal component" (WI-C) was interpreted as `AbilityType.LayOnHands`. The spec's "next Smite cast" (WI-D) was interpreted as `AbilityType.HolyStrike`.

**Rationale:** No `HolySmite` ability exists in the codebase. `HolyStrike` is the Paladin offensive smite. `LayOnHands` is the Paladin heal. DivineShield absorb in `PerformEnemyTurn` is also a WI-C trigger (explicit in spec). DivineShield CAST in `AbilityManager` was added as an additional WI-C trigger (proactive momentum building).

---

## 5. Mage 1.25× damage: HP-delta approach after switch

**Decision:** For Mage Arcane Charge WI-D, captured `enemyHpBeforeAbility` before the `switch (type)` block. After the switch, computed `delta = before - current` and applied `(int)(delta * 0.25f)` extra damage. This adds 25% of what was dealt (total = 1.25×).

**Rationale:** Ability cases each deal damage differently (some use `player.Attack * N - enemy.Defense`, others flat values). Adding individual multiplier checks to every Mage ability case would be ~20 edits and high maintenance. The HP-delta approach is generic and applies correctly regardless of how damage was calculated.

**Caveat:** If a case modifies enemy HP multiple times (e.g., heal enemy then damage), the delta captures the net change. Currently no such Mage ability exists.
