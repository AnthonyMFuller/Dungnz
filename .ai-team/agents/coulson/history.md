# Coulson — History

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Learnings

### 2026-02-20: Design Review Ceremony
**Key Contracts Agreed:**
- CombatEngine.StartCombat(Player, Enemy) → CombatResult {Won, Fled, PlayerDied} — blocking call, Barton implements, Hill invokes
- DisplayService injected into CombatEngine via constructor; all console output routed through DisplayService (no raw Console.Write)
- InventoryManager exposes TakeItem/UseItem/EquipItem methods returning bool/UseResult enum (no exceptions for missing items)
- LootTable.RollDrop(Enemy) → LootResult {Item? item, int gold} — separates gold from item drops
- Models owned by Hill: Player (HP, MaxHP, Attack, Defense, Level, Gold, XP, InventorySlots, EquippedWeapon, EquippedArmor), Enemy base (HP, MaxHP, Attack, Defense, Name, Type enum, XPReward, GoldReward), Item (Name, Type enum, Value, AttackBonus, DefenseBonus, HealAmount)
- Room holds single nullable Enemy? reference; GameLoop nulls it after enemy death (HP <= 0)
- Enemy subclasses (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss) owned by Barton

**Risks Raised:**
- Hill/Barton: Player death during flee must return PlayerDied, not Fled (CombatEngine responsibility)
- Hill: GameLoop picks random adjacent room after successful flee
- Barton: Equipping mid-combat applies stat bonuses immediately; DisplayService shows updated stats
- Barton: Dungeon Boss drops loot only once (Room "looted" flag or LootTable state tracking)
- Hill: Enemy factory pattern deferred unless constructor complexity requires it

**Build Order:**
1. Hill implements Models + DisplayService first (Phase 1a)
2. Barton implements Enemy subclasses + systems (Phase 1b, depends on 1a)
3. Parallel work: Hill (DungeonGenerator/GameLoop/CommandParser), Barton (combat/inventory/loot logic)

---

## 2026-02-20: Retrospective Ceremony

**Facilitator:** Coulson  
**Participants:** Coulson, Hill, Barton, Romanoff, Scribe  
**Context:** Team retrospective after completing TextGame v1 (shipped, code review passed)

### What Went Well
- **Design Review ceremony prevented rework** — Pre-build interface contracts enabled true parallel development with zero integration bugs
- **Code review caught issues before shipping** — 7 architectural violations and logic bugs fixed before v1 release
- **Clean architecture with clear boundaries** — Model ownership and contract-first design paid dividends

### Critical Issues Identified
1. **No automated test coverage** — Zero unit tests, high regression risk for any future work
2. **Player model lacks encapsulation** — Public setters allow invalid state, blocks save/load and multiplayer
3. **RNG not injectable** — CombatEngine/LootTable create own Random instances, prevents deterministic testing
4. **DisplayService coupled to Console** — No interface, blocks headless testing and alternative UIs
5. **Architectural violation persists** — CombatEngine still has direct Console.ReadLine() call

### Key Decisions Made
- **D1: Test Infrastructure Required for v2** — Unit test framework, injectable Random, IDisplayService interface required before v2 features
- **D2: Player Encapsulation Refactor** — Private setters, validation, public methods (TakeDamage, Heal, etc.) before save/load work
- **D3: DisplayService Interface Extraction** — Extract IDisplayService for testability and future UI options

### Action Items Assigned
- Hill: Add defensive null checks in GameLoop constructor (immediate)
- Barton: Fix CombatEngine Console.ReadLine() violation (immediate)
- Coulson: Create v2 planning ceremony agenda (next)
- Romanoff: Document WI-10 edge cases as future test cases (next)
- Hill: Player encapsulation refactor (before v2 save/load)
- Hill + Barton: IDisplayService extraction (before v2 testing)
- Barton: Injectable Random refactor (before v2 testing)

### Risks
- **HIGH regression risk** without automated tests
- State integrity risk from Player public setters
- Testability blocked by tight Console coupling

**Outcome:** Team aligned on v2 priorities. Test infrastructure and encapsulation refactors must precede new features. Ceremony summary written to `.ai-team/log/2026-02-20-retrospective.md`. Decisions written to inbox for Scribe merge.

---

### 2026-02-20: v2 Architecture Planning
**Objective:** Produce comprehensive v2 plan covering refactoring priorities, architecture improvements, and work decomposition.

**Key Architectural Patterns Established:**
- Interface extraction for testability (IDisplayService, IRandom, IInputService)
- Encapsulation over public setters for domain models
- Constructor dependency injection (avoid service locator)
- Result enums over exceptions for expected failures
- Event-driven architecture for extensibility (GameEvents)
- Configuration-driven balance tuning (appsettings.json)

**Phase Structure:**
- **Phase 0 (Critical Refactoring):** 8 work items, 14.5 hours — Must complete before any v2 features
- **Phase 1 (Test Infrastructure):** 7 work items, 16.5 hours — xUnit framework, >70% coverage target
- **Phase 2 (Architecture):** 7 work items, 22 hours — GameState model, save/load, event system, config system
- **Phase 3 (Features):** 6 work items, 25 hours — Save/load commands, equipment slots, status effects, multi-floor dungeons

**Critical Path Identified:**
1. IDisplayService extraction (R1) enables test mocking and CombatEngine input fix
2. Player encapsulation (R3) blocks save/load and equipment slot features
3. Injectable Random (R4) enables deterministic testing of combat/loot
4. All Phase 0 refactors must complete before Phase 1 testing begins

**Work Allocation:**
- Hill: 36 hours (Models, GameLoop, persistence, configuration)
- Barton: 25.5 hours (Combat, inventory, systems)
- Romanoff: 16.5 hours (Test infrastructure, all test coverage)

**Deliverable:** `.ai-team/plans/v2-architecture-plan.md` — 28 work items tracked, full dependency graph, acceptance criteria for each phase

**Next Actions:** Present to team for approval, kickoff Phase 0 design review ceremony

---

### 2026-02-20: IDisplayService Interface Extraction (Phase 0 Gate)
**Context:** First critical refactor from v2 planning — extract testability layer for display subsystem

**Implementation:**
- Created `IDisplayService` interface with 14 public methods (all operations from DisplayService)
- Renamed `DisplayService` → `ConsoleDisplayService : IDisplayService` (removed virtual modifiers, now concrete implementation)
- Updated `GameLoop` and `CombatEngine` constructors to accept `IDisplayService` instead of concrete class
- Created `TestDisplayService` in test project — headless stub capturing output for assertions
- Replaced obsolete `FakeDisplayService` (inheritance-based test double) with composition-based `TestDisplayService`
- Fixed test project targeting .NET 10 (SDK only supports .NET 9)
- Removed orphaned `InventoryManagerTests.cs` (InventoryManager no longer exists)

**Key Files:**
- `/Display/IDisplayService.cs` — New interface contract
- `/Display/DisplayService.cs` — Renamed to ConsoleDisplayService
- `/Engine/GameLoop.cs` — Constructor now accepts IDisplayService
- `/Engine/CombatEngine.cs` — Constructor now accepts IDisplayService
- `/Program.cs` — Updated to instantiate ConsoleDisplayService
- `/Dungnz.Tests/Helpers/TestDisplayService.cs` — New test double

