## Romanoff's Menu QA Analysis — 2026-03-04

**Critical Finding:** Menu cancellation leaves the Content Panel in menu state without restoring room display or calling ShowCommandPrompt(). The Command Input panel becomes unusable after cancel because ReadCommandInput() never returns, waiting for Enter key that will never come after menu exit.

---

### Command: inventory
- **Menu method called:** `ShowInventoryAndSelect(Player)`
- **Cancel return value:** `null` (last menu item with null value)
- **Caller handles null?** YES — InventoryCommandHandler.cs:9 checks `if (selectedItem != null)`
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO**
- **ShowCommandPrompt() called after menu?** **NO**
- **Bugs found:**
  1. **CRITICAL:** When user cancels (selects "← Cancel"), the ContentPanelMenuNullable returns null, inventory handler exits, but Content Panel is left showing the inventory menu markup. ShowRoom() is never called.
  2. **CRITICAL:** ReadCommandInput() is still active, waiting for Enter key. The content panel shows the menu, the input panel shows "> Command:", but typing and pressing Enter does nothing because the menu's ReadKey() loop consumed the keystrokes.
  3. **SHOW-STOPPER:** After cancel, the game loop calls ShowCommandPrompt() (GameLoop.cs:250) then ReadCommandInput() (GameLoop.cs:251), but ReadCommandInput() uses AnsiConsole.Console.Input.ReadKey() in a loop (SpectreLayoutDisplayService.Input.cs:471-492). This loop reads characters one at a time. HOWEVER, the ContentPanelMenuNullable() method (lines 583-613) ALSO uses ReadKey() and returns when Enter is pressed. After the menu returns null, the Content Panel is left in menu state, and ReadCommandInput() starts its own ReadKey() loop. The player is now typing into ReadCommandInput()'s character buffer, but the Content Panel still shows the inventory menu. This is a UX disaster.
  4. The problem: **No cleanup after menu exit**. The ContentPanelMenuNullable method calls SetContent() to render the menu (line 597), reads keys, returns the selected value, but never calls SetContent() again to clear the menu or restore the previous state.

**File/Line References:**
- InventoryCommandHandler.cs:7-18 — Gets selectedItem, checks if null, but NEVER calls ShowRoom() on cancel path
- SpectreLayoutDisplayService.Input.cs:98-109 — ShowInventoryAndSelect appends "← Cancel" with null value
- SpectreLayoutDisplayService.Input.cs:583-613 — ContentPanelMenuNullable renders menu, returns null on cancel, but never clears content
- GameLoop.cs:250 — Calls ShowCommandPrompt() before reading input
- GameLoop.cs:251 — Calls ReadCommandInput() which starts new ReadKey() loop

---

### Command: take (no argument, menu shown)
- **Menu method called:** `ShowTakeMenuAndSelect(IReadOnlyList<Item>)`
- **Cancel return value:** `null` (last menu item "← Cancel")
- **Caller handles null?** YES — TakeCommandHandler.cs:29 checks `if (selection == null)`
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO** (only called on successful take at line 85 or 116)
- **ShowCommandPrompt() called after menu?** **NO** (GameLoop calls it, but menu state is not cleared)
- **Bugs found:**
  1. **CRITICAL:** Same root cause as inventory. When cancel is selected, TakeCommandHandler returns immediately (line 29), Content Panel still shows "📦 Take Items" menu, ShowRoom() is never called to restore room state.
  2. TakeCommandHandler DOES call ShowRoom() after successful item pickup (lines 85, 116), but NOT on cancel path.
  3. TurnConsumed is set to false on cancel (line 29), which is correct, but display state is not restored.

**File/Line References:**
- TakeCommandHandler.cs:28-29 — Shows menu, checks null, returns without ShowRoom()
- TakeCommandHandler.cs:85 — ShowRoom() called ONLY on success
- TakeCommandHandler.cs:116 — ShowRoom() called ONLY after taking all

