### 2026-02-27: Bug hunt findings
**By:** Romanoff
**What:** Found 8 bugs across display/UI alignment, save/load state management, menu navigation, and dead code
**Why:** Requested deep bug hunt session

Issue numbers: #617, #618, #619, #620, #621, #622, #623, #624

---

## Bug Summary

### Logic / State Bugs

**#617 — HandleLoad does not reset RunStats**
`Engine/GameLoop.cs` ~line 665. When `LOAD` is called, `_stats` is not reset to a fresh `RunStats()`. Statistics from the pre-load session (enemies defeated, gold collected, turns, damage) carry over into the loaded run, causing end-of-run summaries to show inflated numbers. Fix: add `_stats = new RunStats();` in `HandleLoad` after state is assigned.

### Display / Box Alignment Bugs

**#618 — ShowEnemyDetail HP line overflows box by 9 chars**
`Display/DisplayService.cs` ~line 1354. Formula `W - 14 - hp_len - maxhp_len` does not account for the 10-char HP bar or the space preceding the HP values. Fixed chars = 23 (not 14), so padding formula should use `W - 23`. The HP line extends 9 columns past the right border.

**#619 — ShowEnemyDetail name line is 2 chars short**
`Display/DisplayService.cs` ~line 1352. Formula `W - 4 - name_len` gives inner content of W-2=34 but box inner width is W=36. Should use `W - 2 - name_len`. Right border appears 2 columns before the top/bottom corners.

**#620 — ShowCombatStart banner overflows border by 2 with double-width emoji**
`Display/DisplayService.cs` ~line 1285. Banner padding uses `44 - banner.Length` (21 C# chars) but the sword emoji (U+2694) renders as 2 visual columns in modern terminals, making the banner 2 cols wider than the 44-char border.

**#621 — ShowLootDrop weapon name line overflows box by 1**
`Display/DisplayService.cs` ~line 304. Padding formula `35 - icon.Length` uses `icon.Length=1` for the sword icon (U+2694, BMP) but it renders as 2 visual columns. Result: weapon loot card name line is 1 col wider than the box. Other icons (shield, flask, ring) are surrogate pairs (.Length=2) matching their visual width; only the sword icon is affected.

**#624 — ShowLevelUpChoice box lines misaligned (dead method)**
`Display/DisplayService.cs` ~lines 1309-1313. LEVEL UP! banner line overflows by 1 (`W-12` padding vs 13-char visible prefix). Option lines are 1 char short (`W-26` should be `W-25`). The method is dead code (CombatEngine calls `ShowLevelUpChoiceAndSelect` instead) but remains on `IDisplayService`.

### Menu / Navigation Bugs

**#622 — SelectFromMenu Escape/X silently selects last option in non-cancel menus**
`Display/DisplayService.cs` ~lines 664-668. Pressing Escape or X always returns `options[options.Count - 1].Value`. For menus with an explicit Cancel last option this is correct, but in difficulty selection (last = Hard) and class selection (last = Ranger) it silently forces an unintended selection.

### Architecture / Testability Bug

**#623 — IMenuNavigator injected but never used in ConsoleDisplayService**
`Display/DisplayService.cs` lines 14-15. `_navigator` is declared and injected but `SelectFromMenu` has its own inline key-handling loop that ignores the injected navigator entirely. TODO comment (`// TODO: wire up navigator call-sites (issue #586)`) has been unfixed. Tests injecting a custom navigator to control menus will have no effect.

---

## Categories
- **State management:** 1 bug (#617)
- **Box border alignment:** 4 bugs (#618, #619, #620, #621, #624)
- **Menu/navigation:** 2 bugs (#622, #623)
