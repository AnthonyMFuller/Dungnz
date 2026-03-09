# Triage: Per-Class Momentum Resource System (Issue #1274)

**Date:** 2026-03-05  
**By:** Coulson  
**Issue:** #1274 — Implement per-class momentum resource system  
**Status:** READY — blocked only on spec clarification (see WI-A)

---

## Complexity Assessment: MEDIUM-HIGH

This is a multi-layer feature touching Models, Engine, and Display. The Rogue
combo point proof-of-concept is already shipping and provides the pattern. The
main risk is CombatEngine state expansion in an already 1,329-line file and
three under-specified triggered effects that need design decisions before
Barton can code.

---

## What Already Exists

| Class     | Existing mechanism                                              | Type        |
|-----------|------------------------------------------------------------------|-------------|
| Rogue     | `ComboPoints` (0–5), `AddComboPoints`/`SpendComboPoints`         | ✅ Complete  |
| Warrior   | `BattleHardenedStacks` (0–4) — ATK bonus based on % HP lost     | ❌ Different |
| Mage      | `ArcaneSurgeReady` (bool) — next ability –1 mana                 | ❌ Different |
| Paladin   | `DivineBulwarkFired` (bool) — once-per-combat Fortified at <25% HP | ❌ Different |
| Ranger    | None                                                             | ❌ Missing   |

The Warrior/Mage/Paladin existing mechanics are **not** the momentum system —
they are threshold-based passives. The issue proposes a separate per-action
counter that coexists with these. Do not remove the existing passives.

---

## Architecture Decision

**Use a shared `MomentumResource` type on Player, not 4 new ad-hoc fields.**

The issue author proposed `MomentumCharge`/`MomentumThreshold` as two bare
ints. Recommend instead a small value type:

```csharp
// Dungnz.Models/MomentumResource.cs
public sealed class MomentumResource
{
    public int Current { get; private set; }
    public int Maximum { get; }
    public bool IsCharged => Current >= Maximum;
    public MomentumResource(int maximum) { Maximum = maximum; }
    public void Add(int amount = 1) => Current = Math.Clamp(Current + amount, 0, Maximum);
    public bool Consume() { if (!IsCharged) return false; Current = 0; return true; }
    public void Reset() => Current = 0;
}
```

**Rogue keeps `ComboPoints`** — it has ability-spending semantics (partial
spend via Flurry/Assassinate) that differ from the threshold-pop model. The
other 4 classes use `Momentum`. This avoids a risky migration of working Rogue
combat in the same PR.

`Player` gains:
```csharp
public MomentumResource Momentum { get; private set; } = new(1); // overwritten at class init
```

Class maximum wired in `PlayerClassDefinition`:
- Warrior Fury: max 5
- Mage Arcane Charge: max 3
- Paladin Devotion: max 4
- Ranger Focus: max 3
- Necromancer: no momentum (keep as max=0 no-op or omit)

`ResetCombatPassives()` calls `Momentum.Reset()`.

Save/load: `MomentumResource.Current` serialised as a single `int` on Player.

---

## Work Items (Ordered)

### WI-A — Design spec sign-off (Coulson + Anthony) — BLOCKS ALL
Three effects are ambiguous and need exact numbers before implementation:

| Class   | Trigger              | Effect (proposed — needs sign-off)                         |
|---------|----------------------|------------------------------------------------------------|
| Warrior | Fury = 5             | Next attack: +100% damage (consume Fury on use)            |
| Mage    | Arcane Charge = 3    | Next ability: 0 mana cost + 25% extra damage (consume)     |
| Paladin | Devotion = 4         | Next Smite: applies Stun 1 turn (consume)                  |
| Ranger  | Focus = 3            | Next attack: ignores enemy DEF entirely (consume; reset on taking damage) |

Also: rename the `"[Battle Hardened] Fury builds"` display string to avoid
confusion with the new Warrior Fury resource UI.

