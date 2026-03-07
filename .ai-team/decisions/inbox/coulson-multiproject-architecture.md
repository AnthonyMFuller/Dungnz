# Architecture Decision: Multi-Project Solution Split

**Date:** 2026-03-06  
**Author:** Coulson (Lead)  
**Status:** Proposed  
**Label:** architecture  

---

## Context

The solution is currently a single executable `Dungnz.csproj` containing all source code across five logical folders (`Models/`, `Systems/`, `Display/`, `Engine/`, `Data/`). A test project `Dungnz.Tests.csproj` references the monolith directly. This structure works but couples all layers at the build level, makes NuGet dependency ownership unclear, and prevents individual layers from being compiled, tested, or reasoned about in isolation.

The goal is to split into separate class library projects that enforce the existing logical boundaries at the compiler level.

---

## Target Architecture

### Project Dependency Graph (acyclic, top-down)

```
Dungnz (exe)
  └─ Dungnz.Engine
       ├─ Dungnz.Display
       │    ├─ Dungnz.Systems
       │    │    ├─ Dungnz.Data
       │    │    │    └─ Dungnz.Models  ← zero deps
       │    │    └─ Dungnz.Models
       │    └─ Dungnz.Models
       ├─ Dungnz.Systems
       ├─ Dungnz.Data
       └─ Dungnz.Models
```

### Project Responsibilities

| Project | Source Folders | NuGet Packages | Notes |
|---|---|---|---|
| `Dungnz.Models` | `Models/` + interfaces moved here | none | Zero external deps. Pure domain + contracts. |
| `Dungnz.Data` | `Data/*.cs` | none | Static data arrays. JSON files stay in Dungnz (exe). |
| `Dungnz.Systems` | `Systems/` (incl. `Enemies/`) | Microsoft.Extensions.Logging, NJsonSchema | All game logic systems. |
| `Dungnz.Display` | `Display/` (incl. `Spectre/`) | Spectre.Console | All rendering implementations. |
| `Dungnz.Engine` | `Engine/` (incl. `Commands/`) | Microsoft.Extensions.Logging | Orchestration: GameLoop, CombatEngine, DungeonGenerator. |
| `Dungnz` (exe) | `Program.cs`, `Data/*.json` | Serilog.Extensions.Logging, Serilog.Sinks.File, Microsoft.Extensions.Logging.Console | Composition root only. |

---

## Circular Dependencies to Resolve Before Split

The current monolith hides three circular dependency groups. These must be resolved before the physical project split can succeed.

### Circular 1: Display ↔ Engine (IDisplayService ↔ StartupMenuOption)

- `Display/IDisplayService.cs` imports `Dungnz.Engine` for `StartupMenuOption`
- `Engine/GameLoop.cs` imports `Dungnz.Display` for `IDisplayService`

**Resolution:** Move `IDisplayService`, `IInputReader`, `IMenuNavigator`, and `StartupMenuOption` from their current locations into `Models/`. These are interface contracts and enums — they belong in the domain layer. All consumers update their `using` directives; no logic changes.

### Circular 2: Systems ↔ Display (IDisplayService)

- Five files in `Systems/` import `Dungnz.Display` solely for `IDisplayService`:
  `AbilityManager`, `EquipmentManager`, `InventoryManager`, `PassiveEffectProcessor`, `StatusEffectManager`
- `Display/SpectreDisplayService` and `SpectreLayoutDisplayService` import `Dungnz.Systems`

**Resolution:** Covered by Circular 1 fix. Once `IDisplayService` lives in `Models/`, Systems references `Dungnz.Models` (already present) not `Dungnz.Display`. The Display→Systems direction is legitimate (Display renders Systems data) and becomes a one-way dependency.

### Circular 3: Models ↔ Systems.Enemies (JsonDerivedType)

- `Models/Enemy.cs` has 30+ compile-time `[JsonDerivedType(typeof(Goblin), "goblin")]` attributes
- All referenced types (`Goblin`, `Skeleton`, etc.) live in `Systems/Enemies/`
- Those enemy types reference `Dungnz.Systems` for `EnemyConfig`/`ItemConfig`

This creates a genuine circular dependency chain:  
`Models → Systems.Enemies → Systems → Models`

