# Decision: Phase 2 — Difficulty Multipliers Wired Into Game Systems

**Date:** 2026-03-01  
**Author:** Barton (Systems Dev)  
**Status:** ✅ Complete  
**Related Issues:** #673, #675, #676, #677, #678, #680  

## Context

Hill completed Phase 1 by adding all 13 multiplier properties to `DifficultySettings` (EnemyStatMultiplier, LootDropMultiplier, GoldMultiplier, PlayerDamageMultiplier, EnemyDamageMultiplier, HealingMultiplier, MerchantPriceMultiplier, XPMultiplier, StartingGold, StartingPotions, ShrineSpawnMultiplier, MerchantSpawnMultiplier, Permadeath).

Phase 2 required wiring these multipliers into the actual game systems so they affect gameplay.

## Decision

Implemented all multipliers at their point of use across 9 files:

### Combat Damage (Issue #675)
- **CombatEngine.cs**: Added `DifficultySettings? difficulty` parameter to constructor (optional, defaults to Normal)
- Applied `PlayerDamageMultiplier` to player damage before applying it to enemy HP
- Applied `EnemyDamageMultiplier` to enemy damage calculation after base damage computed
- Both use `Math.Max(1, ...)` to ensure minimum 1 damage

### Loot & Gold (Issue #673)
- **CombatEngine.cs**: Applied `GoldMultiplier` to gold drops with `Math.Max(1, ...)` minimum
- **CombatEngine.cs**: Passed `LootDropMultiplier` to `LootTable.RollDrop()`
- **LootTable.cs**: Added `lootDropMultiplier` parameter, applied to base 30% drop chance
  - Special drops (boss/epic/legendary) remain unaffected by multiplier (intentional)

### Healing (Issue #676)
- **CombatEngine.cs**: Applied `HealingMultiplier` to Paladin Divine Favor passive heal
- **GameLoop.cs**: Applied `HealingMultiplier` to consumable item healing
- **GameLoop.cs**: Scaled shrine costs **inversely** by HealingMultiplier (higher multiplier = cheaper shrines)
  - Heal: 30g base → 5-30g scaled
  - Bless: 50g base → 10-50g scaled
  - Fortify/Meditate: 75g base → 15-75g scaled
- **IDisplayService.cs** & **DisplayService.cs**: Updated `ShowShrineMenuAndSelect` to accept cost parameters
- **Test stubs**: Updated FakeDisplayService and TestDisplayService signatures

### XP Scaling (Issue #678)
- **CombatEngine.cs**: Applied `XPMultiplier` to XP gains with `Math.Max(1, ...)` minimum

### Merchant Pricing (Issue #677)
- **MerchantInventoryConfig.cs**: Added `difficulty` parameter, applied `MerchantPriceMultiplier` to all computed prices
- **Merchant.cs**: Added `difficulty` parameter to `CreateRandom`, passed through to config and fallback stock
- **DungeonGenerator.cs**: Passed difficulty to `Merchant.CreateRandom` call

### Spawn Rates (Issue #680)
- **DungeonGenerator.cs**: Applied `MerchantSpawnMultiplier` with 35% cap: `Math.Min(35, (int)(20 * multiplier))`
- **DungeonGenerator.cs**: Applied `ShrineSpawnMultiplier` with 35% cap: `Math.Min(0.35, 0.15 * multiplier)`

### Program.cs Integration
- Already had correct call: `new CombatEngine(..., difficulty: difficultySettings)`

## Implementation Notes

### Design Choices

1. **Optional Parameters**: All new `difficulty` parameters default to `null` with `?? 1.0f` or `DifficultySettings.For(Difficulty.Normal)` fallback for backward compatibility.

2. **Minimum Values**: Used `Math.Max(1, ...)` for damage, gold, XP, and healing to ensure meaningful gameplay even with extreme multipliers.