---

### Command: use (no argument, menu shown)
- **Menu method called:** `ShowUseMenuAndSelect(IReadOnlyList<Item>)`
- **Cancel return value:** `null` (last menu item "← Cancel")
- **Caller handles null?** YES — UseCommandHandler.cs:20 checks `if (selected == null)`
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO**
- **ShowCommandPrompt() called after menu?** **NO**
- **Bugs found:**
  1. **CRITICAL:** Same pattern. Cancel returns null (line 20), handler returns immediately, Content Panel shows "🎒 Use Item" menu permanently.
  2. No ShowRoom() call on any code path in UseCommandHandler. The use command modifies player state (heals, buffs, etc.) but never refreshes the room display.
  3. UseCommandHandler is 189 lines long and has ZERO calls to ShowRoom() or any display refresh method other than ShowMessage() and ShowError().

**File/Line References:**
- UseCommandHandler.cs:19-20 — Shows menu, checks null, returns without cleanup
- UseCommandHandler.cs:1-189 — ZERO calls to ShowRoom() in entire handler

---

### Command: equip (no argument, menu shown via EquipmentManager)
- **Menu method called:** `ShowEquipMenuAndSelect(IReadOnlyList<Item>)` (called from EquipmentManager.HandleEquip)
- **Cancel return value:** `null` (last menu item "← Cancel")
- **Caller handles null?** YES — EquipmentManager.cs:35 checks `if (selected == null) return`
- **State restored after cancel?** **NO** (EquipmentManager doesn't call ShowRoom)
- **ShowRoom() called after menu?** YES — EquipCommandHandler.cs:8 calls ShowRoom() AFTER EquipmentManager.HandleEquip() returns
- **ShowCommandPrompt() called after menu?** **NO** (GameLoop calls it, but menu state is not cleared)
- **Bugs found:**
  1. **MEDIUM:** EquipCommandHandler DOES call ShowRoom() after equip (line 8), which partially masks the issue. However, if EquipmentManager.HandleEquip() returns early due to cancel (EquipmentManager.cs:35), ShowRoom() is still called, but the Content Panel is left showing the equip menu until ShowRoom() refreshes it.
  2. **RACE CONDITION:** There's a brief moment where the Content Panel shows the equip menu, then ShowRoom() overwrites it. If the player types quickly, they might see the menu flicker.
  3. UnequipCommandHandler ALSO calls ShowRoom() (line 17), which is consistent.

**File/Line References:**
- EquipmentManager.cs:34-35 — Shows menu, checks null, returns
- EquipCommandHandler.cs:8 — DOES call ShowRoom() after EquipmentManager returns (GOOD)
- UnequipCommandHandler.cs:17 — DOES call ShowRoom() after unequip (GOOD)

---

### Command: compare (no argument, menu shown)
- **Menu method called:** `ShowEquipMenuAndSelect(IReadOnlyList<Item>)`
- **Cancel return value:** `null` (last menu item "← Cancel")
- **Caller handles null?** YES — CompareCommandHandler.cs:19-22 checks `if (selected == null)`
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO**
- **ShowCommandPrompt() called after menu?** **NO**
- **Bugs found:**
  1. **CRITICAL:** Same pattern. Cancel returns null (line 19), TurnConsumed = false (line 21), handler returns (line 22), Content Panel shows "⚔  Equip Item" menu.
  2. CompareCommandHandler has ZERO calls to ShowRoom() or any display cleanup.

**File/Line References:**
- CompareCommandHandler.cs:18-22 — Shows menu, checks null, returns without cleanup
- CompareCommandHandler.cs:1-53 — ZERO calls to ShowRoom()

---

### Command: skills (always shows menu)
- **Menu method called:** `ShowSkillTreeMenu(Player)`
- **Cancel return value:** `null` (last menu item "← Cancel")
- **Caller handles null?** YES — SkillsCommandHandler.cs:10 checks `if (skillToLearn.HasValue)`
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO**
- **ShowCommandPrompt() called after menu?** **NO**
- **Bugs found:**
  1. **CRITICAL:** Same root cause. Cancel returns null (Skills is nullable enum), handler checks HasValue (line 10), returns without cleanup.
  2. SkillsCommandHandler has ZERO calls to ShowRoom().

**File/Line References:**
- SkillsCommandHandler.cs:9-14 — Shows menu, checks HasValue, sets TurnConsumed = false (line 14) but no ShowRoom()
- SpectreLayoutDisplayService.Input.cs:403-458 — ShowSkillTreeMenu uses custom key handling but same pattern

---

### Command: shop (loops until leave)
- **Menu method called:** `ShowShopWithSellAndSelect()`
- **Cancel return value:** `0` (for Leave option)
- **Caller handles null?** N/A — returns int, checks `if (shopChoice == 0)` (ShopCommandHandler.cs:26)
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO**
- **ShowCommandPrompt() called after menu?** **NO**
- **Bugs found:**
  1. **CRITICAL:** When player selects "← Leave" (shopChoice == 0), handler shows two messages (lines 28-29) then returns. Content Panel shows "🏪 Merchant" menu.
  2. Shop loops (lines 19-66) to allow multiple purchases, but when exiting the loop (line 30), no ShowRoom() is called.
  3. ShopCommandHandler has ZERO calls to ShowRoom().

**File/Line References:**
- ShopCommandHandler.cs:26-30 — Checks if shopChoice == 0 (Leave), returns without ShowRoom()
- ShopCommandHandler.cs:1-69 — ZERO calls to ShowRoom()

---

### Command: sell (called from shop menu or standalone)
- **Menu method called:** `ShowSellMenuAndSelect()`
- **Cancel return value:** `0` (for Cancel option)
- **Caller handles null?** N/A — returns int, checks `if (idx == 0)` (SellCommandHandler.cs:28)
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO**
- **ShowCommandPrompt() called after menu?** **NO**
- **Bugs found:**
  1. **CRITICAL:** When player selects "← Cancel" (idx == 0), handler returns (line 29). Content Panel shows "💰 Sell Items" menu.
  2. SellCommandHandler has ZERO calls to ShowRoom().

**File/Line References:**
- SellCommandHandler.cs:27-29 — Shows menu, checks idx == 0, returns without cleanup
- SellCommandHandler.cs:1-50 — ZERO calls to ShowRoom()

---

### Command: craft (no argument, menu shown)
- **Menu method called:** `ShowCraftMenuAndSelect(IEnumerable<(string, bool)>)`
- **Cancel return value:** `0` (for cancelled option)
- **Caller handles null?** N/A — returns int, checks `if (selectedIndex == 0)` (CraftCommandHandler.cs:22)
- **State restored after cancel?** **NO**
- **ShowRoom() called after menu?** **NO**
- **ShowCommandPrompt() called after menu?** **NO**
- **Bugs found:**
  1. **CRITICAL:** When player cancels (selectedIndex == 0), handler returns (line 22). Content Panel shows "⚗  Crafting" menu.
  2. CraftCommandHandler has ZERO calls to ShowRoom().

**File/Line References:**
- CraftCommandHandler.cs:21-22 — Shows menu, checks selectedIndex == 0, returns without cleanup
- CraftCommandHandler.cs:1-62 — ZERO calls to ShowRoom()

---

### Commands that DON'T show menus but ARE tested
- **look:** Calls ShowRoom() (LookCommandHandler.cs:7) — GOOD
- **stats:** Calls ShowPlayerStats() and ShowMessage() (StatsCommandHandler.cs:9-10) — GOOD (no menu)
- **map:** Calls ShowMap() (MapCommandHandler.cs:7) — GOOD (no menu)
- **examine:** Shows item detail, calls ShowEquipmentComparison if equippable, but NO ShowRoom() at end — MEDIUM BUG

**File/Line References:**
- ExamineCommandHandler.cs:1-52 — Shows item info, comparison, but no ShowRoom() to restore context

---

### Commands that call ShowRoom() AFTER their operation (correct pattern)
- **equip:** EquipCommandHandler.cs:8 calls ShowRoom() after HandleEquip()
- **unequip:** UnequipCommandHandler.cs:17 calls ShowRoom() after HandleUnequip()
- **take:** TakeCommandHandler.cs:85, 116 calls ShowRoom() after successful pickup
- **go:** GoCommandHandler.cs:93, 159 calls ShowRoom() after movement
- **descend:** DescendCommandHandler.cs:50 calls ShowRoom() after floor transition
- **load:** LoadCommandHandler.cs:29 calls ShowRoom() after loading save

---

## Root Cause Analysis

**Primary Issue:** ContentPanelMenuNullable() (and ContentPanelMenu()) render menu content via SetContent() but never clear it or restore previous state when exiting.

**Code Location:** SpectreLayoutDisplayService.Input.cs:583-613

The method:
1. Renders menu to Content Panel (line 597: `SetContent(sb.ToString().TrimEnd(), title)`)
2. Reads keys in a loop (lines 599-612)
3. Returns selected value when Enter is pressed (line 610)
4. **NEVER clears the menu from the Content Panel**

**Why it breaks the command loop:**

After menu returns null:
1. Command handler checks null and returns
2. GameLoop.RunLoop() continues (line 250-251):
   ```csharp
   _display.ShowCommandPrompt(_player);  // Updates Input Panel to "> Command:"
   var input = _display.ReadCommandInput() ?? _input.ReadLine() ?? string.Empty;
   ```
3. ShowCommandPrompt() updates the Input Panel border/header but does NOT clear the Content Panel
4. ReadCommandInput() starts its own ReadKey() loop (SpectreLayoutDisplayService.Input.cs:471-492)
5. **Content Panel still shows the menu markup from the canceled menu**
6. Player sees the old menu in Content Panel, types in Input Panel, but the display is confusing
7. Worse: if the player pressed Escape or selected Cancel with arrow keys, those keystrokes were consumed by the menu's ReadKey() loop, so ReadCommandInput() never saw them

**Why ShowRoom() fixes it:**

ShowRoom() (SpectreLayoutDisplayService.cs:520-609) calls SetContent() at line 606, which overwrites the Content Panel with room description, clearing the old menu state.

Commands that call ShowRoom() after their operation (equip, take, go, etc.) accidentally hide this bug. Commands that don't call ShowRoom() (inventory, use, compare, skills, shop, sell, craft) expose it.

---

## Edge Cases NOT Currently Tested

1. **Cancel from every menu type:**
   - Inventory menu → cancel
   - Take menu → cancel
   - Use menu → cancel
   - Equip menu → cancel
   - Compare menu → cancel
   - Skills menu → cancel
   - Shop menu → leave
   - Sell menu → cancel
   - Craft menu → cancel
   - Ability menu (combat) → cancel
   - Combat item menu → cancel
   - Shrine menu → leave
   - Confirm menu (sell confirm) → No

2. **Empty collection menus:**
   - Inventory menu with 0 items (handled: ShowInventoryAndSelect returns null at line 100)
   - Take menu with 0 items (handled: TakeCommandHandler checks roomItems.Count == 0 at line 22)
   - Use menu with 0 usable items (handled: UseCommandHandler checks usable.Count == 0 at line 14)
   - Equip menu with 0 equippable items (handled: EquipmentManager checks equippable.Count == 0 at line 29)
   - Skills menu with 0 learnable skills (handled: ShowSkillTreeMenu returns null at line 421)

3. **Menu shown when another menu is already showing:**
   - NOT POSSIBLE with current architecture. ContentPanelMenu() and ContentPanelMenuNullable() are blocking synchronous methods. A second menu cannot start until the first returns.

4. **Rapid consecutive menu commands:**
   - NOT TESTED. Example: type "inventory" (Enter), press Escape, type "use" (Enter), press Escape, type "look" (Enter).
   - Expected: Each menu should show and hide cleanly.
   - Actual: Content Panel accumulates stale menu state until ShowRoom() or SetContent() is called.

5. **Menu cancel then immediate combat:**
   - Player cancels inventory menu (Content Panel shows inventory menu).
   - Player types "go north" (Enter).
   - GoCommandHandler triggers combat.
   - Combat menu shown via ShowCombatMenuAndSelect().
   - **QUESTION:** Does combat menu clear the old inventory menu, or do they stack?
   - ANSWER: ShowCombatMenuAndSelect() calls SelectionPromptValue(), which calls ContentPanelMenu(), which calls SetContent() at line 560. This DOES clear the old menu. Combat menu is safe.

6. **Menu during combat (abilities, items):**
   - Combat calls ShowAbilityMenuAndSelect() or ShowCombatItemMenuAndSelect().
   - Both use NullableSelectionPrompt() → ContentPanelMenuNullable() → SetContent().
   - When player cancels, CombatEngine checks if result is null and handles gracefully (CombatEngine.cs — not analyzed in detail, but pattern matches).
   - Combat menus DO clear Content Panel because they call SetContent().
   - **BUT:** After combat ends, does CombatEngine call ShowRoom() to restore room context? NEED TO CHECK.

---

## Specific Code Locations Where State Is Not Restored After Menu Cancellation

### Critical (breaks command input):
1. **InventoryCommandHandler.cs:7-18** — No ShowRoom() on cancel path (line 9 returns after null check)
2. **TakeCommandHandler.cs:28-29** — No ShowRoom() on cancel path (line 29 returns after null check)
3. **UseCommandHandler.cs:19-20** — No ShowRoom() on cancel path (line 20 returns after null check)
4. **CompareCommandHandler.cs:18-22** — No ShowRoom() on cancel path (line 22 returns after null check)
5. **SkillsCommandHandler.cs:9-14** — No ShowRoom() on cancel path (line 14 sets TurnConsumed then returns)
6. **ShopCommandHandler.cs:26-30** — No ShowRoom() on cancel path (line 30 returns after Leave)
7. **SellCommandHandler.cs:27-29** — No ShowRoom() on cancel path (line 29 returns after Cancel)
8. **CraftCommandHandler.cs:21-22** — No ShowRoom() on cancel path (line 22 returns after Cancel)

### Medium (UX confusion but not broken):
9. **ExamineCommandHandler.cs:28-50** — Shows item detail/comparison but no ShowRoom() to restore context

---

## Summary: Highest-Risk Paths

**Ordered by severity (most broken first):**

1. **CRITICAL — inventory → cancel:** Content Panel shows inventory menu permanently. ReadCommandInput() loop is active but player sees stale menu. CONFUSING UX. Type "look" to fix.

2. **CRITICAL — use → cancel:** Same issue. Content Panel shows use menu. Player must type command that calls ShowRoom() to restore.

3. **CRITICAL — shop → leave:** Same issue. Shop handler has complex loop logic but never calls ShowRoom() on exit.

4. **CRITICAL — take → cancel:** Same issue. Take handler DOES call ShowRoom() on success but not on cancel.

5. **CRITICAL — skills → cancel:** Same issue. Skills menu uses custom ContentPanelMenu but same SetContent() pattern.

6. **CRITICAL — compare → cancel:** Same issue. Compare shows menu, returns on null, no cleanup.

7. **CRITICAL — sell → cancel:** Same issue. Sell menu is simple but no ShowRoom() on cancel.

8. **CRITICAL — craft → cancel:** Same issue. Craft menu is simple but no ShowRoom() on cancel.

9. **MEDIUM — equip → cancel:** Partially masked because EquipCommandHandler DOES call ShowRoom() after HandleEquip() returns, but there's a brief moment where Content Panel shows equip menu before ShowRoom() overwrites it.

10. **MEDIUM — examine [item]:** Shows item detail and comparison but never calls ShowRoom() to restore room context. Content Panel left showing item stats.

---

## Recommended Fixes

### Option A: Add ShowRoom() to every command handler after menu cancel
- Pros: Localized fix, each handler is responsible for its own cleanup
- Cons: Violates DRY, 8+ handlers need changes, easy to forget in future handlers

### Option B: Add cleanup call to ContentPanelMenuNullable() before returning
- Pros: Centralized fix, all menus benefit
- Cons: Menu method would need to know what content to restore (room? item? previous state?), breaks separation of concerns

### Option C: GameLoop.RunLoop() calls ShowRoom() after every command
- Pros: Simple, guaranteed cleanup
- Cons: Wasteful (ShowRoom() called even when not needed), may flicker or overwrite intentional content changes

### Option D: Add ICommandHandler.Cleanup() method, call after every command
- Pros: Structured, extensible, handlers can opt-in to cleanup
- Cons: Requires interface change, all handlers must implement (even if empty)

### Option E: ContentPanelMenuNullable() calls ShowRoom() via callback before returning
- Pros: Centralized, menu is responsible for cleanup
- Cons: Menu method needs reference to current room, breaks encapsulation

**Romanoff's Recommendation:** **Option A** — Add ShowRoom() to each handler's cancel path. It's localized, testable, and makes the handler's intent explicit. Add a code comment: "// Restore room display after menu cancel" so future devs understand the pattern.

---

## Test Coverage Gaps

Current test suite (Dungnz.Tests/):
- CommandHandlerSmokeTests.cs exists but doesn't test menu cancel paths
- GameLoopTests.cs, GameLoopCommandTests.cs use FakeDisplayService which stubs menu methods
- FakeDisplayService.ShowInventoryAndSelect() reads from _input.ReadLine(), not realistic to real menu behavior
- ZERO tests for "menu cancel → verify Content Panel restored"
- ZERO tests for "menu cancel → verify next command works"

**Missing test scenarios:**
1. Inventory command → cancel → verify ShowRoom() called (or Content Panel cleared)
2. Use command → cancel → verify state restored
3. Take command → cancel → verify state restored
4. Shop command → leave → verify state restored
5. Craft command → cancel → verify state restored
6. Skills command → cancel → verify state restored
7. Compare command → cancel → verify state restored
8. Sell command → cancel → verify state restored
9. Rapid consecutive menu cancel sequence (inventory, cancel, use, cancel, look)
10. Menu cancel → combat → verify combat menu clears previous menu

---

## Anthony — This Is What's Broken

Your player opened inventory, pressed Escape or navigated to "← Cancel", pressed Enter. The menu closed (returned null), the InventoryCommandHandler checked null and returned. BUT:

1. The Content Panel still shows the inventory menu markup ("📦 Inventory (3/10)" header, list of items, "▶ ← Cancel" selected).
2. The Input Panel shows "> Command:" (correct).
3. The player typed a new command, pressed Enter.
4. Nothing happened. Or worse, the game parsed the command but the Content Panel still showed the old menu.

**Why:** ContentPanelMenuNullable() called SetContent() to render the menu, but never called SetContent() again to clear it. The InventoryCommandHandler never called ShowRoom() to restore the room display.

**How to reproduce:**
1. Run the game (with SpectreLayoutDisplayService, not TUI)
2. Type "inventory" (Enter)
3. Press Down arrow to select "← Cancel"
4. Press Enter
5. Content Panel now shows inventory menu frozen
6. Type "look" (Enter)
7. Content Panel updates to show room (ShowRoom() was called by LookCommandHandler)

**The fix:** Every command handler that shows a menu and can return early on cancel must call ShowRoom() before returning.

---

**Romanoff, 2026-03-04 — QA Analysis Complete**
