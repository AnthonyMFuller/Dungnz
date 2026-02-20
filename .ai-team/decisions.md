# Team Decisions

> Canonical decision ledger. Append-only. Agents write to `.ai-team/decisions/inbox/` — Scribe merges here.

---

### 2026-02-20: Design Review decisions
**By:** Coulson  
**What:** Pre-build interface contracts agreed by Hill and Barton  
**Why:** Ceremony before Phase 1 — prevents rework from contract mismatches

#### Agreed Contracts and Decisions

1. **CombatEngine Contract**
   - `CombatEngine.StartCombat(Player, Enemy) → CombatResult {Won, Fled, PlayerDied}`
   - Blocking synchronous call; Hill's GameLoop invokes and handles result
   - CombatEngine receives DisplayService via constructor (Hill instantiates both)

2. **DisplayService Contract**
   - Hill exposes: `ShowCombatStatus(Player, Enemy)`, `ShowCombatMessage(string)`, `ShowInventory(Player)`, `ShowLootDrop(Item)`
   - Rule: ALL console output routed through DisplayService (no raw Console.Write in Barton's code)

3. **Inventory System Contract**
   - Barton exposes: `TakeItem(Player, Item) → bool`, `UseItem(Player, string) → UseResult`, `EquipItem(Player, string) → bool`, `GetInventorySummary(Player) → IReadOnlyList<Item>`
   - UseResult enum: Success, NotFound, InvalidContext (no exceptions for missing items)

4. **LootTable Contract**
   - `LootTable.RollDrop(Enemy) → LootResult { Item? item, int gold }`
   - Separates gold from item drops; caller adds gold to Player.Gold

5. **Model Ownership**
   - Hill defines: Player, Enemy (base), Item, Room, Direction, CombatResult, UseResult, LootResult
   - Barton defines: 5 Enemy subclasses (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss)
   - Player fields: HP, MaxHP, Attack, Defense, Level, Gold, XP, InventorySlots (List<Item>), EquippedWeapon (Item?), EquippedArmor (Item?)
   - Enemy fields: HP, MaxHP, Attack, Defense, Name, Type (enum), XPReward, GoldReward (no IsAlive flag; death = HP <= 0)
   - Item fields: Name, Type (enum: Weapon/Armor/Consumable), Value, AttackBonus, DefenseBonus, HealAmount

6. **Room Enemy Storage**
   - Room holds single nullable `Enemy?` reference (not list)
   - CombatEngine sets `enemy.HP = 0` on death; GameLoop nulls Room.Enemy after combat

7. **Stat Application**
   - Equipping weapon/armor applies Attack/Defense bonuses immediately
   - InventoryManager mutates Player.Attack/Defense when equipping

8. **Dungeon Boss**
   - Spawned once by DungeonGenerator, guards exit room
   - Drops loot only on first death (Room tracks "looted" flag or LootTable checks state)

9. **Player Death Handling**
   - CombatEngine returns `CombatResult.PlayerDied`
   - GameLoop handles game-over display and exit (Barton does NOT call Environment.Exit)

10. **Flee Penalty Edge Case**
    - If flee penalty kills player, result is `PlayerDied`, not `Fled`
    - GameLoop picks random adjacent room after successful flee

#### Build Order
- **Phase 1a (Hill):** Models, enums, DisplayService interface
- **Phase 1b (Barton, depends on 1a):** Enemy subclasses, CombatEngine, InventoryManager, LootTable
- **Phase 2 (Parallel):** Hill builds DungeonGenerator/GameLoop/CommandParser; Barton completes systems


---

# Hill — Phase 1 Design Decisions

**Date:** 2026-02-20  
**Agent:** Hill  
**Work Items:** WI-1 (Scaffold), WI-2 (Core Models)

---

## Decision 1: Item Field Completeness
**Context:** Design Review specified 7 fields for Item, but some overlap (StatModifier vs AttackBonus/DefenseBonus).

**Decision:** Included all fields (StatModifier, AttackBonus, DefenseBonus, HealAmount) in Item class.

**Rationale:**
- Provides flexibility for Barton's systems to use either simple (StatModifier) or granular (AttackBonus/DefenseBonus) approaches
- Avoids future refactoring if requirements evolve
- Minimal cost (4 extra bytes per Item instance)

**Alternatives Considered:**
- Remove StatModifier and use only AttackBonus/DefenseBonus → Rejected: Design Review explicitly listed all fields
- Use only StatModifier → Rejected: Less expressive for complex items (e.g., item that boosts attack but reduces defense)

---

## Decision 2: LootTable Placement
**Context:** LootTable is implemented by Barton but needs to be a property on Enemy (Hill's model).

**Decision:** Placed LootTable class in Models/ folder with placeholder RollDrop method.

**Rationale:**
- Shared type across Hill's (Enemy.LootTable property) and Barton's (RollDrop implementation) domains
- Avoids circular reference if placed in Systems/ (Models would reference Systems)
- Barton can replace stub implementation without moving the class

**Alternatives Considered:**
- Abstract base class in Models/, concrete impl in Systems/ → Rejected: Over-engineered for simple loot logic
- Interface ILootTable in Models/ → Rejected: Adds boilerplate without value (no polymorphism needed)

---

## Decision 3: DisplayService Method Naming
**Context:** Design Review specified ShowCombatStatus and ShowCombatMessage for CombatEngine integration.

**Decision:** Used exact method names from Design Review contract.

**Rationale:**
- Barton can implement CombatEngine without waiting for Hill to clarify naming
- Reduces integration friction (no "which method do I call?" questions)
- Naming is clear and consistent (ShowX pattern for all display methods)

---

## Decision 4: Room.Looted Flag
**Context:** Boss loot should drop only once; Design Review mentioned Room.Looted flag OR LootTable state tracking.

**Decision:** Added Looted flag to Room model.

**Rationale:**
- Simpler than LootTable maintaining per-enemy state across rooms
- Room already has Visited flag; Looted is parallel concept
- GameLoop can check Room.Looted before calling LootTable.RollDrop

**Alternatives Considered:**
- LootTable tracks dropped state per Enemy instance → Rejected: Requires LootTable to store stateful data (breaks stateless design)
- No loot deduplication → Rejected: Boss farming would break game balance

---

## Non-Decisions (Deferred to Barton)
- Enemy subclass implementations (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss)
- LootTable.RollDrop logic (drop rates, random selection)
- InventoryManager implementation (TakeItem, UseItem, EquipItem)
- CombatEngine.StartCombat implementation (turn order, damage calculation, flee mechanics)

---

**Approval:** Self-approved (within Hill's charter scope)  
**Status:** Implemented in commit 5c0901c


---

# Barton Phase 2 Decisions — Combat Systems

**Date:** 2024
**Agent:** Barton (Systems Dev)
**Work Items:** WI-6, WI-7, WI-8

## Decision 1: Flee Mechanic Penalty
**Decision:** Failed flee attempts result in enemy getting a free attack before returning to combat menu.
**Rationale:** Prevents flee spam, adds risk/reward to retreat decisions, maintains combat tension.
**Alternative Considered:** Simply denying flee and continuing turn — rejected as too lenient.

## Decision 2: Equipment Consumption Model
**Decision:** Weapons and Armor permanently increase stats and are removed from inventory when equipped.
**Rationale:** Simplified inventory management, no unequip mechanic needed for Phase 2.
**Future Consideration:** Could add equipment slots and unequip in later phases.

## Decision 3: Loot Drop First-Match-Wins
**Decision:** LootTable checks each drop probability sequentially; first success wins, no stacking.
**Rationale:** Simplifies loot logic, prevents complex multi-drop scenarios in early game.
**Trade-off:** Bosses with multiple drops must rely on high individual chances rather than multiple rolls.

## Decision 4: Level-Up Full Heal
**Decision:** Level up restores HP to new MaxHP value.
**Rationale:** Rewards progression, prevents dying immediately after level up, feels satisfying.
**Balance Note:** Combined with +10 MaxHP per level, player scales well against late-game enemies.

## Decision 5: Enemy Stat Curve
**Decision:** Enemy progression follows: Goblin (20 HP) → Skeleton (30) → Troll (60) → Dark Knight (45) → Boss (100).
**Rationale:** 
- Goblin: early game fodder
- Skeleton: first threat
- Troll: HP tank teaches attrition
- Dark Knight: high offense/defense, mid-boss feel
- Boss: 2.5x HP of Dark Knight, guards exit
**Note:** Dark Knight lower HP than Troll but much higher threat via ATK/DEF.


---

# Hill Phase 2 Design Decisions

**Date:** 2026-02-20  
**Author:** Hill (C# Dev)  
**Work Items:** WI-3, WI-4

## Decision 1: Room Graph Architecture

**Context:** Needed to create a procedurally generated dungeon with guaranteed paths.

**Decision:** Implemented 5x4 grid (20 rooms) with bidirectional connections via Dictionary<Direction, Room> stored directly in Room.Exits.

**Rationale:**
- Grid structure guarantees connectivity (no isolated rooms)
- Bidirectional linking ensures players can backtrack
- Dictionary<Direction, Room> allows O(1) exit lookup in GameLoop
- BFS validation provides safety check (though redundant with full grid)

**Alternatives Considered:**
- Tree structure (rejected: no loops, limited exploration)
- Graph with random connections (rejected: complexity, possible dead ends)

---

## Decision 2: ICombatEngine Interface

**Context:** GameLoop needs combat but Barton owns combat implementation.

**Decision:** Created ICombatEngine interface with single method: `CombatResult RunCombat(Player, Enemy)`.

**Rationale:**
- Clean separation of concerns: Hill owns game loop, Barton owns combat
- Allows parallel development (stub for testing, real impl swaps in)
- GameLoop doesn't know or care about combat internals (attack rolls, flee logic, loot drops)
- Dependency injection via constructor makes testing possible

**Interface Contract:**
```csharp
public interface ICombatEngine
{
    CombatResult RunCombat(Player player, Enemy enemy);
}
```

**Impact:** Barton delivered CombatEngine in parallel; integration was trivial (one line change in Program.cs).

---

## Decision 3: Command Parsing Strategy

**Context:** Need to parse diverse player input (full commands, shortcuts, directions).

**Decision:** Single-pass parser with switch expression on normalized input. Supports aliases (n/s/e/w, i, h/?, q).

**Rationale:**
- Simple and fast: O(1) lookup via switch
- Extensible: new commands added as switch cases
- Case-insensitive for better UX
- Returns structured ParsedCommand (type + argument) for type-safe dispatch

**Alternatives Considered:**
- Regex patterns (rejected: overkill, harder to maintain)
- Command pattern with classes (rejected: too heavyweight for this scale)

---

## Decision 4: Item Usage Semantics

**Context:** Players need to use consumables (heal) and equip gear (weapons/armor).

**Decision:** HandleUse implements:
- Consumables: Apply HealAmount, cap at MaxHP, remove from inventory
- Equippables: Add stat bonuses permanently to player, remove from inventory (single-use equip)

**Rationale:**
- Simple mental model: use = consume/equip, item is removed
- No equipment slot management (out of scope for MVP)
- Stat bonuses stack (multiple weapons/armor allowed)

**Trade-offs:**
- No unequip mechanic (future enhancement)
- No equipment slot limits (could stack infinite items)
- Accepted for MVP; easily extended later if needed

---

## Decision 5: Win/Lose Conditions

**Context:** Game needs clear victory and defeat states.

**Decision:**
- **Win:** Player enters exit room (`IsExit == true`) AND boss is dead (`Enemy == null || Enemy.HP <= 0`)
- **Lose:** `CombatResult.PlayerDied` returned from combat
- **Boss guard:** Cannot enter exit room if boss is alive (enforced in HandleGo)

**Rationale:**
- Boss fight is mandatory (aligns with "dungeon crawler" genre)
- Clear feedback: game ends immediately on win/lose
- Boss guard prevents sequence breaking (can't skip final fight)

**Implementation:** Checked in two places:
1. HandleGo: Block movement to exit if boss alive
2. HandleGo: After moving to exit, check win condition

---

## Decision 6: EnemyFactory Stub Pattern

**Context:** DungeonGenerator needs enemies, but Barton is implementing enemy classes in parallel.

**Decision:** Created EnemyFactory with internal stub classes (GoblinStub, SkeletonStub, etc.) that compile immediately.

**Rationale:**
- Allows Hill to finish WI-3/WI-4 without waiting for Barton
- Stubs have correct Enemy inheritance and basic stats
- Barton's real classes (Systems/Enemies/) can replace stubs transparently
- Factory pattern centralizes enemy creation logic

**Outcome:** Barton delivered real enemy classes in parallel commit. Stubs remain in code but are unused. Factory now returns real instances.

---

## Decision 7: Automatic Combat Trigger

**Context:** When should combat start?

**Decision:** Combat triggers automatically when player enters room with living enemy. Called immediately in HandleGo after room transition.

**Rationale:**
- Simplifies game flow (no "attack" command needed while moving)
- Aligns with classic dungeon crawler behavior (Zork, NetHack)
- Prevents "run past enemies" exploit

**Alternative Considered:** Manual attack command (rejected: adds micromanagement, less immersive)

---

## Notes for Scribe

These decisions are stable and can be merged to main decisions.md. All contracts (ICombatEngine, CommandType, ParsedCommand) are finalized and used by both Hill and Barton's code.


---

### 2026-02-20: Code review verdict
**By:** Romanoff  
**What:** WI-10 review outcome — feature-complete quality gate  
**Why:** Quality gate before v1 ships. Reviewed all 23 source files for bugs, logic errors, architectural violations, and edge cases.

**Verdict:** APPROVED ✓

**Issues Found & Fixed (7):**
1. **Architectural violation** — Program.cs used direct Console.Write/ReadLine instead of DisplayService
2. **Architectural violation** — GameLoop.cs used direct Console.Write("> ") for command prompt
3. **Architectural violation** — CombatEngine.cs used direct Console.Write("[A]ttack or [F]lee? ") for combat prompt
4. **Logic bug** — Dead enemies (HP=0) not cleared from room after combat victory, leaving Enemy!=null
5. **Logic inconsistency** — Win condition checked `HP<=0` instead of `Enemy==null`, creating dead-but-present enemy state
6. **Architecture enforcement** — Added ShowCommandPrompt(), ShowCombatPrompt(), ReadPlayerName() to DisplayService
7. **Lifecycle bug** — Fixed GameLoop to set `_currentRoom.Enemy = null` after CombatResult.Won

**Edge cases verified:**
- Empty inventory USE → handled (error message)
- GO with no argument → handled (error message)
- TAKE with no items in room → handled (error message)
- Combat with dead enemy (HP<=0, Enemy!=null) → FIXED (now impossible — enemy cleared on defeat)

**No blocking issues remain.**

**Commit:** eb99612 "fix: code review corrections (WI-10)"

**Rationale for APPROVED:**
- All architectural violations corrected (DisplayService now sole Console I/O owner)
- Logic bugs fixed (dead enemy cleanup ensures consistent state)
- Null safety verified (LootTable = null! safe — always initialized in constructors)
- Edge cases handled properly
- Code is production-ready

Code is ready to ship. No further revisions required.


---

# Decision: Test Infrastructure Required for v2

**Date:** 2026-02-20  
**Ceremony:** Retrospective  
**Proposed by:** Romanoff, supported by Hill and Barton

## Context
TextGame v1 shipped with zero automated tests. All quality verification was manual (code review, playtesting). Current architecture blocks testability:
- CombatEngine and LootTable create own Random instances (no determinism)
- DisplayService is concrete class, not interface (can't mock console I/O)
- GameLoop tightly couples game state and presentation logic

## Decision
Before any v2 feature work begins, implement test infrastructure:
1. Add unit test framework (xUnit or NUnit)
2. Refactor Random to be injectable for deterministic testing
3. Extract IDisplayService interface for mocking console I/O
4. Create test harness for combat, inventory, and loot systems
5. Document edge cases from WI-10 code review as automated test cases

## Rationale
- Regression risk is HIGH without test coverage
- Confident refactoring requires safety net
- Future features (save/load, balance tuning, new enemies) need regression protection
- Current architecture prevents automated testing

## Impact
- **Blocks:** Any v2 feature work until test infrastructure is in place
- **Requires:** Refactoring of CombatEngine, LootTable, DisplayService consumers
- **Effort estimate:** 1-2 sprints for infrastructure + initial test coverage

## Alternatives Considered
- Continue with manual testing → Rejected: regression risk too high, doesn't scale
- Add tests incrementally as features are added → Rejected: architecture blocks testability now

## Status
Approved by retrospective consensus. Assigned to v2 planning ceremony.


---

# Decision: Player Encapsulation Refactor

**Date:** 2026-02-20  
**Ceremony:** Retrospective  
**Proposed by:** Hill

## Context
Player model currently has all properties with public setters:
```csharp
public int HP { get; set; }
public int MaxHP { get; set; }
public int Attack { get; set; }
public int Defense { get; set; }
// etc.
```

Nothing prevents invalid state mutations:
- `Player.HP = -100`
- `Player.MaxHP = 0`
- `Player.Attack = -50`

This worked for MVP but blocks future use cases requiring state integrity (save/load, multiplayer, modding, analytics).

## Decision
Refactor Player model to:
1. Private setters on all stat properties
2. Public methods for state transitions: `TakeDamage(int amount)`, `Heal(int amount)`, `ModifyAttack(int delta)`, etc.
3. Validation logic: HP capped at MaxHP, stats can't go negative
4. Event hooks for stat changes (future use: achievements, UI updates, analytics)

## Rationale
- Encapsulation prevents invalid state bugs
- Validation centralizes business rules
- Event hooks enable future extensibility
- Aligns with OOP best practices for domain models

## Impact
- **Owner:** Hill
- **Timing:** Must complete before v2 save/load or stat persistence work
- **Breaking change:** Any external code directly setting Player properties must refactor to use methods
- **Effort estimate:** 2-3 hours (refactor + update all call sites)

## Alternatives Considered
- Keep current public setters → Rejected: highest risk for future extensions
- Partial encapsulation (only HP/MaxHP) → Rejected: inconsistent, defers problem

## Status
Approved by retrospective. Assigned to Hill for v2 planning.


---

# Decision: DisplayService Interface Extraction

**Date:** 2026-02-20  
**Ceremony:** Retrospective  
**Proposed by:** Romanoff, Hill, Barton

## Context
DisplayService is currently a concrete class with tight coupling to Console:
```csharp
public class DisplayService
{
    public void ShowMessage(string msg) => Console.WriteLine(msg);
    // etc.
}
```

This blocks:
- Headless automated testing (can't mock console I/O)
- Future GUI or web interface implementations
- Test runners that need to capture/verify output

Current architecture requires DisplayService to be injected, but being a concrete class limits testability and extensibility.

## Decision
Extract IDisplayService interface:
1. Define interface with all current DisplayService methods
2. Rename current DisplayService to ConsoleDisplayService : IDisplayService
3. Update all consumers (GameLoop, CombatEngine, InventoryManager) to depend on IDisplayService interface
4. Inject via constructor (already done, just change type from concrete to interface)

## Rationale
- Enables mocking for automated tests (inject TestDisplayService that captures output)
- Opens path for alternative implementations (GUI, web, Discord bot, etc.)
- Follows dependency inversion principle
- Minimal breaking change (consumers already use constructor injection)

## Impact
- **Owners:** Hill (interface extraction, GameLoop update), Barton (CombatEngine update)
- **Timing:** Before v2 testing infrastructure or alternative UI work
- **Breaking change:** None (constructors already accept DisplayService, just change to interface type)
- **Effort estimate:** 1-2 hours (extract interface + update injection sites)

## Alternatives Considered
- Keep concrete class, test by redirecting Console.Out → Rejected: fragile, doesn't enable alternative UIs
- Create parallel test-only display service → Rejected: duplicates code, inconsistent contracts

## Status
Approved by retrospective. Assigned to Hill and Barton for v2 planning coordination.
### 2026-02-20: V2 Systems Proposal — Gameplay Features & Balance
**By:** Barton  
**What:** Proposed gameplay features, content expansions, and balance improvements for Dungnz v2  
**Why:** Planning phase for v2 to maximize fun, replayability, and strategic depth

## 1. NEW GAMEPLAY FEATURES (Ranked by Impact/Effort)

### Priority 1: Status Effects System (consolidated)
**By:** Barton, Coulson (v2 planning)
**What:** Turn-based status effects (buffs/debuffs) with 6 core types: Poison (DOT), Bleed (DOT), Stun (skip turn), Regen (HOT), Fortified (DEF buff), Weakened (ATK debuff). Enum-based, duration-tracked, on-demand stat modifiers.
**Why:** Adds tactical depth without UI complexity. Counters "spam attack" strategy. Enables build diversity (DOT vs burst).

**Design Details (Barton Implementation):**
- **Effect types:** Enum with 6 entries (type-safe, extensible)
- **Duration tracking:** Each ActiveEffect.RemainingTurns decrements per turn
- **Storage:** Dictionary<object, List<StatusEffect>> keyed by target (fast lookup)
- **Stat application:** Modifiers calculated on-demand (GetStatModifier()) during damage calculations, not mutating base stats
- **Debuff/Buff separation:** Antidote only removes debuffs; buffs persist

**Balance Tuning:**
- DOTs frontloaded: Bleed (5dmg/2turns=10 total) > Poison (3dmg/3turns=9 total)
- Stun brief (1 turn) to prevent frustration
- Stat modifiers 50% to impact without trivializing combat
- Regen (4HP/3turns=12 total) counters DOT pressure

**Integration:**
- StatusEffectManager shared between CombatEngine and GameLoop
- Effects processed before actions (prevents ghost hits from DOT deaths)
- Clear effects on combat end (prevents stale state)

---

### Priority 2: Skill System (HIGH Impact / HIGH Effort)
**What:** Active abilities with cooldowns and resource costs
**Mechanics:**
- Skills unlocked via level milestones (Level 3, 5, 8, 10)
- Consume mana (new stat: Player.Mana, Player.MaxMana)
- Cooldowns prevent spam (e.g., 3-turn cooldown)
- Examples:
  - **Power Strike** (Unlock L3): 150% damage, 20 mana, 3-turn CD
  - **Defensive Stance** (Unlock L5): +5 DEF for 2 turns, 15 mana, 4-turn CD
  - **Poison Dart** (Unlock L8): Apply Poison (3 dmg/turn, 3 turns), 25 mana, 2-turn CD
  - **Second Wind** (Unlock L10): Heal 30 HP, remove debuffs, 40 mana, 5-turn CD

**Formula:**
- Mana regen: +5 per combat turn, +20 on level up, +10 per room rest
- Cooldown tracking per skill: `Dictionary<SkillType, int> ActiveCooldowns`
- Combat menu: [A]ttack / [S]kill / [F]lee

**Why:** Transforms combat from deterministic stat-check to resource management puzzle. Rewards planning over reflexes. Enables "boss prep" gameplay loop (rest → skill spam boss).

---

### Priority 3: Consumable Enhancements (MEDIUM Impact / LOW Effort)
**What:** Expand consumable variety beyond basic healing
**New Items:**
- **Antidote** (20g): Remove Poison/Bleed
- **Rage Potion** (50g): +5 ATK for 3 turns
- **Iron Skin Elixir** (50g): +4 DEF for 3 turns
- **Mana Potion** (30g): Restore 25 mana
- **Smoke Bomb** (40g): Guaranteed flee (consumed on use)
- **Weakness Poison** (60g): Apply Weakened to enemy (-30% ATK, 3 turns)

**Loot Table Integration:**
- Common enemies drop status-cure items (10% chance)
- Mid-tier enemies drop buff potions (15% chance)
- Dark Knight drops tactical items (Smoke Bomb, Weakness Poison, 25% chance)

**Why:** Low effort (reuse UseItem infrastructure). Adds strategic inventory management. Enables "save consumables for boss" risk/reward.

---

### Priority 4: Equipment Slots & Unequip (LOW Impact / MEDIUM Effort)
**What:** Persistent equipment with swap mechanics
**Changes:**
- Player gets fixed slots: `EquippedWeapon`, `EquippedArmor`, `EquippedAccessory`
- Equipping new item replaces old one (returned to inventory)
- Stat bonuses removed when unequipped
- Accessories: new item type (e.g., "Ring of Vitality" +15 MaxHP, "Berserker Amulet" +3 ATK -2 DEF)

**Why:** Enables build experimentation. Prevents infinite stat stacking. Adds inventory pressure (10-slot limit). Moderate effort due to stat recalculation complexity.

---

### Priority 5: Critical Hits & Dodge (MEDIUM Impact / LOW Effort)
**What:** RNG variance in combat outcomes
**Mechanics:**
- **Critical Hit:** 15% chance to deal 2x damage (rolled after armor reduction)
- **Dodge:** (Player.Defense / 2)% chance to avoid attack entirely (capped at 30%)
- Display: "CRITICAL HIT! You dealt 24 damage!" or "The Troll's attack missed!"

**Formula:**
```csharp
bool IsCritical() => _rng.NextDouble() < 0.15;
bool IsDodged(int defenderDefense) => _rng.NextDouble() < Math.Min(0.30, defenderDefense / 200.0);
```

**Why:** Minimal code change. Makes defense scaling feel impactful. Introduces comeback potential in losing fights. Balances with low proc rates to avoid frustration.

---

## 2. CONTENT EXPANSIONS

### New Enemy Archetypes
**Goblin Shaman** (25 HP, 10 ATK, 4 DEF, 20 XP)
- Special: 30% chance to cast Weakened on player (turn 1 only)
- Drops: Mana Potion (40%), 5-12 gold
- **Why:** Introduces tactical threat (kill priority target). Teaches status effect mechanics early.

**Wraith** (35 HP, 15 ATK, 2 DEF, 30 XP)
- Special: 20% dodge chance (replaces defense scaling)
- Drops: Antidote (30%), 8-18 gold
- **Why:** Glass cannon archetype. Punishes low-damage builds. High variance fights.

**Stone Golem** (70 HP, 8 ATK, 18 DEF, 50 XP)
- Special: Immune to status effects
- Drops: Iron Skin Elixir (50%), 15-30 gold
- **Why:** Forces sustained damage strategies. Counters DOT cheese. Mid-game wall.

**Vampire Lord** (55 HP, 20 ATK, 10 DEF, 65 XP)
- Special: Lifesteals 30% of damage dealt to player
- Drops: Dark Blade (60%), Vampiric Ring (+2 ATK, lifesteals 10%, 25%)
- **Why:** Late-game threat. Requires burst damage or Weakened application. Teaches counterplay.

**Mimic** (40 HP, 14 ATK, 6 DEF, 35 XP)
- Special: Spawns disguised as loot chest. Ambushes on "TAKE" command.
- Drops: 2 random items (100% chance)
- **Why:** Risk/reward for loot hunting. Memorable "gotcha" moment. Rewards paranoid players.

---

### Boss Variants
**Elite Bosses** (Rare spawn, replaces normal enemy)
- +50% HP/ATK/DEF of base enemy
- Guaranteed rare drop (boss-tier loot from normal enemy table)
- Spawns 5% of the time instead of normal enemy
- **Why:** Replayability. Exciting RNG moments. Rewards risk-taking.

**Dungeon Boss Phase 2** (Triggers at 40% HP)
- Boss enters "Enraged" state: +10 ATK, attacks twice per turn
- One-time dialog: "The boss roars and strikes wildly!"
- **Why:** Clutch moments. Forces skill/consumable usage. Feels like epic final battle.

---

### New Items
**Weapon Tier 2:**
- **Flameblade** (+7 ATK, 10% to apply Burn)
- **Executioner's Axe** (+9 ATK, -2 DEF)
- **Frost Dagger** (+5 ATK, 15% to apply Stun)

**Armor Tier 2:**
- **Dragon Scale Armor** (+8 DEF, +10 MaxHP)
- **Mage Robes** (+4 DEF, +20 MaxMana)
- **Berserker Plate** (+10 DEF, -2 ATK)

**Accessories:**
- **Amulet of Clarity** (+15 MaxMana, -5% skill cooldowns)
- **Ring of Vitality** (+20 MaxHP, regen 2 HP/turn)
- **Lucky Charm** (+5% crit chance, +3% dodge)

---

## 3. BALANCE IMPROVEMENTS

### Enemy Scaling to Player Level
**Problem:** Current enemies have fixed stats. Goblin at L1 = Goblin at L10.

**Solution:** Dynamic stat scaling formula
```csharp
public static Enemy ScaleToLevel(Enemy baseEnemy, int playerLevel)
{
    var scaled = baseEnemy.Clone();
    var multiplier = 1.0 + ((playerLevel - 1) * 0.12); // +12% per level
    scaled.HP = (int)(scaled.HP * multiplier);
    scaled.MaxHP = scaled.HP;
    scaled.Attack = (int)(scaled.Attack * multiplier);
    scaled.Defense = (int)(scaled.Defense * multiplier);
    scaled.XPValue = (int)(scaled.XPValue * multiplier);
    return scaled;
}
```

**Why:** Maintains challenge throughout dungeon. Prevents trivializing early floors after leveling. Rewards exploration (risk vs reward on revisiting cleared areas).

---

### Loot Progression Curve
**Problem:** Loot drops don't scale with player power. Late-game Skeleton drops worthless Rusty Sword.

**Solution:** Loot tiers based on player level
- **Level 1-3:** Basic items (Rusty Sword +3 ATK, Leather Armor +2 DEF)
- **Level 4-6:** Uncommon items (Iron Blade +5 ATK, Chainmail +4 DEF)
- **Level 7-9:** Rare items (Dark Blade +7 ATK, Knight's Armor +6 DEF)
- **Level 10+:** Epic items (Flameblade +9 ATK + effect, Dragon Scale +8 DEF +10 HP)

**Implementation:**
```csharp
// In LootTable.RollDrop()
private Item ScaleLootToLevel(Item baseItem, int playerLevel)
{
    if (playerLevel < 4) return baseItem; // no scaling early game
    var tier = (playerLevel - 1) / 3; // 0, 1, 2, 3...
    var scaled = baseItem.Clone();
    scaled.AttackBonus += tier * 2;
    scaled.DefenseBonus += tier * 2;
    scaled.HealAmount += tier * 5;
    scaled.Name = $"{TierPrefix(tier)} {scaled.Name}";
    return scaled;
}
```

**Why:** Keeps loot exciting throughout playthrough. Prevents "vendor trash" feeling. Maintains risk/reward for combat.

---

### Difficulty Curve Adjustments
**Current Problems:**
1. Troll (60 HP, 10 ATK) easier than Dark Knight (45 HP, 18 ATK) due to low threat
2. Level 2-3 power spike trivializes mid-game
3. Boss fight too stat-checky (either you win or die, no skill expression)

**Solutions:**

**1. Troll Rework:**
- Reduce HP to 50 (from 60)
- Add "Regeneration" ability: heals 5 HP per turn
- Forces burst damage or Weakened application
- Teaches DOT counters (Poison prevents regen)

**2. Level-Up Rebalance:**
- Reduce per-level gains: +1 ATK (from +2), +1 DEF (same), +8 MaxHP (from +10)
- Keep full heal on level up
- Slower power curve = longer challenge retention

**3. Boss Skill Check:**
- Boss telegraphs big attack: "The boss winds up for a devastating strike!"
- Next turn: deals 3x damage
- Player can respond: Defensive Stance skill, flee, or use Iron Skin Elixir
- **Why:** Rewards reaction, not just stat optimization

---

### Gold Sink & Economy
**Problem:** Gold accumulates with no spending outlet (no shops in v1).

**Solution:** In-Dungeon Merchants (future integration point)
- Merchant room spawns 10% of the time (replaces empty room)
- Sells consumables (2x loot value) and rerolls inventory on revisit
- Buys items (50% of value) for emergency gold

**Alternative (No Merchant):** Shrine System
- Shrines spawn in 15% of rooms
- Costs 30 gold to activate
- Effects: Full heal, Remove all debuffs, +1 random stat permanently, Grant random buff (3 turns)
- **Why:** Adds risk/reward spending. Creates "do I spend gold now or save?" decisions.

---

## 4. PROGRESSION SYSTEM TWEAKS

### Milestone Rewards (Beyond Stats)
- **Level 3:** Unlock Power Strike skill
- **Level 5:** Unlock Defensive Stance skill + Inventory capacity +5 slots
- **Level 8:** Unlock Poison Dart skill + Crit chance +5%
- **Level 10:** Unlock Second Wind skill + Start combat with +50% mana

**Why:** Makes leveling feel like major power spike. Creates aspirational goals. Rewards exploration.

---

### Save-Scumming Prevention (If Save/Load Added)
**Problem:** Players save before boss, retry until RNG favors them.

**Solution:** Permadeath-lite
- Deaths cost 50% gold and return player to entrance (no full game over)
- Boss respawns on death (but loot already taken persists)
- "Hardcore Mode" option: true permadeath for challenge runners

**Why:** Maintains tension. Respects player time. Enables risk-taking without frustration.

---

## IMPLEMENTATION PRIORITY (Barton's Recommendation)

**Sprint 1 (Test Infrastructure):**
- Extract IDisplayService, refactor Random injection, add xUnit harness
- No feature work until tests exist

**Sprint 2 (High ROI Features):**
1. Status Effects System (foundation for all future mechanics)
2. Consumable Enhancements (reuses existing code paths)
3. Critical Hits & Dodge (2-hour implementation)

**Sprint 3 (Combat Depth):**
1. Skill System (major feature, needs status effects first)
2. Boss Phase 2 mechanic (tests skill system under pressure)

**Sprint 4 (Content):**
1. New enemies (Goblin Shaman, Wraith, Stone Golem, Vampire Lord, Mimic)
2. Enemy scaling formula
3. Loot progression curve

**Sprint 5 (Polish):**
1. Equipment slots + unequip
2. Milestone rewards
3. Shrine/Merchant system (economy sink)

---

## METRICS FOR SUCCESS
- **Replayability:** Players complete 3+ runs (track via analytics if added)
- **Strategic Depth:** >40% of players use skills in boss fight (not just spam attack)
- **Balance:** Average boss attempts = 2-3 (not 1-shot or 10+ retries)
- **Fun Factor:** Qualitative feedback ("exciting moments" mentions in playtests)

---

**Status:** Proposal submitted for team review. Requires coordination with Hill (Player model changes, new enemy spawning logic) and Romanoff (test coverage for all new systems).
### 2026-02-20: v2 Phase Structure and Dependencies

**By:** Coulson  
**What:** Established 4-phase v2 development plan with strict dependency gates  
**Why:** Prevents feature creep into refactoring work; ensures stable foundation before new features

#### Phase Gates
1. **Phase 0 (Refactoring) → Phase 1 (Testing):** All architectural violations fixed, testability refactors complete
2. **Phase 1 (Testing) → Phase 2 (Architecture):** ≥70% code coverage achieved on core systems
3. **Phase 2 (Architecture) → Phase 3 (Features):** GameState model, persistence layer, event system in place

**Rationale:** v1 retrospective identified zero test coverage and tight coupling as critical risks. Phase 0/1 eliminate these risks before investing in new features.

---

### 2026-02-20: Interface Extraction & Refactoring Verification (consolidated)

**By:** Coulson, Hill  
**What:** Standardized pattern for extracting interfaces from concrete classes to enable mocking and alternative implementations. Includes verification checklist to catch production entrypoint issues.
**Why:** Enables mocking in unit tests, supports GUI/web/Discord bot implementations, follows dependency inversion principle. Verification catches regressions where tests pass but production code fails.

#### When to Extract Interfaces:
- Need to mock for testing (IDisplayService, IRandom, IInputService)
- Multiple implementations likely (IGamePersistence)
- Crossing architectural boundaries (Engine → Display, Engine → Systems)

#### When NOT to Extract Interfaces:
- Pure data classes (Player, Enemy, Item, Room)
- Single implementation with no test/extension need (CommandParser)

#### Verification Checklist (CRITICAL):
After any interface extraction or class renaming, **ALWAYS**:
1. Run `dotnet build` from clean state (not just `dotnet test`)
2. Explicitly check all entrypoints that instantiate the renamed/extracted class
3. Search for `new OldClassName()` references across the codebase
4. Verify production code paths, not just test code paths

**Common Trap:** Tests pass with mock implementations while production code still references old class names, causing build failures at shipping time.

**Search Pattern:**
```bash
# After renaming DisplayService → ConsoleDisplayService
$ rg "new DisplayService\(\)" --type cs
Program.cs:5:var display = new DisplayService();  # ← Would be missed without this check!
```

#### Example: IDisplayService (Hill's detailed implementation):
```csharp
public interface IDisplayService
{
    void ShowTitle();
    void ShowRoom(Room room);
    void ShowCombat(string message);
    void ShowCombatStatus(Player player, Enemy enemy);
    void ShowCombatMessage(string message);
    void ShowPlayerStats(Player player);
    void ShowInventory(Player player);
    void ShowLootDrop(Item item);
    void ShowMessage(string message);
    void ShowError(string message);
    void ShowHelp();
    void ShowCommandPrompt();
    void ShowCombatPrompt();
    string ReadPlayerName();
}

public class ConsoleDisplayService : IDisplayService
{
    // Existing DisplayService implementation
}

// Test/headless implementation
public class NullDisplayService : IDisplayService
{
    public void ShowTitle() { }
    public void ShowRoom(Room room) { }
    // ... all methods no-op or store to list for verification
}
```

#### Update Injection Sites:
```csharp
// GameLoop.cs
public GameLoop(Player player, Room startRoom, IDisplayService display, ICombatEngine combat)

// CombatEngine.cs
public CombatEngine(IDisplayService display, InventoryManager inventory, Random? rng = null)

// Program.cs
IDisplayService display = new ConsoleDisplayService();
```

**Benefits:**
- Enables unit testing with mock implementations
- Opens path for GUI/web/Discord bot implementations
- Follows dependency inversion principle
- Minimal breaking change (constructors already inject)

**Effort:** 1-2 hours (extract interface + rename class + update injection sites)

---


### 2026-02-20: Domain Model Encapsulation Pattern (consolidated)

**By:** Coulson, Hill  
**What:** Private setters + public mutation methods with validation for domain models (Player, Enemy). Use C# 9+ features (init-only, Math.Clamp).  
**Why:** Prevents invalid state (HP < 0, MaxHP = 0), enables serialization, supports analytics/achievements, supports future multiplayer

#### Pattern (Hill's detailed implementation):
```csharp
public class Player
{
    private int _hp = 100;
    private int _maxHp = 100;
    
    public string Name { get; init; } = string.Empty; // C# 9 init-only
    public int HP 
    { 
        get => _hp; 
        private set => _hp = Math.Clamp(value, 0, MaxHP); 
    }
    public int MaxHP 
    { 
        get => _maxHp; 
        private set => _maxHp = Math.Max(1, value); 
    }
    public int Attack { get; private set; } = 10;
    public int Defense { get; private set; } = 5;
    public int Gold { get; private set; }
    public int XP { get; private set; }
    public int Level { get; private set; } = 1;
    public IReadOnlyList<Item> Inventory => _inventory.AsReadOnly();
    
    private List<Item> _inventory = [];
    
    // State transition methods with validation
    public void TakeDamage(int amount)
    {
        if (amount < 0) throw new ArgumentException("Damage cannot be negative");
        HP -= amount;
    }
    
    public void Heal(int amount)
    {
        if (amount < 0) throw new ArgumentException("Heal amount cannot be negative");
        HP = Math.Min(HP + amount, MaxHP);
    }
    
    public void ModifyAttack(int delta) => Attack = Math.Max(0, Attack + delta);
    public void ModifyDefense(int delta) => Defense = Math.Max(0, Defense + delta);
    public void AddGold(int amount) => Gold = Math.Max(0, Gold + amount);
    public void AddXP(int amount) => XP = Math.Max(0, XP + amount);
    public void LevelUp(int newMaxHP, int attackBonus, int defenseBonus)
    {
        Level++;
        MaxHP = newMaxHP;
        Attack += attackBonus;
        Defense += defenseBonus;
        HP = MaxHP; // Full heal on level up
    }
    
    public void AddItem(Item item) => _inventory.Add(item);
    public bool RemoveItem(Item item) => _inventory.Remove(item);
}
```

**Breaking Change:** All direct property mutations must refactor to method calls (Player.HP = x → Player.TakeDamage/Heal).

**Rationale:** Public setters allow invalid state transitions. Encapsulation centralizes validation, enables event hooks for future analytics/achievements, critical for save/load deserialization safety.

**Effort:** 2-3 hours (refactor Player, Enemy; update all call sites in GameLoop, CombatEngine, InventoryManager)

---

### 2026-02-20: Injectable Random for Deterministic Testing (consolidated)

**By:** Coulson, Hill  
**What:** Inject System.Random via constructor (not IRandom interface) for testable, deterministic RNG-dependent systems (combat flee, loot drops, dungeon generation)  
**Why:** Enables deterministic testing, reproducible bugs, seeded runs for challenge modes

#### Pattern (prefer direct Random injection over IRandom interface):
```csharp
// Use dependency injection for Random
public class CombatEngine
{
    private readonly Random _rng;
    
    public CombatEngine(IDisplayService display, Random? rng = null)
    {
        _rng = rng ?? Random.Shared; // Use shared instance if not injected
    }
}

// Production usage
var display = new ConsoleDisplayService();
var combat = new CombatEngine(display); // Uses Random.Shared

// Testing with deterministic seed
[Fact]
public void Combat_WithFixedSeed_IsDeterministic()
{
    var rng = new Random(42); // Deterministic seed
    var combat = new CombatEngine(mockDisplay, rng);
    // ... test with reproducible outcomes
}
```

**Apply to:** CombatEngine, LootTable, DungeonGenerator constructors

**Rationale:** Direct Random injection simpler than IRandom interface wrapper; System.Random already injectable, no need for abstraction layer (YAGNI).

**Effort:** 2 hours (refactor all Random usages to constructor injection with default Random.Shared)

---


### 2026-02-20: Config-Driven Game Balance (consolidated)

**By:** Coulson, Hill
**What:** Externalized all game balance parameters to JSON config files — enemy stats, item stats, combat settings — loaded at startup with validation
**Why:** 
- **Iteration speed:** Balance tuning (HP, attack, loot) without recompilation
- **Designer-friendly:** JSON readable and editable by non-programmers
- **Version control:** Changes tracked separately from code
- **Validation:** Load-time checks catch config errors before gameplay
- **Extensibility:** Supports A/B testing, modding, difficulty presets

#### Configuration Files:
- **Data/enemy-stats.json** — Enemy archetypes (HP, Attack, Defense, XPValue, GoldReward)
- **Data/item-stats.json** — Item definitions (AttackBonus, DefenseBonus, HealAmount, Value)
- **Data/combat-settings.json** — Flee chance, dodge/crit formulas (future)
- **Data/dungeon-settings.json** — Grid size, spawn rates (future)

#### Pattern:
Static loader classes (EnemyConfig, ItemConfig) with Load(path) methods returning config DTOs. Entity constructors accept nullable config parameters with hardcoded fallbacks. Program.cs loads configs at startup.

#### Trade-offs:
- File I/O at startup (negligible overhead)
- Config must be copied to output directory (.csproj PublishFiles required)
- Two sources of truth during migration (config + hardcoded defaults) — intentional for graceful transition
### 2026-02-20: Dungnz v2 — C# Implementation Proposal
**By:** Hill
**What:** Comprehensive C# refactoring and feature proposals for v2
**Why:** Address technical debt, improve maintainability, enable advanced features

---

## 1. C#-Specific Refactoring

### 1.3 Nullable Reference Type Improvements (Priority: MEDIUM)

**Current Issues:**
- `Enemy? enemy` properly annotated in Room
- `Item?` properly annotated in LootResult
- But `List<Item> Inventory` allows nulls without guard

**Proposed Enhancement:**
```csharp
// Room.cs - add null guards
public class Room
{
    public Enemy? Enemy { get; set; }
    public List<Item> Items { get; set; } = new(); // Already initialized
    
    // Add validation
    public void AddItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item); // .NET 6+
        Items.Add(item);
    }
}

// Player.cs - use collection expressions (C# 12)
public class Player
{
    private List<Item> _inventory = [];
}
```

**Benefits:**
- Compile-time null safety
- Runtime guards prevent corruption
- Modern C# idioms

**Effort:** 30 minutes

---

### 1.4 Record Types for Data Transfer Objects (Priority: LOW)

**Current State:**
```csharp
public enum CombatResult { Won, Fled, PlayerDied }
public enum UseResult { Used, NotUsable, NotFound }
public struct LootResult { public Item? Item; public int Gold; }
```

**Proposed Refactor:**
```csharp
// Use record struct (C# 10+) for value semantics + pattern matching
public readonly record struct LootResult(Item? Item, int Gold);

// Consider discriminated union pattern for combat results
public abstract record CombatOutcome
{
    public sealed record Victory(int XPGained, int GoldGained) : CombatOutcome;
    public sealed record Fled(int DamageTaken) : CombatOutcome;
    public sealed record Defeat : CombatOutcome;
}
```

**Benefits:**
- Immutability by default
- Better pattern matching
- More expressive than enums
- Auto-generated equality/toString

**Effort:** 2 hours (refactor + update pattern matching in GameLoop)

---

## 2. New Engine Features

### 2.1 Save/Load System (Priority: HIGH)

**Requirements:**
- Serialize Player, current Room reference, dungeon graph state
- Use System.Text.Json (built-in, fast, modern)
- Handle circular references (Room.Exits bidirectional links)

**Proposed Implementation:**

```csharp
// Models/GameState.cs
public class GameState
{
    public required PlayerSaveData Player { get; init; }
    public required RoomGraph Dungeon { get; init; }
    public required Guid CurrentRoomId { get; init; }
    public DateTime SavedAt { get; init; }
}

public class PlayerSaveData
{
    public required string Name { get; init; }
    public int HP { get; init; }
    public int MaxHP { get; init; }
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int Gold { get; init; }
    public int XP { get; init; }
    public int Level { get; init; }
    public List<ItemSaveData> Inventory { get; init; } = [];
}

public class RoomGraph
{
    public Dictionary<Guid, RoomSaveData> Rooms { get; init; } = [];
    public Guid StartRoomId { get; init; }
    public Guid ExitRoomId { get; init; }
}

public class RoomSaveData
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public Dictionary<Direction, Guid> ExitIds { get; init; } = []; // Store IDs not references
    public EnemySaveData? Enemy { get; init; }
    public List<ItemSaveData> Items { get; init; } = [];
    public bool IsExit { get; init; }
    public bool Visited { get; init; }
    public bool Looted { get; init; }
}

// Engine/SaveManager.cs
public class SaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public static async Task SaveGameAsync(string filePath, GameState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
    
    public static async Task<GameState> LoadGameAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<GameState>(json, JsonOptions) 
            ?? throw new InvalidDataException("Save file corrupted");
    }
    
    // Hydration: Convert SaveData -> runtime models
    public static (Player, Room, Room) HydrateGameState(GameState state)
    {
        var roomMap = new Dictionary<Guid, Room>();
        
        // First pass: create all Room instances
        foreach (var (id, roomData) in state.Dungeon.Rooms)
        {
            roomMap[id] = new Room
            {
                Description = roomData.Description,
                IsExit = roomData.IsExit,
                Visited = roomData.Visited,
                Looted = roomData.Looted,
                Enemy = roomData.Enemy != null ? HydrateEnemy(roomData.Enemy) : null,
                Items = roomData.Items.Select(HydrateItem).ToList()
            };
        }
        
        // Second pass: wire up exits (now all rooms exist)
        foreach (var (id, roomData) in state.Dungeon.Rooms)
        {
            var room = roomMap[id];
            foreach (var (direction, targetId) in roomData.ExitIds)
            {
                room.Exits[direction] = roomMap[targetId];
            }
        }
        
        var player = HydratePlayer(state.Player);
        var currentRoom = roomMap[state.CurrentRoomId];
        var exitRoom = roomMap[state.Dungeon.ExitRoomId];
        
        return (player, currentRoom, exitRoom);
    }
    
    // Dehydration: Convert runtime models -> SaveData
    public static GameState CreateGameState(Player player, Room currentRoom, Room startRoom, Room exitRoom)
    {
        var roomMap = BuildRoomGraph(startRoom);
        var currentRoomId = roomMap.First(kvp => kvp.Value == currentRoom).Key;
        
        return new GameState
        {
            Player = DehydratePlayer(player),
            Dungeon = new RoomGraph
            {
                Rooms = roomMap.ToDictionary(
                    kvp => kvp.Key,
                    kvp => DehydrateRoom(kvp.Key, kvp.Value, roomMap)
                ),
                StartRoomId = roomMap.First(kvp => kvp.Value == startRoom).Key,
                ExitRoomId = roomMap.First(kvp => kvp.Value == exitRoom).Key
            },
            CurrentRoomId = currentRoomId,
            SavedAt = DateTime.UtcNow
        };
    }
    
    private static Dictionary<Guid, Room> BuildRoomGraph(Room start)
    {
        var map = new Dictionary<Room, Guid>();
        var queue = new Queue<Room>();
        queue.Enqueue(start);
        map[start] = Guid.NewGuid();
        
        while (queue.Count > 0)
        {
            var room = queue.Dequeue();
            foreach (var exit in room.Exits.Values)
            {
                if (!map.ContainsKey(exit))
                {
                    map[exit] = Guid.NewGuid();
                    queue.Enqueue(exit);
                }
            }
        }
        
        return map.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
```

**Key Decisions:**
- Assign Guid to each Room during serialization (not stored in runtime model)
- Replace Room references with Guid during serialization
- Hydration rebuilds object graph from IDs
- Async file I/O for responsiveness
- System.Text.Json native (no Newtonsoft dependency)

**Benefits:**
- Persistence between sessions
- Enables "save before boss" strategy
- Foundation for cloud sync/multiplayer

**Effort:** 6-8 hours

---

### 2.2 Procedural Generation Improvements (Priority: MEDIUM)

**Current Issues:**
- Fixed 5x4 grid (20 rooms)
- Predictable layout (always rectangular)
- No locked doors, keys, or puzzles

**Proposed Enhancements:**

```csharp
// Engine/DungeonGenerator.cs enhancements

public class GeneratorOptions
{
    public int MinRooms { get; init; } = 15;
    public int MaxRooms { get; init; } = 30;
    public double BranchingFactor { get; init; } = 0.3; // % of rooms with >2 exits
    public double TreasureRoomChance { get; init; } = 0.15;
    public double LockedDoorChance { get; init; } = 0.1;
    public int? Seed { get; init; }
}

public class DungeonGenerator
{
    private readonly Random _rng;
    private readonly GeneratorOptions _options;
    
    public (Room start, Room exit) Generate()
    {
        var roomCount = _rng.Next(_options.MinRooms, _options.MaxRooms + 1);
        
        // Use graph-based generation instead of grid
        var rooms = GenerateRoomGraph(roomCount);
        var (start, exit) = DesignateSpecialRooms(rooms);
        
        PlaceEnemies(rooms, start, exit);
        PlaceTreasures(rooms);
        PlaceLockedDoors(rooms, start);
        
        return (start, exit);
    }
    
    private List<Room> GenerateRoomGraph(int count)
    {
        // Procedural graph generation algorithm:
        // 1. Start with single room
        // 2. Expand: pick random room, add 1-3 connected rooms
        // 3. Add cycles: occasionally connect existing rooms
        // 4. Ensure path from start to end exists
    }
    
    private void PlaceLockedDoors(List<Room> rooms, Room start)
    {
        // Key-door pairs: place key before door in dungeon flow
        // Requires topological sort or flow analysis
    }
}
```

**Benefits:**
- Higher replayability
- More varied dungeon layouts
- Foundation for puzzle mechanics

**Effort:** 8-10 hours

---

## 3. Model Improvements

### 3.1 Serialization-Ready Models (Priority: HIGH)

**Current Issues:**
- Bidirectional Room.Exits creates circular references
- No unique IDs for Room instances
- List<T> allows external mutation

**Proposed Changes:**

```csharp
// Add IIdentifiable interface for serialization
public interface IIdentifiable
{
    Guid Id { get; }
}

// Room.cs - Add ID but keep it internal to serialization
public class Room
{
    internal Guid? SerializationId { get; set; } // Only used during save/load
    
    public string Description { get; set; } = string.Empty;
    public Dictionary<Direction, Room> Exits { get; init; } = new();
    public Enemy? Enemy { get; set; }
    
    private List<Item> _items = [];
    public IReadOnlyList<Item> Items => _items.AsReadOnly();
    public void AddItem(Item item) => _items.Add(item);
    public bool RemoveItem(Item item) => _items.Remove(item);
    
    public bool IsExit { get; set; }
    public bool Visited { get; set; }
    public bool Looted { get; set; }
}
```

**Benefits:**
- Clean separation of runtime vs serialization concerns
- Immutable collections prevent accidental mutations
- Guid only allocated during save (not in-memory overhead)

---

### 3.2 Enemy Stats as Readonly Records (Priority: LOW)

**Current State:**
```csharp
public abstract class Enemy
{
    public string Name { get; set; } = string.Empty;
    public int HP { get; set; }
    public int MaxHP { get; set; }
    // ...
}
```

**Problem:** Mutable stats allow accidental corruption

**Proposed Refactor:**
```csharp
public abstract class Enemy
{
    private int _hp;
    
    protected Enemy(string name, int maxHP, int attack, int defense, int xpValue)
    {
        Name = name;
        MaxHP = maxHP;
        _hp = maxHP;
        Attack = attack;
        Defense = defense;
        XPValue = xpValue;
    }
    
    public string Name { get; }
    public int HP 
    { 
        get => _hp; 
        private set => _hp = Math.Clamp(value, 0, MaxHP); 
    }
    public int MaxHP { get; }
    public int Attack { get; }
    public int Defense { get; }
    public int XPValue { get; }
    public abstract LootTable LootTable { get; }
    
    public void TakeDamage(int amount) => HP -= amount;
    public bool IsAlive => HP > 0;
}
```

**Benefits:**
- Immutable stats prevent bugs
- Consistent with Player encapsulation refactor
- Clearer intent (only HP changes during combat)

**Effort:** 3 hours (refactor + update all enemy subclasses)

---

## 4. .NET Idiom Improvements

### 4.1 Collection Expressions (C# 12)

```csharp
// Before
public List<Item> Items { get; set; } = new();
private List<Item> _inventory = new List<Item>();

// After
public List<Item> Items { get; set; } = [];
private List<Item> _inventory = [];
```

### 4.2 Primary Constructors (C# 12)

```csharp
// Before
public class GameLoop
{
    private readonly Player _player;
    private readonly IDisplayService _display;
    
    public GameLoop(Player player, IDisplayService display)
    {
        _player = player;
        _display = display;
    }
}

// After
public class GameLoop(Player player, IDisplayService display, ICombatEngine combat)
{
    // Fields automatically created from constructor parameters
}
```

### 4.3 File-Scoped Namespaces

```csharp
// Before
namespace Dungnz.Models
{
    public class Player { }
}

// After
namespace Dungnz.Models;

public class Player { }
```

### 4.4 Required Members (C# 11)

```csharp
public class Item
{
    public required string Name { get; init; }
    public required ItemType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public int AttackBonus { get; init; }
    public int DefenseBonus { get; init; }
    public int HealAmount { get; init; }
}
```

**Effort for all idiom improvements:** 1-2 hours

---

## 5. Priority Matrix

| Feature | Priority | Effort | Dependencies | Impact |
|---------|----------|--------|--------------|--------|
| Player Encapsulation | HIGH | 2-3h | None | Unblocks save/load, prevents bugs |
| IDisplayService Interface | HIGH | 1-2h | None | Unblocks testing, alternative UIs |
| Save/Load System | HIGH | 6-8h | Player Encapsulation | Major feature, user retention |
| Nullable Improvements | MEDIUM | 30m | None | Code quality, null safety |
| Procedural Gen V2 | MEDIUM | 8-10h | None | Replayability |
| Record Types Refactor | LOW | 2h | None | Code quality, modern C# |
| Random Injection | LOW | 2h | IDisplayService | Testing infrastructure |
| Enemy Encapsulation | LOW | 3h | Player Encapsulation | Consistency |
| .NET Idiom Updates | LOW | 1-2h | None | Readability, modern style |

---

## 6. Recommended Implementation Order

1. **Sprint 1 (Testing Foundation):**
   - IDisplayService interface extraction
   - Random dependency injection
   - Basic xUnit/NUnit setup

2. **Sprint 2 (Encapsulation):**
   - Player encapsulation refactor
   - Enemy encapsulation refactor
   - Nullable reference improvements

3. **Sprint 3 (Persistence):**
   - Save/Load system implementation
   - GameState serialization models
   - Integration with GameLoop

4. **Sprint 4 (Polish):**
   - Procedural generation improvements
   - .NET idiom modernization
   - Record types refactor

---

## 7. Breaking Changes Summary

- **Player.cs:** Public setters → private setters + methods (callers must refactor)
- **DisplayService:** Renamed to ConsoleDisplayService, interface extracted
- **Enemy.cs:** Stats become readonly (constructors must initialize)
- **Room.cs:** Items/Inventory exposed as IReadOnlyList (callers use Add/Remove methods)

All breaking changes are internal to the project; no public API contracts exist yet.

---

## 8. Open Questions

1. Should save files be binary (smaller) or JSON (human-readable)?
   - **Recommendation:** JSON for v2 (debuggability), consider binary for v3
   
2. Should Random be static (Random.Shared) or injected everywhere?
   - **Recommendation:** Inject in testable components, use Random.Shared in leaf nodes
   
3. Should we add async/await for future multiplayer?
   - **Recommendation:** Not yet; premature for single-player console game

4. Target framework: .NET 9 or backport to .NET 8 LTS?
   - **Current:** .NET 9. Recommendation: Stay on 9 for latest C# features.

---

**Status:** Proposal ready for team review
**Next Step:** Coordinate with Barton (CombatEngine updates), Romanoff (test harness)
### 2026-02-20: v2 Testing Strategy and Infrastructure Requirements
**By:** Romanoff
**Context:** Planning v2 testing infrastructure for C# dungeon crawler (Dungnz). v1 shipped with zero automated tests; all quality verification was manual code review.

---

## 1. Testing Infrastructure Requirements

### Framework Selection: xUnit
**Decision:** Use xUnit 2.6+ as primary test framework.

**Rationale:**
- Industry standard for .NET projects
- Better async/await support than NUnit
- Built-in theory/inline data for parameterized tests
- Cleaner test isolation (new class instance per test)
- Strong IDE support (Rider/VS)

**Implementation:**
```xml
<PackageReference Include="xunit" Version="2.6.6" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

### Test Project Structure
```
TextGame.sln
├── Dungnz.csproj (main)
└── Dungnz.Tests/
    ├── Dungnz.Tests.csproj
    ├── Unit/
    │   ├── CombatEngineTests.cs
    │   ├── InventoryManagerTests.cs
    │   ├── LootTableTests.cs
    │   ├── PlayerTests.cs (post-encapsulation)
    │   └── CommandParserTests.cs
    ├── Integration/
    │   ├── GameLoopTests.cs
    │   ├── DungeonGeneratorTests.cs
    │   └── CombatLootIntegrationTests.cs
    ├── Fixtures/
    │   ├── TestDisplayService.cs (mock IDisplayService)
    │   ├── DeterministicRandom.cs (seeded Random wrapper)
    │   └── TestDataFactory.cs (builder pattern for test entities)
    └── TestHelpers/
        └── AssertionExtensions.cs
```

### Mocking Strategy

**IDisplayService Extraction (PREREQUISITE):**
- Extract interface from DisplayService (Hill owns)
- All tests use `Mock<IDisplayService>` via Moq
- No console I/O during test runs
- Verify critical messages (e.g., "You defeated", "LEVEL UP!")

**Deterministic Random Injection:**
- Refactor `CombatEngine` and `LootTable` to accept `Random` via constructor
- Production: inject `new Random()` (or seeded Random for reproducible playtests)
- Tests: inject `new Random(42)` for predictable outcomes
- Enables testing edge cases: guaranteed flee success/failure, guaranteed loot drops

**Example Pattern:**
```csharp
public class CombatEngine : ICombatEngine
{
    private readonly IDisplayService _display;
    private readonly Random _rng;
    
    public CombatEngine(IDisplayService display, Random? rng = null)
    {
        _display = display;
        _rng = rng ?? new Random();
    }
}
```

---

## 2. Coverage Strategy

### MUST HAVE TESTS (High Risk, Complex Logic)

**CombatEngine (Unit):**
- ✅ Player kills enemy → CombatResult.Won, loot drops, XP awarded
- ✅ Enemy kills player → CombatResult.PlayerDied, HP=0
- ✅ Flee success → CombatResult.Fled, player HP unchanged
- ✅ Flee failure → Player takes damage, combat continues
- ✅ Flee failure kills player → CombatResult.PlayerDied (not Fled)
- ✅ Level-up during combat → stats increase, HP restored to new MaxHP
- ✅ Minimum damage rule (1 dmg even if Defense > Attack)
- ✅ Loot distribution: gold added to Player.Gold, item added to Inventory

**LootTable (Unit):**
- ✅ Deterministic drop with seeded Random
- ✅ No drops when all chances fail
- ✅ First-match-wins behavior (multiple items, first success takes priority)
- ✅ Gold range boundaries (minGold, maxGold, equal values)
- ✅ Empty loot table (no crashes)

**InventoryManager (Unit):**
- ✅ TakeItem: success, item not found, item removed from room and added to inventory
- ✅ UseItem consumable: HP restored, capped at MaxHP, item removed
- ✅ UseItem weapon: Attack increased, item removed, permanent stat change
- ✅ UseItem armor: Defense increased, item removed, permanent stat change
- ✅ UseItem not found → UseResult.NotFound
- ✅ Empty inventory use → no crash, NotFound returned

**CommandParser (Unit):**
- ✅ Parse all valid commands (GO, LOOK, TAKE, USE, INVENTORY, STATS, HELP, QUIT)
- ✅ Parse direction shortcuts (n/s/e/w → GO north/south/east/west)
- ✅ Case insensitivity (GO/go/Go all work)
- ✅ Commands with arguments ("take sword", "use potion")
- ✅ Invalid commands → CommandType.Invalid

**Player (Unit — AFTER Hill's Encapsulation Refactor):**
- ✅ TakeDamage: HP reduced, capped at 0 (not negative)
- ✅ Heal: HP increased, capped at MaxHP
- ✅ ModifyAttack: negative values blocked OR stats can't go below 0
- ✅ ModifyDefense: same rules
- ✅ State validation: MaxHP > 0, HP <= MaxHP

**DungeonGenerator (Integration):**
- ✅ Generates 5x4 grid (20 rooms)
- ✅ All rooms connected (BFS reachability test)
- ✅ Spawn room has no enemy
- ✅ Exit room has boss enemy (DungeonBoss type)
- ✅ Boss drops loot only once (Room.Looted flag prevents re-drops)
- ✅ Bidirectional exits (if Room A → North → Room B, then Room B → South → Room A)

**GameLoop (Integration):**
- ✅ Player enters room with enemy → combat triggers automatically
- ✅ Dead enemy cleared from room after CombatResult.Won (Enemy set to null)
- ✅ Exit room blocked if boss alive
- ✅ Exit room accessible after boss dead
- ✅ Win condition: player in exit room AND boss dead
- ✅ Player death ends game

### MANUAL TESTING ONLY (Low Risk, UI-Heavy)

**DisplayService:**
- Output formatting (borders, colors, spacing)
- Console rendering layout
- Help text display
- Rationale: Visual validation more efficient than brittle string assertions

**End-to-End Gameplay:**
- Full playthroughs for balance testing
- Narrative flow and immersion
- Player experience validation
- Rationale: Automated E2E for text games is fragile and expensive to maintain

### OPTIONAL (Nice-to-Have, Lower Priority)

**EnemyFactory (Unit):**
- Returns correct enemy subclass for each type
- Enemy stats match specifications
- LootTable configured correctly per enemy type

**Room Navigation (Unit):**
- Directional movement logic
- Room.Visited flag updates

---

## 3. Edge Case Inventory (High-Risk Areas for v2)

### From v1 Code Review (WI-10 Learnings)

**Fixed in v1 (Regression Tests Required):**
1. **Dead enemy cleanup** — Enemy remains in room after HP=0 unless explicitly nulled
   - Test: After CombatResult.Won, assert `room.Enemy == null`
2. **DisplayService violations** — Direct Console.Write calls bypassing architecture
   - Test: Verify all I/O goes through IDisplayService (code review or static analysis)
3. **Flee penalty death edge case** — Flee failure damage kills player → must return PlayerDied, not Fled
   - Test: Setup player at 5 HP, enemy attack 10, flee fails → assert CombatResult.PlayerDied

**Unfixed in v1 (Not Bugs, But Edge Cases to Monitor):**
4. **Flee-and-return behavior** — Player flees combat, enemy HP persists on re-entry
   - Current: Accepted behavior (enemy doesn't reset)
   - v2 Risk: If save/load added, must serialize enemy HP state
5. **Item stacking exploit** — Player can equip infinite weapons/armor (stats stack)
   - Current: Out of scope for MVP
   - v2 Risk: If equipment slots added, must prevent stacking
6. **Empty LootTable** — Enemy with no drops configured (gold=0, no items)
   - Current: Works fine (returns empty LootResult)
   - v2 Risk: Ensure UI doesn't show "You found nothing!" spam

### New Risks for v2 Features

**Save/Load System (If Added):**
- Serialize Player state: HP, MaxHP, Attack, Defense, Gold, XP, Level, Inventory
- Serialize Room state: Visited, Looted, Enemy (including HP if alive)
- Deserialize invalid data: HP > MaxHP, negative stats, missing inventory items
- JSON deserialization null safety (System.Text.Json nullable handling)

**Player Encapsulation Refactor (Confirmed v2 Work):**
- Public setters removed → all mutations via methods
- TakeDamage/Heal must validate inputs (negative values, overflow)
- Stat modification must prevent negative Attack/Defense
- HP capping logic (HP <= MaxHP enforced in all paths)

**IDisplayService Extraction (Confirmed v2 Work):**
- All consumers updated to IDisplayService (CombatEngine, GameLoop, InventoryManager)
- No direct Console.Write calls remain (grep verification or analyzer rule)

**Deterministic Random Injection (Confirmed v2 Work):**
- CombatEngine accepts Random via constructor (flee rolls, damage variance if added)
- LootTable accepts Random via constructor (drop rolls)
- EnemyFactory uses Random for dungeon placement (if randomized)

**New Enemy Types or Abilities:**
- Multi-turn abilities (e.g., "charge attack" that skips a turn)
- Status effects (poison, stun, bleed)
- Enemy AI variations (aggressive, defensive, fleeing enemies)
- Test: State machine transitions, effect duration, stacking rules

**Dungeon Generation Changes:**
- Non-grid layouts (caves, mazes)
- Multiple floors or branches
- Locked doors requiring keys
- Test: Reachability, item-gate logic (can player soft-lock?), cycle detection

---

## 4. Quality Gates (What Must Pass Before Features Ship)

### Build Gates (Automated, CI/CD)

**Unit Tests:**
- ✅ All CombatEngine tests pass (100% coverage of combat logic)
- ✅ All LootTable tests pass (deterministic drops verified)
- ✅ All InventoryManager tests pass (item lifecycle validated)
- ✅ All CommandParser tests pass (input parsing robust)
- **Threshold:** 0 failures allowed

**Integration Tests:**
- ✅ DungeonGenerator produces valid dungeons (connectivity, boss placement)
- ✅ GameLoop combat triggers and enemy cleanup verified
- ✅ Win/lose conditions work correctly
- **Threshold:** 0 failures allowed

**Code Coverage (After Initial Test Suite):**
- CombatEngine: 90%+ line coverage
- LootTable: 100% (small, critical)
- InventoryManager: 85%+
- Player (post-refactor): 90%+
- **Rationale:** High-risk systems require strong coverage; UI/Display can be lower

### Manual Gates (Romanoff Review)

**Architecture Review:**
- ✅ No direct Console.Write calls (all I/O via IDisplayService)
- ✅ No hardcoded Random instances (all injected)
- ✅ Player encapsulation enforced (no public setters on stat properties)
- ✅ Null safety verified (nullable reference types respected)

**Edge Case Validation:**
- ✅ Dead enemy cleanup (room.Enemy == null after combat)
- ✅ Flee penalty death (CombatResult.PlayerDied if flee damage kills)
- ✅ Level-up HP restore (HP = MaxHP after level increase)
- ✅ Minimum damage (1 dmg even if Defense >= Attack)
- ✅ Empty inventory USE (graceful error, no crash)
- ✅ Boss loot one-time-only (Room.Looted flag prevents re-drops)

**Playtest Validation (Manual):**
- ✅ Full playthrough (spawn to boss kill to exit)
- ✅ Flee-and-return (enemy HP persists, re-engagement works)
- ✅ Item usage (heal, equip weapon, equip armor)
- ✅ Level-up mid-combat (stats update, combat continues)
- ✅ Player death (game ends gracefully, no crash)

### Rejection Criteria (Romanoff Blocks Merge)

**REJECT if any of the following:**
- ❌ Unit tests fail (any failure blocks merge)
- ❌ Code coverage drops below threshold for modified systems
- ❌ Architectural violations introduced (Console.Write, hardcoded Random)
- ❌ Edge case regression (dead enemy not cleaned up, flee death not handled)
- ❌ Null reference exceptions in playtest
- ❌ Player can soft-lock (e.g., locked door with no key, unreachable boss)

**APPROVE with minor issues if:**
- ⚠️ DisplayService formatting issues (fix in follow-up)
- ⚠️ Help text outdated (fix in follow-up)
- ⚠️ Balance issues (enemy too weak/strong — tune in follow-up)

---

## Implementation Timeline (v2 Blocking Work)

**Phase 1: Infrastructure (Sprint 1, Week 1-2):**
1. Hill: Extract IDisplayService interface, refactor consumers
2. Barton: Refactor CombatEngine/LootTable to accept injected Random
3. Romanoff: Create Dungnz.Tests project, add xUnit/Moq/FluentAssertions
4. Romanoff: Implement TestDisplayService and DeterministicRandom fixtures

**Phase 2: Unit Tests (Sprint 1, Week 2-3):**
5. Romanoff: CombatEngine unit tests (8 tests, all edge cases)
6. Romanoff: LootTable unit tests (5 tests, deterministic drops)
7. Romanoff: InventoryManager unit tests (6 tests, item lifecycle)
8. Romanoff: CommandParser unit tests (10 tests, all input cases)

**Phase 3: Integration Tests (Sprint 2, Week 1):**
9. Romanoff: DungeonGenerator integration tests (6 tests, connectivity + boss)
10. Romanoff: GameLoop integration tests (5 tests, combat triggers + win/lose)

**Phase 4: Regression Suite (Sprint 2, Week 2):**
11. Romanoff: WI-10 edge case tests (dead enemy cleanup, flee death, etc.)
12. Romanoff: Playtest validation suite (manual test cases documented)

**Phase 5: CI/CD Integration (Sprint 2, Week 2):**
13. Configure GitHub Actions or similar to run `dotnet test` on every PR
14. Block merge if tests fail or coverage drops

**Estimated Effort:** 1.5-2 sprints (Romanoff dedicated, with Hill/Barton infrastructure support)

---

## C# Testing Patterns and Conventions

### Arrange-Act-Assert Structure (Always)
```csharp
[Fact]
public void RunCombat_PlayerKillsEnemy_ReturnsWon()
{
    // Arrange
    var mockDisplay = new Mock<IDisplayService>();
    var rng = new Random(42); // Deterministic
    var combat = new CombatEngine(mockDisplay.Object, rng);
    var player = new Player { HP = 100, Attack = 50, Defense = 10 };
    var enemy = new Enemy { HP = 10, Attack = 5, Defense = 0 };

    // Act
    var result = combat.RunCombat(player, enemy);

    // Assert
    Assert.Equal(CombatResult.Won, result);
    Assert.True(enemy.HP <= 0);
}
```

### Parameterized Tests (Theory + InlineData)
```csharp
[Theory]
[InlineData("GO NORTH", CommandType.Go, "NORTH")]
[InlineData("n", CommandType.Go, "NORTH")]
[InlineData("take sword", CommandType.Take, "sword")]
[InlineData("invalid", CommandType.Invalid, "")]
public void Parse_VariousInputs_ReturnsExpectedCommand(string input, CommandType expectedType, string expectedArg)
{
    var cmd = CommandParser.Parse(input);
    Assert.Equal(expectedType, cmd.Type);
    Assert.Equal(expectedArg, cmd.Argument);
}
```

### Mocking IDisplayService (Verify Critical Messages)
```csharp
[Fact]
public void RunCombat_PlayerLevelsUp_ShowsLevelUpMessage()
{
    // Arrange
    var mockDisplay = new Mock<IDisplayService>();
    var combat = new CombatEngine(mockDisplay.Object);
    var player = new Player { XP = 95, Level = 1 }; // 5 XP to level 2
    var enemy = new Enemy { HP = 1, XPValue = 10 }; // Gives 10 XP

    // Act
    combat.RunCombat(player, enemy);

    // Assert
    mockDisplay.Verify(d => d.ShowMessage(It.Is<string>(s => s.Contains("LEVEL UP"))), Times.Once);
    Assert.Equal(2, player.Level);
}
```

### System.Text.Json Test Fixtures (If Save/Load Added)
```csharp
[Fact]
public void DeserializePlayer_ValidJson_RestoresState()
{
    // Arrange
    var json = """
    {
        "Name": "TestHero",
        "HP": 75,
        "MaxHP": 100,
        "Attack": 15,
        "Defense": 8,
        "Gold": 50,
        "XP": 120,
        "Level": 2,
        "Inventory": [
            {"Name": "Health Potion", "Type": "Consumable", "HealAmount": 30}
        ]
    }
    """;

    // Act
    var player = JsonSerializer.Deserialize<Player>(json);

    // Assert
    Assert.NotNull(player);
    Assert.Equal("TestHero", player.Name);
    Assert.Equal(75, player.HP);
    Assert.Single(player.Inventory);
}
```

### Deterministic Random for Combat Tests
```csharp
[Fact]
public void RunCombat_FleeAttempt_SucceedsWithSeed42()
{
    // Arrange
    var rng = new Random(42); // Seed 42 → first NextDouble() < 0.5
    var combat = new CombatEngine(mockDisplay.Object, rng);
    var player = new Player { HP = 50 };
    var enemy = new Enemy { HP = 50 };

    // Mock player input: "F" (flee)
    // (In real implementation, would inject input reader or use integration test)

    // Act
    var result = combat.RunCombat(player, enemy);

    // Assert
    Assert.Equal(CombatResult.Fled, result);
    Assert.Equal(50, player.HP); // No damage taken
}
```

---

## Skills and Patterns for Team

**Dependency Injection for Testability:**
- Always inject dependencies (IDisplayService, Random) via constructor
- Use optional parameters for production defaults: `Random? rng = null`
- Production code: `new CombatEngine(displayService)` (uses new Random())
- Test code: `new CombatEngine(mockDisplay, new Random(seed))` (deterministic)

**Interface Extraction for External Dependencies:**
- Console I/O → IDisplayService
- File I/O → IFileSystem (if save/load added)
- Time → IClock (if time-based mechanics added)
- Rationale: Enables mocking, testability, alternative implementations

**Builder Pattern for Test Data:**
```csharp
public class PlayerBuilder
{
    private int _hp = 100;
    private int _attack = 10;
    private int _defense = 5;

    public PlayerBuilder WithHP(int hp) { _hp = hp; return this; }
    public PlayerBuilder WithAttack(int atk) { _attack = atk; return this; }
    public Player Build() => new Player { HP = _hp, Attack = _attack, Defense = _defense };
}

// Usage:
var player = new PlayerBuilder().WithHP(10).WithAttack(50).Build();
```

**Fluent Assertions for Readability:**
```csharp
// Instead of:
Assert.Equal(CombatResult.Won, result);
Assert.True(player.XP == 110);

// Use:
result.Should().Be(CombatResult.Won);
player.XP.Should().Be(110);
player.Inventory.Should().ContainSingle(i => i.Name == "Iron Sword");
```

---

## Summary

v2 testing strategy prioritizes:
1. **High-risk systems** (combat, loot, inventory) get comprehensive unit tests
2. **Deterministic testing** via injected Random for reproducible edge cases
3. **Mock IDisplayService** for headless test runs (no console I/O)
4. **Integration tests** for dungeon generation and game loop state transitions
5. **Regression suite** for WI-10 fixed bugs (dead enemy cleanup, flee death)
6. **Quality gates** block merges if tests fail or coverage drops

**Blocking work for v2:** Infrastructure refactors (IDisplayService extraction, Random injection, Player encapsulation) must complete before feature work begins. Estimated 1.5-2 sprints for full test harness.

**Next Steps:**
1. Hill extracts IDisplayService (1-2 hours)
2. Barton refactors CombatEngine/LootTable for Random injection (2-3 hours)
3. Romanoff creates test project and fixtures (1 day)
4. Romanoff writes unit tests for combat/loot/inventory (3-4 days)
5. Romanoff writes integration tests for dungeon/gameloop (2 days)
6. CI/CD integration (1 day)
### 2026-02-20: Player Encapsulation Implementation Pattern

**By:** Hill  
**Context:** GitHub Issue #2 — Player encapsulation refactor

**What:**
Implemented complete Player encapsulation with private setters, validated mutation methods, and OnHealthChanged event. All 9 Player properties now use private set. Added 7 public methods (TakeDamage, Heal, AddGold, AddXP, ModifyAttack, ModifyDefense, LevelUp) with input validation and clamping.

**Why:**
1. **Prevent invalid state:** Direct property setters allowed negative HP, exceeding MaxHP, stat underflows
2. **Enable future features:** Controlled state changes support save/load, analytics, achievements
3. **Clean API:** Game systems interact through intent-revealing methods (TakeDamage vs HP -= dmg)
4. **Event-driven:** OnHealthChanged event enables reactive systems without coupling

**Pattern Details:**
```csharp
// Private setters for all mutable state
public int HP { get; private set; } = 100;

// Validated mutation with clamping
public void TakeDamage(int amount)
{
    if (amount < 0)
        throw new ArgumentException("Damage amount cannot be negative.", nameof(amount));
    
    var oldHP = HP;
    HP = Math.Max(0, HP - amount);
    
    if (HP != oldHP)
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(oldHP, HP));
}

// Event for state change notifications
public event EventHandler<HealthChangedEventArgs>? OnHealthChanged;
```

**Caller Impact:**
- CombatEngine: 4 call sites (flee damage, combat damage, gold, XP)
- GameLoop: 3 call sites (heal, equip weapon/armor)
- All direct property assignments replaced with method calls

**Testing:**
- Build passes cleanly
- Existing game loop logic unchanged (same behavior, safer implementation)

**Recommendation:**
- Apply same pattern to Enemy class (TakeDamage, ModifyStats)
- Consider IReadOnlyList<Item> for Inventory exposure (prevent external mutation)

**PR:** #26 (squad/2-player-encapsulation)
---

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

---

### 2026-02-20: GameEvents Event System Architecture
**By:** Coulson
**What:** Instance-based event system with optional subscribers using nullable dependency injection
**Why:** 
- Testability requires instance-based (not static) events for mocking and isolation
- Nullable GameEvents? parameter pattern removes tight coupling — events fire unconditionally, subscribers are optional
- Strongly-typed EventArgs provide compile-time safety and rich context for subscribers
- Firing events AFTER state changes ensures subscribers see consistent game state
- Pattern established: inject shared GameEvents instance into subsystems (CombatEngine, GameLoop) at construction

---

### 2026-02-20: Config-Driven Game Balance

**By:** Hill  
**What:** Moved all enemy and item stats from hardcoded C# to external JSON config files (Data/enemy-stats.json, Data/item-stats.json) loaded at startup via EnemyConfig/ItemConfig static classes with validation.

**Why:**  
- **Iteration speed:** Game balance tuning (HP, attack values, loot tables) without recompilation
- **Designer-friendly:** JSON is human-readable and editable by non-programmers
- **Version control:** Balance changes tracked in git separately from code
- **Validation:** Load-time checks with descriptive exceptions catch config errors before gameplay

**Pattern:**  
Static loader classes with Load(path) methods returning config DTOs (EnemyStats, ItemStats records). Entity constructors accept nullable config parameters with hardcoded fallbacks. Program.cs loads configs at startup, crashes with clear error if invalid.

**Trade-offs:**  
- Adds file I/O dependency at startup (negligible overhead)
- Config must be copied to output directory (.csproj configuration required)
- Two sources of truth during migration (config + hardcoded defaults)

---

### 2026-02-20: Two-Pass Serialization for Circular Object Graphs
**By:** Hill  
**What:** Implemented Guid-based two-pass serialization to handle circular Room.Exits references in save/load system.  
**Why:**  
- Room graph contains circular references (Room.Exits points to other Rooms which point back)
- Standard JSON serialization fails on circular references
- Two-pass approach: serialize Guids instead of object references, then rehydrate object graph from Guids
- BFS traversal ensures all reachable rooms are captured
- Guid.NewGuid() provides unique IDs without central ID management
- System.Text.Json (native) preferred over Newtonsoft.Json (external dependency)

**Pattern:**
1. **Serialize:** BFS collect all rooms → RoomSaveData DTOs replace `Room` refs with `Guid` refs → JSON
2. **Deserialize:** JSON → create all Room objects → wire Exits by resolving Guids through dictionary

**Applicability:** Any domain with circular object graphs needing persistence (e.g., enemy spawn graphs, quest dependency trees, dialogue trees)

### 2026-02-20: AppData Save Location for User Data
**By:** Hill  
**What:** Saves stored in `Environment.GetFolderPath(SpecialFolder.ApplicationData)/Dungnz/saves/`  
**Why:**  
- Follows .NET conventions for user-specific application data
- Cross-platform (Windows: %APPDATA%, Linux: ~/.config, macOS: ~/Library/Application Support)
- Survives application upgrades and re-installs
- No admin privileges required

### 2026-02-20: Specific Exception Handling for User-Facing Errors
**By:** Hill  
**What:** Save/load handlers catch `FileNotFoundException`, `InvalidDataException`, and generic `Exception` separately  
**Why:**  
- Different error types warrant different user messages
- FileNotFoundException → "not found, use LIST"
- InvalidDataException → "corrupt save"  
- Generic Exception → "unexpected error"  
- Prevents cryptic .NET stack traces in game console
- Guides users to resolution (e.g., suggests LIST command for typos)

**Pattern:** Catch specific exceptions first, then generic Exception as fallback, always with user-friendly messages.

---

### 2026-02-20: CI Quality Gate Infrastructure Decision

**By:** Romanoff (Tester)

**What:** Established GitHub Actions CI pipeline with 70% code coverage threshold as mandatory quality gate. Fixed test project framework compatibility (net10.0 → net9.0).

**Why:** 
- **CI automation:** Prevents broken code from merging to master. Every push/PR now runs full build + test suite.
- **Coverage enforcement:** 70% line coverage threshold chosen as pragmatic starting point — high enough to catch untested logic, low enough to be achievable without blocking development velocity.
- **Framework fix:** Test project targeted net10.0 (doesn't exist), breaking CI runners. Downgraded to net9.0 to match main project and available SDKs.
- **Tooling:** Used existing Coverlet packages (already in test project), avoided adding new dependencies.

**Impact:**
- Blocks merges below 70% coverage — developers must write tests before shipping features.
- CI will catch compilation errors, test failures, and coverage regressions before code review.
- Foundation for future quality gates: mutation testing, static analysis, performance benchmarks.

**Decision Points:**
- Line coverage vs branch coverage: **Line** chosen for simplicity (branch coverage can be added later as second-tier gate).
- Threshold value: **70%** balances rigor vs pragmatism. Can be raised to 80-90% once baseline is stable.
- Workflow triggers: **Push to master + all PRs**. Does not run on draft PRs to save CI minutes.

**Files:**
- `.github/workflows/ci.yml` (new)
- `Dungnz.Tests/Dungnz.Tests.csproj` (framework fix)

### 2026-02-20: Ability System Architecture

**By:** Barton  
**What:** Combat abilities use in-memory data structures (List<Ability>) rather than JSON config files  
**Why:** Simpler initial implementation for 4 fixed abilities. Hardcoding in AbilityManager constructor provides type safety and avoids deserialization complexity. If ability count grows significantly (>10) or requires frequent balance tuning by non-developers, consider migrating to JSON config similar to enemy/item stats.
