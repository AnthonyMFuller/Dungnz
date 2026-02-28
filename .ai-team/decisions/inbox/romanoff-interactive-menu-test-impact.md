# Test Impact Assessment: Interactive Arrow-Key Menu Navigation

**Author:** Romanoff (Tester/QA)
**Date:** 2026-06-13
**Requested by:** Anthony Fuller
**Feature:** Convert text-command menus to interactive arrow-key navigable menus (↑/↓ + Enter)

---

## Baseline

**Total test methods:** 611 (`[Fact]` + `[Theory]` across all `.cs` files in `Dungnz.Tests/`)  
**Current build/test status:** Build succeeds (0 warnings, 0 errors). All tests pass (confirmed via `dotnet test`).

---

## A. How Many Tests Currently Rely on Text-Input Menu Simulation?

**11 test files** use `FakeInputReader` or a custom `IInputReader` implementation to simulate player input. Total instantiation count: **~49 constructor call sites**.

| Test File | FakeInputReader Usages | Notes |
|---|---|---|
| `CombatEngineTests.cs` | 23 | Every test; uses "A", "F", "B"+"1" patterns |
| `Phase1DisplayTests.cs` | 8 | Combat flow + ability sub-menu ("B", "1") |
| `Phase6ClassAbilityTests.cs` | 3 | Ability sub-menus ("B", "1" through "B", "5") |
| `RngDeterminismTests.cs` | 4 | All attack sequences ("A"×8) |
| `IntegrationTests.cs` | 3 | Attack + trait selection ("A", "1") |
| `SellSystemTests.cs` | 2 (+ 10 via `MakeSellSetup`) | Text commands + numeric item selection + Y/N confirm |
| `Phase8ASetBonusCombatTests.cs` | 2 | Attack sequences |
| `GameLoopTests.cs` | 1 (+ 20 via `MakeLoop`) | All 20 tests route through `FakeInputReader` for exploration commands |
| `IntroSequenceTests.cs` | 1 | Used but `SelectDifficulty`/`SelectClass` already handled by `FakeDisplayService` |
| `Display/ShowEnemyArtTests.cs` | 1 | Single attack to trigger combat |
| `ColorizeDamageTests.cs` | 1 | Attack sequence |
| `CombatBalanceSimulationTests.cs` | 0 (custom) | Uses `AlwaysAttackInputReader : IInputReader` inline class |

**Tests using numeric sub-menu selection specifically** (the inputs most directly affected by arrow-key conversion):

| Pattern | Files | Approximate Test Count |
|---|---|---|
| `"B", "1"` (ability sub-menu entry + select) | CombatEngineTests, Phase1DisplayTests, Phase6ClassAbilityTests, IntegrationTests | ~10 tests |
| `"1"` for trait/level-up selection | CombatEngineTests, IntegrationTests | ~2 tests |
| `"sell", "1", "Y/N"` (sell menu item + confirm) | SellSystemTests | ~7 tests |
| `"sell", "x"` or `"shop", "sell"` (cancel/navigate) | SellSystemTests | ~5 tests |

**~24 tests directly simulate numeric/letter sub-menu selection.** The remaining ~54 tests using `FakeInputReader` use single-letter combat commands (`"A"`, `"F"`) — whether these are affected depends on whether the combat action menu also converts to arrow-key navigation.

---

## B. What Would FakeInputReader Need to Simulate Arrow-Key Navigation?

Currently, `FakeInputReader` is:

```csharp
public class FakeInputReader : IInputReader
{
    private readonly Queue<string> _inputs;
    public string? ReadLine() => _inputs.Count > 0 ? _inputs.Dequeue() : "quit";
}
```

`IInputReader` has only `ReadLine()`. Arrow-key navigation requires `Console.ReadKey()` returning `ConsoleKeyInfo`, which `IInputReader` does not expose.

