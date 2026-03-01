### 2026-03-01: Difficulty balance system design
**By:** Coulson
**What:** Full analysis and design plan for overhauling difficulty scaling
**Why:** Boss reported Casual difficulty is too punishing — healing is scarce, damage is too high. Difficulty levels are not meaningfully differentiated.

---

## Analysis

### Current State of DifficultySettings

The `DifficultySettings` class (Models/Difficulty.cs) defines three knobs per difficulty:

| Setting              | Casual | Normal | Hard |
|----------------------|--------|--------|------|
| EnemyStatMultiplier  | 0.70   | 1.00   | 1.30 |
| LootDropMultiplier   | 1.50   | 1.00   | 0.70 |
| GoldMultiplier       | 1.50   | 1.00   | 0.70 |
| Permadeath           | false  | false  | false|

### Critical Problem: 2 of 3 multipliers are DEAD CODE

**`LootDropMultiplier` is never read by any system.** Not by LootTable.RollDrop, not by DungeonGenerator, not by CombatEngine. The 1.5× loot bonus for Casual simply does not exist.

**`GoldMultiplier` is never read by any system.** Gold drops in CombatEngine.HandleLootAndXP are passed straight through: `player.AddGold(loot.Gold)` — no multiplier applied. The 1.5× gold bonus for Casual does not exist.

Only `EnemyStatMultiplier` is actually wired up — in DungeonGenerator.Generate() at line 69, where it multiplies the floor multiplier before passing to EnemyFactory.CreateScaled. This means:

- On Casual, enemies have 70% stats (HP, ATK, DEF, XP, Gold all ×0.7)
- But the player gets NO extra loot and NO extra gold
- Merchant prices are completely difficulty-agnostic
- Healing item availability is completely difficulty-agnostic

### Why Casual Feels Punishing

1. **Healing economy is broken.** The only healing sources are:
   - Health potions from loot drops (30% chance from enemies, 30% room items — random tier, not guaranteed healing)
   - Shrines (15% room spawn rate, cost 30g to heal)
   - Merchants (20% room spawn rate, potions cost 15-35g)
   - The player starts with **0 gold** regardless of difficulty

2. **Damage intake vs heal availability.** Even with 0.7× enemy stats, a Goblin (ATK 8 → 5.6) vs starting player (DEF 5) deals ~1 dmg/hit. But a Skeleton (ATK 12 → 8.4) or DarkKnight (ATK 18 → 12.6) still deals 3-8 damage per hit against a 100HP player with 5 DEF. Over multiple fights before finding any healing, the player bleeds out.

3. **Gold starvation.** Enemies drop 2-25g base (×0.7 = 1-17g). A Health Potion from a merchant costs ~15g. The shrine full-heal costs 30g. A player might fight 3-4 enemies earning 10-40g total before finding a merchant or shrine, but will have taken 20-40+ HP in damage by then.

4. **No difficulty-scaled starting conditions.** All players start with: 100 HP, 10 ATK, 5 DEF, 0 gold, 0 items. Casual gives no head start.

5. **XP gain is reduced on Casual** because enemy XP values are ×0.7 (via EnemyStatMultiplier affecting all stats). This makes leveling slower, compounding the problem — slower levels mean fewer HP/ATK/DEF gains.

### Files That Control Balance

| File | What It Controls |
|------|-----------------|
| `Models/Difficulty.cs` | DifficultySettings class with multipliers |
| `Engine/DungeonGenerator.cs` | Enemy placement (60%), shrine/merchant spawn rates, room items |
| `Engine/EnemyFactory.cs` | CreateScaled() applies stat scalar to enemies |
| `Engine/CombatEngine.cs` | Damage formulas, loot/XP distribution (HandleLootAndXP) |
| `Engine/IntroSequence.cs` | Player starting stats (BuildPlayer), no difficulty consideration |
| `Models/LootTable.cs` | RollDrop() — 30% item drop chance, no difficulty input |
| `Systems/MerchantInventoryConfig.cs` | ComputePrice() — tier-based, no difficulty input |
| `Models/Merchant.cs` | CreateRandom() — no difficulty parameter |
| `Engine/GameLoop.cs` | Shrine costs (hardcoded 30/50/75g), shop/sell flow |
| `Data/enemy-stats.json` | Base enemy stats |
| `Data/item-stats.json` | Item definitions including heal amounts |
| `Data/merchant-inventory.json` | Per-floor merchant stock pools |

