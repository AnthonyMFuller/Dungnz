# Sell System Test Coverage Gap Analysis

**Auditor:** Romanoff (Tester)  
**Date:** 2026-02-27  
**Scope:** Sell flow test coverage audit following game-breaking sell bug report

---

## Executive Summary

The existing sell system tests (`SellSystemTests.cs`) provide **good coverage of the sell flow mechanics** (happy path, cancellation, equipped items, merchant presence checks) but have **critical gaps in real-world item scenarios**. Specifically:

1. **No tests use items loaded from `item-stats.json`** — all tests use synthetic items created in-memory
2. **No tests verify Floor 1 items specifically** (the reported bug context)
3. **No integration tests combining item data loading + merchant inventory + sell flow**
4. **Missing edge case: items with BOTH explicit SellPrice AND stat bonuses**

---

## Current Test Coverage (SellSystemTests.cs)

### What EXISTS (9 tests total):

#### 1. Formula Tests (5 tests)
- `ComputeSellPrice_CommonItem_ReturnsFortyPercentOfBuyPrice` — Common item, no stats → 40% formula
- `ComputeSellPrice_CommonItemWithStats_ReturnsFortyPercentOfBuyPrice` — Common item with AttackBonus=3, DefenseBonus=2 → 40% of (15 + 5*5) = 16g
- `ComputeSellPrice_UncommonItem_ReturnsFortyPercentOfBuyPrice` — Uncommon item → 40% of buy price
- `ComputeSellPrice_RareItem_ReturnsFortyPercentOfBuyPrice` — Rare item → 40% of buy price
- `ComputeSellPrice_ExplicitSellPrice_UsesExplicitValueIgnoresFormula` — Item with `SellPrice = 75` → returns 75, ignores formula

✅ **Verdict:** Formula logic is well-tested in isolation.

#### 2. Flow Tests (4 tests)
- `Sell_HappyPath_RemovesItemAndIncreasesGold` — Basic sell, item removed, gold awarded
- `Sell_CancelWithN_ItemRemainsInInventoryGoldUnchanged` — Cancel sell, no changes
- `Sell_NoMerchantInRoom_ShowsErrorAndInventoryUnchanged` — No merchant → error message
- `Regression574_Sell_TypedInsideShop_OpensSellerNotExitShop` — SELL command inside shop works correctly

✅ **Verdict:** Happy path and cancellation covered. Regression test for #574 in place.

#### 3. Equipment Tests (2 tests)
- `Sell_EquippedWeaponNotInInventory_OnlyUnequippedItemAppears` — Equipped weapon not in inventory, only unequipped items sellable
- `Sell_OnlyEquippedWeapon_NoInventoryItems_ShowsNoSellNarration` — Empty inventory triggers "NoSell" narration

✅ **Verdict:** Equipment slot exclusion logic covered.

#### 4. Gold-Type Exclusion (1 test)
- `Sell_OnlyGoldTypeItems_ShowsNoSellNarration` — Gold-type items not sellable

✅ **Verdict:** ItemType.Gold exclusion covered.

---

## Coverage GAPS — What's MISSING

### Gap 1: Real Item Data Loading
**Problem:** All tests use synthetic items (`new Item { Name = "...", Tier = Common, AttackBonus = 3 }`).  
**Why it matters:** Real items from `item-stats.json` have:
- Explicit `SellPrice` values (e.g., Iron Sword = 15g, Leather Armor = 10g)
- `Id` fields used by merchant inventory system
- Complex stat combinations (AttackBonus, DefenseBonus, HealAmount, Weight, Slot)

**Missing test:**
```csharp
[Fact]
public void Sell_IronSwordFromItemStats_UsesExplicitSellPrice()
{
    // Load iron-sword from item-stats.json (SellPrice = 15)
    // Sell it
    // Assert player gets exactly 15g (not formula-computed 16g)
}
```

### Gap 2: Floor 1 Merchant Inventory Integration
**Problem:** No tests verify that Floor 1 items (from `merchant-inventory.json`) work correctly with the sell system.  
**Why it matters:** The bug report mentions "Floor 1 item sell failure." We need to confirm:
- Can Floor 1 guaranteed items (health-potion, iron-sword, leather-armor) be sold?
- Do Floor 1 pool items work correctly?

**Missing test:**
```csharp
[Fact]
public void Sell_Floor1GuaranteedItems_AllSellableWithCorrectPrices()
{
    // Load floor 1 guaranteed: health-potion, iron-sword, leather-armor
    // Player has all three in inventory
    // Sell each, verify gold matches item-stats.json SellPrice values
}
```

