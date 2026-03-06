### 2026-03-06: Menu/UI Bug Audit Findings
**By:** Romanoff
**What:** Exhaustive audit of all menu and UI interaction code
**Why:** Systemic menu bug quality failure — player reports new bug every session

---

## Confirmed Bugs

### BUG-001: ContentPanelMenu Escape/Q Returns Last Item Instead of Cancel Sentinel
**FILE:** Display/Spectre/SpectreLayoutDisplayService.Input.cs
**LINE:** 583-585
**SEVERITY:** High
**DESCRIPTION:** In `ContentPanelMenu<T>`, when user presses Escape or Q, the method returns `items[items.Count - 1].Value` (the last item in the list, which is typically the "Cancel" or "Leave" option) instead of treating Escape/Q as a special cancel signal distinct from explicitly selecting the cancel option.
**REPRODUCTION:** 
1. Use any menu that calls `SelectionPromptValue<T>` while Live is active (shop, level-up, difficulty selection, class selection, shrine, etc.)
2. Navigate to any option other than "Cancel" at the bottom
3. Press Escape or Q
4. The method returns `items[items.Count - 1].Value` regardless of what was selected
**IMPACT:** 
- If the last menu item has a non-cancel value, Escape will trigger that action instead of canceling
- This is a logic error but may not manifest as a bug IF all menus structure their last item as the cancel/leave option with a cancel-indicating value (0, null, etc.)
- Differs from `ContentPanelMenuNullable<T>` which correctly returns `null` on Escape/Q (line 625)
- Inconsistent behavior between nullable and non-nullable menu variants
**ROOT CAUSE:** History reveals this was flagged as BUG-D in the 2026-03-04 merchant sell flow audit but marked as Display layer bug, not command handler bug. It was noted but never fixed.

---

### BUG-002: InventoryCommandHandler Does Not Call ShowRoom on Cancel When Item Selected
**FILE:** Engine/Commands/InventoryCommandHandler.cs
**LINE:** 14-28
**SEVERITY:** Medium
**DESCRIPTION:** When `ShowInventoryAndSelect` returns a selected item (non-null), the handler shows item detail and comparison, but never calls `ShowRoom` to restore the room view. The content panel remains stuck on the comparison view. Only when user selects cancel (null) does line 27 call `ShowRoom`.
**REPRODUCTION:**
1. Type `INVENTORY`
2. Select any item from the menu
3. Item detail and comparison appear in content panel
4. Type another command (e.g., `STATS`)
5. Content panel still shows item comparison, not room description
**IMPACT:** Player sees stale item comparison content overlaying subsequent command output. Display state corruption persists until `LOOK` or room navigation.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 23 (after showing comparison).

---

### BUG-003: UseCommandHandler Does Not Call ShowRoom After Menu Use for Consumables
**FILE:** Engine/Commands/UseCommandHandler.cs
**LINE:** 14-22
**SEVERITY:** Medium
**DESCRIPTION:** When `USE` command is invoked without an argument, a menu is shown (`ShowUseMenuAndSelect`). If user selects an item, it's consumed and messages are shown, but `ShowRoom` is never called to restore room view. The content panel remains on the "Use Item" menu. Only on cancel (line 20) is `ShowRoom` called.
**REPRODUCTION:**
1. Type `USE` (no argument)
2. Select any consumable from menu
3. Potion is consumed, messages appear
4. Content panel still shows "Use Item" menu header and/or stale menu content
5. Type `LOOK` to force refresh
**IMPACT:** After using an item via menu, player sees stale menu UI. Next command output may append to stale content or appear under menu artifacts.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 186 (end of consumable switch case, before break).

---

