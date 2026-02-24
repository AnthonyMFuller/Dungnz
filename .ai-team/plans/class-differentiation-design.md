# Class Differentiation System — Design Document

**Author:** Coulson (Lead)  
**Requested by:** Anthony  
**Date:** 2026-02-26  
**Status:** PLANNING — Not yet approved for implementation

---

## Problem Statement

Currently all three classes (Warrior, Mage, Rogue) share the same 4 abilities:
- Power Strike, Defensive Stance, Poison Dart, Second Wind

The only class differentiation comes from:
- Stat bonuses (BonusAttack/Defense/HP/Mana)
- A single passive trait (+5% damage at low HP for Warrior, +20% spell damage for Mage, +10% dodge for Rogue)
- Class-flavored hit/miss messages in CombatEngine

**Result:** Combat feels nearly identical regardless of class choice. Players have no reason to replay as a different class.

---

## 1. Class-Specific Ability Design

Each class gets **5 unique active abilities** that reinforce their fantasy. Shared abilities are eliminated. All abilities unlock by level 7 to match current pacing.

### 1.1 Warrior — Tank/Berserker Fantasy

Design philosophy: **Sustain through combat, punish enemies for hitting you, get stronger as HP drops.**

| # | Ability | Mana | CD | Lvl | Effect | Flavor |
|---|---------|------|-----|-----|--------|--------|
| 1 | **Shield Bash** | 8 | 2 | 1 | Deal 1.2x damage and apply **Stun** for 1 turn (50% chance). | "You slam your shield into the enemy's skull!" |
| 2 | **Battle Cry** | 10 | 4 | 2 | Remove all debuffs from self. Gain **+25% Attack** for 3 turns. | "A primal roar tears from your throat — you will not fall!" |
| 3 | **Fortify** | 12 | 3 | 3 | Gain **Fortified** status (+50% Defense) for 3 turns. If already below 50% HP, also heal 15% MaxHP. | "You plant your feet and brace for the onslaught." |
| 4 | **Reckless Blow** | 15 | 3 | 5 | Deal **2.5x damage** but take 10% MaxHP self-damage (cannot kill you). Ignores 50% of enemy Defense. | "You throw caution aside and swing with everything!" |
| 5 | **Last Stand** | 20 | 6 | 7 | For 2 turns: damage taken reduced by 75%; all attacks deal +50% damage. Cannot be activated above 40% HP. | "Your vision narrows. Everything slows. This ends now." |

**Combo potential:** Battle Cry → Reckless Blow for burst. Last Stand + Fortify for near-immortality window.

---

### 1.2 Mage — Glass Cannon / Mana Battery Fantasy

Design philosophy: **High mana pool, spells scale with mana spent, risk/reward via HP-to-mana conversion.**

| # | Ability | Mana | CD | Lvl | Effect | Flavor |
|---|---------|------|-----|-----|--------|--------|
| 1 | **Arcane Bolt** | 8 | 0 | 1 | Deal (Attack × 1.5) + (CurrentMana / 10) magic damage. Basic spam ability. | "A crackling bolt of raw energy leaps from your fingertips!" |
| 2 | **Frost Nova** | 14 | 3 | 2 | Deal 1.2x damage and apply **Slow** (new status: enemy hits last, -25% damage) for 2 turns. | "A wave of bitter cold explodes outward!" |
| 3 | **Mana Shield** | 0 | 5 | 4 | Toggle: while active, damage taken is converted to mana drain (1.5 mana per HP that would be lost). Lasts until mana depleted or toggled off. | "You wrap yourself in a lattice of pure arcane energy." |
| 4 | **Arcane Sacrifice** | 0 | 3 | 5 | Spend 15% MaxHP to restore 30% MaxMana. Cannot kill you (minimum 1 HP). | "You draw power from your own essence — dangerous, but effective." |
| 5 | **Meteor** | 35 | 5 | 7 | Deal **(Attack × 3) + 20** damage. If enemy HP falls below 20% after this attack, instantly kill it. | "The ceiling cracks. A fragment of the heavens descends." |

**Combo potential:** Arcane Sacrifice → Meteor for high-damage finisher. Mana Shield + Arcane Bolt spam for sustained safe damage.

**New status effect needed: Slow** — affects turn order and damage dealt.

---

### 1.3 Rogue — Burst/Combo/Stealth Fantasy

Design philosophy: **Stack combo points, execute finishers, punish predictable enemies, high risk/high reward positioning.**

