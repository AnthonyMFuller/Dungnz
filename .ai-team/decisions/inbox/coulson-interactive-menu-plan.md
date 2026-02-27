# Interactive Menu Navigation — Design Proposal

**Author:** Coulson (Lead/Architect)  
**Date:** 2026-02-24  
**Requested by:** Anthony Fuller  
**Status:** PROPOSAL — awaiting approval before implementation begins

---

## Problem Statement

Every menu in Dungnz currently follows the same interaction model:

```
_display.ShowMessage("[1] Option A  [2] Option B  [X] Leave");
var choice = _input.ReadLine()?.Trim() ?? "";
switch (choice) { ... }
```

This works but feels archaic. The player must type a number and press Enter. The goal is to replace this with cursor-navigable menus: arrow keys move a `▶` highlight, Enter confirms. The main free-text command loop (movement, EXAMINE, etc.) is **not** changing — only fixed-choice menus.

---

## A. Architecture Approach

### Decision: Add a new `IMenuNavigator` interface

Do **not** extend `IInputReader`. `ReadLine()` is for free-text commands; it should stay focused on that. Menu navigation is a distinct interaction paradigm with distinct testability needs. Separating them keeps both interfaces minimal and honest.

Do **not** put key-reading logic inside `IDisplayService` or `ConsoleDisplayService`. Display is output. Navigation is a separate concern. Mixing them made `SelectDifficulty()` and `SelectClass()` console-coupled already — we should not deepen that pattern.

### New interface

```csharp
// Engine/IMenuNavigator.cs
namespace Dungnz.Engine;

/// <summary>
/// Presents the player with a fixed list of choices and returns their selection.
/// Real implementation uses Console.ReadKey with cursor highlighting.
/// Test implementation accepts a pre-scripted sequence of selections.
/// </summary>
public interface IMenuNavigator
{
    /// <summary>
    /// Displays a cursor-navigable list of options and returns the value of the
    /// selected entry. Blocks until the player confirms with Enter.
    /// </summary>
    T Select<T>(IReadOnlyList<MenuOption<T>> options, string? title = null);

    /// <summary>
    /// Shows a Y/N confirmation prompt. Returns true when the player confirms.
    /// </summary>
    bool Confirm(string prompt);
}

public record MenuOption<T>(string Label, T Value, string? Subtitle = null);
```

### Dependency injection

`IMenuNavigator` is injected via constructor into:
- `ConsoleDisplayService` — for `SelectDifficulty()` and `SelectClass()`
- `GameLoop` — for shrine, armory, trap, shop, and sell menus
- `CombatEngine` — for combat action menu, ability sub-menu, and level-up choice

`Program.cs` constructs one `ConsoleMenuNavigator` and passes it everywhere. Tests inject `FakeMenuNavigator`.

### Existing violation fixed

`SelectDifficulty()` and `SelectClass()` in `DisplayService.cs` currently call `Console.ReadLine()` directly — they bypass `IInputReader` entirely. This is a pre-existing coupling violation (noted in past retrospective). Converting them to use `IMenuNavigator` fixes the violation at no extra cost.

### IDisplayService contract — unchanged

`SelectDifficulty()` and `SelectClass()` remain on `IDisplayService` with the same signature. `ConsoleDisplayService` delegates input to the injected `IMenuNavigator`. `FakeDisplayService` continues to return `SelectDifficultyResult` / `SelectClassResult` as today — these tests are unaffected.

---

## B. Reusable Component Design

### `ConsoleMenuNavigator`

```csharp
// Display/ConsoleMenuNavigator.cs
public class ConsoleMenuNavigator : IMenuNavigator
{
    public T Select<T>(IReadOnlyList<MenuOption<T>> options, string? title = null)
    {
        if (title != null) Console.WriteLine(title);

        int selected = 0;
        int startRow = Console.CursorTop;

        RenderOptions(options, selected, startRow);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selected > 0) { selected--; RenderOptions(options, selected, startRow); }
                    break;
                case ConsoleKey.DownArrow:
                    if (selected < options.Count - 1) { selected++; RenderOptions(options, selected, startRow); }
                    break;
                case ConsoleKey.Enter:
                    Console.WriteLine(); // move past the menu
                    return options[selected].Value;
            }
        }
    }

    public bool Confirm(string prompt)
    {
        return Select(
            new[]
            {
                new MenuOption<bool>("Yes", true),
                new MenuOption<bool>("No",  false),
            },
            title: prompt);
    }

    private static void RenderOptions<T>(IReadOnlyList<MenuOption<T>> options, int selected, int startRow)
    {
        Console.SetCursorPosition(0, startRow);
        for (int i = 0; i < options.Count; i++)
        {
            var prefix    = i == selected ? $"{ColorCodes.Cyan}▶ {ColorCodes.Reset}" : "  ";
            var label     = i == selected
                ? $"{ColorCodes.Bold}{options[i].Label}{ColorCodes.Reset}"
                : $"{ColorCodes.Gray}{options[i].Label}{ColorCodes.Reset}";
            var subtitle  = options[i].Subtitle != null ? $"  {ColorCodes.Gray}{options[i].Subtitle}{ColorCodes.Reset}" : "";
            Console.WriteLine($"  {prefix}{label}{subtitle}    "); // trailing spaces erase any previous longer line
        }
    }
}
```

