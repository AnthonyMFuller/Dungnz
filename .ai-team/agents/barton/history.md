# Barton — History

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Learnings

### Phase 2 - Combat Systems Implementation (WI-6, WI-7, WI-8)

**Files Created:**
- `Engine/ICombatEngine.cs` — interface contract for combat
- `Engine/CombatEngine.cs` — turn-based combat implementation
- `Systems/InventoryManager.cs` — item pickup and use mechanics
- `Models/LootTable.cs` — replaced stub with full probability-based loot system
- `Systems/Enemies/Goblin.cs` — 20 HP, 8 ATK, 2 DEF, 15 XP, drops 2-8 gold
- `Systems/Enemies/Skeleton.cs` — 30 HP, 12 ATK, 5 DEF, 25 XP, drops bone/sword
- `Systems/Enemies/Troll.cs` — 60 HP, 10 ATK, 8 DEF, 40 XP, drops troll hide
- `Systems/Enemies/DarkKnight.cs` — 45 HP, 18 ATK, 12 DEF, 55 XP, drops dark blade/armor
- `Systems/Enemies/DungeonBoss.cs` — 100 HP, 22 ATK, 15 DEF, 100 XP, guaranteed boss key

**CombatEngine Design:**
- Turn-based: player attacks first, then enemy retaliates
- Damage formula: `Math.Max(1, attacker.Attack - defender.Defense)`
- Flee mechanic: 50% success rate; failure results in enemy free hit
- XP/Leveling: 100 XP per level, awards +2 ATK, +1 DEF, +10 MaxHP, full heal
- Loot drops: awarded on enemy death via LootTable.RollDrop()
- Returns CombatResult enum: Won, Fled, PlayerDied

**LootTable Configuration:**
- Each enemy initializes its own LootTable in constructor
- Supports min/max gold ranges
- Item drops use probability (0.0-1.0), first matching drop wins
- Boss drops guaranteed with 1.0 chance

**Inventory System:**
- TakeItem: case-insensitive partial match for item names
- UseItem: handles Consumable (heal), Weapon (ATK boost), Armor (DEF boost)
- Equipment permanently increases stats and is consumed
- Uses DisplayService for all output (no direct Console calls)

