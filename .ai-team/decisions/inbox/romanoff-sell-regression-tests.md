### 2026-02-27: Sell regression tests added
**By:** Romanoff
**What:** Added 5 new regression tests to SellSystemTests.cs
**Why:** Guard against regressions in sell-to-merchant fix (issue #577)

#### Tests added:
1. `Sell_EquippedChestArmor_ShowsSellMenuWithArmorSlotItem` — verifies `BuildSellableList` includes armor-slot equipped items (not just EquippedWeapon)
2. `Sell_MultipleInventoryItems_SellMenuShowsAllItems` — verifies sell menu appears with 3 Uncommon inventory items
3. `Sell_EquippedWeaponAndInventoryItems_BothAppearInSellMenu` — verifies sell menu appears when player has both inventory items and an equipped weapon
4. `Sell_SellFirstInventoryItem_ItemRemovedFromInventory` — verifies selling item 1 removes it from inventory and awards correct gold
5. `Sell_EquippedItem_SellConfirmed_ItemUnequipped` — verifies selling an equipped weapon clears the equipment slot and awards gold

All 5 tests implemented cleanly using existing infrastructure. Tests 4 and 5 were feasible because `HandleSell` follows the same "index → Y/N confirm" pattern as the existing happy-path tests.

**Result:** All 17 SellSystem tests pass (12 pre-existing + 5 new).
