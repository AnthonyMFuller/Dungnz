### 2026-03-01: Systems balance analysis
**By:** Barton  
**What:** Detailed quantitative analysis of combat, healing, and merchant balance  
**Why:** Casual difficulty is punishing — need to identify every lever

---

## Current Balance Numbers

### Player Starting Stats
- HP: 100
- Attack: 10
- Defense: 5

### Difficulty Multipliers (from `Models/Difficulty.cs`)
| Difficulty | Enemy Stats | Loot Drop Rate | Gold |
|------------|-------------|----------------|------|
| Casual     | 0.7×        | 1.5×           | 1.5× |
| Normal     | 1.0×        | 1.0×           | 1.0× |
| Hard       | 1.3×        | 0.7×           | 0.7× |

### Floor 1 Enemy Stats (Base → Casual 0.7×)

| Enemy    | HP (Base→Casual) | ATK (Base→Casual) | DEF (Base→Casual) | Gold (Base→Casual) |
|----------|------------------|-------------------|-------------------|---------------------|
| Goblin   | 20 → 14          | 8 → 5             | 2 → 1             | 2-8 → 3-12          |
| Skeleton | 30 → 21          | 12 → 8            | 5 → 3             | 5-15 → 7-22         |
| Troll    | 60 → 42          | 10 → 7            | 8 → 5             | 10-25 → 15-37       |
| Giant Rat| 15 → 10          | 7 → 4             | 1 → 0             | 1-5 → 1-7           |

### Healing Consumables (from `Data/item-stats.json`)

| Item                   | Heal Amount | Merchant Cost | SellPrice |
|------------------------|-------------|---------------|-----------|
| Health Potion          | 20          | 35g           | 5g        |
| Large Health Potion    | 50          | 65g           | 8g        |
| Bandage                | 10          | 25g           | 4g        |
| Antidote               | 8           | 23g           | 5g        |
| Minor Healing Potion   | 15          | 30g           | 4g        |

**Merchant Price Formula** (from `MerchantInventoryConfig.cs`, line 61-69):
```
ItemTier.Common: 15 + healAmount + (attackBonus + defenseBonus) × 5
```

For Health Potion (20 heal): 15 + 20 = **35 gold**

### Loot Drop Mechanics (from `LootTable.cs`)
- **Base drop chance**: 30% per enemy kill
- **LootDropMultiplier applies to**: the 30% chance itself (NOT confirmed in code)
  - **CRITICAL FINDING**: The `LootDropMultiplier` from `DifficultySettings` is NOT applied anywhere in `LootTable.RollDrop()` — it's only referenced in comments but never used in the actual drop calculation
- **Actual Casual benefit**: 1.5× gold only, loot rate unchanged at 30%

---

## Damage vs. Healing Math

### Damage Formula (from `CombatEngine.cs`)
```csharp
playerDamage = Math.Max(1, player.Attack - enemy.Defense);
enemyDamage = Math.Max(1, enemy.Attack - player.Defense);
```

### Typical Floor 1 Combat Sequence (5 Encounters)

Assume: 3 Goblins, 2 Skeletons

**Goblin (Casual):**
- Player deals: max(1, 10 - 1) = 9 damage/turn
- Turns to kill: ⌈14 / 9⌉ = 2 turns
- Enemy deals: max(1, 5 - 5) = 1 damage/turn
- **Total damage taken: 2 HP**

**Skeleton (Casual):**
- Player deals: max(1, 10 - 3) = 7 damage/turn
- Turns to kill: ⌈21 / 7⌉ = 3 turns
- Enemy deals: max(1, 8 - 5) = 3 damage/turn
- **Total damage taken: 9 HP**

**Floor 1 Totals:**
- Damage taken: (3 × 2) + (2 × 9) = **24 HP** (best case, all weak enemies)
- Gold earned: (3 × 7.5) + (2 × 14.5) = **51 gold average**
- Player survives comfortably at 76 HP