**Build Status:**
- Cannot verify build (dotnet not in PATH)
- All files created successfully, committed to git
- Respected Hill's existing Model contracts exactly
- Integrated real enemies into EnemyFactory.cs (replaced stubs)
- Program.cs already wired to use CombatEngine (Hill's work)

### 2026-02-20: Retrospective Ceremony & v2 Planning Decisions

**Team Update:** Retrospective ceremony identified 3 refactoring decisions for v2:

1. **DisplayService Interface Extraction** — Extract IDisplayService interface for testability. CombatEngine will update to depend on IDisplayService instead of concrete DisplayService. Minimal breaking change. Effort: 1-2 hours.

2. **Player Encapsulation Refactor** — Hill refactoring Player model to use private setters and validation methods. Barton can use these methods in combat/inventory logic instead of direct property mutations.

3. **Test Infrastructure Required** — Before v2 feature work, implement xUnit/NUnit harness. Inject Random into CombatEngine and LootTable for deterministic testing. Blocks feature work. Effort: 1-2 sprints.

**Participants:** Coulson (facilitator), Hill, Barton, Romanoff

**Impact:** Barton coordinates with Hill on IDisplayService updates. Random injection strategy needed for CombatEngine deterministic testing.

### 2026-02-20: V2 Systems Design Proposal

**Context:** Boss requested v2 planning from game systems perspective (features, content, balance).

**Deliverable:** Comprehensive v2 proposal covering:
1. **5 New Gameplay Features** (ranked by impact/effort)
   - Status Effects System (Poison, Bleed, Stun, Regen, etc.) — HIGH priority
   - Skill System with cooldowns and mana resource — HIGH priority
   - Enhanced Consumables (buff potions, antidotes, tactical items) — MEDIUM priority
   - Equipment Slots with unequip mechanics — LOW priority
   - Critical Hits & Dodge RNG variance — MEDIUM priority

2. **Content Expansions**
   - 5 new enemy archetypes (Goblin Shaman, Wraith, Stone Golem, Vampire Lord, Mimic)
   - Boss Phase 2 mechanic (enrage at 40% HP)
   - Elite enemy variants (5% rare spawn, +50% stats)
   - 15+ new items across weapon/armor/accessory tiers

3. **Balance Improvements**
   - Dynamic enemy scaling formula (base stats + 12% per player level)
   - Loot tier progression (scales with player level)
   - Difficulty curve fixes (Troll regen, boss telegraphs, level-up nerf)
   - Gold sink systems (Shrines or Merchant rooms)

**Key Design Decisions:**
- Status effects as foundation for all future mechanics (DOTs, buffs, debuffs)
- Mana resource prevents skill spam, creates resource management gameplay
- Enemy scaling maintains challenge throughout dungeon (no trivial encounters)
- Loot progression prevents "vendor trash" feeling in late game
- Milestone rewards (skills unlocked at L3/5/8/10) create aspirational goals

**Implementation Priority:**
1. Sprint 1: Test infrastructure (blocks all features)
2. Sprint 2: Status effects + consumables + crit/dodge
3. Sprint 3: Skill system + boss phase 2
4. Sprint 4: New enemies + scaling + loot tiers
5. Sprint 5: Equipment slots + economy sinks

**Coordination Required:**
- Hill: Player model changes (Mana, MaxMana, ActiveEffects, EquipmentSlots), enemy spawning logic for new types
- Romanoff: Test coverage for status effects, skill cooldowns, loot scaling
- All: Design review ceremony before Sprint 2 to lock contracts

**File Created:** `.ai-team/decisions/inbox/barton-v2-systems-proposal.md`

**Design Philosophy Applied:**
- Systems should be data-driven (loot tables, enemy stats, skill configs)
- Combat should feel decisive (avoid endless attrition via burst mechanics)
- Enemy variety over quantity (5 new types with unique mechanics > 20 stat clones)
- Every mechanic has counter-play (Poison counters Troll regen, Weakened counters Vampire lifesteal)

### 2026-02-20: Status Effects System Foundation (#12)

**Files Created:**
- `Models/StatusEffect.cs` — enum for 6 status effects (Poison, Bleed, Stun, Regen, Fortified, Weakened)
- `Models/ActiveEffect.cs` — tracks effect type and remaining duration
- `Systems/StatusEffectManager.cs` — manages applying, processing, and removing effects

**Architecture Decisions:**
- Status effects are data-driven: each effect has predefined damage/heal/modifier values
- Effects stored per-target in dictionary (supports both Player and Enemy)
- ProcessTurnStart handles all DOT/HOT/duration logic at combat turn start
- Debuffs (Poison, Bleed, Stun, Weakened) removable by Antidote consumable
- Buffs (Regen, Fortified) cannot be removed by Antidote

**Effect Specifications:**
- Poison: 3 damage/turn, 3 turns
- Bleed: 5 damage/turn, 2 turns
- Stun: skip turn, 1 turn
- Regen: +4 HP/turn, 3 turns
- Fortified: +50% DEF, 2 turns
- Weakened: -50% ATK, 2 turns

**Integration Points:**
- StatusEffectManager needs wiring into CombatEngine constructor
- ProcessTurnStart must be called at combat loop start
- Stat modifiers (Fortified/Weakened) need integration into damage calculations
- Stun check required before player/enemy actions
- Antidote item needs handling in GameLoop.HandleUse()
- DisplayActiveEffects feedback should be added to combat loop

**Next Steps:**
- Wire StatusEffectManager into CombatEngine and GameLoop
- Update damage formulas to respect stat modifiers
- Add stun checks before attack actions
- Implement Antidote consumable usage
- Add active effects display during combat

📌 Team update (2026-02-20): Status Effects System consolidated — Barton + Coulson. Finalized design: Enum-based types, duration tracking, dictionary storage, on-demand stat modifiers. 6 core effects (Poison, Bleed, Stun, Regen, Fortified, Weakened).

📌 Team update (2026-02-20): Ability System Architecture decision merged — Barton. Confirmed in-memory data structure approach (List<Ability>) for 4 fixed abilities with hardcoded definitions in AbilityManager constructor. Migration to JSON config flagged as future consideration if ability count exceeds 10 or balance tuning becomes non-developer responsibility.
### 2026-02-20: Combat Abilities System (#13)

**Files Created:**
- `Models/Ability.cs` — Ability class with Name, ManaCost, CooldownTurns, UnlockLevel, Type
- `Systems/AbilityManager.cs` — Manages ability unlocking, cooldowns, and execution
- `Dungnz.Tests/AbilityManagerTests.cs` — Comprehensive tests for ability mechanics
- `Dungnz.Tests/PlayerManaTests.cs` — Tests for mana system

**Files Modified:**
- `Models/Player.cs` — Added Mana/MaxMana properties (starts 30, +10/level), SpendMana/RestoreMana methods
- `Engine/CombatEngine.cs` — Integrated AbilityManager, added mana regen (+10/turn), ability menu, status effect processing per turn
- `Engine/EnemyFactory.cs` — Fixed Goblin constructor call (no itemConfig parameter)

**Ability Specifications:**
- Power Strike (L1): 10mp, 2-turn CD, 2x normal damage
- Defensive Stance (L3): 8mp, 3-turn CD, applies Fortified for 2 turns (+50% DEF via StatusEffectManager)
- Poison Dart (L5): 12mp, 4-turn CD, applies Poison status effect
- Second Wind (L7): 15mp, 5-turn CD, heals 30% MaxHP

**Combat Loop Changes:**
- Menu expanded from [A]ttack [F]lee to [A]ttack [B]ability [I]tem [F]lee
- Added mana display (Mana: X/Y) when player has unlocked abilities
- Ability submenu shows unlocked abilities with availability (grayed out if on cooldown or insufficient mana)
- Cooldowns tick at start of each combat turn
- Mana regenerates +10 at start of each turn
- Status effects process at turn start for both player and enemy
- Stun effect blocks player/enemy action for that turn

**Architecture Decisions:**
- Abilities stored in AbilityManager as List<Ability> rather than config files (simpler for initial implementation)
- Cooldowns tracked per-ability-type in dictionary, persist across combat (lifetime of AbilityManager instance)
- UseAbilityResult enum returns success/failure states (InvalidAbility, NotUnlocked, OnCooldown, InsufficientMana, Success)
- AbilityManager injected into CombatEngine constructor (optional, defaults to new instance)
- Ability effects directly call StatusEffectManager, Player methods, or Enemy HP modification

**Integration with Existing Systems:**
- Status effects already implemented (#12) — Defensive Stance uses Fortified, Poison Dart uses Poison
- CombatEngine already processes status effects at turn start
- Player.LevelUp() now restores mana to full and increases MaxMana by 10

**Testing:**
- 23 unit tests covering:
  - Mana spending/restoring/leveling (10 tests)
  - Ability unlocking by level (4 tests)
  - Cooldown tracking and ticking (2 tests)
  - Ability execution and effects (4 tests)
  - Failure cases: insufficient mana, on cooldown, not unlocked (3 tests)

**Build Status:**
- Build succeeded with 1 warning (SaveSystem nullability)
- All new tests pass
- PR created: #35

**Next Steps:**
- Combat loop could be further refactored to extract ability/item menu logic into separate classes
- Ability costs/cooldowns could be moved to JSON config for easier balance tuning
- Consider adding ability descriptions to combat menu (currently only shown in submenu)

### 2026-02-20: v3 Planning Session — Systems Gap Analysis

**Context:** v2 complete. Conducting v3 roadmap planning from systems perspective to identify combat/dungeon depth expansions.

**v2 Achievements:**
- Stable combat engine with RNG (crits 20%, dodge DEF-based)
- Status effects system with 6 core types (Poison, Bleed, Stun, Regen, Fortified, Weakened)
- Ability system with 4 abilities (Power Strike L1, Defensive Stance L3, Poison Dart L5, Second Wind L7)
- 9 enemy types with varying mechanics (Mimic ambush, Wraith flat dodge, Vampire lifesteal, DarkKnight scaling)
- Boss Phase 2 enrage at 40% HP + telegraphed charge (3x damage)
- Dungeon generation: 5×4 grid, ~60% enemy rooms, ~30% item rooms, 15% shrine rooms
- Shrine economy: 4 purchasable effects (Heal, Bless, Fortify, Meditate) for 30-75g
- 5 achievements tracking (Glass Cannon, Untouchable, Hoarder, Elite Hunter, Speed Runner)
- Elite variant system: 5% spawn rate, +50% stats (no special abilities)

**Critical Gaps Identified:**

1. **Static Enemy Behavior**
   - All non-boss enemies perform identical action: attack if possible
   - No tactical decision-making or context awareness
   - Abilities like "Troll should regen" or "Vampire should lifesteal" lack AI trigger logic
   - **System Impact:** Combat against 10th Goblin feels identical to 1st Goblin; no strategic depth

2. **Single Boss Archetype**
   - DungeonBoss is only boss type; variation comes only from scaling + enrage
   - No thematic boss variety (elemental, summoner, resurrector, void entity)
   - Phase transitions are limited to single enrage event
   - **System Impact:** Final encounter lacks personality; players don't fear specific boss types

3. **Passive Dungeon Environments**
   - Rooms have flavor text but no mechanical impact on combat
   - No environmental hazards (traps, fire, poisonous fog, falling blocks)
   - No dynamic events that change during exploration
   - **System Impact:** Room location doesn't affect strategy; combat outcomes identical regardless of setting

4. **One-Size-Fits-All Difficulty**
   - Fixed difficulty curve via scaling formula; no accessibility modes
   - No hardcore challenge variants beyond elite spawn chance
   - Elite variants are crude (flat +50% stats, no special abilities)
   - **System Impact:** Casual players feel overwhelmed; hardcore players lack engaging challenge variations

5. **Shallow Economy**
   - Shrines sparse (15% spawn) and limited (4 purchasable effects)
   - No merchant/shop system for consumable purchases or item progression
   - No crafting or upgrade paths beyond equipment swapping
   - **System Impact:** Gold collected but purposeless; no meaningful economic decisions

6. **Generic Item Progression**
   - Random item drops; no progression tiers or item families
   - No unique/legendary items with special mechanics
   - No transmog or upgrade progression paths
   - **System Impact:** Looting feels random; no aspirational item hunting

**v3 Proposed Issues (8 Features):**

| Priority | Issue | Wave | Agent | Rationale |
|----------|-------|------|-------|-----------|
| 1 | Difficulty Modes & Scaling | Foundation | Barton | Enables balanced testing for other features; accessibility |
| 2 | Enemy AI Behaviors | Core | Barton | Highest impact on combat feel; each enemy type uses unique tactics |
| 3 | Boss Variety (3-4 archetypes) | Core | Barton | Final encounter variety; memorable encounters |
| 4 | Environmental Hazards | Core | Barton | Dungeon depth; dynamic combat zones |
| 5 | Elite Variants (with abilities) | Core | Barton | Elite encounters feel special; higher loot/XP reward |
| 6 | Merchant Shop System | Advanced | Coulson/Hill + Barton | Gold becomes meaningful; item progression agency |
| 7 | Procedural Room Types | Advanced | Hill/Coulson + Barton | Thematic exploration; reward/risk tension |
| 8 | Advanced Status Effects | Advanced | Barton | Elemental theming; strategic effect interactions |

**Design Philosophy for v3:**

1. **Behavior Over New Mechanics:** Add combat depth via enemy AI decision-making, not new status effects. Current effects (Poison, Stun, Regen) support rich interactions when AI uses them tactically.

2. **Hazards Damage Both Sides:** Environmental hazards affect player AND enemies equally. Rebalances combat without nerfing; creates resource depletion for both.

3. **Difficulty as Foundation:** Difficulty modes are prerequisite. All future balance (elite rates, boss health, item shop prices) keys off difficulty setting.

4. **Economy Scales with Power:** Merchants and shops don't break economy. Consumable prices scale; core progression remains combat-based.

5. **Room Types Add Flavor + Mechanics:** Libraries grant story; Armories guarantee gear; Miniboss Chambers add challenge; Treasury offers risk/reward.

**Architecture Implications:**

- **Enemy.GetAction():** New method for AI decision logic. CombatEngine calls instead of always attacking.
- **Boss Subclasses:** Need Phase property (int), phase-specific attack methods, HP threshold triggers
- **Room.Hazard:** New property (Hazard type); CombatEngine processes at turn start
- **DifficultyMode:** Global enum/class affecting EnemyFactory.CreateScaled, elite spawn rates, item shop availability
- **StatusEffectManager Extension:** May need group effect support (Volcano hazard applying Burn to all)

**Spike Questions (Design Review needed):**

- Should Elite Ability system use random selection or difficulty-seeded determinism?
- Do hazards apply at start of combat or only when entering room mid-combat?
- Should boss phases reset HP per phase or scale from current HP?
- How do Merchant prices scale with player level? (Linear? Exponential?)

**File Created:** `.ai-team/decisions/inbox/barton-v3-planning.md` — comprehensive roadmap with wave timing, dependencies, and testing strategy.

### 2026-02-20: Pre-v3 Bug Hunt - Combat System Review

**Context:** Requested by Copilot to review Engine/ and Systems/Enemies/ for combat logic bugs before v3 development begins.

**Scope:** CombatEngine.cs, EnemyFactory.cs, DungeonGenerator.cs, StatusEffectManager.cs, all enemy implementations, boss Phase 2 mechanics, status effect interactions.

**Bugs Found:** 14 total (2 Critical, 3 High, 6 Medium, 3 Low)

**Critical Bugs:**
1. **Status effect stat modifiers never applied** (CombatEngine.cs:248,294) — `GetStatModifier()` implemented but NEVER CALLED in damage calculations. Fortified/Weakened have zero gameplay impact. Fix: Integrate `_statusEffects.GetStatModifier(target, "Attack"|"Defense")` into damage formulas.

2. **Poison-on-hit mechanic inverted** (CombatEngine.cs:259-260) — GoblinShaman's poison triggers when PLAYER attacks Shaman, not when Shaman hits player. Player poisons themselves. Fix: Move logic from PerformPlayerAttack() to PerformEnemyTurn() after enemy damage dealt.

**High-Severity Bugs:**
3. **Half enemy roster inaccessible** (DungeonGenerator.cs:114) — Generator only spawns 4 of 9 enemy types (Goblin, Skeleton, Troll, DarkKnight). GoblinShaman, StoneGolem, Wraith, VampireLord, Mimic never appear. Fix: Update enemy type array or use EnemyFactory.CreateRandom().

4. **Stun double-handling fragility** (StatusEffectManager.cs:67-69, CombatEngine.cs:108-114) — Stun logic split between StatusEffectManager (displays message) and CombatEngine (enforces skip). Unclear responsibility. Fix: Remove stun message from ProcessTurnStart(), let CombatEngine own skip logic.

5. **Boss enrage multiplier compounds if status cleared** (DungeonBoss.cs:98) — Enrage multiplies current Attack, not base Attack. If boss heals above 40% or IsEnraged flag resets, re-enrage applies 1.5x to already-enraged value (2.25x total). Fix: Store base attack, always calculate enrage from base.

**Medium-Severity Bugs:**
6. **Boss charge flag sticks if player dodges** (CombatEngine.cs:296-302) — ChargeActive reset only happens if attack lands. If player dodges charged attack, ChargeActive stays true, all future attacks deal 3x damage. Fix: Reset ChargeActive BEFORE dodge check.

7. **Boss enrage delayed to next turn** (CombatEngine.cs:91-97) — CheckEnrage() called at turn start, not after damage dealt. Burst damage can drop boss below 40% without triggering enrage until next turn. Fix: Move CheckEnrage() to PerformPlayerAttack() immediately after damage.

8. **Boss telegraph gives free turn** (CombatEngine.cs:281-286) — Charge telegraph turn: boss doesn't attack, player gets mana/cooldowns/status ticks. Unclear if intentional counterplay window or bug. Fix: Either remove return (boss attacks AND telegraphs) or move telegraph before turn processing.

9. **Mimic ambush bypasses turn processing** (CombatEngine.cs:74-80) — Ambush executes before main loop, skips status ticks/mana regen on turn 1. Fix: Move ambush into main loop after turn processing.

10. **Elite multiplier stacking risk** (EnemyFactory.cs:67-71) — Elite 1.5x applied in CreateRandom() after config stats loaded. If caller chains CreateRandom() → CreateScaled(), multipliers stack. Fix: Pass isElite flag to CreateScaled(), integrate into scalar.

11. **Poison-on-hit wrong immunity check** (CombatEngine.cs:259) — Checks enemy.IsImmuneToEffects when applying poison to player. Symptom of Bug #2.

**Low-Severity Issues:**
12. **Crit chance documentation mismatch** (CombatEngine.cs:366) — Code implements 15% crit, docs say 20%. Unclear if intentional balance change.

13. **PathExists() dead code** (DungeonGenerator.cs:156-160) — Full grid always connected, check always returns true. Safety net for future partial grids or should be removed.

14. **Rectangular grid limitation** (DungeonGenerator.cs:80-102) — Generator creates only full grids, no layout variety. Not a bug, flagged for v3 planning.

**Key Learnings:**
- Status effect integration incomplete — modifiers calculated but never consumed
- GoblinShaman enemy design completely broken (poison-on-hit inverted)
- Boss mechanics fragile — enrage/charge/telegraph have edge cases
- Enemy spawning ignores 5 of 9 types — half the v2 content inaccessible
- Timing issues with ambush, enrage checks, and status processing

**Files Analyzed:**
- Engine/CombatEngine.cs (389 lines) — damage formulas, boss mechanics, status ticks, turn structure
- Engine/EnemyFactory.cs (164 lines) — enemy creation, scaling, elite variants
- Engine/DungeonGenerator.cs (225 lines) — room generation, enemy spawning, connectivity
- Systems/StatusEffectManager.cs (131 lines) — effect application, turn processing, stat modifiers
- Systems/Enemies/*.cs (10 enemy types) — GoblinShaman poison, DungeonBoss enrage/charge, Mimic ambush

**Report Location:** `.ai-team/agents/barton/bug-report-v3-pre-release.md`

**Recommended Fix Priority:**
1. Bug #1 (stat modifiers) — blocks status effect gameplay
2. Bug #2 (poison-on-hit) — breaks GoblinShaman design
3. Bug #3 (enemy spawning) — half the roster inaccessible
4. Bug #4 (stun coupling) — fragile architecture
5. Bug #6 (charge sticking) — boss becomes unkillable
6. Rest are medium/low priority polish issues

**Testing Strategy Post-Fix:**
- Verify all 6 status effects (Poison, Bleed, Stun, Regen, Fortified, Weakened) with GetStatModifier() integration
- Test GoblinShaman poison applies when Shaman hits player, not player hitting Shaman
- Verify all 9 enemy types spawn in dungeons (not just original 4)
- Test boss enrage triggers immediately at 40% HP threshold, not delayed
- Test boss charge sequence: telegraph → charge → reset, with dodge cases
- Test Mimic ambush with pre-existing status effects from previous fights
- Test elite variants spawn via both CreateRandom() and CreateScaled() without double-scaling

### 2026-02-20: Pre-v3 Bug Hunt Session — Combat Systems Findings

📌 **Team update (2026-02-20):** Pre-v3 bug hunt identified 47 critical issues across all systems. Combat systems audit found 14 bugs:

**Critical Blockers (must fix before v3 feature work):**
1. **Status Effect Stat Modifiers Never Applied (CRITICAL):** GetStatModifier() implemented but never called in CombatEngine damage calculations — Fortified and Weakened effects have ZERO gameplay effect
2. **GoblinShaman Poison-on-Hit Inverted (CRITICAL):** Player poisons themselves when attacking Shaman, not when Shaman hits player — enemy design completely broken
3. **Half Enemy Roster Inaccessible (HIGH):** 5 of 9 enemy types never spawn (GoblinShaman, StoneGolem, Wraith, VampireLord, Mimic) — DungeonGenerator hardcoded to original 4 types
4. **Boss Enrage Multiplier Compounds (HIGH):** Enrage multiplies current Attack instead of base — re-enrage applies 2.25x instead of 1.5x

**Recommended Actions:** Fix stat modifiers, poison-on-hit, and enemy spawning before starting v3 boss variety/environmental hazard work. Boss mechanics need hardening before inheriting more complexity.

— decided by Barton (from Pre-v3 Critical Bug Hunt)

### Issue #220: ColorizeDamage — Replace Last Occurrence Fix

**File Modified:** `Engine/CombatEngine.cs`

**Problem:** `ColorizeDamage()` used `string.Replace(damageStr, coloredDamage)` which replaces ALL occurrences of the damage number in the narration string. A message like "5 damage! You deal 5!" would colorize both `5`s — including any that appear earlier in the string and don't represent the damage value.

**Fix Applied:**
- Added private static helper `ReplaceLastOccurrence(string source, string find, string replace)` using `LastIndexOf` to target only the final occurrence of the damage number.
- Updated both call sites in `ColorizeDamage()` (normal damage and crit path) to use `ReplaceLastOccurrence` instead of `string.Replace`.

**Rationale:** Damage values always appear at the end of narration strings by convention, so targeting the last occurrence is semantically correct and avoids false colorization of coincidental number matches earlier in the message.

**Build/Test Status:** Build succeeded (0 errors), all 267 existing tests pass.

**PR:** #223 — `squad/220-colorize-damage-fix`