### WI-B — Model layer: `MomentumResource` + Player wiring (→ Hill)
- New `Dungnz.Models/MomentumResource.cs`
- `Player.Momentum` property, initialised per class in `PlayerClassDefinition`
- Helper `InitMomentumForClass(PlayerClass cls)` on Player or ClassManager
- Include `Momentum.Reset()` in `ResetCombatPassives()`
- JSON-serialisable (`Current` as int; reconstruct max from class at load)
- PR must not touch `ComboPoints` — Rogue unchanged
- Acceptance: builds clean, `Momentum.IsCharged` correct, reset fires on combat start

### WI-C — CombatEngine: increment logic per class (→ Barton)
- Depends on WI-B
- Warrior Fury: `Momentum.Add()` in `PerformPlayerAttack` (on hit) + `PerformEnemyTurn` (on damage taken)
- Mage Arcane Charge: `Momentum.Add()` after any ability resolved in `AbilityProcessor`
- Paladin Devotion: `Momentum.Add()` when Divine Shield ability used or Holy Smite heals
- Ranger Focus: `Momentum.Add()` in `PerformEnemyTurn` when enemy deals 0 damage (dodge/miss); `Momentum.Reset()` on damage taken
- Acceptance: each class accumulates correctly; Necromancer unaffected

### WI-D — CombatEngine: threshold effect application (→ Barton)
- Depends on WI-B, WI-C, and WI-A (needs confirmed effect specs)
- Each class checks `Momentum.IsCharged` at the appropriate point and calls `Momentum.Consume()`
- Warrior: crit modifier flag set before `PerformPlayerAttack` resolves
- Mage: mana cost override + damage multiplier in ability resolve path
- Paladin: Stun applied post-Smite resolve
- Ranger: `bypassDefense = true` flag in attack resolver
- Acceptance: threshold fires once then resets; re-charges correctly

### WI-E — Display: momentum bar in ShowCombatMenu (→ Hill)
- Depends on WI-B
- Extend existing Rogue dot display to all classes in `ShowCombatMenu`
- Label per class: "Fury", "Charge", "Devotion", "Focus"
- Dot format: `●●●○○` (current) up to class max
- "CHARGED" suffix when `Momentum.IsCharged`
- Acceptance: displays for correct class only; Rogue still shows Combo bar (unchanged)

### WI-F — Test coverage (→ Romanoff)
- Depends on WI-B through WI-D
- `MomentumResource` unit tests: Add, Consume, Reset, clamping, IsCharged boundary
- Per-class mechanic tests: 5 classes × charge accumulation + threshold trigger
- Edge cases: Ranger reset on damage, Warrior dual-increment (hit dealt AND taken), Mage multi-ability charge
- Integration: full combat with momentum threshold firing and effect applying
- Acceptance: ≥80% coverage gate maintained

---

## Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| CombatEngine bloat (WI-C + WI-D adds ~100 lines inline) | Medium | Extract `ClassMomentumService` if it grows past 60 lines in WI-D |
| Warrior Fury vs BattleHardened naming confusion | Medium | Rename BattleHardened display string in WI-A |
| "Free enhanced cast" undefined for Mage | High | WI-A blocks WI-D; do not let Barton guess |
| Ranger Focus resets on damage — interacts with Dodge/Evade | Low | Specify: Focus resets only on HP damage (not dodge) |
| Save format: new `Momentum.Current` field | Low | JSON default 0 on load = backward compatible |

---

## Assignment

| Item | Owner | Depends on |
|------|-------|------------|
| WI-A | Coulson + Anthony | — |
| WI-B | Hill | WI-A |
| WI-C | Barton | WI-B |
| WI-D | Barton | WI-B, WI-C, WI-A |
| WI-E | Hill | WI-B |
| WI-F | Romanoff | WI-B through WI-D |

**Primary label:** `agent:Barton` (combat engine owns the bulk of work)  
**Secondary label:** `agent:Hill` (model layer prerequisite)  
**Wave:** `wave:advanced` (Wave 3 feature)  
**Phase:** `phase-3: gameplay`

---

## Priority Note

Issue is marked P2 stretch goal. P1 gameplay bugs (boss loot, HP clamping,
SetBonusManager discard, SoulHarvest dual-impl) should be fixed first. If
Anthony has authorised parallel work, proceed. If not, park this until P1s
are cleared.