### BUG-004: CompareCommandHandler Does Not Call ShowRoom After Showing Comparison
**FILE:** Engine/Commands/CompareCommandHandler.cs
**LINE:** 51
**SEVERITY:** Medium
**DESCRIPTION:** After showing equipment comparison, the handler terminates without calling `ShowRoom`. Content panel remains on comparison view.
**REPRODUCTION:**
1. Type `COMPARE` or `COMPARE <item>`
2. Comparison is shown in content panel
3. Type another command (e.g., `STATS`)
4. Content panel still shows comparison, not room
**IMPACT:** Display state corruption — comparison view persists across commands.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 51.

---

### BUG-005: ExamineCommandHandler Does Not Call ShowRoom After Showing Item Detail
**FILE:** Engine/Commands/ExamineCommandHandler.cs
**LINE:** 28-46
**SEVERITY:** Medium
**DESCRIPTION:** When examining a room item or inventory item, `ShowItemDetail` is called (line 28, 36), potentially followed by `ShowEquipmentComparison` (line 42), but `ShowRoom` is never called. Content panel remains on item detail view.
**REPRODUCTION:**
1. Type `EXAMINE <item>`
2. Item detail appears in content panel
3. Type another command
4. Content panel still shows item detail
**IMPACT:** Display state corruption — item detail persists.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 46 (after the comparison block).

---

### BUG-006: StatsCommandHandler Does Not Call ShowRoom
**FILE:** Engine/Commands/StatsCommandHandler.cs
**LINE:** 9-11
**SEVERITY:** Low
**DESCRIPTION:** `ShowPlayerStats` updates the Stats/Gear panels but leaves the content panel unchanged. If previous command left menu UI in content panel, it persists. The message "Floor: X / Y" appends to whatever content is there.
**REPRODUCTION:**
1. Type `INVENTORY`, select an item (comparison appears)
2. Type `STATS`
3. Stats panel updates, but content panel still shows comparison
4. Floor message appends to stale content
**IMPACT:** Minor display artifact — content panel not refreshed. Not critical since stats are in dedicated panel, but content panel state is unpredictable.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 10 to ensure clean content panel state.

---

### BUG-007: MapCommandHandler Does Not Call ShowRoom
**FILE:** Engine/Commands/MapCommandHandler.cs
**LINE:** 7
**SEVERITY:** Low
**DESCRIPTION:** Similar to StatsCommandHandler — map is updated but content panel is not refreshed.
**REPRODUCTION:**
1. After any menu command (shop, inventory), type `MAP`
2. Map panel updates, content panel remains stale
**IMPACT:** Minor — map is in dedicated panel, but content panel state unpredictable.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 7.

---

