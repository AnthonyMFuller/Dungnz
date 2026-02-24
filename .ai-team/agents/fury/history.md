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

_To be updated as work progresses._
