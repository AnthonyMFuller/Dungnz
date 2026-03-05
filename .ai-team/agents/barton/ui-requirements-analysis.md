# UI/UX Requirements Document — Systems & Game-Feel Perspective

**Author:** Barton (Systems Developer)  
**Date:** 2026-03-05  
**Context:** Analysis of current TUI implementation and Spectre.Console fallback  
**Purpose:** Define what the UI must do well for combat, inventory, exploration, and stat visibility

---

## Executive Summary

The TUI implementation provides persistent layout (map, stats, log, content) which is a **massive UX win** over the old "type STATS to see your HP" model. However, several critical game-feel issues remain:

1. **No visual urgency signals** — HP bars are plain text, no color gradient for danger
2. **Combat log drowns critical info** — damage numbers scroll off screen, no emphasis
3. **Item comparison is text-heavy** — comparing equipped vs new gear requires parsing numbers
4. **Status effects lack prominence** — buffs/debuffs are hidden in text lists
5. **No damage type feedback** — Physical/Fire/Poison/Holy all look identical
6. **Color is completely stripped in TUI** — ShowColoredMessage delegates to plain text

The ideal UI must **amplify tension in combat**, **make loot decisions fast**, and **keep resource urgency (HP/MP) always visible**.

---

## 1. Combat Display Requirements

### 1.1 Always-Visible Combat State

**Requirement:** During combat, the following must be visible **without scrolling**:

| Info | Visibility | Priority |
|------|-----------|----------|
| Player HP bar (with urgency color) | Always | CRITICAL |
| Player MP bar (for casters) | Always | CRITICAL |
| Enemy HP bar (current target) | Always | CRITICAL |
| Active status effects (player) | Always | HIGH |
| Active status effects (enemy) | Always | HIGH |
| Current turn indicator | Always | MEDIUM |
| Player ATK/DEF (current, after buffs) | On-demand (STATS cmd) | MEDIUM |
| Enemy ATK/DEF | On-demand (EXAMINE cmd) | LOW |

**Current Implementation:**
- ✅ ShowCombatStatus displays HP/MP bars and status effects
- ❌ HP bars are plain ASCII with no color urgency
- ❌ Status effects are text-only `[Regen 3t]` with no visual weight

