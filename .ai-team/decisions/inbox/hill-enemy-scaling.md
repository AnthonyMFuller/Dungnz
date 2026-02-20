### 2026-02-20: Enemy Level Scaling Formula
**By:** Hill
**What:** Implemented enemy scaling based on player level using formula `scalar = 1.0f + (playerLevel - 1) * 0.12f`
**Why:** Keeps combat challenging as player progresses. 12% per level provides meaningful difficulty increase without exponential runaway. At level 5, enemies are 48% stronger; at level 10, they're 108% stronger. Formula applies to HP, Attack, Defense, XP rewards, and Gold rewards.

**Implementation:**
- `EnemyFactory.CreateScaled(string enemyType, int playerLevel)` — new method alongside existing Create methods
- `DungeonGenerator.Generate(...)` — added optional `playerLevel` parameter (defaults to 1)
- Reads base stats from enemy-stats.json config, applies scalar, rounds to int
- Backward compatible: existing CreateRandom() and CreateBoss() unchanged

**Testing:** 12 new tests verify scaling at levels 1, 5, 10 for all enemy types.