3. **Inverse Shrine Costs**: Shrine costs scale inversely (`cost / HealingMultiplier`) so that easier difficulties have both better healing *and* cheaper shrines, creating consistent difficulty tuning.

4. **Spawn Rate Caps**: Capped merchant/shrine spawns at 35% to prevent over-saturation even on Casual difficulty (1.40-1.50× multipliers).

5. **Loot Drop Scope**: `LootDropMultiplier` only affects the base 30% chance roll, not boss/epic/legendary special drops. This preserves guaranteed boss loot and rare item rarity.

6. **Display Layer Update**: Shrine menu had hardcoded prices, required interface change to accept dynamic costs.

## Results

- **Build**: ✅ Succeeded (38 warnings, all pre-existing XML doc issues)
- **Tests**: ✅ 1297/1302 pass
  - 2 failures from Hill's Phase 1 value changes (test expectations outdated)
  - 3 failures from pre-existing test infrastructure issues
  - No new failures introduced by Phase 2 changes

## Difficulty Value Reference

| Multiplier            | Casual | Normal | Hard  |
|-----------------------|--------|--------|-------|
| EnemyDamageMultiplier | 0.70   | 1.00   | 1.25  |
| PlayerDamageMultiplier| 1.20   | 1.00   | 0.90  |
| LootDropMultiplier    | 1.60   | 1.00   | 0.65  |
| GoldMultiplier        | 1.80   | 1.00   | 0.60  |
| HealingMultiplier     | 1.50   | 1.00   | 0.75  |
| MerchantPriceMultiplier| 0.65  | 1.00   | 1.40  |
| XPMultiplier          | 1.40   | 1.00   | 0.80  |
| ShrineSpawnMultiplier | 1.50   | 1.00   | 0.70  |
| MerchantSpawnMultiplier| 1.40  | 1.00   | 0.70  |
| StartingGold          | 50     | 15     | 0     |
| StartingPotions       | 3      | 1      | 0     |
| Permadeath            | false  | false  | true  |

## Example Impact

**Casual Mode Player Experience:**
- Deals 20% more damage, takes 30% less damage
- 60% better loot drop chance (48% vs 30%)
- 80% more gold per kill
- Potions heal 50% more (30 HP → 45 HP)
- Shrines cost 35% less (Heal: 20g, Bless: 32g, Fortify: 50g)
- Merchants charge 35% less (Health Potion: 16g vs 25g)
- 40% more XP per kill
- 50% more shrines spawn, 40% more merchants spawn
- Start with 50 gold and 3 potions

**Hard Mode Player Experience:**
- Deals 10% less damage, takes 25% more damage
- 35% worse loot drop chance (19.5% vs 30%)
- 40% less gold per kill
- Potions heal 25% less (15 HP vs 20 HP)
- Shrines cost 33% more (Heal: 40g, Bless: 67g, Fortify: 100g)
- Merchants charge 40% more (Health Potion: 35g vs 25g)
- 20% less XP per kill
- 30% fewer shrines/merchants spawn
- Start with 0 gold, 0 potions, permadeath enabled

## Files Modified

1. Engine/CombatEngine.cs
2. Models/LootTable.cs
3. Engine/GameLoop.cs
4. Display/IDisplayService.cs
5. Display/DisplayService.cs
6. Dungnz.Tests/Helpers/TestDisplayService.cs
7. Dungnz.Tests/Helpers/FakeDisplayService.cs
8. Systems/MerchantInventoryConfig.cs
9. Models/Merchant.cs
10. Engine/DungeonGenerator.cs

## Next Steps

- Hill should update test expectations in `MiscCoverageTests.cs` to match the new multiplier values (1.35 vs 1.3, 0.65/0.60 vs 0.7)
- Consider playtesting each difficulty to validate multiplier values feel appropriately balanced
- Future: consider difficulty-specific enemy AI behaviors (Hard mode enemies use abilities more aggressively)