Key rendering notes:
- `Console.SetCursorPosition(0, startRow)` redraws in-place — no scroll artifacts
- Trailing spaces on each line clear stale characters from shorter-than-previous lines
- `intercept: true` prevents key presses appearing as console output
- `ColorCodes.Bold` + `ColorCodes.Cyan` highlight the selected row using the existing color system

### `FakeMenuNavigator` (test double)

```csharp
// Dungnz.Tests/Helpers/FakeMenuNavigator.cs
public class FakeMenuNavigator : IMenuNavigator
{
    private readonly Queue<int>  _selections    = new();
    private readonly Queue<bool> _confirmations = new();

    /// <summary>Enqueue a 0-based index to be returned on the next Select call.</summary>
    public FakeMenuNavigator EnqueueSelection(int index)
    {
        _selections.Enqueue(index);
        return this;
    }

    /// <summary>Enqueue a bool to be returned on the next Confirm call.</summary>
    public FakeMenuNavigator EnqueueConfirm(bool value)
    {
        _confirmations.Enqueue(value);
        return this;
    }

    public T Select<T>(IReadOnlyList<MenuOption<T>> options, string? title = null)
    {
        var idx = _selections.Count > 0 ? _selections.Dequeue() : 0;
        if (idx < 0 || idx >= options.Count)
            throw new InvalidOperationException(
                $"FakeMenuNavigator: index {idx} out of range (0–{options.Count - 1})");
        return options[idx].Value;
    }

    public bool Confirm(string prompt)
        => _confirmations.Count > 0 ? _confirmations.Dequeue() : false;
}
```

This is intentionally minimal. Tests become:

```csharp
var nav = new FakeMenuNavigator()
    .EnqueueSelection(0)   // buy first item
    .EnqueueSelection(2);  // leave shop (index of "X Leave" option)

var loop = new GameLoop(display, nav, input, ...);
```

No string parsing, no `ReadKey`, no TTY required.

---

## C. Scope of Call Sites

Every numbered or fixed-choice menu that should become interactive:

| # | Menu | File | Approx. line | Notes |
|---|------|------|-------------|-------|
| 1 | **Difficulty select** | `Display/DisplayService.cs` | ~1004 | Uses raw `Console.ReadLine()` today — violation |
| 2 | **Class select** | `Display/DisplayService.cs` | ~1038 | Uses raw `Console.ReadLine()` today — violation |
| 3 | **Shop buy** | `Engine/GameLoop.cs` | ~1164 | Loop: buy/sell/leave; `SELL` word stays as shorthand or becomes a menu option |
| 4 | **Sell menu** | `Engine/GameLoop.cs` | ~1233 | Item list; confirm sub-step at ~1247 → `navigator.Confirm()` |
| 5 | **Level-up choice** | `Engine/CombatEngine.cs` | ~1528 | 3 options: +HP / +ATK / +DEF |
| 6 | **Shrine menu** | `Engine/GameLoop.cs` | ~784 | H/B/F/M/L → 5-option menu |
| 7 | **Forgotten Shrine** | `Engine/GameLoop.cs` | ~851 | 3 blessings + Leave |
| 8 | **Contested Armory** | `Engine/GameLoop.cs` | ~929 | Careful / Reckless / Leave |
| 9 | **Trap — Arrow Volley** | `Engine/GameLoop.cs` | ~1004 | 2 options (shield / sprint) |
| 10 | **Trap — Poison Gas** | `Engine/GameLoop.cs` | ~1041 | 2 options (sprint / bypass) |
| 11 | **Trap — Collapsing Floor** | `Engine/GameLoop.cs` | ~1074 | 2 options (leap / careful) |
| 12 | **Combat action menu** | `Engine/CombatEngine.cs` | ~406 | A / B / F (+ USE item) — highest risk |
| 13 | **Ability sub-menu** | `Engine/CombatEngine.cs` | ~645 | Dynamic list of unlocked abilities |

