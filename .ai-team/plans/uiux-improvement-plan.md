# UI/UX Improvement Plan

## Overview

The goal is to make TextGame feel like a polished console dungeon crawler — visually coherent,
informationally rich, and satisfying to interact with. The game's mechanical foundation is solid:
combat, abilities, status effects, loot tiers, crafting, achievements, multi-floor dungeons. The
rendering layer has not kept pace. This plan addresses that gap in three focused phases, moving
from highest-impact combat feedback to exploration polish to information architecture improvements.

All changes respect the existing architecture: output routed through `IDisplayService`, no raw
`Console.Write` bypasses, ANSI handled via `ColorCodes`, IDisplayService contract changes
require TestDisplayService/FakeDisplayService updates.

---

## Current State Assessment

### What Exists and Works
- ANSI color palette in `ColorCodes.cs`: full set (Red, Green, Blue, Yellow, Cyan, BrightRed,
  BrightWhite, BrightCyan, Magenta, Gray, Bold, Reset) plus context helpers (HealthColor,
  ManaColor, WeightColor, GetRoomTypeColor, ColorizeItemName, StripAnsiCodes)
- Map: fog of war, corridor connectors, room-type color coding, color-coded legend
- Inventory: item-tier colors, slot/weight display, equipped `[E]` tags, grouping by name
- Loot drop cards: box-drawn with tier, stat line, partial upgrade comparison
- Item detail cards (`ShowItemDetail`): full box with all stats, description word-wrap
- Shop and crafting recipe cards: box-drawn with availability indicators
- Combat: HP/MP numbers with HealthColor/ManaColor applied, recent turn log

### What's Missing or Broken
- **No HP/MP bars** — combat status is numbers only; no width-normalized visual bar
- **Status effects invisible** during combat — Poisoned/Stunned player has no persistent indicator
- **Boss enrage has no persistent indicator** — one-time scroll message only
- **Elite enemy tags absent** from combat status — player discovers mechanics by observation
- **`ShowLootDrop` ANSI padding bug** — colorized tier strings corrupt box alignment
- **Level-up choice lacks current values** — player can't see "current: 14 → 16"
- **XP progress not shown** — raw XP number with no next-level threshold
- **No persistent player status** — HP only visible from `STATS` command or last combat message
- **Exits rendered in generation order** — no compass convention, no directional arrows
- **Unvisited rooms on map are blank** — ambiguous between "unexplored" and "no room"
- **Combat entry is abrupt** — no visual separator between room description and combat start
- **Shrine uses different input model** (single-char hotkeys) — breaks verb-driven command convention
- **`EXAMINE` on enemy not surfaced** — inline one-liner, no box card like `ShowItemDetail`
- **Victory/GameOver screens live in GameLoop** — box-drawing logic belongs in display layer
- **Floor transitions are a one-liner** — no banner, no atmospheric weight
- **Achievement unlocks have no in-combat notification path**
- **Item descriptions invisible in inventory** — consumables show no heal/mana values
- **Equipment slot summary absent** — `[E]` tags in inventory list, no dedicated slot overview
- **Ability confirmation feedback weak** — no post-use summary line on success
- **Immune-to-effects gives no feedback** — `Apply()` silently no-ops; player doesn't know why

---

## Phase 1 — Combat Feel

**Theme:** Make every combat turn legible, impactful, and emotionally resonant.  
**Goal:** Player should always know their state, the enemy's state, and what just happened — without having to scroll or guess.

### 1.1 — HP/MP Bars in `ShowCombatStatus` (Hill)
Replace the numbers-only `[You: 45/100 HP]` display with a width-normalized bar alongside the
number. Format: `[████████░░ 45/100]`. Bar width: 10 characters. Use `HealthColor()` for the
filled segment and `Gray` for the empty segment. Apply same to enemy. Mana bar alongside MP
values using `ManaColor()`.

- **Implementation:** Private helper `RenderBar(int current, int max, int width, string fillColor)`
  in `ConsoleDisplayService`. Internal only — no interface change.
- **Owner:** Hill