### Gap 3: Merchant Stock vs Player Inventory Sell
**Problem:** No test verifies selling an item **purchased from a merchant** back to the merchant.  
**Why it matters:** Real gameplay flow:
1. Player buys Iron Sword from Floor 1 merchant (costs 40g per formula: 15 + 5*5)
2. Player later wants to sell it back (should get 15g per explicit SellPrice)
3. System should handle the cloned item correctly (merchant stock items are cloned on purchase)

**Missing test:**
```csharp
[Fact]
public void Sell_ItemBoughtFromMerchant_ClonedItemSellsForCorrectPrice()
{
    // Mock merchant stock with Floor 1 items
    // Player buys Iron Sword (cloned from stock)
    // Player sells the cloned Iron Sword
    // Assert sell price matches original item's SellPrice (15g)
}
```

### Gap 4: Items with Armor Slot Assignment
**Problem:** No test covers selling armor with explicit `Slot` (Chest, Head, Hands, etc.).  
**Why it matters:** 
- Armor slots affect equipping behavior
- Leather Armor (Floor 1) has `Slot: "Chest"`
- `BuildSellableList()` iterates over `_player.AllEquippedArmor` — need to verify multi-slot armor sells correctly

**Missing test:**
```csharp
[Fact]
public void Sell_EquippedChestArmor_UnequipsAndSells()
{
    // Player has Leather Armor equipped in Chest slot
    // Sell it (should be in sellable list with "(equipped)" tag)
    // Assert EquippedChest = null after sell
    // Assert player receives 10g
}
```

### Gap 5: Boundary Case — Item with BOTH Explicit SellPrice AND Zero Stats
**Problem:** Health Potion has `SellPrice = 5` but `AttackBonus = 0, DefenseBonus = 0`.  
**Why it matters:** ComputeSellPrice logic:
```csharp
if (item.SellPrice > 0) return item.SellPrice;  // Takes this branch
return Math.Max(1, ComputePrice(item) * 40 / 100);  // Never executes for health-potion
```
Formula would compute: `(15 + 20 HealAmount + 0) * 0.4 = 14g` but explicit SellPrice = 5g.

**Missing test:**
```csharp
[Fact]
public void Sell_HealthPotionWithLowExplicitSellPrice_IgnoresHigherFormulaPrice()
{
    // Health Potion: HealAmount=20, SellPrice=5 (formula would give 14g)
    // Sell it
    // Assert player gets 5g (explicit), not 14g (formula)
}
```

### Gap 6: Multi-Item Sell Session (Stress Test)
**Problem:** No test sells multiple different items in one merchant visit.  
**Why it matters:** Edge cases in `BuildSellableList()` ordering:
- Unequipped inventory items listed first
- Then equipped items
- Need to verify list indexing stays correct after each sell

**Missing test:**
```csharp
[Fact]
public void Sell_MultipleItemsInOneSession_IndexingRemainsCorrect()
{
    // Player has 3 items: potion (unequipped), sword (unequipped), armor (equipped)
    // Sell potion (index 1) → gold += 5
    // Re-enter sell menu → sword now at index 1, armor at index 2
    // Sell armor (index 2) → gold += 10, EquippedChest = null
}
```

---

## Which Missing Test Would Have Caught the Bug?

**Unknown — need bug details to confirm.** However, based on "Floor 1 item sell failure," the most likely culprit is:

### **Hypothesis:** Item loading or cloning broke SellPrice propagation

**Test that would catch it:**
```csharp
[Fact]
public void Sell_RealFloor1Items_UseSellPriceFromJSON()
{
    var allItems = ItemConfig.Load();  // Load from item-stats.json
    var ironSword = allItems.First(i => i.Id == "iron-sword");
    var leatherArmor = allItems.First(i => i.Id == "leather-armor");
    
    var player = new Player();
    player.Inventory.Add(ironSword);
    player.Inventory.Add(leatherArmor);
    
    // Sell iron-sword
    var price1 = MerchantInventoryConfig.ComputeSellPrice(ironSword);
    price1.Should().Be(15, "iron-sword SellPrice in JSON is 15");
    
    // Sell leather-armor
    var price2 = MerchantInventoryConfig.ComputeSellPrice(leatherArmor);
    price2.Should().Be(10, "leather-armor SellPrice in JSON is 10");
}
```

**Why this catches it:** If `ItemConfig.Load()` or `Item.Clone()` fails to propagate `SellPrice`, this test fails immediately. Current tests never touch the JSON data layer.

