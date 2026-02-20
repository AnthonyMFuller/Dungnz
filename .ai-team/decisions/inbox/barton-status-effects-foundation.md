### 2026-02-20: Status Effects System Architecture

**By:** Barton  
**What:** Implemented foundation for turn-based status effects system with 6 effect types  
**Why:** Adds depth to combat with DOT/HOT mechanics and stat modifiers; enables counter-play strategies

**Design Choices:**
1. **Enum-based effect types** — Simple, type-safe, easy to extend
2. **Duration tracking per effect** — Each ActiveEffect has RemainingTurns that decrements each turn
3. **Dictionary-based storage** — Effects keyed by target object (Player/Enemy) for fast lookup
4. **Debuff/Buff separation** — Antidote only removes debuffs, preserving intentional design
5. **Stat modifiers calculated on-demand** — GetStatModifier() called during damage calculations instead of mutating base stats

**Effect Balance:**
- DOTs frontloaded: Bleed (5dmg/2turns = 10 total) > Poison (3dmg/3turns = 9 total)
- Stun is powerful but brief (1 turn) to avoid frustration
- Stat modifiers use 50% to be impactful without trivializing combat
- Regen (4HP/3turns = 12 total) counters DOT pressure

**Integration Strategy:**
- StatusEffectManager shared between CombatEngine and GameLoop for Antidote usage
- Effects processed before actions to prevent "ghost hits" from DOT deaths
- Clear effects on combat end to prevent stale state
