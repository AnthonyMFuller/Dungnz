# Dungnz v2 Work Items Summary

**Generated:** 2026-02-20  
**Total Items:** 28 work items across 4 phases  
**Total Effort:** 78 engineering hours  

---

## Quick Reference

| Phase | Items | Hours | Owner(s) |
|-------|-------|-------|----------|
| Phase 0: Critical Refactoring | 8 | 14.5 | Hill, Barton |
| Phase 1: Test Infrastructure | 7 | 16.5 | Romanoff |
| Phase 2: Architecture | 7 | 22.0 | Hill, Barton |
| Phase 3: Features | 6 | 25.0 | Hill, Barton |

---

## Phase 0: Critical Refactoring (14.5h)

Must complete before any v2 feature work.

- **R1** (Hill, 2.0h): Extract IDisplayService Interface
- **R2** (Hill, 1.5h): Implement TestDisplayService
- **R3** (Hill, 3.0h): Refactor Player Encapsulation ⚠️ BREAKING
- **R4** (Barton, 2.5h): Make Random Injectable (IRandom)
- **R5** (Barton, 1.0h): Fix CombatEngine Input Coupling → depends on R1
- **R6** (Hill, 1.5h): Update GameLoop for Player Encapsulation → depends on R3
- **R7** (Barton, 1.0h): Update InventoryManager for Player Encapsulation → depends on R3
- **R8** (Barton, 2.0h): Update CombatEngine for Player Encapsulation → depends on R3, R4, R5

**Gate:** All architectural violations fixed, code compiles, manual smoke test passes

---

## Phase 1: Test Infrastructure (16.5h)

Build safety net for Phase 2/3 work.

- **T1** (Romanoff, 1.0h): Add xUnit Test Project → depends on R1-R8
- **T2** (Romanoff, 3.0h): Write Player Model Tests
- **T3** (Romanoff, 4.0h): Write CombatEngine Tests
- **T4** (Romanoff, 2.5h): Write InventoryManager Tests
- **T5** (Romanoff, 2.0h): Write LootTable Tests
- **T6** (Romanoff, 2.5h): Write DungeonGenerator Tests
- **T7** (Romanoff, 1.5h): Document Testing Patterns

**Gate:** ≥70% code coverage, all tests green, CI/CD configured

---

## Phase 2: Architecture (22h)

Optional but enables advanced features.

- **A1** (Hill, 3.0h): Introduce Game State Model → depends on R3, R6
- **A2** (Hill, 2.0h): Extract IGamePersistence Interface → depends on A1
- **A3** (Hill, 4.0h): Implement JsonGamePersistence → depends on A2
- **A4** (Barton, 3.0h): Add Event System → depends on R8
- **A5** (Barton, 2.5h): Refactor Enemy Factory Pattern → depends on R4
- **A6** (Hill, 3.5h): Introduce Configuration System
- **A7** (Hill, 4.0h): Separate Engine from UI → depends on R1, R5

**Gate:** GameState serializable, config-driven balance, GameLoop decoupled from Console

---

## Phase 3: Features (25h)

User-facing features enabled by stable foundation.

- **F1** (Hill, 3.0h): Implement Save/Load Commands → depends on A1, A2, A3
- **F2** (Barton, 4.0h): Add Equipment Slots → depends on R3, R7
- **F3** (Barton, 5.0h): Add Status Effects → depends on R8, A4
- **F4** (Hill, 2.5h): Add Permadeath Mode → depends on F1
- **F5** (Hill, 6.0h): Add Multi-Floor Dungeons → depends on A1, A5
- **F6** (Barton, 4.5h): Add Item Crafting → depends on F2

**Gate:** All features tested, no regressions to v1 functionality

---

## Critical Path (Longest Dependency Chain)

```
R3 (Player Encapsulation, 3h)
  → R8 (Update CombatEngine, 2h)
    → T1 (xUnit Project, 1h)
      → T3 (CombatEngine Tests, 4h)
        → A1 (GameState Model, 3h)
          → A2 (IGamePersistence, 2h)
            → A3 (JsonGamePersistence, 4h)
              → F1 (Save/Load, 3h)

Total critical path: 22 hours
```

All other work can be parallelized around this critical path.

---

## Parallel Work Opportunities

**Can run in parallel:**
- R1 (IDisplayService) + R3 (Player Encapsulation) + R4 (Injectable Random)
- R2 (TestDisplayService) + R6 (GameLoop Update) + R7 (InventoryManager Update)
- T2 (Player Tests) + T4 (InventoryManager Tests) + T5 (LootTable Tests) + T6 (Dungeon Tests)
- A4 (Event System) + A5 (Enemy Factory) + A6 (Configuration)
- F2 (Equipment Slots) + F4 (Permadeath Mode)

**Hill's workload:** R1, R2, R3, R6 (Phase 0) → A1, A2, A3, A6, A7 (Phase 2) → F1, F4, F5 (Phase 3)  
**Barton's workload:** R4, R5, R7, R8 (Phase 0) → A4, A5 (Phase 2) → F2, F3, F6 (Phase 3)  
**Romanoff's workload:** T1-T7 (Phase 1 only)

---

## Risk Flags

⚠️ **High Risk:**
- **R3 (Player Encapsulation):** Breaking change, updates ~30+ call sites across codebase
- **A3 (JSON Persistence):** Save file corruption risk, needs schema versioning

⚡ **Medium Risk:**
- **T1 (xUnit Setup):** May delay Phase 1 if framework issues arise
- **F5 (Multi-Floor Dungeons):** Complex refactor of DungeonGenerator, 6 hours estimate may underrun

✅ **Low Risk:**
- All Phase 0 interface extractions (mechanical refactoring, compiler enforces correctness)
- All Phase 1 test writing (no production code changes)

---

## Next Actions

1. **Coulson:** Present v2 plan to team, get approval
2. **Hill:** Review R1, R2, R3, R6 work items, estimate confidence
3. **Barton:** Review R4, R5, R7, R8 work items, flag concerns
4. **Romanoff:** Review T1-T7 test strategy, confirm 70% coverage target achievable
5. **ALL:** Attend Phase 0 kickoff ceremony (design review for R1, R3, R4)

---

## Full Plan

See `.ai-team/plans/v2-architecture-plan.md` for:
- Detailed work item descriptions
- Code examples for each refactor
- Acceptance criteria per phase
- C# pattern recommendations
- Risk assessment matrix
