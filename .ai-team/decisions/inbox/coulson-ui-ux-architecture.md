# UI/UX Improvement Plan — TextGame v3.5

**Date:** 2026-02-20  
**Lead:** Coulson  
**Context:** Boss-requested initiative to enhance visual clarity and player experience through color, layout, and feedback improvements.

---

## Executive Summary

TextGame currently uses plain text output with Unicode box-drawing characters and emoji for visual distinction. **No color system exists.** The codebase has clean architectural separation (IDisplayService abstraction) that enables UI improvements without touching game logic.

**Proposal:** Implement ANSI color system, enhance visual hierarchy, improve player feedback, and add real-time status tracking—all via DisplayService extensions. No breaking changes to architecture.

**Estimated Scope:** 15-20 hours across 3 phases (Foundation → Enhancement → Polish)

---

## Current State Analysis

### Architecture Strengths
✅ **IDisplayService abstraction** — All display calls routed through interface  
✅ **Clean separation** — Game logic never calls Console directly  
✅ **Single implementation** — ConsoleDisplayService is sole concrete class  
✅ **Test infrastructure** — TestDisplayService exists for headless testing  
✅ **Consistent patterns** — Emoji prefixes, indentation, box-drawing established  

### Current Display Features
- **Text formatting:** Unicode box-drawing (`╔ ║ ═ ╚`), emoji (⚔ 🏛 💧 ✗), indentation
- **Layout patterns:** Blank lines for spacing, bracketed comparisons `[You: X/Y]`, comma-separated lists
- **Visual hierarchy:** Headers with `═══`, indented messages (2 spaces), emoji prefixes for categories

### Critical Gaps
❌ **No color system** — All text plain white  
❌ **No status HUD** — Active effects only shown when applied/expired  
❌ **No equipment comparison** — Equipping gear doesn't show stat delta  
❌ **No progress tracking** — Achievements/unlocks binary only  
❌ **No inventory weight display** — Weight system exists but not visualized  
❌ **Limited combat clarity** — Damage/heals blend into narrative walls  

---

## Design Philosophy

### Core Principles
1. **Console-native aesthetics** — Leverage ANSI colors, box-drawing, and emoji (no external frameworks)
2. **Accessibility first** — Color must enhance, not replace, existing semantic indicators (emoji, labels)
3. **Information density** — Reduce clutter; prioritize actionable info over flavor text
4. **Consistency** — Establish color palette and apply uniformly across all systems
5. **No breaking changes** — All improvements via DisplayService extensions; game logic untouched

### Color Philosophy
- **Color as semantic layer** — HP=red, Mana=blue, gold=yellow, XP=green, errors=red
- **State-based coloring** — Low HP warnings, cooldown readiness, effect durations
- **Context-aware intensity** — Combat uses bold/bright colors; exploration uses muted tones
- **Graceful degradation** — If ANSI unsupported, fall back to current emoji-only design

---

## Proposed Color System

### Color Palette (ANSI Codes)

| Category | Color | ANSI Code | Use Cases |
|----------|-------|-----------|-----------|
| **Health** | Red | `\u001b[31m` | HP values, damage messages |
| **Mana** | Blue | `\u001b[34m` | Mana values, ability costs |
| **Gold** | Yellow | `\u001b[33m` | Gold amounts, loot rewards |
| **XP** | Green | `\u001b[32m` | XP rewards, level-ups |
| **Attack** | Bright Red | `\u001b[91m` | Attack stat, power buffs |
| **Defense** | Cyan | `\u001b[36m` | Defense stat, shields |
| **Success** | Green | `\u001b[32m` | Confirmations, heals |
| **Errors** | Red | `\u001b[31m` | Warnings, failures |
| **Neutral** | White | `\u001b[37m` | Default text |
| **Dim** | Gray | `\u001b[90m` | Cooldowns, disabled options |
| **Highlight** | Bright White | `\u001b[97m` | Important values, headers |
| **Reset** | — | `\u001b[0m` | End colored segments |

### State-Based Colors

**HP Thresholds:**
- `100%-70%` → Green
- `69%-40%` → Yellow
- `39%-20%` → Red
- `19%-0%` → Bright Red (flashing if possible)

**Mana Thresholds:**
- `100%-50%` → Blue
- `49%-20%` → Cyan
- `19%-0%` → Gray (depleted)

**Status Effects:**
- Positive (Regen, Fortified) → Green text
- Negative (Poison, Weakened) → Red text
- Neutral (Stun, Bleed) → Yellow text

