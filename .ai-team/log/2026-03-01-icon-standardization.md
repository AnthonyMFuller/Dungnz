# Icon Standardization Session — 2026-03-01

**Requested by:** Anthony

## Root Cause Diagnosed
🛡 (U+1F6E1) has EAW=N but was not in NarrowEmoji, so EL() gave it 1 space (wide treatment) — text misaligned vs all other slots.

## Executive Directive
All icons must belong to the same character set.

## Work Completed
- Hill created branch `squad/829-standardize-icons`
- Replaced all wide emoji with narrow symbols
- New IL() helper replaces EL() + NarrowEmoji — uniform 2-space padding, no per-symbol logic

## Icon Mapping (Standardized)
- ⚔ Weapon
- ✦ Accessory
- ⛑ Head
- ◈ Shoulders
- ✚ Chest (fixed: was ⛨, now U+271A)
- ☞ Hands
- ≡ Legs
- ⤓ Feet
- ↩ Back
- ⛨ Off-Hand
- ★ Level
- ✦ Combo
- ⚔ Attack
- ✦ Ability
- ↗ Flee
- ⚗ Use Item

## Fixes Applied
- Fixed duplicate icon: Chest and Off-Hand both had ⛨
- Chest now uses ✚ (U+271A)

## References
- PR #830: https://github.com/AnthonyMFuller/Dungnz/pull/830
- GitHub issue #829
