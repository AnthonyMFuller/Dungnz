# v3 Planning: Recommended GitHub Issues

**Prepared by:** Coulson (Lead)  
**Date:** 2026-02-20  
**Status:** PROPOSED (awaiting team approval)  
**Total Estimated Hours:** 105 hours across 4 waves  

---

## Summary

v3 requires foundation work before new features. Player.cs (273 LOC) mixes 7 concerns and blocks refactoring. Integration testing gaps prevent safe decomposition. Equipment, inventory, and ability systems need architecture redesign to support classes, shops, and crafting.

Recommended approach: 4 sequential waves (foundation → systems → features → content) with design review ceremonies before each wave. Critical path: Player decomposition → SaveSystem migration → Integration tests.

---

## Issue #1: Player.cs Decomposition (Foundation)

**Title:** Decompose Player.cs into PlayerStats, PlayerInventory, PlayerCombat modules  
**Wave:** Foundation  
**Agent:** Hill (primary), Barton (review)  
**Estimate:** 12 hours  

**Description:**  
Player.cs is 273 LOC mixing combat stats, inventory management, equipment, mana, abilities, and gold. This violates Single Responsibility Principle and blocks v3 features (character classes, shops, crafting) without refactoring.

Split Player.cs into three focused modules:
- **PlayerStats:** HP, MaxHP, Attack, Defense, Level, Gold, XP, Mana, MaxMana — stat reads/modifications only
- **PlayerInventory:** Inventory list, inventory item operations (add/remove/use) — inventory management only
- **PlayerCombat:** Active effects, ability cooldowns, status tracking — combat subsystem only

Preserve all existing functionality. Update SaveSystem serialization. Add integration tests for cross-module interactions (equip weapon → stat change → inventory removal).

**Definition of Done:**
- [ ] Three new modules created; all methods migrated
- [ ] Existing tests pass; no regressions
- [ ] Each module <150 LOC
- [ ] SaveSystem supports new structure
- [ ] Integration tests for stat application, inventory-equipment interaction