**Option 1 — Extend `IInputReader` with `ReadKey()`:**
```csharp
public interface IInputReader
{
    string? ReadLine();
    ConsoleKeyInfo ReadKey();  // new
}
```
`FakeInputReader` would need a second queue: `Queue<ConsoleKey>`. Test setup becomes:
```csharp
new FakeInputReader(
    keys: new[] { ConsoleKey.DownArrow, ConsoleKey.Enter },
    lines: new[] { "quit" }
);
```
- **Risk:** Adding a method to `IInputReader` is a breaking change — all existing `IInputReader` implementors (including `AlwaysAttackInputReader` in `CombatBalanceSimulationTests.cs`) must be updated.

**Option 2 — New `IMenuNavigator` interface (preferred, additive):**
```csharp
public interface IMenuNavigator
{
    ConsoleKey ReadKey();
}
```
New test helper `FakeMenuNavigator` with a `Queue<ConsoleKey>`. Interactive menus call `IMenuNavigator`; text-based menus continue using `IInputReader`. No existing tests break.

**Key point:** Tests would need to queue navigation sequences like:
```csharp
// Select item at index 1 (navigate down once, then confirm)
new FakeMenuNavigator(ConsoleKey.DownArrow, ConsoleKey.Enter)
```
versus the current:
```csharp
new FakeInputReader("B", "1", "F")
```

---

## C. Would Any Tests Break Immediately If We Add a New `IMenuNavigator` Interface?

**If `IMenuNavigator` is a new, separate interface (Option 2 above): zero tests break on interface introduction alone.** It is purely additive.

**Tests that WILL break when menu logic is migrated** (the read-loop in `GameLoop`, `CombatEngine`, and `SellSystem` switches from `ReadLine()` to `ReadKey()`):

- `CombatEngineTests.cs` — all 23 tests: currently drives combat via `"A"`/`"F"`/`"B"+"1"` strings. If combat menu becomes arrow-key, all 23 fail with no input consumed.
- `Phase1DisplayTests.cs` — 8 tests
- `Phase6ClassAbilityTests.cs` — 3 tests
- `SellSystemTests.cs` — ~11 tests that invoke the sell flow via `"sell", "1", "Y"` sequences
- `IntegrationTests.cs` — tests using `"A", "1"` would break (the `"1"` trait-selection is a sub-menu)
- `Phase6ClassAbilityTests.cs` — `"B", "1", "B", "1", "B", "1", "B", "5"` sequences would all fail
- `CombatBalanceSimulationTests.cs` — `AlwaysAttackInputReader` returns `"A"` from `ReadLine()`; combat action loop switch to `ReadKey()` would break all 5 simulation tests

**Special case — `AlwaysAttackInputReader`:** This is an inline class in `CombatBalanceSimulationTests.cs` (line 273) that implements `IInputReader`. If `IInputReader` gets a `ReadKey()` method (Option 1), this class will fail to compile without modification. This is a concrete compilation break, not just a runtime failure.

---

## D. What New Test Patterns Would We Need for Interactive Menus?

### 1. `FakeMenuNavigator` helper
```csharp
public class FakeMenuNavigator : IMenuNavigator
{
    private readonly Queue<ConsoleKey> _keys;
    public FakeMenuNavigator(params ConsoleKey[] keys) => _keys = new Queue<ConsoleKey>(keys);
    public ConsoleKey ReadKey() => _keys.Count > 0 ? _keys.Dequeue() : ConsoleKey.Escape;
}
```

### 2. Menu rendering verification on `FakeDisplayService`
Interactive menus need to re-render on each keystroke (showing cursor position). `FakeDisplayService` would need:
```csharp
public List<(string[] Options, int SelectedIndex)> MenuRenders { get; } = new();
public void ShowInteractiveMenu(string[] options, int selectedIndex) 
    => MenuRenders.Add((options, selectedIndex));
```
Tests can then assert that `MenuRenders.Last().SelectedIndex == 1` after pressing ↓.

