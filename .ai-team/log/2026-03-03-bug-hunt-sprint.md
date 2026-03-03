# Bug Hunt Sprint — 2026-03-03

**Requested by:** Boss  
**Team:** Coulson, Hill, Barton, Romanoff  
**Duration:** Full codebase bug hunt  

## Summary

Comprehensive audit of Engine/, Models/, Systems/, and Display/ directories. 49 GitHub issues created (#916–#964). 7 PRs merged (#965–#971) fixing P0 combat bugs and P1 reliability issues.

## Key Fixes Merged

1. **#916** — Mana Shield damage calculation inverted (Barton)
2. **#917** — BlockChanceBonus can exceed 100% without cap (Barton)
3. **#920** — Flurry and Assassinate never set cooldown (Barton)
4. **#923** — Overcharge passive permanently active (Barton)
5. **#928** — GameLoop null! fields risky without constructor validation (Hill)
6. **#929** — Silent exception swallowing in SaveSystem/PrestigeSystem (Coulson)
7. **#930** — Console.WriteLine in PrestigeSystem violates separation of concerns (Coulson)

## Issues Created

**P0: Gameplay-breaking (11 issues)**
- Mana Shield formula, block/dodge caps, ability cooldowns, special ability state resets

**P1: Tech debt & reliability (18 issues)**
- Null safety, exception handling, encapsulation violations, mutable collections

**P2: Test gaps & quality (20 issues)**
- Command handlers, narration systems, edge cases, hardcoded values

## Findings by Agent

| Agent | P0 | P1 | P2 | Notes |
|-------|----|----|----|----|
| Coulson | 0 | 4 | 7 | Architectural audit, encapsulation, silent failures |
| Hill | 2 | 7 | 5 | Null safety, game loop risks, duplication |
| Barton | 8 | 3 | 2 | Combat balancing, unbounded bonuses, HP mutations |
| Romanoff | 0 | 0 | 11 | Test coverage gaps, untested handlers, edge cases |

## Root Causes Identified

1. **Unbounded bonus stacking** — Block, Dodge, DefReduction, HolyDamage lack 95% caps
2. **Direct HP mutations** — AbilityManager bypasses validation
3. **Null safety gaps** — GameLoop null!, FirstOrDefault unchecked
4. **Encapsulation violations** — Public mutable collections, magic strings
5. **Test coverage holes** — 30% of implementation untested
6. **Silent failures** — Catch-all exception handlers, no logging

## Next Steps

1. **Phase 0 (P0 fixes):** 80–120 hours, Barton primary
2. **Phase 1 (P1 reliability):** 40–60 hours, Hill + Coulson concurrent
3. **Phase 2 (P2 quality):** 60–100 hours, Romanoff + Coulson