**Out of scope (stays as free-text `ReadLine`):**
- Main exploration loop (line 132 GameLoop) — movement, EXAMINE, TAKE, etc.
- `ReadPlayerName()` — single free-text entry, not a menu

---

## D. Test Strategy

### No real terminal in tests — ever

All menu interaction in tests goes through `FakeMenuNavigator`. It never calls `Console.ReadKey`. Tests remain fully headless.

### What `FakeInputReader` needs — nothing new

`FakeInputReader` already handles `ReadLine()` for free-text commands. It does not need to change. `FakeMenuNavigator` is a separate concern and handles menu selections independently.

### Test patterns

**Menu index testing:**
```csharp
// Test: player heals at shrine (index 0 = Heal)
var nav = new FakeMenuNavigator().EnqueueSelection(0);
// ... invoke shrine handler ...
Assert.True(player.HP == player.MaxHP);
```

**Confirm dialog testing:**
```csharp
// Test: player confirms sell
var nav = new FakeMenuNavigator()
    .EnqueueSelection(1)    // sell item at index 1
    .EnqueueConfirm(true);  // confirm "Yes"
```

**Cancellation testing:**
```csharp
// Test: player backs out of shop
var nav = new FakeMenuNavigator().EnqueueSelection(leaveIndex);
```

### Guarding against silent failures

`FakeMenuNavigator.Select()` throws `InvalidOperationException` if the scripted index is out of range. This surfaces test setup bugs immediately rather than silently selecting index 0.

When the queue is empty, default to index 0 (first option). This is safe for tests that don't care about the menu outcome.

### Existing tests

Existing tests using `FakeDisplayService.SelectDifficultyResult` / `SelectClassResult` are **not broken**. `FakeDisplayService` does not use `IMenuNavigator` at all — it just returns the pre-set value. These tests stay green through all phases.

---

## E. Platform Constraint

### `Console.ReadKey()` and headless environments

`Console.ReadKey(intercept: true)` requires a real TTY (interactive terminal). In GitHub Actions CI, stdin is redirected and `ReadKey` will throw `InvalidOperationException` at runtime.

**Mitigation in `ConsoleMenuNavigator`:**

```csharp
public T Select<T>(IReadOnlyList<MenuOption<T>> options, string? title = null)
{
    // Guard: if not attached to a real TTY, fall back to ReadLine-based selection.
    // This should never happen in production (the game is a terminal app),
    // but defends against accidental calls in CI or piped contexts.
    if (Console.IsInputRedirected)
        return FallbackReadLine(options, title);

    // ... ReadKey loop ...
}

private static T FallbackReadLine<T>(IReadOnlyList<MenuOption<T>> options, string? title)
{
    if (title != null) Console.WriteLine(title);
    for (int i = 0; i < options.Count; i++)
        Console.WriteLine($"  [{i + 1}] {options[i].Label}");
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim() ?? "1";
    return int.TryParse(input, out var idx) && idx >= 1 && idx <= options.Count
        ? options[idx - 1].Value
        : options[0].Value;
}
```

This means:
- **Production terminal**: full arrow-key navigation
- **Piped / redirected stdin**: graceful numbered fallback (identical to current behaviour)
- **Tests**: `FakeMenuNavigator` is injected — `ConsoleMenuNavigator` is never constructed

### `Console.SetCursorPosition` on Linux

`Console.SetCursorPosition` works on all .NET-supported terminals (Linux, macOS, Windows). It requires `TERM` to be set (standard for any interactive terminal). No special handling needed beyond the `IsInputRedirected` guard above.

---

## F. Phasing — Recommended Conversion Order

### Phase 1 — Infrastructure (zero gameplay change)

1. Create `IMenuNavigator` interface (`Engine/IMenuNavigator.cs`)
2. Create `MenuOption<T>` record
3. Create `ConsoleMenuNavigator` with `IsInputRedirected` guard
4. Create `FakeMenuNavigator` in test project
5. Wire `IMenuNavigator` into `ConsoleDisplayService`, `GameLoop`, and `CombatEngine` constructors
6. Update `Program.cs` to construct and inject `ConsoleMenuNavigator`
7. **No menus converted yet** — all existing `ReadLine` paths still work

**Gate:** All 125+ existing tests pass. Build clean.

### Phase 2 — Pre-game menus (highest impact, lowest risk)

Convert `SelectDifficulty()` and `SelectClass()` in `ConsoleDisplayService`. These are the first thing every player sees. High visibility. No game state involved.

