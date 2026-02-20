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
