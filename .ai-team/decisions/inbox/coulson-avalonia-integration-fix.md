# Decision: Two-Executable Architecture for Avalonia GUI

**Date:** 2026-03-13  
**Decider:** Coulson (Lead Architect)  
**Status:** Proposed → pending Anthony approval  
**Context:** PR #1401 (P1+P2 Avalonia scaffold)

---

## Problem

The original Avalonia migration spec (docs/avalonia-migration-spec.md) assumed we could integrate the Avalonia GUI via a `--avalonia` CLI flag in the main `Dungnz.csproj` executable:

```csharp
// Program.cs (original spec)
var useAvalonia = args.Contains("--avalonia");

if (useAvalonia)
{
    var app = AvaloniaAppBuilder.Configure(args);
    app.RunGame(prestige);
}
else
{
    // ... Spectre / Console flow ...
}
```

This required `Dungnz.csproj` to reference `Dungnz.Display.Avalonia.csproj`:

```xml
<ProjectReference Include="Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj" />
```

### The Architectural Conflict

**Avalonia's XAML compiler (AvaloniaUI.Compiler) operates at build-time via source generators.** When a non-Avalonia project references an Avalonia project:

1. The parent project's build includes the Avalonia project's files in its compilation context
2. Avalonia's source generator sees `.axaml` files and tries to generate compiled bindings and controls
3. The generator expects the parent project to have Avalonia packages (`Avalonia`, `Avalonia.Desktop`, etc.)
4. When those packages are missing, the generator throws AVLN2000 errors:
   ```
   Avalonia error AVLN2000: Unable to resolve suitable regular or attached property Title on type ...
   Avalonia error AVLN2000: Index was out of range (ResolveContentPropertyTransformer)
   ```

**Attempted workarounds (all failed or fragile):**

- `<AvaloniaResource Remove="...">` — ignored by source generator (operates at different MSBuild phase)
- `<Compile Remove="...">` — excludes C# files, but doesn't prevent AXAML processing
- Conditional `<ProjectReference Condition="...">` — helps, but breaks IDE tooling and requires build configs

The fundamental issue: **You cannot half-reference an Avalonia project.** It's all or nothing.

---

## Decision

**Adopt a two-executable architecture:**

1. **`Dungnz.csproj`** remains the console/Spectre executable (current default experience)
   - Launches with `dotnet run` or `dotnet run --project Dungnz`
   - Uses `SpectreLayoutDisplayService` or `ConsoleDisplayService` (no Avalonia dependency)
   - No changes needed to existing Program.cs logic (except removal of commented `--avalonia` code)

2. **`Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj`** becomes an independent GUI executable
   - Change `<OutputType>` from `Library` to `Exe`
   - Add its own `Program.cs` that composes the game loop with `AvaloniaDisplayService`
   - Launches with `dotnet run --project Dungnz.Display.Avalonia`
   - Contains ALL Avalonia dependencies, XAML files, ViewModels, AvaloniaDisplayService
   - **References shared game logic:** Models, Engine, Systems, Data (no circular reference)

### Architecture Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                      Shared Game Logic                          │
│  Dungnz.Models / Dungnz.Engine / Dungnz.Systems / Dungnz.Data  │
│  (IDisplayService, IGameDisplay, IGameInput interfaces)         │
│  (Player, Enemy, Room, GameLoop, CombatEngine, etc.)            │
└────────────────────────────────────────────────────────────────┘
                            ▲         ▲
                            │         │
          ┌─────────────────┘         └──────────────────┐
          │                                               │
┌─────────┴──────────┐                     ┌─────────────┴────────────┐
│   Dungnz.csproj    │                     │ Dungnz.Display.Avalonia/ │
│   (Console Exe)    │                     │ Dungnz.Display.Avalonia. │
│                    │                     │         csproj           │
│  Program.cs        │                     │      (GUI Exe)           │
│  - Console I/O     │                     │                          │
│  - SpectreLayout   │                     │  Program.cs              │
│  - GameLoop        │                     │  - Avalonia bootstrap    │
│  - SaveSystem      │                     │  - AvaloniaDisplaySvc    │
│                    │                     │  - GameLoop (Task.Run)   │
│  Uses:             │                     │  - MainWindow.axaml      │
│  ConsoleDisplay    │                     │  - 6 Panel ViewModels    │
│  SpectreDisplay    │                     │                          │
└────────────────────┘                     └──────────────────────────┘

       Default                                    Opt-in GUI
  (current experience)                        (new feature)
