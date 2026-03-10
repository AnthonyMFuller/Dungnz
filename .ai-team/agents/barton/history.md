# Barton — History — Recent Activity (Full archive: history-archive-2026-03-10.md)

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

### 2026-03-06 — Fixed P0 Crash: Removed Broken PauseAndRun Method (#1265)

**Context:** Game was crashing with `InvalidOperationException: Trying to run one or more interactive functions concurrently` when using Attack or any typed input during Live display.

**Root Cause:**
`PauseAndRun` attempted to pause Live rendering to call `AnsiConsole.Prompt()`. This approach was fundamentally broken because:
- Spectre.Console's `DefaultExclusivityMode` holds an atomic `_running = 1` counter for the **entire duration** of `Live.Start()` callback
- Blocking the render thread with `_resumeLiveEvent.Wait()` does NOT release the exclusivity lock
- Any `AnsiConsole.Prompt()` called while Live is active finds `_running == 1` and throws `InvalidOperationException`

**Solution:**
- Removed `PauseAndRun` method entirely
- Fixed `ShowSkillTreeMenu` to call `AnsiConsole.Prompt` directly when `!IsLiveActive` (no wrapper needed)
- Removed all pause/resume infrastructure: `_pauseLiveEvent`, `_liveIsPausedEvent`, `_resumeLiveEvent`, `_pauseDepth`
- Simplified Live render loop — just sleeps 50ms waiting for exit signal
- Updated documentation to clarify the input pattern: ReadKey-based when Live is active, Prompt when not active

**Files Modified:**
- `Dungnz.Display/Spectre/SpectreLayoutDisplayService.Input.cs` — removed PauseAndRun, fixed ShowSkillTreeMenu
- `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs` — removed pause event fields and loop logic

**Key Learning:**
Never call `AnsiConsole.Prompt()` while `Live.Start()` callback is running. The exclusivity lock is held for the entire callback duration regardless of blocking. Always use `ReadKey`-based input (like `ContentPanelMenu`) when Live is active, and guard with `IsLiveActive` checks if you need to fall back to `Prompt`.

**PR:** https://github.com/AnthonyMFuller/Dungnz/pull/1266
**Build:** ✅ Success (0 errors)
**Closes:** #1265

### 2026-03-08 — Added Cooldown Visibility to Combat HUD (#1268)

**Context:** Ability cooldowns were tracked and enforced correctly, but completely invisible during normal combat. Players couldn't see which abilities were on cooldown or when they'd come back, leading to attack spam rather than tactical ability usage.

**Solution:**
- Added `UpdateCooldownDisplay(IReadOnlyList<(string name, int turnsRemaining)> cooldowns)` as a **default interface method** on `IDisplayService` (no-op default) — zero impact on test stubs
- `SpectreLayoutDisplayService` overrides it: caches the list, re-renders the Stats panel to show a `CD:` line under the MP bar
- Format: `CD: ShieldBash:2t  BattleCry:✅  Fortify:✅` — only abilities with a cooldown mechanic (CooldownTurns > 0) are shown; `✅` = ready, `Nt` = N turns remaining
- Cleared when `ShowRoom()` is called (player leaves combat, section disappears)
- `CombatEngine` calls this after `TickCooldowns()` each turn
- Also added **toast notifications** via `ShowCombatMessage` when an ability transitions from on-cooldown → ready: `✅ Shield Bash is ready!`

**Architecture note:** Used a default interface method rather than adding to all 5 `IDisplayService` implementations. .NET 10 fully supports this pattern.

**Files Modified:**
- `Dungnz.Models/IDisplayService.cs` — added `UpdateCooldownDisplay()` default method
- `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs` — `_cachedCooldowns` field, `UpdateCooldownDisplay()` override, cooldown line in `RenderStatsPanel`, clear in `ShowRoom`
- `Dungnz.Engine/CombatEngine.cs` — pre-tick capture, toasts, `UpdateCooldownDisplay()` call

**PR:** https://github.com/AnthonyMFuller/Dungnz/pull/1276
**Build:** ✅ Success (0 errors, 0 warnings)
**Closes:** #1268

## Learnings

