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

**By:** Barton  
**What:** Proposed gameplay features, content expansions, and balance improvements for Dungnz v2  
**Why:** Planning phase for v2 to maximize fun, replayability, and strategic depth

## 1. NEW GAMEPLAY FEATURES (Ranked by Impact/Effort)


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


**What:** Persistent equipment with swap mechanics
**Changes:**
- Player gets fixed slots: `EquippedWeapon`, `EquippedArmor`, `EquippedAccessory`
- Equipping new item replaces old one (returned to inventory)
- Stat bonuses removed when unequipped
- Accessories: new item type (e.g., "Ring of Vitality" +15 MaxHP, "Berserker Amulet" +3 ATK -2 DEF)

**Why:** Enables build experimentation. Prevents infinite stat stacking. Adds inventory pressure (10-slot limit). Moderate effort due to stat recalculation complexity.

---


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


- **Level 3:** Unlock Power Strike skill
- **Level 5:** Unlock Defensive Stance skill + Inventory capacity +5 slots
- **Level 8:** Unlock Poison Dart skill + Crit chance +5%
- **Level 10:** Unlock Second Wind skill + Start combat with +50% mana

**Why:** Makes leveling feel like major power spike. Creates aspirational goals. Rewards exploration.

---


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


**By:** Coulson  
**What:** Established 4-phase v2 development plan with strict dependency gates  
**Why:** Prevents feature creep into refactoring work; ensures stable foundation before new features

#### Phase Gates
1. **Phase 0 (Refactoring) → Phase 1 (Testing):** All architectural violations fixed, testability refactors complete
2. **Phase 1 (Testing) → Phase 2 (Architecture):** ≥70% code coverage achieved on core systems
3. **Phase 2 (Architecture) → Phase 3 (Features):** GameState model, persistence layer, event system in place

**Rationale:** v1 retrospective identified zero test coverage and tight coupling as critical risks. Phase 0/1 eliminate these risks before investing in new features.

---



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

**By:** Hill
**What:** Comprehensive C# refactoring and feature proposals for v2
**Why:** Address technical debt, improve maintainability, enable advanced features

---

## 1. C#-Specific Refactoring



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



```csharp
// Before
public List<Item> Items { get; set; } = new();
private List<Item> _inventory = new List<Item>();

// After
public List<Item> Items { get; set; } = [];
private List<Item> _inventory = [];
```



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

**By:** Romanoff
**Context:** Planning v2 testing infrastructure for C# dungeon crawler (Dungnz). v1 shipped with zero automated tests; all quality verification was manual code review.

---

## 1. Testing Infrastructure Requirements


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



**EnemyFactory (Unit):**
- Returns correct enemy subclass for each type
- Enemy stats match specifications
- LootTable configured correctly per enemy type

**Room Navigation (Unit):**
- Directional movement logic
- Room.Visited flag updates

---

## 3. Edge Case Inventory (High-Risk Areas for v2)



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


**By:** Coulson
**What:** Instance-based event system with optional subscribers using nullable dependency injection
**Why:** 
- Testability requires instance-based (not static) events for mocking and isolation
- Nullable GameEvents? parameter pattern removes tight coupling — events fire unconditionally, subscribers are optional
- Strongly-typed EventArgs provide compile-time safety and rich context for subscribers
- Firing events AFTER state changes ensures subscribers see consistent game state
- Pattern established: inject shared GameEvents instance into subsystems (CombatEngine, GameLoop) at construction

---



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


**By:** Hill  
**What:** Saves stored in `Environment.GetFolderPath(SpecialFolder.ApplicationData)/Dungnz/saves/`  
**Why:**  
- Follows .NET conventions for user-specific application data
- Cross-platform (Windows: %APPDATA%, Linux: ~/.config, macOS: ~/Library/Application Support)
- Survives application upgrades and re-installs
- No admin privileges required


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



**By:** Barton  
**What:** Combat abilities use in-memory data structures (List<Ability>) rather than JSON config files  
**Why:** Simpler initial implementation for 4 fixed abilities. Hardcoding in AbilityManager constructor provides type safety and avoids deserialization complexity. If ability count grows significantly (>10) or requires frequent balance tuning by non-developers, consider migrating to JSON config similar to enemy/item stats.


- **Combat Foundation:** Turn-based, crits, dodge, status effects (6 types), abilities with cooldowns, mana resource
- **Enemy Variety:** 9 types (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss, GoblinShaman, StoneGolem, Wraith, VampireLord, Mimic)
- **Content:** 5-floor dungeons, shrines (gold economy sinks), procedural generation, seeded runs, 5 achievements
- **Systems:** Config-driven enemy scaling, loot tables per enemy, equipment slots, save/load, GameEvents



1. **Enemy Behaviors Are Static**
   - All enemies perform identical action: attack if possible, else skip
   - No tactical decision-making (regen when low, heal allies, dodge strategically)
   - Bosses have Phase 2 enrage/charge, but regular enemies feel samey
   - **Impact:** Combat vs 2nd Goblin plays identical to 1st Goblin

2. **Boss Variety is Limited**
   - Single DungeonBoss type; only variation is scaling + enrage mechanic
   - No thematic boss variety (elemental, summoner, undead, etc.)
   - No dynamic phase transitions or multi-phase strategies
   - **Impact:** Final encounter lacks personality; replay value dependent on RNG difficulty

3. **Dungeon Environments Are Passive**
   - Rooms have flavor text but no mechanical impact (description-only)
   - No environmental hazards (traps, fire, falling blocks, poisonous fog)
   - No dynamic events (room collapses, enemies roused, treasure revealed)
   - **Impact:** Exploration feels one-dimensional; combat location doesn't matter

4. **Difficulty is One-Size-Fits-All**
   - Fixed difficulty curve via enemy scaling formula (+12% per level)
   - No accessibility modes (casual players, hardcore challenge seekers)
   - Elite variant system exists but is crude (5% spawn, +50% stats, no special abilities)
   - **Impact:** Game alienates casual players; hardcore players lack engaging challenge variants

5. **Economy Lacks Depth**
   - Shrines exist but are sparse (15% room spawn) and limited (heal, stat boosts)
   - No merchant/shop system for consumable purchases or item upgrades
   - No crafting or progression sinks beyond leveling
   - **Impact:** Gold is collected but feels purposeless; players hoard without meaningful choices

6. **Item System is Generic**
   - Random item drops; no progression tiers or special item families
   - No unique/rare items with special mechanics
   - Equipment slots exist but no transmog or upgrade paths
   - **Impact:** Looting feels random; no aspirational item hunting

---

## v3 Proposed Features (8 Issues)


These are prerequisites for higher-impact features and should be tackled first.

#### 1. **Difficulty Modes & Scaling Options** → Foundation Wave
- **Title:** Difficulty Modes & Scaling Options
- **Description:** Implement 3 difficulty presets (Casual/Normal/Hard) that scale enemy stats and elite spawn rates. Add per-run modifiers (Half Gold, Double Enemies, Permadeath). Display warnings before starting runs.
- **Why:** Accessibility + replayability. Hard mode challenges veterans; Casual mode welcomes new players. Modifiers create emergent challenge variants.
- **Agent:** Barton (implement difficulty formula and modifier system)
- **Effort:** 1-2 sprints
- **Dependencies:** None (builds on existing EnemyFactory.CreateScaled)

---



#### 2. **Enemy AI Behaviors - Special Actions & Patterns** → Core Wave
- **Title:** Enemy AI Behaviors - Special Actions & Patterns  
- **Description:** Implement context-aware enemy actions. Troll regenerates HP when below 30%. Vampire Lord lifesteal on hit. Goblin Shaman heals allies (if group spawned). Wraith phases dodge every 3 turns. Stone Golem applies Stun to player on hit.
- **Why:** Combat depth without adding new mechanics. Telegraphed actions create counterplay (Poison counters Troll regen). Enemies feel intelligent, not random.
- **Agent:** Barton (add AI decision logic to each enemy class)
- **Effort:** 2-3 sprints
- **Implementation:** Add `GetAction()` method to Enemy base class; CombatEngine calls it instead of always attacking
- **Dependencies:** StatusEffectManager exists; needs slight extension (Apply/Remove method for group effects)

#### 3. **Boss Variety - Multiple Boss Types & Mechanics** → Core Wave
- **Title:** Boss Variety - Multiple Boss Types & Mechanics
- **Description:** Create 3-4 boss archetypes: Elemental Boss (spreads AOE hazard each turn), Summoner Boss (spawns minion enemies), Undead Boss (resurrects at 50% HP with less stats), Void Boss (steals player buffs, applies Weakened). Each has 2-3 phase transitions with unique attacks.
- **Why:** Final encounter defines game memory. Multiple bosses create narrative variety, force different strategies, improve replayability.
- **Agent:** Barton (design boss subclasses, phase transition logic)
- **Effort:** 2-3 sprints
- **Implementation:** Boss base class with `Phase` property; transitions trigger at HP thresholds. Each phase has unique `PerformBossAction()` behavior.
- **Dependencies:** Enemy scaling system; StatusEffectManager for debuff mechanics

#### 4. **Environmental Hazards - Procedural Dungeon Events** → Core Wave
- **Title:** Environmental Hazards - Procedural Dungeon Events
- **Description:** Procedurally spawn room hazards (15-20% of rooms): Spike Traps (3 dmg/turn to all combatants), Fire Pits (apply Burn status), Falling Blocks (dodge checks each turn), Poisonous Fog (apply Poison). Hazards activate at combat start, persist across turns.
- **Why:** Static dungeons become dynamic. Hazards damage both player and enemies (positive: rebalances combat). Room choice matters; players hide from hazards or use them strategically.
- **Agent:** Barton (implement RoomHazard class and hazard logic in CombatEngine)
- **Effort:** 1-2 sprints
- **Implementation:** Add `Hazard` property to Room; CombatEngine.RunCombat() processes hazards at turn start.
- **Dependencies:** Room model modifications

#### 5. **Elite Variants - Rarer Powerful Enemy Spawns** → Core Wave
- **Title:** Elite Variants - Rarer Powerful Enemy Spawns
- **Description:** Expand elite system: difficulty-scaled spawn rates (Casual 1%, Normal 5%, Hard 10%). Elites gain +50% stats + random special ability (bonus dodge, crit damage, status immunity). Color-coded names (e.g., "Elite Goblin" vs "Mighty Goblin"). Higher XP/loot rewards.
- **Why:** Elite encounters create memorable spikes without new enemy types. Stat scaling alone isn't enough; special abilities force tactics.
- **Agent:** Barton (extend EnemyFactory.CreateElite with ability system)
- **Effort:** 1 sprint
- **Implementation:** Enum of elite abilities (DodgeBoost, CritBoost, StatusImmune); elite constructor randomly selects one.
- **Dependencies:** EnemyFactory and scaling system already exist

---



#### 6. **Merchant Shop System - Economy & Item Upgrades** → Advanced Wave
- **Title:** Merchant Shop System - Economy & Item Upgrades
- **Description:** Spawn Merchant NPC in 1-2 rooms per floor. Shop sells consumables (Health/Mana potions 30-50g), level-locked equipment (bronze/silver/gold tiers), ability unlocks (learn new abilities early for cost). Merchants offer item transmog (combine duplicates for rarer item).
- **Why:** Gold becomes meaningful. Shop gives player agency (buy potion now or save for better sword?). Item progression ties to playstyle.
- **Agent:** Coulson or Hill (NPC system, UI) + Barton (balance prices, shop logic)
- **Effort:** 2-3 sprints
- **Implementation:** Shop state persists per run; prices scale with player level. Transmogrification system uses pseudorandom determinism (seeded).
- **Dependencies:** NPC framework; player level-based shop item availability

#### 7. **Procedural Room Types - Varied Dungeon Content** → Advanced Wave
- **Title:** Procedural Room Types - Varied Dungeon Content
- **Description:** Expand room generation: Library (grants lore, one-time +1 ability unlock discount), Armory (guaranteed 1-2 equipment drops), Miniboss Chamber (1 elite + 2 regular enemies + chest), Treasury (1500g guaranteed but 50% chance of trap damage), Shrine Room (2 shrines instead of 1). Dungeon Generator places room types procedurally.
- **Why:** Exploration gains reward/risk tension. Rooms feel thematic, not generic. "Found an Armory!" moment is memorable.
- **Agent:** Hill or Coulson (room generation) + Barton (room mechanics like Miniboss spawn)
- **Effort:** 1-2 sprints
- **Implementation:** Room type enum; DungeonGenerator places types at 20-30% rate. GameLoop.EnterRoom() reads type and triggers mechanics.
- **Dependencies:** Room model already exists; minor enum addition

#### 8. **Advanced Status Effects - Elemental & Crowd Control** → Advanced Wave
- **Title:** Advanced Status Effects - Elemental & Crowd Control
- **Description:** Expand effects: Burn (2 dmg/turn, spreads to player if player-inflicted), Freeze (reduces dodge by 50% for 2 turns), Weakness (target takes +25% damage, 3 turns), Shield (absorbs 15 damage once, auto-removes). Introduce new abilities that use these: Fireball (AoE Burn), Frost Nova (Freeze all), Cursed Blade (Weakness on hit).
- **Why:** Status effects become strategic. Poison used to counter Regen; Weakness counters armor stacking. Elemental theme adds flavor without new mechanics.
- **Agent:** Barton (extend StatusEffectManager, add new abilities to AbilityManager)
- **Effort:** 1-2 sprints
- **Implementation:** Extend StatusEffect enum; add effect descriptions and interaction rules. New abilities unlock at L10+.
- **Dependencies:** StatusEffectManager and AbilityManager exist and are extensible

---

## Prioritization & Wave Timing

**Foundation (Sprint 1):**
- Difficulty Modes (enables balanced playtesting for other features)

**Core (Sprint 2-4):**
1. Enemy AI Behaviors (highest impact: combat feels alive)
2. Boss Variety (final encounter variety)
3. Environmental Hazards (dungeon depth)
4. Elite Variants (elite encounters feel special)