**Resolution:** Replace compile-time `[JsonDerivedType]` attributes with runtime JSON type registration via `JsonSerializerOptions` + `DefaultJsonTypeInfoResolver`. A new `Engine/EnemyTypeRegistry.cs` builds the configured options (Engine can see all layers). `SaveSystem` uses these options. The existing architectural test `AllEnemySubclasses_MustHave_JsonDerivedTypeAttribute` is replaced with a test verifying the runtime registry covers all concrete `Enemy` subclasses via reflection.

---

## InternalsVisibleTo Strategy

Once split, each class library that exposes `internal` members used by tests must declare:

```xml
<AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
  <_Parameter1>Dungnz.Tests</_Parameter1>
</AssemblyAttribute>
```

This applies to: `Dungnz.Models`, `Dungnz.Systems`, `Dungnz.Display`, `Dungnz.Engine`. (`Dungnz.Data` is likely all-public static classes, but should be evaluated.)

---

## Architecture Test Updates

Both test files currently load only `typeof(GameLoop).Assembly`. After the split, ArchUnitNET must load all relevant assemblies:

```csharp
new ArchLoader().LoadAssemblies(
    typeof(GameLoop).Assembly,          // Dungnz.Engine
    typeof(Player).Assembly,            // Dungnz.Models
    typeof(InventoryManager).Assembly,  // Dungnz.Systems
    typeof(IDisplayService).Assembly    // Dungnz.Models (after interface move)
).Build();
```

The reflection-based tests in `Architecture/ArchitectureTests.cs` that call `typeof(GameLoop).Assembly.GetTypes()` must also be updated to aggregate types from all assemblies.

---

## Sequencing Rationale

The split is done layer-by-layer from the bottom up (most independent first):

1. **Scaffolding** — project files created, solution updated, no code moves. Build green.
2. **Interface moves** — break Display↔Engine and Systems↔Display circular deps. All in monolith, no project boundary crossings yet.
3. **JSON refactor** — break Models↔Systems.Enemies circular dep. Runtime registration pattern.
4. **Extract Models** — now has zero external deps, safe to isolate.
5. **Extract Data** — only depends on Models.
6. **Extract Systems** — depends on Models+Data.
7. **Extract Display** — depends on Models+Systems.
8. **Extract Engine** — depends on all of the above.
9. **Thin executable** — Program.cs only, Serilog composition root.
10. **Test updates** — multi-assembly ArchUnitNET, project references, InternalsVisibleTo.

Every step keeps the solution building and all tests passing.

---

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Runtime JSON registration misses an enemy subclass | HIGH | Replace attribute test with a reflection-based registry completeness test |
| InternalsVisibleTo missing on a library causes test compile errors | MEDIUM | Add attribute to all 4 library projects as part of each extraction issue |
| ArchUnitNET multi-assembly loading changes rule behaviour | MEDIUM | Run architecture tests after each extraction; fix incrementally in Issue 10 |
| NuGet package placement — a library gets a dep it shouldn't | LOW | Each csproj is independently reviewed at acceptance |
| `CombatEngine` (1,709 lines) makes Engine extraction high-risk | LOW | No logic changes during extraction — file moves only |
| `Data/*.json` files must still CopyToOutputDirectory in executable | LOW | Explicitly kept in Dungnz.csproj Content item |

---

## Issues Created

| # | Title |
|---|---|
| [#1187](https://github.com/AnthonyMFuller/Dungnz/issues/1187) | Create multi-project class library scaffolding |
| [#1188](https://github.com/AnthonyMFuller/Dungnz/issues/1188) | Resolve circular dep — move interface contracts to Models layer |
| [#1189](https://github.com/AnthonyMFuller/Dungnz/issues/1189) | Resolve circular dep — replace JsonDerivedType attributes with runtime enemy type registration |
| [#1190](https://github.com/AnthonyMFuller/Dungnz/issues/1190) | Extract Dungnz.Models class library |
| [#1191](https://github.com/AnthonyMFuller/Dungnz/issues/1191) | Extract Dungnz.Data class library |
| [#1192](https://github.com/AnthonyMFuller/Dungnz/issues/1192) | Extract Dungnz.Systems class library |
| [#1193](https://github.com/AnthonyMFuller/Dungnz/issues/1193) | Extract Dungnz.Display class library |
| [#1194](https://github.com/AnthonyMFuller/Dungnz/issues/1194) | Extract Dungnz.Engine class library |
| [#1195](https://github.com/AnthonyMFuller/Dungnz/issues/1195) | Finalize Dungnz.csproj as thin executable entry point |
| [#1196](https://github.com/AnthonyMFuller/Dungnz/issues/1196) | Update Dungnz.Tests for multi-project solution |
