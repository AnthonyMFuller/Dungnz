# Decision: Fix item-stats.json Schema Validation

**Date:** 2026-03-03  
**Agent:** Hill (Backend Developer)  
**Issue:** #849  
**PR:** #850  
**Status:** ✅ Resolved

## Problem

Game crashed on startup with schema validation failure:
```
System.IO.InvalidDataException: Schema validation failed for Data/item-stats.json:
#/Items[50]: ArrayItemNotValid
#/Items[77-83]: ArrayItemNotValid (7 items)
#/Items[97]: ArrayItemNotValid
```

All affected items were crafting materials (Iron Ore, Goblin Ear, Skeleton Dust, Troll Blood, Wraith Essence, Dragon Scale, Wyvern Fang, Soul Gem, Rodent Pelt).

## Root Cause

The JSON schema at `Data/schemas/item-stats.schema.json` was incomplete. It defined only 8 properties:
- Name, Type (required)
- HealAmount, AttackBonus, DefenseBonus, IsEquippable, Tier, Id (optional)

But every item in the actual data file has 12 properties, including:
- **StatModifier** — used for stat modifications on equipment
- **Description** — flavor text shown in UI
- **Weight** — used in inventory mechanics
- **SellPrice** — used in merchant interactions

JSON Schema validation rejects properties not defined in the schema by default. The 9 crafting materials happened to be the items that triggered validation errors at startup (likely due to validation ordering or test conditions).

## Solution

Added 4 missing property definitions to the schema:
```json
"StatModifier": { "type": "integer" },
"Description":  { "type": "string" },
"Weight":       { "type": "number", "minimum": 0 },
"SellPrice":    { "type": "integer", "minimum": 0 }
```

**No changes to data files were needed** — the data was always correct; the schema was just incomplete.

## Why This Approach

### Alternative 1: Make schema permissive (additionalProperties: true)
❌ **Rejected** — would allow typos and invalid properties to slip through validation

### Alternative 2: Remove missing properties from data
❌ **Rejected** — these properties are used throughout the codebase (display, economy, inventory systems)

### Alternative 3: Add properties to schema (chosen)
✅ **Accepted** — makes schema match reality, maintains strict validation, zero code changes needed

## Impact

- **Validation:** Schema now correctly validates all 98+ items in item-stats.json
- **Startup:** Game no longer crashes on startup
- **Code:** Zero code changes — all consuming code already handled these properties
- **Data:** Zero data changes — crafting materials already had correct structure

## Testing

1. Build succeeded: `dotnet build`
2. Game starts without errors (previously crashed immediately)
3. Startup validation passes (validated by running game to title screen)
4. No new test failures introduced

## Lessons Learned

1. **Schema validation is strict** — all properties must be explicitly defined
2. **StartupValidator runs early** — catches data issues before game loop begins
3. **Use jq for large JSON files** — `jq '.Items[N]'` to inspect specific indices
4. **Trust validation errors** — the indices in error messages are accurate (0-based)
5. **Test runtime, not just build** — schema issues only appear when validation runs

## Files Changed

- `Data/schemas/item-stats.schema.json` — added 4 property definitions

## References

- Issue: https://github.com/AnthonyMFuller/Dungnz/issues/849
- PR: https://github.com/AnthonyMFuller/Dungnz/pull/850
- Merged to: master (squashed)
- Commit: 3c1a8a2