### 1.2 — Active Effects in `ShowCombatStatus` (Barton + Hill)
Show player and enemy active status effects inline in the combat status header. Format:
`[☠ Poison 2t] [⚡ Stun 1t]` after the HP/MP display.

- **Interface change required:** `ShowCombatStatus(Player player, Enemy enemy,
  IReadOnlyList<ActiveEffect> playerEffects, IReadOnlyList<ActiveEffect> enemyEffects)` — Barton
  passes the collections from `StatusEffectManager` at each call site in `CombatEngine`.
- **Effect icons:** Poison ☠, Bleed 🩸, Stun ⚡, Regen ✨, Fortified 🛡, Weakened 💀 — add
  as constants to `ColorCodes` or a new `StatusEffectIcons` helper.
- **Owner:** Barton (call-site wiring), Hill (display rendering)
- **Design Review needed:** IDisplayService signature change requires TestDisplayService update.

### 1.3 — Elite / Enrage / Special Tags in Combat (Barton)
At combat start, emit a one-line indicator for notable enemy properties:
- `⭐ ELITE — enhanced stats and loot` (yellow, if `IsElite`)
- `⚡ ENRAGED` persistent tag in status header once `DungeonBoss.IsEnraged` is true
- `💉 Lifesteal` / `👻 Flat Dodge` one-time tip at combat entry for Vampire/Wraith

- **Implementation:** New `ShowCombatEntryFlags(Enemy enemy)` on IDisplayService.  
  `ShowCombatStatus` gains an `isEnraged` bool (or reads from `DungeonBoss` subtype).
- **Owner:** Barton (call-site wiring), Hill (display rendering)

### 1.4 — Colorized Turn Log (Barton)
In `ShowRecentTurns`, apply color to damage numbers (BrightRed for damage dealt,
Green for healing), bold+yellow for CRIT, and colored status effect tags for effect applications.
Does not require an interface change — these are string transforms on the messages before passing
to display.

- **Implementation:** Pre-colorize log entries in `CombatEngine` before appending to turn log.
  Or pass structured turn data and let `ShowRecentTurns` apply color — latter preferred.
- **Owner:** Barton

### 1.5 — Level-Up Choice Menu with Current Values (Barton)
On level-up choice, show current stat values alongside the delta:
- `[1] +5 Max HP     (current: 100 → 105)`
- `[2] +2 Attack     (current: 14 → 16)`
- `[3] +2 Defense    (current: 8 → 10)`

- **Implementation:** `ShowLevelUpChoice(Player player)` on IDisplayService (currently inlined
  in CombatEngine). Display-only — all data is on `player`.
- **Owner:** Barton (call-site), Hill (display method)

### 1.6 — XP Progress Post-Combat and in Stats (Barton + Hill)
After combat, show `"You gained 25 XP. (Total: 75/100 to next level)"` with a mini bar or
fraction indicator. In `ShowPlayerStats`, replace raw XP with `XP: 75/100 ▓▓▓▓▓░░░░░ Lv3`.

- **Implementation:** `Player.XPToNextLevel` computed property (or pass threshold to display).
  XP threshold: `100 * player.Level`. Display-only given the threshold formula.
- **Owner:** Barton (combat message), Hill (ShowPlayerStats), shared formula.

### 1.7 — Ability Confirmation Message (Barton)
On `UseAbilityResult.Success`, emit a colored confirmation line before normal turn flow continues:
`"[Power Strike activated — 2× damage this turn]"` in Bold+Yellow. Brief, one line.

- **Owner:** Barton (in `HandleAbilityMenu` after success)

### 1.8 — Status Effect Immune Feedback (Barton)
In `StatusEffectManager.Apply()`, when target is immune, emit a message via the display service:
`"{enemyName} is immune to status effects!"` in Gray+Italic or similar.

- **Owner:** Barton (one-line in Apply())
- **Note:** StatusEffectManager needs IDisplayService access — it may already have it, confirm.

### 1.9 — Achievement Unlock Notification in Combat (Barton)
Wire a `GameEvents.OnAchievementUnlocked` subscriber in `CombatEngine` that queues a display
banner at turn boundary: `"🏆 Achievement Unlocked: Glass Cannon!"` in Bold+Yellow. Does not
interrupt the turn — shown at start of next turn display.