```

**Zero cross-reference between executables.** Both depend on shared libraries, neither depends on the other.

---

## Implementation Changes

### 1. Remove Commented Avalonia Code from `Dungnz/Program.cs`

**Delete lines 2, 31-42** (all commented `--avalonia` references):

```diff
  using Dungnz.Display;
- // using Dungnz.Display.Avalonia;  // TODO: P3 — uncomment when build issues resolved
  using Dungnz.Display.Spectre;

  // ... (rest of startup unchanged) ...

- // TODO: P3 — re-enable --avalonia flag once build issues resolved
- // Check for --avalonia flag
- // var useAvalonia = args.Contains("--avalonia");
- 
- // if (useAvalonia)
- // {
- //     logger.LogInformation("Launching Avalonia UI...");
- //     var app = AvaloniaAppBuilder.Configure(args);
- //     app.RunGame();
- //     // TODO: P3-P8 — wire game loop to run on background thread
- //     return;
- // }

  var inputReader = new ConsoleInputReader();
  // ... (continue with existing Spectre/Console logic) ...
```

**Rationale:** The `--avalonia` flag pattern doesn't work with two-exe architecture. User explicitly chooses which exe to launch.

### 2. Keep `Dungnz.csproj` Reference COMMENTED OUT (Permanent)

**No change needed.** Line 33 stays as-is:

```xml
<!-- P2 NOTE: Project reference intentionally omitted — two-executable architecture -->
<!-- <ProjectReference Include="Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj" /> -->
<ProjectReference Include="Dungnz.Engine/Dungnz.Engine.csproj" />
```

Update comment to clarify this is by design, not a TODO.

### 3. Convert Avalonia Project to Executable

**File:** `Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj`

**Change `<OutputType>` property:**

```diff
  <PropertyGroup>
-   <TargetFramework>net10.0</TargetFramework>
+   <OutputType>Exe</OutputType>
+   <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
```

**Add project references to shared libraries:**

```xml
<ItemGroup>
  <ProjectReference Include="../Dungnz.Models/Dungnz.Models.csproj" />
  <ProjectReference Include="../Dungnz.Data/Dungnz.Data.csproj" />
  <ProjectReference Include="../Dungnz.Systems/Dungnz.Systems.csproj" />
  <ProjectReference Include="../Dungnz.Engine/Dungnz.Engine.csproj" />
  <ProjectReference Include="../Dungnz.Display/Dungnz.Display.csproj" />
</ItemGroup>
```

**Why `Dungnz.Display` reference?** So MapRenderer can fall back to SpectreLayoutDisplayService's markup utilities if helpful. Optional, but maintains symmetry.

### 4. Create `Dungnz.Display.Avalonia/Program.cs`

New file that mirrors the structure of `Dungnz/Program.cs` but bootstraps Avalonia:

```csharp
using Avalonia;
using Dungnz.Display.Avalonia;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog (same as console app)
var logDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "Dungnz", "Logs");
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(Path.Combine(logDir, "dungnz-avalonia-.log"),
                  rollingInterval: Serilog.RollingInterval.Day)
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger("Dungnz.Avalonia");

logger.LogInformation("Dungnz Avalonia GUI starting...");

