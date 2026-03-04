# Rollback Guide: Terminal.Gui TUI Feature

This guide explains when and how to rollback the TUI (Terminal.Gui) feature if issues arise.

## When to Rollback

Consider rolling back the TUI feature if:
- Terminal rendering glitches or display artifacts appear
- Performance is significantly degraded
- Input handling becomes unresponsive
- Terminal compatibility issues prevent the game from running
- You simply prefer the original Spectre.Console display

## How to Rollback

Follow these steps **in order** to safely remove the TUI feature:

### Step 1: Delete the TUI Directory

```bash
rm -rf Display/Tui/
```

This removes all TUI implementation files:
- `GameThreadBridge.cs` (multi-threaded game/display coordination)
- `TerminalGuiDisplayService.cs` (Terminal.Gui renderer)
- `TerminalGuiInputReader.cs` (Terminal.Gui input handler)
- `TuiLayout.cs` (panel layout definition)
- `TuiMenuDialog.cs` (menu dialog helper)

### Step 2: Remove Terminal.Gui from Dungnz.csproj

Open `Dungnz.csproj` and remove this line:

```xml
<PackageReference Include="Terminal.Gui" Version="2.34.0" />
```

### Step 3: Clean Up Program.cs — Remove TUI Launch Code

In `Program.cs`, remove the TUI flag parsing and conditional launch:

**Remove this line:**
```csharp
bool useTui = args.Contains("--tui");
```

**Remove the entire `if (useTui)` block** (if present in your version).

Keep the original `else` block as the default, or simplify to always use `SpectreDisplayService`:

```csharp
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding  = System.Text.Encoding.UTF8;

// ... rest of setup ...

var prestige = PrestigeSystem.Load();
var inputReader = new ConsoleInputReader();
IDisplayService display = new SpectreDisplayService();  // Direct, no if/else needed

// ... continue with startup ...
```

### Step 4: Rebuild and Test

```bash
dotnet clean
dotnet build
dotnet test Dungnz.Tests
dotnet run
```

Verify:
- ✅ Build succeeds with no errors
- ✅ All tests pass
- ✅ Game runs in default Spectre.Console mode
- ✅ Gameplay is unchanged

## Safety Guarantees

Rollback is **zero-risk**:
- ✅ No game saves or progress are affected
- ✅ Prestige system remains intact
- ✅ All test suites pass after rollback
- ✅ Original Spectre.Console display is stable and unchanged
- ✅ No data loss or corruption

## Rollback Verification

After completing the steps above, confirm:

1. **Git status is clean:**
   ```bash
   git status
   ```
   Should show only the deletions you made (Display/Tui/, package reference, Program.cs edits).

2. **Build succeeds:**
   ```bash
   dotnet build
   ```

3. **Tests pass:**
   ```bash
   dotnet test Dungnz.Tests
   ```

4. **Game runs without TUI:**
   ```bash
   dotnet run
   ```
   Should start in default Spectre.Console mode.

## Need Help?

If rollback fails or the game doesn't run afterward:
1. Check `Program.cs` was edited correctly (no syntax errors)
2. Verify `Dungnz.csproj` has valid XML syntax
3. Run `dotnet clean` and try again
4. Check git logs: `git log --oneline` to see what changed