### BUG-008: CraftCommandHandler Does Not Call ShowRoom After Cancelling Menu
**FILE:** Engine/Commands/CraftCommandHandler.cs
**LINE:** 22
**SEVERITY:** Medium
**DESCRIPTION:** When user cancels craft menu (`selectedIndex == 0`), handler returns without calling `ShowRoom`. Content panel remains on craft menu view.
**REPRODUCTION:**
1. Type `CRAFT`
2. Cancel the menu (Escape/Q or select Cancel)
3. Content panel stuck on craft menu
**IMPACT:** Display state corruption after craft menu cancel.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` before line 22 `return;` statement.

---

### BUG-009: CraftCommandHandler Does Not Call ShowRoom After Showing Recipe
**FILE:** Engine/Commands/CraftCommandHandler.cs
**LINE:** 32-40
**SEVERITY:** Medium
**DESCRIPTION:** After displaying `ShowCraftRecipe` (line 32) and attempting to craft (line 34), messages are shown but `ShowRoom` is never called. Content panel remains on recipe card view.
**REPRODUCTION:**
1. Type `CRAFT`, select a recipe
2. Recipe card appears with success/failure message
3. Type another command
4. Content panel still shows recipe card
**IMPACT:** Display state corruption after crafting attempt.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 40 (before the `return;` statement).

---

### BUG-010: TakeCommandHandler Cancel Does Not Always Call ShowRoom
**FILE:** Engine/Commands/TakeCommandHandler.cs
**LINE:** 29
**SEVERITY:** Medium
**DESCRIPTION:** When `ShowTakeMenuAndSelect` returns null (cancel), the handler sets `TurnConsumed = false`, calls `ShowRoom`, then returns. This is correct. HOWEVER, line 29 has `context.TurnConsumed = false; context.Display.ShowRoom(context.CurrentRoom); return;` all on one line with semicolons — while syntactically correct, it's a code smell and easy to misread. More importantly, after examining the code, this handler DOES call ShowRoom correctly on cancel AND after successful pickup (line 85, 116). This is NOT a bug — TakeCommandHandler is correctly implemented.
**SEVERITY:** N/A (not a bug)
**CORRECTION:** This was initially flagged as suspicious but code review confirms ShowRoom is called on all paths (cancel line 29, single item line 85, take all line 116). No bug.

---

### BUG-011: SkillsCommandHandler Does Not Call ShowRoom After Learning Skill
**FILE:** Engine/Commands/SkillsCommandHandler.cs
**LINE:** 9-18
**SEVERITY:** Low
**DESCRIPTION:** When `ShowSkillTreeMenu` returns a skill (non-null), `HandleLearnSpecificSkill` is called, which shows a message but never calls `ShowRoom`. Content panel remains on skill tree view. Only on cancel (line 16) is `ShowRoom` called.
**REPRODUCTION:**
1. Type `SKILLS`
2. Select a skill to learn
3. Skill learned, message appears
4. Content panel stuck on skill tree menu
**IMPACT:** Display state corruption after learning skill.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 12 (after `HandleLearnSpecificSkill`).

---

### BUG-012: EquipCommandHandler Always Calls ShowRoom (Not a Bug)
**FILE:** Engine/Commands/EquipCommandHandler.cs
**LINE:** 12
**SEVERITY:** N/A
**DESCRIPTION:** This handler unconditionally calls `ShowRoom` on line 12 after equip attempt. This is correct behavior.
**CORRECTION:** Not a bug — correctly implemented.

---

### BUG-013: HelpCommandHandler Does Not Call ShowRoom
**FILE:** Engine/Commands/HelpCommandHandler.cs
**LINE:** 7
**SEVERITY:** Low
**DESCRIPTION:** `ShowHelp` displays help content in content panel but does not restore room view afterward.
**REPRODUCTION:**
1. Type `HELP`
2. Help content appears
3. Type another command
4. If that command doesn't call ShowRoom, help content persists or creates display artifact
**IMPACT:** Minor — help is informational, but content panel not restored.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 7. HOWEVER, this may be intentional — help is meant to be read, and player can type `LOOK` to restore room view. Recommend flagging as "design decision" rather than bug.

---

### BUG-014: EquipmentCommandHandler Does Not Call ShowRoom
**FILE:** Engine/Commands/EquipCommandHandler.cs
**LINE:** 29
**SEVERITY:** Low
**DESCRIPTION:** Calls `context.Equipment.ShowEquipment(context.Player);` which likely sets content panel to equipment view, but never restores room view.
**REPRODUCTION:**
1. Type `EQUIPMENT`
2. Equipment display appears
3. Type another command
4. Content panel may retain equipment view
**IMPACT:** Minor display artifact.
**FIX:** Add `context.Display.ShowRoom(context.CurrentRoom);` after line 29.

---

## Suspected Issues (needs verification)

### SUSPECTED-001: ContentPanelMenuNullable May Not Handle Empty Lists
**FILE:** Display/Spectre/SpectreLayoutDisplayService.Input.cs
**LINE:** 595-628
**SEVERITY:** Low
**DESCRIPTION:** If `items` list is empty, `ContentPanelMenuNullable` will render an empty menu and wait for input. Escape/Q will return null (correct), but Enter on an empty list will attempt to access `items[0].Value` which will throw `ArgumentOutOfRangeException`.
**REPRODUCTION:** Pass an empty list to any method using `NullableSelectionPrompt` while Live is active.
**IMPACT:** Potential crash if empty inventory/shop/etc menu is shown.
**VERIFICATION NEEDED:** Check if callers guard against empty lists. Examine `ShowInventoryAndSelect`, `ShowEquipMenuAndSelect`, `ShowUseMenuAndSelect`, etc. Lines 105, 323, 335 check `Count == 0` and return null before calling menu — GUARDED. `ShowCombatItemMenuAndSelect` (line 315) guards. `ShowAbilityMenuAndSelect` (line 293) does not check if availableAbilities is empty but adds unavailable abilities and cancel option, so opts list is never empty — SAFE. `ShowTakeMenuAndSelect` (line 347) guards.
**CONCLUSION:** All callers guard against empty lists. Not a bug, but defensive coding in ContentPanelMenuNullable would be prudent.

---

### SUSPECTED-002: ContentPanelMenu May Not Handle Empty Lists
**FILE:** Display/Spectre/SpectreLayoutDisplayService.Input.cs
**LINE:** 556-588
**SEVERITY:** Low
**DESCRIPTION:** Similar to SUSPECTED-001 — if items list is empty, menu will crash on Enter. However, all callers of `SelectionPromptValue` provide non-empty lists (level-up has 3 options, difficulty has 3, class has 6, shrine has 5, etc.). These are hardcoded option lists, not dynamic.
**CONCLUSION:** Not a bug — all menus using this have fixed, non-empty option lists.

---

### SUSPECTED-003: ShowSkillTreeMenu Has Manual Key Handling with Potential Index Wrap Bug
**FILE:** Display/Spectre/SpectreLayoutDisplayService.Input.cs
**LINE:** 430-454
**SEVERITY:** Low
**DESCRIPTION:** ShowSkillTreeMenu uses a custom key-reading loop (lines 434-454) instead of calling ContentPanelMenuNullable. Line 448 wraps selected index with modulo, line 449 as well. If opts list is empty (line 427 check), method returns null — SAFE. If opts has 1 item (just Cancel), UpArrow/DownArrow will keep `selected = 0`, Enter returns `opts[0].Value` which is null (Cancel) — CORRECT. Escape/Q return null — CORRECT. Logic appears sound.
**CONCLUSION:** Not a bug — correctly handles empty and single-item cases.

---

### SUSPECTED-004: Nested Menu Calls (Shop → Sell) May Leave Stale State
**FILE:** Engine/Commands/ShopCommandHandler.cs
**LINE:** 34-37
**SEVERITY:** Low
**DESCRIPTION:** When user selects "Sell" option in shop menu (line 34-37), `ShopCommandHandler` calls `new SellCommandHandler().Handle()`. SellCommandHandler shows its own menu, loops through sell attempts, then calls `ShowRoom` on exit (line 52 of SellCommandHandler.cs). Control returns to ShopCommandHandler loop (line 38 `continue;`), which shows shop menu again. This SHOULD restore shop menu view, BUT if SellCommandHandler exited abnormally (e.g., exception), content panel might be left in sell menu state.
**REPRODUCTION:** Hard to reproduce — requires exception in SellCommandHandler mid-flow.
**IMPACT:** Low — only manifests on error paths.
**CONCLUSION:** Not a confirmed bug, but defensive coding would wrap sell handler call in try-finally to ensure shop menu is re-rendered.

---

## Untested Menu Paths

### UNTESTED-001: ContentPanelMenu Escape/Q Behavior
**FILE:** Display/Spectre/SpectreLayoutDisplayService.Input.cs
**LINE:** 583-585
**DESCRIPTION:** Zero tests verify that Escape/Q in ContentPanelMenu returns the last item's value. The bug (BUG-001) was identified but no test exercises this behavior. SellSystemTests.cs has menu tests but they use FakeDisplayService which simulates menu responses via queues, not actual ContentPanelMenu logic.
**TEST NEEDED:** `ContentPanelMenu_Escape_ReturnsLastItemValue()` — create menu with options (A=1, B=2, Cancel=0), navigate to A, press Escape, verify returns 0 (last item) not 1 (selected item).

---

### UNTESTED-002: ContentPanelMenuNullable Escape/Q Behavior
**FILE:** Display/Spectre/SpectreLayoutDisplayService.Input.cs
**LINE:** 624-625
**DESCRIPTION:** Zero tests verify that Escape/Q in ContentPanelMenuNullable returns null. Should be tested to document correct behavior and prevent regression.
**TEST NEEDED:** `ContentPanelMenuNullable_Escape_ReturnsNull()`.

---

### UNTESTED-003: InventoryCommandHandler Cancel Path
**FILE:** Engine/Commands/InventoryCommandHandler.cs
**LINE:** 27
**DESCRIPTION:** SellSystemTests.cs does not test INVENTORY command. No test verifies that canceling inventory menu calls ShowRoom.
**TEST NEEDED:** `Inventory_Cancel_CallsShowRoom()`.

---

### UNTESTED-004: InventoryCommandHandler Item Select Path
**FILE:** Engine/Commands/InventoryCommandHandler.cs
**LINE:** 16-24
**DESCRIPTION:** No test verifies that selecting an item from inventory menu shows item detail and comparison, and that ShowRoom is called afterward (BUG-002 confirms it's NOT called, so test would fail).
**TEST NEEDED:** `Inventory_SelectItem_ShowsDetailAndCallsShowRoom()` (will fail until BUG-002 fixed).

---

### UNTESTED-005: UseCommandHandler Menu Cancel Path
**FILE:** Engine/Commands/UseCommandHandler.cs
**LINE:** 20
**DESCRIPTION:** UseCommandHandler has no tests (no UseCommandTests.cs file exists). Menu cancel path is untested.
**TEST NEEDED:** `Use_CancelMenu_CallsShowRoom()`.

---

### UNTESTED-006: UseCommandHandler Menu Select Path
**FILE:** Engine/Commands/UseCommandHandler.cs
**LINE:** 19-22
**DESCRIPTION:** Menu item selection followed by consumable use is untested.
**TEST NEEDED:** `Use_SelectConsumableFromMenu_UsesItemAndCallsShowRoom()` (will fail until BUG-003 fixed).

---

### UNTESTED-007: CompareCommandHandler Menu Cancel Path
**FILE:** Engine/Commands/CompareCommandHandler.cs
**LINE:** 22
**DESCRIPTION:** CompareCommandHandler has no tests (no CompareCommandTests.cs file exists). Menu cancel path is untested.
**TEST NEEDED:** `Compare_CancelMenu_CallsShowRoom()`.

---

### UNTESTED-008: CompareCommandHandler Menu Select Path
**FILE:** Engine/Commands/CompareCommandHandler.cs
**LINE:** 51
**DESCRIPTION:** Selecting an item and showing comparison is untested.
**TEST NEEDED:** `Compare_SelectItem_ShowsComparisonAndCallsShowRoom()` (will fail until BUG-004 fixed).

---

### UNTESTED-009: ExamineCommandHandler Inventory Item Path
**FILE:** Engine/Commands/ExamineCommandHandler.cs
**LINE:** 33-46
**DESCRIPTION:** ExamineCommandHandler has no tests (no ExamineCommandTests.cs file exists). Examining inventory item with comparison is untested.
**TEST NEEDED:** `Examine_InventoryItem_ShowsDetailAndCallsShowRoom()` (will fail until BUG-005 fixed).

---

### UNTESTED-010: CraftCommandHandler Menu Cancel Path
**FILE:** Engine/Commands/CraftCommandHandler.cs
**LINE:** 22
**DESCRIPTION:** CraftCommandHandler has tests in CraftingSystemTests.cs but they test CraftingSystem.TryCraft, not the command handler menu flow. No test for cancel.
**TEST NEEDED:** `Craft_CancelMenu_CallsShowRoom()` (will fail until BUG-008 fixed).

---

### UNTESTED-011: CraftCommandHandler Menu Select Path
**FILE:** Engine/Commands/CraftCommandHandler.cs
**LINE:** 32-40
**DESCRIPTION:** Selecting a recipe and crafting is untested at command handler level.
**TEST NEEDED:** `Craft_SelectRecipe_CraftsAndCallsShowRoom()` (will fail until BUG-009 fixed).

---

### UNTESTED-012: SkillsCommandHandler Menu Cancel Path
**FILE:** Engine/Commands/SkillsCommandHandler.cs
**LINE:** 16
**DESCRIPTION:** SkillsCommandHandler has no tests (no SkillsCommandTests.cs file exists). Menu cancel path is untested.
**TEST NEEDED:** `Skills_CancelMenu_CallsShowRoom()`.

---

### UNTESTED-013: SkillsCommandHandler Menu Select Path
**FILE:** Engine/Commands/SkillsCommandHandler.cs
**LINE:** 9-18
**DESCRIPTION:** Selecting a skill to learn is untested.
**TEST NEEDED:** `Skills_SelectSkill_LearnsAndCallsShowRoom()` (will fail until BUG-011 fixed).

---

### UNTESTED-014: All ShowConfirmMenu Implementations
**FILE:** Display/Spectre/SpectreLayoutDisplayService.Input.cs
**LINE:** 245-253
**DESCRIPTION:** ShowConfirmMenu is used in SellCommandHandler (line 36), but no test verifies the display layer menu behavior. FakeDisplayService simulates Yes/No via queue, but actual ContentPanelMenu logic for Yes/No is not tested.
**TEST NEEDED:** `ShowConfirmMenu_SelectYes_ReturnsTrue()`, `ShowConfirmMenu_SelectNo_ReturnsFalse()`, `ShowConfirmMenu_Escape_ReturnsFalse()`.

---

## Summary

**Confirmed Bugs:** 14 (11 requiring fixes, 3 false positives)
**Suspected Issues:** 4 (all low severity, require verification)
**Untested Menu Paths:** 14 critical menu flows with zero test coverage

**Critical Findings:**
1. **Display state restoration is systematically broken** — 9 command handlers fail to call `ShowRoom` after menu interactions, leaving stale content in the content panel
2. **ContentPanelMenu Escape/Q logic is incorrect** (BUG-001) but may not manifest as user-visible bug due to menu structure conventions
3. **Zero integration tests for menu cancel paths** — all menu tests use FakeDisplayService queues, not actual menu navigation
4. **Command handlers with interactive menus have 0% test coverage** — Use, Compare, Examine, Craft, Skills handlers have no test files

**Recommended Actions:**
1. Fix all 11 confirmed bugs by adding `ShowRoom` calls after menu interactions
2. Add integration tests for all menu cancel paths using actual SpectreLayoutDisplayService (not FakeDisplayService)
3. Fix BUG-001 (ContentPanelMenu Escape logic) to return cancel sentinel, not last item value
4. Create test files: UseCommandTests.cs, CompareCommandTests.cs, ExamineCommandTests.cs, SkillsCommandTests.cs
5. Add ContentPanelMenu/ContentPanelMenuNullable unit tests to verify Escape/Q behavior

**Root Cause Analysis:**
The systemic failure is that command handlers treat `ShowRoom` as optional. There is no enforced pattern that "every command that changes display state must restore room view." Recommendation: Create a CommandHandlerBase class with a `finally` block that calls `ShowRoom`, or add a post-command hook in GameLoop that unconditionally calls `ShowRoom` after every command (unless command explicitly opts out).
