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
