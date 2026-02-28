# Decision: Arrow-Key Menu Migration for Regular Shrine, Shop, and Sell

**Context:** Issues #639 and #641 requested migrating the last letter-key and number-entry interactions in GameLoop to arrow-key menus.

**Decision:** Added 3 new display methods and migrated 3 GameLoop handlers.

## New Display Methods

### `ShowShrineMenuAndSelect(int playerGold) → int`
- Returns 1–4 for the four shrine blessings (Heal/Bless/Fortify/Meditate) or 0 to leave
- Replaces letter-key input (H/B/F/M/L) in `HandleShrine()`

### `ShowShopWithSellAndSelect(stock, playerGold) → int`
- Returns 1-based item index for buying, -1 for "Sell Items", or 0 to leave
- Replaces the old number-entry + "SELL" text command pattern
- Note: Different from `ShowShopAndSelect` which only handles item selection (no Sell option)

### `ShowConfirmMenu(string prompt) → bool`
- Returns true for Yes, false for No
- Replaces Y/N text input in sell confirmation
- Reusable for any binary choice

## Implementation Notes

- All three use the existing `SelectFromMenu<T>` private helper in DisplayService
- Test mode fallback: FakeDisplayService reads from injected IInputReader for realistic test behavior
- Shop loop pattern: Buying/selling continues the loop; only explicit "Leave" exits

## Rationale

- **Consistency:** All interactive choices now use arrow-key menus (combat, level-up, crafting, shrines, shop)
- **Testability:** Numeric returns (int/bool) are easier to mock than string parsing in tests
- **Display layer ownership:** UI logic stays in Display, game state changes stay in GameLoop
- **User experience:** Arrow-key navigation is more intuitive than remembering letter/number codes

## Team Impact

- **Romanoff (Testing):** Test stubs added to FakeDisplayService and TestDisplayService — all tests pass
- **Barton (Combat):** No impact — combat menus already use arrow keys
- **Hill (Engine):** GameLoop now cleaner — no more ReadLine + switch(string) patterns in shrine/shop/sell

## Migration Status

Remaining letter-key interactions: None in core gameplay loop. All major interactions (combat, level-up, shrines, shop, crafting) now use arrow-key menus.
