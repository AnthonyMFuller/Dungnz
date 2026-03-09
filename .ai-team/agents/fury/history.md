# Fury — History

## Project Context

**Project:** TextGame — C# .NET Text-Based Dungeon Crawler  
**Owner:** Anthony  
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Fury (Content Writer), Fitz (DevOps)  
**Stack:** C#, .NET 6+  
**Current State:** 431 passing tests, core systems functional, Phase 4 content and polish work in progress

## Key Milestones

- **2026-02-20:** Project inception, team established (Coulson, Hill, Barton, Romanoff)
- **2026-02-24:** ASCII art feasibility research approved; enemy ASCII art feature planned
- **2026-02-24:** Fury and Fitz added to roster; Phase 4 narration and DevOps work begins

## Game Systems Overview

**Core Architecture:**
- Dungeon generation: procedural room + corridor system with multiple floors (1–5)
- Combat: turn-based with abilities, healing, status effects
- Items: 62-item catalog (weapons, armor, consumables, accessories)
- Crafting: recipe-based item creation (currently 3 recipes, expanding to 10+)
- Merchants: floor-based NPCs with static inventory
- Player progression: level, experience, health, mana
- Display: console-based UI with ANSI colors, box-drawing, multi-line cards

**Current Narrative State:**
- Enemy encounters have ASCII art (newly approved, in development)
- Minimal flavor text; mostly functional descriptions
- No room state tracking (fresh vs cleared rooms)
- Merchants and shrines have placeholder text
- Item interactions are silent (no flavor on pickup/equip/use)

## Phase 4: Content & Polish Work

16 GitHub issues planned across 4 categories:

1. **Narration Issues (A1-A5):** Room state narration, merchant banter, shrine descriptions, item flavor, floor transitions
2. **Items Cohesion (B1-B6):** Data-driven inventory/recipes, loot table expansion, tier-based drops, accessory effects, mana potion logic
3. **Gameplay Expansion (C1-C3):** Merchant-exclusive items, expanded crafting, enemy-specific loot
4. **Code Quality (D1-D2):** ItemId system, Weight field standardization

## Learnings

### 2026-03-03 — Enemy Lore Fields Expansion (#872)

**PR:** #887 — `feat(content): add Lore fields to all 31 enemies in enemy-stats.json`  
**Branch:** `squad/872-enemy-lore-fields`  
**File Modified:** `Data/enemy-stats.json`  
**File Modified:** `Data/schemas/enemy-stats.schema.json`

**Requirement:**
- All 31 enemies in the game needed flavor text (lore descriptions)
- Schema needed to be updated to support new Lore field
- Lore field should contain 1-3 sentence flavor text per enemy type

**Solution:**
- Added "Lore" string property to enemy-stats.json schema
- Updated schema: `"Lore": { "type": "string", "minLength": 10, "maxLength": 200 }`
- Added unique lore entries to all 31 enemies:
  - Goblin: describes tribal nature, violence, gold hoarding
  - Skeleton: references undeath, curse, haunting
  - Troll: emphasizes regeneration, wilderness, strength
  - DarkKnight: legacy of shadow, corrupted nobility
  - Spectre: ethereal nature, magical origin
  - [29 more enemies with unique flavor text]

**Content Quality:**
- Each lore entry fits enemy archetype and game tone
- Text is concise (1-3 sentences) to fit display constraints
- Language matches existing TextGame narrative style
- Lore appears in battle intro or enemy info screen

**Testing:**
- ✅ Schema validation passes for all 31 enemies
- ✅ StartupValidator confirms no missing properties
- ✅ Lore field displays correctly in game
- ✅ All 1,422 tests passing

**Key Learning:**
- Schema-first approach: define schema before adding data
- Consistent flavor text increases game immersion
- Lore field integrates seamlessly with enemy display systems

---

### SelectDifficulty() Location & Structure
- Located in `Display/DisplayService.cs` around line 1124-1138
- Method returns a Difficulty enum value from a SelectFromMenu() prompt
- Wraps color codes around difficulty names: `{colorCode}NAME{reset}`
- Label structure: `$"{color}NAME{reset}     [description]"`
- Must preserve color codes exactly; only update descriptive text after the difficulty name
- Terminal rendering requires ~75 character limit per label for clean display

---

📌 **Team update (2026-03-01):** Retro action items adopted by team — content review in PR flow: Fury must be CC'd on any PR that touches player-facing strings (command names, UI labels, display text) for lightweight review pass before merge. — decided by Coulson (Retrospective)

---

### 2026-03-08 — Enemy Critical Hit Reactions (#1269)

