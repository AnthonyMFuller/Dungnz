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