| # | Ability | Mana | CD | Lvl | Effect | Flavor |
|---|---------|------|-----|-----|--------|--------|
| 1 | **Quick Strike** | 5 | 0 | 1 | Deal 1x damage, gain 1 **Combo Point** (max 5). No cooldown. | "A lightning-fast jab — you're already setting up the next hit." |
| 2 | **Backstab** | 10 | 2 | 2 | Deal 1.5x damage. If enemy is afflicted with **Slow**, **Stun**, or **Bleed**, deal 2.5x damage instead. | "You find the opening and drive your blade home." |
| 3 | **Evade** | 12 | 4 | 3 | Guaranteed dodge on the enemy's next attack. Gain 1 Combo Point. | "You melt into the shadows — the blow finds only air." |
| 4 | **Flurry** | 15 | 3 | 5 | Spend all Combo Points. Deal (0.6 × ComboPoints) × Attack damage per hit. Each hit has 30% chance to apply **Bleed** (3 turns). | "A blur of steel — one cut, two, three, more than they can count." |
| 5 | **Assassinate** | 25 | 6 | 7 | Spend all Combo Points (minimum 3 required). Deal (ComboPoints × 0.8) × Attack damage. If enemy HP is below 30%, execute instantly. | "One clean strike. They never see it coming." |

**New mechanic needed: Combo Points** — Rogue-only resource tracked per combat, capped at 5.

**Combo potential:** Quick Strike × 3 → Flurry for AoE bleed. Evade → Backstab on slowed enemy → Assassinate execute.

---

## 2. Passive Skill Tree Design

**Recommendation: Make passive skills class-specific.**

The current shared skill tree (PowerStrike, IronSkin, Swiftness, ManaFlow, BattleHardened) is generic. Class-specific passives reinforce build identity and create interesting level-up choices.

### 2.1 Warrior Passive Tree

| Skill | Lvl | Effect |
|-------|-----|--------|
| **Iron Constitution** | 3 | +15 MaxHP, +5% damage reduction |
| **Undying Will** | 5 | When HP drops below 25%, gain Regen for 2 turns (once per combat) |
| **Berserker's Edge** | 6 | +10% damage for every 25% HP missing (stacks) |
| **Unbreakable** | 8 | Last Stand can activate at 50% HP instead of 40% |

### 2.2 Mage Passive Tree

| Skill | Lvl | Effect |
|-------|-----|--------|
| **Arcane Reservoir** | 3 | +20 MaxMana |
| **Spell Weaver** | 4 | Spells cost 10% less mana (round down) |
| **Ley Conduit** | 6 | Mana regeneration per turn +5 |
| **Overcharge** | 8 | When mana is above 80%, spell damage +25% |

### 2.3 Rogue Passive Tree

| Skill | Lvl | Effect |
|-------|-----|--------|
| **Quick Reflexes** | 3 | +5% dodge chance (stacks with class passive for +15% total) |
| **Opportunist** | 4 | Backstab bonus triggers on Poison in addition to Slow/Stun/Bleed |
| **Relentless** | 6 | Flurry and Assassinate cooldowns reduced by 1 turn |
| **Shadow Master** | 8 | Evade grants 2 Combo Points instead of 1 |

---

## 3. Architecture Recommendation

### Option A: Add `PlayerClass? ClassRestriction` to `Ability` model (RECOMMENDED)

**How it works:**
1. Add `public PlayerClass? ClassRestriction { get; }` to `Ability` model
2. AbilityManager filters abilities by `player.Class` when `ClassRestriction != null`
3. Each class's abilities are registered in AbilityManager constructor
4. AbilityType enum expands to include all new ability types

**Pros:**
- Minimal change to existing code
- No external file loading needed
- Type-safe, compile-time checks
- Easy to test

**Cons:**
- Ability definitions live in C# code, not data files
- Adding new abilities requires code change

**Verdict: Best for this project.** We're a console game, not a modding-friendly RPG. Data-driven abilities (Option B) add complexity without benefit. The game has 15 abilities total — hardcoding is fine.

### Option B: JSON-driven ability data (REJECTED)

Would require:
- `Data/abilities.json` with ability definitions
- Ability loader similar to `EnemyFactory.Initialize()`
- Runtime reflection or switch-case for ability effect execution
- Significant refactor of AbilityManager

**Why rejected:** Overkill for 15 abilities. Harder to implement conditional ability effects (e.g., "if enemy is stunned"). The game doesn't need moddable abilities.

### Option C: Per-class `IAbilitySet` interface (REJECTED)