---

## Design

### Principle: Difficulty Should Touch Everything

The player's chosen difficulty should produce a DRAMATICALLY different experience across all game systems. Casual should feel forgiving and generous. Hard should feel punishing and scarce.

### Expanded DifficultySettings Model

Add new multipliers and values to `DifficultySettings`:

```
DifficultySettings
├── EnemyStatMultiplier     (existing — enemy HP/ATK/DEF/XP/Gold scaling)
├── LootDropMultiplier      (existing — WIRE IT UP — scales the 30% base drop chance)
├── GoldMultiplier          (existing — WIRE IT UP — scales gold from all sources)
├── Permadeath              (existing)
├── NEW: PlayerDamageMultiplier    (scales player outgoing damage: >1.0 = player hits harder)
├── NEW: EnemyDamageMultiplier     (scales enemy outgoing damage: <1.0 = enemies hit softer)
├── NEW: HealingMultiplier         (scales all healing: potions, shrines, regen)
├── NEW: MerchantPriceMultiplier   (scales merchant buy prices: <1.0 = cheaper shops)
├── NEW: XPMultiplier              (scales XP gains independently of enemy stats)
├── NEW: StartingGold              (gold the player begins with)
├── NEW: StartingPotions           (number of free Health Potions at game start)
├── NEW: ShrineSpawnMultiplier     (scales the 15% shrine spawn rate)
├── NEW: MerchantSpawnMultiplier   (scales the 20% merchant spawn rate)
```

### Proposed Values

| Setting                   | Casual | Normal | Hard   |
|---------------------------|--------|--------|--------|
| EnemyStatMultiplier       | 0.65   | 1.00   | 1.35   |
| EnemyDamageMultiplier     | 0.70   | 1.00   | 1.25   |
| PlayerDamageMultiplier    | 1.20   | 1.00   | 0.90   |
| LootDropMultiplier        | 1.60   | 1.00   | 0.65   |
| GoldMultiplier            | 1.80   | 1.00   | 0.60   |
| HealingMultiplier         | 1.50   | 1.00   | 0.75   |
| MerchantPriceMultiplier   | 0.65   | 1.00   | 1.40   |
| XPMultiplier              | 1.40   | 1.00   | 0.80   |
| StartingGold              | 50     | 15     | 0      |
| StartingPotions           | 3      | 1      | 0      |
| ShrineSpawnMultiplier     | 1.50   | 1.00   | 0.70   |
| MerchantSpawnMultiplier   | 1.40   | 1.00   | 0.70   |
| Permadeath                | false  | false  | true   |

### Where Each Knob Gets Wired

1. **EnemyStatMultiplier** — already wired in DungeonGenerator (line 69). Adjust value from 0.7→0.65 for Casual.

2. **EnemyDamageMultiplier** — apply in CombatEngine enemy attack phase after base damage calc (`enemyDmg = (int)(enemyDmg * settings.EnemyDamageMultiplier)`). Requires CombatEngine to receive DifficultySettings.

3. **PlayerDamageMultiplier** — apply in CombatEngine player attack phase after base damage calc (`playerDmg = (int)(playerDmg * settings.PlayerDamageMultiplier)`).

4. **LootDropMultiplier** — pass to LootTable.RollDrop or apply the multiplier to the 0.30 base drop chance in the combat engine's loot flow. E.g., `if (_rng.NextDouble() < 0.30 * lootMultiplier)`.

