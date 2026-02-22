# Intro Sequence Improvement Plan

**Date:** 2026-02-22  
**Lead:** Coulson  
**Approval:** Pending Anthony approval  
**Effort Estimate:** 6-8 hours (5h dev, 2h testing, 1h review)  

---

## Executive Summary

The current intro is functional but lacks atmosphere, clarity, and player investment. This plan provides:
- **Enhanced title screen** with ASCII art, tagline, and atmospheric lore (skippable)
- **Stat transparency** in class/difficulty selection so players understand tradeoffs
- **Improved flow** that builds investment (name first) and makes informed choices easy
- **Better UX for seed** (auto-generated, shown at end, CLI flag for power users)
- **Prestige celebration** that shows progression and bonuses

**Key principle:** Reduce friction for 95% of players (no seed prompts), empower 5% (CLI override, displayed seed for sharing).

---

## 1. Title Screen Improvements

### Current State
```
╔═══════════════════════════════════════╗
║         DUNGEON CRAWLER               ║
║      A Text-Based Adventure           ║
╚═══════════════════════════════════════╝
```

### Proposed Design

**Full-width ASCII art with mood:**
```
    ╔════════════════════════════════════════════════════════════════╗
    ║                                                                ║
    ║                    D U N G E O N   C R A W L E R              ║
    ║                                                                ║
    ║   ⚔️  ██╗  ██╗███████╗██████╗ ███████╗███╗   ██╗████████╗   ║
    ║       ██║  ██║██╔════╝██╔══██╗██╔════╝████╗  ██║╚══██╔══╝   ║
    ║       ███████║█████╗  ██████╔╝█████╗  ██╔██╗ ██║   ██║      ║
    ║       ██╔══██║██╔══╝  ██╔══██╗██╔══╝  ██║╚██╗██║   ██║      ║
    ║       ██║  ██║███████╗██║  ██║███████╗██║ ╚████║   ██║      ║
    ║       ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝   ╚═╝  🗡️ ║
    ║                                                                ║
    ║           ✦ DESCEND IF YOU DARE ✦                           ║
    ║                                                                ║
    ╚════════════════════════════════════════════════════════════════╝
```

**Color scheme:**
- Title text: **CYAN** (cold, mysterious)
- ASCII art: **BRIGHT WHITE** (bold, dramatic)
- Tagline: **YELLOW** (draws eye, warns of danger)
- Borders: **CYAN**

**Atmosphere narrative (optional, shown after title):**
```
Press ENTER to skip intro, or read on...

The dungeon stretches endlessly downward, breathing with ancient malice.
Countless adventurers have descended. Few return.

You hear the screams of the damned echoing from below.
The choice is yours: courage or cowardice?
```
- Tone: Dark, foreboding, but not grimdark
- Length: 4 sentences max
- Delivery: Slow (one line per 1.5 seconds, optional skip)

---

## 2. Character Creation Sequence

### Revised Flow

The sequence is **reordered** to build investment and enable informed choices:

1. **Title screen** (with optional lore)
2. **Prestige display** (if applicable)
3. **Name entry** (builds investment early)
4. **Difficulty selection** (now transparent with mechanics)
5. **Class selection** (shows full starting stats, not just bonuses)
6. **Seed display** (auto-generated, shown for reference)
7. **Game start**

### 2a. Prestige Display (if player has prestige > 0)

**Current:** Bare text "Prestige Level: 3"  
**Proposed:**

```
╔════════════════════════════════════════════════════════════╗
║               🏆 RETURNING CHAMPION 🏆                    ║
╠════════════════════════════════════════════════════════════╣
║  Prestige Level:  3                                        ║
║  Total Victories: 9 wins (3 runs per level)                ║
║  Win Rate:        45% (9 wins / 20 runs)                   ║
╠════════════════════════════════════════════════════════════╣
║  Starting Bonuses:                                         ║
║    • Attack:    +1                                         ║
║    • Defense:   +1                                         ║
║    • Max HP:    +15                                        ║
╠════════════════════════════════════════════════════════════╣
║  Progress to Prestige 4:  6 more wins needed               ║
║  (The dungeon remembers your victories.)                   ║
╚════════════════════════════════════════════════════════════╝
```

