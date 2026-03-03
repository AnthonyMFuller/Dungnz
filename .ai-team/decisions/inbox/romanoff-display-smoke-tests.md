# Decision: AnsiConsole Capture Pattern is Established Standard

**Date:** 2026-03-03
**Author:** Romanoff (Tester)
**Issue:** #875 — DisplayService smoke tests

## Decision

The `AnsiConsole.Console` swap pattern used in `HelpDisplayRegressionTests` (#870) is now **confirmed and established** as the standard approach for all Spectre.Console display method tests in this project.

## Pattern

```csharp
[Collection("console-output")]
public sealed class MyTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole;
    private readonly StringWriter _writer;

    public MyTests()
    {
        _originalConsole = AnsiConsole.Console;
        _writer = new StringWriter();
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi        = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out         = new AnsiConsoleOutput(_writer),
            Interactive = InteractionSupport.No,
        });
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _writer.Dispose();
    }
}
```

## Rules

1. **Always use `[Collection("console-output")]`** on any test class that redirects `AnsiConsole.Console`. Parallel execution without this causes races.
2. **`MarkupException` is the primary failure mode** for unescaped brackets — `Should().NotThrow()` catches this automatically.
3. **For `ShowSkillTreeMenu`:** use a level ≤ 2 player to avoid the interactive `AnsiConsole.Prompt()` call. All skills require level 3+.
4. **For interactive prompts in other methods** (e.g., `ShowInventoryAndSelect`): do not test with `SpectreDisplayService` directly — use `FakeDisplayService` instead.

## Coverage Added (#875)

- `ShowInventory` (with items, empty)
- `ShowEquipment` (with gear, all empty)
- `ShowSkillTreeMenu` (no-learnable-skills path)
- `ShowHelp`
- `ShowCombatStatus` (no effects, active effects on both sides)
