# Dungnz Test Suite — Naming Convention and Organization

## Quick Reference

**Test File Naming:** `{SystemUnderTest}Tests.cs`  
**Location:** Mirror production code — test directories should match production directories

---

## Naming Convention

### Primary Rule: `{SystemUnderTest}Tests.cs`

Each test file tests a single system.

**Examples:**
- `CombatEngineTests.cs` → tests `CombatEngine` class
- `PlayerTests.cs` → tests `Player` class
- `EnemyFactoryTests.cs` → tests `EnemyFactory` class

### Supplementary Suffixes

When one system requires multiple test files, use descriptive suffixes:

| Suffix | Purpose | Example |
|--------|---------|---------|
| `Additional` | Edge cases, extended scenarios | `EquipmentManagerAdditionalTests.cs` |
| `Regression` | Previously fixed bugs | `AlignmentRegressionTests.cs` |
| `Coverage` | Coverage gaps, branch testing | `EnemyFactoryCoverageTests.cs` |
| `Fuzzy` | Property-based/fuzz tests | `EquipmentManagerFuzzyTests.cs` |
| `Simulation` | Statistical/balance tests | `CombatBalanceSimulationTests.cs` |

---

## Directory Structure

Test directories **MUST** mirror production code:

```
Dungnz/Engine/       → Dungnz.Tests/Engine/
Dungnz/Display/      → Dungnz.Tests/Display/
Dungnz/Systems/      → Dungnz.Tests/Systems/ (when needed)
Dungnz/Models/       → Dungnz.Tests/ (root; widely used)
```

**Current State:**
- ✅ Display/ and Engine/ tests already nested correctly
- ⚠️ Most system tests still in root (81 files)
- ✅ Helpers (Builders/, Helpers/, PropertyBased/, Snapshots/) not mirrored by design

---

## xUnit Collections

Collections enforce exclusive test execution when tests share mutable state.

**Current Collections:**
- `console-output` (12+ tests) — Console I/O isolation
- `EnemyFactory` (6 tests) — Enemy factory state
- `LootTableTests` (2 tests) — Loot distribution RNG
- `PrestigeTests` (7 tests) — Game loop prestige mode
- `save-system` (1 test) — Save file I/O
- `RunStatsFileIO` (1 test) — Run stats disk I/O

**When to Use:**
- When 2+ tests write to shared mutable state (files, static fields, console output)
- Tests in same collection run serially

---

## Test Structure — Arrange-Act-Assert

All unit tests follow AAA pattern:

```csharp
[Fact]
public void PlayerAttacks_EnemyLosesHP_CalculationCorrect()
{
    // ARRANGE
    var player = new Player { Attack = 10 };
    var enemy = new Enemy { HP = 50, Defense = 3 };
    
    // ACT
    player.Attack(enemy);
    
    // ASSERT
    Assert.Equal(expected: 47, actual: enemy.HP);
}
```

**Test Naming:** `{Method}_{Scenario}_{ExpectedOutcome}`

---

## Test Builders and Helpers

**Builders** (Builders/) — Fluent test object construction:
- PlayerBuilder.cs, EnemyBuilder.cs, ItemBuilder.cs, RoomBuilder.cs

**Helpers** (Helpers/) — Fakes, mocks, test utilities:
- FakeDisplayService.cs, FakeInputReader.cs, FakeMenuNavigator.cs
- TestDisplayService.cs, ControlledRandom.cs, EnemyFactoryFixture.cs

---

## Migration Plan

### Opportunistic Migration (Not Big-Bang)

Test files are renamed during normal churn, not in dedicated refactoring:

1. When editing a test file, check if filename matches convention
2. If not, rename it in the same PR
3. Update namespace if nested
4. Run full test suite to verify

**Phase-based tests** (Phase1DisplayTests, Phase6ClassAbilityTests, etc.):
- Keep for now (document delivery phases)
- Rename later when feature matures
- Low priority

**Vague names** (MiscCoverageTests, StaticContentCoverageTests):
- Split into focused tests when touched
- Don't force split immediately

### Golden Rule
**No PR should have renaming as its only purpose.**

---

## Current Test Inventory

**Total files:** 97  
**Root-level:** 81 files  
**Nested:** 6 files (Display/, Engine/, Architecture/, PropertyBased/, Snapshots/)  
**Helpers:** 10 files (Builders/, Helpers/)

**Status:** Most files already follow convention; no action needed for existing files.

---

## Checklist: Creating a New Test File

- [ ] File named `{SystemUnderTest}Tests.cs` or appropriate suffix
- [ ] Location mirrors production code
- [ ] Namespace correct (`Dungnz.Tests` root, `Dungnz.Tests.{Category}` nested)
- [ ] Tests use Arrange-Act-Assert structure
- [ ] Test names follow `{Method}_{Scenario}_{ExpectedOutcome}`
- [ ] Uses builders for complex setup
- [ ] Uses helpers for common mocks
- [ ] Collection added if tests modify shared state
- [ ] All tests pass individually and in full suite

---

## Key Principles

1. One test file per system
2. Suffixes clarify intent
3. Location mirrors production structure
4. Collections isolate shared state
5. Fix names opportunistically
6. Builders reduce test noise
7. Arrange-Act-Assert throughout

---

## References

- xUnit Collections: https://xunit.net/docs/shared-context
- Test Naming: Roy Osherove's *The Art of Unit Testing*
- Arrange-Act-Assert: https://xunit.net/docs/getting-started/xunit-tutorial
