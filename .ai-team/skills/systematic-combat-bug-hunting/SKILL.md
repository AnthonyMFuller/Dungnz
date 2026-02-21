# Systematic Combat Bug Hunting

**Category:** Game Systems / Quality Assurance  
**Confidence:** medium  
**Source:** earned  
**Agent:** Barton  
**Date:** 2026-02-20  

## Context

Game combat systems have complex state interactions: status effects, boss phases, turn timing, stat modifiers, special abilities. Bugs hide in edge cases where multiple systems interact. Traditional testing misses cross-system issues.

## Pattern

### Pre-v3 Bug Hunt Structure

When reviewing combat systems for bugs before major releases, use **layered analysis** approach:

1. **Data Flow Tracing:** Identify where values are calculated but never consumed
   - Example: `GetStatModifier()` implemented but never called in damage formulas
   - Look for "bridge" methods that should connect systems but don't

2. **State Transition Bugs:** Check flag/enum state changes for missing resets or double-application
   - Example: Boss `ChargeActive` flag not reset on dodge → sticks forever
   - Example: Boss enrage multiplies `Attack` instead of stored `_baseAttack` → compounds

3. **Timing Inconsistencies:** Verify turn order and event sequencing
   - Example: Boss enrage checked at turn start, not after damage → burst damage skips Phase 2
   - Example: Ambush logic runs before turn processing → skips status ticks

4. **Inverted Logic:** Check "who affects whom" in bidirectional systems
   - Example: Poison-on-hit in player attack method instead of enemy attack method
   - Example: Immunity check tests wrong entity (enemy immunity for player-targeted effect)

5. **Content Accessibility:** Verify spawning/generation systems use full content pools
   - Example: DungeonGenerator spawns 4 of 9 enemy types → half roster inaccessible

6. **Coupling Fragility:** Identify logic split across multiple classes for same feature
   - Example: Stun handled in both StatusEffectManager (message) and CombatEngine (skip turn)

### Bug Report Format

For each bug:
- **Title:** One-line description of symptom
- **File + Lines:** Exact location in codebase
- **Severity:** Critical/High/Medium/Low based on gameplay impact
- **Reproduction:** Step-by-step path to trigger bug
- **Suggested Fix:** Code-level solution with snippet
- **Notes:** Why it matters, how it affects future work

### Severity Classification

- **Critical:** Feature non-functional or inverted (status modifiers not applied, poison-on-hit backwards)
- **High:** Major content inaccessible or fragile coupling (5 enemy types never spawn, stun double-handling)
- **Medium:** Edge cases or timing issues (boss mechanics bugs, ambush timing)
- **Low:** Documentation mismatches, dead code, design limitations

## Example Application

**Project:** TextGame dungeon crawler pre-v3 review  
**Scope:** CombatEngine (389 lines), StatusEffectManager (131 lines), EnemyFactory, DungeonGenerator, 10 enemy types  
**Result:** 14 bugs found in 4 hours

**Critical Finds:**
- Status effect modifiers calculated but never applied (data flow tracing)
- GoblinShaman poison-on-hit inverted (inverted logic check)
- 5 of 9 enemy types never spawn (content accessibility check)

**Methodology:**
1. Read history.md for system architecture understanding
2. Trace damage formulas end-to-end (identified GetStatModifier() gap)
3. Review boss mechanics for state transition bugs (found enrage compounding, charge sticking)
4. Check turn timing for all special mechanics (found ambush, enrage, stun timing issues)
5. Grep for `Apply*OnHit` patterns (found inverted poison logic)
6. Compare enemy type lists in factory vs. generator (found spawn list mismatch)

## Anti-Patterns

❌ **Only test happy paths** — Bugs hide in edge cases (dodge during charge, heal during enrage)  
❌ **Trust that implemented features work** — Implemented ≠ integrated (GetStatModifier exists but unused)  
❌ **Assume content is accessible** — Generation logic may spawn subset (4 of 9 types)  
❌ **Skip timing analysis** — Turn order matters (enrage check at turn start vs. after damage)  
❌ **Ignore coupling fragility** — Split logic breaks when one side changes (stun in 2 places)

## When to Use

- **Pre-release audits** — Before major version (v2 → v3) to find inherited bugs
- **After system integration** — When new systems added (status effects + combat damage)
- **Before expanding systems** — Don't build boss variety on fragile boss mechanics
- **When content seems missing** — If 5 enemies never encountered, check spawn logic

## Related Skills

- `deterministic-random-testing` — How to test RNG-dependent bugs (charge chance, crit chance)
- `game-systems-balance-planning` — Understanding system interactions for bug patterns
- `dependency-injection-testability` — Why coupling fragility matters for testing

## Variations

- **Performance Review:** Focus on O(n²) loops, unnecessary recalculations
- **Security Review:** Focus on input validation, sanitization (less relevant for single-player games)
- **Save/Load Review:** Focus on state serialization gaps (flags not persisted)

## Notes

This skill is about **systematic thinking**, not checklists. Every project has different interaction patterns. The key is:

1. **Trace data flows** (calculated but not consumed)
2. **Check state transitions** (flags that stick, multipliers that compound)
3. **Verify timing** (when does X happen relative to Y)
4. **Question assumptions** (who affects whom, what's actually accessible)

Bug density: Typical finds are 5-10 bugs per 1000 lines in complex state machines like combat systems. Higher density (14 bugs in ~1000 lines here) suggests integration gaps between systems built separately.
