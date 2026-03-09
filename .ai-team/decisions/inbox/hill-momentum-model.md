# Decision: MomentumResource Initialization Deferred to CombatEngine

**Date:** 2026-03-10  
**Author:** Hill  
**Issue:** #1274 (WI-B)

## Context

`MomentumResource` is a per-class resource (Warrior=5, Mage=3, Paladin=4, Ranger=3, Rogue=null).
It needs initialization with the correct `maximum` value for each class.

## Options Considered

1. **Initialize in Player constructor** — Player is a `partial class` with no explicit constructor;
   adding one creates risk of partial-file ordering issues and `Class` is set externally anyway.

2. **Initialize in Player property setter** — Would require changing `Class` to a non-auto property,
   adding setter logic, and creating coupling between Class assignment and Momentum lifecycle.

3. **Defer to CombatEngine (chosen)** — Consistent with how `BattleHardenedStacks` works: the model
   property starts at its default (null), and CombatEngine sets `player.Momentum = new MomentumResource(max)`
   at combat start for the applicable classes. Rogue stays null.

## Decision

`Momentum` starts as null in `Player`. CombatEngine (Barton) initializes it with `new MomentumResource(max)`
per class at combat start, alongside existing class-passive initialization logic.

`ResetCombatPassives()` calls `Momentum?.Reset()` — safe because null-conditional handles the Rogue/null case.

## Per-Class Max Values

| Class   | Max | Label    |
|---------|-----|----------|
| Warrior | 5   | Fury     |
| Mage    | 3   | Charge   |
| Paladin | 4   | Devotion |
| Ranger  | 3   | Focus    |
| Rogue   | null| (ComboPoints used instead) |
