# Root Cause Analysis: Sell Command Failure

**Investigator:** Coulson (Lead)  
**Date:** 2026-02-27  
**Status:** ✅ CRITICAL BUG IDENTIFIED — COMPILATION ERROR

---

## Executive Summary

The SELL command cannot work because **the codebase does not compile**. Line 1308 in `Engine/GameLoop.cs` calls `_player.RecalculateDerivedStats()`, but this method **does not exist** in the `Player` class.

---

## Root Cause

**File:** `Engine/GameLoop.cs`  
**Line:** 1308  
**Method:** `UnequipItemFromSlot(Item item)`

```csharp
private void UnequipItemFromSlot(Item item)
{
    if (_player.EquippedWeapon    == item) { _player.EquippedWeapon    = null; }
    else if (_player.EquippedAccessory == item) { _player.EquippedAccessory = null; }
    // ... (clears other slots)
    
    _player.Inventory.Remove(item);
    
    // ❌ THIS METHOD DOES NOT EXIST
    _player.RecalculateDerivedStats();  // LINE 1308
}
```

**Build Error:**
```
Engine/GameLoop.cs(1308,17): error CS1061: 'Player' does not contain a definition 
for 'RecalculateDerivedStats' and no accessible extension method 'RecalculateDerivedStats' 
accepting a first argument of type 'Player' could be found
```

---

## Impact Analysis

### Why the player cannot sell items:

1. **Build fails** → No executable can be produced
2. Without a working build, the game cannot run
3. The SELL flow is correct but **cannot execute** because the code doesn't compile

### Affected Code Paths:

- `HandleSell()` line 1257: Calls `UnequipItemFromSlot()` when selling equipped items
- `UnequipItemFromSlot()` line 1308: Calls missing method
- **Result:** Any attempt to compile fails before the game can run

---

## Investigation Details

### SELL Command Flow (Theoretical — Cannot Execute)

1. ✅ Player enters merchant room → sees "Type SHOP or SELL"
2. ✅ Player types `SELL` → `CommandParser.Parse()` returns `CommandType.Sell`
3. ✅ Main loop dispatches to `HandleSell()` (line 197)
4. ✅ `HandleSell()` checks for merchant presence (lines 1214-1218)
5. ✅ `BuildSellableList()` correctly gathers all inventory + equipped items (lines 1273-1288)
6. ✅ `BuildSellableList()` filters out `ItemType.Gold` (line 1278)
7. ✅ Display shows sell menu with correct prices
8. ✅ Player selects item, confirms
9. ❌ **If item is equipped:** `UnequipItemFromSlot()` is called (line 1257)
10. ❌ **Line 1308:** Attempts to call non-existent `RecalculateDerivedStats()`
11. ❌ **Compilation fails** before this point can ever be reached

### Code Analysis Summary:

| Component | Status | Notes |
|-----------|--------|-------|
| CommandParser | ✅ Correct | `SELL` maps to `CommandType.Sell` |
| GameLoop dispatch | ✅ Correct | Case handles `CommandType.Sell` |
| HandleSell() | ✅ Correct | Logic is sound |
| BuildSellableList() | ✅ Correct | Filters Gold, includes all items |
| UnequipItemFromSlot() | ❌ **BROKEN** | Calls missing method line 1308 |
| MerchantInventoryConfig | ✅ Correct | ComputeSellPrice works for all items |
| Item definitions | ✅ Correct | All items have valid SellPrice or tier-based fallback |

---

## Minimal Fix

**Remove line 1308** or **stub the missing method**.

### Option 1: Delete the call (fastest)
```csharp
// Line 1305-1308 (Engine/GameLoop.cs)
_player.Inventory.Remove(item);

// Recalculate derived stats (dodge, poison immunity, etc.)
// _player.RecalculateDerivedStats();  // ← COMMENT OUT OR DELETE
```

### Option 2: Stub the method in Player
```csharp
// Models/Player.cs (add new method)
public void RecalculateDerivedStats()
{
    // TODO: Implement stat recalculation logic
    // For now, this is a no-op to unblock compilation
}
```

**Recommendation:** Use Option 1 for immediate unblock. The stat recalculation was likely meant for a future feature (passive effects from equipment) that isn't fully implemented yet.

---

## Git History Context

Latest commit touching this area:
```
736e5e5 fix: SELL typed inside shop now opens sell menu (fixes #574, #575) (#576)
```

This commit modified `HandleShop()` and `HandleSell()` but did not introduce the `RecalculateDerivedStats()` call — that must have been added in an earlier commit and never caught because:
1. CI may not be enforcing compilation checks
2. The dev environment may have a stale binary that still runs
3. The method was removed from Player but the call site wasn't cleaned up

---

## Conclusion

**Bug Type:** Compilation error (code bug)  
**Severity:** Critical — blocks all gameplay  
**Fix Complexity:** Trivial (1 line change)  
**Testing Required:** Verify build succeeds, then manual test of SELL flow

The SELL flow logic is **architecturally correct**. The only issue is a missing method reference that prevents compilation.