// Build and start Avalonia application
// The App.OnFrameworkInitializationCompleted will create MainWindow
// and start the game loop on a background thread
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
```

**Flow:**
1. Avalonia bootstraps on main thread
2. `App.OnFrameworkInitializationCompleted` creates MainWindow with `AvaloniaDisplayService`
3. Game loop runs on `Task.Run(() => gameLoop.Run(player, room))`
4. All display calls marshal to UI thread via `Dispatcher.UIThread.InvokeAsync`

### 5. Wire Game Loop in `App.axaml.cs`

**File:** `Dungnz.Display.Avalonia/App.axaml.cs`

Replace stub `OnFrameworkInitializationCompleted`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dungnz.Display.Avalonia.ViewModels;
using Dungnz.Display.Avalonia.Views;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dungnz.Display.Avalonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            
            // Load prestige
            var prestige = PrestigeSystem.Load();
            
            // Create main window and ViewModel
            var mainVM = new MainWindowViewModel();
            var displayService = new AvaloniaDisplayService(mainVM);
            
            var mainWindow = new MainWindow
            {
                DataContext = mainVM
            };
            desktop.MainWindow = mainWindow;
            
            // Start game loop on background thread after window shown
            mainWindow.Opened += async (s, e) =>
            {
                // TODO P3-P8: Full startup flow (difficulty select, class select, etc.)
                // For now: launch with default player for testing
                var defaultDiff = DifficultySettings.For(Difficulty.Normal);
                
                EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
                StartupValidator.ValidateOrThrow();
                CraftingSystem.Load("Data/crafting-recipes.json");
                AffixRegistry.Load("Data/item-affixes.json");
                StatusEffectRegistry.Load("Data/status-effects.json");
                var allItems = ItemConfig.Load("Data/item-stats.json")
                    .Select(ItemConfig.CreateItem).ToList();
                
                var generator = new DungeonGenerator(seed: 12345, allItems);
                var (startRoom, _) = generator.Generate(difficulty: defaultDiff);
                
                var player = new Player("Adventurer", PlayerClass.Warrior);
                
                var inputReader = new AvaloniaInputReader(displayService); // NEW: wraps TCS pattern
                var combat = new CombatEngine(displayService, inputReader, difficulty: defaultDiff);
                var gameLoop = new GameLoop(displayService, combat, inputReader,
                    seed: 12345, difficulty: defaultDiff, allItems: allItems,
                    logger: loggerFactory.CreateLogger<GameLoop>());
                
                // Run game on background thread
                await Task.Run(() => gameLoop.Run(player, startRoom));
                
                // Game ended — close window
                mainWindow.Close();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

**Note:** `AvaloniaInputReader` is a thin adapter that delegates to `AvaloniaDisplayService` for input methods (the TCS pattern). Keeps the display service focused on display logic.

---

## Benefits of Two-Executable Architecture

### ✅ **1. Zero Build Conflicts**
- No AXAML cross-compilation issues — ever
- Each project compiles in its own context with its own dependencies
- No MSBuild workarounds, no source generator edge cases

### ✅ **2. Clean Separation of Concerns**
- Console exe: lightweight, no GUI dependencies, fast build, CI-friendly
- GUI exe: full Avalonia stack, platform-native rendering, optional for users
- Shared libraries: game logic is framework-agnostic

### ✅ **3. Easier Testing**
- Console exe tests run without Avalonia UI thread complexity
- Avalonia exe can have its own test suite focused on UI behaviors
- Shared libraries tested independently with `TestDisplayService` (no change)

### ✅ **4. Independent Evolution**
- Console/Spectre can be updated without touching Avalonia code
- Avalonia can be refactored without risking console build
- If Avalonia experiment fails, delete one directory — zero rollback cost

### ✅ **5. User Choice is Explicit**
```bash
# Console mode (current default)
dotnet run