**Equipment Quality:**
- Common → White
- Uncommon → Green
- Rare → Blue
- Epic → Purple (`\u001b[35m`)
- Legendary → Gold (bright yellow `\u001b[93m`)

---

## Improvement Roadmap

### Phase 1: Foundation (Color System Core)
**Priority:** HIGH  
**Estimated Time:** 5-7 hours  
**Dependencies:** None

#### Work Items

**WI-1: Color Utility Class**
- Create `Systems/ColorCodes.cs` with ANSI code constants
- Add `Colorize(string text, ColorCode color)` helper
- Add `HealthColor(int current, int max)` threshold logic
- Add `ManaColor(int current, int max)` threshold logic

**WI-2: DisplayService Color Methods**
- Add `ShowColoredMessage(string message, ColorCode color)` to IDisplayService
- Add `ShowColoredCombatMessage(string message, ColorCode color)`
- Add `ShowColoredStat(string label, string value, ColorCode valueColor)`
- Update ConsoleDisplayService implementation
- Update TestDisplayService to strip ANSI codes for assertions

**WI-3: Core Stat Colorization**
- Update `ShowPlayerStats()` to colorize HP (red), Mana (blue), Gold (yellow), XP (green), Attack (bright red), Defense (cyan)
- Update `ShowCombatStatus()` to apply HP threshold colors to both player and enemy
- Update damage messages in CombatEngine to colorize damage values (red)

#### Acceptance Criteria
- [ ] ANSI color codes constants defined and documented
- [ ] DisplayService has 3 new color-aware methods
- [ ] Player stats display uses semantic colors
- [ ] Combat HP bars change color based on threshold
- [ ] All 125+ existing tests still pass (TestDisplayService strips colors)

---

### Phase 2: Enhancement (Visual Hierarchy & Feedback)
**Priority:** HIGH  
**Estimated Time:** 6-8 hours  
**Dependencies:** Phase 1 complete

#### Work Items

**WI-4: Combat Visual Hierarchy**
- Color damage numbers red with bright highlight
- Color healing green with bright highlight
- Color critical hits with `💥` emoji + bright yellow text
- Color ability names blue in usage messages
- Add colored status effect indicators: `[P]oison` (red), `[R]egen` (green), `[S]tun` (yellow)

**WI-5: Enhanced Combat Status HUD**
- Redesign `ShowCombatStatus()` to show active effects inline:
  ```
  [You: 45/60 HP | 15/30 MP | P(2) R(3)] vs [Goblin: 12/30 HP | W(2)]
  ```
- Color effect abbreviations based on type (positive/negative/neutral)
- Add mana display to HUD (currently only shown in ability menu)

**WI-6: Equipment Comparison Display**
- Add `ShowEquipmentComparison(Item old, Item new)` method to IDisplayService
- Display before/after stats when equipping:
  ```
  Equipping: Iron Sword
  ────────────────────────
  Before: Attack: 10, Defense: 5
  After:  Attack: 15, Defense: 5  [+5 ATK]
  ────────────────────────
  ```
- Color stat deltas: green for increases, red for decreases

**WI-7: Inventory Weight Display**
- Update `ShowInventory()` to include weight header:
  ```
  ═══ INVENTORY ═══
  Slots: 5/8 | Weight: 42/50 | Value: 320g
  ```
- Add weight value to each item line: `• Potion (Consumable) [2 wt]`
- Color weight ratio: green (under 80%), yellow (80-95%), red (96-100%)

**WI-8: Status Effect Summary Panel**
- Add `ShowActiveEffects(Player player)` to IDisplayService
- Display in `ShowPlayerStats()` below main stats:
  ```
  Active Effects:
    Poison (2 turns) - Taking 3 damage per turn
    Regen (3 turns) - Healing 4 HP per turn
  ```
- Color effect names based on type

#### Acceptance Criteria
- [ ] Combat damage/healing uses color highlights
- [ ] Combat HUD shows active effects inline with colored abbreviations
- [ ] Equipment comparison shows stat deltas when equipping
- [ ] Inventory displays weight/value summary with threshold colors
- [ ] Player stats shows active effects panel
- [ ] All tests pass

---

### Phase 3: Polish (Advanced UX Features)
**Priority:** MEDIUM  
**Estimated Time:** 4-5 hours  
**Dependencies:** Phase 2 complete

#### Work Items

