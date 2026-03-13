# Hill — PR #1401 Phase 2 Revisions Required

**Date:** 2026-03-13  
**From:** Coulson (Lead)  
**Context:** Avalonia spec revised to two-executable architecture

---

## Summary

Your P1 (MapRenderer extraction) is ✅ **perfect — no changes needed**.

Your P2 (Avalonia scaffold) is **structurally correct** but needs architectural rework to adopt the two-executable pattern. The original spec's single-exe-with-flag approach doesn't work due to AXAML source generator conflicts.

**Estimated rework: 1-2 hours** (all mechanical changes, no logic rewrites).

---

## What Changed and Why

### Original Spec (Broken)
```
Dungnz.csproj
  └─ <ProjectReference Include="Dungnz.Display.Avalonia.csproj" />
  └─ Program.cs with --avalonia flag

Problem: Avalonia's XAML source generator tries to compile .axaml files
in Dungnz.csproj context → AVLN2000 errors (missing Avalonia packages)
```

### Revised Spec (Two-Executable Architecture)
```
Dungnz.csproj (Console Exe)          Dungnz.Display.Avalonia.csproj (GUI Exe)
  Program.cs                            Program.cs (NEW)
  SpectreLayoutDisplayService           AvaloniaDisplayService
         ↓                                      ↓
      GameLoop ←──── Shared Libraries ─────→ GameLoop
              (Models, Engine, Systems, Data)
```

**No cross-reference between exes.** Both depend on shared game logic; neither depends on the other.

**Launch:**
- Console: `dotnet run` (default, unchanged)
- GUI: `dotnet run --project Dungnz.Display.Avalonia`

**Rationale:** Standard Avalonia pattern. Avoids AXAML build conflicts entirely. Clean rollback (delete directory). Zero contamination of console codebase.

---

## Required Changes

### 1. Update `Dungnz.csproj` Comment (Line 32)

**Current:**
```xml
<!-- TODO: P3 — re-enable Avalonia reference once build issues resolved -->
<!-- <ProjectReference Include="Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj" /> -->
```

**Change to:**
```xml
<!-- NOTE: Avalonia is a separate executable — no reference needed (two-exe architecture) -->
<!-- See docs/avalonia-migration-spec.md Section 3 for rationale -->
```

---

### 2. Delete Commented Avalonia Code from `Dungnz/Program.cs`

**Delete these lines:**
- Line 2: `// using Dungnz.Display.Avalonia;  // TODO: P3 — uncomment when build issues resolved`
- Lines 31-42: All commented `--avalonia` flag logic:
  ```csharp
  // TODO: P3 — re-enable --avalonia flag once build issues resolved
  // Check for --avalonia flag
  // var useAvalonia = args.Contains("--avalonia");
  // 
  // if (useAvalonia)
  // {
  //     logger.LogInformation("Launching Avalonia UI...");
  //     var app = AvaloniaAppBuilder.Configure(args);
  //     app.RunGame();
  //     // TODO: P3-P8 — wire game loop to run on background thread
  //     return;
  // }
  ```

**Why:** No flag-based switching in two-exe architecture. User explicitly chooses which exe to run.

---

### 3. Convert Avalonia Project to Executable

**File:** `Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj`

**Change:**
```diff
  <PropertyGroup>
+   <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
```

**Add project references:**
```xml
<ItemGroup>
  <ProjectReference Include="../Dungnz.Models/Dungnz.Models.csproj" />
  <ProjectReference Include="../Dungnz.Data/Dungnz.Data.csproj" />
  <ProjectReference Include="../Dungnz.Systems/Dungnz.Systems.csproj" />
  <ProjectReference Include="../Dungnz.Engine/Dungnz.Engine.csproj" />
  <ProjectReference Include="../Dungnz.Display/Dungnz.Display.csproj" />
</ItemGroup>
```

**Add logging packages:**
```xml
<ItemGroup>
  <!-- Logging (same as console app) -->
  <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="10.0.3" />
  <PackageReference Include="Serilog.Extensions.Logging" Version="10.0.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
</ItemGroup>
```

**Add Data directory to output:**
```xml
<ItemGroup>
  <!-- Copy JSON config to output directory -->
  <Content Include="../Data/**" Exclude="../Data/**/*.cs">
    <Link>Data/%(RecursiveDir)%(Filename)%(Extension)</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

---

### 4. Create `Dungnz.Display.Avalonia/Program.cs`

**New file:**

```csharp
using Avalonia;
using Dungnz.Display.Avalonia;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog (same pattern as console app)
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