- **Owner:** Barton

### 1.10 — Combat Entry Visual Separator (Hill)
Before `ShowCombatStatus` on combat start, emit a visual separator to mark the transition from
exploration to combat: a colored divider line and `"⚔  COMBAT BEGINS ⚔"` banner.

- **Implementation:** `ShowCombatStart(Enemy enemy)` on IDisplayService — called once at the
  start of `CombatEngine.StartCombat`.
- **Owner:** Hill (display method), Barton (call-site)

---

## Phase 2 — Navigation & Exploration Polish

**Theme:** Make moving through the dungeon feel atmospheric and legible.  
**Goal:** Player always knows where they are, where they can go, and that the world has weight.

### 2.1 — Compass-Ordered Exits with Arrows (Hill)
Replace `room.Exits.Keys` insertion-order join with fixed NSEW ordering and directional arrows.
Format: `↑ North   ↓ South   → East` (only showing available exits).

- **Implementation:** Inline in `ShowRoom()`, constant direction ordering array. No interface change.
- **Owner:** Hill

### 2.2 — Floor Transition Banner (Hill)
Replace `ShowMessage($"Floor {_currentFloor}")` with a proper display method:
`ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)` rendering a box with floor
number, variant name (Catacombs/Dungeon/Forgotten/etc.), and threat-level color (green early,
yellow mid, red deep).

- **Implementation:** New method on `IDisplayService`. Three GameLoop call-sites to update.
  TestDisplayService gets a no-op stub.
- **Owner:** Hill

### 2.3 — Persistent Status Mini-Bar (Hill)
Show a minimal status line before each command prompt:
`[HP 34/100 ▓▓░░░░ | MP 20/30 ▓▓▓░ | Floor 2] > `

This is the highest-impact friction-reduction change. Player no longer needs to `STATS` to see
their health state.

- **Implementation:** `ShowCommandPrompt(Player player)` on IDisplayService (currently takes no
  args). Bar rendering from Phase 1.1 helper. This is an interface change — TestDisplayService
  and all test call-sites must be updated. **Coordinate with Romanoff before merging.**
- **Owner:** Hill
- **Design Review needed:** Interface change has broad test impact.

### 2.4 — Map Unvisited Rooms as `?` (Hill)
Unvisited rooms should render as `[?]` rather than blank space. Clarifies fog-of-war as
"unexplored" vs "absent." Detected rooms (adjacent to visited) could render as dimmer `[?]`
vs fully blank for truly unknown rooms.

- **Implementation:** In `GetRoomSymbol()` / map rendering loop: check if room is in known graph
  but `!room.Visited` → render `[?]` in Gray. No interface change.
- **Owner:** Hill

### 2.5 — Enemy Health State on Map (Hill)
`GetRoomSymbol()` returns `[!]` for any live enemy room. Low-HP enemy (< 30%) could render as
`[~]` in Yellow to hint "wounded target here."

- **Implementation:** HP threshold check in `GetRoomSymbol()`. No interface change.
- **Owner:** Hill

### 2.6 — Hazard Room Forewarning (Hill + Barton)
When entering a hazardous room type (Scorched, Flooded, Dark), include a caution line in
`ShowRoom()` before damage fires: `"⚠ The scorched stone radiates heat — take care."` for
Scorched; `"⚠ The water here looks treacherous."` for Flooded.

- **Implementation:** In `ShowRoom()`, add a type-aware caution line before the standard room
  description. No interface change if it's part of existing `ShowRoom(Room room)`.
- **Owner:** Hill

### 2.7 — DESCEND / Exit Discoverability (Hill)
When player enters a room with `IsExit == true` and boss is dead (or null), `ShowRoom()` should
include: `"✦ A staircase descends deeper. (DESCEND to continue)"`. Currently this is only shown
as a one-time message and lost to scroll.

- **Implementation:** `ShowRoom` gains contextual awareness via a `bool isExitAvailable` param,
  or GameLoop emits it separately via `ShowDescendHint()` on IDisplayService after ShowRoom.
