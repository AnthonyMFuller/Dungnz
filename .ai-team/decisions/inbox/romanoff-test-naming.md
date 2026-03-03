# Decision: Test File Naming Convention — Issue #884

**Date:** 2026-03-02  
**Proposed by:** Romanoff (Tester)  
**Status:** Ready for Review

## Problem

The test suite (97 files) uses inconsistent naming conventions. Navigation becomes harder as the suite grows.

## Solution

Adopt naming convention and opportunistic migration strategy.

### Primary Rule: `{SystemUnderTest}Tests.cs`

Each test file tests exactly one system.

### Supplementary Suffixes

| Suffix | Purpose | Example |
|--------|---------|---------|
| Additional | Edge cases | EquipmentManagerAdditionalTests.cs |
| Regression | Fixed bugs | AlignmentRegressionTests.cs |
| Coverage | Coverage gaps | EnemyFactoryCoverageTests.cs |
| Fuzzy | Property-based tests | EquipmentManagerFuzzyTests.cs |
| Simulation | Balance tests | CombatBalanceSimulationTests.cs |

### Directory Structure — Mirror Production

```
Dungnz/Engine/       → Dungnz.Tests/Engine/
Dungnz/Display/      → Dungnz.Tests/Display/
```

### xUnit Collections

**Current Collections:**
- console-output (12+ tests)
- EnemyFactory (6 tests)
- LootTableTests (2 tests)
- PrestigeTests (7 tests)
- save-system (1 test)
- RunStatsFileIO (1 test)

### Migration Strategy

Files renamed during normal churn, not in dedicated refactoring PR.

**Golden Rule:** No PR should have renaming as its only purpose.

## Current Inventory

**Total:** 97 files  
**Root-level:** 81 files (mostly correct)  
**Nested:** 6 files (Display/, Engine/, Architecture/, PropertyBased/, Snapshots/)  
**Helpers:** 10 files (Builders/, Helpers/)

**Status:** Most files already follow convention.

## Impact

- **Zero code changes** — Documentation only
- **Zero test changes** — All existing files keep current names
- **Zero build impact** — New markdown files
- **Immediate adoption** — New test files follow convention
- **Gradual improvement** — Files improve opportunistically

## Next Steps

1. Commit TESTING.md, SKILL.md, and decision document
2. Begin opportunistic migration on next edits
3. Monitor adoption
