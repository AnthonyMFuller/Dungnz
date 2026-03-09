### 2026-03-09: Gear equip, panel refresh, and input escape fixes

**By:** Barton

**What:**

**Bug 1 — ShowEquipmentComparison overwrote itself immediately**
When the player equipped an item while Live was active, `ShowEquipmentComparison` cleared `_contentLines` then pushed a Spectre `Table` widget directly via `_ctx.UpdatePanel`. The very next `ShowMessage` call from `DoEquip` invoked `RefreshContentPanel()`, which rebuilt the Content panel from the now-empty `_contentLines`, instantly replacing the comparison table with a bare text panel. The comparison was shown for ~0 ms and never readable. Fixed by replacing the direct `_ctx.UpdatePanel` call with `SetContent(markupText, "⚔  ITEM COMPARISON", Color.Yellow)` using two new markup helpers (`AppendIntCompareLine`, `AppendPctCompareLine`). The comparison now lives in `_contentLines` and subsequent `ShowMessage` calls append below it.

**Bug 2 — Gear panel not updated when ShowRoom ran**
`ShowRoom` refreshed the Stats panel via `RenderStatsPanel(_cachedPlayer)` but never called `RenderGearPanel`. While `DoEquip` called `ShowPlayerStats` (which does call `RenderGearPanel`) just before `EquipCommandHandler` triggered `ShowRoom`, the pattern was fragile: any other `ShowRoom` path (e.g. moving rooms, shop, shrine) would leave the Gear panel potentially stale. Fixed by adding `RenderGearPanel(_cachedPlayer)` alongside `RenderStatsPanel(_cachedPlayer)` in `ShowRoom`, making all three persistent panels (Map, Stats, Gear) authoritative after every room render.

**Bug 3 — ContentPanelMenu Escape/Q trapped players**
Commit #1288 removed the "auto-select last item on Escape" behaviour from `ContentPanelMenu<T>` (non-nullable, used when Live is active). The intent was correct for pre-game menus (SelectDifficulty, SelectClass) where selecting the last option accidentally would be wrong. However, it also broke all in-game menus that carry an explicit `("← Cancel", 0)` last item — shop, sell, crafting, shrine, armory. Players pressing Escape in those menus were stuck in a loop. Fixed by adding a cancel-sentinel check: if the last item's label contains "Cancel" (case-insensitive) or starts with "←", Escape/Q returns that item's value. Pre-game menus are unaffected because they only reach `ContentPanelMenu` via `SelectionPromptValue` when Live is active — and they are always invoked pre-`StartAsync`.

**Why:**

- **Bug 1 root cause:** `ShowEquipmentComparison` used `_ctx.UpdatePanel` directly instead of routing through `SetContent`/`AppendContent`, breaking the `_contentLines` contract that the rest of the content-panel system relies on.
- **Bug 2 root cause:** `ShowRoom`'s panel refresh was incomplete — it was written before the Gear panel existed as a separate panel or before the convention of "ShowRoom refreshes all persistent panels" was established.
- **Bug 3 root cause:** The fix in #1288 was correct in its diagnosis (accidental last-item selection) but too broad — it disabled all Escape/Q cancel behaviour without distinguishing menus that carry an explicit cancel sentinel from those that don't.
