# Display Layer Bug Audit — Romanoff
**Date:** 2026-06-10  
**Auditor:** Romanoff (Tester)  
**Scope:** Full display layer audit — `SpectreLayoutDisplayService`, `SpectreLayoutDisplayService.Input`, `SpectreLayoutContext`, `SpectreLayout`, `GameLoop`, all command handlers, `CombatEngine` display paths.

---

## Executive Summary

**18 bugs found.** 1 P0 (game-breaking), 8 P1 (major UX breaks), 6 P2 (notable defects), 3 P3 (minor/cosmetic). The P0 makes every in-game menu throw `InvalidOperationException`. Several P1s cause the content panel to become permanently stale after normal gameplay actions (take, combat, equip). The log panel is spammed with duplicate "Entered room" entries on every hazard tick.

---

## Bug List

### BUG-1: All in-game menus throw `InvalidOperationException` (P0)
**Root cause:** `AnsiConsole.Live(_layout).Start(ctx => {...})` acquires Spectre's `DefaultExclusivityMode` (`_running = 1`) for the entire callback duration — including while the live loop is blocked on `_resumeLiveEvent.Wait()`. When the game thread calls `PauseAndRun` and then `AnsiConsole.Prompt(SelectionPrompt)`, Prompt tries to acquire the same exclusivity lock (`Interlocked.CompareExchange(_running, 1, 0)` fails because it is already 1) and throws `InvalidOperationException: Trying to run one or more interactive functions concurrently`.  
**Files:** `SpectreLayoutDisplayService.Input.cs:490–538` (`PauseAndRun<T>`, `SelectionPromptValue`, `NullableSelectionPrompt`)  
**Trigger:** Any in-game interactive menu after `StartAsync()` has been called: `take` (ShowTakeMenuAndSelect), `inventory` no-arg (ShowInventoryAndSelect), all combat menus (ShowCombatMenuAndSelect, ShowAbilityMenuAndSelect, ShowCombatItemMenuAndSelect), `equip` no-arg, `use` no-arg, `shop`, `sell`, `craft`, shrine/trap/armory menus, level-up selection (ShowLevelUpChoiceAndSelect), skill tree (ShowSkillTreeMenu), confirmation prompts (ShowConfirmMenu). Every single combat turn calls ShowCombatMenuAndSelect → immediate crash.  
**Affected methods (all 16 in-game interactive menus):**
- `ShowTakeMenuAndSelect`, `ShowInventoryAndSelect`, `ShowEquipMenuAndSelect`, `ShowUseMenuAndSelect`
- `ShowCombatMenuAndSelect`, `ShowAbilityMenuAndSelect`, `ShowCombatItemMenuAndSelect`
- `ShowShrineMenuAndSelect`, `ShowForgottenShrineMenuAndSelect`, `ShowContestedArmoryMenuAndSelect`, `ShowTrapChoiceAndSelect`
- `ShowShopWithSellAndSelect`, `ShowSellMenuAndSelect`, `ShowConfirmMenu`, `ShowCraftMenuAndSelect`
- `ShowLevelUpChoiceAndSelect`, `ShowSkillTreeMenu`  
**Fix approach:** Replace all `AnsiConsole.Prompt(SelectionPrompt)` calls with a custom raw-key selection prompt that uses `AnsiConsole.Console.Input.ReadKey(intercept: true)` (same approach as `ReadCommandInput`). Implement a `RawSelectionMenu<T>` helper that renders choices to the content panel via `_ctx.UpdatePanel` and reads arrow keys + Enter without acquiring the exclusivity lock. This is the only safe approach while Live is active.

---

