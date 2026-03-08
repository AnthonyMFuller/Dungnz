# Combat Design Planning Session — 2026-03-08
**Facilitator:** Coulson  
**Participants:** Barton (Systems), Fury (Content), Romanoff (QA)  
**Requested by:** Anthony

## Current State Assessment

Anthony requested combat improvements to address repetitiveness, specifically mentioning:
- Enemy encounter dialog (banter, variety)
- Ability cooldowns
- Anything that makes combat more engaging

### What We Have
- **Cooldowns already work** — AbilityManager tracks cooldowns correctly, decrements them per turn, blocks usage on cooldown
- **Bookend narration exists** — EnemyNarration has 3 intros + 3 deaths per enemy (9 types), BossNarration has multi-line intros/deaths/phases (12 bosses)
- **Combat foundation solid** — 30 class-specific abilities across 6 classes, status effects, enemy special mechanics, boss phases

### The Core Problem: Turn Homogeneity
Every turn presents identical choices: Attack / Ability / Item / Flee. No reactive decisions, no read-ahead, no momentum, no mid-combat personality. Cooldowns exist but are **invisible on the main HUD** — players don't know when abilities return until they check the menu.

## Decisions

### P0 — Quick Wins (High Impact, Low Risk)

#### 1. Make Cooldowns Visible
**Owner:** Barton  
**What:** Add cooldown display to main combat HUD showing ability status with turn counts. Add toast notifications when key abilities come off cooldown.  
**Why:** Cooldowns already work but are invisible — players default to attack spam because they can't see when abilities are ready. Pure display change, zero logic risk.  
**Size:** S (40 lines, display-only)

#### 2. Enemy Crit Reactions
**Owner:** Fury  
**What:** Add 3 enemy-specific lines per enemy type (9×3 = 27 lines) that fire when enemy lands a critical hit. Make enemy crits visible and threatening.  
**Why:** Biggest immersion gap — player crits have rich output, enemy crits are silent. Breaks combat rhythm in a consequential way.  
**Size:** S (content + 1 hook in AttackResolver)

### P1 — Core Improvements (Medium Complexity)

#### 3. Enemy Intent Telegraph System
**Owner:** Barton + Fury  
**What:** Display one-line warning before special enemy actions fire (e.g., "The Skeleton's ribcage glows — it's preparing Bone Rattle"). Extend boss telegraph pattern to all enemies. Adds 3 telegraph lines per enemy type (27 lines).  
**Why:** Players can't react to invisible attacks. Telegraph creates "what do I do about THIS" decision layer.  
**Size:** M (120 lines — AI interface extension + narration data)

#### 4. Mid-Combat Enemy Banter
**Owner:** Fury  
**What:** Four categories of enemy dialog:
- Idle taunts (every 3-4 turns, 25% chance) — 3 lines/enemy
- Reaction to player dodge — 3 lines/enemy  
- Low HP desperation (at 35% threshold) — 2 lines/enemy  
- Enemy crit (covered in P0)
Total: ~100 new narration lines across 9 enemy types + 12 bosses  
**Why:** Enemies have no personality mid-combat. Creates variety in repeated encounters.  
**Size:** M (content + 3 hooks in CombatEngine)

#### 5. Combat Phase-Aware Narration
**Owner:** Barton + Fury  
**What:** Add CombatPhase enum (Opening/MidFight/Desperate/Finishing) derived from HP percentages. Different narration pools fire at different phases. 30 new strings (3 pools × 5 messages).  
**Why:** Same enemy fights feel different based on how combat flows — clean start vs desperate scramble.  
**Size:** M (80 lines — NarrationService + data)

### P2 — Stretch Goals (Higher Complexity)

#### 6. Momentum Resource System (per-class)
**Owner:** Barton  
**What:** Each class builds a secondary resource through combat actions that unlocks enhanced abilities:
- Warrior: Fury (+1 per hit taken/landed, at 5 = free crit)  
- Mage: Arcane Charge (+1 per spell, at 3 = free enhanced cast)  
- Paladin: Devotion (+1 with shield/heal, at 4 = next smite stuns)  
- Ranger: Focus (+1 per turn without damage, at 3 = armor-ignore)  
**Why:** Creates strategic "when do I spend this" decisions. Rogue combo points prove the pattern works.  
**Size:** L (250 lines — Player model + CombatEngine + display)

### Deferred (Out of Scope)
- Environmental/positional combat hooks — requires room system integration
- Interrupt/reaction windows — requires new resource axis (stamina)
- Enemy AI variety expansion — DefaultEnemyAI rewrite needed

## Action Items

| Owner | Action | Issue |
|-------|--------|-------|
| Barton | Cooldown HUD visibility | #TBD |
| Fury | Enemy crit reactions (27 lines) | #TBD |
| Barton + Fury | Enemy telegraph system | #TBD |
| Fury | Mid-combat enemy banter (100 lines) | #TBD |
| Barton + Fury | Phase-aware narration | #TBD |
| Romanoff | Pre-combat test baseline (11 tests) | #TBD |
| Barton | Momentum resource system (stretch) | #TBD |

## Romanoff's Test Requirements

**Before ANY combat feature PR merges, add these 11 baseline tests:**
1. Turn loop phase ordering test
2. Cooldown block + tick tests (2 tests)  
3. Ability damage quantification (4 tests across classes)
4. Multi-effect status interaction (3 tests)
5. Boss phase transition threshold test

**Narration testing strategy:**
- Assert *that hooks fired*, not *which strings returned*
- Use event-based contract tests, not string matching
- Keep content tests separate from behavior tests

## Notes

### Technical Debt Context
- CombatEngine.cs is 1,259 lines — decomposition stubs exist (#1203) but logic migration incomplete
- Changes should wire through interface layers (IEnemyAI, IAttackResolver) not inline into monolith
- Enemy crit tracking doesn't exist on enemy side — needs one-liner bool enemyCrit check

### Risk Assessment
All P0 and P1 changes are LOW risk except narration hook timing (MEDIUM). P2 momentum system is MEDIUM risk due to CombatEngine state machine expansion.

### Estimated Scope
- P0: 2-3 sessions total
- P1: 5-6 sessions total  
- P2: 3-4 sessions
- Test baseline: 1-2 sessions

**Total: 11-15 sessions for P0+P1+tests, 14-19 with P2**