### 3. New test assertion pattern (replacing string-sequence assertions)
**Old pattern:**
```csharp
var input = new FakeInputReader("B", "1", "F");
// run combat...
display.AllOutput.Should().Contain("used ability");
```
**New pattern:**
```csharp
var nav = new FakeMenuNavigator(
    ConsoleKey.DownArrow,   // move to ability row
    ConsoleKey.Enter,       // select Abilities submenu
    ConsoleKey.Enter,       // select first ability (already highlighted)
    ConsoleKey.UpArrow,     // navigate to Flee
    ConsoleKey.Enter        // select Flee
);
// run combat...
display.AllOutput.Should().Contain("used ability");
```

### 4. Sell/Shop menu pattern
Current: `"sell", "1", "Y"` (command + index + confirm)  
New: `nav(Enter[open sell], DownArrow, Enter[select item 2], Enter[confirm])` — or the confirm step may remain text ("Y"/"N"), depending on implementation.

### 5. Hybrid approach (recommended for migration)
If menus support **both** text fallback and arrow-key navigation (e.g., pressing Enter with no movement selects item 1, pressing a number still works), a large portion of existing tests could be kept as-is, limiting new test authoring to ~24 sub-menu tests only.

---

## E. Risk Level

**MEDIUM-HIGH**

| Factor | Assessment |
|---|---|
| Test files directly affected | 11 of ~48 test files |
| Tests requiring rewrite if combat menu migrates | ~78 tests (all FakeInputReader-based tests) |
| Tests requiring rewrite if only sub-menus migrate | ~24 tests (sub-menu numeric selection only) |
| New test infrastructure required | `FakeMenuNavigator`, `IMenuNavigator`, new FakeDisplayService menu methods |
| Compilation break risk | High if `IInputReader` is modified; zero if new `IMenuNavigator` interface is additive |
| Existing precedent | `SelectDifficulty`/`SelectClass` already bypass `IInputReader` via `IDisplayService` — this pattern works |

**Why not "High":** The `SelectDifficulty`/`SelectClass` pattern already demonstrates this architecture working in tests. `FakeDisplayService` handles those via configurable return values with no `FakeInputReader` involvement. If the same pattern is applied to shop/sell/combat menus (interactive selection is encapsulated in `IDisplayService` methods that return an index), the test infrastructure change is contained and manageable.

**Why not "Low":** The `CombatEngine` test suite (23 tests, the largest single block) drives combat entirely through `FakeInputReader` string sequences including `"B"+"1"` ability sub-menus. Full combat menu conversion to arrow-key would require rewriting every one of those tests. `CombatBalanceSimulationTests.cs` has an inline `IInputReader` implementation that would fail to compile under interface extension.

---

## Summary & Recommendations

1. **Use the `IDisplayService` encapsulation pattern** (like `SelectDifficulty`/`SelectClass`) for interactive menus: each interactive menu call returns the selected index directly. This keeps `IInputReader` unchanged and zero existing tests break.

2. **Avoid extending `IInputReader` with `ReadKey()`** — it would force updates to `AlwaysAttackInputReader` and every other `IInputReader` implementation.

3. **Create `FakeMenuNavigator : IMenuNavigator`** as a new test helper alongside `FakeInputReader`. Do not modify `FakeInputReader`.

4. **The 24 sub-menu tests** (ability selection, sell item selection) will need to be rewritten regardless — they encode the old text-index protocol. Budget for this.

5. **If combat action menu (A/F/B) also converts**, budget for rewriting ~78 additional tests across `CombatEngineTests`, `Phase1DisplayTests`, `RngDeterminismTests`, `Phase8ASetBonusCombatTests`, `ColorizeDamageTests`, `ShowEnemyArtTests`, `IntegrationTests`.

**Recommended scope boundary:** Convert shop/sell/inventory/ability sub-menus to arrow-key (24 tests to rewrite). Keep top-level combat actions (A/F/B) as letter-key shortcuts for now. This limits the immediate test surface impact and avoids rewriting the entire combat test suite in one sprint.