5. **GoldMultiplier** — apply in CombatEngine.HandleLootAndXP: `gold = (int)(loot.Gold * settings.GoldMultiplier)`.

6. **HealingMultiplier** — apply wherever Heal() is called with a potion/item: in GameLoop.UseItem and CombatEngine item usage. `healAmount = (int)(item.HealAmount * settings.HealingMultiplier)`.

7. **MerchantPriceMultiplier** — apply in MerchantInventoryConfig.GetStockForFloor or at Merchant.CreateRandom time: `price = (int)(ComputePrice(item) * settings.MerchantPriceMultiplier)`.

8. **XPMultiplier** — apply in CombatEngine.HandleLootAndXP: `xp = (int)(enemy.XPValue * settings.XPMultiplier)`.

9. **StartingGold** — apply in IntroSequence.BuildPlayer: `player.Gold = settings.StartingGold`.

10. **StartingPotions** — apply in IntroSequence.BuildPlayer: add N Health Potions to player.Inventory.

11. **ShrineSpawnMultiplier** — apply in DungeonGenerator: `if (_rng.NextDouble() < 0.15 * settings.ShrineSpawnMultiplier)`.

12. **MerchantSpawnMultiplier** — apply in DungeonGenerator: `if (_rng.Next(100) < 20 * settings.MerchantSpawnMultiplier)`.

### Architecture Approach

