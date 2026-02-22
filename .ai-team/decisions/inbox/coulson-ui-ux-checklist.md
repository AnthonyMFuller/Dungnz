# UI/UX Implementation Checklist

**Quick reference for team during implementation**

---

## Phase 1: Foundation ✓ READY TO START

### WI-1: Color Utility Class
**Owner:** Hill  
**File:** `Systems/ColorCodes.cs`

- [ ] Create ColorCodes static class
- [ ] Add ANSI color constants (Red, Green, Yellow, Blue, Cyan, BrightRed, Gray, Bold, Reset)
- [ ] Add `Colorize(string text, string color)` helper
- [ ] Add `HealthColor(int current, int max)` with thresholds (>70% green, 40-70% yellow, 20-40% red, <20% bright red)
- [ ] Add `ManaColor(int current, int max)` with thresholds (>50% blue, 20-50% cyan, <20% gray)
- [ ] Add XML docs on all public members

**Test:** Create `ColorCodesTests.cs` with threshold boundary tests

---

### WI-2: DisplayService Color Methods
**Owner:** Hill  
**Files:** `Display/IDisplayService.cs`, `Display/ConsoleDisplayService.cs`, `Dungnz.Tests/Helpers/TestDisplayService.cs`

- [ ] Add `void ShowColoredMessage(string message, string color)` to IDisplayService
- [ ] Add `void ShowColoredCombatMessage(string message, string color)` to IDisplayService
- [ ] Add `void ShowColoredStat(string label, string value, string valueColor)` to IDisplayService
- [ ] Implement methods in ConsoleDisplayService
- [ ] Update TestDisplayService to strip ANSI codes (regex: `\u001b\[[0-9;]*m`)
- [ ] Add XML docs on new interface methods

**Test:** Verify all 267 existing tests still pass

---

### WI-3: Core Stat Colorization
**Owner:** Hill  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowPlayerStats()` to colorize:
  - HP value with `ColorCodes.HealthColor(player.HP, player.MaxHP)`
  - Mana value with `ColorCodes.ManaColor(player.Mana, player.MaxMana)`
  - Gold value with `ColorCodes.Yellow`
  - XP value with `ColorCodes.Green`
  - Attack value with `ColorCodes.BrightRed`
  - Defense value with `ColorCodes.Cyan`
- [ ] Update `ShowCombatStatus()` to colorize HP for both player and enemy
- [ ] Update combat damage messages in `Engine/CombatEngine.cs` to highlight damage values in red

**Test:** Manual verification + screenshot comparison

---

## Phase 2: Enhancement ⏸ BLOCKED BY PHASE 1

### WI-4: Combat Visual Hierarchy
**Owner:** Barton  
**File:** `Engine/CombatEngine.cs`

- [ ] Color damage numbers bright red in all attack messages
- [ ] Color healing numbers bright green in all heal messages
- [ ] Color critical hit messages bright yellow + bold
- [ ] Color ability names blue in usage messages
- [ ] Add colored status effect confirmation messages (poison=red, regen=green, stun=yellow)

---

### WI-5: Enhanced Combat Status HUD
**Owner:** Barton  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowCombatStatus()` format to: `[You: HP | MP | Effects] vs [Enemy: HP | Effects]`
- [ ] Add effect abbreviation logic: P(poison), R(regen), S(stun), B(bleed), F(fortified), W(weakened)
- [ ] Color abbreviations by type (positive=green, negative=red, neutral=yellow)
- [ ] Display turns remaining in parentheses

**Test:** Integration test with multiple active effects

---

### WI-6: Equipment Comparison Display
**Owner:** Hill  
**Files:** `Display/IDisplayService.cs`, `Display/ConsoleDisplayService.cs`, `Systems/EquipmentManager.cs`

- [ ] Add `void ShowEquipmentComparison(Item? oldItem, Item newItem)` to IDisplayService
- [ ] Implement method in ConsoleDisplayService with box border
- [ ] Show before/after stats with colored deltas (+X green, -X red, no change gray)
- [ ] Call from EquipmentManager.EquipItem() before applying changes
- [ ] Prompt user to confirm (optional enhancement)

---