---

## Recommended Tests to Add (Priority Order)

### P0 (Critical — should have caught the bug)
1. **`Sell_Floor1ItemsFromJSON_UsesExplicitSellPrices`** — Load iron-sword, leather-armor, health-potion from JSON, verify SellPrice values
2. **`Sell_ItemClonedFromMerchantStock_RetainsSellPrice`** — Buy item from merchant (cloned), sell it back, verify price matches original

### P1 (High — closes obvious gaps)
3. **`Sell_EquippedArmorWithSlot_UnequipsCorrectSlot`** — Sell equipped Chest armor, verify EquippedChest nulled
4. **`Sell_MultipleFloor1Items_GoldAccumulatesCorrectly`** — Sell 3 Floor 1 items in sequence, verify total gold

### P2 (Nice-to-have — stress/edge cases)
5. **`Sell_HealthPotionExplicitPriceLowerThanFormula_PrefersExplicit`** — Verify explicit SellPrice wins over formula
6. **`Sell_MultipleItemsWithReindexing_SelectionRemainsCorrect`** — Sell items, re-enter menu, verify list updates correctly

---

## Data-Driven Test Candidates

Consider parameterized tests for Floor 1 items:

```csharp
[Theory]
[InlineData("health-potion", 5)]
[InlineData("iron-sword", 15)]
[InlineData("leather-armor", 10)]
[InlineData("rusty-sword", 12)]
public void Sell_Floor1Item_MatchesJSONSellPrice(string itemId, int expectedSellPrice)
{
    var allItems = ItemConfig.Load();
    var item = allItems.First(i => i.Id == itemId);
    MerchantInventoryConfig.ComputeSellPrice(item).Should().Be(expectedSellPrice);
}
```

---

## Action Items

**Before implementing new tests:**
1. **Confirm the bug details** — What exactly failed? Item didn't sell? Wrong gold amount? Crash?
2. **Reproduce the bug** — Manual playtest or reproduction script
3. **Write failing test first** — TDD: test should fail before fix, pass after fix
4. **Wait for Barton's fix** — Don't implement tests until fix is confirmed (avoid testing broken behavior)

**After Barton fixes the bug:**
1. Implement P0 tests (Floor 1 JSON loading, cloning)
2. Run full test suite, verify fix didn't break existing tests
3. Add P1 tests for armor slots and multi-item scenarios
4. Consider data-driven test for all Floor 1 pool items

---

## Test Helper Recommendations

**Needed:** `TestItemFactory.LoadRealItem(string itemId)` helper
```csharp
public static class TestItemFactory
{
    private static List<Item>? _cachedItems;
    
    public static Item LoadRealItem(string itemId)
    {
        _cachedItems ??= ItemConfig.Load();
        return _cachedItems.First(i => i.Id == itemId).Clone();
    }
}
```

**Benefit:** Tests can easily load real items without boilerplate:
```csharp
var ironSword = TestItemFactory.LoadRealItem("iron-sword");
player.Inventory.Add(ironSword);
```

---

## Appendix: Floor 1 Items (from JSON)

### Guaranteed Stock
- **Health Potion:** HealAmount=20, SellPrice=5
- **Iron Sword:** AttackBonus=5, SellPrice=15
- **Leather Armor:** DefenseBonus=3, Slot=Chest, SellPrice=10

### Pool Items (commonly found)
- **Rusty Sword:** AttackBonus=3, SellPrice=12
- **Large Health Potion:** HealAmount=50, SellPrice=8
- **Padded Tunic:** DefenseBonus=2, Slot=Chest (SellPrice TBD — check JSON)
- **Iron Helm:** DefenseBonus=2, Slot=Head (SellPrice TBD)

**Note:** Not all pool items may have explicit SellPrice — some may rely on formula. This is a test coverage gap to explore.

---

## Conclusion

The sell system has **solid flow coverage** (happy path, cancellation, equipment handling) but **zero real data coverage**. The missing tests are:

1. Items loaded from JSON
2. Floor 1 merchant stock integration
3. Item cloning (merchant → player → sell back)
4. Armor slot handling
5. Multi-item sell sessions

**Most likely bug cause:** `Item.Clone()` or `ItemConfig.Load()` not propagating `SellPrice` field from JSON to in-memory Item objects, causing formula to compute wrong values or items to fail selling entirely.

**Next step:** Wait for bug details from Anthony or Coulson, then implement P0 tests to validate the fix.