- **Owner:** Hill

### 2.8 — Contextual Prompt Hints (Hill)
When in a shrine room: append `(shrine here — USE SHRINE)` after the room description footer.
When merchant is present: `(merchant here — SHOP)`. This is not a persistent prompt bar —
just a one-time hint when the room is first shown (or every LOOK).

- **Implementation:** `ShowRoom` can check `room.HasShrine` / `room.Merchant != null` and emit
  the hint. Already has room type data. Display-only.
- **Owner:** Hill

---

## Phase 3 — Information Architecture

**Theme:** Surface the right information at the right time without requiring extra commands.  
**Goal:** Inventory decisions, equipment comparisons, and item discovery feel intuitive.

### 3.1 — Enemy Examine Box Card (Hill)
Replace `EXAMINE <enemy>` inline one-liner in GameLoop with a proper box card via
`ShowEnemyDetail(Enemy enemy)` on IDisplayService, matching the design of `ShowItemDetail`.
Include: name, HP/MaxHP bar, ATK, DEF, XP reward, type, elite flag, known abilities.

- **Owner:** Hill
- **Interface change:** `ShowEnemyDetail(Enemy enemy)` added to IDisplayService.

### 3.2 — Victory and GameOver Screens to DisplayService (Hill + Barton)
`ShowVictory` and `ShowGameOver` are currently private GameLoop methods doing their own
box-drawing. Move to IDisplayService as:
- `ShowVictory(Player player, int floorsCleared, RunStats stats)` 
- `ShowGameOver(Player player, string? killedBy, RunStats stats)`

NarrationService flavor lines: pre-pick in GameLoop, pass as string to the display method —
avoids injecting NarrationService into DisplayService.

- **Owner:** Hill (display methods), Barton (call-site coordination)
- **Interface change:** Two new methods on IDisplayService.

### 3.3 — Item Descriptions in ShowInventory (Barton + Hill)
Surface `Item.Description` (consumable effect, unique mechanic) inline in the inventory listing.
For consumables: show heal/mana values. For equipables with special effects (bleed-on-hit, poison
immunity): show a one-word tag. Keep the listing compact — use `Gray` for the description sub-line.

- **Owner:** Barton (deciding what to show per type), Hill (rendering in ShowInventory)

### 3.4 — Equipment Slot Summary Block (Barton + Hill)
Add a dedicated "EQUIPPED" block to `ShowInventory` output (or as part of `ShowPlayerStats`)
showing the three slots clearly:
```
⚔  Weapon:    Dark Blade [+15 ATK]
🛡  Armor:     Knight's Armor [+12 DEF]
💍 Accessory: (empty)
```
Currently the only reference is `[E]` tags scattered through the item list.

- **Owner:** Barton (deciding what to show per slot), Hill (rendering)

### 3.5 — Full Loot Comparison (Barton + Hill)
Extend `ShowLootDrop` comparison to cover all relevant stat deltas:
- Weapon: ATK delta vs `EquippedWeapon`
- Armor: DEF delta vs `EquippedArmor`
- Accessory: all bonus deltas vs `EquippedAccessory`
- Consumables: no comparison (just show effect values clearly)

Also fix the ANSI padding bug: use `ColorCodes.StripAnsiCodes()` to compute plain-text lengths
before applying format specifiers in the box lines.

- **Owner:** Barton (comparison logic), Hill (ANSI padding fix)

### 3.6 — Shrine Command Consistency (Barton)
Shrine interaction currently uses single-char hotkeys (`[H]`, `[B]`, `[F]`, `[M]`, `[L]`), which
breaks the game's verb-driven command model. Normalize to `USE HEAL`, `USE BLESS`, etc. — or
retain single-char hotkeys but clearly label the shrine as a "special menu" and say so on entry.
This is the smallest acceptable fix: add a banner on shrine entry:
`"✨ [Shrine Menu] — press H/B/F/M or L to leave."` so the input model break is at least
disclosed.

- **Owner:** Barton
- **Note:** Full normalization to verb commands is a GameLoop change and should be scoped as a
  separate work item if pursued.