**WI-9: Achievement Progress Tracking**
- Add `ShowAchievementProgress(List<Achievement> locked)` to IDisplayService
- On game end, show locked achievements with progress:
  ```
  ❌ Speed Runner: 142 turns (need <100) — 71% progress
  ❌ Hoarder: 320g / 500g — 64% progress
  ✅ Glass Cannon: UNLOCKED
  ```
- Color progress bars: green (>75%), yellow (50-75%), red (<50%)

**WI-10: Enhanced Room Descriptions**
- Color room type prefixes based on danger level:
  - Safe (standard/mossy/ancient) → green/cyan
  - Hazardous (dark/flooded/scorched) → yellow/red
- Color enemy warnings bright red with bold text
- Color item drops gold

**WI-11: Ability Cooldown Visual**
- Update ability menu to show cooldown status with color:
  ```
  [1] Power Strike (10 MP, ready) ← green
  [2] Defensive Stance (8 MP, 2 turns) ← gray
  ```
- Bold + bright color for ready abilities
- Dim gray for cooling down abilities

**WI-12: Combat Turn Log Enhancement**
- Limit turn log to last 5 turns (currently unbounded)
- Color player actions green, enemy actions red
- Indent alternating turns for visual rhythm
- Add turn numbers: `Turn 3: You strike Goblin for 15 damage`

#### Acceptance Criteria
- [ ] Achievement progress tracked and displayed on game end
- [ ] Room descriptions use danger-based color coding
- [ ] Ability menu shows cooldown readiness with color
- [ ] Combat log uses alternating colors for player/enemy actions
- [ ] All tests pass

---

## Technical Implementation Notes

### ANSI Color Utility (WI-1)

```csharp
namespace Dungnz.Systems;

/// <summary>
/// ANSI escape code constants and color formatting utilities for console output.
/// </summary>
public static class ColorCodes
{
    // Basic colors
    public const string Red = "\u001b[31m";
    public const string Green = "\u001b[32m";
    public const string Yellow = "\u001b[33m";
    public const string Blue = "\u001b[34m";
    public const string Magenta = "\u001b[35m";
    public const string Cyan = "\u001b[36m";
    public const string White = "\u001b[37m";
    
    // Bright colors
    public const string BrightRed = "\u001b[91m";
    public const string BrightGreen = "\u001b[92m";
    public const string BrightYellow = "\u001b[93m";
    public const string BrightWhite = "\u001b[97m";
    public const string Gray = "\u001b[90m";
    
    // Formatting
    public const string Bold = "\u001b[1m";
    public const string Reset = "\u001b[0m";
    
    /// <summary>
    /// Wraps text in ANSI color codes.
    /// </summary>
    public static string Colorize(string text, string color)
        => $"{color}{text}{Reset}";
    
    /// <summary>
    /// Returns threshold-based color for HP values.
    /// </summary>
    public static string HealthColor(int current, int max)
    {
        var ratio = (float)current / max;
        return ratio switch
        {
            >= 0.70f => Green,
            >= 0.40f => Yellow,
            >= 0.20f => Red,
            _ => BrightRed
        };
    }
    
    /// <summary>
    /// Returns threshold-based color for Mana values.
    /// </summary>
    public static string ManaColor(int current, int max)
    {
        var ratio = (float)current / max;
        return ratio switch
        {
            >= 0.50f => Blue,
            >= 0.20f => Cyan,
            _ => Gray
        };
    }
}
```

### Extended IDisplayService Methods (WI-2)

```csharp
/// <summary>
/// Displays a colored message to the player.
/// </summary>
void ShowColoredMessage(string message, string color);

/// <summary>
/// Displays a colored combat message with indentation.
/// </summary>
void ShowColoredCombatMessage(string message, string color);

/// <summary>
/// Displays a stat with colored value (e.g., "HP: 45/60" with 45 red).
/// </summary>
void ShowColoredStat(string label, string value, string valueColor);

/// <summary>
/// Displays equipment comparison when swapping gear.
/// </summary>
void ShowEquipmentComparison(Item? oldItem, Item newItem);

/// <summary>
/// Displays active status effects on player/enemy.
/// </summary>
void ShowActiveEffects(Player player);

/// <summary>
/// Displays achievement progress for locked achievements.
/// </summary>
void ShowAchievementProgress(List<Achievement> achievements);
```

### Combat Status HUD Example (WI-5)

**Current:**
```
[You: 45/60 HP] vs [Goblin: 12/30 HP]
```