- **Default interface methods are the right tool** when adding display-only hooks to `IDisplayService` — avoids touching all 5 implementations (FakeDisplayService, TestDisplayService, ConsoleDisplayService, SpectreDisplayService, SpectreLayoutDisplayService)
- **`_cachedCooldowns = []`** (C# 12 collection expression) works cleanly for empty list initialization of `IReadOnlyList<T>` fields in .NET 10
- **Stats panel vs Content panel split:** `ShowCombatStatus` only updates the Content panel (the narrative); `RenderStatsPanel` owns the top-right Stats panel. HUD additions belong in `RenderStatsPanel`, not `ShowCombatStatus`
- **Pre-tick snapshot pattern for toast detection:** capture `GetCooldown() > 0` state before `TickCooldowns()`, compare after — any that went to 0 fire a toast

### 2026-03-10 — WI-C + WI-D — CombatEngine momentum increment + threshold effects (#1274)

**Context:** Issue #1274, part of the momentum resource system for per-class resource mechanics.
Hill had already pushed `MomentumResource` model + `Player.Momentum` on `squad/1274-momentum-model-display`. Romanoff had already written skipped integration tests on `squad/1274-momentum-tests` that also expect a `Consume()` method.

**Approach:**
1. Created `squad/1274-momentum-engine` from master
2. Cherry-picked Hill's model commit (084242e) — `MomentumResource.cs` + `Player.Momentum { get; set; }` + `Momentum?.Reset()` in `ResetCombatPassives()`
3. Added `MomentumResource.Consume()` — returns `bool`, resets on true — required by Romanoff's unit tests
4. Added WI-C (increment) and WI-D (threshold) hooks

**WI-C hooks added:**
- **Warrior Fury:** `Add(1)` in `AttackResolver.PerformPlayerAttack` after damage is applied; `Add(1)` in `CombatEngine.PerformEnemyTurn` at `player.TakeDamage()` call with `enemyDmgFinal > 0`
- **Mage Arcane Charge:** `Add(1)` at the bottom of `AbilityManager.UseAbility` before `return Success` (fires for all ability types, all classes — but guarded by `player.Class == Mage`)
- **Paladin Devotion:** `Add(1)` in `PerformEnemyTurn` when DivineShield absorbs a blow; `Add(1)` in `AbilityManager` case `LayOnHands` after heal; `Add(1)` in `AbilityManager` case `DivineShield` after cast
- **Ranger Focus:** `Add(1)` via new `AddRangerFocusIfNoDamage(player, hpBefore)` helper at all 5 main-loop `PerformEnemyTurn` call sites; `Reset()` in `PerformEnemyTurn` when `player.TakeDamage(enemyDmgFinal)` is called with actual damage

**WI-D hooks added (all use `Consume()` pattern — atomic check + reset):**
- **Warrior Fury (×5):** In `AttackResolver` after crit check — `if (Consume()) playerDmg *= 2;` with Fury message
- **Mage Arcane Charge (×3):** In `AbilityManager.UseAbility` before mana spend — `if (Consume()) effectiveCost = 0;`. After switch — HP-before/after delta × 0.25 bonus damage applied
- **Paladin Devotion (×4):** In `AbilityManager` case `HolyStrike` — `if (Consume()) Apply(Stun, 1)`; guarded by `!IsImmuneToEffects`
- **Ranger Focus (×3):** In `AttackResolver` before damage calc — `if (Consume()) effectiveDef = 0;`

**Architecture decisions:**
- `InitPlayerMomentum(Player)` is a private static CombatEngine helper — creates new MomentumResource per class at each combat start (Rogue/Necromancer/others get `null`). Called right after `ResetCombatPassives()` at combat start.
- `AddRangerFocusIfNoDamage(player, hpBefore)` private helper avoids repeating HP-tracking logic at 5 separate call sites
- HP-before/after tracking approach is cleaner than modifying PerformEnemyTurn return type. HP compare is `player.HP == hpBefore` — works for all 0-damage paths (dodge, block, DivineShield absorb, ManaShield full absorb, stun skip)
- Mage 1.25× damage: captured `enemyHpBeforeAbility` before the switch block; applied `(delta × 0.25)` extra damage after switch. Handles ALL ability types that deal damage without touching individual cases.
- Paladin WI-C uses "DivineShield cast" AND "DivineShield absorb" AND "LayOnHands heal" as triggers. "Holy Smite heal component" interpreted as LayOnHands (the dedicated Paladin heal ability).
- Paladin WI-D: "next Smite" interpreted as `HolyStrike` (the Paladin offensive strike ability).

**Files changed:**
- `Dungnz.Models/MomentumResource.cs` — added `Consume()` method
- `Dungnz.Engine/AttackResolver.cs` — Warrior WI-C Add, Warrior WI-D Fury 2×, Ranger WI-D DEF=0
- `Dungnz.Engine/CombatEngine.cs` — `InitPlayerMomentum()`, `AddRangerFocusIfNoDamage()`, `ResetFleeState` Reset, combat-start Init, PerformEnemyTurn Warrior/Ranger hooks + Paladin DivineShield Add, 5 call-site Ranger Focus checks
- `Dungnz.Systems/AbilityManager.cs` — Mage WI-D (0 cost + 1.25×), Mage WI-C Add, Paladin WI-C (LayOnHands + DivineShield), Paladin WI-D (HolyStrike Stun)

**PR:** https://github.com/AnthonyMFuller/Dungnz/pull/1295
**Branch:** `squad/1274-momentum-engine`
**Build:** ✅ 0 errors, 0 warnings

## Learnings

- **`Consume()` > `IsCharged + Reset()`:** Romanoff's tests expect a `Consume()` method on MomentumResource. It returns bool and atomically checks+resets. Always prefer `Consume()` for WI-D threshold effects — cleaner than two-step check.
- **HP-before/after for zero-damage tracking:** When you need to detect "did the player take HP damage this enemy turn" across many possible return paths in `PerformEnemyTurn`, tracking `hpBefore` at the call site and comparing after is the least-invasive approach. Avoids changing PerformEnemyTurn's return type or adding fields.
- **Cherry-pick team branch work:** When another agent's branch isn't merged to master yet, `git cherry-pick <commit-sha>` is the clean way to include their work as a foundation.
- **`Mage 1.25× damage via delta pattern`:** Capture `enemyHpBeforeAbility` before the switch block, compute `delta = enemyHpBefore - enemy.HP` after, apply bonus as `enemy.HP -= (int)(delta * 0.25f)`. This handles all damage-dealing ability cases without touching each case individually.
- **Paladin "Holy Smite" = HolyStrike:** The spec said "Holy Smite heal component fires" for WI-C, and "next Smite cast" for WI-D. In the codebase, `AbilityType.HolyStrike` is the Paladin offensive strike, and `AbilityType.LayOnHands` is the dedicated heal. Mapping: DivineShield absorb + LayOnHands = WI-C; HolyStrike = WI-D target.

---

## 2026-03-09: Gear Equip, Panel Refresh, and Input Escape Fixes

### Bug 1 — ShowEquipmentComparison bypassing _contentLines

**Root cause:** `ShowEquipmentComparison` (in SpectreLayoutDisplayService.Input.cs) when Live was active would call `_contentLines.Clear()` then directly invoke `_ctx.UpdatePanel(SpectreLayout.Panels.Content, panel)` with a Spectre `Table` widget. This bypassed the `_contentLines` buffer entirely. The very next `ShowMessage` call (which runs in `DoEquip`) invokes `AppendContent` → `RefreshContentPanel()`, which rebuilds the Content panel from the now-empty `_contentLines`, immediately overwriting the comparison Table with a bare text panel. The comparison was effectively invisible — shown for 0ms.

**Fix:** Replaced the Live-path direct panel update with `SetContent(text, "⚔  ITEM COMPARISON", Color.Yellow)`. Added two private markup-string helpers (`AppendIntCompareLine`, `AppendPctCompareLine`) that populate `_contentLines` with formatted markup. The pre-Live path (startup, pre-`StartAsync`) keeps the rich Spectre Table + `AnsiConsole.Write`. Now the comparison persists in `_contentLines`, and subsequent `ShowMessage` calls *append* below it rather than overwriting it.

**Files:** `Dungnz.Display/Spectre/SpectreLayoutDisplayService.Input.cs`

### Bug 2 — Gear Panel Not Updating After ShowRoom

**Root cause:** `ShowRoom` re-rendered the Stats panel (`RenderStatsPanel(_cachedPlayer)`) but never called `RenderGearPanel`. While `DoEquip` correctly called `ShowPlayerStats` (which calls both RenderStatsPanel and RenderGearPanel) just before `EquipCommandHandler` invoked `ShowRoom`, the Gear panel was left unrefreshed on all other `ShowRoom` calls — e.g. after moving to a new room. More critically, `ShowRoom` is called by `EquipCommandHandler` immediately after equip, and if the gear update from `ShowPlayerStats` and the subsequent Stats re-render from `ShowRoom` happened in a tight batch, the Gear panel could appear stale.

**Fix:** Added `RenderGearPanel(_cachedPlayer)` alongside `RenderStatsPanel(_cachedPlayer)` in `ShowRoom`. Updated the comment: "Auto-populate map, stats, and gear panels on room entry." This ensures the Gear panel is always authoritative after any `ShowRoom` call.

**Files:** `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs`

### Bug 3 — ContentPanelMenu Escape/Q Ignoring Cancel

**Root cause:** `ContentPanelMenu<T>` (non-nullable variant, used when Live is active) previously auto-selected the last item on Escape/Q. Commit #1288 "fixed" this by making Escape/Q a no-op with the comment "Escape/Q do not cancel — ignore and let the user choose." This broke cancel for shop, sell, crafting, shrine, and armory menus — all of which end with `("← Cancel", 0)` as the last item. Players pressing Escape were stuck in the menu with no escape route.

**Fix:** Added a cancel-sentinel check: if the last item's label contains "Cancel" (case-insensitive) or starts with "←", Escape/Q returns that item's value as the cancel sentinel. Menus without an explicit cancel option (SelectDifficulty, SelectClass) are always shown pre-Live via `AnsiConsole.Prompt` — they never reach `ContentPanelMenu` — so they are unaffected.

**Files:** `Dungnz.Display/Spectre/SpectreLayoutDisplayService.Input.cs`

### Patterns Established

- `ShowRoom` should always refresh all three persistent panels: Map, Stats, AND Gear.
- Content panel updates must go through `SetContent` / `AppendContent` to keep `_contentLines` in sync; never call `_ctx.UpdatePanel(Panels.Content, ...)` directly while Live is active.
- `ContentPanelMenu<T>` cancel-sentinel convention: last item with "← Cancel" or "←" label is the cancel option; Escape/Q navigates there automatically.


### 2026-03-10: Implemented combat HUD enemy traits (#1307), boss phase (#1308), regen indicators (#1309) in ShowCombatStatus — PR #1310 merged