**Advanced (Sprint 5+):**
- Merchant Shops (economy depth, but optional for core loop)
- Room Types (thematic variety, but doesn't block other features)
- Advanced Status Effects (builds on existing; polishes system)

---

## Gap Analysis: What's Missing From v2

| Gap | Addressed By | Priority |
|-----|--------------|----------|
| Static enemy behavior | Enemy AI Behaviors | High |
| Single boss type | Boss Variety | High |
| No environmental challenge | Environmental Hazards | High |
| One difficulty level | Difficulty Modes | High |
| Gold feels purposeless | Merchant Shop System | Medium |
| Generic rooms | Procedural Room Types | Medium |
| Limited status effects | Advanced Status Effects | Low |
| No elite abilities | Elite Variants | Medium |

---

## Key Design Decisions for v3

1. **AI Over New Mechanics:** Add depth via enemy behavior, not new status effects. Existing effects (Poison, Stun, Regen) support rich interactions when AI uses them tactically.

2. **Hazards Don't Target Player:** Environmental hazards damage both player AND enemies. This rebalances combat without nerfing players; creates resource depletion for both.

3. **Difficulty as Foundation:** Modes are a prerequisite. All future balancing (elite spawn rates, boss health) keys off difficulty setting.

4. **Shops Don't Break Economy:** Prices scale with player power. Consumables are convenience; core progression remains combat-based.

5. **Room Types Are Flavor + Mechanics:** Library gives story; Armory gives gear; Miniboss gives challenge. No room type is a trap (except Treasury, by design).

---

## Testing Strategy

- **Unit tests:** Hazard damage, elite ability rolls, boss phase transitions
- **Integration tests:** Difficulty scaling affects enemy spawns, shop item availability
- **Playtests:** Does Hard mode feel challenging? Does Casual mode feel accessible? Do elite encounters stand out?

---

## Next Steps (Post v2)

1. **Code Review v2 stabilization** → confirm no regressions
2. **Create Wave 1 ticket:** Difficulty Modes
3. **Schedule design review:** Core wave features (AI, Boss Variety, Hazards, Elites)
4. **Estimate effort:** Coordinate with Hill (room system changes) and Coulson (NPC framework)
5. **Spike:** AI behavior architecture with Hill and Coulson (can use existing Enemy properties or need new interface?)


**Finding:** Player.cs is 273 LOC mixing 7 concerns: stats, inventory, equipment, mana, abilities, health, gold/xp.
**Impact:** Adding character classes, shop system, or crafting requires refactoring Player without breaking saves.
**Decision:** Decompose Player.cs into PlayerStats, PlayerInventory, PlayerCombat modules in Wave 1.
**Reasoning:** High ROI; unblocks 3 major features (classes, shops, crafting); reduces regression risk.


**Finding:** EquipItem/UnequipItem logic in Player prevents equipment config system and shops.
**Impact:** Can't build merchant NPCs or equipment drop customization without refactoring.
**Decision:** Create EquipmentManager to own equipment state and stat application.
**Rationale:** Config-driven equipment (like items/enemies) reduces hardcoding; enables external stat systems.


**Finding:** StatusEffectManager hardcodes 6 effects; no config-driven or stackable effects.
**Impact:** Elemental system, effect combos, and custom effects blocked.
**Decision:** Refactor StatusEffectManager to use IStatusEffect interface + config system.
**Rationale:** Enables v3 feature (elemental damage), v4 feature (effect combos); reduces branching logic.


**Finding:** 91.86% unit test coverage, but zero integration tests for systems interacting (combat → loot → equipment).
**Impact:** Refactoring Player.cs risks regressions in CombatEngine, InventoryManager, SaveSystem interaction.
**Decision:** Create integration test suite (Phase 1) testing multi-system flows.
**Rationale:** Supports safe refactoring; catches edge cases (stun + flee, status + save/load, etc.).


**Finding:** Inventory is List<Item> with no weight/slot limits; TakeItem logic scattered.
**Impact:** Shop systems can't enforce inventory constraints; duping bugs possible.
**Decision:** Create InventoryManager with weight/slot validation and centralized add/remove logic.
**Rationale:** Prepares for trading/shops; reduces bugs; improves save/load robustness.


**Finding:** AbilityManager tied to combat; no passive abilities, trait system, or chaining.
**Impact:** Skill trees and builds in v4 require major redesign.
**Decision:** Extend AbilityManager to support passive abilities + cooldown groups (v3 foundational work).
**Rationale:** Unblocks v4 skill trees with minimal v3 work; improves ability reusability.


**Finding:** No player class concept; all players same stats/abilities/playstyles.
**Impact:** v3 feature "select class at start" has no foundation.
**Decision:** Design ClassDefinition config system + ClassManager (depends on Player decomposition).
**Rationale:** Config-driven approach (like enemies); unblocks class selection, balancing, and testing.


**Finding:** SaveSystem couples to Player structure; no version tracking or migration path.
**Impact:** Player.cs decomposition breaks all existing saves.
**Decision:** Add SaveFormatVersion + migration logic to SaveSystem before refactoring Player.
**Rationale:** Protects user data; establishes pattern for future schema changes.

---

## v3 Wave Structure (Recommended)


**Goal:** Stabilize architecture for v3 features.
**Work:**
1. **Player Decomposition** — Split into PlayerStats, PlayerInventory, PlayerCombat
2. **EquipmentManager Creation** — Extract from Player, design stat application interface
3. **InventoryManager Validation** — Add weight/slot checking, centralize item logic
4. **Integration Test Suite** — Combat→loot, equipment→combat, save→load with all features
5. **SaveSystem Migration** — Add version tracking, support old/new formats

**Critical Path:** Player decomposition → SaveSystem migration → integration tests
**Agents:** Hill (Player decomposition, SaveSystem), Barton (EquipmentManager, InventoryManager), Romanoff (integration tests)
**Duration:** ~40 hours, 2-week sprint


**Goal:** Add character classes and ability expansion.
**Work:**
1. **ClassDefinition System** — Config-driven class stats, starting equipment, class abilities
2. **ClassManager** — Class selection, stat templates, class-specific ability grants
3. **Ability System Expansion** — Passive abilities, ability groups, cooldown pools
4. **Achievement System Expansion** — Class-based achievements, multi-tier achievements

**Dependency:** Requires Wave 1 Player decomposition  
**Agents:** Hill (ClassManager, config), Barton (ability expansion), Romanoff (achievement tests)
**Duration:** ~35 hours, 2-week sprint


**Goal:** Add economic systems.
**Work:**
1. **Shop System Design** — NPC merchants, shop inventories, dynamic pricing
2. **Crafting System** — Recipe definitions, ingredient validation, output generation
3. **Economy Balancing** — Item tiers, shop profit/loss, crafting margins

**Dependency:** Requires Wave 1 equipment/inventory refactoring  
**Agents:** Hill (shop config, economy), Barton (crafting logic, NPC interaction), Romanoff (economy testing)
**Duration:** ~30 hours, 2-week sprint


**Goal:** Expand content and balance.
**Work:**
1. **New Enemy Types** — 5-8 new enemies (elemental damage types, ranged attackers)
2. **Shrine Upgrades** — New shrine types (mana regen, temporary buffs, class bonuses)
3. **Loot Balancing** — Adjust drop rates, add class-specific loot tables
4. **Dungeon Difficulty Tuning** — Adjust enemy scaling, boss mechanics

**Dependency:** Independent; can run parallel to Wave 3 in final sprint  
**Agents:** Barton (new enemies, shrines), Romanoff (balancing tests)
**Duration:** ~25 hours, 1-2 week sprint

---

## v3 Scope Decision


- ✅ Player.cs decomposition (PlayerStats, PlayerInventory, PlayerCombat)
- ✅ EquipmentManager + equipment config system
- ✅ InventoryManager with validation
- ✅ Integration test suite (multi-system flows)
- ✅ Character classes (config-driven)
- ✅ Shop system with NPC merchants
- ✅ Basic crafting system
- ✅ SaveSystem migration + version tracking


- ❌ Skill trees (requires stable Player + Ability architecture)
- ❌ Permadeath/hardcore modes (SaveSystem too fragile)
- ❌ Multiplayer/lobbies (no session system)
- ❌ PvP arena (no turn order/spectating system)
- ❌ Elemental damage system (requires Status effect composition)
- ❌ Trading between players (network/lobby system needed)

---

## Architectural Patterns Established for v3+


**Rule:** All extensible entities (Classes, Abilities, StatusEffects, Shops) defined in config, not hardcoded.
**Precedent:** ItemConfig, EnemyConfig, LootTable — apply same pattern.
**Benefit:** Balancing without code changes; easier testing; mods-friendly.


**Rule:** Use IStatusEffect, IAbility interfaces; compose abilities from components.
**Rationale:** Avoids inheritance hierarchy explosion (combat abilities vs. passive abilities vs. class-specific).
**Implementation:** Each interface has Execute/Update methods; StatusEffectManager/AbilityManager aggregate.


**Rule:** Each major subsystem owns a manager class (EquipmentManager, InventoryManager, ClassManager).
**Responsibility:** Manager validates state changes, applies side effects, fires events.
**Dependency Injection:** Managers receive config, Random, GameEvents via constructor.
**Testing:** All managers use injectable dependencies (no static Random, no hardcoded config).


**Rule:** Systems don't call each other directly; they fire events and subscribe.
**Precedent:** GameEvents system established in v2; extend with EquipmentChanged, ItemCrafted, ClassSelected.
**Benefit:** Decouples features; enables future systems (achievements, UI updates) without touching core logic.


**Rule:** All public methods validate preconditions; throw ArgumentException for invalid state.
**Precedent:** Player.TakeDamage, Player.AddGold; apply across managers.
**Testing:** Every edge case (negative amounts, null items, invalid slot names) has a test.

---

## Integration Strategy: No Breaking Changes


- SaveSystem must support v1, v2, and v3 formats simultaneously.
- Migration happens on load; no player data loss.
- Two-release deprecation window (if data migration needed).


- New EquipmentManager runs in parallel with Player equipment slots initially.
- New InventoryManager validates independently; old behavior unchanged.
- Classes are opt-in; existing save files load as "unclassed" (balanced stats).


- One subsystem per PR (Player.cs → PlayerStats, PlayerInventory, PlayerCombat in separate PRs).
- Each PR has integration tests for that subsystem only.
- Full integration tested in final PR before merge.


- Each Wave starts with architecture ceremony (contracts, interfaces, dependencies).
- No implementation without signed-off design.
- Prevents rework and integration bugs.

---

## Success Criteria for v3


- ✅ Player.cs decomposed into 3 focused modules (<150 LOC each)
- ✅ EquipmentManager created + equipment config system working
- ✅ InventoryManager enforces weight/slot limits
- ✅ Integration test suite covers 10+ multi-system flows
- ✅ SaveSystem handles v2→v3 migration with zero data loss
- ✅ All tests pass; coverage >90%


- ✅ ClassDefinition config system defined (5 classes with unique stats/abilities)
- ✅ ClassManager created; class selection working
- ✅ AbilityManager extended for passive abilities
- ✅ Class-based achievements implemented
- ✅ All tests pass; coverage >90%


- ✅ Shop system deployed (3+ merchant NPCs with rotating inventory)
- ✅ Crafting system working (5+ recipes with ingredient validation)
- ✅ Economy balanced (no exploits, sensible profit margins)
- ✅ All tests pass; coverage >90%


- ✅ 8 new enemy types implemented
- ✅ Difficulty curves balanced (easier early floors, harder late)
- ✅ New shrine types integrated
- ✅ No regressions from v2

---

## Risk Assessment


- **Player.cs Refactoring:** Breaks all old saves. *Mitigated by SaveSystem migration + version tracking.*
- **Multi-System Integration:** Stat application, equipment/ability interaction bugs. *Mitigated by integration test suite.*


- **Scope Creep:** Features block each other (shops need classes, classes need Player split). *Managed by sequential Wave structure.*
- **Config System Complexity:** Too many config files, hard to balance. *Managed by pattern review + centralized config validation.*


- **New Enemy Types:** Standard Barton work; low risk given existing EnemyFactory.
- **Achievement Expansion:** Independent; existing achievement system proven.

---

## Team Assignments (Preliminary)


- Player.cs decomposition (PlayerStats module)
- ClassManager + ClassDefinition config system
- SaveSystem migration + version tracking
- Shop system architecture + NPC config

**Estimate:** 35 hours across v3


- EquipmentManager creation
- InventoryManager creation + validation
- Ability system expansion
- Crafting system implementation
- New enemy types + shrines
- All non-Player content integration

**Estimate:** 40 hours across v3


- Integration test suite (multi-system flows)
- Edge case testing (status + equipment, crafting + save/load, etc.)
- Economy balancing (shop prices, crafting margins)
- Difficulty curve analysis
- Content testing (new enemies, shrines, recipes)

**Estimate:** 30 hours across v3

---

## Next Actions

1. **Immediately:** Present v3 roadmap to team; request approval of Wave structure and agent assignments.
2. **This Week:** Schedule design review ceremony for Wave 1 (Player decomposition contracts).
3. **Next Week:** Kick off Wave 1 Phase 1 (integration test suite + SaveSystem migration).
4. **Ongoing:** Monthly retrospectives to validate Wave structure; adjust based on blockers.

---

## Appendix: Issues to Create on GitHub


1. **Issue: Player.cs Decomposition** (Foundation)
   - Split into PlayerStats, PlayerInventory, PlayerCombat
   - Preserve all existing functionality; add integration tests
   - Save file migration required
   
2. **Issue: EquipmentManager Creation** (Foundation)
   - Extract equipment state from Player
   - Create equipment config system
   - Support stat application via manager instead of Player methods
   
3. **Issue: InventoryManager Validation** (Foundation)
   - Enforce weight/slot limits
   - Centralize item add/remove/use logic
   - Support shop and crafting systems
   
4. **Issue: Integration Test Suite** (Foundation)
   - Multi-system flows (combat→loot→equipment, equipment→combat stat changes)
   - Edge cases (stun+flee, status+save/load, ability+cooldown)
   - Regression tests for boss mechanics, shrine interactions
   
5. **Issue: SaveSystem Migration** (Foundation)
   - Add SaveFormatVersion tracking
   - Support v2→v3 migration (Player decomposition)
   - Test backward compatibility


6. **Issue: Character Class System** (Core)
   - ClassDefinition config (5 classes: Warrior, Mage, Rogue, Paladin, Ranger)
   - ClassManager with stat templates and ability grants
   - Class selection at game start
   
7. **Issue: Ability System Expansion** (Core)
   - Passive abilities support
   - Ability cooldown groups
   - Class-specific ability trees


8. **Issue: Shop System** (Core)
   - NPC merchant implementation
   - Shop inventory config
   - Dynamic pricing and stock rotation

---

**Signed by:** Coulson, Lead
**Status:** PROPOSED (awaiting team approval)



1. **No Character Agency / Customization**
   - All players are identical "generic warrior"
   - No build diversity or strategic choices at character creation
   - Limited replayability—every run feels the same mechanically

2. **Weak Progression Hooks**
   - Leveling is purely binary: gain stats, nothing else
   - No unlocks, no meaningful milestones beyond "level 5"
   - Abilities unlock via AbilityManager, but no strategic choice involved

3. **Content Repetition**
   - 5 floors × 4 rooms = 20 rooms always identical type ("combat room")
   - No environmental variety: no shrines, treasures, arenas
   - Dungeon has no thematic flavor or narrative context

4. **Clarity / UX Issues**
   - No map display—players can't see dungeon layout, navigate by memory
   - Combat log is ephemeral—no record of turn history
   - Inventory is list-heavy; no quick equipment view
   - No narrative framing—purely mechanical "escape the dungeon"



**Player Agency (Replayability)**
- **Character Classes:** Warrior/Rogue/Mage with distinct stat curves, starting abilities, trait pools
  - Expected impact: 3x playstyles (vs. 1 generic), changes build strategy
  - Why: Fundamental franchise mechanic in roguelikes; drives run variety

- **Trait System:** Passive bonuses unlocked at milestones, class-specific options
  - Expected impact: Every 5 levels = meaningful choice (2 trait options)
  - Why: Micro-progression between levels, increases engagement

- **Skill Trees:** Config-driven trees per class, grant stat bonuses or new abilities
  - Expected impact: Path optimization (different for each playthrough), build guides emerge
  - Why: Deep engagement mechanic, community theory-crafting

**Progression Hooks**
- **Trait Selection at Level-Up:** Prompt at Lvl 5/10/15/20 with 2 random traits
  - Expected impact: Player anticipates milestones, makes strategic decisions
  - Why: Turns passive leveling into active choice

**Content Variety**
- **Variant Room Types:** Shrines (blessings/curses), Treasuries (mega-loot), Elite Arenas
  - Expected impact: Breaks "all combat rooms" monotony, adds spatial strategy
  - Why: Small feature, high leverage (5 new room types = 25% room diversity)

- **Dungeon Variants:** "Standard" / "Forsaken" / "Bestiary" / "Cursed" with lore flavor
  - Expected impact: 4x narrative flavor, enemy distribution changes feel fresh
  - Why: Minimal new code (config + generator tweak), huge engagement increase

**UX Improvements**
- **Mini-Map:** ASCII grid showing visited rooms, current position, exit
  - Expected impact: Players feel less random-lost, reduces frustration
  - Why: Addresses common roguelike pain point; ASCII fits aesthetic

- **Combat Clarity:** Turn log (last 5 turns), crit/dodge notifications, action separation
  - Expected impact: Players understand why they took damage, learn combat patterns
  - Why: Transparency increases trust in RNG, enables skill improvement

---

## Recommended v3 Roadmap


*Prerequisite for traits/skill trees; establishes character identity*

1. **Character Class System** (Issue)
   - Models: Add `CharacterClass enum {Warrior, Rogue, Mage}`
   - Player: Add `Class` property, refactor stat defaults by class
   - GameLoop: Add class selection menu at startup
   - Agents: Hill (models), Barton (combat), Romanoff (tests)

2. **Class-Specific Traits** (Issue)
   - Models: Add `Trait` class (Name, Description, StatModifier, Class)
   - Player: Add `ActiveTraits List<Trait>`, trait application logic
   - Config: Add `traits-<classname>.json` per class (4-5 traits each)
   - Agents: Hill (encapsulation), Barton (balance), Romanoff (tests)

3. **Skill Tree Foundation** (Issue)
   - Models: Add `SkillNode` (Name, UnlockLevel, StatBonus, Class)
   - Models: Add `SkillTree List<SkillNode>` per class, config-driven
   - GameLoop: Show tree at level-up (visual representation)
   - Agents: Hill (tree UI), Barton (stat application), Romanoff (unlock tests)

**Expected Timeline:** 2-3 weeks (parallelizable)  
**Blockers:** None (builds on existing Player/config patterns)

---


*Multiplies engagement; traits become meaningful; room variety breaks monotony*

4. **Trait Selection at Level-Up** (Issue)
   - GameLoop: Pause at Lvl 5/10/15/20, show trait menu
   - Player: Apply selected trait stat modifiers, persist in save
   - DisplayService: Show "Available Traits" with descriptions
   - Agents: Hill (UI/flow), Romanoff (save tests)

5. **Variant Room Types** (Issue)
   - Models: Add `RoomType enum {Combat, Shrine, Treasury, EliteArena}`
   - DungeonGenerator: Place shrines (2), treasuries (1), elite arenas (2) per dungeon
   - GameLoop: Handle shrine blessings (menu prompt), treasury auto-loot, arena enemies
   - Agents: Hill (room types), Barton (elite logic), Romanoff (tests)

6. **Combat Clarity System** (Issue)
   - Models: Add `CombatTurn struct` (attacker, action, damage, status)
   - CombatEngine: Log turns to `List<CombatTurn>`
   - DisplayService: Show last 5 turns, crit/dodge notifications
   - Agents: Hill (DisplayService), Barton (log data), Romanoff (integration)

**Expected Timeline:** 2-3 weeks (parallelizable)  
**Blockers:** Requires Wave 1 (traits foundation)

---


*Adds narrative flavor; extends replayability; polishes UX*

7. **Dungeon Variants & Lore** (Issue)
   - Models: Add `DungeonVariant enum {Standard, Forsaken, Bestiary, Cursed}`
   - DungeonGenerator: Adjust enemy distribution per variant
   - GameLoop: Show variant-specific intro/outro flavor text
   - Config: Add `dungeon-variants.json` with enemy pool overrides
   - Agents: Hill (variant enum), Barton (config), Romanoff (integration)

8. **Mini-Map Display** (Issue)
   - DisplayService: Add `ShowMap(Room grid, Room current, Room exit)` method
   - Render: 5×4 ASCII grid with symbols (., ▓, *, !, B)
   - GameLoop: Show map after each move (or MAP command)
   - Agents: Hill (ASCII rendering), Romanoff (state tests)

**Expected Timeline:** 1-2 weeks (parallelizable)  
**Blockers:** None (orthogonal features)

---


*Beyond v3 priority; enables long-term engagement*

- **Prestige System:** Beat boss N times → unlock cosmetics/traits for future runs
- **Difficulty Selector:** Easy/Normal/Hard with enemy stat scaling & reward adjustments
- **Boss Phase 2:** Boss enrages at 50% HP (new attacks, phase-specific mechanics)
- **Leaderboard:** Track fastest wins, highest gold, best achievement hunts (JSON file)
- **Inventory UI:** Sell items, drop items, sort by type (extends shop system)

---

## Design Patterns & C# Considerations


```csharp
public class Trait
{
    public string Name { get; init; }
    public int AttackBonus { get; init; }
    public int DefenseBonus { get; init; }
    public CharacterClass Class { get; init; }
}

public class Player
{
    private List<Trait> _activeTraits = new();
    
    public void AddTrait(Trait trait)
    {
        if (_activeTraits.Count >= MaxTraits) 
            throw new InvalidOperationException("Max traits reached");
        _activeTraits.Add(trait);
        ApplyTraitStatModifiers();
    }
}
```


```json
{
  "WarriorTraits": [
    {
      "Name": "Unbreakable",
      "Description": "+10% Defense",
      "DefenseBonus": 1,
      "Class": "Warrior"
    }
  ]
}
```


```csharp
public enum RoomType { Combat, Shrine, Treasury, EliteArena }

public class Room
{
    public RoomType Type { get; init; } = RoomType.Combat;
    // ... existing Enemy, Items, visited state
}

// In GameLoop
case RoomType.Shrine:
    HandleShrine(currentRoom);
    break;
case RoomType.Treasury:
    HandleTreasury(currentRoom);
    break;
```


```csharp
public void ShowMap(Room[,] grid, Room current, Room exit)
{
    var sb = new StringBuilder("┌─────────┐\n");
    for (int y = 0; y < grid.GetLength(0); y++)
    {
        sb.Append("│");
        for (int x = 0; x < grid.GetLength(1); x++)
        {
            var room = grid[y, x];
            char symbol = room == current ? '*' 
                        : room == exit ? '!' 
                        : room.Visited ? '.' 
                        : '▓';
            sb.Append(symbol);
        }
        sb.Append("│\n");
    }
    sb.Append("└─────────┘");
    _display.ShowMessage(sb.ToString());
}
```

---

## Open Questions & Decisions Needed

1. **Trait Cap:** Should player have max 3 active traits, or scale with level?
   - Recommended: Cap at 5 (prevents stat inflation, forces strategic choice)

2. **Skill Tree Visibility:** Show full tree at game start, or unlock nodes progressively?
   - Recommended: Show full tree grayed-out; highlight achievable nodes by level

3. **Shrine Blessing Duration:** Temporary buff (end of floor) or permanent?
   - Recommended: 5-turn duration (strategic for combat, not trivializing)

4. **Map Display Frequency:** Always show after move, or toggle with MAP command?
   - Recommended: Show after move by default, but allow toggle (accessibility)

5. **Difficulty Selector:** Part of v3, or defer to v4?
   - Recommended: Defer to v4 (simpler v3 ships faster; difficulty unlocks in v4)

---

## Coordination Notes

**With Coulson (Lead):** Confirm wave priorities and merge decision into canonical decisions.md once approved.

**With Barton (Systems):** Enemy distribution config variants, elite arena mechanics, stat balance across new traits.

**With Romanoff (Tester):** Unit tests for trait application, config loading, map rendering, trait selection persistence.

**With Scribe:** Merge this inbox file into decisions.md after team review.

---

## Expected Outcomes

- **Replayability:** +300% (3 classes × trait choices × skill paths)
- **Content Variety:** +400% (5 room types × 4 dungeon variants)
- **Engagement:** +200% (milestone progression, mini-map orientation)
- **Codebase Health:** Minimal (builds on encapsulation patterns, config-driven design, no new dependencies)

---

## Metrics for Success

- [ ] v3a (Wave 1) ships by Week 3 with character classes + traits + skill trees
- [ ] v3b (Wave 2) ships by Week 6 with room variants + trait selection + combat clarity
- [ ] v3c (Wave 3) ships by Week 8 with dungeon variants + mini-map
- [ ] Player feedback: "Feels more like a roguelike now" (trait/class variety)
- [ ] Community engagement: Trait build guides emerge on forums/Discord




**Risk Level:** 🔴 **CRITICAL**

**What's untested:**
- Poison effect (3 damage/turn) application and removal
- Bleed effect (5 damage/turn) application and removal
- Burn effect (2 damage/turn) application and removal
- Stun effect (skip turn) application and removal
- Regen effect (4 HP/turn) application and removal
- Slow effect (reduce damage by 30%) application and removal
- Effect immunity (Stone Golem `IsImmuneToEffects`)
- Duration tracking and decrement
- Stacking behavior (refresh existing effects)
- Turn-start processing (damage application in order)
- Effect removal on duration <= 0

**Impact:**
- Combat balance untested; status effect damage may be incorrect
- Enemy immunity edge case breaks balance (Stone Golem not immune if test never runs)
- Duration bugs could cause infinite debuffs or premature removal
- Stun effect skipping unvalidated (critical for boss difficulty)

**Required Tests:**
```csharp
[Fact] void ApplyPoison_DealsDamageEachTurn()
[Fact] void BleedStack_RefreshesExistingDuration()
[Fact] void StoneGolem_IsImmuneToPoison()
[Fact] void Stun_SkipsEnemyAttack()
[Fact] void StatusEffect_RemovedAfterDurationExpires()
[Fact] void MultipleEffects_ProcessInOrder()
```

---



**Risk Level:** 🔴 **CRITICAL**

**What's untested:**
- Glass Cannon condition: `player.HP < 10 && run won`
- Untouchable condition: `stats.DamageTaken == 0`
- Hoarder condition: `stats.GoldCollected >= 500`
- Elite Hunter condition: `stats.EnemiesDefeated >= 10`
- Speed Runner condition: `stats.TurnsTaken < 100`
- Achievement unlock state persistence (JSON save to AppData)
- Achievement list initialization
- Multiple runs tracking (unlock on 2nd run vs 1st)

**Impact:**
- Achievements unlock silently without test verification
- Save file corruption possible (JSON write permissions, invalid paths)
- Player never sees "Achievement Unlocked!" message; no way to verify
- Balance: Glass Cannon achievable? (< 10 HP is achievable at level 1-2 only)

**Required Tests:**
```csharp
[Fact] void GlassCannon_UnlocksWhenWinWithLowHP()
[Fact] void Untouchable_UnlocksWhenNoDamageTaken()
[Fact] void Achievement_PersistsToJSON()
[Fact] void Achievement_LoadsFromJSON()
[Fact] void CorruptedAchievementFile_LogsErrorGracefully()
```

---



**Risk Level:** 🔴 **CRITICAL**

**What's untested:**
- GameState serialization (Player + Room graph)
- Room graph reconstruction (exits dict roundtrip)
- File I/O safety (directory creation, permission errors, disk full)
- Deserialization validation (HP <= MaxHP, no negative stats, no null items)
- Corruption recovery (missing save file, invalid JSON, truncated file)
- Save name validation (empty string, special chars)
- Enemy state serialization (alive/dead, HP, abilities)

**Impact:**
- Save/load loses data or corrupts game state silently
- Soft-lock: Player loads save with invalid HP (e.g., HP=200 on 100 max)
- No directory exists → crash on first save attempt
- Corrupted JSON → exception uncaught, unrecoverable
- Room graph disconnection on load (exits not reconstructed)

**Required Tests:**
```csharp
[Fact] async Task SaveGame_CreatesDirectoryIfMissing()
[Fact] async Task LoadGame_ValidatesHPBounds()
[Fact] async Task CorruptedJSON_ReturnsGracefulError()
[Fact] async Task RoomExits_ReconstructedCorrectly()
[Fact] void SaveName_ValidatesEmptyString()
```

---



**Risk Level:** 🟠 **HIGH**

**What's untested:**
- Equip weapon → +Attack bonus applied
- Equip armor → +Defense bonus applied
- Equip accessory → special effect active
- Unequip → stat bonus removed
- Equip different item → previous unequipped
- Stat overflow: Equip 10 weapons (attack = 100+?)
- Null equipment handling

**Impact:**
- Player can equip multiple weapons → stat exploitation
- Stat bonuses not applied or applied twice
- Combat balance broken (player 50 Attack vs enemy 18)

**Required Tests:**
```csharp
[Fact] void EquipWeapon_IncreaseAttack()
[Fact] void EquipArmor_IncreaseDefense()
[Fact] void EquipNewWeapon_UnequipsPrevious()
[Fact] void Unequip_RemovesStatBonus()
```

---



**Risk Level:** 🟠 **HIGH**

**What's untested:**
- Elite variant spawning (5% chance correctly implemented)
- Elite variant stat scaling (110% HP, 120% Attack, 110% Defense)
- EnemyConfig values match hardcoded stats in enemy classes
- Config-driven scaling works (config change → enemy stats update)

**Impact:**
- Elite enemies spawn with incorrect stats
- Balance: easy to break via config edit
- No validation that config matches enemy class defaults

**Required Tests:**
```csharp
[Theory]
[InlineData(0, 0.05)] // 1 in 20 spawns is elite
void EnemyFactory_Spawns5PercentElite(int seed, double expectedRate)

[Fact] void EliteGoblin_Stats110Percent()

[Fact] void EnemyConfig_LoadedCorrectly()
```

---

## Quality Risks for v3



**Risk:** No tests for inheritance hierarchy; polymorphic type casting not tested in combat/loot systems.

**Mitigation:**
- Create abstract `CharacterClass` interface
- Test base class → subclass method resolution (virtual methods work)
- Test combat engine accepts ICharacterClass polymorphically
- Test loot table respects class-specific drops

---



**Risk:** Persistent state (inventory qty, recipes) interacts with SaveSystem (untested). Concurrency: multiple shops loaded simultaneously.

**Mitigation:**
- SaveSystem tests pass first (Tier 1)
- Shop state serialization unit tests
- Crafting recipe validation (circular dependencies, invalid ingredient qty)
- Inventory qty overflow tests

---



**Risk:** GameEvents and EnemyConfig are mutable global state. Concurrent reads during hotload could race.

**Mitigation:**
- GameEvents event handler registration tests (thread-safe subscription)
- Config read lock tests (blocking during reload)
- Concurrent combat + config reload tests

---

## Recommended v3 Testing Tiers



**Must complete before v3 core features (shops, crafting, classes) ship.**

| Issue | System | Est. Hours | Tests Required |
|-------|--------|-----------|-----------------|
| V3-T1-StatusEffects | StatusEffectManager | 6 | Poison/Bleed/Burn/Stun/Regen/Slow, immunity, duration, stacking |
| V3-T1-Equipment | Player equipment | 4 | Equip/unequip, stat bonuses, conflict detection |
| V3-T1-SaveSystem | SaveSystem | 8 | Serialization, deserialization, validation, file I/O, corruption recovery |
| V3-T1-Achievements | AchievementSystem | 4 | All 5 conditions, persistence, unlock logic |
| **Tier 1 Total** | | **22 hours** | |



**Complete before v3 advanced features (classes, multithreading) ship.**

| Issue | System | Est. Hours | Tests Required |
|-------|--------|-----------|-----------------|
| V3-T2-ClassHierarchy | Abstract class pattern | 4 | Polymorphism, method resolution, type casting |
| V3-T2-ShopSystem | Shop state + SaveSystem integration | 6 | Buy/sell logic, inventory qty, gold validation, persistence |
| V3-T2-CraftingSystem | Recipe validation + crafting logic | 6 | Ingredient checks, output generation, circular dependencies |
| V3-T2-EnemyVariants | Elite enemies + config | 3 | 5% spawn rate, stat scaling, config matching |
| **Tier 2 Total** | | **19 hours** | |



**Run in parallel with Tier 2; prioritize after Tier 1 blocks.**

| Issue | Focus | Est. Hours | Tests Required |
|-------|-------|-----------|-----------------|
| V3-T3-Boundaries | Input validation + bounds checks | 4 | Negative HP, invalid stat values, circular exits |
| V3-T3-Concurrency | GameEvents + config thread safety | 4 | Concurrent event registration, config read/write locks |
| V3-T3-Deserialization | SaveSystem error recovery | 3 | Corrupt JSON, missing files, invalid paths |
| V3-T3-Regressions | v2 edge cases (dead enemy, flee, status effects) | 2 | Regression suite |
| **Tier 3 Total** | | **13 hours** | |

---

## Quality Gates for v3



```yaml
# .github/workflows/ci.yml
- name: Run Tests
  run: dotnet test

- name: Coverage Check
  run: |
    dotnet test /p:CollectCoverage=true /p:CoverageThreshold=80
    # Must maintain 80%+ on: StatusEffects, Equipment, SaveSystem, Achievements
```



| System | v2 Coverage | v3 Target | Threshold |
|--------|-----------|----------|-----------|
| StatusEffectManager | 0% | 95% | ❌ Fail merge if < 90% |
| AchievementSystem | 0% | 95% | ❌ Fail merge if < 90% |
| SaveSystem | 0% | 95% | ❌ Fail merge if < 90% |
| Equipment system | 0% | 90% | ❌ Fail merge if < 85% |
| Overall codebase | ~70% | 80% | ❌ Fail merge if < 75% |



**Before merge, Romanoff (Tester) must approve:**

1. **SaveSystem reviews:**
   - [ ] File I/O handles missing directory (`Directory.CreateDirectory` called)
   - [ ] Deserialization validates all stat bounds (HP <= MaxHP, no negatives)
   - [ ] Corruption recovery logs error and returns null (doesn't crash)
   - [ ] Permission errors caught and reported gracefully

2. **Equipment system reviews:**
   - [ ] No way to equip multiple weapons (unequip previous first)
   - [ ] Stat bonuses correctly applied/removed
   - [ ] Balance: max possible Attack/Defense within design limits

3. **StatusEffectManager reviews:**
   - [ ] All 6 effects cause correct damage/heal each turn
   - [ ] Duration decrement happens before effect removal
   - [ ] Immunity check prevents application (Stone Golem test passes)
   - [ ] Stun effect actually skips turn (mock combat engine confirms)

4. **AchievementSystem reviews:**
   - [ ] All 5 conditions trigger under correct circumstances
   - [ ] Persistence: save file created with correct permissions
   - [ ] Unlock state survives serialize → deserialize cycle

---

## Regression Test Checklist (v2 Regressions)

**Add to CI regression suite (must pass every merge):**

- [ ] Dead enemy cleanup: `room.Enemy == null` after combat Won (WI-10 fix)
- [ ] Flee-and-return: Enemy HP persists when fleeing and re-entering
- [ ] Status effect during combat: Apply poison → combat → effect triggers → remove
- [ ] Level-up mid-combat: Gain level during combat, stat bonuses apply, combat continues
- [ ] Stun effect skips enemy turn (not player turn)
- [ ] Boss gate: Cannot enter exit room with boss alive
- [ ] Empty inventory: `USE` with no items → error, no crash

---

## Test Infrastructure Requirements for v3



**No changes needed.** v2 established stack is solid.



1. **Async I/O Support for SaveSystem**
   ```csharp
   [Fact]
   public async Task SaveGame_CreatesFileAsync()
   {
       var game = new GameState(...);
       await SaveSystem.SaveGameAsync(game, "test-save");
       File.Exists(...).Should().BeTrue();
   }
   ```

2. **JSON Fixture Loading**
   ```csharp
   // fixtures/corrupt-save.json (invalid HP)
   private static GameState LoadFixture(string name)
   {
       var json = File.ReadAllText($"fixtures/{name}.json");
       return JsonSerializer.Deserialize<GameState>(json);
   }
   ```

3. **Time-Based Effect Testing**
   ```csharp
   [Fact]
   public void StatusEffect_RemovedAfterDurationTurns()
   {
       // Simulate N turn-start calls; effect should disappear on turn N+1
       for (int i = 0; i < duration; i++)
           manager.ProcessTurnStart(target);
       
       manager.ActiveEffects(target).Should().BeEmpty();
   }
   ```

4. **Immutable Test Data Builders**
   ```csharp
   var player = new PlayerBuilder()
       .WithHP(50)
       .WithAttack(20)
       .WithEquippedWeapon(new Item { Attack = 5 })
       .Build();
   ```

---

## Timeline Estimate

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| **Tier 1** | 2-3 weeks | StatusEffects, Equipment, SaveSystem, Achievements tests |
| **Tier 2** | 3-4 weeks | Class hierarchy, Shop, Crafting, Enemy variant tests |
| **Tier 3** | Ongoing | Edge case + regression tests in parallel |
| **CI Integration** | 1 week | GitHub Actions workflow + coverage gates |
| **Total v3 Testing** | 6-8 weeks | All quality gates live |

---

## Recommended Issues for v3 Roadmap

1. **[V3-T1-StatusEffects] Add StatusEffectManager unit tests (6h)** — Poison, bleed, burn, stun, regen, slow; immunity; duration; stacking
2. **[V3-T1-SaveSystem] Add SaveSystem integration tests (8h)** — Serialize/deserialize; validation; file I/O; corruption recovery
3. **[V3-T1-Equipment] Add equipment system unit tests (4h)** — Equip/unequip; stat bonuses; conflict detection
4. **[V3-T1-Achievements] Add AchievementSystem unit tests (4h)** — All 5 conditions; persistence; unlock logic
5. **[V3-T2-ClassHierarchy] Design + test abstract character class pattern (4h)** — Polymorphism; method resolution; loot system integration
6. **[V3-T3-InputValidation] Add input validation + boundary tests (4h)** — Negative values; stat overflow; invalid indices
7. **[CI-CoverageGates] Establish 80%+ coverage thresholds for high-risk systems (2h)** — GitHub Actions + Coverlet integration

---

## Decision: Testing Priority for v3

**Approved by:** Romanoff (Tester)  
**Recommended to:** Coulson (Lead), Hill (Dev), Barton (Systems Dev)

**Decision:**
1. **Tier 1 tests are BLOCKING** — Must complete before v3 feature work on shops/crafting begins. (2-3 week parallel work with Coulson's architecture refactoring.)
2. **80%+ coverage gates are ENFORCED** — StatusEffects, Equipment, SaveSystem, Achievements must reach 90%+ or merge is rejected by CI.
3. **Manual review is REQUIRED** — Romanoff approves SaveSystem file I/O, Equipment stat logic, and AchievementSystem persistence before merge.
4. **Regression suite is MAINTAINED** — All v2 edge case tests must pass every build.

**Rationale:** v2 shipped with untested systems (SaveSystem, Achievements) that are foundational for v3 (save/load, persistence). Testing debt must be paid before new features build on top of broken foundations.




---

# v3 Planning Session — 2026-02-21

### What v2 Delivered
- **Combat Foundation:** Turn-based, crits, dodge, status effects (6 types), abilities with cooldowns, mana resource
- **Enemy Variety:** 9 types (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss, GoblinShaman, StoneGolem, Wraith, VampireLord, Mimic)
- **Content:** 5-floor dungeons, shrines (gold economy sinks), procedural generation, seeded runs, 5 achievements
- **Systems:** Config-driven enemy scaling, loot tables per enemy, equipment slots, save/load, GameEvents


### Critical Gaps (Systems Perspective)

1. **Enemy Behaviors Are Static**
   - All enemies perform identical action: attack if possible, else skip
   - No tactical decision-making (regen when low, heal allies, dodge strategically)
   - Bosses have Phase 2 enrage/charge, but regular enemies feel samey
   - **Impact:** Combat vs 2nd Goblin plays identical to 1st Goblin

2. **Boss Variety is Limited**
   - Single DungeonBoss type; only variation is scaling + enrage mechanic
   - No thematic boss variety (elemental, summoner, undead, etc.)
   - No dynamic phase transitions or multi-phase strategies
   - **Impact:** Final encounter lacks personality; replay value dependent on RNG difficulty

3. **Dungeon Environments Are Passive**
   - Rooms have flavor text but no mechanical impact (description-only)
   - No environmental hazards (traps, fire, falling blocks, poisonous fog)
   - No dynamic events (room collapses, enemies roused, treasure revealed)
   - **Impact:** Exploration feels one-dimensional; combat location doesn't matter

4. **Difficulty is One-Size-Fits-All**
   - Fixed difficulty curve via enemy scaling formula (+12% per level)
   - No accessibility modes (casual players, hardcore challenge seekers)
   - Elite variant system exists but is crude (5% spawn, +50% stats, no special abilities)
   - **Impact:** Game alienates casual players; hardcore players lack engaging challenge variants

5. **Economy Lacks Depth**
   - Shrines exist but are sparse (15% room spawn) and limited (heal, stat boosts)
   - No merchant/shop system for consumable purchases or item upgrades
   - No crafting or progression sinks beyond leveling
   - **Impact:** Gold is collected but feels purposeless; players hoard without meaningful choices

6. **Item System is Generic**
   - Random item drops; no progression tiers or special item families
   - No unique/rare items with special mechanics
   - Equipment slots exist but no transmog or upgrade paths
   - **Impact:** Looting feels random; no aspirational item hunting

---

## v3 Proposed Features (8 Issues)


### Wave 1: Foundation
These are prerequisites for higher-impact features and should be tackled first.

#### 1. **Difficulty Modes & Scaling Options** → Foundation Wave
- **Title:** Difficulty Modes & Scaling Options
- **Description:** Implement 3 difficulty presets (Casual/Normal/Hard) that scale enemy stats and elite spawn rates. Add per-run modifiers (Half Gold, Double Enemies, Permadeath). Display warnings before starting runs.
- **Why:** Accessibility + replayability. Hard mode challenges veterans; Casual mode welcomes new players. Modifiers create emergent challenge variants.
- **Agent:** Barton (implement difficulty formula and modifier system)
- **Effort:** 1-2 sprints
- **Dependencies:** None (builds on existing EnemyFactory.CreateScaled)

---


### Wave 2: Core Depth

#### 2. **Enemy AI Behaviors - Special Actions & Patterns** → Core Wave
- **Title:** Enemy AI Behaviors - Special Actions & Patterns  
- **Description:** Implement context-aware enemy actions. Troll regenerates HP when below 30%. Vampire Lord lifesteal on hit. Goblin Shaman heals allies (if group spawned). Wraith phases dodge every 3 turns. Stone Golem applies Stun to player on hit.
- **Why:** Combat depth without adding new mechanics. Telegraphed actions create counterplay (Poison counters Troll regen). Enemies feel intelligent, not random.
- **Agent:** Barton (add AI decision logic to each enemy class)
- **Effort:** 2-3 sprints
- **Implementation:** Add `GetAction()` method to Enemy base class; CombatEngine calls it instead of always attacking
- **Dependencies:** StatusEffectManager exists; needs slight extension (Apply/Remove method for group effects)

#### 3. **Boss Variety - Multiple Boss Types & Mechanics** → Core Wave
- **Title:** Boss Variety - Multiple Boss Types & Mechanics
- **Description:** Create 3-4 boss archetypes: Elemental Boss (spreads AOE hazard each turn), Summoner Boss (spawns minion enemies), Undead Boss (resurrects at 50% HP with less stats), Void Boss (steals player buffs, applies Weakened). Each has 2-3 phase transitions with unique attacks.
- **Why:** Final encounter defines game memory. Multiple bosses create narrative variety, force different strategies, improve replayability.
- **Agent:** Barton (design boss subclasses, phase transition logic)
- **Effort:** 2-3 sprints
- **Implementation:** Boss base class with `Phase` property; transitions trigger at HP thresholds. Each phase has unique `PerformBossAction()` behavior.
- **Dependencies:** Enemy scaling system; StatusEffectManager for debuff mechanics

#### 4. **Environmental Hazards - Procedural Dungeon Events** → Core Wave
- **Title:** Environmental Hazards - Procedural Dungeon Events
- **Description:** Procedurally spawn room hazards (15-20% of rooms): Spike Traps (3 dmg/turn to all combatants), Fire Pits (apply Burn status), Falling Blocks (dodge checks each turn), Poisonous Fog (apply Poison). Hazards activate at combat start, persist across turns.
- **Why:** Static dungeons become dynamic. Hazards damage both player and enemies (positive: rebalances combat). Room choice matters; players hide from hazards or use them strategically.
- **Agent:** Barton (implement RoomHazard class and hazard logic in CombatEngine)
- **Effort:** 1-2 sprints
- **Implementation:** Add `Hazard` property to Room; CombatEngine.RunCombat() processes hazards at turn start.
- **Dependencies:** Room model modifications

#### 5. **Elite Variants - Rarer Powerful Enemy Spawns** → Core Wave
- **Title:** Elite Variants - Rarer Powerful Enemy Spawns
- **Description:** Expand elite system: difficulty-scaled spawn rates (Casual 1%, Normal 5%, Hard 10%). Elites gain +50% stats + random special ability (bonus dodge, crit damage, status immunity). Color-coded names (e.g., "Elite Goblin" vs "Mighty Goblin"). Higher XP/loot rewards.
- **Why:** Elite encounters create memorable spikes without new enemy types. Stat scaling alone isn't enough; special abilities force tactics.
- **Agent:** Barton (extend EnemyFactory.CreateElite with ability system)
- **Effort:** 1 sprint
- **Implementation:** Enum of elite abilities (DodgeBoost, CritBoost, StatusImmune); elite constructor randomly selects one.
- **Dependencies:** EnemyFactory and scaling system already exist

---


### Wave 3: Advanced Features

#### 6. **Merchant Shop System - Economy & Item Upgrades** → Advanced Wave
- **Title:** Merchant Shop System - Economy & Item Upgrades
- **Description:** Spawn Merchant NPC in 1-2 rooms per floor. Shop sells consumables (Health/Mana potions 30-50g), level-locked equipment (bronze/silver/gold tiers), ability unlocks (learn new abilities early for cost). Merchants offer item transmog (combine duplicates for rarer item).
- **Why:** Gold becomes meaningful. Shop gives player agency (buy potion now or save for better sword?). Item progression ties to playstyle.
- **Agent:** Coulson or Hill (NPC system, UI) + Barton (balance prices, shop logic)
- **Effort:** 2-3 sprints
- **Implementation:** Shop state persists per run; prices scale with player level. Transmogrification system uses pseudorandom determinism (seeded).
- **Dependencies:** NPC framework; player level-based shop item availability

#### 7. **Procedural Room Types - Varied Dungeon Content** → Advanced Wave
- **Title:** Procedural Room Types - Varied Dungeon Content
- **Description:** Expand room generation: Library (grants lore, one-time +1 ability unlock discount), Armory (guaranteed 1-2 equipment drops), Miniboss Chamber (1 elite + 2 regular enemies + chest), Treasury (1500g guaranteed but 50% chance of trap damage), Shrine Room (2 shrines instead of 1). Dungeon Generator places room types procedurally.
- **Why:** Exploration gains reward/risk tension. Rooms feel thematic, not generic. "Found an Armory!" moment is memorable.
- **Agent:** Hill or Coulson (room generation) + Barton (room mechanics like Miniboss spawn)
- **Effort:** 1-2 sprints
- **Implementation:** Room type enum; DungeonGenerator places types at 20-30% rate. GameLoop.EnterRoom() reads type and triggers mechanics.
- **Dependencies:** Room model already exists; minor enum addition

#### 8. **Advanced Status Effects - Elemental & Crowd Control** → Advanced Wave
- **Title:** Advanced Status Effects - Elemental & Crowd Control
- **Description:** Expand effects: Burn (2 dmg/turn, spreads to player if player-inflicted), Freeze (reduces dodge by 50% for 2 turns), Weakness (target takes +25% damage, 3 turns), Shield (absorbs 15 damage once, auto-removes). Introduce new abilities that use these: Fireball (AoE Burn), Frost Nova (Freeze all), Cursed Blade (Weakness on hit).
- **Why:** Status effects become strategic. Poison used to counter Regen; Weakness counters armor stacking. Elemental theme adds flavor without new mechanics.
- **Agent:** Barton (extend StatusEffectManager, add new abilities to AbilityManager)
- **Effort:** 1-2 sprints
- **Implementation:** Extend StatusEffect enum; add effect descriptions and interaction rules. New abilities unlock at L10+.
- **Dependencies:** StatusEffectManager and AbilityManager exist and are extensible

---

## Prioritization & Wave Timing

**Foundation (Sprint 1):**
- Difficulty Modes (enables balanced playtesting for other features)

**Core (Sprint 2-4):**
1. Enemy AI Behaviors (highest impact: combat feels alive)
2. Boss Variety (final encounter variety)
3. Environmental Hazards (dungeon depth)
4. Elite Variants (elite encounters feel special)

**Advanced (Sprint 5+):**
- Merchant Shops (economy depth, but optional for core loop)
- Room Types (thematic variety, but doesn't block other features)
- Advanced Status Effects (builds on existing; polishes system)

---

## Gap Analysis: What's Missing From v2

| Gap | Addressed By | Priority |
|-----|--------------|----------|
| Static enemy behavior | Enemy AI Behaviors | High |
| Single boss type | Boss Variety | High |
| No environmental challenge | Environmental Hazards | High |
| One difficulty level | Difficulty Modes | High |
| Gold feels purposeless | Merchant Shop System | Medium |
| Generic rooms | Procedural Room Types | Medium |
| Limited status effects | Advanced Status Effects | Low |
| No elite abilities | Elite Variants | Medium |

---

## Key Design Decisions for v3

1. **AI Over New Mechanics:** Add depth via enemy behavior, not new status effects. Existing effects (Poison, Stun, Regen) support rich interactions when AI uses them tactically.

2. **Hazards Don't Target Player:** Environmental hazards damage both player AND enemies. This rebalances combat without nerfing players; creates resource depletion for both.

3. **Difficulty as Foundation:** Modes are a prerequisite. All future balancing (elite spawn rates, boss health) keys off difficulty setting.

4. **Shops Don't Break Economy:** Prices scale with player power. Consumables are convenience; core progression remains combat-based.

5. **Room Types Are Flavor + Mechanics:** Library gives story; Armory gives gear; Miniboss gives challenge. No room type is a trap (except Treasury, by design).

---

## Testing Strategy

- **Unit tests:** Hazard damage, elite ability rolls, boss phase transitions
- **Integration tests:** Difficulty scaling affects enemy spawns, shop item availability
- **Playtests:** Does Hard mode feel challenging? Does Casual mode feel accessible? Do elite encounters stand out?

---

## Next Steps (Post v2)

1. **Code Review v2 stabilization** → confirm no regressions
2. **Create Wave 1 ticket:** Difficulty Modes
3. **Schedule design review:** Core wave features (AI, Boss Variety, Hazards, Elites)
4. **Estimate effort:** Coordinate with Hill (room system changes) and Coulson (NPC framework)
5. **Spike:** AI behavior architecture with Hill and Coulson (can use existing Enemy properties or need new interface?)


### 1. Player Model Decomposition Blocking v3
**Finding:** Player.cs is 273 LOC mixing 7 concerns: stats, inventory, equipment, mana, abilities, health, gold/xp.
**Impact:** Adding character classes, shop system, or crafting requires refactoring Player without breaking saves.
**Decision:** Decompose Player.cs into PlayerStats, PlayerInventory, PlayerCombat modules in Wave 1.
**Reasoning:** High ROI; unblocks 3 major features (classes, shops, crafting); reduces regression risk.


### 2. Equipment System Needs Extraction from Player
**Finding:** EquipItem/UnequipItem logic in Player prevents equipment config system and shops.
**Impact:** Can't build merchant NPCs or equipment drop customization without refactoring.
**Decision:** Create EquipmentManager to own equipment state and stat application.
**Rationale:** Config-driven equipment (like items/enemies) reduces hardcoding; enables external stat systems.


### 3. Status Effect System Requires Composition Refactor
**Finding:** StatusEffectManager hardcodes 6 effects; no config-driven or stackable effects.
**Impact:** Elemental system, effect combos, and custom effects blocked.
**Decision:** Refactor StatusEffectManager to use IStatusEffect interface + config system.
**Rationale:** Enables v3 feature (elemental damage), v4 feature (effect combos); reduces branching logic.


### 4. Integration Testing Gap for System Interactions
**Finding:** 91.86% unit test coverage, but zero integration tests for systems interacting (combat → loot → equipment).
**Impact:** Refactoring Player.cs risks regressions in CombatEngine, InventoryManager, SaveSystem interaction.
**Decision:** Create integration test suite (Phase 1) testing multi-system flows.
**Rationale:** Supports safe refactoring; catches edge cases (stun + flee, status + save/load, etc.).


### 5. Inventory System Lacks Validation
**Finding:** Inventory is List<Item> with no weight/slot limits; TakeItem logic scattered.
**Impact:** Shop systems can't enforce inventory constraints; duping bugs possible.
**Decision:** Create InventoryManager with weight/slot validation and centralized add/remove logic.
**Rationale:** Prepares for trading/shops; reduces bugs; improves save/load robustness.


### 6. Ability System Too Combat-Focused
**Finding:** AbilityManager tied to combat; no passive abilities, trait system, or chaining.
**Impact:** Skill trees and builds in v4 require major redesign.
**Decision:** Extend AbilityManager to support passive abilities + cooldown groups (v3 foundational work).
**Rationale:** Unblocks v4 skill trees with minimal v3 work; improves ability reusability.


### 7. Character Class Architecture Missing
**Finding:** No player class concept; all players same stats/abilities/playstyles.
**Impact:** v3 feature "select class at start" has no foundation.
**Decision:** Design ClassDefinition config system + ClassManager (depends on Player decomposition).
**Rationale:** Config-driven approach (like enemies); unblocks class selection, balancing, and testing.


### 8. Save System Fragility with Refactoring
**Finding:** SaveSystem couples to Player structure; no version tracking or migration path.
**Impact:** Player.cs decomposition breaks all existing saves.
**Decision:** Add SaveFormatVersion + migration logic to SaveSystem before refactoring Player.
**Rationale:** Protects user data; establishes pattern for future schema changes.

---

## v3 Wave Structure (Recommended)


### Wave 1: Foundation (Refactoring + Integration Testing)
**Goal:** Stabilize architecture for v3 features.
**Work:**
1. **Player Decomposition** — Split into PlayerStats, PlayerInventory, PlayerCombat
2. **EquipmentManager Creation** — Extract from Player, design stat application interface
3. **InventoryManager Validation** — Add weight/slot checking, centralize item logic
4. **Integration Test Suite** — Combat→loot, equipment→combat, save→load with all features
5. **SaveSystem Migration** — Add version tracking, support old/new formats

**Critical Path:** Player decomposition → SaveSystem migration → integration tests
**Agents:** Hill (Player decomposition, SaveSystem), Barton (EquipmentManager, InventoryManager), Romanoff (integration tests)
**Duration:** ~40 hours, 2-week sprint


### Wave 2: Systems (Config-Driven Architecture)
**Goal:** Add character classes and ability expansion.
**Work:**
1. **ClassDefinition System** — Config-driven class stats, starting equipment, class abilities
2. **ClassManager** — Class selection, stat templates, class-specific ability grants
3. **Ability System Expansion** — Passive abilities, ability groups, cooldown pools
4. **Achievement System Expansion** — Class-based achievements, multi-tier achievements

**Dependency:** Requires Wave 1 Player decomposition  
**Agents:** Hill (ClassManager, config), Barton (ability expansion), Romanoff (achievement tests)
**Duration:** ~35 hours, 2-week sprint


### Wave 3: Features (Merchant/Crafting)
**Goal:** Add economic systems.
**Work:**
1. **Shop System Design** — NPC merchants, shop inventories, dynamic pricing
2. **Crafting System** — Recipe definitions, ingredient validation, output generation
3. **Economy Balancing** — Item tiers, shop profit/loss, crafting margins

**Dependency:** Requires Wave 1 equipment/inventory refactoring  
**Agents:** Hill (shop config, economy), Barton (crafting logic, NPC interaction), Romanoff (economy testing)
**Duration:** ~30 hours, 2-week sprint


### Wave 4: Polish (Enemy/Dungeon Tuning)
**Goal:** Expand content and balance.
**Work:**
1. **New Enemy Types** — 5-8 new enemies (elemental damage types, ranged attackers)
2. **Shrine Upgrades** — New shrine types (mana regen, temporary buffs, class bonuses)
3. **Loot Balancing** — Adjust drop rates, add class-specific loot tables
4. **Dungeon Difficulty Tuning** — Adjust enemy scaling, boss mechanics

**Dependency:** Independent; can run parallel to Wave 3 in final sprint  
**Agents:** Barton (new enemies, shrines), Romanoff (balancing tests)
**Duration:** ~25 hours, 1-2 week sprint

---

## v3 Scope Decision


### IN Scope (Must Have)
- ✅ Player.cs decomposition (PlayerStats, PlayerInventory, PlayerCombat)
- ✅ EquipmentManager + equipment config system
- ✅ InventoryManager with validation
- ✅ Integration test suite (multi-system flows)
- ✅ Character classes (config-driven)
- ✅ Shop system with NPC merchants
- ✅ Basic crafting system
- ✅ SaveSystem migration + version tracking


### OUT of Scope (v4 or Later)
- ❌ Skill trees (requires stable Player + Ability architecture)
- ❌ Permadeath/hardcore modes (SaveSystem too fragile)
- ❌ Multiplayer/lobbies (no session system)
- ❌ PvP arena (no turn order/spectating system)
- ❌ Elemental damage system (requires Status effect composition)
- ❌ Trading between players (network/lobby system needed)

---

## Architectural Patterns Established for v3+


### Pattern 1: Configuration-Driven Entities
**Rule:** All extensible entities (Classes, Abilities, StatusEffects, Shops) defined in config, not hardcoded.
**Precedent:** ItemConfig, EnemyConfig, LootTable — apply same pattern.
**Benefit:** Balancing without code changes; easier testing; mods-friendly.


### Pattern 2: Composition Over Inheritance
**Rule:** Use IStatusEffect, IAbility interfaces; compose abilities from components.
**Rationale:** Avoids inheritance hierarchy explosion (combat abilities vs. passive abilities vs. class-specific).
**Implementation:** Each interface has Execute/Update methods; StatusEffectManager/AbilityManager aggregate.


### Pattern 3: Manager Pattern for Subsystems
**Rule:** Each major subsystem owns a manager class (EquipmentManager, InventoryManager, ClassManager).
**Responsibility:** Manager validates state changes, applies side effects, fires events.
**Dependency Injection:** Managers receive config, Random, GameEvents via constructor.
**Testing:** All managers use injectable dependencies (no static Random, no hardcoded config).


### Pattern 4: Event-Based Cross-System Communication
**Rule:** Systems don't call each other directly; they fire events and subscribe.
**Precedent:** GameEvents system established in v2; extend with EquipmentChanged, ItemCrafted, ClassSelected.
**Benefit:** Decouples features; enables future systems (achievements, UI updates) without touching core logic.


### Pattern 5: Defensive Null Checks & Validation
**Rule:** All public methods validate preconditions; throw ArgumentException for invalid state.
**Precedent:** Player.TakeDamage, Player.AddGold; apply across managers.
**Testing:** Every edge case (negative amounts, null items, invalid slot names) has a test.

---

## Integration Strategy: No Breaking Changes


### Principle: Backward Compatibility for Saves
- SaveSystem must support v1, v2, and v3 formats simultaneously.
- Migration happens on load; no player data loss.
- Two-release deprecation window (if data migration needed).


### Principle: Feature Flags for Risky Refactoring
- New EquipmentManager runs in parallel with Player equipment slots initially.
- New InventoryManager validates independently; old behavior unchanged.
- Classes are opt-in; existing save files load as "unclassed" (balanced stats).


### Principle: Incremental Merging
- One subsystem per PR (Player.cs → PlayerStats, PlayerInventory, PlayerCombat in separate PRs).
- Each PR has integration tests for that subsystem only.
- Full integration tested in final PR before merge.


### Principle: Design Review Before Coding
- Each Wave starts with architecture ceremony (contracts, interfaces, dependencies).
- No implementation without signed-off design.
- Prevents rework and integration bugs.

---

## Success Criteria for v3


### Foundation (Wave 1)
- ✅ Player.cs decomposed into 3 focused modules (<150 LOC each)
- ✅ EquipmentManager created + equipment config system working
- ✅ InventoryManager enforces weight/slot limits
- ✅ Integration test suite covers 10+ multi-system flows
- ✅ SaveSystem handles v2→v3 migration with zero data loss
- ✅ All tests pass; coverage >90%


### System Design (Wave 2)
- ✅ ClassDefinition config system defined (5 classes with unique stats/abilities)
- ✅ ClassManager created; class selection working
- ✅ AbilityManager extended for passive abilities
- ✅ Class-based achievements implemented
- ✅ All tests pass; coverage >90%


### Features (Wave 3)
- ✅ Shop system deployed (3+ merchant NPCs with rotating inventory)
- ✅ Crafting system working (5+ recipes with ingredient validation)
- ✅ Economy balanced (no exploits, sensible profit margins)
- ✅ All tests pass; coverage >90%


### Content (Wave 4)
- ✅ 8 new enemy types implemented
- ✅ Difficulty curves balanced (easier early floors, harder late)
- ✅ New shrine types integrated
- ✅ No regressions from v2

---

## Risk Assessment


### HIGH Risk (Mitigated)
- **Player.cs Refactoring:** Breaks all old saves. *Mitigated by SaveSystem migration + version tracking.*
- **Multi-System Integration:** Stat application, equipment/ability interaction bugs. *Mitigated by integration test suite.*


### MEDIUM Risk (Managed)
- **Scope Creep:** Features block each other (shops need classes, classes need Player split). *Managed by sequential Wave structure.*
- **Config System Complexity:** Too many config files, hard to balance. *Managed by pattern review + centralized config validation.*


### LOW Risk (Accepted)
- **New Enemy Types:** Standard Barton work; low risk given existing EnemyFactory.
- **Achievement Expansion:** Independent; existing achievement system proven.

---

## Team Assignments (Preliminary)


### Hill (Architecture, Models, Persistence)
- Player.cs decomposition (PlayerStats module)
- ClassManager + ClassDefinition config system
- SaveSystem migration + version tracking
- Shop system architecture + NPC config

**Estimate:** 35 hours across v3


### Barton (Systems, Combat, Inventory, Content)
- EquipmentManager creation
- InventoryManager creation + validation
- Ability system expansion
- Crafting system implementation
- New enemy types + shrines
- All non-Player content integration

**Estimate:** 40 hours across v3


### Romanoff (QA, Testing, Balancing)
- Integration test suite (multi-system flows)
- Edge case testing (status + equipment, crafting + save/load, etc.)
- Economy balancing (shop prices, crafting margins)
- Difficulty curve analysis
- Content testing (new enemies, shrines, recipes)

**Estimate:** 30 hours across v3

---

## Next Actions

1. **Immediately:** Present v3 roadmap to team; request approval of Wave structure and agent assignments.
2. **This Week:** Schedule design review ceremony for Wave 1 (Player decomposition contracts).
3. **Next Week:** Kick off Wave 1 Phase 1 (integration test suite + SaveSystem migration).
4. **Ongoing:** Monthly retrospectives to validate Wave structure; adjust based on blockers.

---

## Appendix: Issues to Create on GitHub


### Foundation Issues (Wave 1)
1. **Issue: Player.cs Decomposition** (Foundation)
   - Split into PlayerStats, PlayerInventory, PlayerCombat
   - Preserve all existing functionality; add integration tests
   - Save file migration required
   
2. **Issue: EquipmentManager Creation** (Foundation)
   - Extract equipment state from Player
   - Create equipment config system
   - Support stat application via manager instead of Player methods
   
3. **Issue: InventoryManager Validation** (Foundation)
   - Enforce weight/slot limits
   - Centralize item add/remove/use logic
   - Support shop and crafting systems
   
4. **Issue: Integration Test Suite** (Foundation)
   - Multi-system flows (combat→loot→equipment, equipment→combat stat changes)
   - Edge cases (stun+flee, status+save/load, ability+cooldown)
   - Regression tests for boss mechanics, shrine interactions
   
5. **Issue: SaveSystem Migration** (Foundation)
   - Add SaveFormatVersion tracking
   - Support v2→v3 migration (Player decomposition)
   - Test backward compatibility


### System Design Issues (Wave 2)
6. **Issue: Character Class System** (Core)
   - ClassDefinition config (5 classes: Warrior, Mage, Rogue, Paladin, Ranger)
   - ClassManager with stat templates and ability grants
   - Class selection at game start
   
7. **Issue: Ability System Expansion** (Core)
   - Passive abilities support
   - Ability cooldown groups
   - Class-specific ability trees


### Feature Issues (Wave 3)
8. **Issue: Shop System** (Core)
   - NPC merchant implementation
   - Shop inventory config
   - Dynamic pricing and stock rotation

---

**Signed by:** Coulson, Lead
**Status:** PROPOSED (awaiting team approval)


### Identified Gaps in Player Experience

1. **No Character Agency / Customization**
   - All players are identical "generic warrior"
   - No build diversity or strategic choices at character creation
   - Limited replayability—every run feels the same mechanically

2. **Weak Progression Hooks**
   - Leveling is purely binary: gain stats, nothing else
   - No unlocks, no meaningful milestones beyond "level 5"
   - Abilities unlock via AbilityManager, but no strategic choice involved

3. **Content Repetition**
   - 5 floors × 4 rooms = 20 rooms always identical type ("combat room")
   - No environmental variety: no shrines, treasures, arenas
   - Dungeon has no thematic flavor or narrative context

4. **Clarity / UX Issues**
   - No map display—players can't see dungeon layout, navigate by memory
   - Combat log is ephemeral—no record of turn history
   - Inventory is list-heavy; no quick equipment view
   - No narrative framing—purely mechanical "escape the dungeon"


### Highest-Impact v3 Features

**Player Agency (Replayability)**
- **Character Classes:** Warrior/Rogue/Mage with distinct stat curves, starting abilities, trait pools
  - Expected impact: 3x playstyles (vs. 1 generic), changes build strategy
  - Why: Fundamental franchise mechanic in roguelikes; drives run variety

- **Trait System:** Passive bonuses unlocked at milestones, class-specific options
  - Expected impact: Every 5 levels = meaningful choice (2 trait options)
  - Why: Micro-progression between levels, increases engagement

- **Skill Trees:** Config-driven trees per class, grant stat bonuses or new abilities
  - Expected impact: Path optimization (different for each playthrough), build guides emerge
  - Why: Deep engagement mechanic, community theory-crafting

**Progression Hooks**
- **Trait Selection at Level-Up:** Prompt at Lvl 5/10/15/20 with 2 random traits
  - Expected impact: Player anticipates milestones, makes strategic decisions
  - Why: Turns passive leveling into active choice

**Content Variety**
- **Variant Room Types:** Shrines (blessings/curses), Treasuries (mega-loot), Elite Arenas
  - Expected impact: Breaks "all combat rooms" monotony, adds spatial strategy
  - Why: Small feature, high leverage (5 new room types = 25% room diversity)

- **Dungeon Variants:** "Standard" / "Forsaken" / "Bestiary" / "Cursed" with lore flavor
  - Expected impact: 4x narrative flavor, enemy distribution changes feel fresh
  - Why: Minimal new code (config + generator tweak), huge engagement increase

**UX Improvements**
- **Mini-Map:** ASCII grid showing visited rooms, current position, exit
  - Expected impact: Players feel less random-lost, reduces frustration
  - Why: Addresses common roguelike pain point; ASCII fits aesthetic

- **Combat Clarity:** Turn log (last 5 turns), crit/dodge notifications, action separation
  - Expected impact: Players understand why they took damage, learn combat patterns
  - Why: Transparency increases trust in RNG, enables skill improvement

---

## Recommended v3 Roadmap


### WAVE 1: Foundation (Unlocks Further Content)
*Prerequisite for traits/skill trees; establishes character identity*

1. **Character Class System** (Issue)
   - Models: Add `CharacterClass enum {Warrior, Rogue, Mage}`
   - Player: Add `Class` property, refactor stat defaults by class
   - GameLoop: Add class selection menu at startup
   - Agents: Hill (models), Barton (combat), Romanoff (tests)

2. **Class-Specific Traits** (Issue)
   - Models: Add `Trait` class (Name, Description, StatModifier, Class)
   - Player: Add `ActiveTraits List<Trait>`, trait application logic
   - Config: Add `traits-<classname>.json` per class (4-5 traits each)
   - Agents: Hill (encapsulation), Barton (balance), Romanoff (tests)

3. **Skill Tree Foundation** (Issue)
   - Models: Add `SkillNode` (Name, UnlockLevel, StatBonus, Class)
   - Models: Add `SkillTree List<SkillNode>` per class, config-driven
   - GameLoop: Show tree at level-up (visual representation)
   - Agents: Hill (tree UI), Barton (stat application), Romanoff (unlock tests)

**Expected Timeline:** 2-3 weeks (parallelizable)  
**Blockers:** None (builds on existing Player/config patterns)

---


### WAVE 2: Core (Player Agency & Content)
*Multiplies engagement; traits become meaningful; room variety breaks monotony*

4. **Trait Selection at Level-Up** (Issue)
   - GameLoop: Pause at Lvl 5/10/15/20, show trait menu
   - Player: Apply selected trait stat modifiers, persist in save
   - DisplayService: Show "Available Traits" with descriptions
   - Agents: Hill (UI/flow), Romanoff (save tests)

5. **Variant Room Types** (Issue)
   - Models: Add `RoomType enum {Combat, Shrine, Treasury, EliteArena}`
   - DungeonGenerator: Place shrines (2), treasuries (1), elite arenas (2) per dungeon
   - GameLoop: Handle shrine blessings (menu prompt), treasury auto-loot, arena enemies
   - Agents: Hill (room types), Barton (elite logic), Romanoff (tests)

6. **Combat Clarity System** (Issue)
   - Models: Add `CombatTurn struct` (attacker, action, damage, status)
   - CombatEngine: Log turns to `List<CombatTurn>`
   - DisplayService: Show last 5 turns, crit/dodge notifications
   - Agents: Hill (DisplayService), Barton (log data), Romanoff (integration)

**Expected Timeline:** 2-3 weeks (parallelizable)  
**Blockers:** Requires Wave 1 (traits foundation)

---


### WAVE 3: Advanced (Content Depth)
*Adds narrative flavor; extends replayability; polishes UX*

7. **Dungeon Variants & Lore** (Issue)
   - Models: Add `DungeonVariant enum {Standard, Forsaken, Bestiary, Cursed}`
   - DungeonGenerator: Adjust enemy distribution per variant
   - GameLoop: Show variant-specific intro/outro flavor text
   - Config: Add `dungeon-variants.json` with enemy pool overrides
   - Agents: Hill (variant enum), Barton (config), Romanoff (integration)

8. **Mini-Map Display** (Issue)
   - DisplayService: Add `ShowMap(Room grid, Room current, Room exit)` method
   - Render: 5×4 ASCII grid with symbols (., ▓, *, !, B)
   - GameLoop: Show map after each move (or MAP command)
   - Agents: Hill (ASCII rendering), Romanoff (state tests)

**Expected Timeline:** 1-2 weeks (parallelizable)  
**Blockers:** None (orthogonal features)

---


### WAVE 4: Stretch Goals
*Beyond v3 priority; enables long-term engagement*

- **Prestige System:** Beat boss N times → unlock cosmetics/traits for future runs
- **Difficulty Selector:** Easy/Normal/Hard with enemy stat scaling & reward adjustments
- **Boss Phase 2:** Boss enrages at 50% HP (new attacks, phase-specific mechanics)
- **Leaderboard:** Track fastest wins, highest gold, best achievement hunts (JSON file)
- **Inventory UI:** Sell items, drop items, sort by type (extends shop system)

---

## Design Patterns & C# Considerations


### Encapsulation for Traits
```csharp
public class Trait
{
    public string Name { get; init; }
    public int AttackBonus { get; init; }
    public int DefenseBonus { get; init; }
    public CharacterClass Class { get; init; }
}

public class Player
{
    private List<Trait> _activeTraits = new();
    
    public void AddTrait(Trait trait)
    {
        if (_activeTraits.Count >= MaxTraits) 
            throw new InvalidOperationException("Max traits reached");
        _activeTraits.Add(trait);
        ApplyTraitStatModifiers();
    }
}
```


### Config-Driven Traits
```json
{
  "WarriorTraits": [
    {
      "Name": "Unbreakable",
      "Description": "+10% Defense",
      "DefenseBonus": 1,
      "Class": "Warrior"
    }
  ]
}
```


### Variant Room Handling
```csharp
public enum RoomType { Combat, Shrine, Treasury, EliteArena }

public class Room
{
    public RoomType Type { get; init; } = RoomType.Combat;
    // ... existing Enemy, Items, visited state
}

// In GameLoop
case RoomType.Shrine:
    HandleShrine(currentRoom);
    break;
case RoomType.Treasury:
    HandleTreasury(currentRoom);
    break;
```


### Map Rendering (ASCII)
```csharp
public void ShowMap(Room[,] grid, Room current, Room exit)
{
    var sb = new StringBuilder("┌─────────┐\n");
    for (int y = 0; y < grid.GetLength(0); y++)
    {
        sb.Append("│");
        for (int x = 0; x < grid.GetLength(1); x++)
        {
            var room = grid[y, x];
            char symbol = room == current ? '*' 
                        : room == exit ? '!' 
                        : room.Visited ? '.' 
                        : '▓';
            sb.Append(symbol);
        }
        sb.Append("│\n");
    }
    sb.Append("└─────────┘");
    _display.ShowMessage(sb.ToString());
}
```

---

## Open Questions & Decisions Needed

1. **Trait Cap:** Should player have max 3 active traits, or scale with level?
   - Recommended: Cap at 5 (prevents stat inflation, forces strategic choice)

2. **Skill Tree Visibility:** Show full tree at game start, or unlock nodes progressively?
   - Recommended: Show full tree grayed-out; highlight achievable nodes by level

3. **Shrine Blessing Duration:** Temporary buff (end of floor) or permanent?
   - Recommended: 5-turn duration (strategic for combat, not trivializing)

4. **Map Display Frequency:** Always show after move, or toggle with MAP command?
   - Recommended: Show after move by default, but allow toggle (accessibility)

5. **Difficulty Selector:** Part of v3, or defer to v4?
   - Recommended: Defer to v4 (simpler v3 ships faster; difficulty unlocks in v4)

---

## Coordination Notes

**With Coulson (Lead):** Confirm wave priorities and merge decision into canonical decisions.md once approved.

**With Barton (Systems):** Enemy distribution config variants, elite arena mechanics, stat balance across new traits.

**With Romanoff (Tester):** Unit tests for trait application, config loading, map rendering, trait selection persistence.

**With Scribe:** Merge this inbox file into decisions.md after team review.

---

## Expected Outcomes

- **Replayability:** +300% (3 classes × trait choices × skill paths)
- **Content Variety:** +400% (5 room types × 4 dungeon variants)
- **Engagement:** +200% (milestone progression, mini-map orientation)
- **Codebase Health:** Minimal (builds on encapsulation patterns, config-driven design, no new dependencies)

---

## Metrics for Success

- [ ] v3a (Wave 1) ships by Week 3 with character classes + traits + skill trees
- [ ] v3b (Wave 2) ships by Week 6 with room variants + trait selection + combat clarity
- [ ] v3c (Wave 3) ships by Week 8 with dungeon variants + mini-map
- [ ] Player feedback: "Feels more like a roguelike now" (trait/class variety)
- [ ] Community engagement: Trait build guides emerge on forums/Discord



### 1. StatusEffectManager (84 LOC, 0 tests)

**Risk Level:** 🔴 **CRITICAL**

**What's untested:**
- Poison effect (3 damage/turn) application and removal
- Bleed effect (5 damage/turn) application and removal
- Burn effect (2 damage/turn) application and removal
- Stun effect (skip turn) application and removal
- Regen effect (4 HP/turn) application and removal
- Slow effect (reduce damage by 30%) application and removal
- Effect immunity (Stone Golem `IsImmuneToEffects`)
- Duration tracking and decrement
- Stacking behavior (refresh existing effects)
- Turn-start processing (damage application in order)
- Effect removal on duration <= 0

**Impact:**
- Combat balance untested; status effect damage may be incorrect
- Enemy immunity edge case breaks balance (Stone Golem not immune if test never runs)
- Duration bugs could cause infinite debuffs or premature removal
- Stun effect skipping unvalidated (critical for boss difficulty)

**Required Tests:**
```csharp
[Fact] void ApplyPoison_DealsDamageEachTurn()
[Fact] void BleedStack_RefreshesExistingDuration()
[Fact] void StoneGolem_IsImmuneToPoison()
[Fact] void Stun_SkipsEnemyAttack()
[Fact] void StatusEffect_RemovedAfterDurationExpires()
[Fact] void MultipleEffects_ProcessInOrder()
```

---


### 2. AchievementSystem (96 LOC, 0 tests)

**Risk Level:** 🔴 **CRITICAL**

**What's untested:**
- Glass Cannon condition: `player.HP < 10 && run won`
- Untouchable condition: `stats.DamageTaken == 0`
- Hoarder condition: `stats.GoldCollected >= 500`
- Elite Hunter condition: `stats.EnemiesDefeated >= 10`
- Speed Runner condition: `stats.TurnsTaken < 100`
- Achievement unlock state persistence (JSON save to AppData)
- Achievement list initialization
- Multiple runs tracking (unlock on 2nd run vs 1st)

**Impact:**
- Achievements unlock silently without test verification
- Save file corruption possible (JSON write permissions, invalid paths)
- Player never sees "Achievement Unlocked!" message; no way to verify
- Balance: Glass Cannon achievable? (< 10 HP is achievable at level 1-2 only)

**Required Tests:**
```csharp
[Fact] void GlassCannon_UnlocksWhenWinWithLowHP()
[Fact] void Untouchable_UnlocksWhenNoDamageTaken()
[Fact] void Achievement_PersistsToJSON()
[Fact] void Achievement_LoadsFromJSON()
[Fact] void CorruptedAchievementFile_LogsErrorGracefully()
```

---


### 3. SaveSystem (178 LOC, 0 tests)

**Risk Level:** 🔴 **CRITICAL**

**What's untested:**
- GameState serialization (Player + Room graph)
- Room graph reconstruction (exits dict roundtrip)
- File I/O safety (directory creation, permission errors, disk full)
- Deserialization validation (HP <= MaxHP, no negative stats, no null items)
- Corruption recovery (missing save file, invalid JSON, truncated file)
- Save name validation (empty string, special chars)
- Enemy state serialization (alive/dead, HP, abilities)

**Impact:**
- Save/load loses data or corrupts game state silently
- Soft-lock: Player loads save with invalid HP (e.g., HP=200 on 100 max)
- No directory exists → crash on first save attempt
- Corrupted JSON → exception uncaught, unrecoverable
- Room graph disconnection on load (exits not reconstructed)

**Required Tests:**
```csharp
[Fact] async Task SaveGame_CreatesDirectoryIfMissing()
[Fact] async Task LoadGame_ValidatesHPBounds()
[Fact] async Task CorruptedJSON_ReturnsGracefulError()
[Fact] async Task RoomExits_ReconstructedCorrectly()
[Fact] void SaveName_ValidatesEmptyString()
```

---


### 4. Equipment System (Player model, 0 dedicated tests)

**Risk Level:** 🟠 **HIGH**

**What's untested:**
- Equip weapon → +Attack bonus applied
- Equip armor → +Defense bonus applied
- Equip accessory → special effect active
- Unequip → stat bonus removed
- Equip different item → previous unequipped
- Stat overflow: Equip 10 weapons (attack = 100+?)
- Null equipment handling

**Impact:**
- Player can equip multiple weapons → stat exploitation
- Stat bonuses not applied or applied twice
- Combat balance broken (player 50 Attack vs enemy 18)

**Required Tests:**
```csharp
[Fact] void EquipWeapon_IncreaseAttack()
[Fact] void EquipArmor_IncreaseDefense()
[Fact] void EquipNewWeapon_UnequipsPrevious()
[Fact] void Unequip_RemovesStatBonus()
```

---


### 5. Enemy Variants & Config (9 enemy types + Elite 5%, 0 tests)

**Risk Level:** 🟠 **HIGH**

**What's untested:**
- Elite variant spawning (5% chance correctly implemented)
- Elite variant stat scaling (110% HP, 120% Attack, 110% Defense)
- EnemyConfig values match hardcoded stats in enemy classes
- Config-driven scaling works (config change → enemy stats update)

**Impact:**
- Elite enemies spawn with incorrect stats
- Balance: easy to break via config edit
- No validation that config matches enemy class defaults

**Required Tests:**
```csharp
[Theory]
[InlineData(0, 0.05)] // 1 in 20 spawns is elite
void EnemyFactory_Spawns5PercentElite(int seed, double expectedRate)

[Fact] void EliteGoblin_Stats110Percent()

[Fact] void EnemyConfig_LoadedCorrectly()
```

---

## Quality Risks for v3


### 1. Character Classes (New Feature)

**Risk:** No tests for inheritance hierarchy; polymorphic type casting not tested in combat/loot systems.

**Mitigation:**
- Create abstract `CharacterClass` interface
- Test base class → subclass method resolution (virtual methods work)
- Test combat engine accepts ICharacterClass polymorphically
- Test loot table respects class-specific drops

---


### 2. Shops & Crafting (New Features)

**Risk:** Persistent state (inventory qty, recipes) interacts with SaveSystem (untested). Concurrency: multiple shops loaded simultaneously.

**Mitigation:**
- SaveSystem tests pass first (Tier 1)
- Shop state serialization unit tests
- Crafting recipe validation (circular dependencies, invalid ingredient qty)
- Inventory qty overflow tests

---


### 3. Config Hotload & Multithreading

**Risk:** GameEvents and EnemyConfig are mutable global state. Concurrent reads during hotload could race.

**Mitigation:**
- GameEvents event handler registration tests (thread-safe subscription)
- Config read lock tests (blocking during reload)
- Concurrent combat + config reload tests

---

## Recommended v3 Testing Tiers


### Tier 1: Foundation (Blocking v3 Feature Work)

**Must complete before v3 core features (shops, crafting, classes) ship.**

| Issue | System | Est. Hours | Tests Required |
|-------|--------|-----------|-----------------|
| V3-T1-StatusEffects | StatusEffectManager | 6 | Poison/Bleed/Burn/Stun/Regen/Slow, immunity, duration, stacking |
| V3-T1-Equipment | Player equipment | 4 | Equip/unequip, stat bonuses, conflict detection |
| V3-T1-SaveSystem | SaveSystem | 8 | Serialization, deserialization, validation, file I/O, corruption recovery |
| V3-T1-Achievements | AchievementSystem | 4 | All 5 conditions, persistence, unlock logic |
| **Tier 1 Total** | | **22 hours** | |


### Tier 2: Infrastructure (New System Support)

**Complete before v3 advanced features (classes, multithreading) ship.**

| Issue | System | Est. Hours | Tests Required |
|-------|--------|-----------|-----------------|
| V3-T2-ClassHierarchy | Abstract class pattern | 4 | Polymorphism, method resolution, type casting |
| V3-T2-ShopSystem | Shop state + SaveSystem integration | 6 | Buy/sell logic, inventory qty, gold validation, persistence |
| V3-T2-CraftingSystem | Recipe validation + crafting logic | 6 | Ingredient checks, output generation, circular dependencies |
| V3-T2-EnemyVariants | Elite enemies + config | 3 | 5% spawn rate, stat scaling, config matching |
| **Tier 2 Total** | | **19 hours** | |


### Tier 3: Hardening (Edge Cases & Defensive Coding)

**Run in parallel with Tier 2; prioritize after Tier 1 blocks.**

| Issue | Focus | Est. Hours | Tests Required |
|-------|-------|-----------|-----------------|
| V3-T3-Boundaries | Input validation + bounds checks | 4 | Negative HP, invalid stat values, circular exits |
| V3-T3-Concurrency | GameEvents + config thread safety | 4 | Concurrent event registration, config read/write locks |
| V3-T3-Deserialization | SaveSystem error recovery | 3 | Corrupt JSON, missing files, invalid paths |
| V3-T3-Regressions | v2 edge cases (dead enemy, flee, status effects) | 2 | Regression suite |
| **Tier 3 Total** | | **13 hours** | |

---

## Quality Gates for v3


### Build-Time Gates (CI/CD)

```yaml
# .github/workflows/ci.yml
- name: Run Tests
  run: dotnet test

- name: Coverage Check
  run: |
    dotnet test /p:CollectCoverage=true /p:CoverageThreshold=80
    # Must maintain 80%+ on: StatusEffects, Equipment, SaveSystem, Achievements
```


### High-Risk Systems Coverage Targets

| System | v2 Coverage | v3 Target | Threshold |
|--------|-----------|----------|-----------|
| StatusEffectManager | 0% | 95% | ❌ Fail merge if < 90% |
| AchievementSystem | 0% | 95% | ❌ Fail merge if < 90% |
| SaveSystem | 0% | 95% | ❌ Fail merge if < 90% |
| Equipment system | 0% | 90% | ❌ Fail merge if < 85% |
| Overall codebase | ~70% | 80% | ❌ Fail merge if < 75% |


### Manual Code Review Gates

**Before merge, Romanoff (Tester) must approve:**

1. **SaveSystem reviews:**
   - [ ] File I/O handles missing directory (`Directory.CreateDirectory` called)
   - [ ] Deserialization validates all stat bounds (HP <= MaxHP, no negatives)
   - [ ] Corruption recovery logs error and returns null (doesn't crash)
   - [ ] Permission errors caught and reported gracefully

2. **Equipment system reviews:**
   - [ ] No way to equip multiple weapons (unequip previous first)
   - [ ] Stat bonuses correctly applied/removed
   - [ ] Balance: max possible Attack/Defense within design limits

3. **StatusEffectManager reviews:**
   - [ ] All 6 effects cause correct damage/heal each turn
   - [ ] Duration decrement happens before effect removal
   - [ ] Immunity check prevents application (Stone Golem test passes)
   - [ ] Stun effect actually skips turn (mock combat engine confirms)

4. **AchievementSystem reviews:**
   - [ ] All 5 conditions trigger under correct circumstances
   - [ ] Persistence: save file created with correct permissions
   - [ ] Unlock state survives serialize → deserialize cycle

---

## Regression Test Checklist (v2 Regressions)

**Add to CI regression suite (must pass every merge):**

- [ ] Dead enemy cleanup: `room.Enemy == null` after combat Won (WI-10 fix)
- [ ] Flee-and-return: Enemy HP persists when fleeing and re-entering
- [ ] Status effect during combat: Apply poison → combat → effect triggers → remove
- [ ] Level-up mid-combat: Gain level during combat, stat bonuses apply, combat continues
- [ ] Stun effect skips enemy turn (not player turn)
- [ ] Boss gate: Cannot enter exit room with boss alive
- [ ] Empty inventory: `USE` with no items → error, no crash

---

## Test Infrastructure Requirements for v3


### xUnit + Moq + FluentAssertions (Established)

**No changes needed.** v2 established stack is solid.


### New Requirements

1. **Async I/O Support for SaveSystem**
   ```csharp
   [Fact]
   public async Task SaveGame_CreatesFileAsync()
   {
       var game = new GameState(...);
       await SaveSystem.SaveGameAsync(game, "test-save");
       File.Exists(...).Should().BeTrue();
   }
   ```

2. **JSON Fixture Loading**
   ```csharp
   // fixtures/corrupt-save.json (invalid HP)
   private static GameState LoadFixture(string name)
   {
       var json = File.ReadAllText($"fixtures/{name}.json");
       return JsonSerializer.Deserialize<GameState>(json);
   }
   ```

3. **Time-Based Effect Testing**
   ```csharp
   [Fact]
   public void StatusEffect_RemovedAfterDurationTurns()
   {
       // Simulate N turn-start calls; effect should disappear on turn N+1
       for (int i = 0; i < duration; i++)
           manager.ProcessTurnStart(target);
       
       manager.ActiveEffects(target).Should().BeEmpty();
   }
   ```

4. **Immutable Test Data Builders**
   ```csharp
   var player = new PlayerBuilder()
       .WithHP(50)
       .WithAttack(20)
       .WithEquippedWeapon(new Item { Attack = 5 })
       .Build();
   ```

---

## Timeline Estimate

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| **Tier 1** | 2-3 weeks | StatusEffects, Equipment, SaveSystem, Achievements tests |
| **Tier 2** | 3-4 weeks | Class hierarchy, Shop, Crafting, Enemy variant tests |
| **Tier 3** | Ongoing | Edge case + regression tests in parallel |
| **CI Integration** | 1 week | GitHub Actions workflow + coverage gates |
| **Total v3 Testing** | 6-8 weeks | All quality gates live |

---

## Recommended Issues for v3 Roadmap

1. **[V3-T1-StatusEffects] Add StatusEffectManager unit tests (6h)** — Poison, bleed, burn, stun, regen, slow; immunity; duration; stacking
2. **[V3-T1-SaveSystem] Add SaveSystem integration tests (8h)** — Serialize/deserialize; validation; file I/O; corruption recovery
3. **[V3-T1-Equipment] Add equipment system unit tests (4h)** — Equip/unequip; stat bonuses; conflict detection
4. **[V3-T1-Achievements] Add AchievementSystem unit tests (4h)** — All 5 conditions; persistence; unlock logic
5. **[V3-T2-ClassHierarchy] Design + test abstract character class pattern (4h)** — Polymorphism; method resolution; loot system integration
6. **[V3-T3-InputValidation] Add input validation + boundary tests (4h)** — Negative values; stat overflow; invalid indices
7. **[CI-CoverageGates] Establish 80%+ coverage thresholds for high-risk systems (2h)** — GitHub Actions + Coverlet integration

---

## Decision: Testing Priority for v3

**Approved by:** Romanoff (Tester)  
**Recommended to:** Coulson (Lead), Hill (Dev), Barton (Systems Dev)

**Decision:**
1. **Tier 1 tests are BLOCKING** — Must complete before v3 feature work on shops/crafting begins. (2-3 week parallel work with Coulson's architecture refactoring.)
2. **80%+ coverage gates are ENFORCED** — StatusEffects, Equipment, SaveSystem, Achievements must reach 90%+ or merge is rejected by CI.
3. **Manual review is REQUIRED** — Romanoff approves SaveSystem file I/O, Equipment stat logic, and AchievementSystem persistence before merge.
4. **Regression suite is MAINTAINED** — All v2 edge case tests must pass every build.

**Rationale:** v2 shipped with untested systems (SaveSystem, Achievements) that are foundational for v3 (save/load, persistence). Testing debt must be paid before new features build on top of broken foundations.



---

# From: barton-v3-planning.md

# Barton — v3 Planning Roadmap

**Date:** 2026-02-20  
**Agent:** Barton (Systems Dev)  
**Context:** v2 stabilized with combat abilities, status effects, 9 enemy types, shrines, achievements. Planning v3 expansions.

---

## Analysis: v2 State & Gaps

### 2026-02-20: Pre-v3 Bug Hunt — Integration and State Integrity Issues

**By:** Coulson  
**Context:** Pre-v3 comprehensive architecture review across GameLoop ↔ CombatEngine ↔ Player ↔ SaveSystem ↔ StatusEffectManager. Identified 16 bugs (3 Critical, 4 High, 5 Medium, 4 Low).

**What:** Systematic review of integration points, null checks, state persistence, and cross-system contracts revealed critical bugs in boss mechanics, status effects, save/load, and initialization.

**Why:** v3 planning identified Player.cs decomposition as critical path; refactoring without addressing existing integration bugs risks compounding regressions.

---

## Critical Bugs (Must Fix Before v3 Wave 1)

**#2 — Boss enrage compounds on modified Attack**  
- **File:** Systems/Enemies/DungeonBoss.cs:98  
- **Issue:** CheckEnrage multiplies Attack by 1.5 each time it runs; if boss HP drops below 40% multiple times (e.g., healing), Attack compounds exponentially (22 → 33 → 49.5 → 74).  
- **Fix:** Line 98: `Attack = (int)(_baseAttack * 1.5)` to always multiply from original base.

**#3 — Boss enrage state not saved**  
- **File:** Systems/Enemies/DungeonBoss.cs:16  
- **Issue:** IsEnraged flag not serialized; after save/load, boss has enraged Attack value (33) but IsEnraged=false, breaking CheckEnrage logic.  
- **Fix:** SaveSystem must serialize DungeonBoss-specific fields (IsEnraged, IsCharging, ChargeActive) OR CheckEnrage must detect prior enrage (Attack != _baseAttack).

**#6 — EnemyFactory.Initialize never called**  
- **File:** Program.cs:22  
- **Issue:** DungeonGenerator created before EnemyFactory initialization; _enemyConfig is null, all enemies use fallback hard-coded stats instead of Data/enemies.json.  
- **Fix:** Add `EnemyFactory.Initialize("Data/enemies.json", "Data/items.json")` before line 22.

---

## High Severity (Integration Blockers)

**#1 — Boss enrage timing issue**  
- **File:** Engine/CombatEngine.cs:92  
- **Issue:** CheckEnrage called at turn start before player damage dealt; boss attacks at pre-enraged value the turn HP crosses 40% threshold.  
- **Fix:** Add second CheckEnrage call after player attack (line 168) so enrage triggers mid-combat.

**#4 — StatusEffect stat modifiers never applied**  
- **File:** Systems/StatusEffectManager.cs:113  
- **Issue:** GetStatModifier calculates Weakened (-50% ATK) and Fortified (+50% DEF) bonuses, but CombatEngine never calls GetStatModifier; buffs/debuffs have no combat effect.  
- **Fix:** Integrate GetStatModifier at CombatEngine damage calculations (lines 248, 289, 310).

**#11 — SaveSystem missing current floor**  
- **File:** Systems/SaveSystem.cs:48  
- **Issue:** GameState lacks _currentFloor field; player saves on Floor 3, loads as Floor 1 with Floor 3 enemy scaling (stat mismatch).  
- **Fix:** Add CurrentFloor to GameState and SaveData; restore in GameLoop.

**#12 — Shrine blessing permanent not temporary**  
- **File:** Engine/GameLoop.cs:508  
- **Issue:** Shrine Bless option (+2 ATK/DEF for "5 rooms") calls ModifyAttack/ModifyDefense with no expiration tracking; buff lasts forever.  
- **Fix:** Replace with StatusEffect.Blessed(5 turns) OR track room counter in Player state.

---

## Medium Severity (Correctness Issues)

**#5 — Boss charge race condition**  
- **File:** Engine/CombatEngine.cs:276  
- **Issue:** IsCharging set on warning turn → next turn sets ChargeActive=true but does not clear IsCharging → random roll can set IsCharging again → both flags true simultaneously.  
- **Fix:** Line 277: `boss.IsCharging = false;` after setting ChargeActive.

**#7 — GameLoop null checks missing**  
- **File:** Engine/GameLoop.cs:62  
- **Issue:** Run(player, startRoom) accepts nulls without validation; _player/startRoom assigned directly; NullReferenceException on line 71.  
- **Fix:** Add guards: `if (player == null) throw new ArgumentNullException(nameof(player));`

**#8 — Stun message shown twice**  
- **File:** Systems/StatusEffectManager.cs:68  
- **Issue:** CombatEngine shows "cannot act" when Stun effect blocks action (line 108); ProcessTurnStart also shows "stunned" message (line 68); duplicate output.  
- **Fix:** Remove Stun case from ProcessTurnStart (lines 67-69); message should only show when action blocked.

**#9 — Multi-floor uses same seed**  
- **File:** Engine/GameLoop.cs:466  
- **Issue:** HandleDescend creates `new DungeonGenerator(_seed)` with identical seed; all floors have same room layout.  
- **Fix:** Line 466: `new DungeonGenerator(_seed.HasValue ? _seed.Value + _currentFloor : null)`

**#13 — StatusEffect Weakened uses current stats**  
- **File:** Systems/StatusEffectManager.cs:119  
- **Issue:** Weakened penalty calculated as 50% of current Attack (including equipment bonuses); unequipping weapon changes base, breaking modifier math.  
- **Fix:** Track Player base stats separately OR store original modifier when effect applied.

---

## Low Severity (Polish / Technical Debt)

**#10 — AbilityManager cooldown underflow**  
- **File:** Systems/AbilityManager.cs:93  
- **Issue:** TickCooldowns decrements without floor check; cooldown can become negative (harmless but incorrect state).  
- **Fix:** Line 93: `if (_cooldowns[key] > 0) _cooldowns[key]--;`

**#14 — Room.Looted dead code**  
- **File:** Models/Room.cs:50  
- **Issue:** Looted property exists but never set or checked anywhere in codebase.  
- **Fix:** Remove dead code OR implement in GameLoop.HandleTake.

**#15 — Player.OnHealthChanged unused**  
- **File:** Models/Player.cs:114  
- **Issue:** Event defined and fired on HP changes but no subscribers anywhere.  
- **Fix:** Remove OR document as reserved for future use (achievements, UI updates).

**#16 — LootTable static item pools**  
- **File:** Models/LootTable.cs:16  
- **Issue:** Tier1Items/Tier2Items/Tier3Items are static List<Item>; if item mutated after drop, affects all future drops (unlikely but possible).  
- **Fix:** Clone items on drop: `dropped = CloneItem(pool[_rng.Next(pool.Count)])`

---

## Architecture Patterns Identified

**Missing Integration:** StatusEffectManager and CombatEngine loosely coupled; stat modifiers calculated but never consumed in damage formulas.

**State Integrity Risk:** DungeonBoss mutable state (_baseAttack, IsEnraged, IsCharging) not persisted through save/load; boss mechanics fragile across sessions.

**Incomplete Abstractions:** Player.ModifyAttack/ModifyDefense used for both permanent (equipment) and temporary (shrine) modifications without tracking duration or source.

**Seed Determinism Broken:** Multi-floor progression creates new DungeonGenerator instances with same seed; identical layouts undermine replay value of seeded runs.

---

## Recommendations for v3

1. **Pre-Wave 1 Blockers:** Fix Critical bugs (#2, #3, #6) and High severity bugs (#1, #4, #11, #12) before refactoring Player.cs decomposition.
2. **SaveSystem Versioning:** Add SaveFormatVersion field to GameState; implement migration logic for IsEnraged, CurrentFloor schema changes.
3. **StatusEffect Integration:** Complete StatusEffectManager ↔ CombatEngine integration; write integration tests for Weakened/Fortified/Poison multi-turn scenarios.
4. **Boss Mechanics Hardening:** DungeonBoss needs immutable _baseAttack + serializable phase flags; consider extracting to BossPhaseManager component.
5. **Player Stat Separation:** Split base stats (Level-derived) from modified stats (Equipment + Buffs) to support temporary effects correctly; required for Weakened/Shrine mechanics.

---

**Outcome:** 16 bugs documented. Critical path blockers (#2, #3, #6) must be resolved before v3 Wave 1. Integration bugs (#4, #5, #8) indicate StatusEffectManager and boss mechanics need architectural hardening before multi-floor and class system work begins.

---

### 2026-02-20: Encapsulation Audit Findings — Player vs Enemy/Room Patterns

**By:** Hill
**Context:** Pre-v3 bug hunt revealed inconsistent encapsulation patterns across domain models.

**What:** Player model follows strong encapsulation (private setters + validation methods), but Enemy and Room models expose mutable state via public setters and direct property access throughout the codebase.

**Findings:**

**Player Model (STRONG):**
- Private setters on all stats (HP, Attack, Defense, Gold, XP, Level)
- Public methods with validation: TakeDamage(), Heal(), ModifyAttack(), ModifyDefense(), LevelUp()
- Math.Clamp and ArgumentException guards prevent invalid state
- OnHealthChanged event for state observation
- Equipment slots follow same pattern (private setters, EquipItem/UnequipItem methods)

**Enemy Model (WEAK):**
- Public setters on HP, Attack, Defense, MaxHP
- Direct HP mutations in 3+ locations (CombatEngine:255, AbilityManager:143, StatusEffectManager:57/61/65)
- No validation guards (allows negative HP)
- No death event, no TakeDamage method
- IsElite/IsAmbush have public setters (exploit: disable elite mid-combat)

**Room Model (WEAK):**
- Public setters on Visited, Looted, ShrineUsed
- Direct property mutations in GameLoop (6+ call sites)
- No encapsulation methods (should be MarkVisited(), MarkLooted())

**Inventory Pattern (HYBRID):**
- Player.Inventory is List<Item> with private setter
- But external code calls player.Inventory.Add/Remove directly (CombatEngine:336, GameLoop:298/337)
- Bypasses future validation (weight limits, quest triggers, capacity checks)

**Why This Matters:**

1. **Inconsistent Mental Model:** Player requires methods, Enemy allows direct mutation. New contributors will be confused.
2. **Future Refactoring Cost:** Adding Enemy.TakeDamage requires finding 5+ call sites and migrating each.
3. **Bug Surface Area:** Direct mutations skip validation, allow invalid state (negative HP, visited=false after entry).
4. **Event-Driven Arch Blocked:** No way to observe enemy death, room state changes for analytics/achievements.

**Recommended Pattern (Standardize):**

Apply Player encapsulation pattern universally:
- Private setters on all mutable state
- Public methods with validation for state changes
- Events for observation (OnDeath, OnVisited, OnItemAdded)
- Guard clauses (Math.Max, ArgumentException) at boundaries

**Specific Refactors Needed:**

1. **Enemy.TakeDamage(int amount)** — Replace 5+ direct HP mutations
2. **Enemy.Heal(int amount)** — For lifesteal, regen (currently raw HP += heal)
3. **Room.MarkVisited()** — Replace 3+ .Visited = true assignments
4. **Room.MarkLooted()** — Replace .Looted = true
5. **Player.AddItem(Item), RemoveItem(Item)** — Replace Inventory.Add/Remove (3 call sites)
6. **Enemy.IsElite / IsAmbush** — Change to init-only or constructor-only (prevent runtime exploit)

**Impact:**
- v3 class/trait systems need consistent encapsulation for stat application
- Equipment sets require inventory validation hooks
- Elite variants need IsElite immutability
- Estimated effort: 4-6 hours (Enemy refactor 2h, Room 1h, Inventory 1h, testing 2h)

**Decision:**
Standardize on Player encapsulation pattern before v3 Wave 1. Apply to Enemy, Room, and inventory in order of priority (Enemy highest — blocks combat features).

---

### 2026-02-20: Pre-v3 Critical Bug Hunt Findings

**By:** Barton (Systems Dev)  
**Requested by:** Copilot  
**Context:** Pre-release bug sweep of combat systems before v3 development begins

## What We Found

Conducted comprehensive review of Engine/CombatEngine.cs, Engine/EnemyFactory.cs, Engine/DungeonGenerator.cs, Systems/StatusEffectManager.cs, and all enemy implementations. **Identified 14 bugs** across combat logic, status effects, boss mechanics, and enemy spawning.

## Critical Issues Blocking v3

### 1. Status Effect Stat Modifiers Never Applied (CRITICAL)
**Impact:** Fortified and Weakened effects have ZERO gameplay effect  
**Root Cause:** `StatusEffectManager.GetStatModifier()` implemented but never called in damage calculations  
**Location:** CombatEngine.cs lines 248, 294  
**Blocks:** All status effect balance, Defensive Stance ability useless, future Weakened debuffs broken  

**Fix Required:**
```csharp
// Integrate GetStatModifier() into damage formulas
var playerDmg = Math.Max(1, 
    (player.Attack + _statusEffects.GetStatModifier(player, "Attack")) - 
    (enemy.Defense + _statusEffects.GetStatModifier(enemy, "Defense")));
```

### 2. GoblinShaman Poison-on-Hit Inverted (CRITICAL)
**Impact:** Player poisons THEMSELVES when attacking Shaman, not when Shaman hits player  
**Root Cause:** Poison logic in PerformPlayerAttack() instead of PerformEnemyTurn()  
**Location:** CombatEngine.cs lines 259-260  
**Blocks:** GoblinShaman enemy design completely broken, never tested properly  

**Fix Required:** Move poison application to PerformEnemyTurn() after enemy damage dealt

### 3. Half Enemy Roster Inaccessible (HIGH)
**Impact:** 5 of 9 enemy types never spawn (GoblinShaman, StoneGolem, Wraith, VampireLord, Mimic)  
**Root Cause:** DungeonGenerator spawns only original 4 types (Goblin, Skeleton, Troll, DarkKnight)  
**Location:** DungeonGenerator.cs line 114  
**Blocks:** All v2 enemy content untested in actual gameplay, balance data incomplete  

**Fix Required:** Update enemy spawn list to include all 9 types OR use EnemyFactory.CreateRandom()

## High-Severity Issues

### 4. Stun Logic Double-Handled (HIGH)
StatusEffectManager and CombatEngine both handle stun with fragile coupling. Unclear responsibility split.

### 5. Boss Enrage Multiplier Compounds (HIGH)
Boss enrage multiplies current Attack instead of base Attack. If boss heals or status clears, re-enrage applies 2.25x instead of 1.5x.

## Why This Matters

1. **Status effects system is core v2 feature** — Fortified/Weakened being non-functional means 2 of 6 effects (33%) are broken
2. **GoblinShaman never worked** — Shipped in v2 with inverted mechanic, never tested in real gameplay
3. **Content waste** — Half the enemy roster (5 of 9 types) inaccessible, development time wasted
4. **v3 planning affected** — Boss mechanics fragile (charge sticking, enrage timing), need fixes before building more bosses

## Recommended Actions Before v3

1. **Immediate:** Fix Bug #1 (stat modifiers) and Bug #2 (poison-on-hit) — both block status effect gameplay
2. **High Priority:** Fix Bug #3 (enemy spawning) — makes 5 enemy types actually playable
3. **Medium Priority:** Fix boss mechanics bugs (#6 charge sticking, #7 enrage timing) — prevents v3 boss variety work from inheriting same bugs
4. **Low Priority:** Documentation/polish issues (crit chance mismatch, dead code removal)

## Testing Strategy Post-Fix

- **Status Effects:** Test all 6 types with damage calculations (verify Fortified reduces damage, Weakened reduces enemy damage dealt)
- **GoblinShaman:** Verify poison applies when Shaman hits player, not when player hits Shaman
- **Enemy Spawning:** Generate 10 dungeons, verify all 9 types appear
- **Boss Mechanics:** Test enrage triggers at 40% HP immediately, charge resets correctly after dodge/hit

## Full Report

Complete bug list with reproduction steps, suggested fixes, and severity ratings: `.ai-team/agents/barton/bug-report-v3-pre-release.md`

---

**Decision:** Flag these 3 critical bugs for immediate resolution before starting v3 feature work. Boss variety and environmental hazards will inherit these fragilities if not fixed first.

---

### 2026-02-20: Systems/ Pre-v3 Bug Hunt — 7 Bugs Found

**By:** Romanoff  
**Scope:** SaveSystem state corruption, AchievementSystem double-unlock, RunStats fields never set, config loading failures

## Critical Issues

### BUG-1: SaveSystem.ListSaves() — Incorrect OrderByDescending Sort (Critical)
**File:** Systems/SaveSystem.cs:146  
**Severity:** Critical  
**Description:** `OrderByDescending(File.GetLastWriteTime)` sorts the filename strings, not the file timestamps. This is a LINQ usage error — the lambda parameter is the filename string, not the file path.

**Reproduction:**
1. Create save files: `save1.json` (older), `save2.json` (newer)
2. Call `SaveSystem.ListSaves()`
3. Expected: `["save2", "save1"]` (newest first)
4. Actual: Lexicographic sort by filename descending: `["save2", "save1"]` (accidental correctness in some cases)

**Impact:** Save file list will be mis-sorted if filenames don't alphabetically match write-time order. User expects "most recent save first" but gets arbitrary filename ordering.

**Fix:**
```csharp
return Directory.GetFiles(SaveDirectory, "*.json")
    .OrderByDescending(File.GetLastWriteTime)  // ❌ sorts string, not timestamp
    .Select(Path.GetFileNameWithoutExtension)
    .ToArray()!;
```

Should be:
```csharp
return Directory.GetFiles(SaveDirectory, "*.json")
    .OrderByDescending(File.GetLastWriteTime)  // ✅ sorts by file timestamp
    .Select(Path.GetFileNameWithoutExtension)
    .ToArray()!;
```

**Root Cause:** `OrderByDescending` is applied to the filename string before `GetLastWriteTime` can be called on the path. Need to pass the full path to the sort lambda.

---

### BUG-2: SaveSystem.LoadGame() — No Validation of Deserialized Player State (High)
**File:** Systems/SaveSystem.cs:79-132  
**Severity:** High  
**Description:** `LoadGame()` deserializes Player state from JSON without validating bounds: HP can exceed MaxHP, negative stats allowed, Level can be 0 or negative. Corrupted save files or manual JSON edits bypass all Player encapsulation invariants.

**Reproduction:**
1. Manually edit save file: set `"HP": 999`, `"MaxHP": 100`
2. Load game
3. Player now has 999/100 HP (impossible state)

**Impact:** Save file corruption or exploit editing can cause:
- HP > MaxHP display glitches
- Negative stats breaking combat math (divide-by-zero, underflow)
- Level 0 players unable to access abilities
- Achievement "Glass Cannon" exploit (set HP=1, MaxHP=1000, win with "HP < 10")

**Fix:** Add validation after deserialization in `LoadGame()`:
```csharp
var player = saveData.Player;
// Validate player state
if (player.HP < 0 || player.HP > player.MaxHP)
    throw new InvalidDataException($"Save file has invalid HP: {player.HP}/{player.MaxHP}");
if (player.MaxHP <= 0 || player.Attack <= 0 || player.Level < 1)
    throw new InvalidDataException("Save file has invalid player stats");
if (player.Mana < 0 || player.Mana > player.MaxMana || player.MaxMana < 0)
    throw new InvalidDataException("Save file has invalid mana values");
if (player.Gold < 0 || player.XP < 0)
    throw new InvalidDataException("Save file has negative Gold or XP");
```

**Pattern:** Defensive deserialization — always validate external data before trusting it.

---

### BUG-4: RunStats — DamageDealt and DamageTaken Fields Never Updated (Critical)
**File:** Systems/RunStats.cs:18, 21  
**Related Files:** Engine/CombatEngine.cs, Engine/GameLoop.cs  
**Severity:** Critical  
**Description:** `RunStats.DamageDealt` and `RunStats.DamageTaken` are declared public fields but NEVER incremented during combat. CombatEngine deals/receives damage but doesn't track it in RunStats. This breaks:
- "Untouchable" achievement (requires `DamageTaken == 0`) — **always triggers** because DamageTaken defaults to 0
- End-of-run stat display shows `Damage Dealt: 0` and `Damage Taken: 0` even after full combat runs

**Reproduction:**
1. Start new game
2. Fight and kill 5 enemies (take 50 damage, deal 200 damage)
3. Win game
4. Check `_stats.DamageDealt` and `_stats.DamageTaken`
5. Expected: `DamageDealt = 200`, `DamageTaken = 50`
6. Actual: `DamageDealt = 0`, `DamageTaken = 0`

**Impact:**
- **Achievement exploit:** "Untouchable" achievement ("Win without taking damage") always unlocks because `DamageTaken == 0` by default
- **Stat display inaccuracy:** Players see "Damage Taken: 0" even if they nearly died
- **Analytics broken:** Historical stats-history.json file has garbage damage data

**Root Cause:** CombatEngine.cs:255 and :310 deal damage via `enemy.HP -= playerDmg` and `player.TakeDamage(enemyDmg)` but never call `_stats.DamageDealt += playerDmg` or `_stats.DamageTaken += enemyDmg`. GameLoop doesn't have access to per-turn damage values to aggregate them.

**Fix:** CombatEngine needs to receive RunStats reference and track damage:
1. Add `RunStats? stats` parameter to CombatEngine constructor
2. In `PerformPlayerAttack()` (line 255 area): `if (_stats != null) _stats.DamageDealt += playerDmg;`
3. In `PerformEnemyTurn()` (line 310 area): `if (_stats != null) _stats.DamageTaken += enemyDmg;`
4. GameLoop.Run() must pass `_stats` to CombatEngine constructor

**Alternative Fix (less invasive):** Add damage tracking to Player.TakeDamage() via event subscription in GameLoop — subscribe to `player.OnHealthChanged`, calculate damage deltas, aggregate in RunStats. But this misses enemy damage dealt by player.

**Recommended Fix:** CombatEngine must own damage tracking. Inject RunStats into CombatEngine.

---

## High-Priority Issues

### BUG-5: ItemConfig.Load() and EnemyConfig.Load() — Directory.CreateDirectory Never Called (Medium)
**Files:** Systems/ItemConfig.cs:64-108, Systems/EnemyConfig.cs:55-107  
**Severity:** Medium  
**Description:** Both config loaders check `if (!File.Exists(path))` but never create parent directories. If loading from a path in a non-existent directory, `File.ReadAllText()` throws `DirectoryNotFoundException` instead of the expected `FileNotFoundException`.

**Reproduction:**
1. Delete `Data/` directory
2. Call `ItemConfig.Load("Data/items.json")`
3. Expected: `FileNotFoundException("Item config file not found: Data/items.json")`
4. Actual: `DirectoryNotFoundException` (uncaught, crashes game)

**Impact:** Misleading error messages if config directory is missing. SaveSystem handles this correctly (line 27-32: `Directory.CreateDirectory(SaveDirectory)`), but config loaders don't.

**Fix:** Either:
- Add directory existence check + clearer error message
- Or wrap `File.ReadAllText()` in try-catch for `DirectoryNotFoundException`

**Recommended:** Keep it simple — config files are shipped with the game, directory should exist. Improve error message:
```csharp
if (!File.Exists(path))
{
    throw new FileNotFoundException($"Item config file not found: {path}. Ensure the Data/ directory exists.");
}
```

---

### BUG-7: StatusEffectManager.ProcessTurnStart() — No Check for Dead Entities (Medium)
**File:** Systems/StatusEffectManager.cs:46-79  
**Severity:** Medium  
**Description:** `ProcessTurnStart()` applies damage/healing without checking if the entity is already dead (`HP <= 0`). If poison/bleed damage kills an entity, the effect still decrements duration and shows "effect wore off" message on a corpse.

**Reproduction:**
1. Apply Poison (3 damage/turn, 3 turns) to enemy with 5 HP
2. Turn 1: Enemy takes 3 poison damage (2 HP left)
3. Turn 2: Enemy takes 3 poison damage (HP = -1, dead)
4. Expected: Combat ends, status effects cleared
5. Actual: (If ProcessTurnStart is called again) "The enemy's Poison effect has worn off" message on dead enemy

**Impact:** 
- Cosmetic bug: death messages appear after entity is already dead
- Potential crash: if CombatEngine doesn't check death between status effect damage and next action, game could allow a dead enemy to attack
- Defensive coding violation: StatusEffectManager should not mutate dead entities

**Fix:** Add death check in ProcessTurnStart():
```csharp
public void ProcessTurnStart(object target)
{
    if (!_activeEffects.ContainsKey(target)) return;
    
    // Don't process effects on dead entities
    if (target is Player p && p.HP <= 0) return;
    if (target is Enemy e && e.HP <= 0) return;
    
    var effects = _activeEffects[target];
    // ... rest of method
}
```

**Note:** This assumes CombatEngine checks for death after each status effect tick. Need to verify CombatEngine.RunCombat() flow.

---

## Summary

**7 Total Bugs:**
- **Critical (2):** BUG-1 (SaveSystem sort), BUG-4 (RunStats damage tracking)
- **High (1):** BUG-2 (SaveSystem validation)
- **Medium (2):** BUG-5 (config directory handling), BUG-7 (status effects on dead entities)
- **Low (1):** BUG-6 (redundant .ToList())
- **Retracted (1):** BUG-3 (false alarm — AchievementSystem is correct)

**Blocking Issues for v3:**
- BUG-4: "Untouchable" achievement exploit (always unlocks)
- BUG-2: Save file corruption risk (HP > MaxHP, negative stats)
- BUG-1: Save file UI shows wrong "most recent" order

**Recommendations:**
1. Fix BUG-4 first (inject RunStats into CombatEngine) — blocks achievement system integrity
2. Fix BUG-2 (add SaveSystem validation) — prevents save corruption exploits
3. Fix BUG-1 (correct OrderByDescending usage) — quality-of-life fix
4. Address BUG-5 and BUG-7 as cleanup — non-blocking but good defensive coding

**Test Coverage Required:**
- SaveSystem round-trip test: save → corrupt JSON (HP>MaxHP, negative stats) → load → expect InvalidDataException
- RunStats integration test: fight enemy → verify DamageDealt and DamageTaken incremented
- SaveSystem.ListSaves() test: create files with out-of-order write times → verify sort by timestamp, not filename
- StatusEffectManager test: apply poison to 1 HP enemy → verify no "effect wore off" message on corpse