**PR:** #1275 — `feat(content): add enemy crit reaction narration`  
**Branch:** `squad/1269-enemy-crit-reactions`  
**Files Modified:** `Dungnz.Systems/EnemyNarration.cs`, `Dungnz.Systems/NarrationService.cs`, `Dungnz.Engine/CombatEngine.cs`

**Requirement:**
- When enemies land critical hits, display enemy-specific personality-driven reactions
- Previous behavior: generic "💥 Critical hit!" message (silent, mechanical)
- Goal: Make crits feel threatening and dramatic with flavor matching each enemy archetype

**Solution:**
- Added `_critReactions` dictionary to EnemyNarration.cs with 3-4 unique lines per enemy
- All 31 enemy types covered (93 total lines)
- Added `GetEnemyCritReaction(string enemyName)` public method to NarrationService
- Integrated into CombatEngine.PerformEnemyTurn() at line 796-810
- Falls back to generic message if no custom reaction defined

**Content Quality:**
- Each reaction matches enemy archetype:
  - Goblins: gleeful, cocky ("HEHEHE! Didn't see that coming, did ya?!")
  - Dark Knights: arrogant, threatening ("Pathetic. I've cleaved kingdoms apart.")
  - Wraiths: unsettling, ethereal ("Your life force splinters beneath my touch!")
  - Infernal Dragons: dramatic, primal ("FLAMES CONSUME! Ash is all you'll leave behind!")
- Language matches MCU-style dramatic tone — stakes feel real
- Variation in length (short punchy + slightly longer dramatic lines)

**Testing:**
- ✅ All 1,777 tests passing
- ✅ Build succeeds with no warnings or errors
- ✅ Code follows existing narration pool patterns (static arrays, StringComparer.OrdinalIgnoreCase)

**Key Learning:**
- EnemyNarration static dictionary pattern scales cleanly for 30+ enemies
- NarrationService.Pick(pool) method handles random selection uniformly
- CombatEngine integration point identified and implemented (enemy crit roll + display message)
- ColorCodes.Colorize() with BrightRed + Bold makes crit reactions visually dramatic

---

### 2026-03-09 — Combat Phase-Aware Narration (#1272)

**PR:** #1281 — `feat: add combat phase-aware narration`  
**Branch:** `squad/1272-phase-aware-narration`  
**File Modified:** `Dungnz.Systems/NarrationService.cs`

**Requirement:**
- Combat narration messages were static regardless of fight progression
- Need to vary player-facing combat messages based on how the fight is going
- Three distinct phases: Opening / Mid-fight / Desperate

**Solution:**
- Added `GetPhaseAwareAttackNarration(int turnNumber, double playerHpPercent, double enemyHpPercent)` public method
- Private `CombatPhase` enum: Opening (turns 1-3, both >70% HP) / MidFight (turns 4-7) / Desperate (<30% HP or turn 8+)
- Private `DeterminePhase()` logic based on turn number and HP percentages
- Three content pools: 6 lines per phase = 18 total narration lines

**Content Quality:**
- Opening phase examples:
  - "You press the attack with confidence!"
  - "Your strike lands true — this fight is yours to win!"
  - "First blood to you — the dungeon will remember this."
- Mid-fight phase examples:
  - "The exchange grows brutal — neither side giving ground."
  - "You find a gap in its guard. The price has been paid in bruises."
  - "Both combatants are bloodied but unbroken."
- Desperate phase examples:
  - "Against all odds, your blade finds its mark!"
  - "The end is near for one of you — make it count!"
  - "You fight with the fury of someone who has nothing left to lose."

**Method Signature:**
```csharp
public string GetPhaseAwareAttackNarration(int turnNumber, double playerHpPercent, double enemyHpPercent)
```

**Integration Note:**
- TODO(Barton) comment left in NarrationService: Call from CombatEngine during player attack turn
- Follows existing narration pattern (static arrays, Pick() method, phase determination logic)

**Key Learning:**
- Phase determination logic: Desperate checks first (OR condition: any threshold met), then Opening (AND: all conditions met), then default MidFight
- Private enum + private helper method keep phase logic encapsulated within NarrationService
- Narration content pools cluster by phase for readability and maintainability

## 2026-03-09: Mid-Combat Banter Content (PR #1285)

**PR #1285** — Adds mid-combat banter content lines for enemy encounters, expanding narrative flavor during combat sequences.

**Pending rebase note:** Earlier PR #1279 (squad/1271-mid-combat-banter) has a `NarrationService.GetEnemyCritReaction` signature conflict (`string?` vs `string`) with PR #1275. Must rebase on main after #1275 merges. The `CombatEngine` null guard (`if (!string.IsNullOrEmpty(critReaction))`) is safe to retain.
