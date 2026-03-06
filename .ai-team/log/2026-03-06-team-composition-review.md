# 2026-03-06: Team Composition Review Session Log

**Requested by:** Boss (Anthony)

**Facilitator:** Coulson

**Participants:** Coulson conducted team composition analysis

**Session Focus:** Strategic review of squad structure, workload distribution, and role gaps

---

## Key Findings

### Team Composition Analysis
- Current 6-agent structure is 80% correct
- Three critical gaps identified:
  1. **No quality gate owner** — bugs caught reactively, not preventatively
  2. **Display layer exceeds Hill's capacity** — 2,163 LOC, 60+ bugs since 3/04
  3. **No gameplay design owner** — balance and feel are ad-hoc

### Recommendations Summary

**P0 (Immediate):**
- Promote **Romanoff** to QA Engineer (from Tester)
  - Add PR review mandate for test coverage
  - Enforce smoke test patterns before merge
  - Expected impact: 30–50% reduction in post-merge bugs

**P1 (Within 2 Weeks):**
- Trial **Barton** as Display Specialist
  - Take ownership of SpectreLayoutDisplayService (2,163 LOC)
  - Hill focuses on gameplay bugs instead
  - Measure display bug rate after 2 weeks
  - If successful, make permanent; if not, hire new Display Specialist

- Activate **Fury's** content pipeline
  - File 10–15 content issues (recipes, items, enemies, narration)
  - Fury utilization: 10% → 70%
  - Estimated: 2–3 weeks of work

**P2 (Defer 4–6 Weeks):**
- Add **Game Designer** role after P0/P1 bugs closed
  - Owns balance, progression, combat feel
  - Approves features before engineering
  - Playtests and files balance issues

### Workload Distribution (Last 3 Weeks)

| Agent | Primary Work | LOC Changed | Issue Count | Status |
|-------|--------------|-------------|-------------|--------|
| **Hill** | Display layer, menu input, ShowRoom() fixes | ~1,800 | 18 issues | ⚠️ Overloaded |
| **Barton** | Game-logic integration, input-coupled methods | ~600 | 8 issues | ✅ Balanced |
| **Romanoff** | Deep audits, regression tests, smoke tests | ~900 tests | 0 issues | ⚠️ Reactive only |
| **Fury** | Enemy lore (31 enemies), difficulty text | ~200 data | 1 issue | ⚠️ Underutilized |
| **Fitz** | CI optimization, release pipeline | ~150 YAML | 4 issues | ✅ Stable |

### Quality Pattern: ShowRoom() Bug Hunt

- 15+ command handlers missing ShowRoom() calls
- Root cause: No architectural enforcement; pattern convention failed
- Found through manual audit (not automated testing)
- Indicates need for preventative quality gates (Romanoff as QA Engineer)

### Display Layer Issues

- 60+ bugs since 3/04 TUI migration
- SpectreLayoutDisplayService: 2,163 LOC across 3 partial classes
- 54 IDisplayService methods, 20+ menu flows
- 0% test coverage (`[ExcludeFromCodeCoverage]`)
- Same problem as old GameLoop (1,635 LOC god-class)
- Solution: Decomposition OR dedicated Display Specialist

### Known P1 Gameplay Bugs (Unfixed)

- SetBonusManager 2-piece stat bonuses never applied
- Boss loot scaling broken (missing isBossRoom params)
- Enemy HP can go negative (no clamping)
- SoulHarvest dual implementation (inline + unused GameEventBus)

---

## Action Items

### Immediate (This Sprint)

1. **Coulson:** Update Romanoff's charter to QA Engineer
   - Add PR review requirement for all command handler PRs
   - Add "CAN block PRs if test coverage insufficient" boundary

2. **Coulson:** File content expansion issues for Fury
   - 7 additional crafting recipes
   - 5 merchant-exclusive items
   - 5 enemy variants (bosses, elites)
   - Narration for shrines, merchants, floor transitions

3. **Barton:** Take ownership of SpectreLayoutDisplayService (trial)
   - Claim #1075–#1087 display issues
   - Hill hands off display bugs

4. **Romanoff:** Add cancel-path smoke test template
   - Add to CommandHandlerSmokeTests.cs
   - Pattern: "inventory + cancel → assert ShowRoomCalled"

5. **Hill:** Focus on P1 gameplay bugs
   - SetBonusManager dead code
   - Boss loot scaling
   - HP clamping

### Follow-Up (After 2 Weeks)

6. **Measure** Barton's trial as Display Specialist
   - If display bugs drop below 3 per sprint → success, make permanent
   - If not → implement Recommendation 3 (hire new Display Specialist)

---

## Sign-Off

**Decision:** Update role charters to reflect recommendations above. Trial Barton as Display Specialist. Promote Romanoff to QA Engineer. Activate Fury's content pipeline. Defer Game Designer role until P0/P1 gameplay bugs close.

**Next Steps:** Coulson updates charters, files content issues, Barton takes display ownership, Romanoff adds smoke test template, Hill focuses on gameplay.

**Session Recorded by:** Scribe
**Date:** 2026-03-06
