# Phased Refactoring Strategy for Legacy Codebases

**Confidence:** low  
**Source:** earned  
**Domain:** refactoring, architecture, technical-debt, project-management  
**Date:** 2026-02-20  

## Problem

Legacy codebases accumulate technical debt (no tests, tight coupling, leaky abstractions). Teams want to add new features but refactoring feels risky without safety nets. Common failure modes:

- **Big bang refactor:** Rewrite everything at once → high risk, long delay before value delivery
- **Refactor while shipping features:** Half-baked refactors break existing functionality
- **No refactoring:** Technical debt compounds, velocity slows to crawl

**Key insight:** Features require stable foundation. Phased approach delivers foundation first, features second.

## Pattern: 4-Phase Refactoring

### Phase 0: Critical Refactoring (Foundation)
**Goal:** Fix architectural violations, enable testability  
**Deliverables:** Interface extraction, encapsulation, injectable dependencies  
**Gate:** All code compiles, manual smoke test passes  
**Duration:** ~15-20% of total project time  

**Characteristics:**
- Breaking changes allowed (update all call sites mechanically)
- No new features
- Focus on enabling testability
- Strict scope: only changes that block testing or violate architecture

**Example work items:**
- Extract IDisplayService interface from concrete DisplayService
- Refactor Player public setters to private with mutation methods
- Make Random injectable (IRandom interface)

---

### Phase 1: Test Infrastructure (Safety Net)
**Goal:** Build comprehensive test suite  
**Deliverables:** Unit tests, integration tests, test documentation  
**Gate:** ≥70% code coverage on core systems, all tests green  
**Duration:** ~20-25% of total project time  

**Characteristics:**
- No production code changes (only test code)
- Depends on Phase 0 refactors (testability enabled)
- Focus on critical paths (happy path + edge cases)
- Tests become regression safety net for Phase 2/3

**Example work items:**
- Write CombatEngine tests (win, lose, flee, player death)
- Write Player model tests (TakeDamage, Heal, stat validation)
- Write LootTable tests (deterministic drops with seeded random)

---

### Phase 2: Architecture Improvements (Optional)
**Goal:** Enable advanced features, improve maintainability  
**Deliverables:** State management, persistence layer, configuration system  
**Gate:** All tests still pass, new architecture components tested  
**Duration:** ~25-30% of total project time  

**Characteristics:**
- Optional phase (skip if shipping pressure high)
- Enables advanced features (save/load, multiplayer, config-driven balance)
- Non-breaking changes (additive, not destructive)
- Tests ensure no regressions

**Example work items:**
- Extract GameState model (separates state from presentation)
- Implement JSON persistence (save/load games)
- Add event system (decouple systems, enable analytics)
- Configuration system (balance tuning without recompilation)

---

### Phase 3: Feature Development (Value Delivery)
**Goal:** Ship new features using stable foundation  
**Deliverables:** User-facing features  
**Gate:** All tests pass, features have test coverage  
**Duration:** ~30-40% of total project time  

**Characteristics:**
- High confidence due to test safety net
- Fast velocity (refactored architecture easy to extend)
- Low regression risk (tests catch breaking changes)
- Features enable further architecture improvements (virtuous cycle)

**Example work items:**
- Implement save/load commands
- Add equipment slots (weapon, armor, rings)
- Add status effects (poison, burning, stunned)
- Multi-floor dungeons with difficulty scaling

---

## Dependency Management

Track work items with dependencies using directed acyclic graph (DAG):

```
R1: IDisplayService interface
├─ R2: TestDisplayService (depends on R1)
├─ R5: Fix CombatEngine input (depends on R1)
└─ T1: Add xUnit project (depends on R1)

R3: Player encapsulation
├─ R6: Update GameLoop (depends on R3)
├─ R7: Update InventoryManager (depends on R3)
└─ R8: Update CombatEngine (depends on R3, R4, R5)
```

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Phase 0 breaks production code | Mechanical refactoring (search/replace), manual smoke tests |
| Test framework setup delays Phase 1 | Use familiar framework (xUnit, NUnit), dedicated setup work item |
| Phase 2 features creep into Phase 0 | Strict phase gates, code review enforcement |
| Features ship without tests in Phase 3 | Test coverage requirement in definition of done |

---

## When to Use This Pattern

✅ **Use phased refactoring when:**
- Codebase has high technical debt (no tests, tight coupling)
- Team wants to ship new features but foundation is unstable
- Long-term project (months/years of development ahead)

❌ **Don't use phased refactoring when:**
- Codebase is already well-tested and maintainable
- One-off prototype or throwaway code
- Immediate feature deadline (< 1 week)

---

## Real-World Example: Dungnz v2

**Context:** C# dungeon crawler, shipped v1 with no tests, tight Console coupling, public setters on Player model.

**Phase 0 (14.5 hours):** Interface extraction, Player encapsulation, injectable Random  
**Phase 1 (16.5 hours):** xUnit setup, >70% test coverage  
**Phase 2 (22 hours):** GameState model, JSON persistence, event system, config  
**Phase 3 (25 hours):** Save/load, equipment slots, status effects, multi-floor dungeons  

**Outcome:** 78 total hours, stable foundation, high-confidence feature development in Phase 3.

---

## Tags

refactoring, architecture, technical-debt, phased-approach, project-management, testing, legacy-code
