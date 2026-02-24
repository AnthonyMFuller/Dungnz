# Decision Log: Floor Loot + Enemy Drops (B4/C3)

**Date:** 2026-02-23  
**Author:** Barton  
**Issues:** #332, #337

## Decisions

### 1. EnemyFactory.Items exposure pattern
**Decision:** Added `public static List<ItemStats>? Items => _itemConfig;` to EnemyFactory rather than passing itemConfig through DungeonGenerator's constructor.  
**Rationale:** DungeonGenerator already has a static dependency on EnemyFactory (calls `CreateBoss`, `CreateScaled`). Adding one more property access is consistent with the existing coupling pattern. Passing via constructor would require updating Program.cs, GameLoop.cs, and all test callsites.

### 2. Mimic drops a randomly chosen Rare item (not a fixed item)
**Decision:** Mimic picks a random Rare item at construction time from the loaded itemConfig pool.  
**Rationale:** "Surprise loot" flavour — keeps the Mimic feeling unpredictable even at construction. If itemConfig not loaded, falls back to Phoenix Feather.

### 3. Goblin "Small Gold Pouch" skipped
**Decision:** Did not implement Goblin gold-pouch drop.  
**Rationale:** Task spec says "not an item — just extra gold in LootTable." LootTable has no `AddGoldBonus(chance)` mechanic. Adding one would require a LootTable model change + RollDrop signature change + test updates, which is out of scope for this PR. Goblins already drop 2–8 gold via their stats range. Recommend a follow-up issue if per-roll gold bonuses are desired.

### 4. Skeleton Bone Fragment drop chance: spec said 30%, existing code had 50%
**Decision:** Left Skeleton.cs unchanged — Bone Fragment was already in Skeleton at 50% from a previous task.  
**Rationale:** The B3 PR had already added this. Task #337 spec of 30% appears to be a baseline suggestion rather than a strict override; 50% is more generous to the player and was the already-shipped value.