**BUT: With ANY Troll encounter:**
- Troll damage: max(1, 7 - 5) = 2 damage/turn
- Turns to kill Troll: ⌈42 / 5⌉ = 9 turns
- **Troll alone deals: 18 HP**

**Mixed Floor (2 Goblins, 1 Skeleton, 2 Trolls):**
- Damage: (2 × 2) + (1 × 9) + (2 × 18) = **49 HP**
- Gold: (2 × 7.5) + (1 × 14.5) + (2 × 26) = **81 gold**
- **HP deficit: 49 damage, can buy 1 Health Potion (35g), still need 29 HP**

### Healing Cost vs. Damage
- **Health Potion efficiency**: 20 HP / 35g = 0.57 HP/gold
- **Large Health Potion efficiency**: 50 HP / 65g = 0.77 HP/gold (better)
- **Problem**: Cannot buy Large Health Potion until ~65g earned (2+ combats), by which time damage accumulation forces purchase

---

## Every Balance Knob (with current state)

| Knob                          | File/Location                     | Current Value            | Difficulty-Aware? | Should Be?               |
|-------------------------------|-----------------------------------|--------------------------|-------------------|--------------------------|
| **Enemy HP multiplier**       | `Difficulty.cs:59`                | 0.7 (Casual)             | ✅ Yes            | ✅ Correct               |
| **Enemy ATK multiplier**      | `Difficulty.cs:59`                | 0.7 (Casual)             | ✅ Yes            | ✅ Correct               |
| **Enemy DEF multiplier**      | `Difficulty.cs:59`                | 0.7 (Casual)             | ✅ Yes            | ✅ Correct               |
| **Gold multiplier**           | `Difficulty.cs:59`                | 1.5 (Casual)             | ✅ Yes            | ⚠️ Needs increase        |
| **Loot drop multiplier**      | `Difficulty.cs:59`                | 1.5 (Casual)             | ❌ **NOT USED**   | ⚠️ Must implement        |
| **Merchant healing prices**   | `MerchantInventoryConfig.cs:61-69`| Tier-based formula       | ❌ No             | ⚠️ Needs difficulty scale|
| **Player starting gold**      | N/A                               | 0                        | ❌ No             | ⚠️ Should grant starting |
| **Healing item drop weight**  | `item-stats.json` (all null)      | 30% base chance          | ❌ No             | ⚠️ Needs weight system   |
| **Merchant guaranteed stock** | `merchant-inventory.json:3-5`     | health-potion (floor 1)  | ❌ No             | ⚠️ Could add bandage     |
| **Player starting HP**        | `PlayerStats.cs:17,24`            | 100                      | ❌ No             | ⚠️ Could scale by diff   |
| **Floor scaling**             | `EnemyFactory.cs:141`             | 1 + (level-1) × 0.12     | ❌ No             | ⚠️ Could adjust by diff  |

### Critical Missing Lever
**`LootDropMultiplier` is defined but never used.** The value exists in `DifficultySettings` but `LootTable.RollDrop()` ignores it entirely. The 30% drop chance is hardcoded at line 184 of `LootTable.cs`.

---

## Recommended Per-Difficulty Values

### Immediate Fixes (Phase 1: Address Casual)

| Lever                      | Casual          | Normal   | Hard     | Implementation                                      |
|----------------------------|-----------------|----------|----------|-----------------------------------------------------|
| **Gold multiplier**        | **2.0× → 2.5×** | 1.0×     | 0.7×     | Change `Difficulty.cs:59` from 1.5 to 2.5           |
| **Loot drop chance**       | **45%**         | 30%      | 20%      | Wire `LootDropMultiplier` into `LootTable.cs:184`   |
| **Merchant healing markup**| **0.7× cost**   | 1.0×     | 1.2×     | Add difficulty param to `ComputePrice()`, apply     |
| **Starting gold (Casual)** | **50g**         | 0g       | 0g       | Grant in `IntroSequence.cs` when Casual selected    |