**Proposed:**
```
[You: 45/60 HP | 15/30 MP | P(2) R(3)] vs [Goblin: 12/30 HP | W(2)]
```

With colors:
- HP values use threshold colors (green/yellow/red)
- Mana values use blue
- Effect abbreviations colored by type: P (poison, red), R (regen, green), W (weakened, yellow)
- Numbers in parentheses show turns remaining

### Equipment Comparison Example (WI-6)

**Before equipping Iron Sword:**
```
═══════════════════════════════
Equipping: Iron Sword
───────────────────────────────
Current Weapon: Rusty Dagger
  Attack: 10 → 15  (+5)  ← green
  Defense: 5 → 5   (—)   ← gray
═══════════════════════════════
Equip? [Y/N]
```

### Inventory Weight Display Example (WI-7)

**Current:**
```
═══ INVENTORY ═══
• Health Potion (Consumable)
• Iron Sword (Weapon)
```

**Proposed:**
```
═══ INVENTORY ═══
Slots: 5/8  |  Weight: 42/50  |  Value: 320g
────────────────────────────────────────────
• Health Potion (Consumable) [3 wt] [25g]
• Iron Sword (Weapon) [8 wt] [50g]
```

With colors:
- Weight ratio colored by threshold (green <80%, yellow 80-95%, red >95%)
- Gold values in yellow
- Item names colored by rarity

---

## Architecture Impact

### No Breaking Changes
- All improvements via DisplayService method additions
- Existing methods retain current behavior
- Game logic (CombatEngine, GameLoop, Systems) unchanged except display calls

### Testing Strategy
- Update TestDisplayService to strip ANSI codes before storing output
- Add `StripAnsiCodes(string text)` helper method
- All existing tests pass without modification (they check plain text content)
- Add new tests for color utility functions (`HealthColor`, `ManaColor`)

### Performance Considerations
- ANSI codes add ~10-20 bytes per colored segment (negligible)
- No performance impact on game logic (display is I/O-bound)
- Color utility calls are simple string concatenation (fast)

### Accessibility
- Color enhances existing semantic indicators (emoji, labels), never replaces
- `ShowError()` still prefixes with `✗` even when red
- Combat HUD still shows effect abbreviations even without color
- Equipment comparison shows deltas as text (`+5`) alongside color

---

## Priority Order & Dependencies

### Critical Path
1. **WI-1 (Color Utility)** → Foundation for all color work
2. **WI-2 (DisplayService Methods)** → Interface contracts for color display
3. **WI-3 (Core Stat Colorization)** → Immediate visual impact

### Parallel Work (After WI-3)
**Track A (Combat):**
- WI-4 (Combat Visual Hierarchy)
- WI-5 (Combat Status HUD)
- WI-12 (Turn Log Enhancement)

**Track B (Exploration):**
- WI-6 (Equipment Comparison)
- WI-7 (Inventory Weight Display)
- WI-10 (Room Descriptions)

**Track C (Meta):**
- WI-8 (Status Effect Panel)
- WI-9 (Achievement Progress)
- WI-11 (Ability Cooldown Visual)

### Dependency Graph
```
WI-1 (Color Utility)
  ↓
WI-2 (DisplayService Extensions)
  ↓
WI-3 (Core Stat Colors)
  ├─→ WI-4 (Combat Hierarchy)
  │    ↓
  │   WI-5 (Combat HUD)
  │    ↓
  │   WI-12 (Turn Log)
  │
  ├─→ WI-6 (Equipment Compare)
  │    ↓
  │   WI-7 (Inventory Weight)
  │    ↓
  │   WI-10 (Room Colors)
  │
  └─→ WI-8 (Status Panel)
       ↓
      WI-9 (Achievement Progress)
       ↓
      WI-11 (Ability Cooldown)
```

---

## Risk Assessment

### High Risk
❌ **ANSI code support variance** — Some terminals (older Windows CMD) may not support ANSI  
**Mitigation:** Add ANSI detection and graceful fallback to current emoji-only design

❌ **Test infrastructure breakage** — Color codes may break existing test assertions  
**Mitigation:** Update TestDisplayService to strip ANSI codes in Phase 1

