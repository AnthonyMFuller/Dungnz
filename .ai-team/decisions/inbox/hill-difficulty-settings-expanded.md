### 2025-01-27: Expanded DifficultySettings for balance overhaul

**By:** Hill

**What:** Added 9 new properties to `DifficultySettings` (`PlayerDamageMultiplier`, `EnemyDamageMultiplier`, `HealingMultiplier`, `MerchantPriceMultiplier`, `XPMultiplier`, `StartingGold`, `StartingPotions`, `ShrineSpawnMultiplier`, `MerchantSpawnMultiplier`) and updated `For()` with explicit balance values for Casual/Normal/Hard. Updated `IntroSequence.BuildPlayer()` to apply starting gold and potions from difficulty settings.

**Why:** Phase 1 of difficulty balance overhaul. Previous `DifficultySettings` had `LootDropMultiplier` and `GoldMultiplier` but no systems were reading them (dead code). Expanding the model first enables Phase 2 work to wire up all multipliers in CombatEngine, LootManager, MerchantManager, etc. Starting conditions (gold/potions) are now difficulty-aware immediately.

**Impact:** Players now receive difficulty-appropriate starting resources (Casual: 50g + 3 potions, Normal: 15g + 1 potion, Hard: 0g + 0 potions). All other new properties are ready for systems to consume in follow-up work.