**What's Missing:**
- **HP urgency coloring** — bars should glow RED when player HP < 25%, YELLOW when < 50%
- **MP urgency coloring** — mana bar should glow DIM/GRAY when MP < 20% (can't cast)
- **Status effect icons** — `[🔥 Burn 3t]` is more scannable than `[Burn 3t]`
- **Turn counter** — "Turn 5/∞" helps players track ability cooldowns

### 1.2 Damage Numbers & Combat Log

**Requirement:** Damage numbers must be **immediately distinguishable** by type and severity.

| Damage Type | Visual Treatment |
|-------------|-----------------|
| **Critical Hit** | `💥 CRIT! 45 damage` (bright, bold, emoji prefix) |
| **Normal Hit** | `⚔ 18 damage` (neutral color) |
| **Glancing Blow** | `~ 3 damage` (dim, gray) |
| **Miss** | `✗ MISS` (gray) |
| **Dodge** | `⚡ DODGED!` (cyan, implies speed) |
| **Block** | `🛡 BLOCKED 12 dmg` (blue) |
| **Fire Damage** | `🔥 15 fire damage` (orange/red) |
| **Poison Damage** | `☠ 8 poison damage` (green) |
| **Holy Damage** | `✨ 20 holy damage` (yellow/gold) |

**Current Implementation:**
- ❌ All damage is plain text: `"You deal 18 damage to the Goblin."`
- ❌ Crits use ANSI codes in Spectre but are stripped in TUI
- ❌ No emoji/icon differentiation

**What's Missing:**
- **Damage type icons** — Fire should look different from Physical
- **Critical hit emphasis** — Should be visually exciting (player reward for RNG)
- **Damage magnitude scaling** — Big numbers (50+) could be bolder/larger
- **Combat log color** — Currently strips all ShowColoredCombatMessage colors

**Combat Log Scrolling:**
- Message log holds last 100 messages (MaxMessageHistory)
- **Good:** Auto-scrolls to bottom on new message
- **Bad:** No way to scroll back mid-combat to review what happened 3 turns ago
- **Recommendation:** Message log should be scrollable with PgUp/PgDn during combat menu

### 1.3 Status Effects Display

**Requirement:** Buffs must feel **powerful**, debuffs must feel **threatening**.

| Effect Type | Visual Treatment |
|-------------|-----------------|
| **Buff (Regen, Fortified, Blessed)** | `[✨ Regen 3t]` (green/cyan, uplifting) |
| **Debuff (Poison, Bleed, Burn)** | `[💀 Poison 2t]` (red, ominous) |
| **Control (Stun, Freeze)** | `[❄ Frozen 1t]` (blue, immobilized) |
| **Neutral (Hunter's Mark)** | `[🎯 Marked]` (yellow) |

**Current Implementation:**
- TUI: `"Effects: [Regen 3t] [Poison 2t]"` (plain text)
- Spectre: Uses color but no icons

**What's Missing:**
- **Icon prefix** — `[🔥 Burn 3t]` is more scannable than `[Burn 3t]`
- **Turn urgency** — Effects with 1 turn left should be dimmer (fading out)
- **Buff/debuff separation** — Buffs on left, debuffs on right in status display?

### 1.4 Enemy Health Feedback

**Requirement:** Players need to **feel progress** in long boss fights.

**Current Implementation:**
- ShowCombatStatus displays enemy HP bar + numeric `80/100`
- HP bar is 8 characters wide: `[████████]` (full) → `[████░░░░]` (half)

**What's Missing:**
- **Boss phase indicators** — "Phase 2" or "ENRAGED" label when boss < 40% HP
- **HP threshold warnings** — When enemy HP crosses 50%/25%, show message: `"The Goblin Warchief looks bloodied!"`
- **Minion HP** — When enemy has minions, show their HP as sub-bars or count

### 1.5 Turn Flow & Action Feedback

**Requirement:** Players must always know whose turn it is and what just happened.

**Current Implementation:**
- Combat menu shows: `⚔ Attack`, `✨ Ability`, `🏃 Flee`
- After player action, ShowCombatMessage prints result
- After enemy action, ShowCombatMessage prints result
- Repeat

**What's Missing:**
- **Turn indicator** — `[YOUR TURN]` vs `[ENEMY TURN]` banner
- **Action confirmation** — When player selects "Attack", show `> You attack!` before damage calc
- **Ability cooldown display** — In combat menu, show `✨ Ability (2t CD)` if on cooldown

---

## 2. Exploration / Room Display Requirements

### 2.1 Room Description Rendering

**Requirement:** Room descriptions must be **immersive** but not wall-of-text.

**Current Implementation (TUI):**
- Room type prefix: `🌑 The room is pitch dark.`
- Room description: `"A narrow chamber with damp walls."`
- Hazard warning: `🔥 Lava seams crack the floor — each action will burn you.`
- Exits: `Exits: ↑ North   → East`
- Enemy: `⚔ Goblin is here!`
- Items: `◆ Iron Sword (+8 ATK)`
- Special: `✨ A shrine glimmers here. (USE SHRINE)`

**What Works:**
- ✅ Emoji icons make exits/enemies/items scannable
- ✅ Hazard warnings are prominent

**What's Missing:**
- **Environmental storytelling** — Scorched rooms should feel HOT, flooded rooms should feel DAMP
- **Danger level signaling** — Elite enemy rooms should have red borders or warning icons
- **Boss room fanfare** — Boss rooms need big dramatic entry (ShowCombatStart exists but could be bigger)

### 2.2 Mini-Map Display

**Requirement:** The map must show **where you are**, **where you've been**, and **where you can go**.

**Current Implementation:**
- BuildAsciiMap() does BFS from current room, assigns grid positions, renders fog-of-war
- 15-symbol legend: `[@]` = You, `[B]` = Boss, `[S]` = Shrine, `[M]` = Merchant, `[E]` = Enemy, `[?]` = Unexplored
- Corridors: `───`, `│`, `┌`, `└`, etc.
- Compass rose in corner
- Auto-updates on ShowRoom (#1038)

**What Works:**
- ✅ Map is persistent and always visible in TUI
- ✅ Fog-of-war creates exploration incentive

**What's Missing:**
- **Danger indicators** — Elite enemy rooms should pulse or have `[E!]` instead of `[E]`
- **Path highlighting** — Last 3 rooms visited could be dimmer (breadcrumb trail)
- **Floor transition indicator** — Show stairs/portal symbol for DescendCommandHandler destination
- **Map zoom/scale** — Large dungeons (20+ rooms) might overflow the panel

### 2.3 Exit Indicators & Navigation

**Requirement:** Players must **never be confused** about which direction they can go.

**Current Implementation:**
- ShowRoom lists: `Exits: ↑ North   ↓ South   → East   ← West`
- `GO NORTH` or `N` to move

**What Works:**
- ✅ Arrow symbols are clear
- ✅ Abbreviated commands (N/S/E/W) are fast

**What's Missing:**
- **Locked/blocked exits** — If an exit exists but is locked, show `🔒 North (locked)`
- **Exit danger hints** — If map knows enemy in adjacent room, show `⚠ East (enemy detected)`

---

## 3. Inventory / Equipment Management Requirements

### 3.1 Item Comparison (CRITICAL for Loot Decisions)

**Requirement:** When a new item drops, players must instantly know if it's an upgrade.

**Current Implementation:**
- ShowLootDrop displays item card: `✦ LOOT DROP`, item name, tier, primary stat
- ShowEquipmentComparison (on EQUIP command) shows before/after with color-coded deltas

**What Works:**
- ✅ ShowEquipmentComparison exists and shows `+5 ATK` in green, `-2 DEF` in red

**What's Missing (IN LOOT DROP):**
- **Comparison at drop time** — ShowLootDrop should show `+3 ATK vs equipped` immediately
- **Stat deltas on pickup** — When you TAKE an item, show `"Iron Sword: +3 ATK over Rusty Blade"`
- **Set bonus preview** — If item completes a 2-piece set, show `"(Shadowstalker 2/3: +10% Dodge)"`

### 3.2 Equipment Slots Clarity

**Requirement:** Players must know **what's equipped** and **what slots are empty** at a glance.

**Current Implementation (ShowEquipment in Spectre):**
- Table layout: `⚔ Weapon`, `🪖 Head`, `🥋 Shoulders`, `🛡 Chest`, `🧤 Hands`, `🥾 Feet`
- Shows item name, tier color, and set name if applicable

**What Works:**
- ✅ Emoji icons make slots scannable
- ✅ Tier colors (Common=white, Rare=blue, Epic=purple, Legendary=gold) are clear

**What's Missing (IN TUI):**
- TUI BuildStatsText shows equipment as flat list, no table/grid
- **Empty slot indicators** — Should show `🪖 Head: (empty)` instead of omitting the line
- **Set bonus progress** — Should show `Shadowstalker (2/3)` under set items

### 3.3 Weight / Inventory Capacity

**Requirement:** Players must know when they're **close to full** before picking up items.

**Current Implementation:**
- ShowItemPickup displays: `"Picked up Iron Sword. Slots: 8/20, Weight: 45/100"`
- ShowInventory header: `"Inventory (8/20 slots)"`

**What Works:**
- ✅ Slot count is visible

**What's Missing:**
- **Weight urgency** — When weight > 80%, show `"Weight: 85/100 [HEAVY]"` in yellow/red
- **Pickup warnings** — If picking up an item would exceed capacity, show warning BEFORE pickup

---

## 4. Stats Panel Requirements

### 4.1 At-a-Glance Priority Stats

**Requirement:** The stats panel must show **survival-critical info** without clutter.

**Current Implementation (TUI BuildStatsText):**
```
⚔ PlayerName
Class: Warrior
Level: 5
XP: 320/500

HP: [████████] [OK]
    80/80
MP: [████░░░░] [LOW]
    8/20

ATK: 18
DEF: 12
Gold: 250g

Equipment:
  ⚔ Iron Sword
  🪖 Iron Helm
  ...
```

**What Works:**
- ✅ HP/MP bars with status labels `[OK]` / `[LOW]` / `[CRIT]`
- ✅ All core stats visible
- ✅ Equipment list

**What's Missing:**
- **HP/MP bar colors** — Bars are plain ASCII, no color urgency (RED when CRIT)
- **Temporary buffs** — Shrine blessings (Fortified, Blessed) should show in stats panel
- **Class passive indicators** — Warrior Battle Hardened stacks, Rogue Combo Points, Necromancer minion count

### 4.2 HP/MP Urgency Signals

**Requirement:** Players must **feel danger** when HP is low, not just read it.

**Current Implementation:**
- Text labels: `[OK]` (HP > 50%), `[LOW]` (HP 25-50%), `[CRIT]` (HP < 25%)
- Bars: `[████████]` (full) → `[██░░░░░░]` (low)

**What's Missing:**
- **Color zones** — GREEN bar when OK, YELLOW when LOW, RED when CRIT
- **Pulsing/blinking** — When HP < 25%, bar should blink or pulse (if Terminal.Gui supports it)
- **Audio cue?** — Terminal beep when HP drops below 25% (optional, low priority)

### 4.3 Level / XP Progression

**Requirement:** Players must feel **progress toward level-up** without typing STATS.

**Current Implementation:**
- Stats panel shows: `Level: 5`, `XP: 320/500`
- XP bar is not graphical, just numeric

**What's Missing:**
- **XP bar** — `XP: [████████░░] 320/500` (same style as HP/MP)
- **Level-up preview** — When XP > 80% of next level, show `"Level up soon!"`

---

## 5. Message Log Requirements

### 5.1 Message History Depth

**Requirement:** Log must keep enough history to review combat turns, but not infinite.

**Current Implementation:**
- `_messageHistory` max 100 messages
- Auto-scrolls to bottom on new message
- No scroll controls during gameplay

**What Works:**
- ✅ 100 messages is enough for 10-15 combat turns

**What's Missing:**
- **Scroll controls** — PgUp/PgDn to review history during combat menu
- **Message timestamps** — `[12:34]` prefix for debugging or speedrun analysis (low priority)

### 5.2 Message Type Differentiation

**Requirement:** Critical messages must **stand out** from flavor text.

**Message Types by Priority:**

| Type | Priority | Visual Treatment |
|------|---------|-----------------|
| **Player damage taken** | CRITICAL | `💔 You take 15 damage!` (RED) |
| **Player kills enemy** | HIGH | `⚔ Victory! Goblin defeated.` (GREEN) |
| **Loot drop** | HIGH | `✦ Iron Sword dropped!` (GOLD) |
| **Level up** | HIGH | `⭐ LEVEL UP! You are now level 6.` (GOLD) |
| **Status applied** | MEDIUM | `🔥 You are Burning! (3t)` (ORANGE) |
| **Combat action** | MEDIUM | `⚔ You attack for 18 damage.` (WHITE) |
| **Flavor text** | LOW | `"The goblin snarls at you."` (GRAY) |
| **System message** | LOW | `"Type HELP for commands."` (GRAY) |

**Current Implementation:**
- TUI strips all colors from ShowColoredMessage / ShowColoredCombatMessage
- Spectre preserves colors but no emoji prefix differentiation

**What's Missing:**
- **Emoji prefixes** — Every message type needs an icon for fast scanning
- **Color restoration in TUI** — ShowColoredMessage should map ANSI codes to Terminal.Gui Attribute
- **Message grouping** — Consecutive combat messages could be grouped: `"Turn 3: You attack (18), Enemy attacks (8)"`

### 5.3 Log Scrollability

**Requirement:** Players must be able to **review past messages** mid-combat.

**Current Implementation:**
- MessageLogPanel is a TextView with `.Text = string.Join("\n", _messageHistory)`
- Auto-scrolls to bottom via `.MoveEnd()`
- No scroll controls

**What's Missing:**
- **Scroll lock toggle** — Pressing `ScrollLock` or `Pause` stops auto-scroll
- **PgUp/PgDn bindings** — Scroll log without leaving combat menu
- **Search/filter** — `Ctrl+F` to search log for "CRIT" or "Poison" (low priority)

---

## 6. Biggest Pain Points (Current TUI)

From a **game-feel** perspective, ranked by impact:

### 6.1 No Color Urgency — CRITICAL

**Problem:** HP at 10/80 looks identical to HP at 80/80 except for the numbers.

**Impact:** Players don't **feel** danger. Low HP should trigger panic response, not require mental math.

**Fix:** Implement color zones:
- HP > 50%: GREEN bar
- HP 25-50%: YELLOW bar
- HP < 25%: RED bar + blink/pulse
- Same for MP (BLUE → CYAN → GRAY)

### 6.2 Loot Comparison Requires Manual Math — HIGH

**Problem:** When item drops, player must:
1. Read loot card: `Iron Sword (+8 ATK)`
2. Type `EQUIP` or `STATS` to see current weapon: `Rusty Blade (+5 ATK)`
3. Subtract 8 - 5 = +3 upgrade
4. Decide

**Impact:** Loot decisions take 10-15 seconds instead of 2 seconds. Breaks game flow.

**Fix:** ShowLootDrop should show comparison immediately:
```
✦ LOOT DROP
Iron Sword (Tier: Uncommon)
+8 ATK (+3 vs equipped Rusty Blade) <-- GREEN
```

### 6.3 Combat Log Drowns Damage Numbers — HIGH

**Problem:** In a 10-turn boss fight, damage numbers scroll off screen. Player can't review "Did my crit land?" or "How much did Poison tick for?"

**Impact:** Reduces tactical feedback. Players can't optimize builds if they can't see damage variance.

**Fix:** 
- Make log scrollable (PgUp/PgDn)
- OR: Add "Combat Summary" at end of fight: `"Total damage dealt: 180, Total taken: 45, Crits: 3"`

### 6.4 Status Effects Lack Visual Weight — MEDIUM

**Problem:** `[Regen 3t] [Poison 2t]` is a wall of text. Players miss "I'm poisoned!" warning.

**Impact:** Players take avoidable damage by not noticing debuffs.

**Fix:**
- Add emoji icons: `[🔥 Burn 3t]`
- Color-code: Buffs GREEN, Debuffs RED
- **OR:** Dedicate a "Status" sub-panel in stats area (3-4 lines max)

### 6.5 No Damage Type Differentiation — MEDIUM

**Problem:** Physical, Fire, Poison, Holy damage all look identical. No visual feedback on enemy resistances.

**Impact:** Players can't tell if "Fire damage is working" or "Enemy is Fire-resistant" without reading combat log.

**Fix:**
- Add damage type icons: `🔥 15 fire damage`, `⚔ 18 physical damage`, `☠ 8 poison damage`
- Color-code: Fire=ORANGE, Poison=GREEN, Holy=GOLD

### 6.6 Equipment Slots Hard to Scan (TUI) — LOW

**Problem:** TUI stats panel lists equipment as flat text, no table/grid. Empty slots are omitted, so player doesn't know "I have no helm."

**Impact:** Players forget to equip items in all slots.

**Fix:**
- Show all 6 slots, use `(empty)` for unequipped
- OR: Use table layout like Spectre (if Terminal.Gui supports it)

---

## 7. Wishlist: 3-5 Features That Would Most Improve Game Experience

### 7.1 Color-Coded Damage Numbers by Type ⭐⭐⭐ (TOP PRIORITY)

**What:** Every damage message includes icon + color:
- `🔥 15 fire damage` (ORANGE)
- `⚔ 18 physical damage` (WHITE)
- `☠ 8 poison damage` (GREEN)
- `✨ 20 holy damage` (GOLD)
- `💥 CRIT! 45 damage` (BRIGHT RED, bold)

**Why:** Instant visual feedback on damage type. Players learn enemy resistances by **feeling** it, not reading logs.

**Effort:** MEDIUM — requires wiring TuiColorMapper + adding emoji prefixes to all damage messages.

### 7.2 HP/MP Bar Color Urgency Zones ⭐⭐⭐ (TOP PRIORITY)

**What:** HP bars change color based on thresholds:
- GREEN (HP > 50%)
- YELLOW (HP 25-50%)
- RED (HP < 25%) + blink/pulse if possible

**Why:** **Visceral danger signal.** Red HP bar triggers fight-or-flight response. Dramatically improves combat tension.

**Effort:** LOW — BuildColoredHpBar already exists (#1041), just needs Terminal.Gui Attribute wiring.

### 7.3 Instant Loot Comparison at Drop ⭐⭐ (HIGH PRIORITY)

**What:** ShowLootDrop includes delta vs equipped item:
```
✦ LOOT DROP
Iron Sword (Tier: Uncommon)
+8 ATK (+3 upgrade) <-- GREEN
+0 DEF (no change)
```

**Why:** Eliminates mental math. Loot decisions go from 10 seconds to 2 seconds.

**Effort:** MEDIUM — ShowLootDrop needs to call ShowEquipmentComparison logic inline.

### 7.4 Status Effect Icons & Color-Coding ⭐⭐ (HIGH PRIORITY)

**What:** Replace `[Regen 3t]` with `[✨ Regen 3t]` (GREEN) and `[Poison 2t]` with `[💀 Poison 2t]` (RED).

**Why:** Status effects are **fast scannable**. Player sees RED icon = BAD, GREEN icon = GOOD, no reading required.

**Effort:** LOW — just prepend emoji to effect names in ShowCombatStatus.

### 7.5 Combat Summary Screen at Victory ⭐ (NICE-TO-HAVE)

**What:** After combat, show:
```
═══ VICTORY ═══
Turns: 8
Damage Dealt: 180 (22 avg/turn)
Damage Taken: 45
Crits: 3 (37% of attacks)
Status Effects Applied: Poison (24 total dmg)
```

**Why:** Provides **tactical feedback**. Players learn which strategies work. Builds feel more distinct.

**Effort:** MEDIUM — requires tracking combat stats in CombatEngine (some already exist in RunStats).

---

## 8. Technical Gaps to Address

### 8.1 TUI Color Support Is Stubbed

**Problem:** ShowColoredMessage, ShowColoredCombatMessage, ShowColoredStat all strip colors in TUI.

**Root Cause:** TuiColorMapper exists with full ANSI → Terminal.Gui Attribute mappings, but is never called by TerminalGuiDisplayService.

**Fix:** Wire TuiColorMapper into ShowColoredMessage / ShowColoredCombatMessage.

**Effort:** LOW — 1-2 hours, already architected.

### 8.2 BuildColoredHpBar Computes But Doesn't Use Variable Characters

**Problem:** Line 1383-1388 calculate `barChar` (█ for green, ▓ for yellow, ▒ for red), but line 1390 uses hardcoded `'█'` regardless.

**Root Cause:** Dead code from incomplete implementation (#1041).

**Fix:** Use the computed `barChar` instead of hardcoded `'█'`.

**Effort:** TRIVIAL — 1-line fix.

### 8.3 Message Log Not Scrollable Mid-Combat

**Problem:** MessageLogPanel is read-only during combat menu. Players can't PgUp to review history.

**Fix:** Bind PgUp/PgDn keys in TuiMenuDialog to scroll MessageLogPanel.

**Effort:** MEDIUM — requires key event handling in TuiMenuDialog.

### 8.4 ShowSkillTreeMenu Is a Stub

**Problem:** TUI ShowSkillTreeMenu returns `null` unconditionally. Skill tree not accessible in TUI mode.

**Impact:** Players in TUI mode can't learn new skills, breaking progression.

**Fix:** Implement TuiMenuDialog-based skill tree menu (same pattern as ShowAbilityMenuAndSelect).

**Effort:** MEDIUM — 2-3 hours.

---

## 9. Recommendations for Anthony

### Option A: Incremental Improvements to TUI (LOW RISK)

**Goal:** Fix the 4 critical pain points without rearchitecting.

**Scope:**
1. Wire TuiColorMapper into color methods (HP urgency, damage types)
2. Add emoji prefixes to damage/status messages
3. Implement ShowSkillTreeMenu
4. Add loot comparison deltas to ShowLootDrop

**Effort:** 1-2 days  
**Impact:** Addresses 90% of game-feel issues  
**Risk:** VERY LOW (additive changes only)

### Option B: Replace TUI with Better Framework (HIGH RISK)

**Candidates:**
- **Spectre.Console Live Display** — Use `Live<T>` to render persistent panels (similar to TUI)
- **Terminal.Gui v2** — Already implemented, just needs color wiring
- **Textual (Python)** — Would require rewriting entire game in Python (NOT FEASIBLE)
- **Blessed (Node.js)** — Would require rewriting entire game in JS (NOT FEASIBLE)

**Recommendation:** **DO NOT replace Terminal.Gui.** The dual-thread architecture is clean, the layout works, and all 19 input methods are implemented. The issues are **polish gaps**, not architecture problems.

### Option C: Hybrid — TUI for Layout, Spectre for Rendering (EXPERIMENTAL)

**Idea:** Keep Terminal.Gui's split-screen layout, but render rich Spectre widgets (tables, panels, progress bars) inside each TextView.

**Problem:** Spectre.Console and Terminal.Gui both own the terminal. They conflict.

**Verdict:** NOT FEASIBLE without custom Spectre `IAnsiConsole` backend that writes to TextView instead of stdout.

---

## 10. Final Verdict

**The TUI implementation is 85% complete.** The architecture is sound. The missing 15% is **polish**:

1. **Color urgency** (HP/MP bars, damage types)
2. **Loot comparison** at drop time
3. **Status effect prominence** (icons, colors)
4. **ShowSkillTreeMenu** stub implementation

**Recommendation:** Fix the 4 critical gaps in **Option A** (1-2 days effort), then reassess. Do NOT replace the TUI framework — the bones are good, it just needs skin.

---

## Appendix A: Comparison Matrix — Spectre vs TUI

| Feature | Spectre.Console | Terminal.Gui TUI |
|---------|----------------|------------------|
| **Persistent layout** | ❌ (scroll-based) | ✅ Split-screen |
| **Color support** | ✅ Full markup | ⚠️ Stubbed (TuiColorMapper exists) |
| **HP/MP urgency** | ✅ Color-coded | ❌ Plain text bars |
| **Damage type icons** | ⚠️ ANSI colors only | ❌ All stripped |
| **Loot comparison** | ✅ ShowEquipmentComparison | ❌ Not called in ShowLootDrop |
| **Status effect icons** | ⚠️ No emojis | ❌ Plain text |
| **Map visibility** | ❌ Type `MAP` to see | ✅ Always visible |
| **Stats visibility** | ❌ Type `STATS` to see | ✅ Always visible |
| **Message log scroll** | ✅ Natural terminal scroll | ❌ No PgUp/PgDn |
| **Skill tree menu** | ✅ Full implementation | ❌ Stub (returns null) |

**Verdict:** TUI wins on **layout** (persistent panels = huge UX win). Spectre wins on **rendering polish** (colors, icons). The ideal solution is **TUI layout + Spectre polish**, which is achievable via Option A.

---

## Appendix B: UI Frameworks Considered (For Future Reference)

| Framework | Language | Layout Model | Pros | Cons |
|-----------|----------|--------------|------|------|
| **Terminal.Gui v2** | C# | Widget tree | ✅ Native C# | ⚠️ Color wiring needed |
| **Spectre.Console** | C# | Sequential | ✅ Rich rendering | ❌ No persistent layout |
| **Textual** | Python | Widget tree | ✅ Beautiful | ❌ Requires Python rewrite |
| **Blessed** | Node.js | Widget tree | ✅ Mature | ❌ Requires JS rewrite |
| **ncurses** | C | Low-level | ✅ Fast | ❌ Requires C# interop |
| **Console.ReadKey loop** | C# | Custom | ✅ Full control | ❌ Reinventing wheel |

**Recommendation:** Stick with **Terminal.Gui v2**. It's the only option that doesn't require a language rewrite and has built-in layout management.

