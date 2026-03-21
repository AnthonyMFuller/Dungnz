# Decision: Regression Wave 2 — ViewModel + Console Regression Tests

**Author:** Romanoff (QA)  
**Date:** 2025-07-18  
**Status:** Proposed  

## Context

Wave 1 delivered interface conformance + MapRenderer tests (36 tests, merged). Wave 2 adds two
work items: WI-R04 (ViewModel headless tests) and WI-R06 (console regression suite).

## Decision

1. **Avalonia ProjectReference in test project** — Added `Dungnz.Display.Avalonia.csproj` as a
   ProjectReference to `Dungnz.Tests.csproj`. No AXAML generator conflicts observed.

2. **ViewModel tests are headless** — CommunityToolkit.Mvvm `ObservableObject` requires zero
   Avalonia runtime. ViewModels instantiate, mutate, and fire PropertyChanged like any POCO.

3. **Console regression tests reuse existing fakes** — No new test infrastructure needed.
   `FakeDisplayService`, `FakeInputReader`, and `ControlledRandom` cover all paths.

4. **54 new tests total** — 44 ViewModel + 10 console regression. Suite grows from 2,190 → 2,244.

## Consequences

- Any future ViewModel additions should have corresponding tests in `Dungnz.Tests/ViewModels/`
- Console regression suite can be extended for new game systems without additional infrastructure
- The Avalonia ProjectReference means test builds pull in Avalonia NuGet packages (adds ~2s build time)
