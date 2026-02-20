# Design Review Ceremony — 2026-02-20

**Facilitator:** Coulson  
**Participants:** Hill (C# Dev), Barton (Systems Dev)  
**Duration:** Focused  
**Objective:** Establish interface contracts between Hill's and Barton's domains before Phase 1 implementation

---

## Key Decisions / Contract Agreements

### 1. CombatEngine Interface
**Decision:** `CombatEngine.StartCombat(Player player, Enemy enemy) → CombatResult`
- **Type:** Blocking call, returns enum `{Won, Fled, PlayerDied}`
- **Ownership:** Barton implements, Hill calls from GameLoop
- **Edge case agreed:** If flee penalty kills player, result is `PlayerDied`, not `Fled`
- **Thread model:** Fully synchronous blocking loop (no threading concerns)

### 2. DisplayService Integration
**Decision:** CombatEngine receives DisplayService via constructor injection
- **Contract methods Hill will provide:**
  - `ShowCombatStatus(Player player, Enemy enemy)`
  - `ShowCombatMessage(string message)`
  - `ShowInventory(Player player)`
  - `ShowLootDrop(Item item)`
- **Rule:** Barton's code calls DisplayService methods only — NO raw Console.Write

### 3. Inventory System Interface
**Decision:** Barton exposes `InventoryManager` with these methods:
- `TakeItem(Player player, Item item) → bool` (false if inventory full)
- `UseItem(Player player, string itemName) → UseResult` (enum: Success/NotFound/InvalidContext)
- `EquipItem(Player player, string itemName) → bool`
- `GetInventorySummary(Player player) → IReadOnlyList<Item>`
- **Error handling:** UseItem returns enum (not exception) when item not found

### 4. LootTable Interface
**Decision:** `LootTable.RollDrop(Enemy enemy) → LootResult { Item? item, int gold }`
- **Rationale:** Separates gold from item drops cleanly
- **Ownership:** Barton implements, Hill calls post-combat or from DungeonGenerator
- **Gold handling:** LootResult contains gold value; caller adds to Player.Gold directly

### 5. Model Contracts (Hill owns definitions, Barton mutates)
**Player:**
- Fields: `HP, MaxHP, Attack, Defense, Level, Gold, XP, InventorySlots (List<Item>), EquippedWeapon (Item?), EquippedArmor (Item?)`
- **Mutability:** Barton's systems modify directly

**Enemy:**
- Fields: `HP, MaxHP, Attack, Defense, Name, Type (enum), XPReward, GoldReward`
- **Death check:** `HP <= 0` (no IsAlive flag)
- **Ownership:** Hill owns base class, Barton owns 5 subclasses (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss)

**Item:**
- Fields: `Name, Type (enum: Weapon/Armor/Consumable), Value, AttackBonus, DefenseBonus, HealAmount`
- **Type:** Enum, not string

**Room:**
- Enemy storage: **Single `Enemy?` reference** (nullable)
- Clear on death: CombatEngine sets `enemy.HP = 0`, GameLoop nulls Room.Enemy
- Items: `List<Item>` — LootTable writes here post-combat

### 6. UseItem Context Handling
**Decision:** Single `UseItem(Player, string)` method; behavior inferred from GameLoop state
- **In combat:** Heal/buff items work, equip is deferred
- **Overworld:** All item types work
- **Implementation:** Barton's InventoryManager checks Player state or receives context flag if needed (deferred to implementation)

### 7. Enemy Factory Pattern
**Decision:** If Barton's enemy constructors are complex, Hill will use `IEnemyFactory.Create(EnemyType) → Enemy`
- **Deferred:** Start with simple constructors; add factory only if DI/complexity demands it

### 8. Dungeon Boss Uniqueness
**Decision:** Boss is spawned once by DungeonGenerator, guards exit room
- **Loot:** Boss drops loot only on first death (LootTable checks state or Hill's Room tracks "looted" flag)
- **Respawn:** Boss does NOT respawn in v1

### 9. Stat Application Timing
**Decision:** Equipping weapon/armor applies bonuses immediately
- **Mid-combat equip:** DisplayService shows updated stats next turn
- **Responsibility:** InventoryManager updates Player.Attack/Defense when equipping

---

## Action Items

### Hill (C# Dev) — FIRST
1. Define Models: Player, Enemy (base), Item, Room, Direction enums
2. Define `CombatResult` enum: Won, Fled, PlayerDied
3. Define `UseResult` enum: Success, NotFound, InvalidContext
4. Define `LootResult` struct: `{ Item? item, int gold }`
5. Create DisplayService interface/class with agreed methods
6. Create `IEnemyFactory` interface (even if not used initially)

### Barton (Systems Dev) — DEPENDS ON HILL
1. Implement 5 Enemy subclasses (Goblin, Skeleton, Troll, DarkKnight, DungeonBoss)
2. Implement CombatEngine.StartCombat (consumes DisplayService)
3. Implement InventoryManager (all methods)
4. Implement LootTable.RollDrop

### Both (Parallel after models defined)
- Hill: DungeonGenerator, GameLoop, CommandParser
- Barton: Combat logic, inventory logic, loot tables

---

## Risks Raised

### Critical Risks
1. **GameLoop → CombatEngine handoff** — Agreed as blocking `StartCombat()` call; Hill's GameLoop owns state transitions pre/post combat
2. **DisplayService dependency injection** — CombatEngine must receive DisplayService in constructor (Hill instantiates both)
3. **Player death handling** — CombatEngine returns `PlayerDied`; GameLoop shows game-over and exits (Barton does NOT call Environment.Exit)

### Medium Risks
4. **Flee destination selection** — GameLoop picks random adjacent room after `CombatResult.Fled`
5. **Enemy reference in Room** — Single nullable `Enemy?`; cleared by GameLoop when `enemy.HP <= 0`
6. **Inventory full handling** — `TakeItem` returns false; GameLoop shows "inventory full" via DisplayService

### Low Risks
7. **Item use context** — Deferred to Barton's implementation (can add context flag later if needed)
8. **Enemy factory** — Start without factory; add if needed during implementation
9. **Boss loot uniqueness** — Handle via Room "looted" flag or LootTable state (Hill and Barton coordinate during implementation)

---

## Ceremony Outcome
✅ **Contracts agreed**  
✅ **Dependencies clear: Hill implements models first, Barton depends on them**  
✅ **No blocking ambiguities remain**  

**Next step:** Hill begins Phase 1 (Models + DisplayService), then Barton begins his systems.
