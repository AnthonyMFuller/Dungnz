# Skill: Game Systems Balance Planning

**Confidence:** low  
**Source:** earned  
**Scope:** Game development projects (RPGs, roguelikes, combat-focused games)

## What
A structured framework for proposing gameplay features, content expansions, and balance improvements for game systems (combat, progression, loot, enemies).

## When to Use
- Planning v2+ iterations of game systems
- Balancing combat mechanics and difficulty curves
- Proposing new features for RPG/dungeon crawler games
- Designing enemy variety and player progression

## Core Pattern

### 1. Feature Ranking Matrix (Impact × Effort)
Rank proposed features by:
- **Impact:** How much does this improve fun/replayability?
- **Effort:** Implementation complexity and dependencies
- Priority = HIGH Impact / LOW-MEDIUM Effort features first

Example:
- Status Effects (HIGH/MEDIUM) > Equipment Slots (LOW/MEDIUM)
- Critical Hits (MEDIUM/LOW) > Full Skill Tree (HIGH/HIGH)

### 2. Enemy Design Archetypes
Each enemy should have a **unique mechanical identity**:
- **Tank:** High HP, low ATK (Troll with regen)
- **Glass Cannon:** High ATK, low DEF (Wraith with dodge)
- **Status Applier:** Medium stats, applies debuffs (Goblin Shaman)
- **Counter-Required:** Mechanic that forces specific strategy (Stone Golem immune to status)

**Anti-pattern:** Don't create stat clones (Goblin 2 = Goblin 1 but +10 HP).

### 3. Balance Formula Documentation
Always provide **concrete stat formulas**, not vague descriptions:

```csharp
// Good: Actionable formula
var scaledHP = baseHP * (1.0 + (playerLevel - 1) * 0.12);

// Bad: Vague intent
// "Enemies should scale with player level"
```

Include:
- Damage calculations (before/after changes)
- Drop rates (exact percentages)
- XP curves (formula, not table)
- Stat growth per level

### 4. Content Expansion Structure
Organize by **system domain**, not implementation order:
- **New Enemies:** List with stats, special mechanics, drops
- **New Items:** Grouped by type (weapons/armor/consumables)
- **New Mechanics:** Status effects, skills, phases

**Why:** Designers/testers can review content completeness independently of code structure.

### 5. Counter-Play Design
Every powerful mechanic should have a **strategic counter**:
- Troll Regen → Poison (prevents healing) or Burst Damage
- Vampire Lifesteal → Weakened debuff or Smoke Bomb (flee)
- Boss Enrage → Defensive Stance or Stun

**Anti-pattern:** Stat-check bosses (you win if ATK > X, else you lose).

### 6. Economy Sinks
If adding currency/loot, **always propose spending outlets**:
- Shrines (pay gold for buffs/heals)
- Merchants (buy consumables)
- Upgrade systems (reroll loot stats)

**Why:** Prevents infinite resource accumulation, creates risk/reward decisions.

### 7. Implementation Phasing
Break work into **dependency-aware sprints**:
1. **Foundation Sprint:** Core systems (status effects, mana resource)
2. **Depth Sprint:** Advanced features (skills, combos)
3. **Content Sprint:** Enemies, items, balance tuning
4. **Polish Sprint:** Edge cases, QoL, economy

**Anti-pattern:** Building skills before status effects exist (blocked by dependencies).

## Example Application

**Bad Proposal:**
> "Add more enemies and make them harder. Maybe some skills?"

**Good Proposal:**
```markdown
## Priority 1: Status Effects (HIGH/MEDIUM)
- Poison: 3 dmg/turn, 3 turns
- Stun: Skip 1 turn
- Formula: foreach (effect in ActiveEffects) { ProcessEffect(effect); }

## Priority 2: Enemy — Stone Golem
- 70 HP, 8 ATK, 18 DEF
- Special: Immune to status effects
- Drops: Iron Skin Elixir (50%)
- Counter-Play: Requires sustained damage (no DOT cheese)

## Balance: Enemy Scaling Formula
scaled.HP = base.HP * (1.0 + (playerLevel - 1) * 0.12)
// Level 5 Goblin: 20 * 1.48 = 29.6 HP
```

## Why This Works
- **Specificity:** Engineers know exactly what to build
- **Justification:** Each mechanic has "Why" rationale
- **Balance:** Formulas can be tested/tuned before implementation
- **Prioritization:** Team knows what to build first

## Anti-Patterns to Avoid
❌ Proposing features without effort estimates  
❌ Enemy variety via stat tweaks only  
❌ No counter-play for powerful mechanics  
❌ Vague balance goals ("make it harder")  
❌ Gold/loot systems without sinks  
❌ Building dependent features out-of-order

## Context
Extracted from Dungnz v2 planning (C# dungeon crawler). Applied game design principles:
- Systems-driven design (data-driven enemies, loot tables)
- Combat should feel decisive (burst > attrition)
- Enemy variety over quantity (unique mechanics > stat clones)
- Every mechanic has counter-play

## Tags
game-design, balance, combat-systems, rpg-mechanics, feature-planning