### WI-7: Inventory Weight Display
**Owner:** Hill  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowInventory()` to add header line: `Slots: X/Y | Weight: X/Y | Value: Xg`
- [ ] Color weight ratio by threshold (<80% green, 80-95% yellow, >95% red)
- [ ] Add `[X wt]` suffix to each item line
- [ ] Add `[Xg]` value suffix to each item line

---

### WI-8: Status Effect Summary Panel
**Owner:** Barton  
**Files:** `Display/IDisplayService.cs`, `Display/ConsoleDisplayService.cs`

- [ ] Add `void ShowActiveEffects(Player player)` to IDisplayService
- [ ] Implement method showing effect name, turns remaining, and per-turn effect
- [ ] Color effect names by type
- [ ] Call from `ShowPlayerStats()` after main stats block

---

## Phase 3: Polish ⏸ BLOCKED BY PHASE 2

### WI-9: Achievement Progress Tracking
**Owner:** Barton  
**Files:** `Display/IDisplayService.cs`, `Systems/AchievementSystem.cs`

- [ ] Add `void ShowAchievementProgress(List<Achievement> achievements, RunStats stats)` to IDisplayService
- [ ] Implement showing locked achievements with progress percentage
- [ ] Color progress by threshold (>75% green, 50-75% yellow, <50% red)
- [ ] Add progress calculation methods to AchievementSystem

---

### WI-10: Enhanced Room Descriptions
**Owner:** Hill  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowRoom()` to color room type prefix:
  - Safe types (standard, mossy, ancient) → cyan/green
  - Hazardous types (dark, flooded, scorched) → yellow/red
- [ ] Color enemy warning bright red + bold
- [ ] Color item names yellow

---

### WI-11: Ability Cooldown Visual
**Owner:** Barton  
**File:** `Engine/CombatEngine.cs` (ability menu display)

- [ ] Color ready abilities (cooldown=0, mana sufficient) green + bold
- [ ] Color cooling abilities (cooldown>0) gray
- [ ] Color insufficient mana abilities red
- [ ] Change text: "ready" instead of "CD: 0 turns"

---

### WI-12: Combat Turn Log Enhancement
**Owner:** Barton  
**File:** `Engine/CombatEngine.cs`

- [ ] Limit turn log to last 5 turns (add ring buffer or list truncation)
- [ ] Color player actions green
- [ ] Color enemy actions red
- [ ] Add turn numbers to each line
- [ ] Consider indentation for visual rhythm

---

## Testing Checklist (Romanoff)

### After Phase 1
- [ ] All 267 tests pass
- [ ] TestDisplayService strips ANSI codes correctly
- [ ] Manual test: Player stats show colored values
- [ ] Manual test: Combat HP bars change color at thresholds
- [ ] Manual test: Color codes work on Windows Terminal, macOS Terminal, Linux terminal

### After Phase 2
- [ ] All tests still pass
- [ ] Manual test: Combat HUD shows active effects
- [ ] Manual test: Equipment comparison displays correctly
- [ ] Manual test: Inventory shows weight/value summary
- [ ] Manual test: Status effect panel visible in stats

### After Phase 3
- [ ] All tests still pass
- [ ] Manual test: Achievement progress displays
- [ ] Manual test: Room descriptions use danger colors
- [ ] Manual test: Ability menu shows cooldown colors
- [ ] Manual test: Turn log limited to 5 entries
- [ ] Regression check: All gameplay systems work as before
- [ ] Performance check: No noticeable slowdown

---

## Code Review Gates (Coulson)

### After Phase 1
- [ ] ColorCodes follows C# conventions (PascalCase constants)
- [ ] IDisplayService methods have XML docs
- [ ] TestDisplayService ANSI stripping is robust
- [ ] No ANSI codes leak into game logic (CombatEngine, GameLoop, Systems)

### After Phase 2
- [ ] Equipment comparison doesn't block gameplay (optional prompt)
- [ ] Combat HUD doesn't cause line wrapping on 80-column terminals
- [ ] Status effect abbreviations are intuitive (consider legend display)
- [ ] Inventory weight display aligns with existing patterns

### After Phase 3
- [ ] Achievement progress calculations are accurate
- [ ] Room color coding enhances (doesn't replace) existing emoji
- [ ] Turn log truncation preserves recent context
- [ ] Overall UX feels polished and consistent

---

## Merge Criteria (Final Gate)

- [ ] All 267 tests pass
- [ ] Zero regressions in gameplay
- [ ] Visual clarity improved (Boss/team approval via screenshots)
- [ ] Accessibility maintained (color-blind testing)
- [ ] Performance acceptable (no slowdown)
- [ ] Code review approved (Coulson)
- [ ] README.md updated (if commands/UI changed)

**When all criteria met:** Merge to master branch