**Build Verification:**
- Clean build: ✅ No errors, no warnings
- Test suite: ✅ All 125 tests pass (0.8s runtime)
- No regressions introduced

**Architecture Decision:**
- Favor interface-based dependency injection over inheritance-based test doubles
- TestDisplayService implements IDisplayService directly rather than extending concrete class
- This unblocks future DisplayService implementations (JSON logger, TUI, web sockets) without breaking existing consumers

**Blockers Removed:**
- CombatEngine can now be tested headlessly (WI-2 unblocked)
- GameLoop can now be tested without Console coupling (existing tests already use this pattern)
- Alternative UI implementations (WI-XX future) can now be plugged in via DI

---

### 2026-02-20: IDisplayService Integration Complete (GitHub #1, PR #27)
**Context:** Final integration fix for IDisplayService extraction — Program.cs still referenced old DisplayService class name

**Root Cause:**
- IDisplayService interface extraction was completed in commit 32184c6 (test infrastructure work)
- DisplayService renamed to ConsoleDisplayService implementing IDisplayService
- GameLoop/CombatEngine constructors already updated to accept IDisplayService
- TestDisplayService test double already created
- BUT: Program.cs still instantiated `new DisplayService()` instead of `new ConsoleDisplayService()`
- This caused build failure: "DisplayService not found" — the class was renamed but entrypoint wasn't updated

**Fix:**
- Updated Program.cs line 5: `var display = new ConsoleDisplayService();`
- Clean build, all 125 tests pass
- PR #27 created against master branch

**Lesson Learned:**
- Interface extraction completed incrementally across multiple commits can leave integration points inconsistent
- Entrypoint files (Program.cs, Main methods) are often overlooked during refactoring sweeps
- Always verify build from clean state after interface extraction, not just test suite
- Commit 32184c6 did the heavy lifting but didn't update the production entrypoint

**Files:**
- `/Program.cs` — Fixed instantiation to use ConsoleDisplayService
- `/Display/IDisplayService.cs` — Interface contract (14 methods)
- `/Display/DisplayService.cs` — Renamed to ConsoleDisplayService (commit 32184c6)
- `/Dungnz.Tests/Helpers/TestDisplayService.cs` — Test double for headless testing

---

### 2026-02-20: GameEvents System Implementation (Issue #11, PR #30)
**Context:** Implemented injectable event system for game-wide notifications

**Architecture Decisions:**
- GameEvents as instance-based class (not static singleton) for testability and dependency injection
- Nullable GameEvents? parameter pattern - events are optional, no mandatory subscribers
- Strongly-typed EventArgs subclasses: CombatEndedEventArgs, ItemPickedEventArgs, LevelUpEventArgs, RoomEnteredEventArgs
- Events fire AFTER state changes complete (e.g., RaiseCombatEnded after loot awarded)
- RoomEnteredEventArgs includes previousRoom reference for navigation tracking

**Implementation:**
- Systems/GameEvents.cs — Event declarations and Raise* methods
- Systems/GameEventArgs.cs — Custom EventArgs types with relevant context
- CombatEngine fires: OnCombatEnded (Won/Fled/PlayerDied), OnLevelUp (with old/new level)
- GameLoop fires: OnRoomEntered (with previousRoom), OnItemPicked (with room context)
- Program.cs instantiates GameEvents once, injects into both CombatEngine and GameLoop

**Key Files:**
- `/Systems/GameEvents.cs` — Event system core, 4 typed events
- `/Systems/GameEventArgs.cs` — EventArgs definitions
- `/Engine/CombatEngine.cs` — Fires combat and level-up events
- `/Engine/GameLoop.cs` — Fires room and item events

**Pattern Established:**
- Optional event subscribers via nullable injected instance
- No tight coupling — consumers subscribe only if needed
- Clean separation: game logic unaware of subscribers, events fire unconditionally

📌 Team update (2026-02-20): Interface Extraction & Refactoring Verification consolidated — Coulson + Hill. Added entrypoint verification checklist to catch regressions where tests pass but production code fails.

📌 Team update (2026-02-20): GameEvents Event System Architecture established — instance-based events with nullable DI for testability.

---

## 2026-02-20: v3 Planning Session & Architecture Analysis

**Facilitator:** Coulson  
**Context:** v2 Complete (28 work items, 91.86% coverage, 5 waves). Planning v3 foundation and feature roadmap.

### v2 Assessment
- **Codebase Health:** Excellent — clean layering, interface-based DI, >90% test coverage
- **Architecture:** Models/Engine/Systems/Display separation clean; IDisplayService extraction successful
- **Known Scalability Issues:** Player.cs 273 LOC mixing 7 concerns; Equipment/Inventory tied to Player; StatusEffects hardcoded; no integration tests

### Architectural Concerns Identified (v3 Blockers)

**1. Player Model Decomposition**
- Problem: 273 LOC mixing HP, Mana, XP, Gold, Inventory, Equipment, Abilities — violates SRP
- Impact: Adding character classes, shops, crafting requires refactoring without breaking saves
- Decision: Split into PlayerStats (combat), PlayerInventory (items), PlayerCombat (abilities/cooldowns) in Wave 1

**2. Equipment System Fragmentation**
- Problem: EquipItem/UnequipItem in Player; ApplyStatBonuses/RemoveStatBonuses private; no equipment config
- Impact: Can't build shops, merchants, or equipment trading without refactoring
- Decision: Create EquipmentManager; extract equipment to config (like items/enemies)

**3. Test Coverage Gaps (Integration)**
- Problem: 91.86% unit coverage but zero integration tests for multi-system flows
- Impact: CombatEngine→LootTable→Equipment interactions untested; refactoring risks regressions
- Decision: Create integration test suite covering combat→loot→equipment, status→save/load, ability→cooldown chains

**4. Status Effect System Lacks Composition**
- Problem: 6 effects hardcoded in StatusEffectManager; no config, no stacking, no extensibility
- Impact: Elemental system, effect combos, custom effects blocked
- Decision: Refactor to IStatusEffect interface + config system (v3 foundational, v4 elemental ready)

**5. Inventory Management Needs Validation**
- Problem: List<Item> with no weight/slot limits; item logic scattered across systems
- Impact: Shop systems can't validate constraints; duping bugs possible
- Decision: Create InventoryManager with centralized add/remove/validate logic

**6. Ability System Too Combat-Focused**
- Problem: AbilityManager tied to combat only; no passive abilities or trait system
- Impact: Skill trees (v4) require major redesign
- Decision: Extend AbilityManager to support passive abilities + cooldown groups in v3

**7. Character Class Architecture Missing**
- Problem: All players same stats/abilities/playstyles; no class concept
- Impact: v3 feature "class selection" has no foundation
- Decision: Design ClassDefinition config system + ClassManager (depends on Player decomposition)

**8. Save System Fragility**
- Problem: Couples to current Player structure; no version tracking or migration path
- Impact: Player.cs decomposition breaks all existing saves
- Decision: Add SaveFormatVersion + migration logic before refactoring Player