### 3.7 — Class Selection Abilities Preview (Hill)
The class selection screen shows stat bars but not the abilities/skills available to each class.
Add a brief `"Abilities: Power Strike (L1), Defensive Stance (L3)"` line to each class card in
`ShowClassSelection`.

- **Owner:** Hill

### 3.8 — Save Command Default Name (Barton / Hill)
`SAVE` without a name currently returns `✗ Save as what? Usage: SAVE <name>`. Add a
timestamp-based default: `SAVE` alone → `SAVE autosave-{DateTime.Now:yyyyMMdd-HHmm}`. Minor
GameLoop change.

- **Owner:** Hill (GameLoop command handler)

---

## Shared Infrastructure

These components are needed across multiple phases and should be built first within their phase.

### `RenderBar(int current, int max, int width, string fillColor)` — Phase 1
Private helper in `ConsoleDisplayService`. Renders `████████░░` with fill color and Gray empty.
Used by: Phase 1.1 (combat), Phase 2.3 (prompt status), 3.1 (enemy detail), potentially stats.

### Status Effect Icons — Phase 1
Constants or a static lookup in `ColorCodes` or a new `EffectIcons` class:
`Poison → "☠"`, `Bleed → "🩸"`, `Stun → "⚡"`, `Regen → "✨"`, `Fortified → "🛡"`, `Weakened → "💀"`

### `ShowCombatStatus` Signature Change — Phase 1.2
Adds `IReadOnlyList<ActiveEffect>` parameters. All IDisplayService consumers (TestDisplayService,
FakeDisplayService if any) must be updated. Romanoff: coordinate test updates before merging.

### `ShowCommandPrompt(Player player)` — Phase 2.3
Interface change from no-args to Player parameter. Wide test impact. Romanoff must audit and
update test call-sites across the test suite before this merges. Consider phasing: add an
overload `ShowCommandPrompt(Player? player = null)` to minimize churn.

### New IDisplayService Methods (all phases)
The following methods are new additions to the interface — each requires a no-op/stub in
TestDisplayService:
- `ShowCombatStart(Enemy enemy)` — Phase 1.10
- `ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant)` — Phase 2.2
- `ShowEnemyDetail(Enemy enemy)` — Phase 3.1
- `ShowVictory(Player player, int floorsCleared, RunStats stats)` — Phase 3.2
- `ShowGameOver(Player player, string? killedBy, RunStats stats)` — Phase 3.2
- `ShowLevelUpChoice(Player player)` — Phase 1.5

### ANSI-Safe Box Padding Helper — Phase 3.5
Utility in `ColorCodes` or `ConsoleDisplayService`:
`static int VisibleLength(string s)` → `s.Length - StripAnsiCodes(s).Length` correction.
Use pattern: `new string(' ', Math.Max(0, targetWidth - ColorCodes.StripAnsiCodes(s).Length))`.
This pattern is already used correctly in some methods but not `ShowLootDrop`. Standardize it.

---

## Success Criteria

**Phase 1 complete when:**
- Combat turn always shows HP/MP bars, active status effects, and elite/enrage flags
- Player never has to ask "am I poisoned?" — it's on screen
- Turn log damage numbers are color-coded; crits are visually distinct
- Level-up choices show current → projected values
- XP progress is visible with a fractional or bar indicator

**Phase 2 complete when:**
- Player can see their HP/MP before typing a command (without typing `STATS`)
- Map shows `[?]` for unexplored rooms instead of blank space
- Exits are always rendered in compass order with arrows
- Floor transitions feel like events, not footnotes
- Shrine/merchant rooms announce their presence without player discovery

**Phase 3 complete when:**
- `EXAMINE <enemy>` gives a box card, not a one-liner
- Inventory shows equipment slot summary and item descriptions/effects
- Loot drop cards show meaningful comparison for all slot types
- Victory and game-over screens feel climactic
- Class selection previews the abilities each class unlocks

**Overall success signal:** A first-time player completing floor 1 understands their HP, their
resources, what they're fighting, what they picked up, and how to proceed — without reading the
help text or making a wrong-turn command.
