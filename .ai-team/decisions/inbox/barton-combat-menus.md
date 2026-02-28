# Decision: Arrow-Key Navigation — Combat, Level-Up, Crafting (WI-6+7+8)

**Date:** 2026-02-27  
**Author:** Barton (Systems Dev)  
**Branch:** feat/interactive-menus

---

## Decisions Made

### 1. SelectFromMenu<T> as private ConsoleDisplayService helper
`SelectFromMenu<T>` is implemented as a private instance method on `ConsoleDisplayService`, not on the interface. This keeps the interface clean — callers get domain-specific return types (int for level-up, string for combat, int for craft). The helper handles arrow-key + numbered fallback transparently.

### 2. ShowCombatMenuAndSelect returns shorthand string ("A"/"B"/"F")
Returns single-char strings matching the existing combat dispatch conditions (`"A"`, `"B"`, `"F"`). This minimises CombatEngine changes — the existing `if (choice == "F" || choice == "FLEE")` pattern still works.

### 3. Combat resource context shown inside ShowCombatMenuAndSelect
The mana/combo points/shield status lines are rendered inside `ShowCombatMenuAndSelect` using direct `Player` property access, not via AbilityManager. This avoids introducing a display→systems dependency. The separate `ShowCombatMenu(player)` private method in CombatEngine is now dead code (not deleted to avoid noise in the diff, but the call is removed).

### 4. ShowLevelUpChoiceAndSelect replaces both display + input in one call
`ShowLevelUpChoice(player)` (which shows the box-drawn card) is no longer called — `ShowLevelUpChoiceAndSelect` handles both rendering and selection. The old `ShowLevelUpChoice` method remains on the interface for backward compatibility but is no longer called from CombatEngine.

### 5. WI-8: Interactive CRAFT menu only on bare CRAFT command
`CRAFT <name>` still routes directly to `TryCraft` (no menu). The interactive recipe browser only activates when the player types `CRAFT` with no argument. This preserves power-user efficiency while improving discoverability.

### 6. SelectFromMenu fallback for tests
When `ReadKey()` returns `null` (test stubs, non-interactive stdin), `SelectFromMenu` falls back to numbered list + `ReadLine()` input. `FakeInputReader.ReadKey()` returns null, so existing tests driving menus via numbered `ReadLine()` inputs continue to work unchanged.

### 7. ConsoleDisplayService constructor optional params
Changed `ConsoleDisplayService(IInputReader input, IMenuNavigator navigator)` to have optional defaults (`?? new ConsoleInputReader()` / `?? new ConsoleMenuNavigator()`). This fixes test compilation broken by Hill's constructor addition (pre-existing in the working tree). Safe because the real app wires explicit instances via Program.cs.
