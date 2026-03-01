# Architecture Violations Found — Sprint 3

**Date:** 2026-03-01
**Agent:** Romanoff (Quality & Testing)
**Source:** Architecture tests in `Dungnz.Tests/Architecture/ArchitectureTests.cs` + existing `Dungnz.Tests/ArchitectureTests.cs`

---

## Violation 1: Models → Systems Dependency

**Test:** `Models_Must_Not_Depend_On_Systems` (ArchUnitNET)
**Status:** FAILING (pre-existing tech debt)

**Violations Found:**
- `Dungnz.Models.Enemy` → `Dungnz.Systems.Enemies.*` (via `[JsonDerivedType]` attributes on Enemy base class)
- `Dungnz.Models.Merchant` → `Dungnz.Systems.MerchantInventoryConfig`
- `Dungnz.Models.Player` → `Dungnz.Systems.SkillTree` and `Dungnz.Systems.Skill`

**Root Cause:** The `[JsonDerivedType]` attributes on `Enemy` reference concrete enemy subclasses in `Systems.Enemies` namespace, creating an upward dependency from the domain model to the systems layer. Similarly, `Merchant` references `MerchantInventoryConfig` and `Player` references `SkillTree`/`Skill`.

**Recommended Fix:**
1. Move enemy subclass registrations to a JSON serialization configuration class in `Systems/` rather than as attributes on the `Enemy` base class
2. Move `MerchantInventoryConfig` to `Models/` or use an interface in Models
3. Move `SkillTree`/`Skill` to `Models/` or decouple via interface
4. Alternative: Accept a "shared" layer where Models can reference Systems types used for serialization config

---

## Violation 2: GenericEnemy Missing `[JsonDerivedType]`

**Test:** `AllEnemySubclasses_MustHave_JsonDerivedTypeAttribute`
**Status:** FAILING (pre-existing tech debt)

**Violation:** `GenericEnemy` in `Systems.Enemies` is a concrete `Enemy` subclass but lacks a `[JsonDerivedType]` registration on the `Enemy` base class.

**Root Cause:** `GenericEnemy` was added as a data-driven enemy type but the `[JsonDerivedType]` attribute was not added to `Enemy.cs`.

**Recommended Fix:** Add `[JsonDerivedType(typeof(GenericEnemy), "genericenemy")]` to the `Enemy` base class.

---

## Violation 3: Display Namespace Uses Raw Console

**Test:** `Display_Should_Not_Depend_On_System_Console` (IL scanning)
**Status:** FAILING (pre-existing tech debt)

**Violations Found (sample):**
- `ConsoleDisplayService.ShowTitle` → `Console.Clear`
- `ConsoleDisplayService.ShowRoom` → `Console.WriteLine`
- `ConsoleDisplayService.ShowCombat` → `Console.WriteLine`
- (40+ methods total)

**Root Cause:** `ConsoleDisplayService` is the original display implementation that directly calls `System.Console`. The intended architecture is for all display to go through Spectre Console or the `IDisplayService` abstraction.

**Recommended Fix:**
1. Migrate `ConsoleDisplayService` to use `SpectreDisplayService` internally, or
2. Create a `ConsoleWrapper` abstraction that `ConsoleDisplayService` uses instead of raw Console calls
3. This is a large refactor — should be a dedicated sprint item

---

## Violation 4: Engine Namespace Uses Console (via ConsoleInputReader)

**Test:** `Engine_Must_Not_Call_Console_Directly` (IL scanning)
**Status:** FAILING (pre-existing tech debt)

**Violations Found:**
- `ConsoleInputReader.ReadLine` → `Console.ReadLine`
- `ConsoleInputReader.ReadKey` → `Console.ReadKey`
- `ConsoleInputReader.get_IsInteractive` → `Console.get_IsInputRedirected`

**Root Cause:** `ConsoleInputReader` is the concrete `IInputReader` implementation and legitimately needs to call Console for input. However, the architecture rule says Engine types should not touch Console.

**Recommended Fix:**
1. Move `ConsoleInputReader` to a new `Dungnz.Infrastructure` namespace (adapter pattern)
2. Or accept `ConsoleInputReader` as a documented exception to the Engine-no-Console rule
3. Lowest effort: Add `[ExcludeFromArchitectureTest]` convention and exclude it from the IL scan

---

## Summary Table

| # | Violation | Severity | Fix Effort |
|---|-----------|----------|------------|
| 1 | Models→Systems dependency | High | Medium (move types or add interfaces) |
| 2 | GenericEnemy missing JsonDerivedType | Critical (save crash risk) | Trivial (one line) |
| 3 | Display uses raw Console | Medium | Large (full refactor to Spectre) |
| 4 | Engine ConsoleInputReader | Low | Small (move to Infrastructure namespace) |
