# Skill: Test File Naming Convention

## Pattern

### Primary Rule
`{SystemUnderTest}Tests.cs` — Each test file tests exactly one system.

### Supplementary Suffixes

| Suffix | Purpose | Example |
|--------|---------|---------|
| Additional | Edge cases | EquipmentManagerAdditionalTests.cs |
| Regression | Fixed bugs | AlignmentRegressionTests.cs |
| Coverage | Coverage gaps | EnemyFactoryCoverageTests.cs |
| Fuzzy | Property-based tests | EquipmentManagerFuzzyTests.cs |
| Simulation | Balance tests | CombatBalanceSimulationTests.cs |

## Directory Structure — Mirror Production

```
Dungnz/Engine/      → Dungnz.Tests/Engine/
Dungnz/Display/     → Dungnz.Tests/Display/
```

## xUnit Collections

Use when 2+ tests share mutable state (console, files, static fields).

**Current:** console-output, EnemyFactory, LootTableTests, PrestigeTests, save-system, RunStatsFileIO

## Test Structure — Arrange-Act-Assert

**Naming:** {Method}_{Scenario}_{ExpectedOutcome}

## Migration — Opportunistic

Files renamed during normal maintenance, never in dedicated refactoring PRs.

**Golden Rule:** No PR should have renaming as its only purpose.
