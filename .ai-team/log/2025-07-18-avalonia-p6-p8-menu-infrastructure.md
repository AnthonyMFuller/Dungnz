# Session: 2025-07-18 — Avalonia P6–P8: Menu Input Infrastructure + All 23 Stubs

**Requested by:** Anthony  
**Team:** Hill  

---

## What They Did

### Hill — Avalonia P6–P8 Implementation

**PR:** #1434 | **Branch:** `squad/1428-avalonia-menu-infrastructure`  
**Issues:** #1428, #1429, #1430, #1431, #1432, #1433

Replaced all 23 hardcoded input stubs in `AvaloniaDisplayService.cs` with real implementations using a text-based numbered menu system. Updated `App.axaml.cs` to use `StartupOrchestrator` instead of hardcoded startup values.

**Files changed (2):**

| File | Change |
|------|--------|
| `AvaloniaDisplayService.cs` | +583 lines. Added `WaitForTextInput()` helper, `SelectFromMenu<T>()` generic infrastructure, and all 23 method implementations. |
| `App.axaml.cs` | Replaced hardcoded startup (seed 12345, Adventurer, Warrior, Normal) with full `StartupOrchestrator` flow supporting New Game, Load Save, New Game with Seed, and Exit. |

#### Architecture: Text-Based Numbered Menus

All menus use a text-based approach:
1. Display numbered options in ContentPanel via `SetContent()`
2. Accept a number via the existing InputPanel text entry
3. Validate and return the corresponding value
4. Re-prompt on invalid input (error logged to LogPanel)

**Key helpers:**
- `WaitForTextInput(prompt)` — Extracted TCS bridge pattern from `ReadCommandInput` into reusable private method
- `SelectFromMenu<T>(header, options, allowCancel, cancelValue)` — Generic menu: displays numbered options, validates input, loops on invalid

#### Method Categories Implemented (23 total)

- **Startup (6):** `ReadPlayerName`, `ReadSeed`, `ShowStartupMenu`, `SelectDifficulty`, `SelectClass` (with stat cards + prestige), `SelectSaveToLoad`
- **Combat (4):** `ShowCombatMenuAndSelect` (mana/combo/shield info + letter shortcuts A/B/F/I), `ShowAbilityMenuAndSelect` (unavailable abilities as info lines), `ShowCombatItemMenuAndSelect`, `ShowTrapChoiceAndSelect`
- **Inventory (4):** `ShowInventoryAndSelect`, `ShowEquipMenuAndSelect`, `ShowUseMenuAndSelect`, `ShowTakeMenuAndSelect` (with Take All)
- **Economy (4):** `ShowShopAndSelect`, `ShowSellMenuAndSelect`, `ShowShopWithSellAndSelect` (buy/sell/leave), `ShowCraftMenuAndSelect`
- **Progression (2):** `ShowLevelUpChoiceAndSelect`, `ShowSkillTreeMenu` (locked as info, available as selectable)
- **Special Rooms (3):** `ShowShrineMenuAndSelect`, `ShowForgottenShrineMenuAndSelect`, `ShowContestedArmoryMenuAndSelect`
- **Utility (2):** `ShowConfirmMenu` (Yes/No)

#### App.axaml.cs Startup Flow

Replaced the P2 scaffold (`TODO(P3-P8)` block) with:
1. `StartupOrchestrator.Run()` gathers all user choices (menu → intro sequence → name/class/difficulty)
2. Pattern-match `StartupResult` to handle NewGame, LoadedGame, and ExitGame
3. ExitGame closes window via `Dispatcher.UIThread.InvokeAsync`
4. Data systems initialized after startup (not before), matching console `Program.cs` flow

#### Thread Safety

- All UI updates dispatched via `Dispatcher.UIThread.InvokeAsync()`
- `Interlocked.Exchange` used for `_pendingCommand` field (matching P5 pattern)
- `TaskCreationOptions.RunContinuationsAsynchronously` prevents deadlocks
- Game loop runs on background thread via `Task.Run()`, all input bridged through TCS

---

## Key Technical Decisions

1. **Text-based menus over arrow-key navigation** — Simplest reliable approach. No AXAML changes needed; works with existing ContentPanel + InputPanel. Future P9+ can add rich selection controls.
2. **Combat menu accepts letter shortcuts** — Players can type "A", "B", "F", "I" directly in addition to numbers 1–4.
3. **SelectClass renders stat cards inline** — Shows HP/ATK/DEF/Mana stats and prestige bonuses for each class before prompting.
4. **ShowSkillTreeMenu dual-mode** — Shows locked skills as info lines, available skills as numbered menu, "Press Enter" dismiss when no skills available.
5. **ShowShopWithSellAndSelect returns -1 for "Sell Items"** — Matches the console implementation contract.

---

## Validation

- ✅ `dotnet build Dungnz.Display.Avalonia/` — 0 errors (1 pre-existing warning)
- ✅ `dotnet test` — 2,351 passed, 0 failed, 4 skipped (pre-existing)
- ✅ Only `Dungnz.Display.Avalonia/` files modified
- ✅ Pre-commit hook passed (full solution build)

---

## Related PRs

- PR #1434: Avalonia P6–P8 — Menu Input Infrastructure + All 23 Stubs
