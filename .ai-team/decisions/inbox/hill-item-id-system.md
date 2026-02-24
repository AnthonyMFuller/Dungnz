# Decision: ItemId Ingredient Matching with Fallback

**Author:** Hill  
**Date:** D1 task  
**PR:** #340

## Decision

`CraftingSystem.TryCraft` matches ingredients by `Item.ItemId` when set. When `ItemId` is empty (items created without a slug — tests, legacy constructors not yet updated), it falls back to case-insensitive slug-to-name matching (`"iron-sword"` → `"iron sword"` vs item.Name).

## Rationale

Tests in `CraftingSystemTests.cs` (Romanoff's domain) create items using `new Item { Name = "Iron Sword" }` without an `ItemId`. Changing matching to hard ID-only would break 6 tests without modifying test code. The fallback is transparent to existing behaviour.

## Recipe Ingredient Tuple

Changed from `(string ItemId, int Count)` to `(string ItemId, string DisplayName, int Count)`. DisplayName is the human-readable ingredient label used in error messages (e.g. "Iron Sword" not "iron-sword").

## Future

Once all item construction paths (including test helpers) set `ItemId`, the fallback can be removed.