### BUG-2: PauseAndRun has no acknowledgement signal — race condition under load (P1)
**Root cause:** `PauseAndRun` sets `_pauseLiveEvent.Set()` then does `Thread.Sleep(100)`, assuming the Live loop will have entered `_resumeLiveEvent.Wait()` within 100ms. There is no `_livePausedAckEvent` to confirm the live thread is actually blocked. If the system is under load and the live thread is mid-`ctx.Refresh()` or mid-`Thread.Sleep(50)`, the 100ms budget could elapse before the live loop enters `Wait()`. The SelectionPrompt then runs while Live is still running → P0 exception (or silent concurrent output corruption if not using Spectre's exclusivity check).  
**Files:** `SpectreLayoutDisplayService.cs:83–100` (live loop), `SpectreLayoutDisplayService.Input.cs:490–506` (`PauseAndRun<T>`)  
**Trigger:** Any menu call on a slow or heavily loaded system.  
**Fix approach:** Add a `ManualResetEventSlim _livePausedAckEvent` that the live loop sets after entering `_resumeLiveEvent.Wait()` (i.e., the moment it's truly blocked). `PauseAndRun` waits on `_livePausedAckEvent` with a timeout instead of using `Thread.Sleep(100)`. Note: moot if BUG-1 is fixed with raw key reading.

---

### BUG-3: `_resumeLiveEvent` race between sequential PauseAndRun calls (P1)
**Root cause:** `_resumeLiveEvent` is a shared `ManualResetEventSlim(false)`. After one `PauseAndRun` completes (sets `_resumeLiveEvent`), the live thread may not have had time to `Reset()` it before the next `PauseAndRun` call sets `_pauseLiveEvent` again. When the live loop wakes, it resets `_resumeLiveEvent` and calls `ctx.Refresh()` — but then the loop body checks `_pauseLiveEvent` which is now set again for the second pause. However, `_resumeLiveEvent` was already reset in the prior cycle. This sequencing depends on exact thread scheduling and can cause the live loop to momentarily skip the second pause (proceeds without blocking) or double-reset the event.  
**Files:** `SpectreLayoutDisplayService.cs:85–91`, `SpectreLayoutDisplayService.Input.cs:494–505`  
**Trigger:** Any two sequential interactive menus in the same command handler (e.g., `SellCommandHandler`: ShowSellMenuAndSelect → ShowConfirmMenu; `ShopCommandHandler`: multiple back-to-back prompts in the while loop).  
**Fix approach:** Use `AutoResetEventSlim` or `SemaphoreSlim(0,1)` instead of `ManualResetEventSlim` for `_resumeLiveEvent`. Moot if BUG-1 is fixed.

---

### BUG-4: After `take`, content panel stuck on "📦 Pickup"; room view not restored (P1)
**Root cause:** `TakeCommandHandler` calls `ShowItemPickup()` which calls `SetContent("📦 Pickup", ...)`, replacing the content panel with the pickup view. Neither `ShowRoom()` nor `RefreshDisplay()` is called after the take operation completes. The content panel remains on "📦 Pickup" (or the last item's pickup message in "take all") until the player manually types `look` or moves rooms.  
**Files:** `TakeCommandHandler.cs:67–117`, `SpectreLayoutDisplayService.cs:726–741` (`ShowItemPickup`)  
**Trigger:** Player types `take` (with or without argument) and successfully picks up one or more items. Content panel stuck.  
**Fix approach:** Call `context.Display.ShowRoom(context.CurrentRoom)` (or `RefreshDisplay`) at the end of `TakeSingleItem` and after the loop in `TakeAllItems`.

---

### BUG-5: After combat, map panel still shows enemy `[!]`; content panel is stale (P1)
**Root cause:** After `CombatResult.Won`, `GoCommandHandler` sets `context.CurrentRoom.Enemy = null` but never calls `RenderMapPanel`, `ShowRoom`, or `RefreshDisplay`. The map panel continues to show `[!]` for the cleared room because `GetMapRoomSymbol` checks `r.Enemy?.HP > 0` — with `Enemy = null`, the room would correctly render `[+]`, but the panel is never re-rendered. Content panel stays in the post-combat loot/message state.  
**Files:** `GoCommandHandler.cs:145–167`, `SpectreLayoutDisplayService.cs:579` (`ShowRoom` → `RenderMapPanel`)  
**Trigger:** Player enters a room with an enemy and wins combat. Map still shows `[!]`, content shows combat/loot messages.  
**Fix approach:** Call `context.Display.ShowRoom(context.CurrentRoom)` (or `RefreshDisplay`) after the `CombatResult.Won` branch in `GoCommandHandler` (after the post-combat ShowMessage calls).

---

### BUG-6: `ShowRoom` always appends "Entered [room]" to log — spams log in hazard rooms (P1)
**Root cause:** `ShowRoom` unconditionally calls `AppendLog($"Entered {GetRoomDisplayName(room)}")` every time it's invoked. `RefreshDisplay` calls `ShowRoom`. `ApplyRoomHazard` (in `GameLoop.RunLoop`) calls `RefreshDisplay` on every turn a player takes an action in a LavaSeam, CorruptedGround, or BlessedClearing room. This causes the log to fill with "Entered [room]" entries every single turn — 50 entries in 50 turns — hiding actual gameplay events.  
**Files:** `SpectreLayoutDisplayService.cs:578–579` (`ShowRoom`), `GameLoop.cs:313–328` (`ApplyRoomHazard`)  
**Trigger:** Player stays in any room with `EnvironmentalHazard` and takes actions. Log becomes "Entered Room, Entered Room, Entered Room…"  
**Fix approach:** Only log room entry from `ShowRoom` when `room.Visited == false` at the time of the call (i.e., first visit), or pass a `bool logEntry = true` parameter and pass `false` from `RefreshDisplay`.

---

### BUG-7: Hazard damage message in content panel is instantly erased by `RefreshDisplay` (P1)
**Root cause:** In `ApplyRoomHazard`, the sequence is: `ShowMessage("🔥 lava sears you")` → `AppendContent(...)` → immediately `RefreshDisplay(...)` → `ShowRoom()` → `SetContent(room description)` — which clears `_contentLines` and replaces with the room description. Since these are synchronous same-thread calls with no visual delay, the hazard damage message appears in the content panel for approximately zero time before being overwritten. The player can only see it in the log panel.  
**Files:** `GameLoop.cs:310–328` (`ApplyRoomHazard`), `SpectreLayoutDisplayService.cs:146–156` (`SetContent` clearing `_contentLines`)  
**Trigger:** Player takes any action in a LavaSeam or CorruptedGround room.  
**Fix approach:** Don't call `RefreshDisplay` from `ApplyRoomHazard`. Instead call `ShowPlayerStats(player)` (to update the stats panel with new HP) without calling `ShowRoom`. The hazard message will remain visible in the content panel until the next natural `ShowRoom` call.

---

### BUG-8: `ShowCombatStart` does not clear content — old room content bleeds into combat view (P1)
**Root cause:** `ShowCombatStart` sets `_contentHeader` and `_contentBorderColor` then calls `AppendContent` three times. It does NOT call `_contentLines.Clear()` or `SetContent`. When called, the content panel contains the room description from the preceding `ShowRoom` call. The combat start banner is appended below the room text. Compare `ShowCombat` which does call `_contentLines.Clear()`.  
**Files:** `SpectreLayoutDisplayService.cs:1002–1010` (`ShowCombatStart`), `SpectreLayoutDisplayService.cs:588–594` (`ShowCombat` — correct reference)  
**Trigger:** Player enters any room with an enemy. Content panel shows room description + combat header at the bottom, not a clean combat view.  
**Fix approach:** Add `_contentLines.Clear();` at the start of `ShowCombatStart`, before the `AppendContent` calls.

---

### BUG-9: After equip/unequip, content panel left on comparison/message view; not restored to room (P1)
**Root cause:** `EquipmentManager.DoEquip` calls `ShowEquipmentComparison` (writes to content panel) then `ShowPlayerStats` (updates stats + gear panels). No `ShowRoom` or `RefreshDisplay` is called. Content panel stays on the equipment comparison table (or the equip confirmation messages from `ShowMessage`) until the player types `look`.  
**Files:** `Systems/EquipmentManager.cs:130–141` (`DoEquip`), `SpectreLayoutDisplayService.Input.cs:22–53` (`ShowEquipmentComparison`)  
**Trigger:** Player equips or unequips any item. Content panel stuck on comparison/confirmation.  
**Fix approach:** `EquipmentManager` doesn't have access to the current room, so it cannot call `ShowRoom`. The fix is either to pass the current room to `HandleEquip`/`HandleUnequip`, or to have `EquipCommandHandler` call `context.Display.ShowRoom(context.CurrentRoom)` after `Equipment.HandleEquip` returns.

---

### BUG-10: `ShowEquipmentComparison` bypasses `_contentLines` — subsequent `AppendContent` restores old content (P2)
**Root cause:** `ShowEquipmentComparison` calls `_ctx.UpdatePanel(SpectreLayout.Panels.Content, panel)` directly, bypassing `SetContent()`. The internal `_contentLines`, `_contentHeader`, and `_contentBorderColor` are NOT updated to reflect the comparison table. If any subsequent method calls `AppendContent()` or `RefreshContentPanel()` (which rebuilds from `_contentLines`), the comparison table is silently overwritten with the stale pre-comparison content.  
**Files:** `SpectreLayoutDisplayService.Input.cs:50–53` (`ShowEquipmentComparison`)  
**Trigger:** Equipment comparison is shown, then anything that calls `AppendContent` (e.g., `ShowMessage`, `ShowCombatMessage`) — the comparison is replaced with old room content.  
**Fix approach:** Replace the direct `_ctx.UpdatePanel` call with a `SetContent` call that serializes the comparison table to markup, OR update `_contentLines`, `_contentHeader`, `_contentBorderColor` before calling `_ctx.UpdatePanel` so the internal buffer matches the panel state.

---

### BUG-11: `RefreshDisplay` causes double `RenderStatsPanel` and double `RenderMapPanel` per call (P2)
**Root cause:** `RefreshDisplay` calls `ShowPlayerStats` (which calls `RenderStatsPanel` + `RenderGearPanel`), then `ShowRoom` (which at the end calls `if (_cachedPlayer != null) RenderStatsPanel(_cachedPlayer)` — double stats render), then `ShowMap` (which calls `RenderMapPanel`) — but `ShowRoom` already called `RenderMapPanel` (double map render). Each `RefreshDisplay` call renders stats panel twice and map panel twice.  
**Files:** `SpectreLayoutDisplayService.cs:1106–1111` (`RefreshDisplay`), `SpectreLayoutDisplayService.cs:493–585` (`ShowRoom`), `SpectreLayoutDisplayService.cs:648–653` (`ShowPlayerStats`)  
**Trigger:** Every `RefreshDisplay` call (run start, every hazard tick, shrine/armory/library interactions). Causes extra flicker and CPU waste.  
**Fix approach:** Remove the `if (_cachedPlayer != null) RenderStatsPanel(_cachedPlayer)` call from `ShowRoom`. Stats rendering should only happen in `ShowPlayerStats`. Also remove `ShowMap(room, floor)` from `RefreshDisplay` since `ShowRoom` already calls `RenderMapPanel`.

---

### BUG-12: Map shows special room icons after clearing — `GetMapRoomSymbol` ignores `SpecialRoomUsed` (P2)
**Root cause:** `GetMapRoomSymbol` returns `[A]`, `[L]`, `[F]` for `ContestedArmory`, `PetrifiedLibrary`, `ForgottenShrine` unconditionally, without checking `r.SpecialRoomUsed`. After the player uses these rooms, the map still shows the special icon instead of `[+]` (cleared). `TrapRoom` correctly checks `!r.SpecialRoomUsed`. The legend builder (`BuildMapMarkup`) also doesn't correctly exclude cleared special rooms from their respective legend entries.  
**Files:** `SpectreLayoutDisplayService.cs:391–398` (`GetMapRoomSymbol`), also `SpectreLayoutDisplayService.cs:346–352` (legend logic)  
**Trigger:** Player visits and uses a Contested Armory, Petrified Library, or Forgotten Shrine. Map continues to show `[A]`/`[L]`/`[F]` instead of `[+]`.  
**Fix approach:** Add `SpecialRoomUsed` checks to `GetMapRoomSymbol`:
```csharp
if (r.Type == RoomType.ContestedArmory  && !r.SpecialRoomUsed) return "[yellow][[A]][/]";
if (r.Type == RoomType.PetrifiedLibrary && !r.SpecialRoomUsed) return "[blue][[L]][/]";
if (r.Type == RoomType.ForgottenShrine  && !r.SpecialRoomUsed) return "[cyan][[F]][/]";
```

---

### BUG-13: `ShowCombatStatus` wipes accumulated combat messages every round (P2)
**Root cause:** `ShowCombatStatus` calls `SetContent(...)` which clears `_contentLines` and rebuilds with the HP bars. Any combat messages appended via `ShowCombatMessage` (which calls `AppendContent`) since the last `ShowCombatStatus` call are erased. Only the HP-bar snapshot is shown; the combat narrative from `ShowRecentTurns` (appended as `ShowMessage` calls before `ShowCombatStatus`) is immediately wiped when the next `ShowCombatStatus` is called.  
**Files:** `SpectreLayoutDisplayService.cs:597–637` (`ShowCombatStatus`), `SpectreLayoutDisplayService.cs:639–645` (`ShowCombatMessage`)  
**Trigger:** Any multi-turn combat. Round 1 messages appear, then are erased at the start of round 2 when `ShowCombatStatus` is called.  
**Fix approach:** `ShowCombatStatus` should append to content (like `ShowCombatMessage` does) rather than replacing it, keeping the `_contentHeader = "⚔  Combat"` and border color. Only call `SetContent` on the FIRST call to `ShowCombatStatus` (when header/color need setting) and subsequently use `AppendContent`.

---

### BUG-14: `ShowFloorBanner` updates `_currentFloor` but does not refresh map panel header (P2)
**Root cause:** `ShowFloorBanner` sets `_currentFloor = floor` and updates the content panel with the floor banner. It does NOT call `RenderMapPanel`. The map panel header is `$"[bold green]Floor {_currentFloor}[/]"` and is only updated when `RenderMapPanel` (called from `ShowRoom`, `ShowMap`, or `RefreshDisplay`) is next invoked. After `ShowFloorBanner`, if `ShowRoom` hasn't been called yet, the map panel header still shows the previous floor number.  
**Files:** `SpectreLayoutDisplayService.cs:1035–1046` (`ShowFloorBanner`)  
**Trigger:** Player descends to a new floor. Map panel shows old floor number until next room entry.  
**Fix approach:** Add `RenderMapPanel(_cachedRoom ?? ...)` at the end of `ShowFloorBanner`, or call `ShowMap(_cachedRoom, floor)` if `_cachedRoom` is not null.

---

### BUG-15: `TakeAllItems` — multiple `ShowItemPickup` calls each replace content panel (P2)
**Root cause:** In `TakeAllItems`, `ShowItemPickup(item, ...)` is called for each item picked up. `ShowItemPickup` calls `SetContent(...)` which REPLACES (clears + rebuilds) the content panel each time. For a room with 3 items, the content panel flickers through 3 different "📦 Pickup" views, each replacing the previous. Only the last item's pickup view is retained.  
**Files:** `TakeCommandHandler.cs:87–117` (`TakeAllItems`), `SpectreLayoutDisplayService.cs:726–741` (`ShowItemPickup`)  
**Trigger:** Player selects "Take All" when multiple items are on the floor.  
**Fix approach:** Either (a) use `AppendContent` for 2nd+ items in `TakeAllItems`, or (b) build a summary string for all taken items and call `SetContent` once at the end.

---

### BUG-16: `GetRoomDisplayName` returns "Room" for most room types — content panel header loses context (P3)
**Root cause:** `GetRoomDisplayName` only has cases for 8 named room types; the `_` default returns `"Room"`. Standard room types (`RoomType.Standard`, `RoomType.Corridor`, `RoomType.TrapRoom`, etc.) all show "Room" as the content panel header. Trap rooms show "Room" instead of "Trap Room".  
**Files:** `SpectreLayoutDisplayService.Input.cs:613–624` (`GetRoomDisplayName`)  
**Trigger:** Player enters any non-special room. Content panel header shows "Room" instead of a descriptive name.  
**Fix approach:** Add missing cases: `RoomType.TrapRoom => "Trap Room"`, `RoomType.Standard => "Chamber"`, `RoomType.Corridor => "Corridor"`, etc.

---

### BUG-17: `ShowIntroNarrative` always returns `false` regardless of display state (P3)
**Root cause:** `ShowIntroNarrative` always returns `false`. The contract (per XML doc) says the return value "is reserved for a future skip path." If `StartupOrchestrator` or callers check this return value to determine whether to wait for the player to press Enter (or skip the intro), they will always proceed without waiting — the intro text shown in the content panel is never confirmed as "seen" by the player.  
**Files:** `SpectreLayoutDisplayService.cs:913–928` (`ShowIntroNarrative`)  
**Trigger:** Every game start. Intro narrative may be skipped/missed.  
**Fix approach:** When `_ctx.IsLiveActive`, after calling `SetContent`, prompt the player to press Enter via the existing `ReadCommandInput()` mechanism and return `true`. When not live, use `Console.ReadLine()` and return `true`.

---

### BUG-18: `ShowRoom` is called with stale `_currentFloor` from `RefreshDisplay` — map briefly shows wrong floor (P3)
**Root cause:** `RefreshDisplay(player, room, floor)` calls `ShowPlayerStats`, then `ShowRoom(room)`, then `ShowMap(room, floor)`. Inside `ShowRoom`, `RenderMapPanel(room)` is called — this calls `UpdateMapPanel(BuildMapMarkup(room))` which uses `_currentFloor` in the header `$"[bold green]Floor {_currentFloor}[/]"`. At the time `ShowRoom` runs, `_currentFloor` has NOT yet been updated (that happens in `ShowMap`). On floor descents, the map panel header will briefly show the previous floor number during `ShowRoom`'s render, then be corrected by `ShowMap`.  
**Files:** `SpectreLayoutDisplayService.cs:127–134` (`UpdateMapPanel`), `SpectreLayoutDisplayService.cs:1106–1111` (`RefreshDisplay`), `SpectreLayoutDisplayService.cs:819–824` (`ShowMap`)  
**Trigger:** Player descends a floor; `RefreshDisplay` is called with the new floor number.  
**Fix approach:** In `RefreshDisplay`, set `_currentFloor = floor` before calling `ShowRoom` (or call `ShowMap` before `ShowRoom`).

---

## Summary Table

| # | Title | Severity | File(s) |
|---|-------|----------|---------|
| 1 | All in-game menus throw `InvalidOperationException` | **P0** | `Input.cs:490–538` |
| 2 | PauseAndRun: no ack signal — race under load | P1 | `Input.cs:490–506`, main `.cs:83–100` |
| 3 | `_resumeLiveEvent` race between sequential pause cycles | P1 | main `.cs:85–91`, `Input.cs:494–505` |
| 4 | After `take`, content panel stuck on Pickup view | P1 | `TakeCommandHandler.cs:67–117` |
| 5 | After combat, map shows stale `[!]`; content stale | P1 | `GoCommandHandler.cs:145–167` |
| 6 | `ShowRoom` spams log "Entered room" every hazard tick | P1 | main `.cs:579`, `GameLoop.cs:313–328` |
| 7 | Hazard damage message instantly erased by RefreshDisplay | P1 | `GameLoop.cs:310–328` |
| 8 | `ShowCombatStart` appends to old content — room text bleeds | P1 | main `.cs:1002–1010` |
| 9 | After equip/unequip, content panel not restored | P1 | `EquipmentManager.cs:130–141` |
| 10 | `ShowEquipmentComparison` bypasses `_contentLines` | P2 | `Input.cs:50–53` |
| 11 | `RefreshDisplay` double-renders stats and map panels | P2 | main `.cs:1106–1111` |
| 12 | Map ignores `SpecialRoomUsed` for Armory/Library/Shrine | P2 | main `.cs:391–398` |
| 13 | `ShowCombatStatus` wipes accumulated combat messages | P2 | main `.cs:597–637` |
| 14 | `ShowFloorBanner` doesn't update map panel header | P2 | main `.cs:1035–1046` |
| 15 | `TakeAllItems`: each item flickers through a new content view | P2 | `TakeCommandHandler.cs:87–117` |
| 16 | `GetRoomDisplayName` returns "Room" for most types | P3 | `Input.cs:613–624` |
| 17 | `ShowIntroNarrative` always returns `false` | P3 | main `.cs:913–928` |
| 18 | `RefreshDisplay` renders map with stale floor number | P3 | main `.cs:1106–1111` |

---

*Generated by Romanoff — Tester*