### Medium Risk
⚠️ **Display method signature changes** — New methods require IDisplayService updates  
**Mitigation:** Add new methods (don't modify existing); backwards-compatible

⚠️ **Color readability** — Some color combinations may be hard to read on certain terminals  
**Mitigation:** Use high-contrast colors; test on multiple terminal emulators

### Low Risk
✅ **Performance impact** — ANSI codes are small strings; no measurable slowdown expected  
✅ **Architecture coupling** — DisplayService abstraction prevents leakage into game logic

---

## Success Metrics

### Phase 1 (Foundation)
- [ ] All stats in `ShowPlayerStats()` use semantic colors
- [ ] Combat HP bars change color based on health threshold
- [ ] Damage numbers highlighted in combat messages
- [ ] Zero test failures after color integration

### Phase 2 (Enhancement)
- [ ] Combat HUD shows active effects with colored abbreviations
- [ ] Equipment comparison displays before/after stats
- [ ] Inventory shows weight/value summary with threshold colors
- [ ] Status effect panel visible in player stats

### Phase 3 (Polish)
- [ ] Achievement progress tracked and displayed
- [ ] Room descriptions use danger-based coloring
- [ ] Ability cooldowns show readiness with color
- [ ] Combat turn log uses alternating player/enemy colors

### Overall Success
- **Visual clarity:** Player feedback improves (damage, healing, status changes immediately obvious)
- **Information density:** More data displayed without increasing clutter
- **Accessibility:** Color enhances existing indicators without replacing them
- **Stability:** All 267 tests pass; no regressions in game logic

---

## Team Allocation

**Hill (Lead Engineer):** 8-10 hours
- WI-1: Color Utility Class
- WI-2: DisplayService Extensions
- WI-3: Core Stat Colorization
- WI-6: Equipment Comparison
- WI-7: Inventory Weight Display

**Barton (Systems Engineer):** 7-9 hours
- WI-4: Combat Visual Hierarchy
- WI-5: Combat Status HUD
- WI-8: Status Effect Panel
- WI-11: Ability Cooldown Visual
- WI-12: Combat Turn Log Enhancement

**Romanoff (Tester):** 3-4 hours
- Update TestDisplayService ANSI stripping
- Verify all 267 tests pass across all phases
- Add color utility unit tests
- Manual testing on multiple terminal emulators

**Coulson (Architect):** 2-3 hours
- Design review before Phase 1 kickoff
- Code review after each phase
- Approval gate before Phase 3 (validate architecture decisions)

---

## Open Questions

1. **ANSI Detection:** Should we auto-detect terminal color support or require opt-in flag?
   - **Recommendation:** Auto-detect via `Environment.GetEnvironmentVariable("TERM")` and Windows version check
   
2. **Color Customization:** Should players be able to configure color theme?
   - **Recommendation:** Defer to v4; use hard-coded theme for v3.5

3. **Equipment Rarity Colors:** Should we add rarity system (common/rare/epic) now or later?
   - **Recommendation:** Add rarity enum + colors in Phase 2; populate rarities in Phase 3

4. **Combat Log Length:** Should turn log be limited to 5 turns or configurable?
   - **Recommendation:** Hard-code 5 turns; add config option in v4 if requested

5. **Status Effect Abbreviations:** What should abbreviation scheme be?
   - **Recommendation:** Single-letter where unambiguous (P=Poison, R=Regen, S=Stun, B=Bleed, F=Fortified, W=Weakened)

---

## Post-Implementation Review Criteria

After Phase 3 completion, evaluate:

- [ ] **Visual clarity improved?** — Can players instantly identify HP state, active effects, cooldowns?
- [ ] **Information density optimal?** — Is all actionable info visible without scrolling?
- [ ] **Accessibility maintained?** — Do color-blind players still have full experience via emoji/labels?
- [ ] **Zero regressions?** — All 267 tests pass; no gameplay bugs introduced?
- [ ] **Performance acceptable?** — No noticeable slowdown in display rendering?

If all criteria met: **Ship to master**  
If any criteria unmet: **Iterate or roll back**

---

## Future Considerations (v4+)

- **Animated effects** — ANSI cursor positioning for "live" HP bars
- **Color themes** — Multiple palettes (classic, solarized, high-contrast)
- **Advanced HUD** — Split-screen combat view with persistent stat panels
- **Sound effects** — Terminal bell for critical hits, level-ups (via `\a` escape)
- **Mouse support** — ANSI mouse tracking for menu selections

---

**Decision Authority:** Coulson (Lead)  
**Approval Status:** DRAFT — Awaiting team review and Boss approval  
**Next Steps:** Schedule design review ceremony with Hill, Barton, Romanoff

