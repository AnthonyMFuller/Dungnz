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
