# Systems UX Findings — Player Feedback Analysis
**Author:** Barton (Systems Dev)  
**Date:** 2026-02-20  
**Context:** Boss requested UX analysis from systems perspective. Current display is single-color text; need to identify where color/formatting would improve player feedback in combat, status, and progression systems.

---

## Executive Summary

The game has solid mechanical depth (status effects, abilities, equipment, crafting, boss phases) but **player visibility of this complexity is minimal**. All combat and status information is plain white text with no visual hierarchy. Players cannot quickly parse critical information during combat, track active effects, or understand what's happening mechanically.

**Critical UX Gaps:**
1. **Status effects are invisible** — player/enemy effects exist but are not displayed during combat
2. **Combat damage lacks context** — no indication of crits, dodges, modifiers, or damage types
3. **Health/mana status buried** — only shown when explicitly requesting stats or during specific prompts
4. **No danger signals** — boss enrage, telegraphed attacks, hazards look identical to normal text
5. **Equipment changes invisible** — stat changes happen but player can't see the impact
6. **Ability feedback minimal** — cooldowns/costs shown in menu but not current battle state

---

## 1. Combat Display Analysis

### Current State
- **Turn structure:** Player sees `[A]ttack [B]ability [F]lee` menu with mana count if abilities unlocked
- **Hit/miss feedback:** Single-line text messages (e.g., "You strike Goblin for 8 damage!")
- **Combat status:** One-line format: `[You: 45/50 HP] vs [Goblin: 12/20 HP]`
- **Turn log:** Last 3 actions displayed before menu (good idea, but format needs work)

### What's Clear
✅ Basic damage dealt and HP remaining  
✅ When abilities are available (mana shown)  
✅ Recent combat history (turn log)

### What's Confusing
❌ **No visual distinction between normal hits and crits** — both look identical despite 2x damage  
❌ **Dodge mechanics unclear** — player sees "Goblin dodges!" but doesn't know *why* (defense-based formula)  
❌ **Status effect modifiers hidden** — Fortified gives +50% DEF but player never sees "28 → 42 DEF"  
❌ **Boss mechanics buried** — enrage, charge telegraph, phase transitions are plain text in message flood  
❌ **Enemy special abilities invisible** — Vampire lifesteal, Wraith dodge chance, Shaman heals look like normal attacks  

### Color/Format Opportunities (Combat)

| Element | Current | Improvement | Impact |
|---------|---------|-------------|--------|
| **Critical hits** | "You strike for 16 damage!" | 💥 `[CRIT]` or red damage number | **HIGH** — crits feel impactful |
| **Dodge/miss** | "Goblin dodges your attack!" | Gray text or ↗️ arrow symbol | **MEDIUM** — clarity on miss reason |
| **Player damage taken** | "Goblin strikes you for 8 damage!" | Yellow/orange text for incoming | **HIGH** — danger visibility |
| **Boss charge telegraph** | "Boss is charging an attack!" | ⚠️ `[WARNING]` red/bold | **CRITICAL** — life-saving signal |
| **Status damage ticks** | "You take 3 poison damage!" | Green text with 🧪 symbol | **MEDIUM** — effect visibility |
| **Healing** | "You heal 20 HP!" | Bright green text with + | **MEDIUM** — positive reinforcement |
| **Enemy death** | "Goblin is defeated!" | Gray strikethrough or skull | **MEDIUM** — combat closure clarity |

---

## 2. Player Feedback on Status/Effects