Would require:
- `IAbilitySet` interface
- `WarriorAbilitySet`, `MageAbilitySet`, `RogueAbilitySet` implementations
- Factory to resolve ability set from PlayerClass

**Why rejected:** Over-engineered. The AbilityManager already handles ability registration and resolution. Adding interface indirection provides no testability benefit here.

---

## 4. New Systems Required

### 4.1 Combo Points (Rogue-only)

Add to Player model:
```csharp
public int ComboPoints { get; private set; } = 0;
public void AddComboPoints(int amount) { ComboPoints = Math.Min(5, ComboPoints + amount); }
public int SpendComboPoints() { var pts = ComboPoints; ComboPoints = 0; return pts; }
public void ResetComboPoints() { ComboPoints = 0; } // Called on combat end
```

### 4.2 Slow Status Effect

Add to `StatusEffect` enum:
```csharp
Slow // -25% damage dealt, enemy attacks last in turn order
```

Modify `StatusEffectManager.GetStatModifier()` to reduce Attack by 25% when Slow is active.

### 4.3 Mana Shield Toggle State

Add to Player model:
```csharp
public bool IsManaShieldActive { get; set; } = false;
```

CombatEngine checks this before applying damage and drains mana instead.

### 4.4 Expanded AbilityType Enum

Current:
```csharp
PowerStrike, DefensiveStance, PoisonDart, SecondWind
```

New:
```csharp
// Warrior
ShieldBash, BattleCry, Fortify, RecklessBlow, LastStand,
// Mage
ArcaneBolt, FrostNova, ManaShield, ArcaneSacrifice, Meteor,
// Rogue
QuickStrike, Backstab, Evade, Flurry, Assassinate
```

---

## 5. Work Item Breakdown

### Phase 1: Foundation (must complete first)

| WI | Title | Owner | Est | Depends On |
|----|-------|-------|-----|------------|
| WI-1 | Add `PlayerClass? ClassRestriction` to Ability model | Hill | 0.5h | — |
| WI-2 | Add Combo Points to Player model | Hill | 0.5h | — |
| WI-3 | Add Slow status effect to StatusEffect enum | Hill | 0.25h | — |
| WI-4 | Add ManaShield toggle state to Player model | Hill | 0.25h | — |
| WI-5 | Expand AbilityType enum with all 15 new types | Hill | 0.5h | — |

**Phase 1 can be parallelized:** WI-1 through WI-5 are independent data model changes.

---

### Phase 2: AbilityManager Refactor

| WI | Title | Owner | Est | Depends On |
|----|-------|-------|-----|------------|
| WI-6 | AbilityManager: Filter abilities by ClassRestriction | Barton | 1h | WI-1 |
| WI-7 | AbilityManager: Register all Warrior abilities | Barton | 1.5h | WI-5, WI-6 |
| WI-8 | AbilityManager: Register all Mage abilities | Barton | 1.5h | WI-5, WI-6 |
| WI-9 | AbilityManager: Register all Rogue abilities | Barton | 1.5h | WI-5, WI-6 |

**WI-7, WI-8, WI-9 can be parallelized** once WI-6 is complete.

---

### Phase 3: Ability Effect Implementation

| WI | Title | Owner | Est | Depends On |
|----|-------|-------|-----|------------|
| WI-10 | Implement Warrior ability effects in UseAbility switch | Barton | 2h | WI-7 |
| WI-11 | Implement Mage ability effects (including Mana Shield) | Barton | 2.5h | WI-8, WI-4 |
| WI-12 | Implement Rogue ability effects (including Combo Points) | Barton | 2.5h | WI-9, WI-2 |
| WI-13 | Implement Slow status effect in StatusEffectManager | Barton | 1h | WI-3 |

**WI-10, WI-11, WI-12 can be parallelized.** WI-13 can run in parallel with any of them.

---

### Phase 4: SkillTree Refactor

| WI | Title | Owner | Est | Depends On |
|----|-------|-------|-----|------------|
| WI-14 | Refactor SkillTree to support class-specific skill pools | Hill | 1.5h | — |
| WI-15 | Add Warrior passive skills | Hill | 1h | WI-14 |
| WI-16 | Add Mage passive skills | Hill | 1h | WI-14 |
| WI-17 | Add Rogue passive skills | Hill | 1h | WI-14 |

**WI-15, WI-16, WI-17 can be parallelized** once WI-14 is complete.

---

### Phase 5: Display & Narration