# GUI mode (new feature)
dotnet run --project Dungnz.Display.Avalonia
```

Clear, unambiguous. No hidden flags. No runtime switching complexity.

### ✅ **6. Standard Avalonia Pattern**
Most Avalonia applications are standalone executables. This follows framework conventions and community best practices. The single-exe-with-flag pattern is non-standard and fight against the tooling.

---

## Drawbacks and Trade-offs

### ❌ **1. Code Duplication in Program.cs**
Both executables need startup logic: logging, data loading, prestige, dungeon generation.

**Mitigation:** Extract to `Startup.cs` helper in `Dungnz.Engine`:

```csharp
public static class StartupOrchestrator
{
    public static (Player player, Room startRoom, GameLoop gameLoop) 
        Bootstrap(IDisplayService display, IInputReader input, PrestigeData prestige, int seed);
}
```

Both `Program.cs` files call this. Shared code stays DRY.

### ❌ **2. Two Binaries to Distribute**
Release artifacts now include:
- `Dungnz.exe` (or `Dungnz` on Linux/macOS)
- `Dungnz.Display.Avalonia.exe` (or `.../Avalonia`)

**Mitigation:** 
- Document clearly in README: "For console mode: run `Dungnz`. For GUI: run `Dungnz.Avalonia`."
- Future: Add a launcher exe that presents a dialog: "Console or GUI?" and spawns the chosen binary.
- Not a blocker — users who want GUI will figure it out.

### ❌ **3. No Runtime Switching**
User can't toggle `--avalonia` flag to switch UIs without restarting.

**Reality check:** This was never a real use case. Players pick one mode and stick with it for a session. If they want to try the other, they restart.

---

## Revised Phase Roadmap

Only **Phase 2** and **Phase 3** descriptions change. All other phases (P4-P11) proceed as originally spec'd.

### **Phase 2 (Revised): Scaffold Avalonia Executable**

**Who:** Hill  
**Dependencies:** P0 (IDisplayService split), P1 (MapRenderer extraction)  
**Deliverables:**
1. Change `Dungnz.Display.Avalonia.csproj` to `<OutputType>Exe</OutputType>`
2. Add project references to Engine, Systems, Models, Data, Display
3. Create `Dungnz.Display.Avalonia/Program.cs` with Avalonia bootstrap
4. Update `App.axaml.cs` to wire MainWindow + game loop on background thread (stub flow: default player, default dungeon)
5. Verify: `dotnet run --project Dungnz.Display.Avalonia` launches window, runs 1 turn of game loop, closes
6. Keep `Dungnz.csproj` reference commented with clarifying comment
7. Delete commented `--avalonia` code from `Dungnz/Program.cs`

**Acceptance Criteria:**
- `dotnet build` builds both executables with no errors
- `dotnet run` launches console mode (unchanged)
- `dotnet run --project Dungnz.Display.Avalonia` launches GUI window (empty panels, runs game loop stub)
- All existing tests pass (GUI exe has no tests yet — that's P9+)

### **Phase 3 (Revised): Implement Output-Only Display Methods**

**No changes** — proceeds as originally spec'd. `AvaloniaDisplayService` implements `IGameDisplay` methods.

---

## Impact on PR #1401

**Current state:** Hill's P1+P2 work is correct, except for the commented-out integration code that assumes single-exe architecture.

**Required changes:**
1. Uncomment and update `Dungnz.csproj` reference comment (clarify it's permanent, not TODO)
2. Delete all commented `--avalonia` code from `Dungnz/Program.cs` (lines 2, 31-42)
3. Convert `Dungnz.Display.Avalonia.csproj` to `<OutputType>Exe</OutputType>`
4. Add shared library references to Avalonia csproj
5. Create `Dungnz.Display.Avalonia/Program.cs`
6. Update `App.axaml.cs` with game loop wiring (stub for P2)
7. Update `AvaloniaAppBuilder.cs` or delete it (its `RunGame` method is replaced by `App.OnFrameworkInitializationCompleted`)

**Estimated rework:** 1-2 hours. Mechanical changes, no logic rewrites.

**Testing:** Build both exes, run console mode (verify unchanged), launch GUI mode (verify window opens).

---

## Alternatives Considered

### Alternative A: Runtime Assembly Loading
Load `Dungnz.Display.Avalonia.dll` at runtime via `Assembly.Load` + reflection when `--avalonia` flag present.

**Rejected because:**
- Fragile: breaks with any type signature changes
- No compile-time safety
- Harder to debug
- Non-standard pattern in .NET

### Alternative B: MSBuild Conditional Compilation
Use `<ProjectReference Condition="'$(BuildAvalonia)'=='true'">` and build configurations.

**Rejected because:**
- Still has source generator issues (Condition evaluated after generator runs)
- IDE support broken (Rider/VS don't understand the condition)
- Requires two build scripts/CI jobs
- Adds cognitive load: "Which config am I building?"

### Alternative C: Keep Current Workaround
The `<Compile Remove>` and `<AvaloniaResource Remove>` exclusions currently prevent build errors.

**Rejected because:**
- Fragile: relies on MSBuild evaluation order, could break in future SDK versions
- Doesn't actually fix the problem, just masks it
- IDE tooling still confused (IntelliSense errors, Go To Definition breaks)
- Non-standard pattern — future devs will be confused

**Two-executable is the only architecturally sound solution.**

---

## Rollback Plan

If Avalonia migration fails or Anthony decides to abandon:

1. Delete `Dungnz.Display.Avalonia/` directory entirely
2. Revert any `Dungnz.slnx` changes (remove Avalonia project entry)
3. No changes to `Dungnz.csproj` needed (reference was never uncommented)
4. No changes to existing game logic needed (all Engine/Systems code is Avalonia-agnostic)

**Cost of rollback:** Delete 1 directory, revert 1 line in solution file. < 5 minutes.

---

## Decision

**Adopt two-executable architecture** for Avalonia GUI integration.

**Rationale:** Only architecturally sound pattern that avoids build tool conflicts, follows Avalonia best practices, and maintains clean separation between console and GUI modes.

**Next Steps:**
1. Hill updates PR #1401 with Phase 2 revisions per this decision
2. Coulson updates `docs/avalonia-migration-spec.md` to replace Section 3-4 with two-exe architecture
3. Anthony approves decision + PR merges
4. Phases P3-P11 proceed unchanged

---

**References:**
- Avalonia docs on application structure: https://docs.avaloniaui.net/docs/concepts/application-lifetimes
- Terminal.Gui rollback precedent: `git log --grep="delete TUI" --oneline`
- AXAML source generator issue tracking: Avalonia GitHub issue #8712 (cross-project XAML compilation)
