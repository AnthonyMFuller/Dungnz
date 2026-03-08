### 2026-03-08: SetBonusManager stat application fixed

**By:** Barton  
**What:** Fixed 4 stat bonus application bugs — MaxHP, MaxMana, CritChance, Attack bonuses now properly applied to player  
**Why:** These were P1 gameplay bugs — entire set bonus mechanics were cosmetic

**Details:**
- Shadowstalker 4-piece SetId mismatch: changed "shadowstalker" → "shadowstep-set"
- MaxHP/MaxMana bonuses: now applied to player.MaxHP and player.MaxMana (lines 229-231)
- CritChanceBonus: now included in AttackResolver.RollCrit() calculation
- AttackBonus: now included in AttackResolver damage calculation (line 125)

**Impact:**
- Ironclad 2-piece (+10 HP), Arcanist 2-piece (+20 mana) now functional
- Shadowstalker 2-piece (+10% crit), Shadowstep 4-piece (+15% crit) now functional
- Arcane Ascendant 2-piece (+20 attack) now functional
- Shadowstep 4-piece (guaranteed bleed) now activates correctly

**Related:** Issues #1240, #1242, #1253, #1254  
**PR:** #1259