**Color:**
- Header: **BRIGHT WHITE**
- "RETURNING CHAMPION": **YELLOW** (celebration)
- Stats labels: **CYAN**
- Stats values: **GREEN** (positive reinforcement)
- Progress bar: **YELLOW** or **GREEN**

### 2b. Name Entry

**Current:** `Console.Write("Enter your name, adventurer: ");`  
**Proposed:**

```
╔════════════════════════════════════════════════════════════╗
║         What is your name, adventurer?                    ║
║                                                            ║
║  Enter a name (or press ENTER for "Hero"):                ║
║                                                            ║
║  ► _                                                       ║
╚════════════════════════════════════════════════════════════╝
```

- Prompt is flavor-rich, not bare
- Shows default fallback
- Input line shows cursor/prompt clearly

### 2c. Difficulty Selection

**Current:**
```
Choose difficulty: [1] Casual  [2] Normal  [3] Hard
```

**Proposed: Difficulty card selection with mechanics transparency**

```
╔════════════════════════════════════════════════════════════╗
║                  CHOOSE DIFFICULTY                        ║
╠════════════════════════════════════════════════════════════╣
║                                                            ║
║  [1] CASUAL — Perfect for your first descent              ║
║      Enemy Damage: 80% (easier)                           ║
║      Loot Quality: 150% (generous rewards)                ║
║      Elite Spawn Rate: 5% (mostly normal enemies)         ║
║      Recommended: First run, relaxed playstyle            ║
║                                                            ║
║  [2] NORMAL — The intended experience                     ║
║      Enemy Damage: 100% (balanced)                        ║
║      Loot Quality: 100% (standard rewards)                ║
║      Elite Spawn Rate: 15% (more tough fights)            ║
║      Recommended: Subsequent runs, standard challenge     ║
║                                                            ║
║  [3] HARD — Only the worthy survive                       ║
║      Enemy Damage: 130% (brutal)                          ║
║      Loot Quality: 70% (scarce rewards)                   ║
║      Elite Spawn Rate: 30% (many tough fights)            ║
║      Recommended: Mastery run, prestige farming           ║
║                                                            ║
║  ► Select [1/2/3]:                                        ║
╚════════════════════════════════════════════════════════════╝
```

**Color scheme:**
- Header: **CYAN**
- Difficulty level names: **BRIGHT WHITE** (1), **GREEN** (2), **BrightRed** (3)
- Mechanical stats: **YELLOW**
- Input prompt: **CYAN**

**Benefit:** Players understand tradeoffs, not just labels. Casuals aren't scared; Hard players see the real challenge.

### 2d. Class Selection

**Current:**
```
Choose your class:
[1] Warrior - High HP, defense, and attack bonus. Reduced mana.
[2] Mage - High mana and powerful spells. Reduced HP and defense.
[3] Rogue - Balanced with an attack bonus. Extra dodge chance.
```

**Proposed: Class cards showing full starting stats**

```
╔════════════════════════════════════════════════════════════╗
║                  CHOOSE YOUR CLASS                        ║
╠════════════════════════════════════════════════════════════╣
║                                                            ║
║  [1] ⚔️  WARRIOR — The Unbreakable                        ║
║      Starting Stats (base + class + prestige):            ║
║        HP:       100 → 120  (+20)                         ║
║        Attack:    10 → 12   (+2)                          ║
║        Defense:    3 → 5    (+2)                          ║
║        Mana:      30 → 20   (-10)                         ║
║      PASSIVE: Unstoppable — +3% defense per 10% HP        ║
║      PLAYSTYLE: Tank through attrition.                   ║
║                 Survive what others can't.                ║
║                                                            ║
║  [2] 🔮 MAGE — The Mystic Force                           ║
║      Starting Stats (base + class + prestige):            ║
║        HP:        60 → 75   (+15)                         ║
║        Attack:    10 → 8    (-2)                          ║
║        Defense:    3 → 2    (-1)                          ║
║        Mana:      60 → 80   (+20)                         ║
║      PASSIVE: Spellweaver — Spell crit chance: 15%        ║
║      PLAYSTYLE: Glass cannon burst.                       ║
║                 End fights before they start.             ║
║                                                            ║
║  [3] 🗡️  ROGUE — The Swift Shadow                         ║
║      Starting Stats (base + class + prestige):            ║
║        HP:        80 → 95   (+15)                         ║
║        Attack:    10 → 12   (+2)                          ║
║        Defense:    3 → 3    (+0)                          ║
║        Mana:      30 → 30   (+0)                          ║
║      PASSIVE: Evasion — +10% chance to dodge attacks      ║
║      PLAYSTYLE: Balanced and nimble.                      ║
║                 Skill and speed over raw power.           ║
║                                                            ║
║  ► Select [1/2/3]:                                        ║
╚════════════════════════════════════════════════════════════╝
```