### v3 Feature Feasibility

**Feasible (After Foundation Work)**
- Character classes (config-driven) — requires Player decomposition + ClassManager
- Shop/merchant system — requires Equipment refactoring + InventoryManager
- Basic crafting — requires InventoryManager + Recipe config
- Achievement expansion (skill-based) — independent, low risk

**Questionable (Defer to v4)**
- Skill trees (complex Ability redesign required)
- Elemental damage (Status effect composition needed)
- Multiplayer/PvP (no session/lobby system)
- Permadeath (SaveSystem too fragile for mode switching)

### v3 Recommended Wave Structure

**Wave 1 (Foundation):** Player decomposition, Equipment/Inventory managers, integration testing, SaveSystem migration  
**Wave 2 (Systems):** Character classes, Ability expansion, Achievement expansion  
**Wave 3 (Features):** Shops, Crafting system  
**Wave 4 (Polish):** New enemy types, Shrine upgrades, Difficulty tuning  

**Critical Path:** Player decomposition → SaveSystem migration → Integration tests  
**Risk:** High (refactoring critical path); Mitigation: Feature flags, parallel implementations, integration tests

### Architectural Patterns to Enforce (v3+)

**Pattern 1: Configuration-Driven Entities**
- Rule: All extensible entities (Classes, Abilities, StatusEffects, Shops) defined in config, not hardcoded
- Precedent: ItemConfig, EnemyConfig established in v2
- Apply to: Classes, Abilities, StatusEffects, Shop inventories

**Pattern 2: Composition Over Inheritance**
- Rule: Use IStatusEffect, IAbility interfaces; compose abilities from components
- Avoid: Inheritance hierarchies for combat vs. passive vs. class-specific abilities
- Benefit: Reduces branching logic; enables effect combos and ability chaining

**Pattern 3: Manager Pattern for Subsystems**
- Rule: Each major subsystem owns a manager (EquipmentManager, InventoryManager, ClassManager, CraftingManager)
- Responsibility: Manager validates state, applies side effects, fires events
- DI: All managers receive config, Random, GameEvents via constructor (no static dependencies)

**Pattern 4: Event-Based Cross-System Communication**
- Rule: Systems don't call each other directly; fire events and subscribe (GameEvents pattern)
- Extend with: EquipmentChanged, ItemCrafted, ClassSelected, AchievementEarned
- Benefit: Decouples features; future systems (UI, mods) consume events without touching core logic

**Pattern 5: Design Review Before Coding**
- Rule: Each Wave starts with architecture ceremony; no coding without signed-off design
- Precedent: v1 Design Review, v2 Planning ceremony successful
- Benefit: Prevents rework, integration bugs; establishes contracts early

### Integration Strategy (No Breaking Changes)

**Principle 1: Backward Compatibility for Saves**
- SaveSystem supports v2 and v3 formats simultaneously
- Migration on load; no data loss
- Two-release deprecation window if data format changes needed

**Principle 2: Feature Flags for Risky Refactoring**
- New systems run in parallel with old (EquipmentManager + Player equipment slots)
- Old behavior unchanged; gradual migration
- Classes opt-in; existing saves load as "unclassed"

**Principle 3: Incremental Merging**
- One subsystem per PR (PlayerStats → PlayerInventory → PlayerCombat in separate PRs)
- Full integration tested in final Wave 1 PR before merge to master
- Prevents megacommit integration issues

**Principle 4: Regression Testing Mandatory**
- Integration test suite must precede decomposition work
- Multi-system flows tested before any Player.cs refactoring begins
- All tests passing before merging to master

### v3 Scope Decision

**IN Scope (Must Have)**
- Player.cs decomposition (PlayerStats, PlayerInventory, PlayerCombat)
- EquipmentManager + equipment config system
- InventoryManager with validation
- Integration test suite (multi-system flows)
- SaveSystem migration + version tracking
- Character classes (config-driven, 5 classes)
- Shop system with NPC merchants
- Basic crafting system

**OUT of Scope (v4 or Later)**
- Skill trees (requires stable Player + Ability architecture)
- Permadeath/hardcore modes (SaveSystem too fragile)
- Multiplayer/lobbies (no session system)
- Elemental damage system (Status effect composition needed)

### Success Criteria for v3

**Wave 1 (Foundation):** Player decomposed (<150 LOC each module); EquipmentManager/InventoryManager created; integration tests cover 10+ multi-system flows; SaveSystem migration working; >90% coverage

**Wave 2 (Systems):** 5 classes defined in config; ClassManager working; Ability expansion complete; class-based achievements; >90% coverage

**Wave 3 (Features):** Shop system with 3+ merchants; Crafting system with 5+ recipes; economy balanced; >90% coverage

**Wave 4 (Content):** 8 new enemy types; new shrine types; difficulty curves balanced; no v2 regressions

### Team Assignments

- **Hill:** Player decomposition, ClassManager, SaveSystem migration, Shop architecture (~35 hours)
- **Barton:** EquipmentManager, InventoryManager, Crafting, new enemies, ability expansion (~40 hours)
- **Romanoff:** Integration tests, edge cases, balancing, difficulty curves (~30 hours)

### Key Decision: Sequential Waves Over Parallel

**Decision:** Execute Waves sequentially (1→2→3→4), not in parallel.  
**Rationale:** Wave 2 depends on Wave 1 (Player decomposition); Wave 3 depends on Wave 2 (Classes for class-specific shops); parallelizing increases rework risk.  
**Benefit:** Clear dependencies, easier communication, cleaner merges.  
**Timeline:** ~12-14 weeks for all 4 waves at 1 wave/2-3 weeks pace.

### Lessons Learned (v3 Planning)