- `DifficultySettings` remains a C#-defined class (not JSON) since values are tied to game logic
- Pass `DifficultySettings` through to CombatEngine constructor (it currently doesn't receive it)
- Pass `DifficultySettings` through to Merchant.CreateRandom and MerchantInventoryConfig
- IntroSequence.BuildPlayer receives DifficultySettings to set starting conditions
- All multipliers use simple float multiplication — no complex formulas

---

## GitHub Issues to Create

### Issue 1: Wire up LootDropMultiplier and GoldMultiplier (dead code fix)
**Assignee:** barton
**Labels:** bug, balance

**Body:**
`DifficultySettings.LootDropMultiplier` and `DifficultySettings.GoldMultiplier` are defined but never consumed by any game system. This means Casual's advertised 1.5× loot/gold bonuses and Hard's 0.7× penalties do not function.

**Acceptance Criteria:**
- LootTable.RollDrop() (or the calling code in CombatEngine.HandleLootAndXP) uses `LootDropMultiplier` to scale the base 30% item drop chance
- Gold awarded in CombatEngine.HandleLootAndXP is multiplied by `GoldMultiplier`
- DifficultySettings must be accessible in CombatEngine (add constructor parameter or pass through)
- Existing tests updated to reflect wired-up multipliers

**Files:** Engine/CombatEngine.cs, Models/LootTable.cs, Models/Difficulty.cs

---

### Issue 2: Expand DifficultySettings with new balance knobs
**Assignee:** hill
**Labels:** enhancement, balance

**Body:**
Add new properties to `DifficultySettings` to support comprehensive difficulty scaling. Currently only `EnemyStatMultiplier` meaningfully differentiates difficulties.

New properties needed:
- `float PlayerDamageMultiplier` — scales player outgoing damage
- `float EnemyDamageMultiplier` — scales enemy incoming damage to player
- `float HealingMultiplier` — scales all healing received
- `float MerchantPriceMultiplier` — scales merchant buy prices
- `float XPMultiplier` — scales XP gains
- `int StartingGold` — gold at game start
- `int StartingPotions` — free health potions at start
- `float ShrineSpawnMultiplier` — scales shrine spawn rate
- `float MerchantSpawnMultiplier` — scales merchant spawn rate

Update `DifficultySettings.For()` with values per the balance table in the design doc.

**Acceptance Criteria:**
- All new properties defined with `{ get; init; }` pattern
- `For()` returns correct values for Casual, Normal, Hard
- XML documentation on all new properties
- Existing tests still pass; new properties testable

**Files:** Models/Difficulty.cs

---

### Issue 3: Apply EnemyDamageMultiplier and PlayerDamageMultiplier in CombatEngine
**Assignee:** barton
**Labels:** enhancement, balance
**Depends on:** Issue 2

**Body:**
CombatEngine must accept `DifficultySettings` and apply damage multipliers during combat.

- After computing `playerDmg` (line ~756), apply `PlayerDamageMultiplier`
- After computing `enemyDmg` (line ~1138), apply `EnemyDamageMultiplier`
- CombatEngine constructor or StartCombat needs DifficultySettings parameter
- Program.cs must pass settings through

**Acceptance Criteria:**
- Player damage scaled by `PlayerDamageMultiplier` before applying to enemy HP
- Enemy damage scaled by `EnemyDamageMultiplier` before applying to player HP
- Min damage of 1 preserved after scaling
- No changes to boss-specific mechanics (they stack on top)

**Files:** Engine/CombatEngine.cs, Program.cs

---

### Issue 4: Apply HealingMultiplier to all healing sources
**Assignee:** barton
**Labels:** enhancement, balance
**Depends on:** Issue 2

**Body:**
All healing received by the player should be scaled by `DifficultySettings.HealingMultiplier`.

Healing sources to cover:
- Consumable item usage in GameLoop (line ~575: `player.Heal(item.HealAmount)`)
- Combat-time item usage (if any)
- Shrine full heal (GameLoop line ~820) — this heals to max so no multiplier needed, but the gold cost should be adjusted
- Paladin Divine Heal passive (CombatEngine line ~1252)
- Sacred Ground auto-heal (already heals to full — no change needed)

**Acceptance Criteria:**
- `HealAmount` from consumables multiplied by `HealingMultiplier` before calling `player.Heal()`
- On Casual (1.5×), a 20HP potion heals for 30HP
- On Hard (0.75×), a 20HP potion heals for 15HP
- Shrine percentage-based heals (full heal) are unaffected
- Shrine gold costs scale inversely: Casual costs less, Hard costs more

**Files:** Engine/GameLoop.cs, Engine/CombatEngine.cs

---

### Issue 5: Apply MerchantPriceMultiplier to merchant pricing
**Assignee:** barton
**Labels:** enhancement, balance
**Depends on:** Issue 2

**Body:**
Merchant buy prices must scale with difficulty. On Casual, items should be cheaper; on Hard, more expensive.

- `MerchantInventoryConfig.GetStockForFloor()` or `Merchant.CreateRandom()` must receive `DifficultySettings`
- Apply `MerchantPriceMultiplier` to the computed price for each `MerchantItem`
- Sell prices should NOT be affected (selling is already a player advantage)

**Acceptance Criteria:**
- Merchant prices multiplied by `MerchantPriceMultiplier` (Casual 0.65×, Hard 1.40×)
- A 15g potion costs ~10g on Casual, ~21g on Hard
- Sell prices unchanged
- DifficultySettings flows through Program.cs → DungeonGenerator → Merchant.CreateRandom

**Files:** Systems/MerchantInventoryConfig.cs, Models/Merchant.cs, Engine/DungeonGenerator.cs, Program.cs

---

### Issue 6: Apply XPMultiplier to experience gains
**Assignee:** barton
**Labels:** enhancement, balance
**Depends on:** Issue 2

**Body:**
XP awarded after combat should be scaled by `DifficultySettings.XPMultiplier`, independently of enemy stat scaling.

Currently, enemy XPValue is reduced by `EnemyStatMultiplier` (since CreateScaled scales all stats). This double-penalizes Casual (weaker enemies = less XP). The XPMultiplier compensates.

- In CombatEngine.HandleLootAndXP, compute: `xp = (int)(enemy.XPValue * settings.XPMultiplier)`
- Use the scaled value for `player.AddXP()` and the display message

**Acceptance Criteria:**
- XP gains multiplied by `XPMultiplier` (Casual 1.40×, Hard 0.80×)
- Display message shows actual XP gained (post-multiplier)
- Level-up thresholds unchanged (still 100 × level)

**Files:** Engine/CombatEngine.cs

---

### Issue 7: Difficulty-scaled starting conditions (gold, potions)
**Assignee:** hill
**Labels:** enhancement, balance
**Depends on:** Issue 2

**Body:**
Player starting conditions should vary by difficulty. Currently all players start with 0 gold and 0 items regardless of difficulty choice.

In `IntroSequence.BuildPlayer()`:
- Set `player.Gold = settings.StartingGold` (Casual: 50, Normal: 15, Hard: 0)
- Add `settings.StartingPotions` Health Potions to `player.Inventory` (Casual: 3, Normal: 1, Hard: 0)
- IntroSequence.Run() needs to receive DifficultySettings (or create it from the chosen difficulty)

**Acceptance Criteria:**
- Casual players start with 50g and 3 Health Potions
- Normal players start with 15g and 1 Health Potion
- Hard players start with 0g and 0 items
- Health Potions are the standard item from item-stats.json (id: "health-potion")

**Files:** Engine/IntroSequence.cs, Program.cs

---

### Issue 8: Difficulty-scaled shrine and merchant spawn rates
**Assignee:** barton
**Labels:** enhancement, balance
**Depends on:** Issue 2

**Body:**
Shrine and merchant room spawn rates in `DungeonGenerator` should scale with difficulty so Casual players find more healing and shopping opportunities.

- Shrine spawn: `_rng.NextDouble() < 0.15 * difficulty.ShrineSpawnMultiplier`
- Merchant spawn: `_rng.Next(100) < (int)(20 * difficulty.MerchantSpawnMultiplier)`
- DungeonGenerator.Generate() already receives `DifficultySettings? difficulty`

**Acceptance Criteria:**
- Casual: ~22.5% shrine spawn, ~28% merchant spawn
- Normal: 15% shrine, 20% merchant (unchanged)
- Hard: ~10.5% shrine, ~14% merchant
- Capped at reasonable maximums (shrine ≤ 30%, merchant ≤ 35%)

**Files:** Engine/DungeonGenerator.cs

---

### Issue 9: Regression tests for difficulty balance system
**Assignee:** romanoff
**Labels:** testing, balance
**Depends on:** Issues 1-8

**Body:**
Add test coverage for all new difficulty scaling behaviors. Tests should be deterministic using fixed-seed RNG where applicable.

Test cases needed:
1. `DifficultySettings.For()` returns correct values for all new properties across Casual/Normal/Hard
2. CombatEngine applies `PlayerDamageMultiplier` and `EnemyDamageMultiplier` correctly
3. CombatEngine applies `GoldMultiplier` to gold drops
4. CombatEngine applies `XPMultiplier` to XP gains
5. LootTable drop chance is scaled by `LootDropMultiplier`
6. MerchantInventoryConfig prices are scaled by `MerchantPriceMultiplier`
7. IntroSequence produces correct starting gold/potions per difficulty
8. DungeonGenerator shrine/merchant spawn rates respect multipliers
9. HealingMultiplier scales consumable heal amounts
10. Sell prices are NOT affected by difficulty

**Files:** Dungnz.Tests/ (new test file: DifficultyBalanceTests.cs)

---

### Issue 10: Update difficulty selection screen to show new balance details
**Assignee:** fury
**Labels:** enhancement, balance, UX
**Depends on:** Issue 2

**Body:**
The difficulty selection screen should communicate the actual gameplay impact of each choice so players can make an informed decision.

Update the difficulty descriptions to include key info:
- Casual: "Relaxed adventure — enemies weaker, better loot, cheaper shops, starting supplies"
- Normal: "The intended experience — balanced challenge"
- Hard: "Punishing — stronger enemies, scarce resources, permadeath"

Show a brief stat summary: enemy strength %, loot bonus %, gold bonus %, starting supplies.

**Files:** Display/ (ConsoleDisplayService difficulty menu), Models/Difficulty.cs (if description helper needed)