**Rationale:**
- **Gold 2.5×**: Floor 1 with 2 Trolls yields ~120g instead of 81g → can buy 2 Health Potions (70g) + have buffer
- **Loot 45%**: Increases healing item finds from 30% to 45%, ~1 extra potion per 3 combats
- **Merchant discount 0.7×**: Health Potion becomes 25g instead of 35g → 20 HP for 25g = 0.8 HP/gold (28% better)
- **Starting gold 50g**: Guarantees 1 Health Potion on floor entry OR 2 Bandages, safety net

### Long-Term Scaling (Phase 2: Full Difficulty Curve)

| Lever                    | Casual  | Normal | Hard   | Notes                                                  |
|--------------------------|---------|--------|--------|--------------------------------------------------------|
| **Player starting HP**   | 120     | 100    | 80     | Adjust in `PlayerStats.cs` via difficulty constructor  |
| **Healing drop weight**  | 2.0×    | 1.0×   | 0.5×   | Add `DropWeight` system to `LootTable.cs`, use in roll |
| **Floor enemy scaling**  | 0.10    | 0.12   | 0.15   | Adjust `EnemyFactory.cs:141` formula per difficulty    |
| **Merchant restock rate**| More    | Std    | Less   | Increase guaranteed healing items on Casual floors     |

---

## Root Cause Summary

**Problem:** Casual difficulty receives insufficient gold and healing to offset damage accumulation. The difficulty settings promise loot/gold bonuses, but:

1. **LootDropMultiplier is not implemented** — the field exists but is never referenced in `LootTable.RollDrop()`
2. **Merchant prices are not difficulty-aware** — a Health Potion costs 35g regardless of difficulty
3. **Starting gold is zero** — player has no safety net for early bad RNG (no drops in first 2 combats)
4. **Gold multiplier (1.5×) is too weak** — even with 1.5×, a typical floor yields ~51-81g, barely covering 1-2 Health Potions (35g each) while taking 24-49 damage

**Player experience:** "I need 2 Health Potions (70g) but only earn 51g by the time I'm at 51 HP."

---

## Files Controlling Balance

| System              | Primary File                           | Key Lines         |
|---------------------|----------------------------------------|-------------------|
| Difficulty values   | `Models/Difficulty.cs`                 | 57-62             |
| Enemy base stats    | `Data/enemy-stats.json`                | entire file       |
| Enemy scaling       | `Engine/EnemyFactory.cs`               | 141, 183-191      |
| Combat damage       | `Engine/CombatEngine.cs`               | 747, 1110, 1138   |
| Loot drops          | `Models/LootTable.cs`                  | 149-198           |
| Merchant prices     | `Systems/MerchantInventoryConfig.cs`   | 51-69, 101        |
| Merchant stock      | `Data/merchant-inventory.json`         | floors[].pool     |
| Item stats          | `Data/item-stats.json`                 | Items[].HealAmount|

---

## Next Steps (Implementation Checklist)

### ✅ Must Fix (Blocks Casual playability)
1. Wire `DifficultySettings.LootDropMultiplier` into `LootTable.RollDrop()` line 184
2. Increase `GoldMultiplier` for Casual from 1.5× to 2.5× in `Difficulty.cs`
3. Add difficulty-aware merchant pricing in `MerchantInventoryConfig.ComputePrice()`
4. Grant 50g starting gold when Casual difficulty selected (`IntroSequence.cs`)

### 🔧 Should Fix (Improves feel)
5. Add `DropWeight` system to healing items in `item-stats.json`
6. Increase Casual player starting HP to 120 in `PlayerStats.cs` constructor
7. Add "bandage" to floor 1 guaranteed merchant stock (cheap 10 HP option)

### 📊 Nice to Have (Future balance pass)
8. Implement per-difficulty floor scaling in `EnemyFactory.CreateScaled()`
9. Add healing item weight biasing in `LootTable.RollDrop()` based on `DropWeight`
10. Playtesting: verify Casual completes floor 1 at 60+ HP with current gold