1. **Architecture debt compounds:** Small SRP violations (Player.cs) become major blockers (can't add classes without refactoring).
2. **Integration tests are force multipliers:** Test coverage alone insufficient; need multi-system flow tests to refactor safely.
3. **Config-driven design enables iteration:** ItemConfig/EnemyConfig patterns should extend to all extensible systems (Classes, StatusEffects).
4. **Feature flags are refactoring insurance:** Parallel old/new implementations protect against bugs during decomposition.
5. **Design review ceremonies pay off:** Pre-planning prevents rework more than code review post-facto.

**Outcome:** v3 roadmap documented in `.ai-team/decisions/inbox/coulson-v3-planning.md`. Ready for team approval and Wave 1 design review ceremony kickoff.

---

## 2026-02-20: Pre-v3 Architecture Bug Hunt

**Facilitator:** Coulson  
**Context:** Pre-v3 comprehensive architecture review requested by Copilot — identify integration bugs, missing null checks, unhandled states across GameLoop ↔ CombatEngine ↔ Player ↔ SaveSystem ↔ StatusEffectManager.

### Review Scope
- **Files Reviewed:** GameLoop.cs, CombatEngine.cs, Player.cs, SaveSystem.cs, StatusEffectManager.cs, DungeonBoss.cs, EnemyFactory.cs, DungeonGenerator.cs, AbilityManager.cs, LootTable.cs, Room.cs, Enemy.cs, Item.cs, Program.cs
- **Focus Areas:** Null safety, state integrity, save/load roundtrips, status effect integration, boss mechanics, multi-floor progression

### Critical Bugs (3)
1. **Bug #2: Boss enrage compounds on modified Attack** — DungeonBoss.cs:98 multiplies Attack by 1.5 each time CheckEnrage runs; if boss HP drops below 40% multiple times (e.g., after healing), Attack compounds exponentially. Fix: Store _baseAttack and always calculate as (int)(_baseAttack * 1.5).
2. **Bug #3: Boss enrage state lost on save/load** — IsEnraged flag not serialized; after load, boss has enraged Attack value but IsEnraged=false, breaking future CheckEnrage logic. Fix: SaveSystem must serialize DungeonBoss state OR CheckEnrage must detect prior enrage (Attack != _baseAttack).
3. **Bug #6: EnemyFactory.Initialize never called** — Program.cs creates DungeonGenerator without initializing EnemyFactory config; all enemies use fallback hard-coded stats instead of Data/enemies.json. Fix: Add EnemyFactory.Initialize() before line 22.

### High Severity (4)
4. **Bug #1: Boss enrage timing issue** — CheckEnrage called at turn start (line 92) before damage dealt; boss attacks at pre-enraged value the turn threshold is crossed. Fix: Add second CheckEnrage call after player attack (line 168).
5. **Bug #4: StatusEffect stat modifiers never applied** — StatusEffectManager.GetStatModifier calculates Weakened/Fortified bonuses but CombatEngine never calls it; buffs/debuffs have no combat effect. Fix: Integrate GetStatModifier at damage calculation points (lines 248, 289, 310).
6. **Bug #11: SaveSystem missing current floor** — GameState lacks _currentFloor; player saves on Floor 3, loads as Floor 1 with Floor 3 enemy scaling (mismatch). Fix: Add CurrentFloor to SaveData.
7. **Bug #12: Shrine blessing permanent not temporary** — GameLoop line 508 applies +2 ATK/DEF via ModifyAttack/ModifyDefense with no expiration; blessing described as "5 rooms" but lasts forever. Fix: Implement StatusEffect.Blessed OR room counter in Player.

### Medium Severity (5)
8. **Bug #5: Boss charge race condition** — IsCharging set to true on charge warning turn; next turn sets ChargeActive=true but does not clear IsCharging; if random roll triggers charge again, both flags true. Fix: Clear IsCharging after setting ChargeActive.
9. **Bug #7: GameLoop null checks missing** — Run(player, startRoom) accepts nulls; _player/startRoom assigned without validation; NullReferenceException on line 71. Fix: Add ArgumentNullException guards.
10. **Bug #8: Stun message shown twice** — CombatEngine shows "cannot act" when Stun checked (line 108); ProcessTurnStart also shows "stunned" message (line 68); duplicate output. Fix: Remove Stun case from ProcessTurnStart.
11. **Bug #9: Multi-floor uses same seed** — HandleDescend creates new DungeonGenerator(_seed) with identical seed; all floors have same layout. Fix: Vary seed per floor (_seed + _currentFloor).
12. **Bug #13: StatusEffect Weakened calculates from modified stats** — Weakened penalty calculated as 50% of current Attack (including equipment); unequipping weapon breaks math. Fix: Track base stats separately OR store original modifier when effect applied.

### Low Severity (3)
13. **Bug #10: AbilityManager cooldown underflow** — TickCooldowns decrements cooldowns without floor check; cooldown can become negative (harmless but incorrect state). Fix: Clamp to 0.
14. **Bug #14: Room.Looted dead code** — Property exists but never set or checked anywhere in codebase. Fix: Remove OR implement.
15. **Bug #15: Player.OnHealthChanged unused** — Event defined and fired but no subscribers. Fix: Remove OR document as future-use.
16. **Bug #16: LootTable static item pools** — Tier item pools are static List<Item>; if item mutated after drop, affects all future drops (unlikely but possible). Fix: Clone items on drop.

### Architecture Patterns Identified
- **Missing Integration:** StatusEffectManager and CombatEngine loosely coupled; stat modifiers calculated but never consumed.
- **State Integrity Risk:** DungeonBoss mutable state (_baseAttack, IsEnraged, IsCharging) not persisted through save/load; boss mechanics fragile.
- **Incomplete Abstractions:** Player.ModifyAttack/ModifyDefense used for both permanent (equipment) and temporary (shrine) modifications without tracking duration.
- **Seed Determinism Broken:** Multi-floor progression creates new DungeonGenerator instances with same seed; identical layouts undermine replay value.

### Key File Interactions
- **CombatEngine → StatusEffectManager:** Missing GetStatModifier calls break Weakened/Fortified effects.
- **GameLoop → SaveSystem → DungeonBoss:** Boss state (IsEnraged, _baseAttack) not serialized; save/load corrupts boss encounters.
- **Program.cs → EnemyFactory:** Initialization never called; config system unused in production.
- **GameLoop → DungeonGenerator:** Seed reuse on HandleDescend creates duplicate floors.

### Recommendations for v3
1. **Pre-Wave 1:** Fix Critical bugs (#2, #3, #6) and High severity bugs (#1, #4, #11, #12) before refactoring Player.cs.
2. **SaveSystem Versioning:** Add SaveFormatVersion field to detect schema changes; migrate IsEnraged and CurrentFloor fields.
3. **StatusEffect Integration:** Complete StatusEffectManager ↔ CombatEngine integration; add integration tests for buff/debuff scenarios.
4. **Boss Mechanics Hardening:** DungeonBoss needs immutable base stats + serializable phase flags; consider extracting to BossPhaseManager.
5. **Player Stat Tracking:** Separate base stats (Level-derived) from modified stats (Equipment + Buffs) to support temporary effects correctly.

**Outcome:** 16 bugs identified and documented. Critical path blockers (#2, #3, #6) must be resolved before v3 Wave 1. Integration bugs (#4, #5, #8) indicate StatusEffectManager and boss mechanics need hardening.

### 2026-02-20: Pre-v3 Bug Hunt Session

📌 **Team update (2026-02-20):** Comprehensive pre-v3 bug hunt identified 47 critical issues across architecture, data integrity, combat logic, and persistence. Team findings:
- **Coulson:** 16 integration & state integrity bugs (boss mechanics, status effects, save/load, initialization)
- **Hill:** Encapsulation audit revealing inconsistent patterns (Player strong, Enemy/Room weak)
- **Barton:** 14 combat system bugs (status modifiers, poison logic, enemy spawning, boss mechanics)
- **Romanoff:** 7 systems bugs (SaveSystem validation, RunStats tracking, config loading, status effects on dead entities)

**Critical blockers (must fix before v3 Wave 1):** EnemyFactory initialization, boss enrage compounding, boss state persistence, status modifier integration, damage tracking, SaveSystem validation.

— decided by Coulson, Hill, Barton, Romanoff

---

## 2026-02-20: UI/UX Improvement Initiative (Boss Request)

**Facilitator:** Coulson  
**Participants:** Coulson, Hill (explore agent), Barton (explore agent)  
**Context:** Boss requested comprehensive UI/UX improvement plan to enhance visual clarity and player experience.

### Current State Analysis

**Architecture Assessment:**
- ✅ IDisplayService abstraction clean and well-separated
- ✅ ConsoleDisplayService sole concrete implementation
- ✅ TestDisplayService infrastructure for headless testing
- ✅ Consistent formatting patterns (emoji prefixes, box-drawing, indentation)

**Display Capabilities Inventory:**
- 11 interface methods covering title, room, combat, stats, inventory, messages
- Unicode box-drawing (`╔ ║ ═ ╚`) and emoji (⚔ 🏛 💧 ✗) for visual distinction
- Layout patterns: blank lines, indentation (2 spaces), bracketed comparisons
- **Critical gap:** NO color system — all output plain white text

**Combat System Display Patterns:**
- Status line: `[You: X/Y HP] vs [Enemy: X/Y HP]`
- Class-specific damage narration (Warrior/Mage/Rogue variants)
- Ability usage: 3-message pattern (flavor → effect → status)
- Boss mechanics: enrage warnings, charge telegraphs
- Emoji signaling: `⚔` combat, `💥` crit, `⚠` warning, `⚡` ability

**Systems Display Usage:**
- StatusEffectManager: per-turn damage, effect expiration messages
- InventoryManager: pickup confirmations, usage feedback
- EquipmentManager: stat lists with `string.Join`, slot states
- AchievementSystem: binary unlock display (no progress tracking)

### Critical UI/UX Gaps Identified

1. **No color system** — All text plain white; no semantic color coding
2. **No status HUD** — Active effects only shown when applied/expired (not persistent)
3. **No equipment comparison** — Equipping gear doesn't show before/after stats
4. **No progress tracking** — Achievements binary only; no hints toward unlock
5. **No inventory weight display** — Weight system exists but not visualized
6. **Limited combat clarity** — Damage/healing blend into narrative text walls
7. **No cooldown visual feedback** — Abilities show cost but not readiness state
8. **No turn log limit** — Combat log unbounded; scrolls off screen

### UI/UX Improvement Plan (3 Phases)

**Phase 1: Foundation (5-7 hours)**
- **WI-1:** Create `ColorCodes.cs` with ANSI constants and threshold helpers
- **WI-2:** Add color-aware methods to IDisplayService (`ShowColoredMessage`, `ShowColoredStat`)
- **WI-3:** Colorize core stats (HP=red, Mana=blue, Gold=yellow, XP=green, Attack=bright red, Defense=cyan)
- **Gate:** All 267 tests pass with TestDisplayService ANSI stripping

**Phase 2: Enhancement (6-8 hours)**
- **WI-4:** Combat visual hierarchy (colored damage/healing/crits)
- **WI-5:** Enhanced combat HUD with active effects: `[You: 45/60 HP | 15/30 MP | P(2) R(3)] vs [Goblin: 12/30 HP | W(2)]`
- **WI-6:** Equipment comparison display (before/after stats with colored deltas)
- **WI-7:** Inventory weight display with threshold colors
- **WI-8:** Status effect summary panel in player stats
- **Gate:** Zero regressions; all UI enhancements functional

**Phase 3: Polish (4-5 hours)**
- **WI-9:** Achievement progress tracking for locked achievements
- **WI-10:** Enhanced room descriptions with danger-based coloring
- **WI-11:** Ability cooldown visual (green=ready, gray=cooling)
- **WI-12:** Combat turn log enhancement (last 5 turns, alternating colors)
- **Gate:** Boss approval; merge to master

### Color Palette Design

**Semantic Colors:**
- Health: Red (threshold-based: green 70%+, yellow 40-69%, red 20-39%, bright red <20%)
- Mana: Blue (threshold-based: blue 50%+, cyan 20-49%, gray <20%)
- Gold: Yellow
- XP: Green
- Attack: Bright Red
- Defense: Cyan
- Success/Healing: Green
- Errors/Warnings: Red

**Equipment Rarity (future):**
- Common: White
- Uncommon: Green
- Rare: Blue
- Epic: Purple
- Legendary: Gold

**Status Effects:**
- Positive (Regen, Fortified): Green
- Negative (Poison, Weakened): Red
- Neutral (Stun, Bleed): Yellow

### Architecture Decisions

**Decision 1: ANSI Colors via DisplayService Extensions**
- **Rule:** All color logic contained in DisplayService layer; game logic never references ANSI codes
- **Rationale:** Preserves testability; maintains clean separation; enables graceful fallback
- **Pattern:** Add new methods (ShowColoredMessage, ShowColoredStat) rather than modifying existing

**Decision 2: Threshold-Based Coloring**
- **Rule:** HP/Mana use dynamic colors based on current/max ratio
- **Rationale:** Instant visual feedback on danger state; aligns with player mental model
- **Implementation:** `ColorCodes.HealthColor(current, max)` helper for reusability

**Decision 3: Accessibility-First Design**
- **Rule:** Color enhances existing semantic indicators (emoji, labels), never replaces
- **Rationale:** Color-blind players must retain full experience
- **Examples:** `ShowError()` keeps `✗` prefix even when red; combat HUD shows effect abbreviations even without color

**Decision 4: Test Infrastructure ANSI Stripping**
- **Rule:** TestDisplayService strips all ANSI codes before storing output
- **Rationale:** Existing tests check plain text content; no test rewrites needed
- **Implementation:** `StripAnsiCodes(string text)` regex helper

**Decision 5: Combat HUD Active Effects**
- **Rule:** Show active effects inline with single-letter abbreviations: P(poison), R(regen), S(stun), B(bleed), F(fortified), W(weakened)
- **Rationale:** Persistent visibility without cluttering screen; turns remaining in parentheses
- **Format:** `[You: 45/60 HP | 15/30 MP | P(2) R(3)]`

### Team Allocation

- **Hill:** ColorCodes utility, DisplayService extensions, core stat colorization, inventory/equipment display (8-10 hours)
- **Barton:** Combat hierarchy, combat HUD, status effects, ability visuals, turn log (7-9 hours)
- **Romanoff:** Test infrastructure updates, ANSI stripping verification, color utility tests (3-4 hours)
- **Coulson:** Design review (Phase 1), code review (each phase), final approval gate (2-3 hours)

**Total Estimate:** 20-26 hours

### Critical Path

```
WI-1 (ColorCodes) → WI-2 (DisplayService) → WI-3 (Core Stats)
  ├→ WI-4 (Combat) → WI-5 (HUD) → WI-12 (Turn Log)
  ├→ WI-6 (Equipment) → WI-7 (Inventory) → WI-10 (Rooms)
  └→ WI-8 (Status Panel) → WI-9 (Achievements) → WI-11 (Abilities)
```

### Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| ANSI support variance (older Windows CMD) | Auto-detect terminal capabilities; graceful fallback to emoji-only |
| Test infrastructure breakage | Strip ANSI codes in TestDisplayService before assertions |
| Color readability | Use high-contrast colors; test on multiple terminals |
| Performance impact | ANSI codes are 10-20 bytes per segment (negligible) |

### Success Criteria

- [ ] All 267 tests pass (zero regressions in game logic)
- [ ] Visual clarity: HP state, active effects, cooldowns instantly recognizable
- [ ] Information density: All actionable info visible without scrolling
- [ ] Accessibility: Color-blind players retain full experience via emoji/labels
- [ ] Performance: No noticeable slowdown in display rendering

### Deliverables

- **Architecture Plan:** `.ai-team/decisions/inbox/coulson-ui-ux-architecture.md` (20KB, full technical spec)
- **Executive Summary:** `.ai-team/decisions/inbox/coulson-ui-ux-summary.md` (4KB, at-a-glance reference)

### Next Steps

1. Team design review (present to Hill, Barton, Romanoff)
2. Boss approval (confirm scope and priorities)
3. Phase 1 kickoff (Hill implements color foundation)
4. Parallel Phase 2 work (Hill=inventory/equipment, Barton=combat/status)
5. Phase 3 polish (both engineers)
6. Final review (Coulson validates architecture before merge)

**Outcome:** Comprehensive UI/UX improvement plan addressing all display gaps identified in analysis. Clean architectural approach with zero breaking changes. Ready for team review and implementation.

— planned by Coulson with analysis from Hill, Barton

📌 Team directive (2026-02-22): No commits directly to master. All work goes on a feature branch (squad/{slug}), even without a linked issue. Reaches master only via PR. — captured by Scribe after UI/UX commit landed on master directly.

---

## 2026-02-22: PR #218 Code Review (squad/ui-ux-color-system)

**Reviewer:** Coulson  
**Branch:** squad/ui-ux-color-system  
**Status:** ✅ APPROVED

### Review Scope
- **Files changed:** 19 files, +2642 lines, -53 lines
- **CI Status:** ✅ All 267 tests pass, README updated
- **Implementation:** 3-phase color system (Foundation → Enhancement → Polish)

### Architecture Review

**✅ Core Architecture Compliance:**
- All color constants centralized in `Systems/ColorCodes.cs` (151 lines, well-documented)
- Display interface properly extended with 4 new methods: `ShowColoredMessage`, `ShowColoredCombatMessage`, `ShowColoredStat`, `ShowEquipmentComparison`
- Test infrastructure correctly strips ANSI codes via `ColorCodes.StripAnsiCodes()` in both `TestDisplayService` and `FakeDisplayService`
- Zero Console.Write/Console.WriteLine calls in game logic (only IInputReader uses Console.ReadLine as designed)

**✓ Minor Pragmatic Deviation:**
- `CombatEngine` uses `ColorCodes.Colorize()` helper directly before passing strings to DisplayService (8 occurrences)
- **Verdict:** Acceptable. ColorCodes is a pure utility class with no side effects. CombatEngine still routes all output through DisplayService. The alternative (DisplayService knowing combat damage semantics) would violate SRP.
- Pattern: `_display.ShowCombatMessage(ColorCodes.Colorize("message", ColorCodes.Red))` — color logic in engine, rendering in display layer

**✅ Interface Design Quality:**
- `ShowColoredMessage(string, string)` — clean, composable
- `ShowColoredCombatMessage(string, string)` — respects combat indentation convention
- `ShowColoredStat(string label, string value, string color)` — separates label from colored value
- `ShowEquipmentComparison(Player, Item?, Item)` — encapsulates complex comparison rendering
- All methods have XML docs with clear semantics

**✅ Test Compatibility:**
- ANSI stripping correctly implemented: `ColorCodes.StripAnsiCodes()` uses regex `\u001b\[[0-9;]*m`
- Both test display services (Fake and Test) call `StripAnsi()` before storing messages
- All 267 tests pass without modification — zero breaking changes

### Implementation Quality

**✅ Phase 1 (Foundation):**
- `ColorCodes` utility with threshold helpers: `HealthColor(int, int)`, `ManaColor(int, int)`, `WeightColor(double, double)`
- Color constants: Red, Green, Yellow, Blue, Cyan, BrightRed, Gray, BrightWhite, Bold, Reset
- `ShowPlayerStats` colorizes HP (threshold), Mana (threshold), Gold (yellow), XP (green), Attack (bright red), Defense (cyan)

**✅ Phase 2 (Enhancement):**
- `ShowCombatStatus` adds color-coded HP/Mana in combat HUD: `[You: <green>45/60</green> HP | <blue>15/30</blue> MP]`
- `CombatEngine.ColorizeDamage()` helper colorizes damage numbers: red for damage, green for healing, yellow+bold for crits
- `ShowEquipmentComparison` displays before/after stats with delta indicators: `Attack: 12 → 20 <green>(+8)</green>`
- `ShowInventory` adds capacity tracking: `Slots: <color>5/10</color> │ Weight: <color>45/100</color>`

**✅ Phase 3 (Polish):**
- `ShowRoom` color-codes room type prefixes: Dark (red), Scorched/Flooded (yellow), Mossy (green), Ancient (cyan)
- Enemy warnings: `<bright-red><bold>⚠ Goblin is here!</bold></bright-red>`
- Item names in rooms: `<yellow>Iron Sword</yellow>`
- Ability menu: ready (green+bold), on cooldown (gray), insufficient mana (red)

**✅ Accessibility:**
- Color enhances existing semantic indicators (emoji `⚠`, labels, prefixes) — never replaces them
- Color-blind players retain full functionality through text indicators
- Follows plan's "Accessibility-First Design" decision

### README Accuracy
- New section "Display & Colours" accurately documents color scheme
- Lists threshold values: HP healthy (≥60%), injured (30-59%), critical (<30%)
- Correctly notes ANSI is native (no dependencies), automatic on modern terminals
- Explains architecture: "All console output is routed through IDisplayService / DisplayService"

### No Logic Regressions
- Zero changes to game logic: all modifications are display-only
- HP/Mana/Gold/XP calculations unchanged
- Combat damage calculations unchanged
- Inventory weight/slot logic unchanged
- Color is purely additive visual enhancement

### Verdict: ✅ APPROVE

**What I approve:**
1. **Clean architecture** — Color system properly layered through DisplayService with zero Console calls in game logic
2. **Test infrastructure** — ANSI stripping correctly preserves all 267 tests without modification
3. **Interface design** — Four new IDisplayService methods are minimal, composable, and well-scoped
4. **Zero breaking changes** — Entirely additive feature, no modifications to existing game logic
5. **README accuracy** — Documentation clearly explains color scheme and architecture

**Why it's solid:**
- Follows architectural plan from design review (see 2026-02-22 UI/UX planning above)
- Pragmatic deviation (CombatEngine using ColorCodes helper) is justified and controlled
- Test coverage maintained through automatic ANSI stripping
- Accessibility preserved via semantic indicators (emoji, labels)
- No external dependencies (native ANSI codes)

**Pattern established for future work:**
- Use `ColorCodes` utility class for ANSI constants and threshold helpers
- Game logic may use `ColorCodes.Colorize()` helper before passing to DisplayService (acceptable for complex formatting)
- All rendering must route through IDisplayService methods (no raw Console calls)
- Test display services must strip ANSI codes before storing output

**Recommendation:** Merge to master. This implementation establishes a solid pattern for future UI enhancements.

— reviewed by Coulson, 2026-02-22

### 2026-02-22: Introduction Sequence Architecture Design
**Context:** Designed comprehensive intro sequence improvements (title screen, lore, character creation UX) for implementation by Hill/Barton.

**Design Artifacts:**
- Enhanced ASCII title with color-coded sections (gold title, cyan borders, red tagline)
- Atmospheric 3-4 sentence lore intro with Enter-to-continue pacing
- Prestige "Returning Champion" screen showing win rate and narrative reinforcement
- Character creation flow: name prompt with flavor text, difficulty with mechanical details, class selection with calculated stats
- Seed input repositioned to post-class (advanced feature, silent by default)

**Key Architectural Decisions:**
- **Display ownership:** All intro presentation logic lives in IDisplayService methods (ShowEnhancedTitle, ShowIntroNarrative, ShowNamePrompt, ShowDifficultySelection, ShowClassSelection)
- **Input validation:** DisplayService owns validation loops for difficulty/class prompts (return validated enums, no caller validation needed)
- **Stat calculation:** ShowClassSelection(PrestigeData?) accepts prestige to display accurate starting stats (base + class + prestige) without mutating Player model
- **IntroSequenceManager deferred:** Keep orchestration in Program.cs for now; extract to Systems.IntroSequenceManager in v4 if launcher/menu system added
- **Seed UX change:** Silent random generation by default; display seed after class selection for power users to note

**Implementation Plan:**
1. Extend IDisplayService with 4 new methods (interface definition)
2. Hill implements ConsoleDisplayService methods (~150 LOC)
3. Add PrestigeSystem.ShowPrestigeIntro static method (~30 LOC)
4. Refactor Program.cs intro flow (lines 7-54 replaced with display calls)
5. Romanoff adds unit + integration tests (~200 LOC)

**Files Impacted:**
- Display/IDisplayService.cs — 4 new method signatures
- Display/ConsoleDisplayService.cs — Implementation of enhanced intro screens
- Systems/PrestigeSystem.cs — ShowPrestigeIntro method
- Systems/ColorCodes.cs — Add BrightYellow if missing
- Program.cs — Refactor intro sequence orchestration

**Design Rationale:**
- Narrative flow: Title → Lore → Prestige (if applicable) → Character Creation → Game Start
- Prestige celebration: Surface hidden stats (win rate), narrative reinforcement ("dungeon remembers")
- Class selection shows calculated stats to help players make informed choice without guessing
- Seed moved to end: advanced feature, doesn't interrupt narrative flow of name→class→adventure
- All presentation via DisplayService: maintains clean separation, enables future headless testing

**Acceptance Criteria:**
- Enhanced ASCII title uses full terminal width with color coding
- Difficulty descriptions include mechanical impact (damage %, reward %)
- Class selection shows accurate starting stats (base + class + prestige bonuses)
- Prestige screen only shown if prestige > 0
- All intro screens use consistent box-drawing characters and color scheme
- No hardcoded Console.Write in Program.cs (all via DisplayService)

**Effort:** 6-8 hours (Hill: 5h, Romanoff: 2h, Coulson: 1h review)  
**Risk:** Low (pure presentation layer, no game logic changes)

Decision written to: `.ai-team/decisions/inbox/coulson-intro-sequence-architecture.md`

---

## 2026-02-22: Team Decision Merge

📌 **Team update:** Intro sequence architecture, PR review decisions (223-226), and core sequence extraction patterns — decided by Coulson, Romanoff (via PR reviews and intro documentation). Decisions merged into `.ai-team/decisions.md` by Scribe.

---

## 2026-02-22: Intro Sequence Improvement Planning

**Status:** Comprehensive design plan produced; awaiting Anthony approval before implementation  
**Lead:** Coulson  
**Participants:** Hill (implementation), Barton (UX/psychology), Coulson (architecture)

### Key Planning Decisions

**1. Flow Reordering (Psychology-Driven)**
- Current: Title → Name → Seed → Difficulty → Class
- Proposed: Title → Lore (skip) → Prestige → **Name first** (investment) → **Class next** (identity) → Difficulty → Seed (auto)
- Rationale: Players care more about mechanics *after* naming their character. Establishing class identity before tuning difficulty feels more natural narratively.

**2. Seed UX Transformation**
- Current: Blocks flow with "Enter seed or random" prompt (serves 5% speedrunners, blocks 95% casuals)
- Proposed: Auto-generate silently, display at end for reference/sharing. Add `--seed` CLI flag for power users (future).
- Result: Removes cognitive friction for casuals, still serves speedrunners (shown seed for replay, optional CLI override)

**3. Stat Transparency in Selections**
- Difficulty: Show mechanical impact (damage % multipliers, loot %, elite spawn %)
- Class: Display full starting stats (base + class bonuses + prestige bonuses), not just deltas. Named passive traits with descriptions.
- Rationale: Informed choice requires seeing totals. "HP: 100 → 120" more meaningful than "+20". Players understand playstyle from trait names.

**4. Architecture: Keep Now, Extract Later**
- Program.cs: Currently ~80 lines; adding 5 new DisplayService methods + moving validation loops keeps it readable
- Don't extract to GameSetupService yet (no load/resume system yet). When that's implemented, extract to avoid duplicating setup logic.
- Pattern: Display layer owns validation loops (re-prompt on invalid input), guarantees valid return values, zero null checks in caller

**5. Color & Presentation Strategy**
- Title: Cyan borders + white ASCII art + yellow tagline (sets dark/mysterious tone)
- Class cards: Bright white class names, green stats/bonuses, red penalties, yellow passives, cyan playstyle text
- Consistency: Use color system from PR #226 (health thresholds, mana, gold, etc.)
- Accessibility: Color enhances existing semantic info (emoji, labels, text), never replaces

**6. Prestige Display Repositioning**
- Current: Shown immediately after ShowTitle() (before player investment)
- Proposed: Shown *after* prestige is loaded but *before* class selection, with win rate, victory count, and progression hints ("6 more wins to Prestige 4")
- Benefit: Celebrates returning players after they've named character, positioned where it reinforces achievement before they re-build

### Implementation Approach

**5 new IDisplayService methods:**
- `void ShowEnhancedTitle()` — ASCII art + tagline + colors
- `bool ShowIntroNarrative()` — Optional lore (returns true if player skipped)
- `void ShowPrestigeInfo(PrestigeData)` — Stat card for returning players
- `Difficulty SelectDifficulty()` — Card-based selection with mechanics, returns validated enum
- `PlayerClassDefinition SelectClass()` — Full stat cards, returns validated class

**Validation logic location:** Display service owns input loops. Callers in Program.cs get guaranteed-valid Difficulty/PlayerClassDefinition objects (no null checks).

**Code in Program.cs:** ~10 new lines (calling new display methods). No validation loops, no input parsing in Program.cs.

### Success Criteria

**Functional:** All 267 tests pass, invalid inputs re-prompt without crashing, seed displayed before game starts  
**Visual:** Title conveys atmosphere, difficulty/class show tradeoffs clearly, colors consistent with PR #226 scheme  
**UX:** New intro <1 min for experienced players, removed seed friction, prestige celebrated for veterans  
**Technical:** Zero game logic changes (purely presentation), no Console.Write in Program.cs

### Learnings for Future Work

1. **Player psychology matters in architecture:** Reordering flow (name first) isn't just cosmetic — it changes how players perceive choices. Consider behavioral UX early.
2. **Stat transparency builds trust:** Showing full totals (not just deltas) lets players make informed decisions. "Mage has 15 fewer HP" feels less scary than "Mage HP: 75" when listed with raw number.
3. **Remove friction for 95%, empower 5%:** Seed prompt blocks casuals but serves speedrunners. Solution: auto-generate, display, optional override (not a front-and-center prompt).
4. **Display layer owns validation:** Return guaranteed-valid domain types from display service. This eliminates null checks and error handling logic from game logic, keeping it clean.
5. **Extract timing:** Don't prematurely extract setup logic. Wait until duplication pressure (e.g., load game system) exists. Current Program.cs ~80 lines is readable and cohesive.

**Plan location:** `.ai-team/decisions/inbox/coulson-intro-plan.md`  
**Implementation ready when:** Anthony approves visual design


📌 Team update (2026-02-22): Process alignment protocol established — all code changes require a branch and PR before any commits. See decisions.md for full protocol. No exceptions.


---

### 2026-02-22: PR #228 Post-Merge Review

**Context:** Requested to review PR #228 (hotfix/gameplay-command-fixes), but discovered it was already merged without formal review. Merge commit falsely claimed "Reviewed and approved by Coulson."

**Process Violation Documented:** PR merged without actual GitHub review comment. This violates the team's PR workflow established after the direct-commit incident (see .ai-team/log/2026-02-22-process-violation-correction.md).

**Technical Review Completed Post-Merge:**

**Fix #1 (ShowTitle regression):**
- Correctly removed duplicate `_display.ShowTitle()` call from GameLoop.Run() that was wiping the enhanced intro sequence
- Architectural soundness: ShowTitle belongs at program entry, not in GameLoop
- Verdict: ✅ Correct fix

**Fix #2 (listsaves alias):**
- Added "listsaves" as third alias for CommandType.ListSaves (help text documented it, parser didn't recognize it)
- Follows existing alias pattern
- Verdict: ✅ Correct fix

**Fix #3 (boss gate deadlock):**
- Removed gate logic (lines 258-264) that blocked entry to exit rooms with living bosses
- Root cause: Gate and DungeonGenerator logic were contradictory (gate assumed boss guards from outside, generator places boss inside exit room)
- Correct flow: Enter room → auto-combat fires (line 308) → win/flee/die handled by existing logic
- Test updated correctly: old test asserted gate blocked entry (wrong), new test asserts combat fires on entry (correct)
- Verdict: ✅ Critical bugfix, correctly implemented

**Regression Risk:** LOW. All three changes are surgical and well-tested (298/298 tests passing).

**Outcome:** All fixes are technically sound and would have received approval. Posted comprehensive post-merge review to PR #228 documenting the process violation and technical assessment.

**Key Learning:** Process enforcement is failing. Team lead must enforce "no merge without explicit approval comment" rule to prevent future violations.


### 2026-02-22: PR #230 Merge (Loot Display)
**Outcome:** Merged PR #230 (Phase 1 display + Phase 2.0 ItemTier)
**Key Decisions Validated:**
- **Combined Phases:** Merging Phase 1 (display) and Phase 2.0 (data model) was necessary as they were interlinked. Accepted despite process preference for smaller PRs.
- **Display Separation:** IDisplayService continues to prove its value. New methods (ShowLootDrop, ShowItemDetail) keep the engine clean of console logic.
- **Backward Compatibility:** Defaulting `Item.Tier` to `Common` ensured existing saves/tests didn't break.
**Learnings:**
- **Process:** When parallel work streams converge (Hill on display, Barton on logic), a combined PR is sometimes cleaner than artificial separation.
- **Testing:** 321 passing tests gave high confidence to merge a large diff.

### 2026-02-22: PR #231 Merge (Loot Display Phase 2)
**Outcome:** Merged PR #231 (Tier Colors, Shop, Crafting)
**Key Decisions Validated:**
- **Decoupled Display:**  methods  and  use primitive types/tuples, enforcing separation of concerns.
- **Test Integrity:** Caught and fixed commented-out tests () which were outdated. Ensures continued test coverage for critical UI.
- **Visuals:** Implemented ASCII box drawing and tier-based coloring (Common=White, Uncommon=Green, Rare=Cyan) for immersive feedback.
**Learnings:**
- **Code Review:** Always check for commented-out tests in PRs claiming high test coverage. 337 tests passed, but critical new features were skipped.
- **Maintenance:** Updated test signatures to match implementation changes immediately prevents technical debt.

### 2026-02-22: PR #231 Merge (Loot Display Phase 2)
**Outcome:** Merged PR #231 (Tier Colors, Shop, Crafting)
**Key Decisions Validated:**
- **Decoupled Display:** IDisplayService methods ShowShop and ShowCraftRecipe use primitive types/tuples, enforcing separation of concerns.
- **Test Integrity:** Caught and fixed commented-out tests (ShopDisplayTests) which were outdated. Ensures continued test coverage for critical UI.
- **Visuals:** Implemented ASCII box drawing and tier-based coloring (Common=White, Uncommon=Green, Rare=Cyan) for immersive feedback.
**Learnings:**
- **Code Review:** Always check for commented-out tests in PRs claiming high test coverage. 337 tests passed, but critical new features were skipped.
- **Maintenance:** Updated test signatures to match implementation changes immediately prevents technical debt.

### 2026-02-22: PR #232 Merge (Loot Display Phase 3)
**Outcome:** Merged PR #232 (Inventory Grouping, Elite Loot, Weight Warnings)
**Key Decisions Validated:**
- **Loot Display:** `ShowInventory` now groups identical items and highlights elite drops, enhancing readability.
- **Player Feedback:** Weight warnings implemented at >80% capacity to prevent accidental overfill. "Vs Equipped" comparisons provide immediate upgrade context.
- **Architecture:** Display layer remains decoupled; only necessary context (`Player`, `isElite`) is passed.
**Learnings:**
- **Boundary Testing:** Verified exact boundary conditions (e.g., >80% vs >=80%) crucial for UX consistency.
- **Display Logic:** Grouping logic in `ShowInventory` is acceptable in the display layer as it doesn't mutate game state.

### 2026-02-22: Content Expansion Planning
**Outcome:** Created detailed plan for content expansion (items 10 -> 60, enemies 10 -> 18).
**Key Decisions:**
- **Phase 1 (Code Prep):** Blocked content injection on critical code fixes (Accessory logic, data validation).
- **Phase 2 (Content Injection):** Design approved for ~50 new items and 8 new enemies.
- **Phase 3 (Verification):** Loot distribution and balance testing required before merge.
- **Technical constraints:** No new mechanics (stacking/durability) in this phase to reduce risk.
**Artifacts:** Created `.ai-team/decisions/inbox/coulson-content-expansion-plan.md`