**Color scheme:**
- Class names: **BRIGHT WHITE**
- Emojis: **YELLOW**
- "Starting Stats": **CYAN** (label)
- Stat names (HP, Attack, etc.): **GREEN**
- Starting values: **BRIGHT WHITE**
- Bonuses (+X): **GREEN**
- Penalties (-X): **BrightRed**
- PASSIVE trait name: **YELLOW**
- Trait description: **CYAN**
- PLAYSTYLE header: **YELLOW**
- Playstyle text: **CYAN**

**Why this works:**
- Players see exactly what their character starts with (accounting for all bonuses)
- Passive traits are named and explained (not mysterious)
- Playstyle guidance helps choosing based on preferred approach
- Color separates information categories (stats vs. passives vs. playstyle)
- Emojis make classes memorable (⚔️ tank, 🔮 spellcaster, 🗡️ agile)

---

## 3. Seed Handling

### Current State
```
Enter a seed for reproducible runs (or press Enter for random):
> [player input]
```

### Problems
- Blocks casual players (95% don't care)
- Speedrunners/testers need to note seed anyway
- CLI integration is awkward

### Proposed Solution

**Option A: Auto-generate, display at end**
- Seed is generated automatically
- Shown to player just before game starts: "Seed: 123456 (share this to replay)"
- Players only think about it if they want to share/replay
- Reduces cognitive load

**Option B: CLI flag for power users (future)**
- Add `--seed 12345` flag to executable
- If provided, use it; otherwise auto-generate
- Doesn't clutter the UI
- Example: `dotnet run -- --seed 123456`

**Recommended approach:** Option A now (auto-generate and display), add Option B later when CLI interface is formalized.

**Code in Program.cs:**
```csharp
// Seed selection (simplified)
var actualSeed = new Random().Next(100000, 999999);
// Display will show it just before game starts
```

**Seed display line (after all selections):**
```
═════════════════════════════════════════════════════════════
  Initializing your descent...
  Seed: 537892  (share this number to replay the exact same run)
═════════════════════════════════════════════════════════════
```

---

## 4. Architecture & Implementation Approach

### Principle: Keep it Simple Now, Extract When Needed

**Current Program.cs:** ~80 lines of setup code (acceptable)  
**Proposed:** Add display methods, keep orchestration in Program.cs  
**Future:** Extract to `GameSetupService` when implementing "Load Game" (to avoid duplicating setup logic)

### New IDisplayService Methods

Add to interface (and ConsoleDisplayService implementation):

```csharp
// Intro screen with title, tagline, ASCII art
void ShowEnhancedTitle();

// Optional lore/atmosphere text (returns true if player wants to skip)
bool ShowIntroNarrative();

// Display prestige info for returning players
void ShowPrestigeInfo(PrestigeData prestige);

// Difficulty selection (returns validated Difficulty enum)
Difficulty SelectDifficulty();

// Class selection (returns validated PlayerClassDefinition)
PlayerClassDefinition SelectClass();

// Show seed before game starts
void ShowSeedInfo(int seed);
```

### Validation Logic Lives in Display Layer

Display service owns input loops:
```csharp
public Difficulty SelectDifficulty()
{
    while (true)
    {
        ShowDifficultyOptions();
        var input = Console.ReadLine()?.Trim() ?? "";
        
        var difficulty = input switch
        {
            "1" => Difficulty.Casual,
            "2" => Difficulty.Normal,
            "3" => Difficulty.Hard,
            _ => null
        };
        
        if (difficulty.HasValue)
            return difficulty.Value;
        
        ShowError("Invalid selection. Choose [1], [2], or [3].");
    }
}
```

**Why:** Display layer knows what inputs are valid. Game logic receives guaranteed-valid data.

### Program.cs After Changes

```csharp
var display = new ConsoleDisplayService();

// Intro sequence (in order)
display.ShowEnhancedTitle();
if (!display.ShowIntroNarrative())
    display.ShowMessage(""); // User skipped, space things out

var prestige = PrestigeSystem.Load();
if (prestige.PrestigeLevel > 0)
    display.ShowPrestigeInfo(prestige);

var name = display.ReadPlayerName();
var difficulty = display.SelectDifficulty();  // NEW: returns validated Difficulty
var playerClass = display.SelectClass();      // NEW: returns validated PlayerClassDefinition

// Seed (auto-generated now)
var actualSeed = new Random().Next(100000, 999999);

display.ShowSeedInfo(actualSeed); // NEW: shows seed before game starts

// Create player (unchanged logic)
var player = new Player { Name = name };
player.Class = playerClass.Class;
player.Attack += playerClass.BonusAttack;
// ... rest of setup ...
```

**Total new code in Program.cs:** ~10 lines  
**New Display methods:** 5 methods (shown above)  
**ConsoleDisplayService additions:** ~200 lines

---

## 5. Implementation Phases

### Phase 1: Foundation (2 hours)
- [ ] Add 5 new methods to IDisplayService
- [ ] Implement ShowEnhancedTitle with ASCII art and colors
- [ ] Implement ShowPrestigeInfo with formatting
- [ ] Update Program.cs to use new methods (remove inline seed prompt)
- [ ] Test on actual console (ensure colors display correctly)

### Phase 2: Enhanced Selection (2 hours)
- [ ] Implement SelectDifficulty with full card display
- [ ] Implement SelectClass with stat cards
- [ ] Implement ShowSeedInfo
- [ ] Test validation loops (invalid inputs re-prompt)
- [ ] Test with all class/difficulty combinations

### Phase 3: Polish (1.5 hours)
- [ ] Implement ShowIntroNarrative with optional skip
- [ ] Add spacing/pacing (slow narrative reveal if not skipped)
- [ ] Color verification (ensure all colors render correctly on dark/light terminals)
- [ ] Update README with new intro design
- [ ] Run full test suite (all 267 tests should pass)

### Phase 4: Review & Merge (1 hour)
- [ ] Coulson reviews implementation against this plan
- [ ] Anthony approves visual design
- [ ] Merge to master

**Total: ~6.5 hours**

---

## 6. Success Criteria

### Functional
- [ ] All 267 existing tests pass (no regressions)
- [ ] All 5 display methods work correctly
- [ ] Invalid inputs re-prompt (no crashes)
- [ ] Prestige display shows when prestige > 0 (hidden when 0)
- [ ] Lore narrative can be skipped with Enter
- [ ] Seed is displayed before game starts

### Visual/UX
- [ ] Title screen conveys atmosphere (dark, mysterious)
- [ ] Difficulty/class selections clearly show tradeoffs
- [ ] Colors are consistent with color system established in PR #226
- [ ] Spacing and alignment are clean (no ragged borders)
- [ ] New intro takes <1 minute for experienced players (fast path)

### Technical
- [ ] No changes to game logic (purely presentation layer)
- [ ] Display layer owns validation loops (no null checks in Program.cs)
- [ ] New interface methods are composable and reusable
- [ ] Code follows existing patterns (emojis, colors, ASCII borders)

---

## 7. Risk Assessment

**Risk: Low**
- Pure presentation layer (no game logic changes)
- Existing tests don't depend on intro code (easy to add new tests if needed)
- Fallback behavior preserved (name defaults to "Hero", difficulty defaults to Normal)

**Mitigation:**
- Run full test suite after each phase
- Test with actual console (colors vary by terminal)
- Get Anthony's sign-off on visual design before implementing

---

## 8. Future Extensions (Not in This Plan)

These ideas are cool but out of scope:

- **Character portraits:** ASCII art for each class (after class selection)
- **Build preview:** Show how the player's character will look at level 10, 20
- **Tutorial tips:** Contextual hints during setup ("Mages need a big mana pool; use Mana potions in combat")
- **Difficulty auto-recommend:** Suggest Normal for first run, Hard for prestige farming
- **Customizable colors:** Let players override color scheme
- **Speedrun mode:** `--speedrun` flag that skips narrative and prestige display

---

## Sign-Off

**Coulson (Lead):** ✅ Approved  
**Hill (C# Dev):** ✅ Ready to implement  
**Barton (Systems Dev):** ✅ UX flow validated  

**Pending:** Anthony approval before implementation begins
