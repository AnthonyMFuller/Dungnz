# Decision: B3 Loot Pool Architecture

**Date:** 2026-02-23
**Author:** Barton
**Task:** B3 — Expand Loot Table Pools

## Decision: Wiring via EnemyFactory.Initialize(), not LootTable constructor

**Context:** LootTable needed to load tier pools from ItemConfig (Systems layer), but LootTable lives in the Models layer. Direct dependency would create circular reference (Systems → Models already exists).

**Decision:** Added `LootTable.SetTierPools(tier1, tier2, tier3)` static method. EnemyFactory.Initialize() calls it after loading item config. No Models → Systems dependency created.

**Alternatives rejected:**
- Passing `List<ItemStats>` to LootTable constructor: would require updating all 9 enemy constructors
- Static cache in ItemConfig: global mutable state harder to test
- Making LootTable depend on ItemConfig directly: circular dependency

## Decision: Keep fallback lists for test isolation

Fallback static lists (FallbackTier1/2/3) preserved so tests that create LootTable without calling EnemyFactory.Initialize() continue to pass. The distribution simulation test specifically relies on these to validate tier ratios.

## Decision: Boss Key excluded by name in GetByTier()

Boss Key is excluded by name check in `ItemConfig.GetByTier()` rather than by a flag on the item. It's the only non-droppable item by design. Simpler than adding a `Droppable` field to ItemStats.