### Current State
- **Status effects exist:** Poison, Bleed, Stun, Regen, Fortified, Weakened (6 total)
- **Effect application:** Text messages like "Poison Dart! Goblin is poisoned!"
- **Effect ticks:** Messages per turn: "You take 3 poison damage!" (DOT) or "You regenerate 4 HP!" (HOT)
- **Effect expiry:** "Your Fortified effect has worn off."
- **Stat modifiers:** `GetStatModifier()` exists but **NEVER DISPLAYED** (Bug #1 from pre-v3 hunt)

### Critical Gap: **Active Effects Display**
- **StatusEffectManager has `GetActiveEffects(target)` method** but it's **never called for display**
- History notes: "DisplayActiveEffects feedback should be added to combat loop" (never implemented)
- **Player cannot see:**
  - What effects are currently on them or the enemy
  - How many turns remain on each effect
  - What stat modifiers are active (Fortified +50% DEF, Weakened -50% ATK)

### Where Effects Should Be Shown
1. **Combat status bar** — next to HP/mana display:
   ```
   [You: 45/50 HP] 🧪 Poisoned(2) 🛡️ Fortified(1)
   vs
   [Goblin: 12/20 HP] ⚔️ Weakened(3)
   ```

2. **Stats command** — active effects section showing modifiers:
   ```
   Active Effects:
     • Fortified (1 turn) — Defense +50%
     • Poison (2 turns) — 3 damage/turn
   ```

3. **Effect application/removal** — already has text messages (good)

### Color/Format Opportunities (Status)

| Effect Type | Symbol | Color | When to Show |
|-------------|--------|-------|--------------|
| Poison | 🧪 | Green | Every turn during combat status |
| Bleed | 🩸 | Red | Combat status + damage ticks |
| Stun | 💫 | Yellow | Combat status + "cannot act" message |
| Regen | ❤️ | Bright green | Combat status + heal ticks |
| Fortified | 🛡️ | Blue | Combat status + DEF value |
| Weakened | ⚔️ (broken) | Gray | Combat status + ATK value |

---

## 3. Player Status Visibility

### Current State
- **Stats command** — shows full player stats (HP, Mana, Attack, Defense, Gold, XP, Level, Class Trait)
- **Equipment command** — shows equipped weapon/armor/accessory with bonuses
- **Inventory command** — lists items with type annotations
- **Combat status** — HP and mana shown during fight menu

### What's Missing
❌ **No persistent status display** — player must type STATS every time to see health outside combat  
❌ **No quick HP/mana check** — no shorthand command for "how much HP do I have?"  
❌ **XP progress invisible** — player sees "XP: 85" but not "85/100 to Level 4"  
❌ **Equipment stat totals unclear** — player sees "+5 ATK weapon" but final Attack value only in STATS  
❌ **Gold value feedback weak** — picks up gold but can't easily see total without STATS  
❌ **Ability cooldowns not visible** — must enter combat and press [B]ability to see cooldown state  

### Color/Format Opportunities (Status)

| Element | Current | Improvement | Impact |
|---------|---------|-------------|--------|
| **Low HP warning** | No indication | Red HP text when < 30% | **HIGH** — survival awareness |
| **XP to next level** | "XP: 85" | "XP: 85/100 (15 to level 4)" | **MEDIUM** — progression clarity |
| **Stat increases** | No feedback | "+2 ATK!" after equip/level | **HIGH** — reward visibility |
| **Mana regeneration** | Silent | "+10 mana" at turn start | **LOW** — resource tracking |
| **Gold pickup** | "You picked up 12 gold" | "Gold: 45 → 57 (+12)" | **MEDIUM** — wealth tracking |
| **Full heal on levelup** | Silent | "✨ HP/Mana fully restored!" | **MEDIUM** — milestone moment |

---

## 4. Combat UX Opportunities by System

### Damage Numbers
**Current:** "You strike Goblin for 8 damage!"  
**Opportunity:** Show damage *breakdown* on crits or complex hits:
```
💥 CRITICAL HIT! 16 damage (8 base × 2 crit)
Your attack: 25 vs Defense: 10 = 15 base → 16 crit
```
**Impact:** **MEDIUM** — helps players understand stat math, but could be verbose

### Boss Mechanics
**Current:** All text, no visual priority  
**Opportunity:**
- Enrage: `⚠️ [ENRAGED] Attack 22 → 33 (+50%)`
- Charge telegraph: `⚡ [CHARGING] Next attack deals 3× damage!`
- Phase transition: Boss ASCII art or separator line

**Impact:** **CRITICAL** — boss fights are climax moments, must feel dramatic

### Enemy Special Abilities
**Current:** "Vampire Lord attacks you for 12 damage and heals 6 HP!"  
**Opportunity:** Color-code by mechanic type:
- Lifesteal: Red text for damage, green for heal
- Ambush: ⚡ symbol + yellow text
- Self-heal: Green + ❤️
- Status application: Effect symbol + color

**Impact:** **HIGH** — makes enemy variety *visible*

### Turn Log
**Current:** Last 3 actions, plain text  
**Opportunity:** Icon-prefix each log entry:
```
⚔️ You hit Goblin for 8 damage
🛡️ Goblin attacks but you dodge
🧪 Goblin takes 3 poison damage
```
**Impact:** **MEDIUM** — easier to scan history

---

## 5. Systems That Need Color Coding

### By Priority

#### CRITICAL (P0) — Core Combat Visibility
1. **Status effects display** — show active effects on player/enemy during combat status
2. **Boss mechanics** — enrage, charge, phase transitions need RED/BOLD
3. **Player damage taken** — incoming hits need distinct color from outgoing
4. **Low HP warning** — red text when HP < 30%

#### HIGH (P1) — Combat Clarity
5. **Critical hits** — 💥 symbol or red/bold damage numbers
6. **Stat modifiers** — show ATK/DEF changes from Fortified/Weakened
7. **Enemy special abilities** — lifesteal, dodge, heal need visual distinction
8. **Ability cooldown/mana** — gray out unavailable abilities in menu

#### MEDIUM (P2) — Feedback & Polish
9. **XP progress** — show "X/Y to next level"
10. **Gold changes** — show running total on pickup
11. **Healing** — green text for all heal sources
12. **Equipment stat changes** — "+5 ATK!" when equipping weapon
13. **Dodge/miss** — gray text or symbol

#### LOW (P3) — Nice-to-Have
14. **Turn log icons** — prefix each action with symbol
15. **Room type colors** — scorched = red, flooded = blue, etc.
16. **Item rarity** — if legendary items exist, color-code them
17. **Mana regen feedback** — "+10 mana" at turn start

---

## 6. Information That's Hard to Find Right Now

### During Combat
❌ "Am I poisoned right now?" — must read back through messages  
❌ "How many turns until Second Wind is off cooldown?" — must press [B]ability to check  
❌ "Is the boss enraged?" — must read back through messages  
❌ "What's my current defense after Fortified?" — stat modifiers never shown  

### During Exploration
❌ "What's my current HP?" — must type STATS  
❌ "How much XP until I level?" — must type STATS and do mental math  
❌ "What equipment am I wearing?" — must type EQUIPMENT  
❌ "What abilities do I have unlocked?" — must enter combat or type SKILLS  

### After Actions
❌ "Did my attack crit?" — damage number looks identical  
❌ "Did equipping this armor help?" — no before/after stat display  
❌ "Did I level up?" — text message exists but no fanfare  
❌ "How much damage did I avoid by dodging?" — never shown  

---

## 7. What Would Make Combat More Satisfying to Watch

### Moment-to-Moment Feedback
1. **Hit impact** — crits should *feel* different (💥 symbol, color, maybe "!" or larger text)
2. **Survivability** — show how close to death (HP bar color, percentage)
3. **Momentum** — consecutive hits or "on fire" mechanics (not implemented, but would feel good)
4. **Risk/reward** — telegraphed boss attacks create tension *if visually distinct*

### Progression Milestones
5. **Level-up celebration** — full heal is great, but needs visual fanfare (ASCII banner, bold text)
6. **Ability unlock** — "You've learned [Poison Dart]!" at L5 should be a Big Deal
7. **First crit** — tutorial moment: "💥 Critical hit! Your high attack gave a 15% chance to double damage!"
8. **Boss phases** — Phase 2 enrage should feel like the fight changed (separator line, ASCII art)

### Strategic Information
9. **Status effect counterplay** — "Goblin is Poisoned — deals 3 dmg/turn for 3 turns"
10. **Enemy danger level** — boss HP bar, elite enemy markers, threat indicators
11. **Resource tracking** — mana/cooldown visibility so player can *plan* ability usage
12. **Stat math transparency** — occasional damage breakdown to teach formulas

---

## 8. Recommendations for Coulson's Master Plan

### Phase 1: Core Visibility (Must-Have)
- **Display active effects** in combat status bar (player + enemy)
- **Color-code damage types:** red for incoming, white for outgoing, green for healing
- **Boss mechanic warnings:** red/bold for enrage and charge telegraph
- **Low HP indicator:** red text when player HP < 30%

### Phase 2: Combat Clarity (High-Value)
- **Critical hit markers:** 💥 symbol or distinct color
- **Stat modifier display:** show ATK/DEF changes from buffs/debuffs
- **Ability cooldown visibility:** gray out unavailable abilities in [B]ability menu
- **XP progress bar:** "85/100 XP to Level 4" in STATS command

### Phase 3: Polish & Delight (Nice-to-Have)
- **Turn log icons:** prefix actions with ⚔️ 🛡️ 🧪 symbols
- **Level-up fanfare:** ASCII banner or separator line
- **Equipment feedback:** "+5 ATK!" when equipping weapon
- **Gold running total:** "Gold: 45 → 57 (+12)" on pickup

### Non-Combat Improvements
- **Persistent status bar?** — some roguelikes show HP/Mana at top of screen always
- **Quick status command:** alias "S" for STATS (faster than typing full word)
- **EXAMINE improvements:** show more detail on enemies (abilities, resistances, threat level)

---

## Architecture Notes for Implementation

### Display Service Changes
- Current `IDisplayService` has no color/formatting support — all plain text
- Need to add:
  - `ShowColoredMessage(string message, ConsoleColor color)`
  - `ShowCombatMessageWithEmoji(string emoji, string message, ConsoleColor? color = null)`
  - `ShowStatusBar(Player player, Enemy enemy)` — enhanced version with active effects

### StatusEffectManager Integration
- `GetActiveEffects(target)` already exists but never used for display
- Add utility method: `FormatEffectsForDisplay(object target) → string`
- Call during combat status display (CombatEngine line ~267)

### CombatEngine Changes
- Damage calculation points need color logic:
  - Crit detection (line ~366) → red/bold
  - Player damage (line ~248) → white
  - Enemy damage (line ~294) → yellow/orange
- Boss mechanics (enrage, charge) → red/bold
- Status tick processing (line ~228) → color by effect type

### Existing Display Patterns
- `ShowCombatMessage(string)` is primary output
- `ShowCombatStatus(Player, Enemy)` is one-line HP display
- Combat messages use emoji already (⚔ ⚠ ✦ 💥) — good foundation
- Turn log exists (last 3 actions) — just needs formatting

---

## Technical Considerations

### Console Color Limitations
- Standard Console.ForegroundColor has 16 colors (8 + bright variants)
- Not all terminals support full RGB (Windows CMD, Linux terminal varies)
- **Recommendation:** Stick to basic colors (Red, Green, Yellow, Blue, White, Gray) + Bold/Dim
- **Fallback:** All features should degrade gracefully to plain text

### Performance
- Color changes via `Console.ForegroundColor` are fast (no concern)
- Emoji may have issues on some terminals (Windows CMD especially)
- **Recommendation:** Make emoji toggleable (config flag: `USE_EMOJI = true`)

### Accessibility
- Color-blind players may not see red/green distinction
- **Recommendation:** Always pair color with symbols (🧪, ⚔️, 🛡️) or text tags `[CRIT]`
- **Recommendation:** High-contrast mode option (yellow/blue instead of red/green)

---

## Closing Thoughts

The game's *mechanical depth is invisible*. Players have status effects, abilities, equipment, boss phases, and enemy variety — but it all looks the same on screen. Small visual improvements (color, symbols, formatting) would massively increase:

1. **Player skill ceiling** — seeing cooldowns/effects enables strategic planning
2. **Combat satisfaction** — crits and big moments need to *feel* big
3. **Accessibility** — new players currently must read walls of text to parse combat
4. **Perceived polish** — color-coded UI feels more "finished" than plain text

**Bottom line:** The Systems layer has done its job (rich combat mechanics exist). The Display layer needs to catch up so players can *see* what's happening.

---

**Next Steps:**
1. Coulson incorporates this into master UX plan
2. Display service gets color/formatting methods
3. CombatEngine integrates active effect display
4. Incremental rollout: P0 → P1 → P2 → P3
