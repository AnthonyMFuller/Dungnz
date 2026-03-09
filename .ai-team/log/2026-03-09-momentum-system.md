# Session: 2026-03-09 — Momentum System (Issue #1274)

**Requested by:** Anthony  
**Team:** Hill, Barton, Romanoff  

---

## What They Did

### Hill — WI-B + WI-E: MomentumResource Model & Display (PR #1293)

Created `Dungnz.Models/MomentumResource.cs` — a sealed class with `Add(int)`, `Reset()`, `Consume()`, `Current`, `Maximum`, and `IsCharged` properties. Initialization deferred to `CombatEngine` (not `PlayerClassDefinition`) to avoid constructor ordering issues with the `partial class Player` pattern. `Player.Momentum` is null before first combat; `ResetCombatPassives()` calls `Momentum?.Reset()` (null-safe for Rogue).

Per-class maximums: Warrior=5 (Fury), Mage=3 (Charge), Paladin=4 (Devotion), Ranger=3 (Focus). Rogue keeps `ComboPoints` unchanged.

Also extended `ShowCombatMenu` display to render class-appropriate momentum bars alongside the existing Rogue combo dot display. Label and dot format: `●●●○○` up to class max; "CHARGED" suffix appended when `IsCharged`.

### Barton — WI-C + WI-D: CombatEngine Increment + Threshold Effects (PR #1295)

Added `InitPlayerMomentum(Player)` called at the top of `CombatEngine.RunCombat()`. Per-class increment logic (WI-C):
- **Warrior Fury:** `Momentum.Add()` in `PerformPlayerAttack` (on hit) and `PerformEnemyTurn` (on HP damage taken)
- **Mage Arcane Charge:** `Momentum.Add()` after any ability resolves in `AbilityProcessor`
- **Paladin Devotion:** `Momentum.Add()` on DivineShield cast (`AbilityManager`) and on `LayOnHands` heal
- **Ranger Focus:** `Momentum.Add()` in `PerformEnemyTurn` when enemy deals 0 HP damage; `Momentum.Reset()` on any HP damage taken

Threshold effects (WI-D) via `Momentum.Consume()`:
- **Warrior:** `+100%` damage modifier on next `PerformPlayerAttack`
- **Mage:** zero mana cost override + `1.25×` damage multiplier (HP-delta approach post-`switch (type)` block — `(int)(delta * 0.25f)` added after)
- **Paladin:** Stun 1 turn applied after `HolyStrike` resolves
- **Ranger:** `bypassDefense = true` in attack resolver for next attack

Ranger Focus HP-before/after tracking uses `AddRangerFocusIfNoDamage(player, hpBefore)` helper at all 5 `PerformEnemyTurn` call sites to avoid void-return refactor of a 15+-early-return method. ManaShield partial absorb correctly reaches `TakeDamage`, triggering Ranger Reset.

### Romanoff — WI-F: Test Coverage + PR Review (PR #1294)

Authored `MomentumResourceTests.cs`: Add, Consume, Reset, clamping, `IsCharged` boundary — full unit coverage of the model. Authored `MomentumResourcePlayerInitTests.cs` (skipped — tests WI-B path via `PlayerClassDefinition`, not the CombatEngine path actually implemented).

Reviewed and approved PR #1293 (Hill) and PR #1295 (Barton). All 3 PRs merged. Issue #1274 closed.

Documented key post-combat test limitations: (1) `Momentum.Current` is always 0 after `CombatResult.Won` due to `ResetCombatPassives()` cleanup; (2) `InitPlayerMomentum()` overwrites any pre-set momentum at `RunCombat()` start; (3) Ranger 0-damage tests blocked by `Math.Max(1, ...)` minimum-damage rule — Mage/Ranger integration tests remain skipped.

---

## Key Technical Decisions

- **`MomentumResource` sealed class** rather than 4 ad-hoc fields — consistent with existing `ComboPoints` pattern, encapsulates `IsCharged` logic, enables `Consume()` as atomic check+reset.
- **Initialization deferred to CombatEngine** — `Player.Momentum` is null before first combat; avoids `partial class` constructor hazard. `Consume()` added for clean WI-D call sites.
- **Rogue ComboPoints unchanged** — partial-spend semantics (Flurry/Assassinate) differ from threshold-pop model; migration would be high risk for no gain.
- **Ranger HP-delta via helper at call sites** — avoids changing `PerformEnemyTurn` return type from `void` to carry "did damage" signal through 15+ early-return paths.
- **Mage damage multiplier via HP-delta post-switch** — generic across all Mage ability cases without per-case edits; correct for current abilities (no multi-damage-mutation cases).

---

## Test Suite

**1872 passing, 4 skipped, 0 failing.**  
Skipped: Mage ability-submenu navigation tests and Ranger 0-damage tests — both deferred pending test infrastructure work (FakeMenuNavigator submenu support, Ranger 0-damage scenario).

---

## Related PRs

- PR #1293: MomentumResource model + display (Hill)
- PR #1294: Momentum test coverage + PR review (Romanoff)
- PR #1295: CombatEngine momentum increment + threshold effects (Barton)
- Issue #1274: Closed ✅
