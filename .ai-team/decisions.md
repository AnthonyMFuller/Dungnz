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


---

### 2026-02-22: No commits directly to master
**By:** Anthony (via Copilot coordinator)
**What:** All work — whether triggered by a GitHub issue or an open-ended request — must be done on a feature branch. No commits may land directly on master. Branches should follow the naming convention squad/{slug} or squad/{issue-number}-{slug}. Work reaches master only via PR review and merge.
**Why:** The squad committed UI/UX implementation work directly to master during a session where no GitHub issue was present, bypassing the branch/PR workflow.

---

### 2026-02-22: PR #218 code review verdict
**By:** Coulson
**What:** ✅ APPROVED — Color system is well-structured, architecturally sound, and ready to merge.
**Why:** The PR respects all charter principles and team decisions. Notes below for follow-up.

---

## Review Summary

**Branch:** `squad/ui-ux-color-system`
**Files changed:** 8 code files + README + team docs
**Tests:** All 267 pass ✅
**Build:** Clean (0 errors, pre-existing warnings only)

## Architecture Assessment

| Principle | Status | Notes |
|-----------|--------|-------|
| Display layer separation | ✅ Pass | All Console calls confined to `ConsoleDisplayService`. CombatEngine and EquipmentManager use `IDisplayService` only. |
| Idiomatic C# / clean interfaces | ✅ Pass | `IDisplayService` extended with 4 well-documented methods. `ColorCodes` is a clean static utility class with `const` fields. |
| No Console leaks in game logic | ✅ Pass | Zero `Console.*` calls in `CombatEngine.cs` or `EquipmentManager.cs`. |
| ANSI stripped in tests | ✅ Pass | Both `FakeDisplayService` and `TestDisplayService` use `ColorCodes.StripAnsiCodes()` on all text inputs. |
| Console-first, no external deps | ✅ Pass | Pure ANSI escape codes, no third-party packages. |

## What's Good

1. **`ColorCodes.cs`** — Clean, well-documented static utility. Threshold-based helpers (`HealthColor`, `ManaColor`, `WeightColor`) are a smart pattern. `StripAnsiCodes` regex is correct and reusable.
2. **Test doubles updated correctly** — Both fake services strip ANSI on all text-accepting methods, preventing false failures.
3. **CombatEngine stays clean** — `ColorizeDamage` is a private helper that only manipulates strings; all output still goes through `_display`.
4. **EquipmentManager** — Comparison panel shown via `IDisplayService.ShowEquipmentComparison`, not raw Console. Good.

## Minor Issues (Non-blocking, follow-up recommended)

### 1. README health thresholds don't match code
README says: `≥ 60% Green, 30–59% Yellow, < 30% Red`
Code (`HealthColor`): `> 70% Green, > 40% Yellow, > 20% Red, ≤ 20% BrightRed`
**Action:** Update README table to match actual thresholds. Also, README omits the 4th tier (BrightRed < 20%).

### 2. `ColorizeDamage` uses naive `string.Replace`
If the damage number (e.g. `"5"`) appears elsewhere in the narration message (e.g. "5 goblins"), it will colorize the wrong occurrence. Low probability given current narration templates, but fragile.
**Action:** Consider replacing only the last occurrence, or passing the damage value separately to the display layer.

### 3. `ShowEquipmentComparison` box-drawing alignment
The right `║` border uses `{"",20}` fixed padding, but ANSI color codes have zero visible width and non-zero string length. When deltas are present (colored `(+X)`), the right border shifts right by ~12 invisible characters, breaking the box alignment.
**Action:** Calculate visible string width excluding ANSI codes, or pad after stripping.

### 4. `ShowColoredStat` added but unused
The method is on `IDisplayService` and implemented in all three services, but `ShowPlayerStats()` in `DisplayService` uses inline `ColorCodes` references instead of calling its own `ShowColoredStat`. Either use it or defer adding it until needed (YAGNI).
**Action:** Refactor `ShowPlayerStats` to use `ShowColoredStat`, or remove the method from the interface until a consumer exists.

## Verdict

**✅ APPROVED** — Merge as-is. The four minor issues above are cosmetic/documentation and can be addressed in a follow-up PR. The architecture is correct, the layer boundaries are respected, and all tests pass.

# UI/UX Improvement Plan — TextGame v3.5

**Date:** 2026-02-20  
**Lead:** Coulson  
**Context:** Boss-requested initiative to enhance visual clarity and player experience through color, layout, and feedback improvements.

---

## Executive Summary

TextGame currently uses plain text output with Unicode box-drawing characters and emoji for visual distinction. **No color system exists.** The codebase has clean architectural separation (IDisplayService abstraction) that enables UI improvements without touching game logic.

**Proposal:** Implement ANSI color system, enhance visual hierarchy, improve player feedback, and add real-time status tracking—all via DisplayService extensions. No breaking changes to architecture.

**Estimated Scope:** 15-20 hours across 3 phases (Foundation → Enhancement → Polish)

---

## Current State Analysis

### Architecture Strengths
✅ **IDisplayService abstraction** — All display calls routed through interface  
✅ **Clean separation** — Game logic never calls Console directly  
✅ **Single implementation** — ConsoleDisplayService is sole concrete class  
✅ **Test infrastructure** — TestDisplayService exists for headless testing  
✅ **Consistent patterns** — Emoji prefixes, indentation, box-drawing established  

### Current Display Features
- **Text formatting:** Unicode box-drawing (`╔ ║ ═ ╚`), emoji (⚔ 🏛 💧 ✗), indentation
- **Layout patterns:** Blank lines for spacing, bracketed comparisons `[You: X/Y]`, comma-separated lists
- **Visual hierarchy:** Headers with `═══`, indented messages (2 spaces), emoji prefixes for categories

### Critical Gaps
❌ **No color system** — All text plain white  
❌ **No status HUD** — Active effects only shown when applied/expired  
❌ **No equipment comparison** — Equipping gear doesn't show stat delta  
❌ **No progress tracking** — Achievements/unlocks binary only  
❌ **No inventory weight display** — Weight system exists but not visualized  
❌ **Limited combat clarity** — Damage/heals blend into narrative walls  

---

## Design Philosophy

### Core Principles
1. **Console-native aesthetics** — Leverage ANSI colors, box-drawing, and emoji (no external frameworks)
2. **Accessibility first** — Color must enhance, not replace, existing semantic indicators (emoji, labels)
3. **Information density** — Reduce clutter; prioritize actionable info over flavor text
4. **Consistency** — Establish color palette and apply uniformly across all systems
5. **No breaking changes** — All improvements via DisplayService extensions; game logic untouched

### Color Philosophy
- **Color as semantic layer** — HP=red, Mana=blue, gold=yellow, XP=green, errors=red
- **State-based coloring** — Low HP warnings, cooldown readiness, effect durations
- **Context-aware intensity** — Combat uses bold/bright colors; exploration uses muted tones
- **Graceful degradation** — If ANSI unsupported, fall back to current emoji-only design

---

## Proposed Color System

### Color Palette (ANSI Codes)

| Category | Color | ANSI Code | Use Cases |
|----------|-------|-----------|-----------|
| **Health** | Red | `\u001b[31m` | HP values, damage messages |
| **Mana** | Blue | `\u001b[34m` | Mana values, ability costs |
| **Gold** | Yellow | `\u001b[33m` | Gold amounts, loot rewards |
| **XP** | Green | `\u001b[32m` | XP rewards, level-ups |
| **Attack** | Bright Red | `\u001b[91m` | Attack stat, power buffs |
| **Defense** | Cyan | `\u001b[36m` | Defense stat, shields |
| **Success** | Green | `\u001b[32m` | Confirmations, heals |
| **Errors** | Red | `\u001b[31m` | Warnings, failures |
| **Neutral** | White | `\u001b[37m` | Default text |
| **Dim** | Gray | `\u001b[90m` | Cooldowns, disabled options |
| **Highlight** | Bright White | `\u001b[97m` | Important values, headers |
| **Reset** | — | `\u001b[0m` | End colored segments |

### State-Based Colors

**HP Thresholds:**
- `100%-70%` → Green
- `69%-40%` → Yellow
- `39%-20%` → Red
- `19%-0%` → Bright Red (flashing if possible)

**Mana Thresholds:**
- `100%-50%` → Blue
- `49%-20%` → Cyan
- `19%-0%` → Gray (depleted)

**Status Effects:**
- Positive (Regen, Fortified) → Green text
- Negative (Poison, Weakened) → Red text
- Neutral (Stun, Bleed) → Yellow text

**Equipment Quality:**
- Common → White
- Uncommon → Green
- Rare → Blue
- Epic → Purple (`\u001b[35m`)
- Legendary → Gold (bright yellow `\u001b[93m`)

---

## Improvement Roadmap

### Phase 1: Foundation (Color System Core)
**Priority:** HIGH  
**Estimated Time:** 5-7 hours  
**Dependencies:** None

#### Work Items

**WI-1: Color Utility Class**
- Create `Systems/ColorCodes.cs` with ANSI code constants
- Add `Colorize(string text, ColorCode color)` helper
- Add `HealthColor(int current, int max)` threshold logic
- Add `ManaColor(int current, int max)` threshold logic

**WI-2: DisplayService Color Methods**
- Add `ShowColoredMessage(string message, ColorCode color)` to IDisplayService
- Add `ShowColoredCombatMessage(string message, ColorCode color)`
- Add `ShowColoredStat(string label, string value, ColorCode valueColor)`
- Update ConsoleDisplayService implementation
- Update TestDisplayService to strip ANSI codes for assertions

**WI-3: Core Stat Colorization**
- Update `ShowPlayerStats()` to colorize HP (red), Mana (blue), Gold (yellow), XP (green), Attack (bright red), Defense (cyan)
- Update `ShowCombatStatus()` to apply HP threshold colors to both player and enemy
- Update damage messages in CombatEngine to colorize damage values (red)

#### Acceptance Criteria
- [ ] ANSI color codes constants defined and documented
- [ ] DisplayService has 3 new color-aware methods
- [ ] Player stats display uses semantic colors
- [ ] Combat HP bars change color based on threshold
- [ ] All 125+ existing tests still pass (TestDisplayService strips colors)

---

### Phase 2: Enhancement (Visual Hierarchy & Feedback)
**Priority:** HIGH  
**Estimated Time:** 6-8 hours  
**Dependencies:** Phase 1 complete

#### Work Items

**WI-4: Combat Visual Hierarchy**
- Color damage numbers red with bright highlight
- Color healing green with bright highlight
- Color critical hits with `💥` emoji + bright yellow text
- Color ability names blue in usage messages
- Add colored status effect indicators: `[P]oison` (red), `[R]egen` (green), `[S]tun` (yellow)

**WI-5: Enhanced Combat Status HUD**
- Redesign `ShowCombatStatus()` to show active effects inline:
  ```
  [You: 45/60 HP | 15/30 MP | P(2) R(3)] vs [Goblin: 12/30 HP | W(2)]
  ```
- Color effect abbreviations based on type (positive/negative/neutral)
- Add mana display to HUD (currently only shown in ability menu)

**WI-6: Equipment Comparison Display**
- Add `ShowEquipmentComparison(Item old, Item new)` method to IDisplayService
- Display before/after stats when equipping:
  ```
  Equipping: Iron Sword
  ────────────────────────
  Before: Attack: 10, Defense: 5
  After:  Attack: 15, Defense: 5  [+5 ATK]
  ────────────────────────
  ```
- Color stat deltas: green for increases, red for decreases

**WI-7: Inventory Weight Display**
- Update `ShowInventory()` to include weight header:
  ```
  ═══ INVENTORY ═══
  Slots: 5/8 | Weight: 42/50 | Value: 320g
  ```
- Add weight value to each item line: `• Potion (Consumable) [2 wt]`
- Color weight ratio: green (under 80%), yellow (80-95%), red (96-100%)

**WI-8: Status Effect Summary Panel**
- Add `ShowActiveEffects(Player player)` to IDisplayService
- Display in `ShowPlayerStats()` below main stats:
  ```
  Active Effects:
    Poison (2 turns) - Taking 3 damage per turn
    Regen (3 turns) - Healing 4 HP per turn
  ```
- Color effect names based on type

#### Acceptance Criteria
- [ ] Combat damage/healing uses color highlights
- [ ] Combat HUD shows active effects inline with colored abbreviations
- [ ] Equipment comparison shows stat deltas when equipping
- [ ] Inventory displays weight/value summary with threshold colors
- [ ] Player stats shows active effects panel
- [ ] All tests pass

---

### Phase 3: Polish (Advanced UX Features)
**Priority:** MEDIUM  
**Estimated Time:** 4-5 hours  
**Dependencies:** Phase 2 complete

#### Work Items

**WI-9: Achievement Progress Tracking**
- Add `ShowAchievementProgress(List<Achievement> locked)` to IDisplayService
- On game end, show locked achievements with progress:
  ```
  ❌ Speed Runner: 142 turns (need <100) — 71% progress
  ❌ Hoarder: 320g / 500g — 64% progress
  ✅ Glass Cannon: UNLOCKED
  ```
- Color progress bars: green (>75%), yellow (50-75%), red (<50%)

**WI-10: Enhanced Room Descriptions**
- Color room type prefixes based on danger level:
  - Safe (standard/mossy/ancient) → green/cyan
  - Hazardous (dark/flooded/scorched) → yellow/red
- Color enemy warnings bright red with bold text
- Color item drops gold

**WI-11: Ability Cooldown Visual**
- Update ability menu to show cooldown status with color:
  ```
  [1] Power Strike (10 MP, ready) ← green
  [2] Defensive Stance (8 MP, 2 turns) ← gray
  ```
- Bold + bright color for ready abilities
- Dim gray for cooling down abilities

**WI-12: Combat Turn Log Enhancement**
- Limit turn log to last 5 turns (currently unbounded)
- Color player actions green, enemy actions red
- Indent alternating turns for visual rhythm
- Add turn numbers: `Turn 3: You strike Goblin for 15 damage`

#### Acceptance Criteria
- [ ] Achievement progress tracked and displayed on game end
- [ ] Room descriptions use danger-based color coding
- [ ] Ability menu shows cooldown readiness with color
- [ ] Combat log uses alternating colors for player/enemy actions
- [ ] All tests pass

---

## Technical Implementation Notes

### ANSI Color Utility (WI-1)

```csharp
namespace Dungnz.Systems;

/// <summary>
/// ANSI escape code constants and color formatting utilities for console output.
/// </summary>
public static class ColorCodes
{
    // Basic colors
    public const string Red = "\u001b[31m";
    public const string Green = "\u001b[32m";
    public const string Yellow = "\u001b[33m";
    public const string Blue = "\u001b[34m";
    public const string Magenta = "\u001b[35m";
    public const string Cyan = "\u001b[36m";
    public const string White = "\u001b[37m";
    
    // Bright colors
    public const string BrightRed = "\u001b[91m";
    public const string BrightGreen = "\u001b[92m";
    public const string BrightYellow = "\u001b[93m";
    public const string BrightWhite = "\u001b[97m";
    public const string Gray = "\u001b[90m";
    
    // Formatting
    public const string Bold = "\u001b[1m";
    public const string Reset = "\u001b[0m";
    
    /// <summary>
    /// Wraps text in ANSI color codes.
    /// </summary>
    public static string Colorize(string text, string color)
        => $"{color}{text}{Reset}";
    
    /// <summary>
    /// Returns threshold-based color for HP values.
    /// </summary>
    public static string HealthColor(int current, int max)
    {
        var ratio = (float)current / max;
        return ratio switch
        {
            >= 0.70f => Green,
            >= 0.40f => Yellow,
            >= 0.20f => Red,
            _ => BrightRed
        };
    }
    
    /// <summary>
    /// Returns threshold-based color for Mana values.
    /// </summary>
    public static string ManaColor(int current, int max)
    {
        var ratio = (float)current / max;
        return ratio switch
        {
            >= 0.50f => Blue,
            >= 0.20f => Cyan,
            _ => Gray
        };
    }
}
```

### Extended IDisplayService Methods (WI-2)

```csharp
/// <summary>
/// Displays a colored message to the player.
/// </summary>
void ShowColoredMessage(string message, string color);

/// <summary>
/// Displays a colored combat message with indentation.
/// </summary>
void ShowColoredCombatMessage(string message, string color);

/// <summary>
/// Displays a stat with colored value (e.g., "HP: 45/60" with 45 red).
/// </summary>
void ShowColoredStat(string label, string value, string valueColor);

/// <summary>
/// Displays equipment comparison when swapping gear.
/// </summary>
void ShowEquipmentComparison(Item? oldItem, Item newItem);

/// <summary>
/// Displays active status effects on player/enemy.
/// </summary>
void ShowActiveEffects(Player player);

/// <summary>
/// Displays achievement progress for locked achievements.
/// </summary>
void ShowAchievementProgress(List<Achievement> achievements);
```

### Combat Status HUD Example (WI-5)

**Current:**
```
[You: 45/60 HP] vs [Goblin: 12/30 HP]
```

**Proposed:**
```
[You: 45/60 HP | 15/30 MP | P(2) R(3)] vs [Goblin: 12/30 HP | W(2)]
```

With colors:
- HP values use threshold colors (green/yellow/red)
- Mana values use blue
- Effect abbreviations colored by type: P (poison, red), R (regen, green), W (weakened, yellow)
- Numbers in parentheses show turns remaining

### Equipment Comparison Example (WI-6)

**Before equipping Iron Sword:**
```
═══════════════════════════════
Equipping: Iron Sword
───────────────────────────────
Current Weapon: Rusty Dagger
  Attack: 10 → 15  (+5)  ← green
  Defense: 5 → 5   (—)   ← gray
═══════════════════════════════
Equip? [Y/N]
```

### Inventory Weight Display Example (WI-7)

**Current:**
```
═══ INVENTORY ═══
• Health Potion (Consumable)
• Iron Sword (Weapon)
```

**Proposed:**
```
═══ INVENTORY ═══
Slots: 5/8  |  Weight: 42/50  |  Value: 320g
────────────────────────────────────────────
• Health Potion (Consumable) [3 wt] [25g]
• Iron Sword (Weapon) [8 wt] [50g]
```

With colors:
- Weight ratio colored by threshold (green <80%, yellow 80-95%, red >95%)
- Gold values in yellow
- Item names colored by rarity

---

## Architecture Impact

### No Breaking Changes
- All improvements via DisplayService method additions
- Existing methods retain current behavior
- Game logic (CombatEngine, GameLoop, Systems) unchanged except display calls

### Testing Strategy
- Update TestDisplayService to strip ANSI codes before storing output
- Add `StripAnsiCodes(string text)` helper method
- All existing tests pass without modification (they check plain text content)
- Add new tests for color utility functions (`HealthColor`, `ManaColor`)

### Performance Considerations
- ANSI codes add ~10-20 bytes per colored segment (negligible)
- No performance impact on game logic (display is I/O-bound)
- Color utility calls are simple string concatenation (fast)

### Accessibility
- Color enhances existing semantic indicators (emoji, labels), never replaces
- `ShowError()` still prefixes with `✗` even when red
- Combat HUD still shows effect abbreviations even without color
- Equipment comparison shows deltas as text (`+5`) alongside color

---

## Priority Order & Dependencies

### Critical Path
1. **WI-1 (Color Utility)** → Foundation for all color work
2. **WI-2 (DisplayService Methods)** → Interface contracts for color display
3. **WI-3 (Core Stat Colorization)** → Immediate visual impact

### Parallel Work (After WI-3)
**Track A (Combat):**
- WI-4 (Combat Visual Hierarchy)
- WI-5 (Combat Status HUD)
- WI-12 (Turn Log Enhancement)

**Track B (Exploration):**
- WI-6 (Equipment Comparison)
- WI-7 (Inventory Weight Display)
- WI-10 (Room Descriptions)

**Track C (Meta):**
- WI-8 (Status Effect Panel)
- WI-9 (Achievement Progress)
- WI-11 (Ability Cooldown Visual)

### Dependency Graph
```
WI-1 (Color Utility)
  ↓
WI-2 (DisplayService Extensions)
  ↓
WI-3 (Core Stat Colors)
  ├─→ WI-4 (Combat Hierarchy)
  │    ↓
  │   WI-5 (Combat HUD)
  │    ↓
  │   WI-12 (Turn Log)
  │
  ├─→ WI-6 (Equipment Compare)
  │    ↓
  │   WI-7 (Inventory Weight)
  │    ↓
  │   WI-10 (Room Colors)
  │
  └─→ WI-8 (Status Panel)
       ↓
      WI-9 (Achievement Progress)
       ↓
      WI-11 (Ability Cooldown)
```

---

## Risk Assessment

### High Risk
❌ **ANSI code support variance** — Some terminals (older Windows CMD) may not support ANSI  
**Mitigation:** Add ANSI detection and graceful fallback to current emoji-only design

❌ **Test infrastructure breakage** — Color codes may break existing test assertions  
**Mitigation:** Update TestDisplayService to strip ANSI codes in Phase 1

