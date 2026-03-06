## Hill's Menu Bug Analysis — 2026-03-05

### Bug 1: No Escape Key Handling in ContentPanelMenuNullable
- **Location:** `ContentPanelMenuNullable<T>`, lines ~599-612
- **Defect:** The method reads keys in a `while(true)` loop but only handles UpArrow, DownArrow, and Enter. Escape key (and Q) are not handled. When player presses Escape or Q to cancel, the loop continues infinitely consuming keypresses. The menu never exits, and control never returns to `ReadCommandInput`.
- **Player symptom:** After opening inventory menu, pressing Escape/Q does nothing. The menu appears stuck. Subsequent keypresses are consumed by the infinite loop but never processed. The Command panel becomes unresponsive because `ReadCommandInput` is never called again.
- **Fix:** Add Escape key handling to return null (cancel):
```csharp
case System.ConsoleKey.Escape:
    return null;  // or return the last item if it's the Cancel option
```

### Bug 2: No Escape Key Handling in ContentPanelMenu
- **Location:** `ContentPanelMenu<T>`, lines ~562-575
- **Defect:** Same as Bug 1. The non-nullable variant also lacks Escape key handling. This affects all menus using `SelectionPromptValue<T>` when Live is active.
- **Player symptom:** Any menu using non-nullable selection (difficulty select, class select, combat menu, shop menu, etc.) cannot be cancelled via Escape. Player must make a selection or close the game.
- **Fix:** Add Escape key handling. Since this is non-nullable, either return the currently selected value or throw an exception to signal cancellation.

### Bug 3: No Escape Key Handling in ShowSkillTreeMenu
- **Location:** `ShowSkillTreeMenu`, lines ~438-446
- **Defect:** The skill tree has inline key handling (not using ContentPanelMenu) and also only handles UpArrow, DownArrow, and Enter. No Escape key support.
- **Player symptom:** Player cannot cancel skill tree menu with Escape, must navigate to "← Cancel" option and press Enter.
- **Fix:** Add Escape key case to return null.

### Bug 4: Content Panel Not Restored After Menu Exit
- **Location:** `ShowInventoryAndSelect`, `ShowEquipMenuAndSelect`, `ShowUseMenuAndSelect`, `ShowTakeMenuAndSelect`, all menu methods, lines ~98-351
- **Defect:** When a menu displays in the Content panel via `SetContent()`, the previous content (adventure narration) is overwritten. When the menu exits (via Enter selecting Cancel or via Escape if fixed), the Content panel still shows the menu. The previous narration is lost. `SetContent()` clears `_contentLines`.
- **Player symptom:** After exiting inventory menu, the Content panel still shows "📦 Inventory" menu text instead of returning to the adventure narration. The game state has advanced but the player can't see the current room description.
- **Fix:** Before showing a menu, save the current content state. After menu exits, restore it. Or: use a different panel for menus (not Content). Or: refresh the adventure display after menu exits.

### Bug 5: No Explicit Content Refresh After Menu Methods
- **Location:** All menu methods called by command handlers, e.g. `ShowInventoryAndSelect` line ~98
- **Defect:** Menu methods like `ShowInventoryAndSelect` use `SetContent()` which replaces the content panel. After they return, there's no explicit call to restore/refresh the content to show the current room/narration. The game loop expects the display state to remain consistent, but the menu has mutated it.
- **Player symptom:** Same as Bug 4 — Content panel shows stale menu text.
- **Fix:** After any menu that uses `SetContent()`, explicitly restore the content panel. Either:
  1. Command handlers call `_display.ShowMessage()` or similar to refresh, OR
  2. Menu methods restore content before returning, OR
  3. `ReadCommandInput()` calls a content refresh before accepting next command.

### Bug 6: ReadCommandInput Does Not Verify Live State Before Starting
- **Location:** `ReadCommandInput`, line ~461
- **Defect:** The method directly calls `AnsiConsole.Console.Input.ReadKey(intercept: true)` without checking if Live display is active. It updates the Input panel via `UpdateCommandInputPanel()` which calls `_ctx.UpdatePanel()`. If Live is not active (stopped or paused), panel updates may fail or behave unexpectedly.
- **Player symptom:** If Live display is somehow stopped or paused without proper resume, `ReadCommandInput` may hang or fail to render input properly. Input panel may not update to show typed characters.
- **Fix:** Add `if (!_ctx.IsLiveActive) { /* fallback */ }` check at start of `ReadCommandInput()`. Or ensure Live is always active before calling this method.

### Bug 7: PauseAndRun Can Leave Live Paused on Exception
- **Location:** `PauseAndRun<T>`, lines ~521-537
- **Defect:** The method sets `_pauseLiveEvent.Set()` and then executes the action in a try/finally. The finally block calls `_resumeLiveEvent.Set()`. However, if the action throws an exception, the finally does run, but the `_pauseDepth` decrement happens in the finally. If `_pauseDepth` was 1, it becomes 0 and `_resumeLiveEvent.Set()` is called. This is correct. However, if an exception occurs before the decrement (e.g. in the `Interlocked.Increment` logic or in the action itself before try), the pause/resume events may be in an inconsistent state.
- **Player symptom:** After an exception in a menu method, the Live display may remain paused indefinitely. The screen freezes. No further input is accepted.
- **Fix:** Ensure the finally block always runs. The current code looks correct, but add logging or guards to detect pause/resume state mismatches.