**Why first:** Every run starts here. Player immediately perceives the UX upgrade. Easiest to manually verify. Also fixes the `Console.ReadLine()` bypass violation.

### Phase 3 — Shop and Sell menus

Convert `HandleShop()` and `HandleSell()` in `GameLoop`. The shop is the most player-visited non-combat menu. Convert sell confirmation to `navigator.Confirm()`.

**Note:** The shop render (`ShowShop`) shows rich box-drawn item cards — keep `ShowShop()` as a pure display call for the header/items. The navigator provides the selection separately. The item cards become a visual reference; the cursor moves in a simpler list below, or we render a compact selector list (discuss with Hill before implementation).

### Phase 4 — Level-up choice

Convert `CheckLevelUp()` in `CombatEngine`. Three options. Low risk — already well-tested.

### Phase 5 — Special rooms

Convert shrine, forgotten shrine, armory, and all three trap variants in `GameLoop`. All follow the same shape: 2–5 options + Leave. Straightforward once Phase 1 is in place.

### Phase 6 — Combat menus (highest risk)

Convert combat action menu and ability sub-menu in `CombatEngine`. These execute every combat turn. Existing combat tests are extensive — run full suite before and after.

**Risk note for combat menus:** The ability sub-menu is dynamically built (varies by class, mana, unlocked abilities). The `MenuOption<T>` list must be constructed correctly from the dynamic ability list. Test coverage for this path must be reviewed with Romanoff before conversion.

---

## Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| `Console.ReadKey` throws in CI | **HIGH** | `IsInputRedirected` guard in ConsoleMenuNavigator |
| `Console.SetCursorPosition` panics on narrow terminal | Medium | Clamp rendering width; test on 80-col terminal |
| Combat menu conversion breaks existing combat tests | Medium | Phase 6 last; Romanoff reviews test coverage before work starts |
| Shop "SELL" typed as free-text inside shop | Low | Either keep it as a fallback text alias, or add "Sell Items" as an explicit menu option |
| `SelectDifficulty`/`SelectClass` currently on `IDisplayService` — changes affect FakeDisplayService | Low | FakeDisplayService unchanged; only ConsoleDisplayService changes |
| `FakeMenuNavigator` queue exhaustion produces wrong results | Low | Throw on out-of-range index; default-to-zero only when queue empty |
| Phase 6 (combat) regresses ability ordering across classes | Medium | Romanoff writes ability-menu ordering tests before Phase 6 starts |

---

## Summary of Interface Changes

### New: `Engine/IMenuNavigator.cs`
```
+ interface IMenuNavigator
+ record MenuOption<T>(string Label, T Value, string? Subtitle = null)
```

### New: `Display/ConsoleMenuNavigator.cs`
```
+ class ConsoleMenuNavigator : IMenuNavigator
```

### New: `Dungnz.Tests/Helpers/FakeMenuNavigator.cs`
```
+ class FakeMenuNavigator : IMenuNavigator
```

### Modified: `Display/DisplayService.cs` (ConsoleDisplayService)
```
+ constructor parameter: IMenuNavigator navigator
~ SelectDifficulty(): removes Console.ReadLine() loop → uses _navigator.Select(...)
~ SelectClass():      removes Console.ReadLine() loop → uses _navigator.Select(...)
```

### Modified: `Engine/GameLoop.cs`
```
+ constructor parameter: IMenuNavigator navigator
~ HandleShop, HandleSell, HandleShrine, HandleForgottenShrine,
  HandleContestedArmory, HandleTrapRoom: ReadLine → navigator.Select(...)
```

### Modified: `Engine/CombatEngine.cs`
```
+ constructor parameter: IMenuNavigator navigator
~ ShowCombatMenu/action read, HandleAbilityMenu, CheckLevelUp: ReadLine → navigator.Select(...)
```

### Modified: `Program.cs`
```
+ var navigator = new ConsoleMenuNavigator();
~ pass navigator into ConsoleDisplayService, GameLoop, CombatEngine
```

### Unchanged
- `IInputReader` / `ConsoleInputReader` / `FakeInputReader` — untouched
- `IDisplayService` interface — signature unchanged (SelectDifficulty, SelectClass stay)
- `FakeDisplayService` — untouched; existing tests unaffected
- All 125+ existing tests — must remain green after Phase 1

---

*Awaiting Anthony's approval before any implementation begins. Phase 1 (infrastructure only) can be assigned to Hill (interface + ConsoleMenuNavigator) and Romanoff (FakeMenuNavigator + test harness update) in parallel.*
