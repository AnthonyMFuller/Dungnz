# Decisions — Hill: Interactive Menus (WI-2 through WI-5)

**Date:** feat/interactive-menus branch  
**Author:** Hill  
**Commit:** a8dcb52

---

## Decision 1: `SelectFromMenu<T>` accepts `IInputReader` (not delegates to `IMenuNavigator`)

**Context:** Both `IInputReader` and `IMenuNavigator`/`ConsoleMenuNavigator` are available. The task spec required `SelectFromMenu` to accept `IInputReader` and handle null-ReadKey fallback.

**Decision:** `SelectFromMenu<T>` uses `IInputReader` directly for its key loop. `_navigator` is available in `ConsoleDisplayService` (injected via constructor) but not used by `SelectFromMenu`.

**Rationale:** Spec compliance; simpler null-fallback path via `IInputReader.ReadKey() == null → ReadLine()` rather than `Console.IsInputRedirected`.

---

## Decision 2: `ConsoleDisplayService` constructor uses optional parameters

**Signature:** `public ConsoleDisplayService(IInputReader? input = null, IMenuNavigator? navigator = null)`

**Rationale:** Multiple test classes call `new ConsoleDisplayService()` with zero arguments. Making parameters optional (with sane defaults) preserves BC without requiring test changes.

---

## Decision 3: `SelectClass()` retains card-rendering, replaces only input loop

**Context:** `SelectClass()` renders elaborate ASCII stat cards for all 6 classes, then waits for numbered input.

**Decision:** Card rendering is preserved as-is. Only the `while(true) ReadLine()` selection loop at the bottom is replaced with a `SelectFromMenu` call using simple icon + class name labels.

**Rationale:** The cards are informational (show stats, mana, passives). The arrow-key selection appears below them as a compact picker.

---

## Decision 4: `ShowShopAndSelect` / `ShowSellMenuAndSelect` return 1-based index (0 = cancel)

**Convention:** Matches the existing shop/sell convention in `GameLoop.cs` where `1` = first item, `0` = cancel/exit.

**FakeDisplayService stubs:** Both return `0` (cancel) by default — neutral/safe for tests that don't exercise shop flow.

---

## Decision 5: `SelectFromMenu` null probe — consume first keypress eagerly

**Pattern:** Call `input.ReadKey()` once before rendering to probe whether key input is available. If null → fall back to ReadLine loop. If non-null → render, then process that probe key as the first input.

**Rationale:** Avoids an extra `if (Console.IsInputRedirected)` check and works correctly with `FakeInputReader.ReadKey() == null`.
