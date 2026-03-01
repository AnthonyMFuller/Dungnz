# Difficulty Selection Screen Update — Design Decision

**Date:** 2026-02-[current]  
**Agent:** Fury (Content Writer)  
**Work:** Update SelectDifficulty() labels to communicate actual gameplay impact  
**Issue:** New DifficultySettings have broader impact than old labels reflected

---

## Decision

Updated three difficulty labels in `Display/DisplayService.cs` (SelectDifficulty, lines 1133-1135) from raw multiplier notation to player-centric descriptive text that communicates:
1. Overall gameplay feel
2. Most impactful stats (enemy power, loot/gold, starting supplies)
3. Permadeath flag for Hard

## Labels Chosen

```
CASUAL     Weaker enemies · Cheap shops · Start with 50g + 3 potions
NORMAL     Balanced challenge · The intended experience · Start with 15g + 1 potion
HARD       Stronger enemies · Scarce rewards · No starting supplies · ☠ Permadeath
```

## Rationale

**Why narrative over multipliers:**
- Raw numbers (×1.35, ×0.65) are meaningless to players unfamiliar with balance targets
- Players need to understand *what they're actually getting into*: Are enemies harder? Is loot better? Do I start with supplies?

**Label design:**
- **CASUAL:** Emphasizes relaxation (weaker foes, cheap items, generous starting resources)
- **NORMAL:** Frames itself as "the intended experience" — authority positioning
- **HARD:** Emphasizes punishment (stronger foes, scarce drops, permadeath, no handouts)
- **Key QoL highlight:** Starting supplies differ drastically (50g+3 potions vs. 0g+0 potions) — this is a major difficulty marker
- **Selectivity:** Omitted less-impactful stats (XP multiplier, shrine spawn rate) to keep labels under ~75 characters and scannable

**Format preservation:**
- Kept all color codes (`{green}CASUAL{reset}`, etc.) exactly as designed
- Maintained bullet-point spacing (·) for visual rhythm

## Alternatives Considered

1. **Hybrid approach** (multipliers + description) → Rejected: Too noisy, defeats "communicate feel" goal
2. **Emojis for all difficulty names** → Rejected: Only permadeath gets ☠; others are thematic flavor
3. **Single-sentence labels** → Rejected: Would omit starting supplies detail (major QoL difference)

## Impact

- Players now see concrete gameplay consequences before committing
- Difficulty choice becomes informed rather than blind
- Permadeath is explicitly flagged for Hard (safety)
