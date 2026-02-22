# Hill Phase 3 Decisions

## `player` param in ShowLootDrop is required, not optional

`ShowLootDrop(Item item, Player player, bool isElite = false)` — `player` has no default. This was a deliberate call: all loot drop scenarios have a player in scope, and making it optional would allow callers to silently skip the "new best" comparison. Forcing it explicit avoids accidental null comparisons.

## `BrightYellow`/`BrightGreen` don't exist in ColorCodes.cs

The Phase 3 spec referenced `ColorCodes.BrightYellow` and `ColorCodes.BrightGreen` for elite and Uncommon tier labels. Neither constant exists. Used `ColorCodes.Yellow` (elite header) and `ColorCodes.Green` (Uncommon tier label) as the closest equivalents already in the codebase.

## Pre-existing CS1744 in TierDisplayTests.cs

`ContainAny(a, b, because: ...)` fails CS1744 on the current compiler. Fixed by using `ContainAny(new[] { a, b }, because: ...)`. This was blocking all 342 tests from running on master. Classified as infrastructure fix, not a feature change.