### Medium Risk
⚠️ **Display method signature changes** — New methods require IDisplayService updates  
**Mitigation:** Add new methods (don't modify existing); backwards-compatible

⚠️ **Color readability** — Some color combinations may be hard to read on certain terminals  
**Mitigation:** Use high-contrast colors; test on multiple terminal emulators

### Low Risk
✅ **Performance impact** — ANSI codes are small strings; no measurable slowdown expected  
✅ **Architecture coupling** — DisplayService abstraction prevents leakage into game logic

---

## Success Metrics

### Phase 1 (Foundation)
- [ ] All stats in `ShowPlayerStats()` use semantic colors
- [ ] Combat HP bars change color based on health threshold
- [ ] Damage numbers highlighted in combat messages
- [ ] Zero test failures after color integration

### Phase 2 (Enhancement)
- [ ] Combat HUD shows active effects with colored abbreviations
- [ ] Equipment comparison displays before/after stats
- [ ] Inventory shows weight/value summary with threshold colors
- [ ] Status effect panel visible in player stats

### Phase 3 (Polish)
- [ ] Achievement progress tracked and displayed
- [ ] Room descriptions use danger-based coloring
- [ ] Ability cooldowns show readiness with color
- [ ] Combat turn log uses alternating player/enemy colors

### Overall Success
- **Visual clarity:** Player feedback improves (damage, healing, status changes immediately obvious)
- **Information density:** More data displayed without increasing clutter
- **Accessibility:** Color enhances existing indicators without replacing them
- **Stability:** All 267 tests pass; no regressions in game logic

---

## Team Allocation

**Hill (Lead Engineer):** 8-10 hours
- WI-1: Color Utility Class
- WI-2: DisplayService Extensions
- WI-3: Core Stat Colorization
- WI-6: Equipment Comparison
- WI-7: Inventory Weight Display

**Barton (Systems Engineer):** 7-9 hours
- WI-4: Combat Visual Hierarchy
- WI-5: Combat Status HUD
- WI-8: Status Effect Panel
- WI-11: Ability Cooldown Visual
- WI-12: Combat Turn Log Enhancement

**Romanoff (Tester):** 3-4 hours
- Update TestDisplayService ANSI stripping
- Verify all 267 tests pass across all phases
- Add color utility unit tests
- Manual testing on multiple terminal emulators

**Coulson (Architect):** 2-3 hours
- Design review before Phase 1 kickoff
- Code review after each phase
- Approval gate before Phase 3 (validate architecture decisions)

---

## Open Questions

1. **ANSI Detection:** Should we auto-detect terminal color support or require opt-in flag?
   - **Recommendation:** Auto-detect via `Environment.GetEnvironmentVariable("TERM")` and Windows version check
   
2. **Color Customization:** Should players be able to configure color theme?
   - **Recommendation:** Defer to v4; use hard-coded theme for v3.5

3. **Equipment Rarity Colors:** Should we add rarity system (common/rare/epic) now or later?
   - **Recommendation:** Add rarity enum + colors in Phase 2; populate rarities in Phase 3

4. **Combat Log Length:** Should turn log be limited to 5 turns or configurable?
   - **Recommendation:** Hard-code 5 turns; add config option in v4 if requested

5. **Status Effect Abbreviations:** What should abbreviation scheme be?
   - **Recommendation:** Single-letter where unambiguous (P=Poison, R=Regen, S=Stun, B=Bleed, F=Fortified, W=Weakened)

---

## Post-Implementation Review Criteria

After Phase 3 completion, evaluate:

- [ ] **Visual clarity improved?** — Can players instantly identify HP state, active effects, cooldowns?
- [ ] **Information density optimal?** — Is all actionable info visible without scrolling?
- [ ] **Accessibility maintained?** — Do color-blind players still have full experience via emoji/labels?
- [ ] **Zero regressions?** — All 267 tests pass; no gameplay bugs introduced?
- [ ] **Performance acceptable?** — No noticeable slowdown in display rendering?

If all criteria met: **Ship to master**  
If any criteria unmet: **Iterate or roll back**

---

## Future Considerations (v4+)

- **Animated effects** — ANSI cursor positioning for "live" HP bars
- **Color themes** — Multiple palettes (classic, solarized, high-contrast)
- **Advanced HUD** — Split-screen combat view with persistent stat panels
- **Sound effects** — Terminal bell for critical hits, level-ups (via `\a` escape)
- **Mouse support** — ANSI mouse tracking for menu selections

---

**Decision Authority:** Coulson (Lead)  
**Approval Status:** DRAFT — Awaiting team review and Boss approval  
**Next Steps:** Schedule design review ceremony with Hill, Barton, Romanoff

# UI/UX Implementation Checklist

**Quick reference for team during implementation**

---

## Phase 1: Foundation ✓ READY TO START

### WI-1: Color Utility Class
**Owner:** Hill  
**File:** `Systems/ColorCodes.cs`

- [ ] Create ColorCodes static class
- [ ] Add ANSI color constants (Red, Green, Yellow, Blue, Cyan, BrightRed, Gray, Bold, Reset)
- [ ] Add `Colorize(string text, string color)` helper
- [ ] Add `HealthColor(int current, int max)` with thresholds (>70% green, 40-70% yellow, 20-40% red, <20% bright red)
- [ ] Add `ManaColor(int current, int max)` with thresholds (>50% blue, 20-50% cyan, <20% gray)
- [ ] Add XML docs on all public members

**Test:** Create `ColorCodesTests.cs` with threshold boundary tests

---

### WI-2: DisplayService Color Methods
**Owner:** Hill  
**Files:** `Display/IDisplayService.cs`, `Display/ConsoleDisplayService.cs`, `Dungnz.Tests/Helpers/TestDisplayService.cs`

- [ ] Add `void ShowColoredMessage(string message, string color)` to IDisplayService
- [ ] Add `void ShowColoredCombatMessage(string message, string color)` to IDisplayService
- [ ] Add `void ShowColoredStat(string label, string value, string valueColor)` to IDisplayService
- [ ] Implement methods in ConsoleDisplayService
- [ ] Update TestDisplayService to strip ANSI codes (regex: `\u001b\[[0-9;]*m`)
- [ ] Add XML docs on new interface methods

**Test:** Verify all 267 existing tests still pass

---

### WI-3: Core Stat Colorization
**Owner:** Hill  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowPlayerStats()` to colorize:
  - HP value with `ColorCodes.HealthColor(player.HP, player.MaxHP)`
  - Mana value with `ColorCodes.ManaColor(player.Mana, player.MaxMana)`
  - Gold value with `ColorCodes.Yellow`
  - XP value with `ColorCodes.Green`
  - Attack value with `ColorCodes.BrightRed`
  - Defense value with `ColorCodes.Cyan`
- [ ] Update `ShowCombatStatus()` to colorize HP for both player and enemy
- [ ] Update combat damage messages in `Engine/CombatEngine.cs` to highlight damage values in red

**Test:** Manual verification + screenshot comparison

---

## Phase 2: Enhancement ⏸ BLOCKED BY PHASE 1

### WI-4: Combat Visual Hierarchy
**Owner:** Barton  
**File:** `Engine/CombatEngine.cs`

- [ ] Color damage numbers bright red in all attack messages
- [ ] Color healing numbers bright green in all heal messages
- [ ] Color critical hit messages bright yellow + bold
- [ ] Color ability names blue in usage messages
- [ ] Add colored status effect confirmation messages (poison=red, regen=green, stun=yellow)

---

### WI-5: Enhanced Combat Status HUD
**Owner:** Barton  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowCombatStatus()` format to: `[You: HP | MP | Effects] vs [Enemy: HP | Effects]`
- [ ] Add effect abbreviation logic: P(poison), R(regen), S(stun), B(bleed), F(fortified), W(weakened)
- [ ] Color abbreviations by type (positive=green, negative=red, neutral=yellow)
- [ ] Display turns remaining in parentheses

**Test:** Integration test with multiple active effects

---

### WI-6: Equipment Comparison Display
**Owner:** Hill  
**Files:** `Display/IDisplayService.cs`, `Display/ConsoleDisplayService.cs`, `Systems/EquipmentManager.cs`

- [ ] Add `void ShowEquipmentComparison(Item? oldItem, Item newItem)` to IDisplayService
- [ ] Implement method in ConsoleDisplayService with box border
- [ ] Show before/after stats with colored deltas (+X green, -X red, no change gray)
- [ ] Call from EquipmentManager.EquipItem() before applying changes
- [ ] Prompt user to confirm (optional enhancement)

---

### WI-7: Inventory Weight Display
**Owner:** Hill  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowInventory()` to add header line: `Slots: X/Y | Weight: X/Y | Value: Xg`
- [ ] Color weight ratio by threshold (<80% green, 80-95% yellow, >95% red)
- [ ] Add `[X wt]` suffix to each item line
- [ ] Add `[Xg]` value suffix to each item line

---

### WI-8: Status Effect Summary Panel
**Owner:** Barton  
**Files:** `Display/IDisplayService.cs`, `Display/ConsoleDisplayService.cs`

- [ ] Add `void ShowActiveEffects(Player player)` to IDisplayService
- [ ] Implement method showing effect name, turns remaining, and per-turn effect
- [ ] Color effect names by type
- [ ] Call from `ShowPlayerStats()` after main stats block

---

## Phase 3: Polish ⏸ BLOCKED BY PHASE 2

### WI-9: Achievement Progress Tracking
**Owner:** Barton  
**Files:** `Display/IDisplayService.cs`, `Systems/AchievementSystem.cs`

- [ ] Add `void ShowAchievementProgress(List<Achievement> achievements, RunStats stats)` to IDisplayService
- [ ] Implement showing locked achievements with progress percentage
- [ ] Color progress by threshold (>75% green, 50-75% yellow, <50% red)
- [ ] Add progress calculation methods to AchievementSystem

---

### WI-10: Enhanced Room Descriptions
**Owner:** Hill  
**File:** `Display/ConsoleDisplayService.cs`

- [ ] Update `ShowRoom()` to color room type prefix:
  - Safe types (standard, mossy, ancient) → cyan/green
  - Hazardous types (dark, flooded, scorched) → yellow/red
- [ ] Color enemy warning bright red + bold
- [ ] Color item names yellow

---

### WI-11: Ability Cooldown Visual
**Owner:** Barton  
**File:** `Engine/CombatEngine.cs` (ability menu display)

- [ ] Color ready abilities (cooldown=0, mana sufficient) green + bold
- [ ] Color cooling abilities (cooldown>0) gray
- [ ] Color insufficient mana abilities red
- [ ] Change text: "ready" instead of "CD: 0 turns"

---

### WI-12: Combat Turn Log Enhancement
**Owner:** Barton  
**File:** `Engine/CombatEngine.cs`

- [ ] Limit turn log to last 5 turns (add ring buffer or list truncation)
- [ ] Color player actions green
- [ ] Color enemy actions red
- [ ] Add turn numbers to each line
- [ ] Consider indentation for visual rhythm

---

## Testing Checklist (Romanoff)

### After Phase 1
- [ ] All 267 tests pass
- [ ] TestDisplayService strips ANSI codes correctly
- [ ] Manual test: Player stats show colored values
- [ ] Manual test: Combat HP bars change color at thresholds
- [ ] Manual test: Color codes work on Windows Terminal, macOS Terminal, Linux terminal

### After Phase 2
- [ ] All tests still pass
- [ ] Manual test: Combat HUD shows active effects
- [ ] Manual test: Equipment comparison displays correctly
- [ ] Manual test: Inventory shows weight/value summary
- [ ] Manual test: Status effect panel visible in stats

### After Phase 3
- [ ] All tests still pass
- [ ] Manual test: Achievement progress displays
- [ ] Manual test: Room descriptions use danger colors
- [ ] Manual test: Ability menu shows cooldown colors
- [ ] Manual test: Turn log limited to 5 entries
- [ ] Regression check: All gameplay systems work as before
- [ ] Performance check: No noticeable slowdown

---

## Code Review Gates (Coulson)

### Phase 1: Foundation (5-7 hours)
- ANSI color system core
- DisplayService color methods
- Core stat colorization (HP, Mana, Gold, XP, Attack, Defense)

### Phase 2: Enhancement (6-8 hours)
- Combat visual hierarchy (colored damage/healing/crits)
- Enhanced combat HUD with active effects
- Equipment comparison display
- Inventory weight tracking
- Status effect summary panel

### Phase 3: Polish (4-5 hours)
- Achievement progress tracking
- Enhanced room descriptions
- Ability cooldown visual indicators
- Combat turn log improvements

**Total Estimate:** 15-20 hours

---

## Key Improvements At-a-Glance

| Feature | Before | After |
|---------|--------|-------|
| **HP Display** | `45/60` (white) | `45/60` (green/yellow/red based on %) |
| **Combat Status** | `[You: 45/60 HP] vs [Goblin: 12/30 HP]` | `[You: 45/60 HP │ 15/30 MP │ P(2) R(3)] vs [Goblin: 12/30 HP │ W(2)]` |
| **Damage Messages** | `You strike Goblin for 15 damage!` | `You strike Goblin for **15** damage!` (red highlight) |
| **Equipment** | `You equipped Iron Sword.` | Shows before/after stats with colored deltas |
| **Inventory** | Lists items only | Shows slots, weight, value with threshold colors |
| **Abilities** | Lists all abilities | Colors ready abilities green, cooling abilities gray |
| **Achievements** | Shows unlocked only | Shows progress toward locked achievements |

---

## Color Palette

| Element | Color | Purpose |
|---------|-------|---------|
| HP | Red (threshold-based) | Health status at-a-glance |
| Mana | Blue | Mana/resource tracking |
| Gold | Yellow | Currency |
| XP | Green | Experience gains |
| Attack | Bright Red | Offensive stats |
| Defense | Cyan | Defensive stats |
| Success | Green | Confirmations, healing |
| Errors | Red | Warnings, failures |
| Cooldowns | Gray | Disabled abilities |

---

## Architecture Impact

✅ **No breaking changes** — All improvements via DisplayService extensions  
✅ **Clean separation** — Game logic untouched; only display layer modified  
✅ **Test-friendly** — TestDisplayService strips ANSI codes automatically  
✅ **Accessible** — Color enhances existing emoji/labels, never replaces  

---

## Team Workload

- **Hill:** 8-10 hours (Color system, core colorization, inventory/equipment display)
- **Barton:** 7-9 hours (Combat hierarchy, HUD, status effects, ability visuals)
- **Romanoff:** 3-4 hours (Test infrastructure updates, verification)
- **Coulson:** 2-3 hours (Design review, code review, approval gates)

**Total:** 20-26 hours

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| ANSI support variance | Auto-detect + graceful fallback |
| Test breakage | Strip ANSI codes in TestDisplayService |
| Color readability | High-contrast palette, tested on multiple terminals |

---

## Success Metrics

- [ ] All 267 tests pass (zero regressions)
- [ ] Visual clarity: HP state, effects, cooldowns instantly recognizable
- [ ] Information density: All actionable info visible without scrolling
- [ ] Accessibility: Color-blind players retain full experience via emoji/labels

---

## Next Steps

1. **Team Design Review** — Present full plan to Hill, Barton, Romanoff
2. **Boss Approval** — Confirm scope and priorities
3. **Phase 1 Kickoff** — Hill implements ColorCodes utility + DisplayService extensions
4. **Parallel Phase 2 Work** — Hill (inventory/equipment), Barton (combat/status)
5. **Phase 3 Polish** — Both engineers tackle remaining enhancements
6. **Final Review** — Coulson validates architecture decisions before merge

---

**Full Plan:** `.ai-team/decisions/inbox/coulson-ui-ux-architecture.md` (20KB)
# UI/UX Improvement Plan — Visual Examples

**Before & After Comparisons**

---

## Example 1: Player Stats Display

### BEFORE (Current)
```
═══ PLAYER STATS ═══
Name: Thorin
Level: 5
HP: 45/60
Mana: 15/30
Attack: 18
Defense: 12
Gold: 320
XP: 450/500
```

### AFTER (Phase 1)
```
═══ PLAYER STATS ═══
Name: Thorin
Level: 5
HP: 45/60        ← yellow (75% health)
Mana: 15/30      ← cyan (50% mana)
Attack: 18       ← bright red
Defense: 12      ← cyan
Gold: 320        ← yellow
XP: 450/500      ← green
```

### AFTER (Phase 2 - with status effects)
```
═══ PLAYER STATS ═══
Name: Thorin
Level: 5
HP: 45/60        ← yellow
Mana: 15/30      ← cyan
Attack: 18       ← bright red
Defense: 12      ← cyan
Gold: 320        ← yellow
XP: 450/500      ← green

Active Effects:
  Poison (2 turns) - Taking 3 damage per turn     ← red
  Regen (3 turns) - Healing 4 HP per turn         ← green
```

---

## Example 2: Combat Status Line

### AFTER (Phase 2 - Enhanced HUD)
```
[You: 45/60 HP | 15/30 MP | P(2) R(3)] vs [Goblin: 12/30 HP | W(2)]
      ↑ yellow    ↑ cyan   ↑red ↑green       ↑ red         ↑yellow

  You strike Goblin for 15 damage!       ← 15 bright red
  Goblin attacks you for 8 damage!       ← 8 bright red

Legend: P=Poison, R=Regen, W=Weakened, (X)=turns remaining
```

---

## Example 3: Equipment Comparison

### AFTER (Phase 2)
```
════════════════════════════════════
Equipping: Iron Sword
────────────────────────────────────
Current Weapon: Rusty Dagger
  Attack: 10 → 15  (+5)    ← green for increase
  Defense: 5 → 5   (—)     ← gray for no change
════════════════════════════════════
Equipped Iron Sword
```

---

## Example 4: Inventory Display

### AFTER (Phase 3)
```
Choose an ability:
[1] Power Strike (10 MP, ready)        ← green bold (ready!)
[2] Defensive Stance (8 MP, ready)     ← green bold
[3] Poison Dart (12 MP, 2 turns)       ← gray (on cooldown)
[4] Second Wind (15 MP, 3 turns)       ← gray (on cooldown)

Mana: 15/30  ← cyan
```

---

## Example 6: Combat Critical Hit

### BEFORE (Current - on game end)
```
═══ ACHIEVEMENTS UNLOCKED ═══
🏆 Glass Cannon — Win with HP below 10
```

### AFTER (Phase 3 - shows locked achievements with progress)
```
═══ ACHIEVEMENTS ═══

UNLOCKED:
🏆 Glass Cannon — Win with HP below 10

PROGRESS:
❌ Speed Runner: 142 turns (need <100) — 71% progress    ← red (far from goal)
❌ Hoarder: 320g / 500g — 64% progress                   ← yellow (moderate)
❌ Elite Hunter: 8/10 enemies defeated — 80% progress    ← green (close!)
```

---

## Example 8: Room Description

### BEFORE (Current - can scroll indefinitely)
```
Turn 1: You attack Goblin for 12 damage
Turn 2: Goblin attacks you for 8 damage
Turn 3: You use Power Strike for 24 damage!
Turn 4: Goblin attacks you for 8 damage
Turn 5: You attack Goblin for 12 damage
Turn 6: Goblin misses!
Turn 7: You attack Goblin for 12 damage
```

### AFTER (Phase 3 - last 5 turns, colored)
```
Recent Turns (last 5):
  Turn 3: You use Power Strike for 24 damage!    ← green (player action)
  Turn 4: Goblin attacks you for 8 damage        ← red (enemy action)
  Turn 5: You attack Goblin for 12 damage        ← green
  Turn 6: Goblin misses!                         ← red
  Turn 7: You attack Goblin for 12 damage        ← green
```

---

## Color Palette Reference

| Element | ANSI Code | Example Use |
|---------|-----------|-------------|
| Red | `\u001b[31m` | HP (low), damage taken, errors |
| Green | `\u001b[32m` | HP (high), healing, XP, success |
| Yellow | `\u001b[33m` | HP (medium), gold, warnings |
| Blue | `\u001b[34m` | Mana (high), abilities |
| Cyan | `\u001b[36m` | Mana (medium), defense |
| Bright Red | `\u001b[91m` | Attack stat, critical damage |
| Bright Yellow | `\u001b[93m` | Critical hits, legendary items |
| Gray | `\u001b[90m` | Cooldowns, disabled options |

---

## Key Benefits

✅ **Instant health assessment** — Color-coded HP bars let players judge danger at a glance  
✅ **Active effect visibility** — Combat HUD shows buffs/debuffs persistently  
✅ **Informed decisions** — Equipment comparison shows stat changes before committing  
✅ **Goal clarity** — Achievement progress shows how close players are to unlocks  
✅ **Combat clarity** — Colored damage/healing stands out from narrative text  
✅ **Resource management** — Mana threshold colors warn when running low  
✅ **Ability readiness** — Cooldown colors instantly show what's available  

All while maintaining **full accessibility** — every color enhancement preserves existing emoji/text indicators!
# Systems UX Findings — Player Feedback Analysis
**Author:** Barton (Systems Dev)  
**Date:** 2026-02-20  
**Context:** Boss requested UX analysis from systems perspective. Current display is single-color text; need to identify where color/formatting would improve player feedback in combat, status, and progression systems.

---

## Executive Summary

The game has solid mechanical depth (status effects, abilities, equipment, crafting, boss phases) but **player visibility of this complexity is minimal**. All combat and status information is plain white text with no visual hierarchy. Players cannot quickly parse critical information during combat, track active effects, or understand what's happening mechanically.

**Critical UX Gaps:**
1. **Status effects are invisible** — player/enemy effects exist but are not displayed during combat
2. **Combat damage lacks context** — no indication of crits, dodges, modifiers, or damage types
3. **Health/mana status buried** — only shown when explicitly requesting stats or during specific prompts
4. **No danger signals** — boss enrage, telegraphed attacks, hazards look identical to normal text
5. **Equipment changes invisible** — stat changes happen but player can't see the impact
6. **Ability feedback minimal** — cooldowns/costs shown in menu but not current battle state

---

## 1. Combat Display Analysis

### Current State
- **Turn structure:** Player sees `[A]ttack [B]ability [F]lee` menu with mana count if abilities unlocked
- **Hit/miss feedback:** Single-line text messages (e.g., "You strike Goblin for 8 damage!")
- **Combat status:** One-line format: `[You: 45/50 HP] vs [Goblin: 12/20 HP]`
- **Turn log:** Last 3 actions displayed before menu (good idea, but format needs work)

### What's Clear
✅ Basic damage dealt and HP remaining  
✅ When abilities are available (mana shown)  
✅ Recent combat history (turn log)

### What's Confusing
❌ **No visual distinction between normal hits and crits** — both look identical despite 2x damage  
❌ **Dodge mechanics unclear** — player sees "Goblin dodges!" but doesn't know *why* (defense-based formula)  
❌ **Status effect modifiers hidden** — Fortified gives +50% DEF but player never sees "28 → 42 DEF"  
❌ **Boss mechanics buried** — enrage, charge telegraph, phase transitions are plain text in message flood  
❌ **Enemy special abilities invisible** — Vampire lifesteal, Wraith dodge chance, Shaman heals look like normal attacks  

### Color/Format Opportunities (Combat)

| Element | Current | Improvement | Impact |
|---------|---------|-------------|--------|
| **Critical hits** | "You strike for 16 damage!" | 💥 `[CRIT]` or red damage number | **HIGH** — crits feel impactful |
| **Dodge/miss** | "Goblin dodges your attack!" | Gray text or ↗️ arrow symbol | **MEDIUM** — clarity on miss reason |
| **Player damage taken** | "Goblin strikes you for 8 damage!" | Yellow/orange text for incoming | **HIGH** — danger visibility |
| **Boss charge telegraph** | "Boss is charging an attack!" | ⚠️ `[WARNING]` red/bold | **CRITICAL** — life-saving signal |
| **Status damage ticks** | "You take 3 poison damage!" | Green text with 🧪 symbol | **MEDIUM** — effect visibility |
| **Healing** | "You heal 20 HP!" | Bright green text with + | **MEDIUM** — positive reinforcement |
| **Enemy death** | "Goblin is defeated!" | Gray strikethrough or skull | **MEDIUM** — combat closure clarity |

---

## 2. Player Feedback on Status/Effects

### Critical Gap: **Active Effects Display**
- **StatusEffectManager has `GetActiveEffects(target)` method** but it's **never called for display**
- History notes: "DisplayActiveEffects feedback should be added to combat loop" (never implemented)
- **Player cannot see:**
  - What effects are currently on them or the enemy
  - How many turns remain on each effect
  - What stat modifiers are active (Fortified +50% DEF, Weakened -50% ATK)

### Where Effects Should Be Shown
1. **Combat status bar** — next to HP/mana display:
   ```
   [You: 45/50 HP] 🧪 Poisoned(2) 🛡️ Fortified(1)
   vs
   [Goblin: 12/20 HP] ⚔️ Weakened(3)
   ```

2. **Stats command** — active effects section showing modifiers:
   ```
   Active Effects:
     • Fortified (1 turn) — Defense +50%
     • Poison (2 turns) — 3 damage/turn
   ```

3. **Effect application/removal** — already has text messages (good)

### Color/Format Opportunities (Status)

| Effect Type | Symbol | Color | When to Show |
|-------------|--------|-------|--------------|
| Poison | 🧪 | Green | Every turn during combat status |
| Bleed | 🩸 | Red | Combat status + damage ticks |
| Stun | 💫 | Yellow | Combat status + "cannot act" message |
| Regen | ❤️ | Bright green | Combat status + heal ticks |
| Fortified | 🛡️ | Blue | Combat status + DEF value |
| Weakened | ⚔️ (broken) | Gray | Combat status + ATK value |

---

## 3. Player Status Visibility

### What's Missing
❌ **No persistent status display** — player must type STATS every time to see health outside combat  
❌ **No quick HP/mana check** — no shorthand command for "how much HP do I have?"  
❌ **XP progress invisible** — player sees "XP: 85" but not "85/100 to Level 4"  
❌ **Equipment stat totals unclear** — player sees "+5 ATK weapon" but final Attack value only in STATS  
❌ **Gold value feedback weak** — picks up gold but can't easily see total without STATS  
❌ **Ability cooldowns not visible** — must enter combat and press [B]ability to see cooldown state  

### Damage Numbers
**Current:** "You strike Goblin for 8 damage!"  
**Opportunity:** Show damage *breakdown* on crits or complex hits:
```
💥 CRITICAL HIT! 16 damage (8 base × 2 crit)
Your attack: 25 vs Defense: 10 = 15 base → 16 crit
```
**Impact:** **MEDIUM** — helps players understand stat math, but could be verbose

### Boss Mechanics
**Current:** All text, no visual priority  
**Opportunity:**
- Enrage: `⚠️ [ENRAGED] Attack 22 → 33 (+50%)`
- Charge telegraph: `⚡ [CHARGING] Next attack deals 3× damage!`
- Phase transition: Boss ASCII art or separator line

**Impact:** **CRITICAL** — boss fights are climax moments, must feel dramatic

### Enemy Special Abilities
**Current:** "Vampire Lord attacks you for 12 damage and heals 6 HP!"  
**Opportunity:** Color-code by mechanic type:
- Lifesteal: Red text for damage, green for heal
- Ambush: ⚡ symbol + yellow text
- Self-heal: Green + ❤️
- Status application: Effect symbol + color

**Impact:** **HIGH** — makes enemy variety *visible*

### Turn Log
**Current:** Last 3 actions, plain text  
**Opportunity:** Icon-prefix each log entry:
```
⚔️ You hit Goblin for 8 damage
🛡️ Goblin attacks but you dodge
🧪 Goblin takes 3 poison damage
```
**Impact:** **MEDIUM** — easier to scan history

---

## 5. Systems That Need Color Coding

### By Priority

#### CRITICAL (P0) — Core Combat Visibility
1. **Status effects display** — show active effects on player/enemy during combat status
2. **Boss mechanics** — enrage, charge, phase transitions need RED/BOLD
3. **Player damage taken** — incoming hits need distinct color from outgoing
4. **Low HP warning** — red text when HP < 30%

#### HIGH (P1) — Combat Clarity
5. **Critical hits** — 💥 symbol or red/bold damage numbers
6. **Stat modifiers** — show ATK/DEF changes from Fortified/Weakened
7. **Enemy special abilities** — lifesteal, dodge, heal need visual distinction
8. **Ability cooldown/mana** — gray out unavailable abilities in menu

#### MEDIUM (P2) — Feedback & Polish
9. **XP progress** — show "X/Y to next level"
10. **Gold changes** — show running total on pickup
11. **Healing** — green text for all heal sources
12. **Equipment stat changes** — "+5 ATK!" when equipping weapon
13. **Dodge/miss** — gray text or symbol

#### LOW (P3) — Nice-to-Have
14. **Turn log icons** — prefix each action with symbol
15. **Room type colors** — scorched = red, flooded = blue, etc.
16. **Item rarity** — if legendary items exist, color-code them
17. **Mana regen feedback** — "+10 mana" at turn start

---

## 6. Information That's Hard to Find Right Now

### During Combat
❌ "Am I poisoned right now?" — must read back through messages  
❌ "How many turns until Second Wind is off cooldown?" — must press [B]ability to check  
❌ "Is the boss enraged?" — must read back through messages  
❌ "What's my current defense after Fortified?" — stat modifiers never shown  

### During Exploration
❌ "What's my current HP?" — must type STATS  
❌ "How much XP until I level?" — must type STATS and do mental math  
❌ "What equipment am I wearing?" — must type EQUIPMENT  
❌ "What abilities do I have unlocked?" — must enter combat or type SKILLS  

### After Actions
❌ "Did my attack crit?" — damage number looks identical  
❌ "Did equipping this armor help?" — no before/after stat display  
❌ "Did I level up?" — text message exists but no fanfare  
❌ "How much damage did I avoid by dodging?" — never shown  

---

## 7. What Would Make Combat More Satisfying to Watch

### Moment-to-Moment Feedback
1. **Hit impact** — crits should *feel* different (💥 symbol, color, maybe "!" or larger text)
2. **Survivability** — show how close to death (HP bar color, percentage)
3. **Momentum** — consecutive hits or "on fire" mechanics (not implemented, but would feel good)
4. **Risk/reward** — telegraphed boss attacks create tension *if visually distinct*

### Progression Milestones
5. **Level-up celebration** — full heal is great, but needs visual fanfare (ASCII banner, bold text)
6. **Ability unlock** — "You've learned [Poison Dart]!" at L5 should be a Big Deal
7. **First crit** — tutorial moment: "💥 Critical hit! Your high attack gave a 15% chance to double damage!"
8. **Boss phases** — Phase 2 enrage should feel like the fight changed (separator line, ASCII art)

### Strategic Information
9. **Status effect counterplay** — "Goblin is Poisoned — deals 3 dmg/turn for 3 turns"
10. **Enemy danger level** — boss HP bar, elite enemy markers, threat indicators
11. **Resource tracking** — mana/cooldown visibility so player can *plan* ability usage
12. **Stat math transparency** — occasional damage breakdown to teach formulas

---

## 8. Recommendations for Coulson's Master Plan

### Phase 1: Core Visibility (Must-Have)
- **Display active effects** in combat status bar (player + enemy)
- **Color-code damage types:** red for incoming, white for outgoing, green for healing
- **Boss mechanic warnings:** red/bold for enrage and charge telegraph
- **Low HP indicator:** red text when player HP < 30%

### Phase 2: Combat Clarity (High-Value)
- **Critical hit markers:** 💥 symbol or distinct color
- **Stat modifier display:** show ATK/DEF changes from buffs/debuffs
- **Ability cooldown visibility:** gray out unavailable abilities in [B]ability menu
- **XP progress bar:** "85/100 XP to Level 4" in STATS command

### Phase 3: Polish & Delight (Nice-to-Have)
- **Turn log icons:** prefix actions with ⚔️ 🛡️ 🧪 symbols
- **Level-up fanfare:** ASCII banner or separator line
- **Equipment feedback:** "+5 ATK!" when equipping weapon
- **Gold running total:** "Gold: 45 → 57 (+12)" on pickup

### Non-Combat Improvements
- **Persistent status bar?** — some roguelikes show HP/Mana at top of screen always
- **Quick status command:** alias "S" for STATS (faster than typing full word)
- **EXAMINE improvements:** show more detail on enemies (abilities, resistances, threat level)

---

## Architecture Notes for Implementation

### Display Service Changes
- Current `IDisplayService` has no color/formatting support — all plain text
- Need to add:
  - `ShowColoredMessage(string message, ConsoleColor color)`
  - `ShowCombatMessageWithEmoji(string emoji, string message, ConsoleColor? color = null)`
  - `ShowStatusBar(Player player, Enemy enemy)` — enhanced version with active effects

### StatusEffectManager Integration
- `GetActiveEffects(target)` already exists but never used for display
- Add utility method: `FormatEffectsForDisplay(object target) → string`
- Call during combat status display (CombatEngine line ~267)

### CombatEngine Changes
- Damage calculation points need color logic:
  - Crit detection (line ~366) → red/bold
  - Player damage (line ~248) → white
  - Enemy damage (line ~294) → yellow/orange
- Boss mechanics (enrage, charge) → red/bold
- Status tick processing (line ~228) → color by effect type

### Existing Display Patterns
- `ShowCombatMessage(string)` is primary output
- `ShowCombatStatus(Player, Enemy)` is one-line HP display
- Combat messages use emoji already (⚔ ⚠ ✦ 💥) — good foundation
- Turn log exists (last 3 actions) — just needs formatting

---

## Technical Considerations

### Console Color Limitations
- Standard Console.ForegroundColor has 16 colors (8 + bright variants)
- Not all terminals support full RGB (Windows CMD, Linux terminal varies)
- **Recommendation:** Stick to basic colors (Red, Green, Yellow, Blue, White, Gray) + Bold/Dim
- **Fallback:** All features should degrade gracefully to plain text

### Performance
- Color changes via `Console.ForegroundColor` are fast (no concern)
- Emoji may have issues on some terminals (Windows CMD especially)
- **Recommendation:** Make emoji toggleable (config flag: `USE_EMOJI = true`)

### Core Abstraction
**IDisplayService** provides a complete separation layer between game logic and presentation:
- **Location:** `Display/IDisplayService.cs`
- **Implementation:** `ConsoleDisplayService` (Display/DisplayService.cs, 324 LOC)
- **Pattern:** Interface-based inversion of control — all Engine/ and Systems/ code receives IDisplayService via constructor injection

### Display Contract (14 public methods)
```
- ShowTitle()              → Title screen
- ShowRoom(Room)           → Room description + exits + enemies + items
- ShowCombat(string)       → Combat headline
- ShowCombatStatus(P, E)   → HP status bars
- ShowCombatMessage(str)   → Combat narrative
- ShowPlayerStats(Player)  → Full stat sheet
- ShowInventory(Player)    → Item list
- ShowLootDrop(Item)       → Loot announcement
- ShowMessage(string)      → General output
- ShowError(string)        → Error messages
- ShowHelp()               → Command list
- ShowCommandPrompt()      → Input prompt
- ShowMap(Room)            → ASCII mini-map with BFS traversal
- ReadPlayerName()         → Initial name prompt (input method)
```

### Architecture Strengths (What's Working)
1. **Zero Console.Write leakage** — Engine/ has ZERO direct Console calls (verified by grep)
2. **Clean separation** — CombatEngine (746 LOC), GameLoop (977 LOC) entirely decoupled from display
3. **Testability** — Interface allows stub implementation (Dungnz.Tests/DisplayServiceTests.cs exists)
4. **Single responsibility** — DisplayService owns all rendering; game logic owns state/rules

### Current Visual Elements
**Unicode box drawing:** ═ ║ ╔ ╗ ╚ ╝  
**Emoji indicators:** ⚔ ⚠ ✦ ✗ 🌑 🌿 💧 🔥 🏛  
**ASCII map symbols:** [*] [B] [E] [!] [S] [+] [ ]  
**All output:** Plain white text on default background (no color)

---

## 2. TECHNICAL ASSESSMENT

### Code Quality
✅ **Excellent foundation** — Interface contract is well-defined  
✅ **DI-ready** — All dependencies injected; no static coupling  
✅ **Documented** — XML comments on every public member  
✅ **Consistent** — Single class handles all display; no scattered Console calls  

### What's Limiting UX Improvements
1. **Monochrome output** — All text is same color (white on black or system default)
2. **No emphasis** — Important info (HP warnings, errors, loot) visually identical to regular text
3. **Flat hierarchy** — Headers, body text, prompts all blend together
4. **No state signaling** — Can't tell at a glance if room is safe/dangerous/cleared

### Console API Coverage
**Current:** Console.WriteLine, Console.Write, Console.Clear, Console.ReadLine  
**Not used:** Console.ForegroundColor, Console.BackgroundColor, Console.ResetColor, Console.SetCursorPosition

---

## 3. IMPROVEMENT OPPORTUNITIES

### High-Impact, Low-Complexity Changes

#### A. Color Coding by Semantic Meaning
Add color support via Console.ForegroundColor:
- **RED** → Errors, HP warnings, enemy names, combat damage
- **GREEN** → Positive events (loot drops, level-up, heals)
- **YELLOW** → Warnings, hazards, important choices
- **CYAN** → Headers, section titles, help text
- **MAGENTA** → Rare/special items, boss encounters
- **GRAY** → Flavor text, room descriptions, minor details

**Implementation:** Add SetColor(ConsoleColor) helper; wrap text blocks with color + ResetColor()

#### B. HP Status Bar Enhancement
Current: `[You: 45/100 HP] vs [Goblin: 12/25 HP]`  
Improved: Color-coded HP based on % remaining:
- >70% → GREEN
- 40-70% → YELLOW  
- <40% → RED

#### C. Structured Layout Improvements
- **Combat messages** → Indent with color-coded prefixes
- **Inventory** → Color items by type (weapons=yellow, armor=cyan, consumables=green)
- **Room descriptions** → Gray text for atmosphere, WHITE for exits/items
- **Map** → Color symbols ([!]=RED enemy, [S]=MAGENTA shrine, [E]=GREEN exit)

#### D. Message Type Differentiation
Current ShowMessage() and ShowError() look identical except for ✗ prefix.  
Improved: ShowError → RED text; ShowMessage → WHITE text; ShowCombat → YELLOW/RED

### Technical Approach
**Option 1: Extend IDisplayService with color variants**
```csharp
void ShowMessage(string message, ConsoleColor color = ConsoleColor.White);
void ShowColoredText(string text, ConsoleColor color);
```
❌ Problem: Changes interface → breaks existing callers

**Option 2: Internal color logic in ConsoleDisplayService**
```csharp
// No interface changes; DisplayService decides colors internally
public void ShowError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ {message}");
    Console.ResetColor();
}
```
✅ **RECOMMENDED:** Zero breaking changes; backward compatible

**Option 3: Rich text markup system**
```csharp
ShowMessage("You found {green}50 gold{/green}!");
```
❌ Overkill for current needs; adds parsing complexity

---

## 4. ARCHITECTURAL RECOMMENDATIONS

### Phase 1: Internal Color Enhancement (No API changes)
**Scope:** Update ConsoleDisplayService implementation only  
**Effort:** ~2-3 hours  
**Impact:** Immediate visual improvement; zero regression risk

Changes:
1. ShowError → RED text
2. ShowCombat → YELLOW text for headline
3. ShowCombatStatus → Color-coded HP bars
4. ShowLootDrop → GREEN text
5. ShowPlayerStats → CYAN header
6. ShowInventory → Color by ItemType
7. ShowMap → Color-coded symbols

### Phase 2: Optional Display Preferences (Future)
If we want player control:
- Add DisplayOptions class (Colors: bool, Emoji: bool, Layout: Compact|Verbose)
- Pass to DisplayService constructor
- Allows accessibility (colorblind mode, screen reader support)

### Phase 3: Advanced Layouts (Low priority)
- Status bar at top of screen (HP/Floor/Gold always visible)
- Box borders for combat log
- Clear screen less often; use SetCursorPosition for updates

---

## 5. RISK ASSESSMENT

### What Could Go Wrong
1. **Terminal compatibility** → Some terminals don't support ANSI colors
   - Mitigation: Detect via Environment variables; fall back to monochrome
2. **Color blindness** → RED/GREEN distinction fails for 8% of users
   - Mitigation: Use brightness + symbols, not color alone
3. **Readability on light backgrounds** → YELLOW text invisible on white terminal
   - Mitigation: Test with both dark/light themes; adjust palette if needed

### Breaking Changes (None expected)
- IDisplayService interface unchanged
- All callers (GameLoop, CombatEngine) unaffected
- Tests unchanged (stub implementation ignores color)

---

## 6. IMPLEMENTATION NOTES

### Key Design Patterns to Preserve
1. **Separation of concerns** — Game logic never knows about colors
2. **Dependency injection** — DisplayService injected, not newed
3. **Interface stability** — Public API unchanged
4. **Testability** — Color is display detail; tests verify text content only

### Code Ownership (per charter)
- **Hill owns:** DisplayService implementation, IDisplayService interface
- **Barton owns:** Nothing in Display/ folder
- **Changes:** All within Hill's boundaries

### Files to Modify
- `Display/DisplayService.cs` (324 LOC) — Primary target
- `Display/IDisplayService.cs` — NO CHANGES (keep interface stable)

### Files NOT to Touch
- `Engine/GameLoop.cs` — Already correct; uses IDisplayService properly
- `Engine/CombatEngine.cs` — Already correct; no Console calls
- `Program.cs` — Only 4 Console calls for setup prompts (acceptable; one-time use)

---

## 7. CONCLUSION

### Current State Summary
**Architecture: A+** — Clean separation, DI-ready, zero leakage  
**Visual design: C** — Functional but monochrome; no emphasis or hierarchy  
**Extensibility: A** — Ready for color enhancement without refactoring

### Recommended Next Steps
1. Implement Phase 1 (internal color enhancement) — Hill can do this solo in <3 hours
2. Test on multiple terminals (PowerShell, bash, Windows Terminal, gnome-terminal)
3. Get user feedback on color choices
4. Document color conventions in .ai-team/decisions/ for team reference

### Key Insight
We have an **excellent foundation** that makes UX improvements trivial to add. The abstraction layer is working perfectly — we can dramatically improve visual clarity without touching a single line of game logic. The interface pattern has paid off.

---

**Status:** Ready for Coulson to synthesize into master plan  
**Blocker:** None  
**Dependency:** None (standalone enhancement)
# UI/UX Improvement Plan — Documentation Index

**Initiative:** TextGame UI/UX Enhancement  
**Lead:** Coulson  
**Date:** 2026-02-20  
**Status:** ✅ READY FOR TEAM REVIEW

---

## What's in this folder

This folder contains the complete UI/UX improvement plan requested by the Boss. The plan addresses the current limitation that all TextGame output is single-color text with no visual hierarchy beyond emoji and Unicode characters.

---

## Documents

### 1. **Executive Summary** (START HERE)
📄 `coulson-ui-ux-summary.md` (4 KB)

Quick overview for busy readers. Covers:
- The problem (no color system)
- The solution (3-phase implementation)
- Key improvements at-a-glance
- Team workload estimates
- Risk mitigation

**Read this first** to decide if you want to approve the initiative.

---

### 2. **Full Architecture Plan** (TECHNICAL SPEC)
📄 `coulson-ui-ux-architecture.md` (21 KB)

Comprehensive technical design document. Covers:
- Current state analysis (architecture strengths/gaps)
- Proposed color system (ANSI palette, semantic colors, thresholds)
- Complete improvement roadmap (12 work items across 3 phases)
- Technical implementation details with code examples
- Architecture decisions and patterns
- Dependency graph and critical path
- Risk assessment and mitigation
- Success metrics and acceptance criteria

**Read this** if you're implementing the plan or need full technical context.

---

### 3. **Visual Examples** (BEFORE/AFTER)
📄 `coulson-ui-ux-visual-examples.md` (7 KB)

Side-by-side comparisons showing exactly how the UI will change. Includes:
- Player stats display
- Combat status HUD
- Equipment comparison
- Inventory weight display
- Ability cooldown menu
- Achievement progress tracking
- Room descriptions
- Combat turn log

**Read this** to see what the improvements will look like.

---

### 4. **Implementation Checklist** (TEAM REFERENCE)
📄 `coulson-ui-ux-checklist.md` (8 KB)

Work breakdown for Hill, Barton, and Romanoff. Covers:
- Phase 1 checklist (ColorCodes, DisplayService, core stats)
- Phase 2 checklist (combat, equipment, inventory, status effects)
- Phase 3 checklist (achievements, rooms, abilities, turn log)
- Testing checklist (per phase)
- Code review gates (Coulson checkpoints)
- Final merge criteria

**Use this** during implementation to track progress.

---

## Quick Facts

| Metric | Value |
|--------|-------|
| **Total Work Items** | 12 (WI-1 through WI-12) |
| **Estimated Time** | 15-20 hours total |
| **Team Allocation** | Hill (8-10h), Barton (7-9h), Romanoff (3-4h), Coulson (2-3h) |
| **Phases** | 3 (Foundation → Enhancement → Polish) |
| **Breaking Changes** | ZERO (all via DisplayService extensions) |
| **Test Impact** | All 267 tests must pass (no rewrites needed) |

---

## Key Improvements

✅ **Color-coded HP/Mana** — Instant visual feedback on health status  
✅ **Active effects HUD** — Combat status shows buffs/debuffs persistently  
✅ **Equipment comparison** — Before/after stats when equipping gear  
✅ **Inventory weight tracking** — Visual weight/value summary with threshold colors  
✅ **Achievement progress** — Shows how close players are to unlocks  
✅ **Combat clarity** — Colored damage/healing stands out from narrative  
✅ **Ability readiness** — Cooldown status visible at a glance  

---

## Architecture Principles

1. **Console-native** — ANSI colors, no external frameworks
2. **Accessibility-first** — Color enhances, never replaces emoji/labels
3. **Clean separation** — All changes in Display layer; game logic untouched
4. **Test-friendly** — TestDisplayService strips ANSI codes automatically
5. **No breaking changes** — Existing DisplayService methods unchanged

---

## Next Steps

### For Boss:
1. Review `coulson-ui-ux-summary.md` (4 KB, 2-minute read)
2. Browse `coulson-ui-ux-visual-examples.md` to see the vision
3. **DECISION:** Approve initiative or request changes

### For Team (after Boss approval):
1. Schedule design review ceremony (present full plan)
2. Hill kicks off Phase 1 (ColorCodes + DisplayService foundation)
3. Parallel Phase 2 work (Hill=inventory/equipment, Barton=combat/status)
4. Phase 3 polish (both engineers)
5. Coulson code review + final approval gate

---

## Questions?

- **"Will this break existing tests?"** — No. TestDisplayService strips ANSI codes; all tests check plain text.
- **"What if terminals don't support ANSI colors?"** — Graceful fallback to current emoji-only design.
- **"How much time will this take?"** — 15-20 hours total across 3 phases (2-3 weeks at normal pace).
- **"Can we do Phase 1 only?"** — Yes. Each phase delivers value independently.
- **"Will this slow down the game?"** — No. ANSI codes are 10-20 bytes per segment (negligible).

---

**Ready to proceed?** Review the summary, approve, and we'll kick off Phase 1.

— Coulson, Lead Architect

--- FILE: barton-intro-flow-ux-recommendations.md ---

### 2026-02-22: Intro Flow & Character Creation UX Recommendations

**By:** Barton  
**Context:** Analysis of player psychology and game mechanics for optimal intro sequence.

---

## 1. Optimal Intro Flow

**Recommended Order:**
```
1. Lore intro (optional, 3-4 sentences) → Sets tone
2. Player name → Personal investment before mechanical choices
3. Prestige display (if >0) → Shows progression context
4. Class selection (redesigned) → Core mechanical identity
5. Difficulty selection (with transparency) → Challenge tuning
6. Seed display/override → Advanced option, auto-generated
```

**Rationale:**
- **Name first** creates investment before mechanical friction. Players care more about choices after naming their character.
- **Class before difficulty** lets players choose playstyle identity before tuning challenge. "I'm a Warrior" → "now how hard?"
- **Seed last** because 95% of players don't care. Auto-generate it, show it before dungeon entry, allow override via CLI flag or advanced prompt.

---

## 2. Class Selection UX — Make It FEEL Like a Choice

**Current Problem:** Dry bullet list with vague descriptions ("High HP" vs. actual +20 bonus). Passive traits (Warrior +5% @ low HP, Mage +20% spell damage, Rogue +10% dodge) are NEVER shown during selection—only discovered in combat.

**Redesigned Card Format:**

```
╔══════════════════════════════════════════════════════════════╗
║ [1] WARRIOR — The Unyielding Vanguard                       ║
╠══════════════════════════════════════════════════════════════╣
║ Base Stats (+ Bonuses):                                     ║
║   HP: 100 → 120  |  ATK: 10 → 13  |  DEF: 3 → 5            ║
║   Mana: 30 → 20                                             ║
║                                                              ║
║ Passive Trait:                                              ║
║   🗡️ Battle Fury — +5% damage when HP < 50%                 ║
║                                                              ║
║ Playstyle:                                                  ║
║   Tank through attrition. High sustain, slower ability use. ║
║   Best for: Defensive, methodical players.                  ║
╚══════════════════════════════════════════════════════════════╝

╔══════════════════════════════════════════════════════════════╗
║ [2] MAGE — The Arcane Bombardier                            ║
╠══════════════════════════════════════════════════════════════╣
║ Base Stats (+ Bonuses):                                     ║
║   HP: 100 → 90  |  ATK: 10 → 10  |  DEF: 3 → 2             ║
║   Mana: 30 → 60                                             ║
║                                                              ║
║ Passive Trait:                                              ║
║   ✨ Spell Mastery — Abilities deal +20% damage             ║
║                                                              ║
║ Playstyle:                                                  ║
║   Glass cannon burst. High ability spam, low survivability. ║
║   Best for: Aggressive, high-risk players.                  ║
╚══════════════════════════════════════════════════════════════╝

╔══════════════════════════════════════════════════════════════╗
║ [3] ROGUE — The Shadow Dancer                               ║
╠══════════════════════════════════════════════════════════════╣
║ Base Stats (+ Bonuses):                                     ║
║   HP: 100 → 100  |  ATK: 10 → 12  |  DEF: 3 → 3            ║
║   Mana: 30 → 30                                             ║
║                                                              ║
║ Passive Trait:                                              ║
║   👤 Shadow Step — +10% dodge chance                        ║
║                                                              ║
║ Playstyle:                                                  ║
║   Evasion-focused. Balanced offense/defense, relies on RNG. ║
║   Best for: Tactical, risk-mitigating players.             ║
╚══════════════════════════════════════════════════════════════╝
```

**Why This Works:**
- **Explicit numbers** remove guesswork. Players see +20 HP, not "high HP."
- **Passive trait shown upfront** defines combat feel. Mage players know spells are their win condition.
- **Playstyle guidance** helps new players self-select. "Do I want sustain, burst, or evasion?"
- **Visual hierarchy** (cards with emoji icons) creates excitement vs. plain bullet list.

---

## 3. Difficulty Selection — Show the Mechanics

**Current Problem:** Three options with zero explanation. Players don't know what changes. "Casual" could mean 50% easier or 90% easier—unclear.

**Redesigned Prompt:**

```
Choose your challenge:

[1] CASUAL — Learning the Ropes
    • Enemies have 30% less HP, ATK, DEF (0.7x scaling)
    • Loot drops are 50% more generous (1.5x gold/items)
    • Elite spawn rate: 3%
    ➤ Recommended for first playthrough

[2] NORMAL — Balanced Challenge
    • Enemies use standard stats (1.0x scaling)
    • Loot drops at base rates
    • Elite spawn rate: 5%
    ➤ The intended experience

[3] HARD — Hardcore Mode
    • Enemies have 30% more HP, ATK, DEF (1.3x scaling)
    • Loot drops reduced by 30% (0.7x gold/items)
    • Elite spawn rate: 8%
    ➤ For veterans seeking punishment
```

**Why This Works:**
- **Multipliers shown explicitly** (0.7x/1.0x/1.3x) make impact clear.
- **Loot transparency** helps players understand risk/reward. Hard = less safety net.
- **Elite spawn rates** communicate late-game danger scaling.
- **Recommendations** guide new players without condescension.

---

## 4. Seed System — Stop Blocking Casual Players

**Current Problem:** Mandatory seed prompt before game start. 95% of players don't care about reproducibility—only speedrunners and content creators do.

**Recommended Approach:**

**Option A: Auto-Generate + Display (Simplest)**
```
1. Automatically generate random seed in background (no prompt)
2. Display seed right before dungeon entry:
   "Entering dungeon... Seed: 482739 (share this to replay the same run)"
3. Add optional CLI flag for custom seeds:
   `dotnet run --seed 123456`
4. No prompt unless --seed flag omitted but --ask-seed flag present
```

**Option B: Advanced Options Menu (More Flexible)**
```
1. After difficulty selection, show:
   "Press [Enter] to begin, or [A] for Advanced Options"
2. Advanced menu includes:
   • Custom seed entry
   • Prestige reset (future)
   • Debug mode toggles (future)
3. Default behavior (Enter) auto-generates seed and starts immediately
```

**Recommendation:** **Option A**. CLI flags are familiar to target audience (terminal users). Option B adds menu complexity for minimal gain.

**Why This Works:**
- **Zero friction for casual players.** Seed generation is invisible.
- **Power users get control** via CLI flags (familiar pattern for devs/speedrunners).
- **Seed still shareable** (displayed at dungeon entry), enabling reproducibility without blocking flow.

---

## 5. Prestige Integration — When to Show Bonuses

**Current Flow:** Prestige bonuses displayed immediately after title screen (lines 10-13 of Program.cs), BEFORE name/class/difficulty selection.

**Problem:** Players see "+1 Atk, +1 Def, +5 HP" but don't yet know base stats (100 HP, 10 ATK, 3 DEF). Bonuses feel abstract.

**Redesigned Flow:**

**Option 1: Show Before Class Selection (Recommended)**
```
1. Display prestige AFTER player enters name, BEFORE class cards
2. Frame as "Your starting bonuses:"
   "⭐ Prestige Level 2 — You begin with +2 ATK, +2 DEF, +10 HP"
3. Class cards show TOTAL stats (base + class + prestige):
   Warrior: HP 100 → 130 (20 class, 10 prestige)
4. Players see their full power level at decision time
```

**Option 2: Show After Class Selection**
```
1. Player chooses class, sees base + class bonuses
2. Prestige applied silently
3. Display total stats after all choices:
   "Final Stats: HP 130, ATK 14, DEF 7, Mana 20"
4. One-line prestige acknowledgment: "Prestige bonuses applied: +2 ATK, +2 DEF, +10 HP"
```

**Recommendation:** **Option 1**. Players should see their TOTAL starting power when making class choice. Hiding prestige bonuses until after selection creates a "gotcha" feeling—players can't properly evaluate class tradeoffs.

**Additional Prestige UX:**
- If prestige level > 0, show progression hint:
  ```
  ⭐ Prestige Level 2 (6 wins total)
  Next prestige level at 9 wins (+1 ATK, +1 DEF, +5 HP)
  ```
- Creates aspirational goal ("just 3 more wins for next bonus").

---

## 6. Optional Lore Intro — Set the Tone

**Current Flow:** Jumps straight to name entry. No context, no stakes, no atmosphere.

**Proposed Intro (3-4 sentences, skippable):**

```
The dungeon beneath the Shattered Peaks has claimed a thousand souls.
Treasure and death lie in equal measure within its depths.
You stand at the entrance, the weight of your weapon familiar in your hand.
What you seek below—glory, gold, or simply survival—only the shadows know.

[Press Enter to continue]
```

**Why This Works:**
- **Sets tone** (grim, high-stakes) before mechanical choices.
- **Establishes genre** (classic dungeon crawler, not comedic roguelike).
- **Skippable** (single Enter press) to avoid annoying returning players.
- **No lore dump.** 3-4 sentences max. Flavor, not exposition.

---

## Implementation Priority

1. **High Priority (Critical UX Issues):**
   - Redesign class selection cards with stat bonuses + passive traits (Issue #1)
   - Add difficulty multiplier transparency (Issue #2)
   - Move seed to auto-generate + CLI flag (Issue #3)

2. **Medium Priority (Polish):**
   - Reposition prestige display after name entry, before class selection (Issue #4)
   - Add prestige progression hint ("Next level at X wins")

3. **Low Priority (Nice-to-Have):**
   - Add optional lore intro paragraph (Issue #5)

---

## Open Questions for Design Review

1. **Should class cards show prestige bonuses inline?**
   - Option A: Warrior HP 100 → 130 (includes prestige)
   - Option B: Warrior HP 100 → 120, then separate "+ 10 prestige"

2. **Should difficulty affect prestige gain rate?**
   - Hard mode grants prestige faster (every 2 wins instead of 3)?
   - Keeps prestige purely win-based (current design)?

3. **Do we want a "recommended class" hint for first-time players?**
   - E.g., "(New players: try Warrior for survivability)"
   - Or trust playstyle descriptions?

4. **Should seed be shown in HUD during gameplay?**
   - Helps content creators verify correct seed mid-stream
   - Adds visual clutter for everyone else

---

## Design Philosophy Applied

- **Every choice is a tutorial.** Class selection teaches resource tradeoffs (HP vs. Mana). Difficulty teaches scaling mechanics.
- **Informed choices > surprises.** Players should see exact stat bonuses, not vague descriptions.
- **Friction kills retention.** Seed prompts block 95% of players to serve 5%. Remove friction.
- **Progression should feel rewarding.** Prestige bonuses mean nothing if players don't see them applied at decision time.
- **Tone matters.** The intro is the first impression. Lore paragraph sets dungeon crawler atmosphere.

---

**Next Steps:**
- Review recommendations with team (Coulson, Hill)
- Prototype class selection card format (DisplayService method)
- Spike: CLI argument parsing for --seed flag
- Update Program.cs flow based on finalized design

--- FILE: barton-intro-systems-design.md ---
# 2026-02-22: Intro Systems Design Notes

**By:** Barton  
**What:** Game systems perspective on intro improvements  
**Why:** Planning session for intro UX improvements  

---

## Executive Summary

The intro flow is **functionally complete but lacks informativeness**. Players make consequential choices (class, difficulty) with insufficient information to understand the tradeoffs. From a systems perspective, class selection needs transparent stat and playstyle information so players can make *meaningful* choices that affect how they play the game. Difficulty descriptions should communicate **mechanical impact**, not just flavor. The seed is currently over-emphasized for a single-player experience.

---

## Current State Assessment

### What Players See Today
```
Title screen
↓
Player name input
↓
Seed selection (mandatory prompt)
↓
Difficulty choice: [1] Casual [2] Normal [3] Hard (no explanation)
↓
Class choice with 2-line description for each class
↓
Game starts
```

### Current Class Information Quality

| Class   | Stat Bonuses (Hidden)              | Shown Description                          | Trait (Shown in-game) |
|---------|------------------------------------|--------------------------------------------|----------------------|
| Warrior | +3 ATK, +2 DEF, +20 HP, -10 Mana | High HP, defense, attack bonus. Reduced mana. | +5% damage @ <50% HP |
| Mage    | +0 ATK, -1 DEF, -10 HP, +30 Mana | High mana and powerful spells. Reduced HP. | +20% spell damage     |
| Rogue   | +2 ATK, +0 DEF, +0 HP, +0 Mana  | Balanced with attack bonus. Extra dodge.   | +10% dodge chance     |

**Problem:** Players don't see the *numbers*. Descriptions are vague ("High HP" vs. actual +20). No indication of *playstyle impact*. Rogue description mentions "balanced" but offers no mechanical clarity on what makes them different from default.

### Current Difficulty Information Quality

Three options presented with **zero explanation** of what each means:
- Casual: Unknown scaling factors
- Normal: Assumed default
- Hard: No indication of what gets harder

**Problem:** A new player can't answer "which difficulty should I choose for my first playthrough?" Players don't know if Hard means enemies have 10% more stats or 100% more.

### Current Seed Handling

Forced early prompt asking for reproducibility option. If skipped, shows "Random seed: XXXXXX" with instruction to "share this to replay."

**Problem:** Single-player experience; seed is not a primary choice factor. Overloading this in the intro flow for the 5% of players interested in reproducibility.

---

## Design Goals & Constraints

### Overarching Principles
1. **Players should make informed choices** — stats, abilities, and playstyle tradeoffs should be visible at selection time
2. **Difficulty should communicate mechanical impact** — players understand what "harder" means in concrete terms
3. **Game feel over technical features** — seed/reproducibility is cool but not a primary game loop concern
4. **Onboarding should establish playstyle** — the class choice should telegraph the game's tactical depth and core loop

### Constraints from System Design
- **Prestige system exists** — returning players get stat bonuses; intro should acknowledge this
- **Ability unlock system exists** — Warrior/Mage/Rogue have different ability availability; should hint at this
- **Difficulty scaling is mechanical** — enemy HP, loot rates, gold multipliers are concrete per DifficultySettings
- **Status effects / traits are real** — each class has a passive trait that defines playstyle (Warrior comeback, Mage burst, Rogue evasion)

---

## System-Driven Design Recommendations

### 1. Class Selection Card Redesign

**Current Problem:** Text descriptions don't convey stat tradeoffs. Players can't compare meaningfully.

**Recommended Card Format:**
```
┌─────────────────────────────────────────────┐
│ [1] WARRIOR - "The Bastion"                 │
├─────────────────────────────────────────────┤
│ Starting Stats:                              │
│   HP: 50 + 20 = 70   ATK: 10 + 3 = 13       │
│   DEF: 8 + 2 = 10    Mana: 30 - 10 = 20     │
│                                              │
│ Playstyle:                                   │
│   • Sustain focused — high defense absorbs   │
│     enemy damage                              │
│   • Comeback mechanic — gains +5% damage     │
│     when HP < 50%, encouraging aggressive    │
│     play in desperate situations              │
│   • Slow mana — abilities are supplementary  │
│     to physical attacks                       │
└─────────────────────────────────────────────┘
```

**Rationale:**
- **Shows actual numbers** — players see the tradeoff (Warrior trades mana for HP/DEF)
- **Explains the why** — "comeback mechanic" tells experienced players about Warrior's identity
- **Establishes playstyle** — "sustain focused" vs. "burst focused" helps players choose based on preference
- **Emphasizes trait** — passive ability is mentioned upfront, not buried in combat menus

**Repeating for all 3 classes:**
```
┌─────────────────────────────────────────────┐
│ [2] MAGE - "The Evoker"                     │
├─────────────────────────────────────────────┤
│ Starting Stats:                              │
│   HP: 50 - 10 = 40   ATK: 10 + 0 = 10       │
│   DEF: 8 - 1 = 7     Mana: 30 + 30 = 60     │
│                                              │
│ Playstyle:                                   │
│   • Burst damage focus — high mana enables   │
│     frequent ability usage                   │
│   • Spell-first combat — +20% spell damage   │
│     makes abilities primary, attacks         │
│     supplementary                             │
│   • Fragile — low HP/DEF means positioning   │
│     and ability chaining are critical        │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ [3] ROGUE - "The Opportunist"               │
├─────────────────────────────────────────────┤
│ Starting Stats:                              │
│   HP: 50 + 0 = 50    ATK: 10 + 2 = 12       │
│   DEF: 8 + 0 = 8     Mana: 30 + 0 = 30      │
│                                              │
│ Playstyle:                                   │
│   • Evasion focused — +10% dodge chance      │
│     turns defense into probability, not      │
│     raw mitigation                            │
│   • Versatile — balanced stats + combat     │
│     bonus good for learning all mechanics    │
│   • Hit-and-fade — medium damage/mana       │
│     supports both abilities and attacks      │
└─────────────────────────────────────────────┘
```

**Implementation Note:** No code changes needed. Display service already has `PlayerClassDefinition.All` to iterate. Just enhance `ConsoleDisplayService.ShowClassSelection()` to format richer output.

---

### 2. Difficulty Selection with Mechanical Clarity

**Current Problem:** "Casual/Normal/Hard" with no context. Players don't understand the **multipliers**.

**Recommended Format:**
```
Choose your difficulty:

[1] CASUAL — "Learning the Ropes"
    Enemy Power: 70% (weaker enemies)
    Loot Frequency: +50% (more items drop)
    Gold Rewards: +50% (richer experience)
    → Good for: First playthrough, learning mechanics, relaxed pace

[2] NORMAL — "Balanced Challenge" (recommended)
    Enemy Power: 100% (baseline)
    Loot Frequency: 100% (balanced)
    Gold Rewards: 100% (baseline rewards)
    → Good for: Experience players, intended experience

[3] HARD — "Hardcore Mode"
    Enemy Power: 130% (stronger enemies)
    Loot Frequency: 70% (fewer drops)
    Gold Rewards: 70% (lean economy)
    → Good for: Challenge runs, experienced players

Enter your choice [1-3] (default: 2 Normal):
```

**Rationale:**
- **Explicitly states multipliers** — players know exactly what "harder" means
- **Recommends default** — reduces decision paralysis for new players
- **Frames difficulty as intent** — "Learning the Ropes" is not shameful; it's a valid playstyle
- **Sets economy expectations** — Hard mode communicates scarcity, not just danger

**Implementation Note:** Data already exists in `DifficultySettings.For()`. Just format output in `ConsoleDisplayService.ShowDifficultyMenu()`. No calculation changes needed.

---

### 3. Seed Handling: Move to Advanced Options

**Current Problem:** Reproducibility is interesting for speedrunners/content creators (5% of players), but it's blocking the intro flow for everyone.

**Recommended Approach:**

**Main Flow (unchanged):**
```
Player name: [_______]
Choose class: [1] Warrior [2] Mage [3] Rogue
Choose difficulty: [1] Casual [2] Normal [3] Hard
→ Generate random seed internally
→ Display before entering dungeon: "Your seed: XXXXX (share to replay)"
```

**Advanced Options (post-difficulty):**
```
Advanced Options:

Would you like to enter a custom seed for reproducible runs? [Y/n]
→ If yes: "Enter seed (6 digits): [_______]"
→ If no: auto-generate
```

**Or simpler (not modal):**
```
[Press S to set custom seed, or Enter to continue with random...]
```

**Rationale:**
- **Unblocks casual players** — they skip right to playing
- **Honors speedrunner/content creator needs** — opt-in advanced feature
- **Improves game feel** — reduces intro friction significantly
- **Still shows seed pre-dungeon** — anyone interested can write it down for later

**Systems Consideration:** Seed is mechanically important for replay consistency, but from a **player experience standpoint**, it's a niche feature. Defaulting to random with visible output satisfies both audiences.

---

### 4. Prestige System Acknowledgment

**Current State:** If prestige.PrestigeLevel > 0, shows prestige display after title.

**Recommendation:** Keep the prestige display, but enhance it slightly:

```
═══════════════════════════════════════════
  PRESTIGE LEVEL 2 — "Veteran Adventurer"
  Bonuses: +4 starting ATK, +3 DEF, +20 HP
  Progress: Unlock PRESTIGE LEVEL 3 at 250 kills
═══════════════════════════════════════════

[Returning player? Your bonuses apply to all classes above.]
```

**Rationale:**
- **Reinforces progression** — shows prestige matters
- **Manages expectations** — shows stat additions are real, not flavor
- **Motivates continuation** — "unlock next level at X" creates aspirational goal

**Implementation Note:** Prestige system already exists. Just enhance `PrestigeSystem.GetPrestigeDisplay()` output.

---

### 5. Optional: One-Paragraph Lore Intro

**Current State:** None. Goes straight to name input.

**Recommendation:**

```
═══════════════════════════════════════════════════════════
                    THE DUNGEON AWAITS

The ancient dungeon has been sealed for centuries, guarded
by cursed guardians and forgotten treasures. Rumors speak
of an artifact at its heart—power beyond measure for those
brave enough to claim it. But the dungeon shows no mercy to
the unprepared. Many have entered. Few have returned.

Will you be the one?
═══════════════════════════════════════════════════════════

Enter your adventurer's name: [_______]
```

**Rationale:**
- **Sets tone** — dungeon crawlers work better with a sense of dread/adventure
- **Justifies why** — "why are you going down here?" is answered
- **Minimal friction** — 3-4 sentences, no gameplay impact
- **Optional** — can be toggled in future "skip intro" option

**Systems Consideration:** This is narrative setup, not mechanics. From a systems perspective, it establishes the *feeling* that the game is dangerous and worth taking seriously. This influences player decision-making (choosing Casual vs. Hard becomes a "how prepared am I?" decision rather than just "I want an easier game").

---

## Summary of Improvements

| Aspect | Current | Recommended | Impact |
|--------|---------|-------------|--------|
| **Class Info** | 2 lines per class, no stats | Full stat card + playstyle | Players make informed choices |
| **Difficulty Info** | 3 names only | Names + multipliers + recommendations | Players understand scaling |
| **Seed Flow** | Mandatory prompt | Optional advanced feature | Reduces intro friction |
| **Prestige Display** | If applicable | Enhanced with progression hint | Reinforces long-term goals |
| **Lore** | None | Optional opening paragraph | Establishes game tone/stakes |

---

## Implementation Priority

1. **Class Selection Cards** (1-2 hours) — highest impact, demonstrates stats/tradeoffs
2. **Difficulty Descriptions** (30 min) — quick win, immediately clarifies player choices
3. **Seed to Advanced Option** (30 min) — friction reduction
4. **Prestige Enhancement** (20 min) — polish returning player experience
5. **Lore Intro** (optional, 15 min) — tone-setting, can be added/removed easily

---

## Design Philosophy Applied

**Systems Perspective:**
- The intro is the first tutorial. Players learn from choices they make and information they're given. Make choices visible.
- Class selection is the first meaningful decision in the game. It should **teach the game's resource model** (HP/ATK/DEF/Mana) through example, not just flavor text.
- Difficulty is a game mechanics lever (scaling multipliers), not just flavor. Communicate the mechanics.
- Each class has a distinct playstyle (Warrior sustain, Mage burst, Rogue evasion). Make this explicit so players choose based on preference, not random guessing.

**Game Feel:**
- Intro friction (long menus, unclear options) kills retention. Every prompt should have clear purpose.
- Prestige/progression mechanics should feel rewarding. Showing advancement paths encourages return playthroughs.
- Tone matters. A one-paragraph danger setup makes the first enemy encounter feel real, not abstract.

---

## Open Questions for Design Review

1. Should difficulty recommendations include mention of permadeath/save mechanics? (Currently no permadeath system exists.)
2. Should class cards show unlock level hints for abilities? (e.g., "Warrior abilities unlocked at L1, L3, L5, L7")
3. Should the lore intro be toggleable for speedrunners? (Advanced option?)
4. Should returning players see a "Quick Start" option? (Remember last class/difficulty, skip choices?)
5. Should Rogue description be clearer about evasion being *probabilistic* vs. Warrior's *deterministic* defense?

--- FILE: coulson-intro-sequence-architecture.md ---

### 2026-02-22: Introduction Sequence Architecture Design
**By:** Coulson  
**What:** Comprehensive architectural design for game introduction sequence improvements (title, lore, character creation, UX)  
**Why:** Current intro is functional but lacks atmosphere, narrative hook, and visual polish. This design provides specific implementation guidance for Hill/Barton without deviating from established patterns.

---

## 1. TITLE SCREEN

### Visual Design
```
╔═══════════════════════════════════════════════════════════════════╗
║                                                                   ║
║    ▓█████▄  █    ██  ███▄    █   ▄████  ███▄    █  ▒███████▒    ║
║    ▒██▀ ██▌ ██  ▓██▒ ██ ▀█   █  ██▒ ▀█▒ ██ ▀█   █  ▒ ▒ ▒ ▄▀░    ║
║    ░██   █▌▓██  ▒██░▓██  ▀█ ██▒▒██░▄▄▄░▓██  ▀█ ██▒ ░ ▒ ▄▀▒░     ║
║    ░▓█▄   ▌▓▓█  ░██░▓██▒  ▐▌██▒░▓█  ██▓▓██▒  ▐▌██▒   ▄▀▒   ░    ║
║    ░▒████▓ ▒▒█████▓ ▒██░   ▓██░░▒▓███▀▒▒██░   ▓██░ ▒███████▒    ║
║     ▒▒▓  ▒ ░▒▓▒ ▒ ▒ ░ ▒░   ▒ ▒  ░▒   ▒ ░ ▒░   ▒ ▒  ░▒▒ ▓░▒░▒    ║
║     ░ ▒  ▒ ░░▒░ ░ ░ ░ ░░   ░ ▒░  ░   ░ ░ ░░   ░ ▒░ ░░▒ ▒ ░ ▒    ║
║     ░ ░  ░  ░░░ ░ ░    ░   ░ ░ ░ ░   ░    ░   ░ ░  ░ ░ ░ ░ ░    ║
║       ░       ░              ░       ░          ░      ░ ░        ║
║     ░                                                ░            ║
║                                                                   ║
║                    A Text-Based Dungeon Crawler                  ║
║                                                                   ║
║                      ⚔️  DESCEND IF YOU DARE  ⚔️                   ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

**Color Scheme:**
- Title ASCII: `BrightYellow` (gold/treasure theme)
- Subtitle: `Gray` (subtle)
- Tagline: `BrightRed` (danger/adventure theme)
- Box borders: `Cyan` (consistent with room headers)

### Technical Implementation
- New method: `IDisplayService.ShowEnhancedTitle()`
- ConsoleDisplayService implements with color-coded WriteLine calls
- Add `Systems.ColorCodes.BrightYellow` constant if missing
- Replace Program.cs line 7 `display.ShowTitle()` → `display.ShowEnhancedTitle()`

---

## 2. STORY/LORE INTRO

### Text Content (Atmospheric Hook)
After title screen, display:

```
╔═══════════════════════════════════════════════════════════════════╗
║                                                                   ║
║  The ancient dungeon of Dungnz stirs beneath the mountain.       ║
║                                                                   ║
║  Legends speak of treasures hoarded by its keeper—and of the     ║
║  countless adventurers who never returned to tell the tale.      ║
║                                                                   ║
║  You stand at the entrance, torch in hand. The stone steps       ║
║  descend into darkness. Will you claim glory... or join the      ║
║  forgotten?                                                       ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝

Press [ENTER] to begin your descent...
```

**Color Scheme:**
- Narrative text: `Cyan` (mystical/ancient feel)
- "Press ENTER" prompt: `Yellow` (call to action)

### Enhanced Design
If prestige > 0, show:

```
╔═══════════════════════════════════════════════════════════════════╗
║                      RETURNING CHAMPION                           ║
║                                                                   ║
║  Prestige Level: ⭐ 3                                             ║
║  Runs Completed: 9 / 12 (75% win rate)                           ║
║                                                                   ║
║  Bonus Stats: +3 Attack | +3 Defense | +15 HP                   ║
║                                                                   ║
║  The dungeon remembers your strength. You begin this run with    ║
║  enhanced abilities.                                              ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝

Press [ENTER] to continue...
```

**Color Scheme:**
- "RETURNING CHAMPION": `BrightYellow` (celebratory)
- Prestige stars: `Yellow`
- Stats: `Green` (positive buffs)
- Narrative: `Gray` (flavor text)

### 4a. Name Entry (Enhanced)

**Current:**
```
Enter your name, adventurer: _
```

**Enhanced:**
```
╔═══════════════════════════════════════════════════════════════════╗
║                      CHARACTER CREATION                           ║
╠═══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  What name will be carved on your tombstone... or sung by        ║
║  bards in celebration?                                            ║
║                                                                   ║
║  Enter your name: _                                               ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

**Technical:**
- New method: `IDisplayService.ShowNamePrompt()`
- Returns string (player name)
- Replaces Program.cs line 15: `var name = display.ShowNamePrompt();`

---

### 4b. Difficulty Selection (Enhanced)

**Current:**
```
Choose difficulty: [1] Casual  [2] Normal  [3] Hard
> _
```

**Enhanced:**
```
╠═══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  Choose your challenge:                                           ║
║                                                                   ║
║  [1] Casual   — For those who seek exploration over danger       ║
║                 • Enemies deal 80% damage                         ║
║                 • +20% gold and XP rewards                        ║
║                                                                   ║
║  [2] Normal   — Balanced risk and reward                          ║
║                 • Standard difficulty                             ║
║                                                                   ║
║  [3] Hard     — Only the brave or foolish dare                   ║
║                 • Enemies deal 120% damage                        ║
║                 • +50% gold and XP rewards                        ║
║                 • Boss has enhanced abilities                     ║
║                                                                   ║
║  Enter difficulty [1-3]: _                                        ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

**Color Scheme:**
- Option numbers: `Yellow`
- Difficulty names: `White` (Casual), `Cyan` (Normal), `Red` (Hard)
- Bullet points: `Gray`

**Technical:**
- New method: `IDisplayService.ShowDifficultySelection()` → Difficulty enum
- Validates input (1-3), re-prompts on invalid
- Replaces Program.cs lines 28-39

---

### 4c. Class Selection (Enhanced)

**Current:**
```
Choose your class:
[1] Warrior - High HP, defense, and attack bonus. Reduced mana.
[2] Mage - High mana and powerful spells. Reduced HP and defense.
[3] Rogue - Balanced with an attack bonus. Extra dodge chance.
> _
```

**Enhanced:**
```
╠═══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  Choose your path:                                                ║
║                                                                   ║
║  [1] ⚔️  WARRIOR                                                   ║
║      "Steel and courage. Nothing more, nothing less."             ║
║                                                                   ║
║      Starting Stats: 120 HP | 40 Mana | 13 Attack | 7 Defense   ║
║      Passive: +5% damage when HP < 50% (Last Stand)              ║
║                                                                   ║
║  [2] 🔮 MAGE                                                       ║
║      "Knowledge is power. Power is survival."                     ║
║                                                                   ║
║      Starting Stats: 90 HP | 80 Mana | 10 Attack | 4 Defense    ║
║      Passive: Spells deal +20% damage (Arcane Mastery)           ║
║                                                                   ║
║  [3] 🗡️  ROGUE                                                     ║
║      "Strike first. Strike true. Disappear."                      ║
║                                                                   ║
║      Starting Stats: 100 HP | 50 Mana | 12 Attack | 5 Defense   ║
║      Passive: +10% dodge chance (Shadow Step)                    ║
║                                                                   ║
║  Enter class [1-3]: _                                             ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

**Color Scheme:**
- Class names: `BrightRed` (Warrior), `BrightBlue` (Mage), `BrightGreen` (Rogue)
- Flavor quotes: `Gray` (italic feel)
- Stats: `Cyan`
- Passive text: `Yellow`

**Technical:**
- New method: `IDisplayService.ShowClassSelection()` → PlayerClassDefinition
- Display calculated starting stats (base Player stats + class bonuses + prestige)
- Validates input (1-3), re-prompts on invalid
- Replaces Program.cs lines 41-54

---

## 5. SEED INPUT (REVISED PLACEMENT)

### Current Flow
Title → Prestige → Name → **Seed** → Difficulty → Class → Start

### Recommended Flow
Title → Prestige → Name → Difficulty → Class → **Seed** → Start

**Rationale:**
- Seed is advanced/technical feature (most players skip)
- Character identity (name/class) should come first for narrative flow
- Seed feels like "advanced options" — place last

### Enhanced Presentation

**Option A: Collapsed by default**
```
╠═══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  Advanced: Enter a seed for reproducible runs                    ║
║            (or press ENTER for random seed)                       ║
║                                                                   ║
║  Seed: _                                                          ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

**Option B: Hidden unless requested**
Skip seed prompt entirely; generate random seed.
Display seed on game start:
```
═══════════════════════════════════════════════════════════════════
Your adventure begins... [Seed: 847293]
(Share this seed to replay the same dungeon layout)
═══════════════════════════════════════════════════════════════════
```

**Recommendation:** Use Option B — most players don't care about seeds until they have a memorable run.

**Technical:**
- Keep seed generation logic in Program.cs (lines 17-26)
- Remove seed prompt
- Display seed after class selection: `display.ShowMessage($"⚙️ Seed: {actualSeed} (share this to replay the same dungeon)")`

---

## 6. FLOW SEQUENCE (FINAL ARCHITECTURE)

### Recommended Order
1. **ShowEnhancedTitle()** — Full ASCII art, tagline
2. **ShowIntroNarrative()** — 3-4 sentence atmospheric hook, wait for Enter
3. **ShowPrestigeIntro(prestige)** — If prestige > 0, show returning champion screen, wait for Enter
4. **ShowNamePrompt()** — Character creation header, name entry
5. **ShowDifficultySelection()** — Multi-line difficulty descriptions with mechanics
6. **ShowClassSelection()** — Class flavor quotes, calculated starting stats, passive abilities
7. **Display generated seed** — One-line message after class selection
8. **Begin dungeon** — Transition to first room

**Total Time:** ~60-90 seconds (skip-friendly with Enter keypresses)

---

## 7. ARCHITECTURAL IMPLEMENTATION PLAN

### Phase 1: Interface Definition (Coulson / Hill)
- Extend `IDisplayService` with new methods:
  - `void ShowEnhancedTitle()`
  - `void ShowIntroNarrative()`
  - `string ShowNamePrompt()`
  - `Difficulty ShowDifficultySelection()`
  - `PlayerClassDefinition ShowClassSelection(PrestigeData? prestige)` ← needs prestige to calculate display stats
- Add `void ShowPrestigeIntro(PrestigeData data)` to `PrestigeSystem` (static method, display injection)

### Phase 2: ConsoleDisplayService Implementation (Hill)
- Implement 4 new display methods
- Use existing `ColorCodes` constants (add BrightYellow if missing)
- Include input validation loops for difficulty/class selection
- Calculate display stats for class selection: base Player stats + class bonuses + prestige bonuses

### Phase 3: Program.cs Integration (Hill)
- Replace lines 7-54 with new display method calls
- Move seed display to after class selection
- Remove seed prompt (use silent random generation)
- Pass prestige data to ShowClassSelection for stat calculation

### Phase 4: Testing (Romanoff)
- Unit tests for new display methods (TestDisplayService verification)
- Integration test: full intro sequence flow
- Verify prestige stat display matches actual applied bonuses
- Edge cases: prestige=0, empty name input, invalid difficulty/class input

---

## 8. ARCHITECTURE DECISIONS RATIONALE

### Stay in Program.cs vs Extract IntroSequence Service?
**Decision:** Extract to `Systems.IntroSequenceManager` in future refactor, but not required for this feature.

**Rationale:**
- Program.cs currently 83 LOC with intro sequence
- Extracting intro logic would move it to ~50 LOC
- Not a critical refactor for v3 — defer to v4 "Launcher/Menu System" work
- Current approach: Keep in Program.cs, use DisplayService methods to encapsulate presentation

**If extracted later:**
```csharp
var intro = new IntroSequenceManager(display, prestigeSystem);
var config = intro.RunIntroSequence(); // returns (name, seed, difficulty, class)
```

### Why Not Add IntroService?
- DisplayService already handles presentation
- IntroSequence would just orchestrate DisplayService calls
- Adds abstraction layer without clear value (no alternative intro flows planned)
- Keep it simple: orchestration stays in Program.cs until complexity justifies extraction

### Input Validation: DisplayService or Caller?
**Decision:** DisplayService owns validation for prompted inputs.

**Rationale:**
- Prompts like ShowDifficultySelection() return validated Difficulty enum
- Caller doesn't need to handle invalid input — display layer loops until valid
- Consistent with ReadPlayerName() pattern (display owns UX loop)

### Calculated Stats Display for Class Selection
**Problem:** Class selection should show final starting stats (base + class + prestige), but this requires prestige data.

**Solution:** `ShowClassSelection(PrestigeData? prestige)` parameter
- DisplayService calculates display values: base Player stats + class modifiers + prestige bonuses
- Does NOT apply stats — that's still Program.cs responsibility
- Allows accurate preview without duplicating stat application logic

**Alternative Considered:** Pass Player instance to ShowClassSelection
- Rejected: Player shouldn't exist before class selection completes
- Rejected: Violates separation — DisplayService shouldn't mutate models

---

## 9. FILE IMPACT SUMMARY

**New Files:** None  
**Modified Files:**
- `Display/IDisplayService.cs` — Add 4 new method signatures
- `Display/ConsoleDisplayService.cs` — Implement 4 new methods (~150 LOC)
- `Systems/PrestigeSystem.cs` — Add ShowPrestigeIntro static method (~30 LOC)
- `Systems/ColorCodes.cs` — Add BrightYellow constant if missing
- `Program.cs` — Replace lines 7-54 with new display method calls (~20 LOC)

**Test Files:**
- `Dungnz.Tests/Display/ConsoleDisplayServiceTests.cs` — Unit tests for new methods (~100 LOC)
- `Dungnz.Tests/Systems/PrestigeSystemTests.cs` — Test prestige intro display (~20 LOC)
- `Dungnz.Tests/Integration/IntroSequenceTests.cs` — NEW FILE, integration tests (~80 LOC)

---

## 10. ACCEPTANCE CRITERIA

**Visual Quality:**
- ✅ Enhanced ASCII title uses full terminal width
- ✅ Consistent box-drawing characters across all intro screens
- ✅ Color-coded difficulty/class names improve scannability
- ✅ Prestige screen celebrates returning players with stats

**UX Flow:**
- ✅ Intro narrative provides atmospheric hook without blocking
- ✅ Seed generation silent by default (power users can note seed from start message)
- ✅ Difficulty descriptions include mechanical impact (damage %, rewards)
- ✅ Class selection shows calculated starting stats (base + class + prestige)

**Technical:**
- ✅ No hardcoded Console.Write in Program.cs (all via DisplayService)
- ✅ Input validation handled by DisplayService methods
- ✅ Prestige stat display matches actual applied bonuses
- ✅ All new methods covered by unit tests (>90% branch coverage)

**Backward Compatibility:**
- ✅ Existing Player/PrestigeData models unchanged
- ✅ Save files unaffected
- ✅ Game loop integration unchanged

---

**Estimated Effort:** 6-8 hours (Hill: 5h implementation, Romanoff: 2h testing, Coulson: 1h review)  
**Risk:** Low — pure presentation layer, no game logic changes  
**Dependencies:** None — can start immediately  
**Merge Readiness:** Requires code review (display content/tone), UX feedback (skip-friendliness)

--- FILE: coulson-pr223-review.md ---
# PR #223 Review — Barton: ColorizeDamage LastIndexOf fix + README

**Reviewer:** Coulson (Lead)
**Branch:** `squad/220-colorize-damage-fix`
**Verdict:** ✅ APPROVED

## Code Review

### `ReplaceLastOccurrence` helper (CombatEngine.cs)
- Clean `private static` helper using `LastIndexOf` — correct approach
- Null-safe: returns `source` unchanged if `find` not found
- XML doc accurately explains why last-occurrence is the right semantic
- Both call sites updated: normal damage and crit path

### `ColorizeDamage` changes
- `string.Replace` → `ReplaceLastOccurrence` on both code paths (normal + crit)
- Preserves existing colorization logic (BrightRed for damage, Yellow+Bold for crits)
- Fix directly addresses Issue #220: when damage number appears in enemy name, only the trailing (actual damage) occurrence is colorized

### README update
- Accurately documents the `LastIndexOf` behaviour
- Placed in the correct section (Display & Colours)
- Concise, informative

### Build & Tests
- ✅ All 267 tests pass on this branch
- No new warnings introduced

## Decision
Merge to master. Clean, minimal, correct fix.

--- FILE: coulson-pr224-review.md ---
# PR #224 Review — Coulson

**PR:** #224 `squad/219-221-222-display-fixes`
**Verdict:** ✅ APPROVED
**Date:** 2025-07-15

## Summary

All three follow-up fixes from PR #218 code review are correctly addressed:

### #219 — README health threshold table
The table now matches the actual `HealthColor()` switch expression:
- `> 70%` → Green
- `40–70%` → Yellow
- `20–40%` → Red
- `≤ 20%` → Bright Red

Verified against `Systems/ColorCodes.HealthColor()`. ✅

### #221 — `ShowEquipmentComparison` right-border alignment
Padding now uses `StripAnsiCodes()` to compute visible character width before calculating whitespace. The `attackPrefix.Length - 1` correctly excludes the left `║` border from the inner-width calculation, and `innerWidth = 39` matches the box geometry. ✅

### #222 — `ShowPlayerStats` refactored to use `ShowColoredStat()`
All six inline `Colorize` / manual ANSI calls replaced with `ShowColoredStat(label, value, color)`. HP and Mana use dynamic threshold colors; Attack/Defense/Gold/XP use static colors. Label padding via `{label,-8}` in the helper is consistent. ✅

### Bonus: #220 — `ColorizeDamage` last-occurrence fix
`ReplaceLastOccurrence` helper added to `CombatEngine` — correct `LastIndexOf`-based implementation. Both call sites updated. ✅

### Test quality
- Proper xUnit `[Fact]` tests with clear Arrange-Act-Assert structure
- `ColorizeDamage_NormalCase_OnlyColorizesDamageNumber` — baseline: single occurrence, correct colorization
- `ColorizeDamage_EdgeCase_OnlyLastOccurrenceIsColorized_WhenDamageAppearsInEnemyName` — the #220 edge case: enemy named "5" dealing 5 damage, verifies only the trailing "5" is colorized
- `CountColorized` helper is clean and reusable
- Uses `RawCombatMessages` (new property on FakeDisplayService) to inspect ANSI-intact output — good design

### FakeDisplayService change
- Added `RawCombatMessages` list that stores messages before ANSI stripping
- Minimal, non-breaking addition — existing `CombatMessages` (stripped) still works for all other tests

## ShowEquipmentComparison Alignment Tests (ShowEquipmentComparisonAlignmentTests.cs) — ❌ FAIL (2/2)

### Test quality — tests are CORRECT
- `ShowEquipmentComparison_AllBoxLines_HaveConsistentVisualWidth_WhenDeltasAreColoured` — verifies every `║`-prefixed line matches border width after ANSI stripping
- `ShowEquipmentComparison_RightBorderChar_AppearsAtConsistentColumn_WhenOnlyAttackChanges` — mixed case: one delta row, one plain row
- Both use `IDisposable` pattern with `StringWriter` console capture — clean
- `BoxWidth` helper correctly derives expected width from the `╔═══╗` border line

### Why they fail
The tests correctly detect a **remaining alignment bug**: master's #221 fix (PR #224) corrected the Attack/Defense delta rows but did NOT fix the non-delta content rows:
- `║ Current:  {name,-27}║` → produces 40-char lines
- `║ New:      {name,-27}║` → produces 40-char lines
- Border `╔═══...═══╗` → 41 chars

The padding should be `,-28` (not `,-27`) to match the 41-char border width. This is a **production code bug**, not a test bug.

### Action needed
**Follow-up required:** Fix `ShowEquipmentComparison` non-delta rows to use correct padding (`,-28`). File as follow-up to #221 or have Barton fix before merge.

## Test Results Summary
- ColorizeDamage tests: 2/2 PASS ✅
- Alignment tests: 2/2 FAIL ❌ (correct tests, incomplete production fix)
- All other tests: 269/269 PASS ✅

## Decision
Tests are well-structured, correctly written, and exercise the right edge cases. Approve the test code. The alignment test failures are a signal that the #221 fix needs a follow-up patch to the `Current:`/`New:` rows in `ShowEquipmentComparison`. **Do not merge until alignment fix lands** (or tests will break CI).

--- FILE: coulson-pr226-review.md ---
# PR #226 Review — Hill: Box alignment follow-up (#221)

**Reviewer:** Coulson (Lead)
**Branch:** `squad/221-box-alignment-followup`
**Verdict:** ✅ APPROVED

## Summary

Targeted one-commit fix: changes item name row padding from `,-27` to `,-28` in `ShowEquipmentComparison`.

## Verification

### All three occurrences fixed ✅
1. `oldItem.Name,-28` — current item (equipped)
2. `"(none)",-28` — current item (empty slot fallback)
3. `newItem.Name,-28` — new item

### Width math ✅
- Border line `╔═══...═══╗`: `╔` + 39×`═` + `╗` = 41 visible chars
- Content line: `║` (1) + ` Current:  ` (11) + name (28 padded) + `║` (1) = 41 ✅
- Previous `,-27` produced 40-char lines — off by one vs 41-char border

### Scope ✅
- Single file changed: `Display/DisplayService.cs`
- Three lines modified, all in `ShowEquipmentComparison`
- No logic changes, no interface changes, no test changes
- Directly addresses the issue flagged in PR #225 review

## Decision
Merge to master. This completes the #221 alignment fix and unblocks Romanoff's alignment tests (PR #225).

— reviewed by Coulson

--- FILE: hill-intro-display-design.md ---

### 2026-02-22: Intro display design notes
**By:** Hill  
**What:** Display engineering perspective on intro improvements  
**Why:** Planning session for intro UX improvements

---

## Current State Assessment

### What's Visually Weak in ShowTitle() and Program.cs

The current intro flow has three distinct problems:

1. **Minimal title screen** — ShowTitle() renders a plain box with generic text:
   ```
   ╔═══════════════════════════════════════╗
   ║         DUNGEON CRAWLER               ║
   ║      A Text-Based Adventure           ║
   ╚═══════════════════════════════════════╝
   ```
   This feels flat and uninviting. It's functionally clear but lacks personality and visual impact.

2. **Text-dump UI for class and difficulty** — Both selections are presented as wall-of-text list options:
   - Class selection: 3 plain text lines, no visual hierarchy or stat preview
   - Difficulty selection: Single inline text "[1] Casual [2] Normal [3] Hard" with no visual distinction or guidance

3. **No stat context for choice** — Players can't see how their choice affects starting stats before committing. Class descriptions are present but don't show actual numbers or visual comparison.

4. **Missing color coding** — Difficulty and class selections are monochrome text. ColorCodes system is available but not used in intro.

5. **No visual separation** — Name input, seed input, and difficulty/class selection flow together in a featureless stream of prompts.

---

## Design 1: Enhanced Title Screen with ASCII Art

Instead of a simple bordered box, create a visually striking title screen that sets the dungeon tone:

```
████████████████████████████████████████████████
██                                              ██
██    ██████╗ ██╗   ██╗███╗   ██╗ ██████╗███████╗
██    ██╔══██╗██║   ██║████╗  ██║██╔════╝██╔════╝
██    ██║  ██║██║   ██║██╔██╗ ██║██║     █████╗  
██    ██║  ██║██║   ██║██║╚██╗██║██║     ██╔══╝  
██    ██████╔╝╚██████╔╝██║ ╚████║╚██████╗███████╗
██    ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝╚══════╝
██                                              ██
██          Crawl through darkness.             ██
██            Survive the depths.               ██
██                                              ██
████████████████████████████████████████████████
```

**Rationale:**
- Larger, more memorable visual impact
- Sets the dungeon/dark mood through ASCII art
- Tagline communicates the core gameplay loop
- Still readable in all terminal widths (50-80 char safe)

**Implementation considerations:**
- Should remain console-safe (Unicode box-drawing where possible, ASCII fallback)
- Use ColorCodes to tint the title (optional: dark gray for border, bright white for title text, yellow for tagline)
- Center the output using padding logic (need ANSI-aware padding, similar to StripAnsiCodes pattern)

---

## Design 2: Class Selection as Formatted Panels

Replace the 3-line text dump with side-by-side visual class cards:

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                       SELECT YOUR CLASS (1-3)                            ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  [1] WARRIOR                [2] MAGE                   [3] ROGUE         ║
║  ─────────────────────      ─────────────────────      ─────────────────║
║                                                                           ║
║  HP:      +25    [█████░]   Mana:   +150   [░░░░█]   HP:      +10  [███ ║
║  Attack:  +5     [████░░]   Attack: -5     [█░░░░]   Attack:  +8   [███ ║
║  Defense: +10    [█████░]   Defense:-10    [█░░░░]   Defense: +5   [███ ║
║  Mana:    -50    [░░░░░░]   Mana:   +150   [█████░]   Mana:    +0   [███ ║
║                                                                           ║
║  Trait: Tank mechanic      Trait: Spell damage      Trait: Dodge bonus  ║
║         with armor bonus          with mana pool          (+10%)         ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

**Rationale:**
- Shows stat changes side-by-side, making comparison immediate
- Simple bar chart (using ░ and █) provides visual representation of stat impact
- Color-coded stat lines use existing ColorCodes (Red for attack, Cyan for defense, Blue for mana, Green for HP)
- Trait descriptions contextualize the mechanical differences
- Number selection (1-3) maps to column position visually

**Implementation approach:**
- New method: `ShowClassSelection()` that displays all three classes with stats
- Use `ShowColoredStat()` pattern internally for each stat line
- Build the bars using a helper function (e.g., `BuildStatBar(current, max, bonus)`)
- Position columns with padding; StripAnsiCodes needed for right-alignment with colored values

---

## Design 3: Difficulty Selection with Color-Coded Panels

Replace inline text with a three-panel difficulty chooser:

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                        SELECT DIFFICULTY (1-3)                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  [1] CASUAL                [2] NORMAL               [3] HARD             ║
║  ───────────────────────   ───────────────────────  ───────────────────  ║
║                                                                           ║
║  ✓ Forgiving              ✓ Balanced encounter    ✓ Punishing difficulty║
║  ✓ Abundant resources     ✓ Standard loot drops   ✓ Rare item drops    ║
║  ✓ Weaker enemies         ✓ Standard enemies      ✓ Stronger enemies   ║
║  ✓ Perfect for learning   ✓ Original vision       ✓ High risk/reward   ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

**Color scheme:**
- Panel border: Standard (gray or white)
- [1] CASUAL: Green color for the title and checkmarks
- [2] NORMAL: Yellow color (default/recommended)
- [3] HARD: Red color (danger/hardcore)

**Rationale:**
- Consistent with ColorCodes system (Green=safe, Yellow=normal, Red=dangerous)
- Bullet points (✓) make features scannable
- Three columns encourage deliberation before selection
- Clear positioning matches class selection layout

**Implementation approach:**
- New method: `ShowDifficultySelection()` 
- Use `ShowColoredMessage()` for each panel header with appropriate color
- Checkmark feature lists use ShowMessage or direct Console.WriteLine with indentation

---

## Design 4: Prestige Display and Flow Integration

The prestige bonus display exists but appears cold. Enhance it:

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                         ★ PRESTIGE BONUSES ★                            ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  Prestige Level: 3    ✧ Ascended 3 times  ✧ Earned during playthroughs  ║
║                                                                           ║
║  Starting Bonuses:                                                       ║
║    +5 Attack      +3 Defense      +25 Max HP                            ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

Use BrightWhite or Yellow for the star (★) and ✧ symbols, and colorize the bonus values:
- Attack: BrightRed
- Defense: Cyan
- HP: Green

**Rationale:**
- Celebrates player progression without overwhelming
- Matches the card/panel style of other intro elements
- Prestige visually stands apart (star symbols)

---

## IDisplayService Methods to Add/Modify

### New Methods Required

1. **ShowIntroTitle()** — Display the enhanced ASCII art title screen
   ```csharp
   /// <summary>
   /// Displays an enhanced title screen with ASCII art and tagline.
   /// Called once at game start before player name prompt.
   /// </summary>
   void ShowIntroTitle();
   ```

2. **ShowClassSelection()** — Display class cards with stat comparisons
   ```csharp
   /// <summary>
   /// Displays three class option panels (Warrior, Mage, Rogue) with stats,
   /// bars, and trait descriptions. Used for player class selection at start.
   /// Returns when player has made a valid choice (1-3).
   /// </summary>
   /// <returns>The player's class choice (1 = Warrior, 2 = Mage, 3 = Rogue).</returns>
   int ShowClassSelection();
   ```

3. **ShowDifficultySelection()** — Display difficulty panels with color coding
   ```csharp
   /// <summary>
   /// Displays three difficulty option panels (Casual, Normal, Hard) with
   /// color-coded headers and feature descriptions. Used for difficulty
   /// selection at start.
   /// </summary>
   /// <returns>The player's difficulty choice (1 = Casual, 2 = Normal, 3 = Hard).</returns>
   int ShowDifficultySelection();
   ```

4. **ShowPrestigeDisplay(PrestigeSystem prestige)** — Replace raw PrestigeSystem.GetPrestigeDisplay() call
   ```csharp
   /// <summary>
   /// Displays prestige bonuses in a formatted panel with stars and color-coded
   /// stat bonuses. Called at game start if player has prestige level > 0.
   /// </summary>
   /// <param name="prestige">The prestige system instance with level and bonus data.</param>
   void ShowPrestigeDisplay(PrestigeSystem prestige);
   ```

5. **ShowSeedPrompt()** — Formatted seed entry prompt
   ```csharp
   /// <summary>
   /// Displays a formatted prompt for seed entry with explanation of reproducibility.
   /// Returns the player's input (empty string if they press Enter).
   /// </summary>
   /// <returns>The player's seed input, or empty string for random.</returns>
   string ShowSeedPrompt();
   ```

### Modified Methods

1. **ShowTitle()** — Keep existing method for backward compatibility
   - Can remain as a simple variant, or delegate to ShowIntroTitle()
   - Document that ShowIntroTitle() is preferred for intro flow

2. **ReadPlayerName()** — Optional enhancement
   - Current implementation is fine, but could add a decorative border:
   ```csharp
   Enter your name, adventurer:
   > 
   ```

### Helper Methods (Internal to ConsoleDisplayService)

Not part of IDisplayService, but needed internally:

```csharp
/// <summary>Renders a horizontal stat bar using ░ (empty) and █ (full) chars.</summary>
private static string BuildStatBar(int value, int maxValue);

/// <summary>Strips ANSI codes and pads text to a fixed width for alignment.</summary>
private static string PadColoredText(string text, int width);

/// <summary>Centers text in a given width, accounting for ANSI codes.</summary>
private static string CenterText(string text, int width);
```

---

## ANSI-Aware Padding Pattern

The existing `StripAnsiCodes()` utility in ColorCodes is crucial for intro UI. All formatted panels will need it:

1. **Title centering** — StripAnsiCodes to measure true character width, then calculate padding
2. **Class stat alignment** — When displaying colored stat values, strip codes to find visible width for right-alignment
3. **Difficulty panel headers** — Strip codes before padding to correct columns

Example pattern (similar to ShowEquipmentComparison):
```csharp
var coloredText = $"{ColorCodes.Red}{value}{ColorCodes.Reset}";
var visibleLength = ColorCodes.StripAnsiCodes(coloredText).Length;
var padding = totalWidth - visibleLength;
Console.WriteLine(coloredText + new string(' ', padding));
```

This pattern must be applied to every formatted line in the new intro screens.

---

## Integration with Program.cs

Current flow:
```csharp
display.ShowTitle();
var name = display.ReadPlayerName();
// ... seed input (text dump)
// ... difficulty input (text dump)
// ... class input (text dump)
```

New flow:
```csharp
display.ShowIntroTitle();  // New method
display.ShowMessage("");   // Blank line for breathing room

var name = display.ReadPlayerName();

var seed = display.ShowSeedPrompt();  // New method
int? parsedSeed = int.TryParse(seed, out var s) ? s : null;
var actualSeed = parsedSeed ?? new Random().Next(100000, 999999);
display.ShowMessage($"Seed: {actualSeed}");

var difficulty = display.ShowDifficultySelection();  // New method
var chosenDifficulty = difficulty switch { 1 => Casual, 3 => Hard, _ => Normal };

var classChoice = display.ShowClassSelection();  // New method
var chosenClass = classChoice switch { 2 => Mage, 3 => Rogue, _ => Warrior };

// ... rest of setup
```

---

## Visual Rendering Principles

### Terminal Safety
- Assume 80-char width minimum (most terminals)
- Use box-drawing characters (╔═╗║╚╝, etc.) available in UTF-8
- Test with common terminal widths (80, 120, 160)
- Provide ASCII fallback if needed (e.g., `+--+` instead of `╔══╗`)

### Color Use
Leverage existing ColorCodes:
- HP stat lines: `ColorCodes.HealthColor()` (based on percentage)
- Attack values: `ColorCodes.BrightRed`
- Defense values: `ColorCodes.Cyan`
- Mana values: `ColorCodes.Blue`
- Gold/rewards: `ColorCodes.Yellow`
- Trait descriptions: `ColorCodes.Cyan` or gray for flavor text
- Difficulty markers: Green (Casual), Yellow (Normal), Red (Hard)

### Spacing & Layout
- Blank lines between sections (visual breathing room)
- Consistent indentation (2 spaces for most content)
- Box borders for grouped information (class/difficulty panels)
- Right-aligned stats within columns using ANSI-aware padding

---

## Implementation Priority

**Phase 1 (Minimum Viable):**
1. ShowIntroTitle() — Enhanced title with ASCII art (2 hours)
2. ShowDifficultySelection() — Color-coded difficulty panel (1.5 hours)
3. ShowClassSelection() — Class cards with stat bars (3 hours)

**Phase 2 (Polish):**
1. ShowPrestigeDisplay() — Prestige panel with stars (1 hour)
2. ShowSeedPrompt() — Formatted seed input (30 minutes)
3. Prestige banner integration into Program.cs (30 minutes)

**Phase 3 (Future):**
1. ASCII art animation (fade-in effect for title)
2. Animated stat bars (filling effect during class selection)
3. Sound/terminal bell (low priority, optional)

---

## Testing & Validation

Key scenarios to verify:
1. Title screen renders in 80, 120, and 160 char widths
2. Class panels center correctly with colored stat values
3. ANSI-aware padding handles all color code combinations
4. Difficulty selection correctly maps inputs (1→Casual, 2→Normal, 3→Hard)
5. Prestige display shows only if prestige.PrestigeLevel > 0
6. All colored text respects ColorCodes.Reset properly (no color bleed)

---

## Summary

The intro improvements transform a flat, text-heavy startup experience into a visually engaging onboarding flow:

- **Title:** From a simple box to an ASCII art dungeon crawler banner
- **Class selection:** From 3 text lines to 3 stat-comparison cards
- **Difficulty selection:** From inline text to 3 color-coded feature panels
- **Prestige display:** From raw console output to a celebration banner

All changes respect the existing DisplayService architecture, leverage ColorCodes for consistency, and use ANSI-aware padding to ensure terminal safety. The new IDisplayService methods are cohesive, focused, and backward-compatible.

--- FILE: hill-intro-sequence-extraction.md ---

### 2026-02-21: Intro Sequence Extraction Architecture

**By:** Hill  
**What:** Architectural guidance for extracting intro sequence from Program.cs  
**Why:** Improve separation of concerns, testability, and maintainability of game initialization

---

## Recommendation: Extract to `GameSetupService`

**Decision:** Extract intro sequence to `Systems/GameSetupService.cs` with clear contract returning initialization bundle.

**Rationale:**
1. **Separation of Concerns:** Program.cs should be thin entry point, not business logic
2. **Testability:** Setup logic is I/O-heavy but still has testable branches (difficulty mapping, class selection, prestige application)
3. **Reusability:** Load game feature will need similar initialization without re-prompting
4. **Consistency:** Matches existing pattern (NarrationService, PrestigeSystem, etc. in Systems/)

---

## Proposed Contract

```csharp
namespace Dungnz.Systems;

/// <summary>
/// Handles initial game setup including player creation, class selection,
/// difficulty configuration, and prestige bonus application.
/// </summary>
public class GameSetupService
{
    private readonly IDisplayService _display;

    public GameSetupService(IDisplayService display)
    {
        _display = display;
    }

    /// <summary>
    /// Runs the full intro sequence and returns configured game state.
    /// </summary>
    /// <returns>Bundle containing configured Player, seed, and difficulty settings.</returns>
    public GameSetup RunIntroSequence()
    {
        _display.ShowTitle();
        
        // Prestige display
        var prestige = PrestigeSystem.Load();
        if (prestige.PrestigeLevel > 0)
        {
            _display.ShowMessage(PrestigeSystem.GetPrestigeDisplay(prestige));
        }
        
        // Player name
        var name = _display.ReadPlayerName();
        
        // Seed selection
        var seed = PromptForSeed();
        
        // Difficulty selection
        var difficulty = PromptForDifficulty();
        
        // Class selection
        var classDefinition = PromptForClass();
        
        // Create and configure player
        var player = CreatePlayer(name, classDefinition, prestige);
        
        return new GameSetup(player, seed, difficulty);
    }

    private int PromptForSeed() { /* ... */ }
    private DifficultySettings PromptForDifficulty() { /* ... */ }
    private PlayerClassDefinition PromptForClass() { /* ... */ }
    private Player CreatePlayer(string name, PlayerClassDefinition classDef, PrestigeData prestige) { /* ... */ }
}

/// <summary>
/// Immutable bundle of initial game state returned by setup sequence.
/// </summary>
public record GameSetup(Player Player, int Seed, DifficultySettings Difficulty);
```

---

## Integration Pattern

**Program.cs becomes:**

```csharp
var display = new ConsoleDisplayService();
var setupService = new GameSetupService(display);

// Run intro sequence
var setup = setupService.RunIntroSequence();

// Initialize game systems
EnemyFactory.Initialize("Data/enemy-stats.json", "Data/item-stats.json");
var generator = new DungeonGenerator(setup.Seed);
var (startRoom, _) = generator.Generate(difficulty: setup.Difficulty);

var inputReader = new ConsoleInputReader();
var combat = new CombatEngine(display, inputReader);
var gameLoop = new GameLoop(display, combat, inputReader, seed: setup.Seed, difficulty: setup.Difficulty);

gameLoop.Run(setup.Player, startRoom);
```

**Clean separation:**
- GameSetupService: Owns intro flow, prompting, player creation
- Program.cs: Thin orchestrator wiring services together
- GameLoop: Owns main loop, receives configured state

---

## Testability Strategy

**What's testable:**
- Difficulty enum mapping (1→Casual, 2→Normal, 3→Hard, default→Normal)
- Class definition selection (1→Warrior, 2→Mage, 3→Rogue, default→Warrior)
- Player stat application (class bonuses, prestige bonuses)
- Seed parsing (valid int vs null → random)

**What's NOT worth testing:**
- Raw Console.ReadLine() interaction (covered by integration tests)
- Display method calls (stub IDisplayService in unit tests)

**Test approach:**
```csharp
// Example: Test class bonus application
var display = new StubDisplayService(["MyName", "", "1", "2"]); // Answers: name, seed, casual, mage
var service = new GameSetupService(display);
var setup = service.RunIntroSequence();

Assert.Equal("Mage", setup.Player.Class);
Assert.Equal(80, setup.Player.MaxHP); // Base 100 + Mage -20
Assert.Equal(150, setup.Player.MaxMana); // Base 100 + Mage +50
```

---

## Where Prestige Bonuses Apply

**Decision:** Apply in `CreatePlayer()` method AFTER class bonuses.

**Order of operations:**
1. Create Player with base stats (HP=100, Attack=10, Defense=5)
2. Apply class bonuses (e.g., Mage: MaxHP-20, MaxMana+50)
3. Apply prestige bonuses (e.g., PrestigeLevel 2: +2 Attack, +1 Defense, +10 HP)
4. Set HP = MaxHP and Mana = MaxMana

**Rationale:**
- Prestige bonuses are additive on top of chosen build
- Prestige affects "starting stats" not "base stats" (class choice comes first)
- Current Program.cs already does this correctly (lines 69-75)

---

## Alternative Considered: Builder Pattern

```csharp
var setup = new GameSetupBuilder(display)
    .WithPrestigeDisplay()
    .PromptPlayerName()
    .PromptSeed()
    .PromptDifficulty()
    .PromptClass()
    .ApplyPrestigeBonuses()
    .Build();
```

**Rejected because:**
- Over-engineered for linear flow with no branching
- Harder to read than straight procedural code
- No benefit for testing (still need to mock display)
- Fluent API obscures the simple sequential nature of setup

---

## Alternative Considered: Keep in Program.cs

**Rejected because:**
- Program.cs is 83 lines, 70% intro sequence (lines 7-75)
- Intro logic mixed with wiring logic (EnemyFactory.Initialize, DungeonGenerator, GameLoop)
- Harder to locate "where game loop starts" when scanning Program.cs
- Load game will duplicate setup logic (needs Player without re-prompting)

---

## File Structure After Extraction

```
TextGame/
├── Program.cs                    (15 lines: setup, wire, run)
├── Display/
│   ├── IDisplayService.cs
│   └── ConsoleDisplayService.cs  (no changes)
├── Engine/
│   └── GameLoop.cs               (no changes)
├── Systems/
│   ├── GameSetupService.cs       (NEW: 120 lines intro logic)
│   └── PrestigeSystem.cs         (no changes)
└── Models/
    ├── Player.cs                 (no changes)
    ├── PlayerClass.cs            (assumes exists)
    └── Difficulty.cs             (no changes)
```

---

## Decision: Do NOT Extract Yet

**Important:** This is ARCHITECTURAL GUIDANCE ONLY.

**Why not implement now:**
- Need team consensus (Copilot/Coulson approval)
- Current Program.cs works and isn't blocking anyone
- Extraction is refactoring, not new feature
- Should be done as dedicated work item, not side quest

**When to extract:**
- When load game feature is being implemented (avoid duplication)
- When team agrees Program.cs is too long
- When intro flow becomes more complex (e.g., tutorial mode, multi-page setup)

**Action:** Document this decision in inbox for Coulson to review.

--- FILE: romanoff-intro-qa-notes.md ---
# 2026-02-22: Intro QA and Testability Notes

**By:** Romanoff  
**What:** QA perspective on intro improvements  
**Why:** Planning session for intro UX improvements

---

## Executive Summary

The current intro sequence in `Program.cs` (lines 7–82) has **critical testability issues** that will make quality assurance difficult as we expand it. The top-level script mixes game setup, user input, player creation, and engine initialization in a single unmanaged flow. Adding richer intro features (lore screens, class descriptions, difficulty explanations) without refactoring will increase regression risk and blind spots.

**Recommendation:** Extract intro to a dedicated `IntroSequence` class with dependency injection. This enables unit testing of player creation logic, validation, and edge cases without console I/O.

---

## 1. Current Testability Assessment: Program.cs Intro Logic

### What's There
Lines 7–82 of `Program.cs` execute:
1. `ShowTitle()` — Display splash screen
2. `ReadPlayerName()` — Read and validate player name
3. Seed selection — Read optional integer seed
4. Difficulty selection — Menu selection (1/2/3 → Casual/Normal/Hard)
5. Class selection — Menu selection (1/2/3 → Warrior/Mage/Rogue)
6. Player initialization — Construct `Player` object, apply class bonuses, prestige bonuses
7. Factory initialization — Load `enemy-stats.json`, `item-stats.json`
8. Engine startup — Create `DungeonGenerator`, `GameLoop`, run game

### Testability Risks

| **Risk** | **Severity** | **Impact** |
|----------|------------|-----------|
| **No entry point to intro logic** | HIGH | Cannot test player creation without console I/O. All intro logic is baked into top-level script. |
| **Console.ReadLine() hardcoded** | CRITICAL | Line 20, 31, 47: `Console.ReadLine()` called directly; cannot inject test input. Manual testing only. |
| **Input validation absent** | HIGH | Empty name → crashes ReadPlayerName(). Invalid class/difficulty → silent fallback to defaults (lines 32-36, 48-52). No error messages. |
| **Prestige system side effect** | MEDIUM | Lines 10-12, 69-75: Loads prestige state globally; affects player stats without validation. Hard to isolate in tests. |
| **Seed state management** | MEDIUM | Lines 21-24: Random seed generated and printed, then passed to GameLoop. No record of which seed was used; hard to verify determinism. |
| **Player created at script level** | MEDIUM | Line 56: Player object created inline with hardcoded name and stat modifications. Cannot test player creation in isolation. |
| **Magic number dependencies** | LOW | Class/difficulty selection hardcoded to 1/2/3 strings. Adding new classes requires editing multiple switch statements. |

### Edge Cases NOT Tested in Current Code

1. **Empty player name** — `ReadPlayerName()` has fallback (`?? "Unnamed"`), but no test ensures this works.
2. **Invalid seed input** — "abc" → silently treated as null, random seed used. Is this intentional? Unclear.
3. **Invalid class/difficulty** — Bad input → silent default (Warrior / Normal). Should we error? Log? Retry?
4. **Prestige loading failure** — What if prestige save is corrupted? Currently no error handling.
5. **Factory initialization failure** — What if `enemy-stats.json` is missing? Currently crashes without hint.
6. **Name with special characters** — "Player\n" or "Player\0" — will it break? Untested.
7. **Very long player name** — 1000 characters — display truncation? Validation? Untested.

---

## 2. IDisplayService Interface: Testing Implications

### Current IDisplayService Methods
- `ShowTitle()` — splash, no return
- `ReadPlayerName()` — returns string ✓ testable
- `ShowMessage(string)` — output, void
- `ShowError(string)` — error output, void
- `ShowCommandPrompt()` — prompt symbol, void

### Proposed New Methods for Improved Intro

#### Option A: Display-only (return void)
```csharp
void ShowClassSelection();
void ShowDifficultyMenu();
void ShowLore();
```
**Problem:** Tests cannot verify that the player saw the menu. Can only test the side effect (console output).

#### Option B: Return validated choice
```csharp
PlayerClass SelectClass();    // returns chosen class or throws
Difficulty SelectDifficulty(); // returns chosen difficulty
string SelectSeed();           // returns seed input (empty or valid int string)
```
**Advantage:** Separates **validation logic from display logic**. Tests call `SelectClass()` with mocked input; tests verify return value. Display methods are tested separately via console capture.

### Testing Implications

**With void methods (current approach):**
- Must capture console output to verify menu was shown
- Cannot test input validation separately
- Cannot test "menu shown" → "valid choice made" contract
- FakeDisplayService must track what was displayed; test code reads `display.Messages` list

**With return-value methods (recommended):**
- Unit tests provide fake input (e.g., mock `IInputReader`)
- Assert return values directly: `Assert.Equal(PlayerClass.Mage, SelectClass())`
- Display testing separated from logic testing
- Decouples "show the menu" from "handle the choice"

### Recommendation

**Extract input handling into a separate method that returns a value.** For example:

```csharp
// Display only — no return
void ShowClassSelection();

// Input + validation — has return
PlayerClass SelectPlayerClass(IInputReader input);
```

This mirrors the existing `ReadPlayerName()` pattern and enables true unit testing.

---

## 3. Edge Cases the Improved Intro Must Handle

### Player Name Edge Cases
| **Input** | **Current Behavior** | **Should Behavior** | **Test Case** |
|-----------|-------------------|------------------|--------------|
| `""` (empty) | Fallback to "Unnamed" | Allow fallback gracefully | `test_empty_name_uses_default` |
| `"  "` (whitespace) | Unknown — likely treated as empty | Trim and validate non-empty | `test_whitespace_only_name` |
| 100 chars | Unknown — might overflow display | Truncate to 20-30 chars or reject | `test_long_name_handling` |
| `"Player\n"` | Unknown — newline in name | Reject or strip newline | `test_name_with_newline` |
| `null` | Cannot happen in C# (method signature prevents it) | N/A | N/A |

### Difficulty Menu Edge Cases
| **Input** | **Current Behavior** | **Should Behavior** | **Test Case** |
|-----------|-------------------|------------------|--------------|
| `"1"` | → Casual | ✓ Correct | `test_casual_selected` |
| `"2"` | → Normal (implicit fallback) | ✓ Correct | `test_normal_selected` |
| `"3"` | → Hard | ✓ Correct | `test_hard_selected` |
| `""` (empty) | → Normal (fallback) | Should ask again or confirm default? | `test_empty_difficulty_input` |
| `"4"` (invalid) | → Normal (fallback) | Unclear if this is a bug or feature | `test_invalid_difficulty_input` |
| `"abc"` | → Normal (fallback) | Same as above | `test_non_numeric_difficulty` |

**Issue:** No clear contract for invalid input. Should we retry? Log? Accept silently? **Recommendation:** Add explicit validation and retry loop, or accept silently but document it.

### Class Selection Edge Cases
| **Input** | **Current Behavior** | **Should Behavior** | **Test Case** |
|-----------|-------------------|------------------|--------------|
| `"1"` | → Warrior | ✓ Correct | `test_warrior_selected` |
| `"2"` | → Mage | ✓ Correct | `test_mage_selected` |
| `"3"` | → Rogue | ✓ Correct | `test_rogue_selected` |
| `""` (empty) | → Warrior (fallback) | Should ask again or confirm default? | `test_empty_class_input` |
| `"4"` (invalid) | → Warrior (fallback) | Unclear if this is a bug or feature | `test_invalid_class_input` |
| `"1.5"` | → Warrior (fallback) | Same as above | `test_float_class_input` |

**Issue:** Silent fallback is confusing UX. **Recommendation:** Add confirmation: *"No selection made. Defaulting to Warrior?"* or retry with error message.

### Seed Input Edge Cases
| **Input** | **Current Behavior** | **Should Behavior** | **Test Case** |
|-----------|-------------------|------------------|--------------|
| `""` (empty) | Random seed generated | ✓ Correct | `test_empty_seed_random` |
| `"12345"` | Parsed to seed 12345 | ✓ Correct | `test_valid_seed` |
| `"0"` | Parsed to seed 0 | Valid or invalid? (0 is a valid seed) | `test_zero_seed` |
| `"-1"` | Treated as non-numeric fallback? | Clarify: negative seeds allowed? | `test_negative_seed` |
| `"abc"` | Treated as non-numeric; random seed | ✓ Correct | `test_non_numeric_seed` |
| `"99999999999"` | May overflow int; behavior undefined | Reject with message or cap? | `test_seed_overflow` |

**Issue:** No validation on seed range. Recommend: *"Seed must be 0-999999"* or accept any 32-bit int.

### Player Creation Edge Cases
| **Scenario** | **Current Behavior** | **Should Behavior** | **Test Case** |
|---|---|---|---|
| Prestige loaded successfully | Bonuses applied | ✓ Correct | `test_prestige_bonuses_applied` |
| Prestige load fails | Unknown — crashes? Silently fails? | Should handle gracefully | `test_prestige_load_failure` |
| Class bonus puts stat negative | `Math.Max(0, ...)` used (line 59) | ✓ Defensive clamping | `test_class_bonus_never_negative` |
| MaxHP = 1 after bonuses | `Math.Max(1, ...)` used (line 60) | ✓ Enforced minimum | `test_min_hp_enforced` |
| Mana = -100 before clamping | `Math.Max(0, ...)` used (line 62) | ✓ Defensive clamping | `test_mana_never_negative` |
| Rogue dodge bonus applied | `ClassDodgeBonus = 0.10f` (line 65) | ✓ Correct | `test_rogue_dodge_bonus` |

---

## 4. Existing Test Infrastructure We Can Reuse

### FakeDisplayService
**Location:** `Dungnz.Tests/Helpers/FakeDisplayService.cs`

**Capabilities:**
- Implements `IDisplayService` fully
- Tracks all output in `Messages`, `Errors`, `CombatMessages`, `AllOutput` lists
- Strips ANSI color codes for assertion-friendly plain text
- `ReadPlayerName()` returns hardcoded `"TestPlayer"`

**For Intro Testing:**
```csharp
var display = new FakeDisplayService();
// Call intro sequence with display
display.Messages.Should().Contain("Welcome");
```

### Test Pattern: Console Output Capture
**Location:** `Dungnz.Tests/DisplayServiceTests.cs` (lines 12–28)

Pattern using `StringWriter` + `Console.SetOut()`:
```csharp
private StringWriter _output;
private TextWriter _originalOut;

public DisplayServiceTests()
{
    _originalOut = Console.Out;
    _output = new StringWriter();
    Console.SetOut(_output);
}

public void Dispose() => Console.SetOut(_originalOut);
```

**For Intro Testing:** Can use this for `ConsoleDisplayService` testing but unnecessary for `FakeDisplayService` testing.

### FakeInputReader
**Location:** `Dungnz.Tests/Helpers/FakeInputReader.cs`

**Capability:** Simulates console input for tests (if it exists).

**For Intro Testing:** If `FakeInputReader` provides queued input, we can inject it into intro sequence and test the full flow.

### Existing Test Patterns
- **Arrange-Act-Assert** structure used throughout
- **FluentAssertions** for readable assertions (`.Should().Be()`, `.Should().Contain()`)
- **InlineData** for parameterized tests (Theory + InlineData)
- Builder pattern for test data (check `Dungnz.Tests/Fixtures/`)

---

## 5. Architectural Recommendation: IntroSequence Class

### Current Architecture (Problematic)
```
Program.cs (top-level script)
  ├─ ShowTitle()
  ├─ ReadPlayerName()
  ├─ Seed selection (Console.ReadLine)
  ├─ Difficulty selection (Console.ReadLine, switch)
  ├─ Class selection (Console.ReadLine, switch)
  ├─ Player creation (inline, stat modifications)
  ├─ Factory initialization
  └─ GameLoop.Run()
```

**Problems:**
- All logic at top-level script scope
- Tight coupling to Console I/O
- No testable entry points
- No validation layer
- Side effects (prestige loading, factory initialization) mixed with game startup

### Proposed Architecture

```csharp
public class IntroSequence
{
    private readonly IDisplayService _display;
    private readonly IInputReader _input;
    private readonly IPrestigeProvider _prestige;
    
    public IntroSequence(
        IDisplayService display,
        IInputReader input,
        IPrestigeProvider prestige)
    {
        _display = display;
        _input = input;
        _prestige = prestige;
    }
    
    /// <summary>
    /// Orchestrates the entire intro: title, name, seed, difficulty, class.
    /// Returns a fully-initialized Player ready for dungeon entry.
    /// </summary>
    public Player Run(out int seed)
    {
        _display.ShowTitle();
        
        var name = _display.ReadPlayerName();
        var validatedName = ValidateName(name);
        
        seed = SelectSeed();
        var difficulty = SelectDifficulty();
        var playerClass = SelectClass();
        
        var player = CreatePlayer(validatedName, playerClass, difficulty);
        
        return player;
    }
    
    // Testable methods
    private string ValidateName(string name) { ... }
    private int SelectSeed() { ... }
    private Difficulty SelectDifficulty() { ... }
    private PlayerClass SelectClass() { ... }
    private Player CreatePlayer(string name, PlayerClass cls, Difficulty diff) { ... }
}
```

**In Program.cs:**
```csharp
var intro = new IntroSequence(display, inputReader, prestige);
var player = intro.Run(out int seed);
var difficulty = DifficultySettings.For(player.Difficulty);
// ... continue with dungeon setup
```

### Benefits
1. **Testable:** Can inject `FakeDisplayService`, `FakeInputReader`, test data providers
2. **Reusable:** Other classes can trigger intro (new game button, character creation UI)
3. **Extensible:** Add lore screens, class descriptions, difficulty explanations without touching Program.cs
4. **Validatable:** Each step (ValidateName, SelectSeed, etc.) is a method; unit tests validate behavior
5. **Decoupled:** Display logic, input logic, validation logic, player creation logic are separate concerns

---

## 6. Test Cases for Improved Intro (Priority Order)

### Tier 1: Core Functionality (MUST WRITE)

```csharp
public class IntroSequenceTests
{
    // Player name validation
    [Fact] public void ValidateName_EmptyString_ReturnsDefault()
    [Fact] public void ValidateName_NormalInput_ReturnsInput()
    [Fact] public void ValidateName_Whitespace_TrimsThenValidates()
    [Theory]
    [InlineData("Player\n")]
    [InlineData("Player\0")]
    public void ValidateName_SpecialCharacters_Rejected(string name)
    
    // Seed selection
    [Fact] public void SelectSeed_EmptyInput_ReturnsRandomSeed()
    [Theory]
    [InlineData("12345")]
    [InlineData("0")]
    [InlineData("999999")]
    public void SelectSeed_ValidNumeric_ParsesCorrectly(string input)
    [Fact] public void SelectSeed_InvalidNumeric_ReturnsRandomSeed()
    
    // Difficulty selection
    [Theory]
    [InlineData("1", Difficulty.Casual)]
    [InlineData("2", Difficulty.Normal)]
    [InlineData("3", Difficulty.Hard)]
    public void SelectDifficulty_ValidInput_ReturnsDifficulty(string input, Difficulty expected)
    [Fact] public void SelectDifficulty_InvalidInput_ReturnsNormalDefault()
    [Fact] public void SelectDifficulty_EmptyInput_ReturnsNormalDefault()
    
    // Class selection
    [Theory]
    [InlineData("1", PlayerClass.Warrior)]
    [InlineData("2", PlayerClass.Mage)]
    [InlineData("3", PlayerClass.Rogue)]
    public void SelectClass_ValidInput_ReturnsClass(string input, PlayerClass expected)
    [Fact] public void SelectClass_InvalidInput_ReturnsWarriorDefault()
    [Fact] public void SelectClass_EmptyInput_ReturnsWarriorDefault()
    
    // Player creation
    [Fact] public void CreatePlayer_WarriorBonus_AppliesCorrectly()
    [Fact] public void CreatePlayer_MageBonus_AppliesCorrectly()
    [Fact] public void CreatePlayer_RogueBonus_AppliesCorrectly()
    [Fact] public void CreatePlayer_ClassBonus_CannotMakeStatNegative()
    [Fact] public void CreatePlayer_PrestigeBonus_AppliedOnTopOfClass()
    
    // Integration
    [Fact] public void Run_FullIntro_ReturnsInitializedPlayer()
    [Fact] public void Run_FullIntro_DisplaysTitle()
    [Fact] public void Run_FullIntro_DisplaysDifficultyConfirmation()
}
```

### Tier 2: Edge Cases & Error Handling

```csharp
[Fact] public void ValidateName_VeryLongName_TruncatesOr Rejects()
[Fact] public void SelectSeed_OverflowInt_HandlesGracefully()
[Fact] public void SelectSeed_NegativeNumber_AcceptedOrRejected()
[Fact] public void CreatePlayer_PrestigeLoadFails_ContinuesWithZeroPrestige()
[Fact] public void Run_UserCancels_ExitsGracefully()
```

### Tier 3: Regression & Integration

```csharp
// Regression from Program.cs v1
[Fact] public void CreatePlayer_WithPrestige_StatsMatchManualCalculation()
[Fact] public void SelectDifficulty_MenuSelection_MatchesDifficultySettings()

// Integration with DungeonGenerator
[Fact] public void Run_WithSeed_DungeonIsReproducible()
```

---

## 7. Quality Gates & Rejection Criteria

### For Intro Refactoring PR

**Must Pass (BLOCKING):**
- [ ] All Tier 1 tests pass (24 test cases)
- [ ] No Console.ReadLine calls in intro logic (all routed through IInputReader)
- [ ] No hardcoded PlayerClass/Difficulty values; only use enums
- [ ] FakeDisplayService captures all intro output
- [ ] Player returned by IntroSequence has non-zero HP, non-empty Name

**Should Pass (MERGE GATE):**
- [ ] All Tier 2 edge case tests pass
- [ ] Code review: no silent fallbacks without documentation (e.g., "Invalid input defaults to Normal")
- [ ] Documentation: updated README or in-game help explaining valid inputs

**Nice-to-Have (FOLLOW-UP):**
- [ ] Tier 3 regression tests
- [ ] Console output capture tests for menu formatting

### Rejection Criteria

- ❌ Test failures (any Tier 1 failure blocks merge)
- ❌ Silent input fallback without error message or retry loop
- ❌ ConsoleDisplayService not capturing menu output in FakeDisplayService
- ❌ Player creation applies stat bonuses without bounds checking (e.g., HP < 1, Defense < 0)
- ❌ Prestige loading failure crashes intro instead of gracefully continuing
- ❌ Seed parsing overflows without explicit handling or documentation

---

## 8. Summary: Testing Implications of Intro Improvements

| **Aspect** | **Current Risk** | **Improved Approach** | **Testing Impact** |
|---|---|---|---|
| **Top-level script logic** | HIGH — Untestable | Extract `IntroSequence` class | Enables unit testing of all intro paths |
| **Console I/O coupling** | CRITICAL — No test input | Inject `IInputReader` interface | Tests provide queued input; verify output |
| **Input validation** | HIGH — Silent fallbacks | Validate before creating Player | Unit tests verify error handling |
| **IDisplayService methods** | MEDIUM — void methods only | Add return-value methods (e.g., `SelectClass()`) | Tests verify choices returned; display tests separate |
| **Prestige side effects** | MEDIUM — Global load | Inject `IPrestigeProvider` | Mock prestige in unit tests |
| **Seed reproducibility** | MEDIUM — No record | Return seed from `Run(out int seed)` | Tests verify seed is consistent |
| **Player creation** | LOW — Inline with defaults | Dedicated `CreatePlayer()` method | Unit tests verify class bonuses, prestige bonuses applied correctly |
| **Edge cases** | UNKNOWN — Most untested | Parameterized tests (Theory + InlineData) | Comprehensive edge case coverage |

---

## 9. Files to Create/Modify

| **File** | **Change** | **Owner** |
|---|---|---|
| `Engine/IntroSequence.cs` | NEW CLASS — Orchestrates intro sequence | Hill or Barton |
| `Display/IDisplayService.cs` | ADD METHODS — `SelectClass()`, `SelectDifficulty()`, `SelectSeed()` | Hill |
| `Display/ConsoleDisplayService.cs` | ADD IMPLEMENTATIONS — Impl the new interface methods | Hill |
| `Program.cs` | REFACTOR — Replace lines 7-82 with `new IntroSequence().Run()` | Hill |
| `Dungnz.Tests/IntroSequenceTests.cs` | NEW TEST CLASS — 24+ test cases per Tier 1 | Romanoff |
| `Dungnz.Tests/Helpers/FakeInputReader.cs` | ENHANCE if needed — Queue input for testing | Romanoff or Hill |

---

## Appendix: Risk Inventory for v3 Intro Features

### Lore Screen
- **Risk:** No test for display timing (too long? blocks interaction?)
- **Test:** Verify lore can be skipped via input

### Improved Class Descriptions
- **Risk:** Unaligned stat descriptions (e.g., "Mage has high mana" but actually +50 mana, unclear if that's "high")
- **Test:** Unit test for stat delta display accuracy

### Difficulty Explanations
- **Risk:** Outdated explanations if DifficultySettings values change
- **Test:** Integration test: verify explanation text matches actual difficulty scaling

### Character Portrait ASCII Art
- **Risk:** Display corruption on narrow terminals
- **Test:** Console capture test for width validation

---

## Closing Notes

The current intro is **fragile and unmaintainable**. As new features (lore, portraits, difficulty explanations) are added, the risk of regressions grows exponentially without a testable architecture.

**I recommend extracting `IntroSequence` BEFORE adding new intro features.** This unblocks testing and makes future UI improvements safe.

**Quality target:** 24+ unit tests covering all intro paths, with 95%+ coverage of player creation logic.

—**Romanoff**

# Intro Sequence Improvement Plan

**Date:** 2026-02-22  
**Lead:** Coulson  
**Approval:** Pending Anthony approval  
**Effort Estimate:** 6-8 hours (5h dev, 2h testing, 1h review)  

---

## Executive Summary

The current intro is functional but lacks atmosphere, clarity, and player investment. This plan provides:
- **Enhanced title screen** with ASCII art, tagline, and atmospheric lore (skippable)
- **Stat transparency** in class/difficulty selection so players understand tradeoffs
- **Improved flow** that builds investment (name first) and makes informed choices easy
- **Better UX for seed** (auto-generated, shown at end, CLI flag for power users)
- **Prestige celebration** that shows progression and bonuses

**Key principle:** Reduce friction for 95% of players (no seed prompts), empower 5% (CLI override, displayed seed for sharing).

---

## 1. Title Screen Improvements

### Current State
```
╔═══════════════════════════════════════╗
║         DUNGEON CRAWLER               ║
║      A Text-Based Adventure           ║
╚═══════════════════════════════════════╝
```

### Proposed Design

**Full-width ASCII art with mood:**
```
    ╔════════════════════════════════════════════════════════════════╗
    ║                                                                ║
    ║                    D U N G E O N   C R A W L E R              ║
    ║                                                                ║
    ║   ⚔️  ██╗  ██╗███████╗██████╗ ███████╗███╗   ██╗████████╗   ║
    ║       ██║  ██║██╔════╝██╔══██╗██╔════╝████╗  ██║╚══██╔══╝   ║
    ║       ███████║█████╗  ██████╔╝█████╗  ██╔██╗ ██║   ██║      ║
    ║       ██╔══██║██╔══╝  ██╔══██╗██╔══╝  ██║╚██╗██║   ██║      ║
    ║       ██║  ██║███████╗██║  ██║███████╗██║ ╚████║   ██║      ║
    ║       ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝   ╚═╝  🗡️ ║
    ║                                                                ║
    ║           ✦ DESCEND IF YOU DARE ✦                           ║
    ║                                                                ║
    ╚════════════════════════════════════════════════════════════════╝
```

**Color scheme:**
- Title text: **CYAN** (cold, mysterious)
- ASCII art: **BRIGHT WHITE** (bold, dramatic)
- Tagline: **YELLOW** (draws eye, warns of danger)
- Borders: **CYAN**

**Atmosphere narrative (optional, shown after title):**
```
Press ENTER to skip intro, or read on...

The dungeon stretches endlessly downward, breathing with ancient malice.
Countless adventurers have descended. Few return.

You hear the screams of the damned echoing from below.
The choice is yours: courage or cowardice?
```
- Tone: Dark, foreboding, but not grimdark
- Length: 4 sentences max
- Delivery: Slow (one line per 1.5 seconds, optional skip)

---

## 2. Character Creation Sequence

### Revised Flow

The sequence is **reordered** to build investment and enable informed choices:

1. **Title screen** (with optional lore)
2. **Prestige display** (if applicable)
3. **Name entry** (builds investment early)
4. **Difficulty selection** (now transparent with mechanics)
5. **Class selection** (shows full starting stats, not just bonuses)
6. **Seed display** (auto-generated, shown for reference)
7. **Game start**

### 2a. Prestige Display (if player has prestige > 0)

**Current:** Bare text "Prestige Level: 3"  
**Proposed:**

```
╔════════════════════════════════════════════════════════════╗
║               🏆 RETURNING CHAMPION 🏆                    ║
╠════════════════════════════════════════════════════════════╣
║  Prestige Level:  3                                        ║
║  Total Victories: 9 wins (3 runs per level)                ║
║  Win Rate:        45% (9 wins / 20 runs)                   ║
╠════════════════════════════════════════════════════════════╣
║  Starting Bonuses:                                         ║
║    • Attack:    +1                                         ║
║    • Defense:   +1                                         ║
║    • Max HP:    +15                                        ║
╠════════════════════════════════════════════════════════════╣
║  Progress to Prestige 4:  6 more wins needed               ║
║  (The dungeon remembers your victories.)                   ║
╚════════════════════════════════════════════════════════════╝
```

**Color:**
- Header: **BRIGHT WHITE**
- "RETURNING CHAMPION": **YELLOW** (celebration)
- Stats labels: **CYAN**
- Stats values: **GREEN** (positive reinforcement)
- Progress bar: **YELLOW** or **GREEN**

### 2b. Name Entry

**Current:** `Console.Write("Enter your name, adventurer: ");`  
**Proposed:**

```
╔════════════════════════════════════════════════════════════╗
║         What is your name, adventurer?                    ║
║                                                            ║
║  Enter a name (or press ENTER for "Hero"):                ║
║                                                            ║
║  ► _                                                       ║
╚════════════════════════════════════════════════════════════╝
```

- Prompt is flavor-rich, not bare
- Shows default fallback
- Input line shows cursor/prompt clearly

### 2c. Difficulty Selection

**Current:**
```
Choose difficulty: [1] Casual  [2] Normal  [3] Hard
```

**Proposed: Difficulty card selection with mechanics transparency**

```
╔════════════════════════════════════════════════════════════╗
║                  CHOOSE DIFFICULTY                        ║
╠════════════════════════════════════════════════════════════╣
║                                                            ║
║  [1] CASUAL — Perfect for your first descent              ║
║      Enemy Damage: 80% (easier)                           ║
║      Loot Quality: 150% (generous rewards)                ║
║      Elite Spawn Rate: 5% (mostly normal enemies)         ║
║      Recommended: First run, relaxed playstyle            ║
║                                                            ║
║  [2] NORMAL — The intended experience                     ║
║      Enemy Damage: 100% (balanced)                        ║
║      Loot Quality: 100% (standard rewards)                ║
║      Elite Spawn Rate: 15% (more tough fights)            ║
║      Recommended: Subsequent runs, standard challenge     ║
║                                                            ║
║  [3] HARD — Only the worthy survive                       ║
║      Enemy Damage: 130% (brutal)                          ║
║      Loot Quality: 70% (scarce rewards)                   ║
║      Elite Spawn Rate: 30% (many tough fights)            ║
║      Recommended: Mastery run, prestige farming           ║
║                                                            ║
║  ► Select [1/2/3]:                                        ║
╚════════════════════════════════════════════════════════════╝
```

**Color scheme:**
- Header: **CYAN**
- Difficulty level names: **BRIGHT WHITE** (1), **GREEN** (2), **BrightRed** (3)
- Mechanical stats: **YELLOW**
- Input prompt: **CYAN**

**Benefit:** Players understand tradeoffs, not just labels. Casuals aren't scared; Hard players see the real challenge.

### 2d. Class Selection

**Current:**
```
Choose your class:
[1] Warrior - High HP, defense, and attack bonus. Reduced mana.
[2] Mage - High mana and powerful spells. Reduced HP and defense.
[3] Rogue - Balanced with an attack bonus. Extra dodge chance.
```

**Proposed: Class cards showing full starting stats**

```
╔════════════════════════════════════════════════════════════╗
║                  CHOOSE YOUR CLASS                        ║
╠════════════════════════════════════════════════════════════╣
║                                                            ║
║  [1] ⚔️  WARRIOR — The Unbreakable                        ║
║      Starting Stats (base + class + prestige):            ║
║        HP:       100 → 120  (+20)                         ║
║        Attack:    10 → 12   (+2)                          ║
║        Defense:    3 → 5    (+2)                          ║
║        Mana:      30 → 20   (-10)                         ║
║      PASSIVE: Unstoppable — +3% defense per 10% HP        ║
║      PLAYSTYLE: Tank through attrition.                   ║
║                 Survive what others can't.                ║
║                                                            ║
║  [2] 🔮 MAGE — The Mystic Force                           ║
║      Starting Stats (base + class + prestige):            ║
║        HP:        60 → 75   (+15)                         ║
║        Attack:    10 → 8    (-2)                          ║
║        Defense:    3 → 2    (-1)                          ║
║        Mana:      60 → 80   (+20)                         ║
║      PASSIVE: Spellweaver — Spell crit chance: 15%        ║
║      PLAYSTYLE: Glass cannon burst.                       ║
║                 End fights before they start.             ║
║                                                            ║
║  [3] 🗡️  ROGUE — The Swift Shadow                         ║
║      Starting Stats (base + class + prestige):            ║
║        HP:        80 → 95   (+15)                         ║
║        Attack:    10 → 12   (+2)                          ║
║        Defense:    3 → 3    (+0)                          ║
║        Mana:      30 → 30   (+0)                          ║
║      PASSIVE: Evasion — +10% chance to dodge attacks      ║
║      PLAYSTYLE: Balanced and nimble.                      ║
║                 Skill and speed over raw power.           ║
║                                                            ║
║  ► Select [1/2/3]:                                        ║
╚════════════════════════════════════════════════════════════╝
```

**Color scheme:**
- Class names: **BRIGHT WHITE**
- Emojis: **YELLOW**
- "Starting Stats": **CYAN** (label)
- Stat names (HP, Attack, etc.): **GREEN**
- Starting values: **BRIGHT WHITE**
- Bonuses (+X): **GREEN**
- Penalties (-X): **BrightRed**
- PASSIVE trait name: **YELLOW**
- Trait description: **CYAN**
- PLAYSTYLE header: **YELLOW**
- Playstyle text: **CYAN**

**Why this works:**
- Players see exactly what their character starts with (accounting for all bonuses)
- Passive traits are named and explained (not mysterious)
- Playstyle guidance helps choosing based on preferred approach
- Color separates information categories (stats vs. passives vs. playstyle)
- Emojis make classes memorable (⚔️ tank, 🔮 spellcaster, 🗡️ agile)

---

## 3. Seed Handling

### Current State
```
Enter a seed for reproducible runs (or press Enter for random):
> [player input]
```

### Problems
- Blocks casual players (95% don't care)
- Speedrunners/testers need to note seed anyway
- CLI integration is awkward

### Proposed Solution

**Option A: Auto-generate, display at end**
- Seed is generated automatically
- Shown to player just before game starts: "Seed: 123456 (share this to replay)"
- Players only think about it if they want to share/replay
- Reduces cognitive load

**Option B: CLI flag for power users (future)**
- Add `--seed 12345` flag to executable
- If provided, use it; otherwise auto-generate
- Doesn't clutter the UI
- Example: `dotnet run -- --seed 123456`

**Recommended approach:** Option A now (auto-generate and display), add Option B later when CLI interface is formalized.

**Code in Program.cs:**
```csharp
// Seed selection (simplified)
var actualSeed = new Random().Next(100000, 999999);
// Display will show it just before game starts
```

**Seed display line (after all selections):**
```
═════════════════════════════════════════════════════════════
  Initializing your descent...
  Seed: 537892  (share this number to replay the exact same run)
═════════════════════════════════════════════════════════════
```

---

## 4. Architecture & Implementation Approach

### Principle: Keep it Simple Now, Extract When Needed

**Current Program.cs:** ~80 lines of setup code (acceptable)  
**Proposed:** Add display methods, keep orchestration in Program.cs  
**Future:** Extract to `GameSetupService` when implementing "Load Game" (to avoid duplicating setup logic)

### New IDisplayService Methods

Add to interface (and ConsoleDisplayService implementation):

```csharp
// Intro screen with title, tagline, ASCII art
void ShowEnhancedTitle();

// Optional lore/atmosphere text (returns true if player wants to skip)
bool ShowIntroNarrative();

// Display prestige info for returning players
void ShowPrestigeInfo(PrestigeData prestige);

// Difficulty selection (returns validated Difficulty enum)
Difficulty SelectDifficulty();

// Class selection (returns validated PlayerClassDefinition)
PlayerClassDefinition SelectClass();

// Show seed before game starts
void ShowSeedInfo(int seed);
```

### Validation Logic Lives in Display Layer

Display service owns input loops:
```csharp
public Difficulty SelectDifficulty()
{
    while (true)
    {
        ShowDifficultyOptions();
        var input = Console.ReadLine()?.Trim() ?? "";
        
        var difficulty = input switch
        {
            "1" => Difficulty.Casual,
            "2" => Difficulty.Normal,
            "3" => Difficulty.Hard,
            _ => null
        };
        
        if (difficulty.HasValue)
            return difficulty.Value;
        
        ShowError("Invalid selection. Choose [1], [2], or [3].");
    }
}
```

**Why:** Display layer knows what inputs are valid. Game logic receives guaranteed-valid data.

### Program.cs After Changes

```csharp
var display = new ConsoleDisplayService();

// Intro sequence (in order)
display.ShowEnhancedTitle();
if (!display.ShowIntroNarrative())
    display.ShowMessage(""); // User skipped, space things out

var prestige = PrestigeSystem.Load();
if (prestige.PrestigeLevel > 0)
    display.ShowPrestigeInfo(prestige);

var name = display.ReadPlayerName();
var difficulty = display.SelectDifficulty();  // NEW: returns validated Difficulty
var playerClass = display.SelectClass();      // NEW: returns validated PlayerClassDefinition

// Seed (auto-generated now)
var actualSeed = new Random().Next(100000, 999999);

display.ShowSeedInfo(actualSeed); // NEW: shows seed before game starts

// Create player (unchanged logic)
var player = new Player { Name = name };
player.Class = playerClass.Class;
player.Attack += playerClass.BonusAttack;
// ... rest of setup ...
```

**Total new code in Program.cs:** ~10 lines  
**New Display methods:** 5 methods (shown above)  
**ConsoleDisplayService additions:** ~200 lines

---

## 5. Implementation Phases

### Phase 1: Foundation (2 hours)
- [ ] Add 5 new methods to IDisplayService
- [ ] Implement ShowEnhancedTitle with ASCII art and colors
- [ ] Implement ShowPrestigeInfo with formatting
- [ ] Update Program.cs to use new methods (remove inline seed prompt)
- [ ] Test on actual console (ensure colors display correctly)

### Phase 2: Enhanced Selection (2 hours)
- [ ] Implement SelectDifficulty with full card display
- [ ] Implement SelectClass with stat cards
- [ ] Implement ShowSeedInfo
- [ ] Test validation loops (invalid inputs re-prompt)
- [ ] Test with all class/difficulty combinations

### Phase 3: Polish (1.5 hours)
- [ ] Implement ShowIntroNarrative with optional skip
- [ ] Add spacing/pacing (slow narrative reveal if not skipped)
- [ ] Color verification (ensure all colors render correctly on dark/light terminals)
- [ ] Update README with new intro design
- [ ] Run full test suite (all 267 tests should pass)

### Phase 4: Review & Merge (1 hour)
- [ ] Coulson reviews implementation against this plan
- [ ] Anthony approves visual design
- [ ] Merge to master

**Total: ~6.5 hours**

---

## 6. Success Criteria

### Functional
- [ ] All 267 existing tests pass (no regressions)
- [ ] All 5 display methods work correctly
- [ ] Invalid inputs re-prompt (no crashes)
- [ ] Prestige display shows when prestige > 0 (hidden when 0)
- [ ] Lore narrative can be skipped with Enter
- [ ] Seed is displayed before game starts

### Visual/UX
- [ ] Title screen conveys atmosphere (dark, mysterious)
- [ ] Difficulty/class selections clearly show tradeoffs
- [ ] Colors are consistent with color system established in PR #226
- [ ] Spacing and alignment are clean (no ragged borders)
- [ ] New intro takes <1 minute for experienced players (fast path)

### Technical
- [ ] No changes to game logic (purely presentation layer)
- [ ] Display layer owns validation loops (no null checks in Program.cs)
- [ ] New interface methods are composable and reusable
- [ ] Code follows existing patterns (emojis, colors, ASCII borders)

---

## 7. Risk Assessment

**Risk: Low**
- Pure presentation layer (no game logic changes)
- Existing tests don't depend on intro code (easy to add new tests if needed)
- Fallback behavior preserved (name defaults to "Hero", difficulty defaults to Normal)

**Mitigation:**
- Run full test suite after each phase
- Test with actual console (colors vary by terminal)
- Get Anthony's sign-off on visual design before implementing

---

## 8. Future Extensions (Not in This Plan)

These ideas are cool but out of scope:

- **Character portraits:** ASCII art for each class (after class selection)
- **Build preview:** Show how the player's character will look at level 10, 20
- **Tutorial tips:** Contextual hints during setup ("Mages need a big mana pool; use Mana potions in combat")
- **Difficulty auto-recommend:** Suggest Normal for first run, Hard for prestige farming
- **Customizable colors:** Let players override color scheme
- **Speedrun mode:** `--speedrun` flag that skips narrative and prestige display

---

## Sign-Off

**Coulson (Lead):** ✅ Approved  
**Hill (C# Dev):** ✅ Ready to implement  
**Barton (Systems Dev):** ✅ UX flow validated  

**Pending:** Anthony approval before implementation begins
### 2026-02-22: User directive — no direct commits to master

**By:** Anthony (via Copilot)
**What:** ALL changes must go through feature/hotfix branches and PRs. Commits must NEVER be made directly to `master`. No exceptions. This applies to ALL squad members, including the coordinator.
**Why:** User request — captured after second violation in same session. Direct master commits bypass review gates and break the team's PR workflow.

---


### 2026-02-22: Process Alignment Protocol (All-Hands Ceremony)

**By:** Coulson (Lead) — facilitated; Hill, Barton, Romanoff — unanimous consensus  
**Trigger:** Repeated direct-to-master commits; Anthony directive "alignment on processes"

**Binding protocol (no exceptions):**

1. **Branch check first** — before writing any code: `git branch --show-current`. If on `master`, STOP. Create a branch.
2. **Branch naming** — `feature/{slug}` for new work, `hotfix/{slug}` for fixes.
3. **All commits go to branch only** — zero commits directly to `master`. Ever.
4. **Push branch → open PR → request review** — no self-merges without lead sign-off.
5. **CI must pass** before any merge is permitted.
6. **Self-correction protocol** (if violation detected):
   - Stop immediately
   - Cherry-pick rogue commits to a `hotfix/` branch
   - Reset `master` to `origin/master`
   - Open PR with description of what happened
   - Log violation in `decisions.md` within the same session
   - Notify Anthony

**Top enforcement recommendation (all agents, unanimous):**  
Enable GitHub branch protection on `master` — disable direct pushes at the repo level. Settings → Branches → Add protection rule → `master` → require PR + 1 review + block direct pushes.

**Why:** The gap is enforcement, not documentation. Branch protection makes violations mechanically impossible.

---

### 2026-02-22: PR #228 Review Verdict
**By:** Coulson
**What:** APPROVED — PR #228
**Why:** All three fixes are correct, minimal, and well-tested. No regressions introduced.

---

## Detailed Review

### Fix 1: Remove duplicate `ShowTitle()` call — ✅ CORRECT

**File:** `Engine/GameLoop.cs` (line 120 removed)

The `_display.ShowTitle()` call in `Run()` was clearing the screen and printing a basic header, wiping the enhanced intro sequence from PR #227. Removing it is the right call — the intro sequence is the new entry point, and `Run()` should not override it.

**Concern:** None. The remaining `ShowMessage` calls for difficulty and floor info are appropriate post-intro context.

### Fix 2: Add `"listsaves"` alias — ✅ CORRECT

**File:** `Engine/CommandParser.cs` (line 153)

Help text advertised `listsaves` as valid input; the parser only matched `"list"` and `"saves"` as separate tokens. Adding `"listsaves"` as a third pattern match is the minimal correct fix.

**Architecture:** Clean — stays in the existing switch expression pattern. No separation-of-concerns issues.

**Note:** `CommandParser.Parse()` splits on the first space, so `"list saves"` would match `"list"` with argument `"saves"`. The single-word `"listsaves"` correctly needed its own alias. Good catch.

### Fix 3: Remove boss gate deadlock — ✅ CORRECT

**File:** `Engine/GameLoop.cs` (lines 258-264 removed)

The gate at line 258 checked `nextRoom.IsExit && nextRoom.Enemy != null && nextRoom.Enemy.HP > 0` and blocked entry. But `DungeonGenerator` places the boss *inside* the exit room, meaning:
- Gate says: "Can't enter until boss is dead"
- Boss location says: "Boss is inside — enter to fight"
- Result: Circular deadlock. Boss is permanently unreachable.

The existing auto-combat trigger at line 317 (`if (_currentRoom.Enemy != null && _currentRoom.Enemy.HP > 0)`) correctly handles this: player enters room → combat fires → if won, `Enemy` nulled → line 352 win condition checks `IsExit && Enemy == null`. The flow is sound without the gate.

**Test update:** `Dungnz.Tests/GameLoopTests.cs` — Old test `BossGate_CannotEnterExitRoomWithBossAlive` correctly replaced with `BossRoom_CanEnterExitRoomWithBossAlive_CombatTriggered`. New test:
- Sets up boss room with enemy
- Mocks `RunCombat` to return `CombatResult.Won`
- Asserts no "boss blocks" error
- Verifies `RunCombat` was called exactly once

This is a proper behavioral test that validates the intended game flow.

### Overall Assessment

| Criterion | Verdict |
|-----------|---------|
| Correctness | ✅ All three fixes address real bugs with minimal, targeted changes |
| Architecture | ✅ No separation-of-concerns violations; changes stay in appropriate layers |
| Regressions | ✅ No new regressions — `WinCondition_EnteringExitRoomWithBossDead` test still validates the boss-dead → exit path |
| Test coverage | ✅ Boss gate test updated to match new behavior; 298/298 passing |
| Commit hygiene | ✅ Two focused commits with clear messages and rationale |

**Verdict: APPROVED.** Ship it.


---

## Coulson's Looting UX Improvement Plan
**Date:** 2026-02-22
**Requested by:** Anthony
**Status:** Plan delivered — awaiting approval before implementation

### Audit Context

**Hill** audited 10 looting display surfaces — all underutilize available item data.

**Barton** audited loot systems and item data model — 14 properties on Item, only Name shown on drops.

### Architectural Decision
Add ItemTier enum (Common/Uncommon/Rare) as explicit property rather than deriving from LootTable.

### Three-Phase Implementation Plan

**Phase 1 (5h, display-only)**
- Loot drop cards
- Gold color highlighting
- Room item type icons
- Pickup stats
- Examine stat card
- Inventory enhancement

**Phase 2 (5h, model change)**
- Add ItemTier enum + dependent tier-colored names
- Merchant affordability indication
- Crafting status

**Phase 3 (4h, polish)**
- Consumable grouping
- Elite loot callout
- Weight warning
- Upgrade indicator

**Total effort:** 14 hours
**Total value:** Transform 1-property display into 14-property rich loot experience
### 2026-02-22: Looting UI/UX Improvement Plan
**By:** Coulson
**What:** Plan for improving the looting dialog and related loot/item display surfaces
**Why:** All loot surfaces are monochromatic; rich item data exists but is hidden from the player

---

## Executive Summary

The Item model carries 14 properties. Most display surfaces show only `Name`. The player has no way to evaluate loot at the moment it matters — when it drops, when they see it in a room, when they're deciding what to buy. The equipment comparison screen (already color-coded with deltas) proves we *can* do this well. This plan extends that quality bar to every other item surface.

**Scope:** 10 display surfaces, organized into 3 phases by impact and dependency.
**Estimated effort:** ~14 hours total (Hill ~8h, Barton ~4h, Romanoff ~2h).
**Risk:** Low-to-moderate. Phase 1 is pure display changes. Phase 2 adds a model property. Phase 3 is polish.

---

## Architectural Decision: Tier/Rarity

**The Question:** Items have no `Tier` or `Rarity` property. Tier is implicit — determined by which `LootTable` list (`Tier1Items`, `Tier2Items`, `Tier3Items`) the item lives in. Should we:

**(A) Derive tier at display time** — scan LootTable lists to find which tier an item belongs to, or
**(B) Add a `Tier` property to the Item model** — set it at definition time?

**Decision: Option B — Add `ItemTier` enum and `Tier` property to Item.**

Rationale:
- **Option A is fragile.** Items from crafting, merchants, or future quest rewards won't appear in LootTable lists. Derivation breaks immediately.
- **Option B is 2 lines of model change** (`public ItemTier Tier { get; set; } = ItemTier.Common;`) plus updating existing item definitions (~20 items across 3 lists).
- The Tier property enables color-coding, filtering, and sorting everywhere without coupling display logic to LootTable internals.
- Follows the principle: *data belongs on the model, not derived from infrastructure.*

```csharp
public enum ItemTier { Common, Uncommon, Rare }
```

Mapping: Tier1 → Common (white), Tier2 → Uncommon (green), Tier3 → Rare (cyan/bright).

**Assigned to:** Barton (model change + LootTable annotation).

---

## Color Language (Established Palette, Extended)

| Semantic           | Color       | ANSI Code | Usage                              |
|--------------------|-------------|-----------|-------------------------------------|
| Attack / Damage    | Red         | `\e[31m`  | AttackBonus, bleed, weapon stats   |
| Defense / Shield   | Cyan        | `\e[36m`  | DefenseBonus, armor stats          |
| Healing / HP       | Green       | `\e[32m`  | HealAmount, success                |
| Mana               | Blue        | `\e[34m`  | ManaRestore, MaxManaBonus          |
| Gold / Currency    | Yellow      | `\e[33m`  | Gold amounts, prices               |
| Disabled / Flavor  | Gray        | `\e[90m`  | Weight, descriptions, unavailable  |
| Highlights / Names | BrightWhite | `\e[97m`  | Item names, headers                |
| Tier: Common       | White       | `\e[37m`  | Tier 1 items                       |
| Tier: Uncommon     | Green       | `\e[32m`  | Tier 2 items                       |
| Tier: Rare         | BrightCyan  | `\e[96m`  | Tier 3 items                       |

Type icons (emoji, consistent across all surfaces):
- ⚔ Weapon  |  🛡 Armor  |  💍 Accessory  |  🧪 Consumable

---

## Phase 1: High-Impact Quick Wins (5 hours)

These are display-only changes. No model modifications. Immediate player-visible improvement.

### 1.1 — Loot Drop Display (CombatEngine.cs → DisplayService)
**Priority:** Highest — this is the #1 moment of excitement in a dungeon crawler.
**Assigned to:** Hill
**Effort:** 1 hour

**Currently:**
```
✦ Dropped: Short Sword
```

**Proposed:**
```
╔══════════════════════════════════════╗
║  ✦ LOOT DROP                        ║
║  ⚔ Short Sword                      ║
║  Attack +2  •  3 wt                  ║
╚══════════════════════════════════════╝
```

For items with multiple stats:
```
╔══════════════════════════════════════╗
║  ✦ LOOT DROP                        ║
║  ⚔ Mythril Blade                    ║
║  Attack +8  •  Bleed on hit          ║
║  +5% dodge  •  5 wt                  ║
╚══════════════════════════════════════╝
```

For consumables:
```
╔══════════════════════════════════════╗
║  ✦ LOOT DROP                        ║
║  🧪 Greater Healing Potion           ║
║  Heals 50 HP  •  1 wt               ║
╚══════════════════════════════════════╝
```

**File changes:**
- `Display/ConsoleDisplayService.cs` — Rewrite `ShowLootDrop(Item)` to render stat card with box drawing, type icon, color-coded stats.
- `Display/IDisplayService.cs` — Signature unchanged (`ShowLootDrop(Item)` already exists).

**Implementation notes:**
- Build a reusable `FormatItemStatLine(Item)` private helper in ConsoleDisplayService that produces the stat summary string. This helper will be reused across multiple surfaces.
- Color: item name in BrightWhite, attack stats in Red, defense in Cyan, healing in Green, mana in Blue, weight in Gray.

---

### 1.2 — Gold Acquisition Display (CombatEngine.cs)
**Priority:** High — gold drops are frequent and currently invisible against other text.
**Assigned to:** Hill
**Effort:** 30 minutes

**Currently:**
```
You found 15 gold!
```

**Proposed:**
```
  💰 +15 gold  (Total: 47g)
```

**File changes:**
- `Engine/CombatEngine.cs` — Change the `ShowMessage` call at line ~663 to use a new `ShowGoldPickup(int amount, int newTotal)` method.
- `Display/IDisplayService.cs` — Add `void ShowGoldPickup(int amount, int newTotal)`.
- `Display/ConsoleDisplayService.cs` — Implement with Yellow color for amount, Gray for total.

---

### 1.3 — Room Items Display (ConsoleDisplayService.cs)
**Priority:** High — players scan room descriptions constantly.
**Assigned to:** Hill
**Effort:** 45 minutes

**Currently:**
```
Items: Short Sword, Healing Potion
```

**Proposed:**
```
Items on the ground:
  ⚔ Short Sword (Attack +2)
  🧪 Healing Potion (Heals 25 HP)
```

**File changes:**
- `Display/ConsoleDisplayService.cs` — Rewrite the room items block (lines 62-67) to iterate items individually with type icon and primary stat.
- Uses the same `FormatItemStatLine` helper from 1.1.

---

### 1.4 — Item Pickup Display (GameLoop.cs)
**Priority:** Medium — confirms the action, should show what player gained.
**Assigned to:** Hill
**Effort:** 30 minutes

**Currently:**
```
You take the Short Sword.
Every bit helps down here.
```

**Proposed:**
```
  ⚔ Picked up: Short Sword (Attack +2)
  Weight: 18/50  •  Slots: 6/10
  Every bit helps down here.
```

**File changes:**
- `Engine/GameLoop.cs` — Replace `ShowMessage($"You take the {item.Name}.")` with call to new `ShowItemPickup(Item, Player)` method.
- `Display/IDisplayService.cs` — Add `void ShowItemPickup(Item item, Player player)`.
- `Display/ConsoleDisplayService.cs` — Implement with stat line + weight/slot status (reuse existing `WeightColor` helper).

---

### 1.5 — Item Examination Display (GameLoop.cs)
**Priority:** Medium — this is the player's "tell me about this item" action.
**Assigned to:** Hill
**Effort:** 45 minutes

**Currently:**
```
Short Sword: A basic blade.
```

**Proposed:**
```
═══════════════════════════════════
  ⚔ Short Sword
  "A basic blade."
─────────────────────────────────
  Attack +2
  Weight: 3
  Type: Weapon  •  Equippable
═══════════════════════════════════
```

For a complex item:
```
═══════════════════════════════════
  ⚔ Mythril Blade
  "Forged in starlight."
─────────────────────────────────
  Attack +8
  +5% Dodge
  Bleed on Hit
  Weight: 5
  Type: Weapon  •  Equippable
═══════════════════════════════════
```

**File changes:**
- `Engine/GameLoop.cs` — Replace inline `ShowMessage` with call to new `ShowItemDetail(Item)` method.
- `Display/IDisplayService.cs` — Add `void ShowItemDetail(Item item)`.
- `Display/ConsoleDisplayService.cs` — Full stat card with all non-zero properties, color-coded.

---

### 1.6 — Inventory Display Enhancement (ConsoleDisplayService.cs)
**Priority:** Medium — inventory is viewed often; currently lists names without stats.
**Assigned to:** Hill
**Effort:** 1 hour

**Currently:**
```
═══ INVENTORY ═══
Slots: 4/10 │ Weight: 12/50

  • Short Sword (Weapon) [3 wt]
  • Healing Potion (Consumable) [1 wt]
  • Chain Mail (Armor) [8 wt]
```

**Proposed:**
```
═══ INVENTORY ═══
Slots: 4/10 │ Weight: 12/50

  ⚔ Short Sword         Attack +2           [3 wt]
  🧪 Healing Potion      Heals 25 HP         [1 wt]
  🛡 Chain Mail          Defense +10      [E] [8 wt]
```

Key improvements:
- Type icons replace `(Type)` text — faster scanning.
- Primary stat shown inline — player can evaluate without examining each item.
- `[E]` marker on equipped items — currently no visual indicator.
- Consumables with identical names could be grouped (e.g., `🧪 Healing Potion ×3`) — defer to Phase 3.

**File changes:**
- `Display/ConsoleDisplayService.cs` — Rewrite the `foreach` loop in `ShowInventory` to use columnar layout with type icons, stat summary, equipped marker.
- Needs to check `player.EquippedWeapon == item`, `player.EquippedArmor == item`, `player.EquippedAccessory == item` for the `[E]` marker.

---

## Phase 2: Model Change + Dependent Surfaces (5 hours)

Phase 2 requires the `Tier` property on Item (architectural change) and touches surfaces where the player makes economic decisions.

### 2.0 — Add ItemTier to Item Model
**Priority:** Required foundation for Phase 2.
**Assigned to:** Barton
**Effort:** 1.5 hours

**Changes:**
- `Models/Item.cs` — Add `public ItemTier Tier { get; set; } = ItemTier.Common;` and `ItemTier` enum.
- `Models/LootTable.cs` — Set `Tier = ItemTier.Common` on all Tier1Items, `Tier = ItemTier.Uncommon` on Tier2Items, `Tier = ItemTier.Rare` on Tier3Items.
- `Systems/CraftingSystem.cs` — Set appropriate Tier on crafted result items (crafted items should generally be Uncommon or Rare since they require investment).
- Merchant stock items — Set Tier appropriately.

**Testing note:** Existing tests don't assert on item properties beyond what's needed for logic. Adding a new property with a default value (`Common`) is non-breaking. Romanoff should add a test verifying all LootTable items have non-default Tier set explicitly.

---

### 2.1 — Tier-Colored Item Names (All Surfaces)
**Priority:** High — once Tier exists, this is the single highest-impact visual change.
**Assigned to:** Hill (after Barton completes 2.0)
**Effort:** 1 hour

**Change:** Add a `ColorizeItemName(Item)` helper to `ColorCodes` that wraps item name in tier-appropriate color:
```csharp
public static string ColorizeItemName(Item item) => item.Tier switch
{
    ItemTier.Common   => $"{White}{item.Name}{Reset}",
    ItemTier.Uncommon => $"{Green}{item.Name}{Reset}",
    ItemTier.Rare     => $"{BrightCyan}{item.Name}{Reset}",
    _                 => item.Name
};
```

Then replace all `item.Name` references in display code with `ColorCodes.ColorizeItemName(item)`. This single change cascades across loot drops, inventory, room items, pickup, examination, shop, crafting, and equipment — *every* surface lights up with tier color.

**File changes:**
- `Systems/ColorCodes.cs` — Add `ColorizeItemName` helper.
- All display surfaces from Phase 1 — swap `item.Name` for `ColorizeItemName(item)`.

---

### 2.2 — Merchant Shop Display (GameLoop.cs)
**Priority:** High — buying decisions need information.
**Assigned to:** Hill (display) + Barton (affordability logic if needed)
**Effort:** 1 hour

**Currently:**
```
=== MERCHANT SHOP (Old Bones) ===
[1] Steel Sword — A sturdy blade — 50g
[2] Chain Mail — Solid protection — 75g
[3] Healing Potion — Restores health — 15g
Your gold: 47g
[#] Buy  [X] Leave
```

**Proposed:**
```
╔═══════════════════════════════════════════════════╗
║  🏪 OLD BONES' SHOP                    Gold: 47g ║
╠═══════════════════════════════════════════════════╣
║  [1] ⚔ Steel Sword          Attack +5      50g   ║
║  [2] 🛡 Chain Mail           Defense +10    75g   ║
║  [3] 🧪 Healing Potion       Heals 25 HP   15g   ║
╚═══════════════════════════════════════════════════╝
  [#] Buy  [X] Leave
```

Key improvements:
- Type icons + inline stats (same pattern as inventory).
- **Affordability coloring:** Price in Yellow if player can afford, Red+strikethrough-style dim if they can't.
- Gold total shown in header so player doesn't have to look down.
- Item names colored by tier (from 2.1).

**File changes:**
- `Engine/GameLoop.cs` — Replace inline shop rendering with call to new `ShowMerchantShop(Merchant, Player)`.
- `Display/IDisplayService.cs` — Add `void ShowMerchantShop(Merchant merchant, Player player)`.
- `Display/ConsoleDisplayService.cs` — Implement with box drawing, affordability colors, stat lines.

---

### 2.3 — Crafting Display (GameLoop.cs)
**Priority:** Medium — crafting is less frequent but equally opaque.
**Assigned to:** Hill (display) + Barton (ingredient availability check)
**Effort:** 1 hour

**Currently:**
```
=== CRAFTING RECIPES ===
  Silver Blade: 1x Iron Ore, 1x Silver Dust + 30g → Silver Blade
  Health Elixir: 2x Healing Herb + 10g → Health Elixir
Type CRAFT <recipe name> to craft.
```

**Proposed:**
```
═══ CRAFTING RECIPES ═══

  ✅ Silver Blade                           → ⚔ Silver Blade (Attack +6)
     1x Iron Ore ✓  1x Silver Dust ✓  30g ✓

  ❌ Health Elixir                          → 🧪 Health Elixir (Heals 40 HP)
     2x Healing Herb ✗ (have 1)  10g ✓

Type CRAFT <recipe name> to craft.
```

Key improvements:
- ✅/❌ at-a-glance craftability indicator.
- Ingredient availability check — ✓ if player has enough, ✗ with count if not.
- Result item shows type icon + primary stat (so player knows what they're crafting *before* committing).
- Result item name colored by tier.

**File changes:**
- `Engine/GameLoop.cs` — Replace inline crafting display with call to new `ShowCraftingMenu(List<Recipe>, Player)`.
- `Display/IDisplayService.cs` — Add `void ShowCraftingMenu(List<CraftingRecipe> recipes, Player player)`.
- `Display/ConsoleDisplayService.cs` — Implement with availability checks.
- Barton: Add a `CanCraft(CraftingRecipe, Player)` helper to `CraftingSystem` if one doesn't exist, returning ingredient availability details. Display layer calls this, doesn't re-implement the logic.

---

### 2.4 — Equipped Items Display (EquipmentManager.cs)
**Priority:** Medium — equipment screen currently has stats but no color or tier.
**Assigned to:** Hill
**Effort:** 30 minutes

**Currently:**
```
=== EQUIPMENT ===
Weapon: Short Sword (Attack +2)
Armor: Chain Mail (Defense +10)
Accessory: (empty)
```

**Proposed:**
```
═══ EQUIPMENT ═══
  ⚔ Weapon:    Short Sword       Attack +2
  🛡 Armor:     Chain Mail        Defense +10
  💍 Accessory: (empty)
```

Key improvements:
- Type icons for visual consistency.
- Item names colored by tier.
- Stats colored by type (Red for attack, Cyan for defense).
- Box-drawing header for visual consistency with other surfaces.

**File changes:**
- `Systems/EquipmentManager.cs` — Update `ShowEquipment` to use tier-colored names and stat coloring.
- This is a light touch-up since it already shows stats. Mainly adding color and icons.

---

## Phase 3: Polish & Grouping (4 hours)

Lower-priority improvements that enhance scannability and delight.

### 3.1 — Consumable Grouping in Inventory
**Assigned to:** Hill
**Effort:** 1 hour

**Currently:** Three identical potions listed as three separate lines.

**Proposed:**
```
  🧪 Healing Potion ×3      Heals 25 HP         [1 wt each]
```

**Implementation:** Group inventory items by Name, show count. Only for identical items (same Name + same stats). Display layer only — inventory model stays as a flat list.

**File changes:**
- `Display/ConsoleDisplayService.cs` — Add grouping logic in `ShowInventory`.

---

### 3.2 — Elite Loot Drop Callout
**Assigned to:** Hill (display) + Barton (pass EnemyType to loot display)
**Effort:** 1 hour

**Currently:** Elite enemies drop better loot but the player doesn't know why or that it's special.

**Proposed:**
```
╔══════════════════════════════════════╗
║  ✦ ELITE LOOT DROP                   ║
║  ⚔ Steel Sword [Uncommon]            ║
║  Attack +5  •  Bleed on hit          ║
║  4 wt                                ║
╚══════════════════════════════════════╝
```

- Header says "ELITE LOOT DROP" instead of "LOOT DROP" for elite kills.
- Tier label shown in brackets, colored by tier.

**File changes:**
- `Display/IDisplayService.cs` — Extend `ShowLootDrop(Item item, bool isElite = false)` with optional parameter (non-breaking).
- `Engine/CombatEngine.cs` — Pass enemy type info when calling ShowLootDrop.
- `Display/ConsoleDisplayService.cs` — Conditional header text.

---

### 3.3 — Loot Drop Weight Warning
**Assigned to:** Hill
**Effort:** 30 minutes

When picking up an item would put the player over 80% weight capacity:

```
  ⚠ Inventory weight: 42/50 (84%)
```

When inventory is full and loot is lost:

```
  ❌ Inventory full — Steel Sword was lost!
     Drop something and come back? (items stay on the ground)
```

**File changes:**
- `Display/ConsoleDisplayService.cs` — Add weight warning line to `ShowItemPickup`.
- `Engine/CombatEngine.cs` — The "inventory full" message already exists; enhance with color (Red) and suggestion.

---

### 3.4 — "New Best" Indicator on Loot Drop
**Assigned to:** Hill + Barton
**Effort:** 1.5 hours

When a dropped weapon has higher AttackBonus than the currently equipped weapon:

```
╔══════════════════════════════════════╗
║  ✦ LOOT DROP                        ║
║  ⚔ Steel Sword [Uncommon]           ║
║  Attack +5  (+3 vs equipped!)        ║
║  4 wt                                ║
╚══════════════════════════════════════╝
```

The `(+3 vs equipped!)` is shown in Green. Helps the player immediately recognize upgrades.

**File changes:**
- `Display/ConsoleDisplayService.cs` — In `ShowLootDrop`, compare against player's current equipment. Requires `ShowLootDrop` to accept Player reference or current equipment stats.
- `Display/IDisplayService.cs` — May need signature change: `ShowLootDrop(Item item, Player player, bool isElite = false)`.
- **Architectural note:** This couples the loot drop display to player state. Acceptable because the equipment comparison screen already does exactly this. Follow same pattern.

---

## Summary: Work Allocation

| Phase | Item | Hill | Barton | Romanoff | Hours |
|-------|------|------|--------|----------|-------|
| 1 | 1.1 Loot Drop Display | ✅ | | | 1.0 |
| 1 | 1.2 Gold Acquisition | ✅ | | | 0.5 |
| 1 | 1.3 Room Items | ✅ | | | 0.75 |
| 1 | 1.4 Item Pickup | ✅ | | | 0.5 |
| 1 | 1.5 Item Examination | ✅ | | | 0.75 |
| 1 | 1.6 Inventory Enhancement | ✅ | | | 1.0 |
| 2 | 2.0 ItemTier Model | | ✅ | | 1.5 |
| 2 | 2.1 Tier-Colored Names | ✅ | | | 1.0 |
| 2 | 2.2 Merchant Shop | ✅ | ✅ | | 1.0 |
| 2 | 2.3 Crafting Display | ✅ | ✅ | | 1.0 |
| 2 | 2.4 Equipment Display | ✅ | | | 0.5 |
| 3 | 3.1 Consumable Grouping | ✅ | | | 1.0 |
| 3 | 3.2 Elite Loot Callout | ✅ | ✅ | | 1.0 |
| 3 | 3.3 Weight Warning | ✅ | | | 0.5 |
| 3 | 3.4 "New Best" Indicator | ✅ | ✅ | | 1.5 |
| All | Test coverage for new display methods | | | ✅ | 2.0 |
| | **Totals** | **~8h** | **~4h** | **~2h** | **~14h** |

---

## Implementation Order & Dependencies

```
Phase 1 (no dependencies — can start immediately)
  ├── 1.1 Loot Drop ──┐
  ├── 1.2 Gold         │
  ├── 1.3 Room Items   ├── All independent, Hill can parallelize
  ├── 1.4 Item Pickup  │
  ├── 1.5 Examination  │
  └── 1.6 Inventory ───┘

Phase 2 (sequential dependency)
  2.0 ItemTier Model (Barton) ──→ 2.1 Tier Colors (Hill) ──→ 2.2-2.4 (Hill)
                                                              (tier colors cascade)

Phase 3 (after Phase 1+2 merged)
  ├── 3.1 Consumable Grouping (independent)
  ├── 3.2 Elite Callout (independent)
  ├── 3.3 Weight Warning (independent)
  └── 3.4 "New Best" (depends on 1.1 loot drop format)
```

**Recommended branching:**
- `feature/loot-display-phase1` — Hill, all Phase 1 items
- `feature/item-tier-model` — Barton, 2.0 only
- `feature/loot-display-phase2` — Hill, 2.1-2.4 (branched after 2.0 merges)
- `feature/loot-display-phase3` — Hill + Barton, all Phase 3 items

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Box-drawing characters break on some terminals | Low | Medium | Test on Windows Terminal, macOS Terminal, Linux. We already use box drawing in equipment comparison — same risk. |
| Tier assignment is subjective for crafted/merchant items | Low | Low | Barton assigns; Coulson reviews. Rule of thumb: merchant stock = Uncommon, crafted = Uncommon/Rare. |
| New IDisplayService methods bloat the interface | Medium | Low | We're adding 4-5 methods. IDisplayService already has ~20. Acceptable. If it hits 30+, consider splitting into ILootDisplay, ICombatDisplay, etc. — but not now. |
| Phase 2 model change breaks tests | Low | Medium | `ItemTier.Common` as default means existing items without explicit Tier still work. Romanoff adds assertion that LootTable items have explicit Tier. |
| FormatItemStatLine helper becomes a god method | Medium | Low | Keep it simple: returns `"Attack +N"` / `"Heals N HP"` / `"Defense +N"` based on item type and non-zero stats. No layout logic — callers handle layout. |

---

## What This Plan Does NOT Cover

- **Tooltip/hover system** — We're a console app. Stat cards on examination are the equivalent.
- **Item comparison on pickup** — Phase 3.4 adds a light version ("new best" indicator). Full side-by-side comparison on pickup is deferred (the equip flow already has this).
- **Rarity beyond Tier 3** — No Legendary/Epic tier. Three tiers matches three loot lists. Add more when content demands it.
- **Loot animation/delay** — No typewriter effects or dramatic pauses. Console games benefit from snappy output.
- **Sort/filter in inventory** — Useful but out of scope. Separate work item if Anthony wants it.

---

**Status:** Awaiting Anthony's approval before implementation begins.

— Coulson, Lead

---

## Decision: PR Review Process Enforcement Required
**Date:** 2026-02-22
**Author:** Coulson (Lead)
**Status:** Approved

### Problem
PR #228 was merged to master with a commit message claiming "Reviewed and approved by Coulson (Lead)" but no actual GitHub review was posted. The PR was merged without formal approval, violating the team's PR workflow.

### Decision
**All PRs must receive explicit GitHub approval review before merge.**

The team established a "no direct commits to master" rule after the process violation on 2026-02-22 (see `.ai-team/log/2026-02-22-process-violation-correction.md`). However, the rule is incomplete without enforcement of the review step.

### Requirements
1. **No PR may be merged without at least one approval review posted via `gh pr review <number> --approve`**
2. **Merge commit messages must NOT claim approval unless GitHub review exists**
3. **If urgent hotfix required, reviewer must post approval comment before merge, even if brief**

### Rationale
- Reviews provide audit trail and accountability
- Reviews force explicit technical assessment before code enters master
- Reviews prevent merge-first-review-later scenarios that bypass quality gates

### Applies To
All team members, including the coordinator and Anthony (repository owner).

---

## PR #230 Final Verdict
**Date:** 2026-02-22  
**Author:** Coulson (Lead)  
**Status:** APPROVED and MERGED

### Scope
Phase 1 + Phase 2.0 combined delivery: core models, DisplayService foundation, initial loot display.

### Findings
- ✅ Tests pass: 321/321
- ✅ Display logic properly separated in DisplayService
- ✅ New methods (`ShowLootDrop`, `ShowItemPickup`, `ShowItemDetail`) follow established pattern
- ✅ ItemTier enum clean and integrated into Item model and LootTable
- ✅ Color usage consistent (Cyan for stats, Yellow for loot)
- ✅ Backward compatibility maintained via default `ItemTier.Common`

### Decision
**Approved.** Master branch ready for Phase 2.1 build.

---

## PR #231 Verdict
**Date:** 2026-02-22  
**Author:** Coulson (Lead)  
**Status:** APPROVED and MERGED

### Scope
Phase 2.1–2.4: tier-colored display system (`ShowShop`, `ShowCraftRecipe`, centralized tier colorization).

### Findings
- ✅ Code Quality: `ShowShop` and `ShowCraftRecipe` implementations clean, properly decoupled via DTOs/tuples
- ✅ Tests: Found `ShopDisplayTests` and `CraftRecipeDisplayTests` commented out in `TierDisplayTests.cs`
  - Action taken: Uncommented and updated to match new `IDisplayService` signatures
  - All tests now passing
- ✅ Design: Tier colorization logic centralized in `ColorizeItemName` as requested
- ✅ Merge: Squashed and merged to master

### Decision
**Approved.** Master ready for Phase 3 work.

---

## PR #232 Verdict: Phase 3 Loot Polish
**Date:** 2026-02-22  
**Author:** Coulson (Lead)  
**Status:** APPROVED and MERGED

### Scope
Phase 3 loot UX polish: consumable grouping, elite loot callout + tier labels, weight warning, "new best" indicator.

### Findings
- ✅ Tests: 359/359 tests passed (100% green, up from 342)
- ✅ Implementation Quality:
  - `ShowInventory` grouping logic is purely presentational and effective
  - `ShowLootDrop` correctly handles elite status and "new best" comparisons
  - `ShowItemPickup` weight warnings respect `>80%` threshold (confirmed by tests)
  - `IDisplayService` changes consistent with plan
- ✅ Architecture:
  - No coupling violations; display logic remains in `DisplayService`
  - Game loop and combat engine only pass necessary data (`isElite`, `player`)

### Learnings
- Use of `Action` delegates for display injection remains robust
- Pattern emerging: passing `Player` context to display methods for feature richness acceptable but warrants monitoring
- Current state: display methods only read properties (no mutations), no logic leakage observed

### Decision
**Approved.** Squashed and merged to master. Commit: 4b839bf

---

## Decision: ShowLootDrop `player` Parameter is Required, Not Optional
**Date:** 2026-02-22  
**Author:** Hill (Developer)  
**Status:** Informational

### Technical Detail
```csharp
void ShowLootDrop(Item item, Player player, bool isElite = false)
```

The `player` parameter has **no default value**. This was intentional.

### Rationale
- All loot drop scenarios have player in scope
- Making `player` optional would allow callers to silently skip "new best" comparison
- Forcing explicit required parameter prevents accidental null comparisons and regressions
- Consistency with Phase 3 design: feature richness requires player context

---

## Decision: Color Code Substitution (BrightYellow/BrightGreen Missing)
**Date:** 2026-02-22  
**Author:** Hill (Developer)  
**Status:** Informational

### Issue
Phase 3 spec referenced `ColorCodes.BrightYellow` and `ColorCodes.BrightGreen` for elite and Uncommon tier labels. Neither constant exists in `ColorCodes.cs`.

### Resolution
- **Elite header:** Used `ColorCodes.Yellow` (closest equivalent)
- **Uncommon tier label:** Used `ColorCodes.Green`
- Pre-existing constants maintain color scheme consistency

### Rationale
Avoid breaking builds with missing references. If BrightYellow/BrightGreen are added to ColorCodes in future, they can be substituted back.

---

## Decision: Phase 3 Loot UX — Test Coverage Scope
**Date:** 2026-02-22  
**Author:** Romanoff (Tester)  
**Status:** Informational

### Scope
Test coverage for Phase 3 looting UX features. New test file: `Dungnz.Tests/Phase3LootPolishTests.cs` with 17 unit tests, all passing.

### Coverage Details

#### 3.1 — Consumable Grouping (ShowInventory)
- Three identical potions → `×3` badge ✅
- Different items stay separate (name-only grouping) ✅
- Single item → no multiplier badge ✅
- Empty inventory → clean output (edge case) ✅

#### 3.2 — Elite Loot Callout (ShowLootDrop)
- `isElite: true` → "ELITE LOOT DROP" header ✅
- `isElite: false` → "LOOT DROP" without "ELITE" ✅
- Uncommon item → `[Uncommon]` badge ✅
- Rare item → `[Rare]` badge ✅
- Common item → `[Common]` badge ✅

#### 3.3 — Weight Warning (ShowItemPickup)
- 85% weight → ⚠️ + "nearly full" ✅
- 79% weight → no warning ✅
- Exactly 80% weight → no warning (boundary: strict `>` not `>=`) ✅
- 82% weight → confirms threshold behavior ✅

#### 3.4 — New Best Indicator (ShowLootDrop)
- Attack +5 vs +2 equipped → "+3 vs equipped" (delta + improvement) ✅
- Attack +5 vs +5 equipped → no indicator (no improvement) ✅
- Attack +3 vs +5 equipped → no indicator (downgrade) ✅
- No weapon equipped → no indicator (null guard) ✅

### Key Design Observations

1. **Exact 80% boundary is exclusive**
   - Uses `weightCurrent > weightMax * 0.8` (strict greater-than)
   - Exactly 80% does NOT trigger warning
   - Tests document both sides of boundary

2. **"New best" only for positive delta, weapons, equipped context**
   - Non-weapon items don't trigger comparison
   - Zero-delta and negative-delta skip indicator
   - Requires weapon equipped

3. **Grouping is name-based**
   - Items with same Name but different stats would group
   - Acceptable for Phase 3; potential edge case for future (crafted upgrades)

### Infrastructure Fix
Pre-existing `CS1744` error in `TierDisplayTests.cs` line 390 (FluentAssertions `ContainAny` named-arg conflict) fixed during test work. Removed redundant `because:` argument. This was blocking all 342 tests from compiling.

### Verdict
Phase 3 test coverage complete. All 17 tests pass. Hill's implementation confirmed correct against all specified Phase 3 behaviors.

### 2026-02-22: Content Expansion Plan — Phase 1
**By:** Coulson (with Hill, Barton, Romanoff)
**What:** Full plan for greatly expanding game content (10 → 60+ items, 10 → 18 enemies)
**Why:** Anthony approved — awaiting final implementation approval

## Executive Summary
We will expand the game from a "tech demo" scope (10 items) to a "playable game" scope (60+ items).
**Strategy:** Fix code limitations first, then flood the game with data.
**Risk:** High risk of "silent failures" (invalid tiers, broken equips) without Phase 1 code fixes.

## Phase 1: Code & Systems Prep (Hill + Romanoff)
*Before adding a single item, we must ensure the engine can handle them.*

1.  **Fix Accessory Logic (Critical):**
    *   Update `InventoryManager.UseItem()` to handle `ItemType.Accessory`.
    *   Ensure `Player.Equip()` correctly calculates stats from accessories.
2.  **Harden Data Validation:**
    *   Update `ItemConfig.Load()` to throw exceptions on invalid Tiers (currently defaults to Common).
    *   Validate `Weight` > 0.
    *   Validate `Name` length < 30 chars (to prevent UI breaking).
3.  **Display Safety:**
    *   Update `DisplayService` to truncate names > 30 chars with "..." instead of breaking layout.

## Phase 2: Content Injection (Barton)
*Once the pipes are fixed, turn on the water.*

### New Item Types (Enabled by Phase 1)
*   **Accessories:** Ring of Vitality, Amulet of the Sage, Boots of Swiftness, Pendant of Resistance.

### Weapons (12 New)
*   **Tier 1:** Iron Dagger, Wooden Staff, Rusty Spear, Bone Club
*   **Tier 2:** Executioner's Axe, Poisoned Blade, Lightning Staff, Warlord's Maul
*   **Tier 3:** Starfall Blade, Inferno Tome, Dragonsoul Axe, Soulreaver Dagger

### Armor (12 New)
*   **Tier 1:** Padded Tunic, Wooden Shield, Fur Cloak, Iron Helm
*   **Tier 2:** Scale Mail, Knight's Breastplate, Elven Leathers, Mithril Chainmail
*   **Tier 3:** Obsidian Plate, Dragon Scale Armor, Veil of Stars

### Consumables (12 New)
*   **Potions:** Minor/Greater Health, Mana Draught, Regeneration Elixir
*   **Tactical:** Antidote (Cures Poison), Escape Scroll, Bomb
*   **Buffs:** Fortitude Elixir (+DEF), Power Draught (+ATK)

### Enemies (8 New)
*   **Tier 1:** Goblin Scout (Fast), Zombie (Tank)
*   **Tier 2:** Orc Berserker (Burst), Frost Wraith (Debuff), Giant Spider (Poison)
*   **Tier 3:** Void Sentinel (High Dodge), Demon Lord (Boss-tier stats)

## Phase 3: Verification (Romanoff)
1.  **Loot Distribution Test:** Simulate 10,000 drops to ensure Tiers 1/2/3 drop at expected 60/30/10% rates (approx).
2.  **UI Regression:** Verify inventory screen doesn't break with 20+ items.
3.  **Combat Balance:** Auto-resolve 100 battles to ensure no "immortal" enemies or "one-shot" players.

## Work Items

| ID | Title | Assignee | Dep |
|----|-------|----------|-----|
| `code-fix-accessory` | Implement Accessory equip logic in InventoryManager | Hill | - |
| `code-harden-json` | Add validation for Tiers/Weight/Length in ItemConfig | Hill | - |
| `data-add-items` | Add all new items to item-stats.json | Barton | `code-harden-json` |
| `data-add-enemies` | Add all new enemies to enemy-stats.json | Barton | - |
| `test-loot-dist` | Verify loot drop rates with new larger pool | Romanoff | `data-add-items` |

## Technical Decisions
*   **No new mechanics yet:** Stackable items and Two-handed weapons are deferred to Phase 2 to keep this expansion purely data-driven (mostly).
*   **Strict Name Limits:** All item names must be <= 30 chars to avoid complete UI rewrite.
### 2026-02-22: Map UI/UX improvement — added to content expansion plan
**By:** Coulson
**What:** Updated the Content Expansion Plan to include a "Phase 4: Map & UI Overhaul" focused on tactical readability and visual polish. This phase integrates strict fog-of-war, room type color coding, and corridor rendering.

**Why:** Anthony requested map UI/UX improvements. The current map is monochromatic and lacks connectivity indicators, making navigation difficult and unrewarding.

#### New Phase 4: Map & UI Overhaul
*Prerequisites: Phase 1 (Core Content)*

**1. Room Model Updates** (Hill)
- Add `bool IsExplored` to `Room` class (true if visited OR seen from adjacent room).
- Add `RoomType` metadata to support color mapping.

**2. Enhanced Map Rendering** (Hill)
- **Corridors:** Implement 2-pass rendering to draw `│` and `─` connectors between rooms based on `Exits`.
- **Fog of War:**
  - Hide completely unknown rooms.
  - Render "seen" (adjacent to visited) rooms as `[ ]` (Gray).
  - Render visited rooms with full state symbols.
- **Colors:** Apply `Systems.ColorCodes` based on room state/type.

**3. Map Symbols & Legibility** (Barton)
- **Standardized Legend:**
  - `[*]` **You** (Bright White)
  - `[!]` **Enemy** (Red) / `[B]` **Boss** (Bold Red)
  - `[E]` **Exit** (Green)
  - `[~]` **Hazard** (Yellow - Dark/Scorched/Flooded)
  - `[?]` **Lootable** (Cyan - Visited but has items/shrine)
  - `[+]` **Cleared** (Dark Gray - Empty & safe)
  - `[ ]` **Unexplored** (Dim Gray - Adjacent edge)

**4. UI Polish** (Hill)
- Add a "Depth/Floor" indicator to the map header.
- Ensure legend prints dynamically based on what is actually visible on the current map.
### 2026-02-22: GitHub issues created for content expansion plan
**By:** Coulson
**What:** Created 16 GitHub issues covering all 4 phases of the content expansion plan
**Why:** Anthony approved the plan — issues track implementation work

### 2026-02-22: Structural enforcement of no-direct-master rule
**By:** Coulson (Lead)
**What:** Added scripts/pre-push git hook that blocks all pushes to master. git config core.hooksPath=scripts activates it. Scribe charter updated to explicitly require branch+PR workflow for .ai-team/ commits.
**Why:** Direct commits to master occurred twice (commits 402fa5f, 41b7acf from Scribe). Process-only reminders failed. Hook enforces the rule mechanically — a push to master fails with an error regardless of agent instructions.
# Decision: Define Balance Budget Before Enemy Content

**Status:** Proposed  
**Owner:** Coulson  
**Date:** 2026-02-22  
**Context:** Retrospective finding

## Problem
Combat balance tests in Phase 3 caught the Lich King imbalance, but those tests were written *after* 8 enemies were tuned by hand. Balance tests should assert a spec, not discover one.

## Proposal
For future enemy content phases, define a balance budget upfront:
- Damage ranges per enemy tier (e.g., Lvl 1-3: 5-8 ATK, Lvl 4-6: 10-15 ATK)
- HP tiers per zone (e.g., Zone 1: 20-40 HP, Zone 2: 50-80 HP)
- Win rate boundaries at target level ranges (e.g., Lvl 12 vs. Lich King: 40-60% win rate)

Balance tests then assert conformance to the budget. Enemy tuning becomes spec-driven, not ad-hoc.

## Impact
- Tests become validators, not explorers
- Enemy design has clear constraints
- Balance issues surface during design, not after implementation

## Open Questions
- Who owns the balance budget definition? (Coulson? Barton? Anthony?)
- Should we retrofit a budget for existing enemies?
# Decision: No Enemy Ships Without Balance Test

**Status:** Proposed  
**Owner:** Romanoff (enforcer), Barton (implementer)  
**Date:** 2026-02-22  
**Context:** Retrospective finding

## Problem
Phase 2 added 8 enemies before Phase 3 added balance tests. Those 8 enemies were tuned blind. The Lich King imbalance was caught retroactively by tests that should have been gates, not validators.

## Proposal
Establish a policy: **No enemy ships without at least a win-rate boundary test at its target level range.**

Example:
- Enemy: Lich King (Lvl 10-12 content)
- Required test: Assert 40-60% win rate for Lvl 12 player with median gear

This test must exist and pass before the enemy is merged. Balance tests gate content, not validate it after the fact.

## Impact
- Balance issues surface during design, not after merge
- Romanoff has clear acceptance criteria for enemy PRs
- Reduces balance rework and post-merge tuning

## Consequences
- Slower enemy addition initially (tests must be written first)
- Requires stat ranges to be defined upfront (see balance budget decision)

## Open Questions
- What's the minimum acceptable test? (just win rate? damage variance? duration?)
- Who writes the test? (Barton with the enemy? Romanoff before merge?)
# Decision: Repo Setup Checklist Before Agent Work

**Status:** Proposed  
**Owner:** Coulson  
**Date:** 2026-02-22  
**Context:** Retrospective finding

## Problem
Two direct master commits occurred before pre-push hook enforcement. The hook was added reactively after violations, not proactively as a setup step. Guardrails should precede agent work, not follow mistakes.

## Proposal
Define a repo setup checklist that must be completed before any agent begins feature work:

**Guardrails:**
- [ ] Pre-push hook installed (prevents direct master commits)
- [ ] Branch protection rules configured (if using hosted Git)
- [ ] Linting enforcement at pre-commit (if applicable)
- [ ] CI pipeline validates on all branches

**Tooling:**
- [ ] Test runner configured and passing baseline
- [ ] Build scripts tested and documented
- [ ] Agent model assignments documented (who uses which model)

**Documentation:**
- [ ] README reflects current architecture
- [ ] Team charter and agent roles defined
- [ ] Decisions log initialized

## Impact
- Prevents process violations before they happen
- Makes onboarding deterministic (checklist, not tribal knowledge)
- Shifts quality enforcement left (setup, not cleanup)

## Open Questions
- Who owns checklist execution? (Coulson? Anthony?)
- Should this be a script that validates the repo state?
# Decision: Agent Stall Escalation Policy

**Status:** Proposed  
**Owner:** Coulson  
**Date:** 2026-02-22  
**Context:** Retrospective finding

## Problem
gemini-3-pro-preview stalled twice during this session with no immediate escalation. Each stall burned time that could have been reallocated. Currently there's no defined policy on when to escalate vs. retry.

## Proposal
Establish a stall policy:
- **Stall definition:** No meaningful diff output for 20 consecutive minutes
- **Action:** Immediate escalation to Lead (Coulson)
- **Lead response:** Reassign work to a different agent/model or break the task into smaller chunks
- **No retries without change:** If the same agent stalls twice on the same task, the task is too large or the model is unsuitable

## Impact
- Reduces time waste from unproductive agent loops
- Makes stalls a first-class signal, not a "wait and see" situation
- Creates a clear handoff protocol

## Open Questions
- Should the 20-minute threshold be configurable per task type? (e.g., longer for complex refactors)
- Who monitors for stalls? (manual observation? automated timeout?)
# Decision: Systems Spec Before Content Spec

**Status:** Proposed  
**Owner:** Coulson  
**Date:** 2026-02-22  
**Context:** Retrospective finding

## Problem
Phase 4 (map UI overhaul) came after Phase 2 (enemy/item content). This created retrofit work — Barton had to untangle draw-order assumptions and room population logic that Phase 2 had already hardcoded. Content defined the box instead of filling it.

## Proposal
For future roadmaps, enforce sequencing:
1. **Systems primitives first:** Lock map rendering layers, combat stat ranges, inventory constraints
2. **Content second:** Add enemies, items, rooms within the system constraints
3. **Polish third:** UI/UX refinements, balance tuning, test expansion

Content should fill the box; it should not define the box.

## Impact
- Reduces retrofit work and dependency inversions
- Makes content addition more mechanical and parallelizable
- Systems changes become less risky (no content to break)

## Consequences
- Requires upfront systems design before content work begins
- May feel slower initially (more planning phase) but faster overall (less rework)

## Open Questions
- How much systems design is "enough" before content can start?
- Can content and systems run in parallel if seams are well-defined?

# Decision: UI/UX improvement plan produced

**Status:** Approved  
**Owner:** Coulson  
**Date:** 2026-02-22  
**Context:** Boss requested UI/UX evolution; retrospective flagged display systems as an improvement area.

## What
Team-aligned plan for aesthetic and gameplay UI/UX improvements across three phases: Combat Feel (Phase 1), Navigation & Exploration Polish (Phase 2), and Information Architecture (Phase 3). 20 specific work items identified, each with owner, implementation notes, and interface-change flags.

## Why
The game's mechanical depth (status effects, abilities, multi-floor dungeons, achievements, crafting) has outpaced the rendering layer — players can't see the systems they're interacting with.

## Plan Location
`.ai-team/plans/uiux-improvement-plan.md`

## Key Decisions in the Plan
- `ShowCombatStatus` signature extended with active effect lists (IDisplayService change — requires Romanoff test coordination)
- `ShowCommandPrompt` extended with Player parameter for persistent status mini-bar (IDisplayService change — wide test impact, use optional overload)
- Victory/GameOver screens moved from GameLoop private methods to IDisplayService (structural improvement, not just cosmetic)
- NarrationService stays in GameLoop — flavor strings passed as parameters to display methods, not injected into ConsoleDisplayService
- ANSI-safe box padding standardized via `ColorCodes.StripAnsiCodes()` pattern (fixes existing `ShowLootDrop` bug)


# Decision: Achievement notifications in combat

**Status:** Implemented  
**Owner:** Barton  
**Date:** 2026-02-24  
**Context:** Issue #280 — achievement unlocks should be visible during combat, not missed by players

## What
Added `OnAchievementUnlocked` event to `GameEvents`. CombatEngine wires milestone checks to fire the event at turn boundaries, displaying achievement banners during active combat.

## Why
Previously, achievement notifications were only shown after combat ended. Players engaged in complex battles could miss unlock moments entirely. Moving achievement feedback into combat loop keeps players informed of progress in real time.

## Implementation
- New event: `GameEvents.OnAchievementUnlocked`
- CombatEngine checks milestones at turn end and fires event
- Display layer renders achievement banner immediately
- Does not interrupt combat flow

## Impact
- Players see achievement unlocks immediately when earned
- Increases sense of progression and engagement
- No mechanical changes; purely feedback loop improvement

## Quality
- Merged in PR #309
- Tested with existing achievement system tests
- No new test failures

# Decision: RunStats type — confirmed exists

**Status:** Resolved  
**Owner:** Barton  
**Date:** 2025-01-20  

The UI/UX shared infrastructure spec requires a `RunStats` type for `ShowVictory` and `ShowGameOver` display methods. **CONFIRMED:** `RunStats` already exists in the codebase at `Systems/RunStats.cs` (`Dungnz.Systems.RunStats`).

Existing class provides all necessary fields for end-of-run display:
- Kills → `EnemiesDefeated`
- Gold earned → `GoldCollected`
- Items found → `ItemsFound`
- Floors cleared → `FloorsVisited`
- Play time → `TimeElapsed`
- Damage stats → `DamageDealt`, `DamageTaken`

RunStats exists and is ready to use. No new type needs to be created.

# Decision: Phase 0 UI/UX infrastructure — complete and merged

**Status:** Implemented  
**Owner:** Coulson  
**Date:** 2026-02-22  

Phase 0 of the UI/UX improvement plan is complete and merged to master. All shared infrastructure is now in place, unblocking Phase 1 combat enhancements.

**Phase 0 deliverables (complete):**

1. **RenderBar() helper** — Private static method in ConsoleDisplayService. Width-normalized progress bars for HP/MP/XP displays using filled/empty blocks and ANSI colors.

2. **ANSI-safe padding helpers** — VisibleLength, PadRightVisible, PadLeftVisible methods. Fixes alignment bugs in ShowLootDrop and ShowInventory.

3. **New IDisplayService methods** — ShowCombatStatus extended with active effects; ShowCommandPrompt extended with optional Player parameter; 7 new stub methods added (ShowCombatStart, ShowCombatEntryFlags, ShowLevelUpChoice, ShowFloorBanner, ShowEnemyDetail, ShowVictory, ShowGameOver).

**Quality gates:**
- Build: 0 errors
- Tests: 416 tests passing
- Architecture: Phase 0 changes merge cleanly with Barton's Phase 1 prep
- Backward compatibility: ShowCommandPrompt optional parameter preserves existing call sites

**Key decisions:**
- RenderBar is private (internal utility, not public contract)
- ANSI padding helpers in display layer (not ColorCodes)
- Stub implementations for Phase 1-3 (enables parallel work)
- Barton's systems changes implemented before Phase 0 merged
- Achievement notifications (1.9) deferred to future phase

**Merged PRs:** #298 (Hill Phase 0), #299 (Barton Phase 1 prep)

---

# Decision: ASCII Art for Enemy Encounters — Feasibility Assessment

**By:** Coulson  
**Date:** 2026-02-24  
**Status:** RESEARCH COMPLETE — Ready for Phase Planning

## Executive Summary

Adding ASCII art for enemies is **architecturally feasible and low-risk**. The display layer is well-abstracted, multi-line output is already established, and enemy data can be extended without disrupting existing systems. **Effort estimate: Phase 1 (small, 1–2 work items).**

## Key Findings

### 1. Architectural Fit: Display Layer Already Supports Multi-Line Blocks ✅

**Current Architecture:**
- `IDisplayService` is the central abstraction for all console output
- No `Console.Write` calls exist in game logic — all output routed through `DisplayService`
- DisplayService already renders complex multi-line structures:
  - Enhanced title screen with colored box-drawing (ShowEnhancedTitle, 9 lines)
  - Class selection cards with stat bars (SelectClass, 20+ lines each, 3 options)
  - Equipment comparison with dynamic padding (ShowEquipmentComparison, 8–10 lines)
  - Loot drop cards with box-drawing (ShowLootDrop, 6 lines)
  - Enemy detail cards (ShowEnemyDetail, 36-wide box, multi-stat display)

**Conclusion:** Multi-line ASCII art blocks fit naturally into IDisplayService without requiring interface changes. The display abstraction is flexible and extensible.

### 2. Integration Point: Combat Start Banner ✅

**Natural Location:** `ShowCombatStart(Enemy enemy)` method

**Where ASCII Art Fits:**
- **BEFORE** current banner: Enemy portrait/icon above the "COMBAT BEGINS" line
- **AFTER** banner: Enemy silhouette/portrait below the enemy name
- **Alternative:** Replace the current banner section entirely with a more decorative art-based layout

**Call Site is Stable:** The method already receives the Enemy object, so accessing enemy type/name is straightforward.

### 3. Console Size and Layout Constraints ✅

**ASCII Art Size Recommendation:**
- **Width:** 30–42 characters (fits within standard 80-char terminal with 2-space margins)
- **Height:** 5–10 lines (keeps combat start banner readable without excessive scrolling)
- **Safe pattern:** Match existing card widths (36 chars inner content)

### 4. Scope Estimate: Phase 1 Effort ✅

**Work Breakdown:**

1. **Art Design & Definition** (Small)
   - Design 10–12 ASCII portraits
   - ~5–10 min per portrait (text-based, hand-drawn, 5–8 lines each)
   - **Estimate:** 1 short work item

2. **Display Integration** (Small)
   - Extract art data into a structure (static method or lookup dict)
   - Modify ShowCombatStart to render enemy portrait between banner and name
   - Add color support (use existing ColorCodes utility for enemy type theming)
   - **Estimate:** 0.5 work item (1–2 hours)

3. **Testing** (Small)
   - Verify art renders correctly for all enemy types
   - Check terminal width edge cases
   - Spot-check visual alignment with existing UI
   - **Estimate:** 0.5 work item

**Total Phase 1 Effort:** ~2 work items, ~6–8 hours of implementation + test work.

## Risks and Mitigations

### 1. Terminal Width Compatibility ⚠️
**Mitigation:** Add Console.WindowWidth check; provide compact fallback art for terminals < 60 chars

### 2. ANSI Color Portability ⚠️
**Mitigation:** Stick to 16 standard ANSI colors already in use; provide monochrome fallback

### 3. Test Complexity 🟡
**Mitigation:** Don't snapshot-test exact art; test behavioral verification instead (output non-empty lines, etc.)

### 4. Maintenance Burden 🟡
**Mitigation:** Store art in isolated section (AsciiArtRegistry class); consider Phase 2 migration to JSON if collection grows

### 5. Visual Consistency 🟡
**Mitigation:** Establish simple template (max 8 lines, 36 chars wide); use consistent character palettes; single designer for all art

## Recommendation

**Go ahead.** ASCII art for enemy encounters is a **low-risk, high-flavor addition** that:
- ✅ Fits cleanly into the existing display architecture
- ✅ Requires no interface changes (just a ShowCombatStart enhancement)
- ✅ Uses existing color and box-drawing infrastructure
- ✅ Is sized appropriately for the console UI (5–10 lines, 30–42 chars wide)
- ✅ Is a small Phase 1 effort (~2 work items)
- ✅ Has clear, manageable risks with straightforward mitigations

**Phase 2 possibilities:** Per-type color theming, elite/boss variants, animated frames.