**Key points:**
- Mirrors console `Program.cs` structure
- Uses `dungnz-avalonia-.log` prefix (separate from console logs)
- Avalonia takes over main thread
- Game loop wired in `App.axaml.cs.OnFrameworkInitializationCompleted`

---

### 5. Update `Dungnz.Display.Avalonia/App.axaml.cs`

**Replace current stub `OnFrameworkInitializationCompleted` with:**

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
            
            // Start game loop on background thread after window is shown
            mainWindow.Opened += async (s, e) =>
            {
                // TODO P3-P8: Full startup flow (StartupOrchestrator, SelectClass, SelectDifficulty)
                // For P2: launch with default player for smoke test
                
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
                
                // TODO P3: Create AvaloniaInputReader
                var inputReader = new ConsoleInputReader(); // TEMP stub
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

**Key points:**
- Game loop runs on `Task.Run` (background thread)
- Main thread runs Avalonia UI loop
- P2 stub: default player, seed 12345, one room, then exit
- Input uses `ConsoleInputReader` temporarily (P3 will add `AvaloniaInputReader`)

---

### 6. Delete `Dungnz.Display.Avalonia/AvaloniaAppBuilder.cs`

**Why:** Replaced by `Program.cs` + `App.axaml.cs` pattern. The helper class is no longer needed.

**Alternative:** Keep it if you want, but comment it out — it's not used in the two-exe flow.

---

## Testing the Changes

### Build Test
```bash
# Should build both executables
dotnet build

# Should see:
#   Dungnz -> .../Dungnz.dll
#   Dungnz.Display.Avalonia -> .../Dungnz.Display.Avalonia.dll
```

### Console Mode Test (Unchanged)
```bash
dotnet run

# Should launch Spectre UI as normal
# All existing behavior unchanged
```

### GUI Mode Test (New)
```bash
dotnet run --project Dungnz.Display.Avalonia

# Should:
# 1. Open a window with 6 empty panels (Map|Stats, Content|Gear, Log|Input)
# 2. Display for 1-2 seconds (stub game loop runs)
# 3. Close automatically
```

### Automated Tests
```bash
dotnet test

# All 2,154 tests should pass
# Console mode unchanged — no test updates needed
```

---

## Acceptance Criteria (Revised)

1. ✅ `dotnet build` builds both executables with no errors
2. ✅ `dotnet run` launches console mode (Spectre UI, unchanged)
3. ✅ `dotnet run --project Dungnz.Display.Avalonia` launches GUI window
4. ✅ GUI window shows 6 empty panels for 1-2 seconds, then closes
5. ✅ All 2,154 existing tests pass
6. ✅ No commented Avalonia code remains in `Dungnz/Program.cs`
7. ✅ `Dungnz.csproj` comment clarifies reference is omitted BY DESIGN

---

## Phase 3+ (No Changes Needed)

**Phases P3-P8 proceed as originally spec'd:**
- P3: Implement output-only display methods (Stats, Gear, Log, Content panels)
- P4: Map panel rendering
- P5: Input panel + `ReadCommandInput`
- P6: Menu input (`*AndSelect` methods)
- P7: Text entry inputs
- P8: Startup flow menus

The two-exe architecture change only affects P2. All display service implementation work stays the same.

---

## Questions?

**Why two executables instead of one with a flag?**
- Avalonia's XAML source generator doesn't work when parent project lacks Avalonia packages
- Two-exe is the standard pattern for GUI frameworks (git vs git-gui, dotnet CLI vs VS)
- Clean rollback: delete directory, zero contamination of console code

**Will users be confused by two binaries?**
- README will document clearly: "Console: run Dungnz. GUI: run Dungnz.Avalonia."
- Console remains default — GUI is opt-in feature

**Do we need to maintain two Program.cs files forever?**
- ~30 lines of duplication max
- Can extract shared logic to `StartupBootstrap` helper later (not P2 scope)
- Worth it for the architectural cleanliness

**What if this fails?**
- Rollback: delete `Dungnz.Display.Avalonia/`, revert one line in `.slnx`
- Console code untouched — zero risk to existing functionality

---

## References

- **Revised spec:** `docs/avalonia-migration-spec.md` (Sections 3-7 updated)
- **Decision doc:** `.ai-team/decisions/inbox/coulson-avalonia-integration-fix.md`
- **Commit:** `9b0e023` on `squad/avalonia-p1-p2-scaffold`

Let me know if you hit any issues. The changes are mechanical — should be smooth.

— Coulson