| WI | Title | Owner | Est | Depends On |
|----|-------|-------|-----|------------|
| WI-18 | Update ShowCombatMenu to show class-specific abilities | Hill | 1h | WI-7, WI-8, WI-9 |
| WI-19 | Update character sheet to show Combo Points (Rogue) | Hill | 0.5h | WI-2 |
| WI-20 | Write flavor text for all 15 abilities | Fury | 2h | WI-7, WI-8, WI-9 |
| WI-21 | Write passive skill descriptions | Fury | 1h | WI-15, WI-16, WI-17 |

---

### Phase 6: Testing

| WI | Title | Owner | Est | Depends On |
|----|-------|-------|-----|------------|
| WI-22 | Unit tests: Ability class restriction filtering | Romanoff | 1h | WI-6 |
| WI-23 | Unit tests: Warrior ability effects | Romanoff | 1.5h | WI-10 |
| WI-24 | Unit tests: Mage ability effects (incl. Mana Shield) | Romanoff | 1.5h | WI-11 |
| WI-25 | Unit tests: Rogue ability effects (incl. Combo Points) | Romanoff | 1.5h | WI-12 |
| WI-26 | Unit tests: Class-specific passive skills | Romanoff | 1h | WI-15, WI-16, WI-17 |
| WI-27 | Integration test: Full combat flow per class | Romanoff | 2h | WI-22–WI-26 |

---

## 6. Critical Path

```
WI-1 ──┬─→ WI-6 ──┬─→ WI-7 → WI-10 → WI-23 ──┐
WI-5 ──┘         ├─→ WI-8 → WI-11 → WI-24 ──┼─→ WI-27
                 └─→ WI-9 → WI-12 → WI-25 ──┘
                           ↓
                        WI-18, WI-19, WI-20
```

**Minimum time to completion:** ~3-4 work sessions assuming parallelization.

---

## 7. Risks & Mitigations

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Combo Points reset on flee** | Low | CombatEngine already calls cleanup on flee — add `player.ResetComboPoints()` |
| **Mana Shield infinite loop with 0 mana** | High | Check mana > 0 before draining; auto-disable shield when mana hits 0 |
| **Last Stand + Fortify too strong** | Medium | Playtesting required; can tune duration/magnitude |
| **Execute abilities (Meteor, Assassinate) trivialize bosses** | Medium | Add `IsImmuneToExecute` flag to DungeonBoss; already has `IsImmuneToEffects` pattern |
| **Slow status + Rogue passive stacking** | Medium | Balance during playtesting; cap damage bonus at +100% |
| **Save/Load compatibility** | Medium | ComboPoints, ManaShieldActive must be persisted; add to SaveData model |
| **Prestige bonuses interact with abilities** | Low | Prestige only affects base stats, not ability scaling — should be fine |
| **Items that interact with abilities** | Medium | Out of scope for v1; can add `AbilityPowerBonus` to items later |

---

## 8. Scope Boundary — Explicitly Excluded

The following are **NOT** part of this feature to keep scope contained:

1. ❌ **Class-specific items** (e.g., "Warrior-only armor") — separate feature
2. ❌ **Multi-class or hybrid builds** — each player is exactly one class
3. ❌ **Ability upgrades or talent trees** — abilities are fixed per level
4. ❌ **Enemy abilities that mirror player abilities** — enemies keep existing behavior
5. ❌ **PvP or multiplayer considerations** — single-player game
6. ❌ **Achievement integration** — can be added later ("Use Assassinate to execute 10 enemies")
7. ❌ **Tutorial for new abilities** — existing tutorial system doesn't need changes
8. ❌ **Respec / ability reassignment** — not planned
9. ❌ **Data-driven ability definitions** — hardcoded in AbilityManager
10. ❌ **Visual ability effects / ASCII animations** — abilities just produce text output

---

## 9. Recommendation

**Proceed with this design.** The class differentiation system:

- ✅ Gives each class 5 unique, mechanically interesting abilities
- ✅ Creates distinct playstyles (tank/sustain, burst/mana, combo/execute)
- ✅ Uses existing architecture patterns (status effects, ability manager)
- ✅ Requires only 2 new mechanics (Combo Points, Slow status)
- ✅ Has clear work item decomposition with parallel execution opportunities
- ✅ Risks are well-understood with mitigations

**Estimated total effort:** ~25-30 hours across Hill, Barton, Romanoff, and Fury.

---

## Approval

Awaiting Anthony's approval before creating GitHub issues and beginning implementation.