### Bug 8: No Q Key Handling for Quick Cancel
- **Location:** All `ContentPanelMenu*` methods, lines ~562-612
- **Defect:** Many games support 'Q' as a quick cancel key. The current implementation does not. Players may press Q expecting to cancel, but it's ignored.
- **Player symptom:** Pressing 'Q' does nothing in menus. Player must use Escape or navigate to Cancel option.
- **Fix:** Add case for Q key to return null/cancel:
```csharp
case System.ConsoleKey.Q:
    return null;
```

### Bug 9: Input Panel Not Cleared Before ReadCommandInput
- **Location:** `ReadCommandInput`, line ~461 and `RunLoop`, line ~250
- **Defect:** Before calling `ReadCommandInput()`, the `RunLoop` calls `ShowCommandPrompt()` which sets the panel to "> Command:". Then `ReadCommandInput()` calls `UpdateCommandInputPanel("")` to clear it and start accepting input. However, if a menu method left the Input panel in a bad state (e.g. with stale text), the initial `ShowCommandPrompt()` call should clear it. But the sequence is: ShowCommandPrompt → ReadCommandInput → UpdateCommandInputPanel. The first two calls both update the panel. This is inefficient and may cause flicker.
- **Player symptom:** Input panel may flicker or show stale content briefly before accepting input.
- **Fix:** Merge the panel clearing into `ReadCommandInput()` and don't call `ShowCommandPrompt()` before it. Or: ensure `ShowCommandPrompt()` fully resets the panel state so `UpdateCommandInputPanel("")` is redundant.

### Bug 10: No Detection of Escape vs Cancel Option Selection
- **Location:** All menu methods using `NullableSelectionPrompt`, lines ~635-650
- **Defect:** When a menu returns null, it's ambiguous: did the player press Escape (if we add that handling), or did they navigate to "← Cancel" and press Enter? For logging and UX, these should be distinguishable. The command handler sees null and treats both the same.
- **Player symptom:** No immediate symptom, but analytics/logging cannot distinguish "player cancelled quickly" from "player navigated to Cancel".
- **Fix:** Return a discriminated union or special sentinel value for Escape vs explicit Cancel selection. Or: log the difference in the menu method before returning null.

### Bug 11: No Content Restoration When Escape Is Eventually Added
- **Location:** All menu methods, lines ~98-351
- **Defect:** Once Escape key handling is added (Bugs 1-3), menus will return null immediately. But the Content panel still shows the menu text because `SetContent()` was called but no restoration logic exists. The menu exits cleanly but leaves UI garbage.
- **Player symptom:** After pressing Escape in inventory, the Content panel shows "📦 Inventory (0/10)" with no items listed (since the menu loop exited). The panel should show the current room narration instead.
- **Fix:** Before calling `SetContent()` in a menu, save `_contentLines`, `_contentHeader`, `_contentBorderColor`. After the menu exits (in finally block or after the menu call), restore them.

### Bug 12: Nested Menu Calls May Corrupt Content State
- **Location:** Example: `ShowInventoryAndSelect` → `ShowItemDetail` → `ShowEquipmentComparison`, lines ~98-60
- **Defect:** `ShowInventoryAndSelect` calls `SetContent()` to show the menu. If it returns an item, the command handler calls `ShowItemDetail()` and `ShowEquipmentComparison()`, both of which may also call `SetContent()`. The content state is mutated multiple times without restoration. If the player then opens another menu, the content state is lost.
- **Player symptom:** After viewing item detail in inventory, the Content panel shows the comparison table. When player exits back to game loop, the Content panel still shows the comparison table instead of room narration.
- **Fix:** Same as Bug 11 — save/restore content state around menu operations.

### Bug 13: ReadCommandInput Does Not Re-render After Menu Exits
- **Location:** `RunLoop`, lines ~250-251
- **Defect:** The loop calls `ShowCommandPrompt()` then `ReadCommandInput()`. If a command opens a menu (e.g. inventory), the menu uses `SetContent()` to render in the Content panel. When the menu exits, control returns to `RunLoop`, which loops back and calls `ShowCommandPrompt()` again. But `ShowCommandPrompt()` only updates the Input panel, not the Content panel. So the Content panel is left showing the menu.
- **Player symptom:** Same as Bugs 4-5 — stale menu content in Content panel after menu exits.
- **Fix:** After a command that might open a menu, call a method to refresh the Content panel to current room/narration state. Or: make menu methods responsible for restoring content before returning.

### Summary of Root Causes

1. **Missing Escape key handling** in all in-game menus (ContentPanelMenu, ContentPanelMenuNullable, ShowSkillTreeMenu).
2. **Content panel state not saved/restored** around menu operations that call `SetContent()`.
3. **No explicit content refresh** after menu methods return to game loop.

All three issues combine to create the reported symptom: player opens inventory, presses Escape (ignored), can't exit, and after forcing exit the Content panel shows stale menu content and Input panel is unresponsive.