**Dependencies:** SaveSystem migration (Issue #5) must precede or run in parallel.

---

## Issue #2: EquipmentManager Creation (Foundation)

**Title:** Extract equipment system into EquipmentManager; create equipment config system  
**Wave:** Foundation  
**Agent:** Barton (primary), Hill (review)  
**Estimate:** 10 hours  

**Description:**  
Equipment logic (EquipItem, UnequipItem, stat application) is embedded in Player.cs. This prevents:
1. Equipment config system (like ItemConfig/EnemyConfig)
2. External stat systems (shops, crafting, class bonuses)
3. Equipment trading and merchant systems

Create EquipmentManager to own equipment state and stat application:
- **EquipmentManager.cs:** Manages 3 equipment slots (weapon, armor, accessory), stat bonuses, swapping logic
- **EquipmentConfig.cs:** Config-driven equipment definitions (attack/defense bonuses, weight, rarity)
- **IEquipmentSystem interface:** Contract for stat application (enables custom stat systems)

Stat application moved from Player to manager; events fire when equipment changes (EquipmentChanged event).

**Definition of Done:**
- [ ] EquipmentManager created; all equipment logic migrated from Player
- [ ] Equipment config system working (5+ test items in config)
- [ ] IEquipmentSystem interface defined and implemented
- [ ] EquipmentChanged event fired on equip/unequip
- [ ] All existing tests pass; new tests for manager
- [ ] Integration tests for equipment→stat changes→combat calculations

**Dependencies:** Depends on Issue #1 (Player decomposition), Issue #4 (integration tests).

---

## Issue #3: InventoryManager Creation & Validation (Foundation)

**Title:** Create InventoryManager with weight/slot validation and centralized item logic  
**Wave:** Foundation  
**Agent:** Barton (primary), Hill (review)  
**Estimate:** 9 hours  

**Description:**  
Inventory is a simple List<Item> in Player with no validation. Item operations (add/remove/use) are scattered across GameLoop, CombatEngine, and LootTable. This prevents:
1. Inventory constraints (weight limits, slot limits)
2. Shop and crafting systems (can't validate sufficient inventory space)
3. Save/load robustness (no validation on load)

Create InventoryManager to centralize inventory operations:
- **InventoryManager.cs:** Owns inventory list, enforces weight/slot limits, provides TakeItem/UseItem/DropItem methods
- **Validation:** Each item has Weight; inventory has MaxWeight; TakeItem validates before adding
- **Centralized Logic:** All item operations routed through manager (prevents duping bugs)
- **Events:** ItemAdded, ItemRemoved, ItemUsed events fire for UI/achievement integration

**Definition of Done:**
- [ ] InventoryManager created with weight/slot validation
- [ ] All TakeItem/UseItem calls routed through manager
- [ ] Item weight property added to Item model
- [ ] Validation tests cover edge cases (full inventory, weight limits, invalid items)
- [ ] All existing tests pass; no regressions
- [ ] Integration tests for inventory-shop and inventory-crafting scenarios

**Dependencies:** Depends on Issue #1 (Player decomposition).

---

## Issue #4: Integration Test Suite for Multi-System Flows (Foundation)

**Title:** Create comprehensive integration tests for system interactions (combat→loot, equipment→stats, ability→cooldown)  
**Wave:** Foundation  
**Agent:** Romanoff (primary), Hill & Barton (review)  
**Estimate:** 14 hours  

**Description:**  
Current test coverage is 91.86% unit tests, but zero integration tests for multi-system flows. Refactoring Player.cs and equipment system risks regressions in CombatEngine, LootTable, and SaveSystem interactions.

Create integration test suite covering:
1. **Combat→Loot→Equipment:** Enemy dies → loot drops → equipment added to inventory → player equips → stats applied
2. **Equipment→Combat Stat Changes:** Equip weapon → attack bonus applied → combat calculation uses new attack value
3. **Status→Save/Load:** Apply status effect → save game → load → status effect persists with correct duration
4. **Ability→Cooldown:** Use ability → cooldown starts → level up → cooldown resets → ability usable again
5. **Inventory→Shop:** Buy item → added to inventory → equip → stat change → sell equipped item → item removed and stats reverted
6. **Boss Phase 2:** Boss HP low → phase 2 triggered → different attack pattern → player takes bonus damage

Each test uses ControlledRandom for deterministic runs; tests all edge cases (stun prevents ability use, flee while poisoned, etc.).

**Definition of Done:**
- [ ] 10+ integration test cases written
- [ ] All multi-system flows tested (combat, equipment, inventory, status, abilities, save/load)
- [ ] Edge cases covered (stun+action, status+save, ability+cooldown+levelup)
- [ ] All tests passing; baseline established for refactoring work
- [ ] Integration tests run as part of CI gate

**Dependencies:** Can run in parallel with other foundation issues; must complete before Wave 1 merge.

---

## Issue #5: SaveSystem Migration & Version Tracking (Foundation)

**Title:** Add SaveFormatVersion to SaveSystem; implement v2→v3 migration for Player decomposition  
**Wave:** Foundation  
**Agent:** Hill (primary), Barton (review)  
**Estimate:** 10 hours  

**Description:**  
SaveSystem couples directly to current Player structure. When Player.cs is decomposed (Issue #1), all existing saves become incompatible. Add version tracking and migration logic to protect user data.

Create SaveFormatVersion system:
- **SaveFormatVersion:** Track save file format (v2=original, v3=decomposed Player)
- **SaveSystem.Load(path):** Detect version; apply migrations if needed
- **Migrations:** v2→v3 reads old Player fields, creates new PlayerStats/PlayerInventory/PlayerCombat objects
- **Backward Compatibility:** Support loading v2 saves for 2 releases; warn users to upgrade

Changes:
1. Add `int SaveFormatVersion = 3` to SaveData class
2. Create SaveMigration.cs with v2→v3 logic
3. Update SaveSystem.Load() to detect version and migrate
4. Update SaveSystem.Save() to write current version
5. Add migration tests (load v2 save, verify data integrity in v3 format)

**Definition of Done:**
- [ ] SaveFormatVersion added to SaveData
- [ ] SaveMigration.cs created with v2→v3 converter
- [ ] SaveSystem.Load() detects and migrates versions
- [ ] All existing v2 saves load correctly in v3 format
- [ ] No data loss in migration
- [ ] Migration tests covering all Player fields
- [ ] Backward compatibility verified

**Dependencies:** Must complete before or in parallel with Issue #1 (Player decomposition).

---

## Issue #6: Character Class System (Core)

**Title:** Design and implement character classes with config-driven class definitions  
**Wave:** Core (Wave 2)  
**Agent:** Hill (primary), Barton (review)  
**Estimate:** 14 hours  

**Description:**  
v2 shipped with single-player archetype (generic melee/mage). v3 adds character classes to provide distinct playstyles, balance, and replayability.

Implement character class system:
- **ClassDefinition:** Config-driven class definition (name, starting stats, base abilities, equipment templates)
- **ClassManager.cs:** Manages class selection, stat templates, ability grants on class selection
- **Class Selection:** Player chooses class at game start (Warrior, Mage, Rogue, Paladin, Ranger)
- **Class-Specific Abilities:** Each class has 3 unique abilities granted on selection
- **Stat Templates:** Each class has starting stat templates (Warrior: +attack, -mana; Mage: +mana, -defense)

Create classes/config/classes.json with 5 classes; each class playable from start to end of dungeons. Add class-based achievements (e.g., "Beat final boss as Mage").

**Definition of Done:**
- [ ] ClassDefinition and ClassManager created
- [ ] 5 classes defined in config (Warrior, Mage, Rogue, Paladin, Ranger)
- [ ] Class selection integrated into game start
- [ ] Each class playable and balanced through dungeons
- [ ] Class-specific abilities working
- [ ] Tests for class stat application, ability grants
- [ ] Integration tests for class→combat, class→abilities

**Dependencies:** Requires Issue #1 (Player decomposition) to be complete.

---

## Issue #7: Shop System & NPC Merchants (Core)

**Title:** Implement shop system with NPC merchants, shop inventory config, and buy/sell mechanics  
**Wave:** Core (Wave 3)  
**Agent:** Hill (architect), Barton (implementation)  
**Estimate:** 16 hours  

**Description:**  
Add economic system with merchant NPCs in dungeons. Players can buy items, sell loot, and upgrade equipment without grinding combat.

Implement shop system:
- **ShopConfig.cs:** Config-driven shop definitions (merchant name, inventory list, prices, stock limits)
- **ShopManager.cs:** Manages shop inventory, stock rotation, price adjustments
- **Shop Interaction:** "SHOP" command enters shop; "BUY item", "SELL item", "INVENTORY" commands
- **Economy:** Items have buy/sell prices (sell price = 60% buy price); shop stock limited (5 items max)
- **NPC Integration:** 3+ merchants in different dungeon levels (early-game armor shop, late-game weapon shop, universal potion shop)

Shop prices balanced to prevent exploitation (crafting items worth more than buying, boss loot valuable).

**Definition of Done:**
- [ ] ShopManager and ShopConfig created
- [ ] 3+ merchants with unique inventories deployed in dungeons
- [ ] Buy/Sell commands working
- [ ] Prices balanced (no exploitation paths)
- [ ] Stock rotation working
- [ ] Tests for shop logic, price calculations, stock limits
- [ ] Integration tests for buy→inventory, sell→gold, equip from shop

**Dependencies:** Requires Issue #1 (Player decomposition), Issue #3 (InventoryManager), Issue #2 (EquipmentManager).

---

## Issue #8: Crafting System & Recipe Engine (Core)

**Title:** Implement crafting system with recipe definitions and ingredient validation  
**Wave:** Core (Wave 3)  
**Agent:** Barton (primary), Hill (review)  
**Estimate:** 12 hours  

**Description:**  
Add crafting system allowing players to combine items into new items. Provides alternative progression path and item acquisition.

Implement crafting system:
- **RecipeDefinition:** Config-driven recipe (name, ingredients list, output item, crafting time/cost)
- **CraftingManager.cs:** Validates ingredients, removes from inventory, produces output
- **Recipe Config:** Define 5+ recipes (e.g., "Sharp Blade" from "Iron Ore + Sharp Stone", "Healing Potion" from "Herb + Water")
- **Crafting Command:** "CRAFT recipe_name" validates ingredients, crafts item, fires ItemCrafted event
- **Ingredient Validation:** CraftingManager uses InventoryManager to check inventory before crafting

Recipes valuable but not essential (can progress without crafting; crafting is convenience/shortcut).

**Definition of Done:**
- [ ] CraftingManager and RecipeDefinition created
- [ ] 5+ recipes defined in config
- [ ] Recipe lookup and validation working
- [ ] Ingredients consumed and output produced correctly
- [ ] Tests for recipe validation, ingredient checking, item creation
- [ ] Integration tests for crafting→inventory, crafting→equipment, crafting→shop price differences

**Dependencies:** Requires Issue #1 (Player decomposition), Issue #3 (InventoryManager).

---

## Summary Table

| # | Title | Wave | Agent | Hours | Dependencies |
|---|-------|------|-------|-------|--------------|
| 1 | Player.cs Decomposition | Foundation | Hill | 12 | #5 (parallel) |
| 2 | EquipmentManager Creation | Foundation | Barton | 10 | #1, #4 |
| 3 | InventoryManager & Validation | Foundation | Barton | 9 | #1 |
| 4 | Integration Test Suite | Foundation | Romanoff | 14 | Parallel |
| 5 | SaveSystem Migration | Foundation | Hill | 10 | #1 (parallel) |
| 6 | Character Class System | Core (Wave 2) | Hill | 14 | #1, #5 |
| 7 | Shop System | Core (Wave 3) | Hill/Barton | 16 | #1, #2, #3 |
| 8 | Crafting System | Core (Wave 3) | Barton | 12 | #1, #3 |

**Total:** 105 hours across 4 waves (Foundation, Core x2 waves, Content)

---

## Wave Breakdown & Timeline

### Wave 1 (Foundation) — 2-3 weeks
Issues: #1, #2, #3, #4, #5  
Goal: Refactor Player.cs, create managers, establish integration testing  
Team: Hill (12h), Barton (19h), Romanoff (14h) = 45 hours  

### Wave 2 (Systems) — 2 weeks
Issue: #6  
Goal: Add character classes, expand abilities  
Team: Hill (14h + expansion work), Barton (expansion work), Romanoff (testing) = ~20 hours  

### Wave 3 (Features) — 2-3 weeks
Issues: #7, #8  
Goal: Implement shops and crafting  
Team: Hill (16h), Barton (12h + content), Romanoff (testing) = 28 hours  

### Wave 4 (Content) — 1-2 weeks
New enemy types, shrines, difficulty tuning (parallel with Wave 3 final week)  
Team: Barton (content), Romanoff (balancing) = 12 hours  

**Grand Total Timeline:** ~12-14 weeks for full v3 roadmap

---

## Risks & Mitigations

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Player.cs refactoring breaks saves | HIGH | SaveSystem migration (Issue #5) precedes decomposition; version tracking |
| Multi-system integration bugs | HIGH | Integration test suite (Issue #4) before decomposition work; feature flags |
| Scope creep (classes + shops + crafting) | MEDIUM | Sequential wave structure; design reviews before each wave |
| Equipment manager stat bugs | MEDIUM | IEquipmentSystem interface; extensive integration tests |
| Inventory weight system exploits | LOW | Validation tests; price balancing prevents gold exploits |

---

## Next Steps

1. **Team Review:** Present roadmap to Hill, Barton, Romanoff; request feedback on estimates and ordering
2. **Design Review Ceremony:** Schedule for Wave 1 (Player decomposition contracts, EquipmentManager interface, InventoryManager validation rules)
3. **Issue Creation:** Create GitHub issues from this doc (add acceptance criteria, link to ceremony notes)
4. **Work Allocation:** Assign agents; kickoff Wave 1 with architecture ceremony
5. **Retrospectives:** Monthly check-ins to validate wave progress; adjust Wave 4 scope if needed

**Lead:** Coulson (oversight, architecture approval, integration sign-off)
