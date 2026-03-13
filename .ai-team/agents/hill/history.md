# Hill — History

## Project Context
**Project:** TextGame — C# Text-Based Dungeon Crawler
**Stack:** C#, .NET console application
**Requested by:** Boss
**Team:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester), Scribe, Ralph

## Core Context

**Summarized:** Entries from 2026-02-20 through 2026-02-23 (archived to history-archive.md)

**v1 Project Scaffold (2026-02-20):**
- Created .NET 9 console project with Models/Engine/Systems/Display layer separation
- Models: Player (9 fields), Enemy (abstract base), Item (7 fields), Room (exits Dictionary<Direction,Room>), LootTable stub, all enums (Direction, CombatResult, UseResult, LootResult, ItemType)
- DisplayService created as sole Console I/O owner with 11 methods using Unicode box-drawing and emoji
- DungeonGenerator: procedural 5×4 room grid with BFS path validation, ~60% enemy placement, ~30% item placement
- GameLoop + CommandParser: 10 command types (Go/Look/Examine/Take/Use/Inventory/Stats/Help/Quit/Unknown), case-insensitive with shortcuts

**v2 Phase 0 Refactors (2026-02-20):**
- IDisplayService extracted: ConsoleDisplayService renamed, GameLoop/CombatEngine constructors updated to accept IDisplayService
- TestDisplayService created as composition-based headless test double (not inheritance-based)
- Player Encapsulation Refactor (PR #26): Private setters, TakeDamage/Heal/ModifyAttack/LevelUp public methods, Math.Clamp guards, IReadOnlyList<Item> for inventory
- Dead code removal: InventoryManager class removed (responsibility merged into Player/Equipment)
- Config-Driven Balance System (PR #31): appsettings.json for enemy/item stats, configurable spawn rates
- Equipment Slot System (PR #34): EquippedWeapon, EquippedArmor, EquippedAccessory, ArmorSlot enum, GetArmorSlotItem()

**v2 v3 Planning Contributions (2026-02-20):**
- v2 C# implementation proposal: Save/load via System.Text.Json with Guid hydration/dehydration, record types for DTOs, file-scoped namespaces, C# 12 collection expressions
- v3 session: Player.cs 273 LOC SRP violation, Equipment/Inventory fragmentation, zero integration tests flagged as blockers
- Pre-v3 encapsulation audit: Player strong encapsulation; Enemy/Room inconsistent (public setters without validation)
- Save/load architecture: two-pass hydration (create all Rooms, then wire Exits), BFS traversal for dehydration

**UI/UX Phase 0 Infrastructure (PR #298, 2026-02-22):**
- Added RenderBar() private static helper for HP/MP/XP bars (filled `█` + empty `░` blocks with ANSI color)
- Added ANSI-safe padding helpers: VisibleLength(), PadRightVisible(), PadLeftVisible() (strips ANSI before measuring)
- Fixed ShowLootDrop/ShowInventory alignment bugs using PadRightVisible
- Updated ShowCombatStatus signature to include playerEffects + enemyEffects (IReadOnlyList<ActiveEffect>)
- Added 7 new IDisplayService stubs for Phases 1–3 display work

**Phase 1 Loot Display + Phase 2 Display (PRs #228, #230, #231, 2026-02-22):**
- Tier-colored item names (ColorizeItemName pattern) across ShowLootDrop, ShowInventory, ShowShop
- ShowInventory grouping by item type (3.1), ShowLootDrop signature change with weight warning (3.2–3.4)
- Phase 4 ShowMap overhaul (PRs #239, #243, #248): BFS-based ASCII map with dynamic legend, box-drawing connectors, compass rose

**Phase 1 Display Implementations (PR #304, 2026-02-23):**
- Combat start display, floor banner, victory/gameover screens wired to IDisplayService methods
- Intro display design: ShowEnhancedTitle, ShowIntroNarrative, ShowNamePrompt, ShowDifficultySelection, ShowClassSelection
- Intro seed input repositioned post-class selection (silent by default)
- ASCII art research for enemies: AsciiArtRegistry class, ShowCombatStart(Enemy) integration point, max 8 lines × 36 chars

**Interactive Menus + UI Consistency (2026-02-22–27):**
- feat/interactive-menus: Arrow-key navigation for all menu prompts (commit a8dcb52)
- fix/ui-consistency-class-card (PR #595): Warrior icon standardization, Rogue indentation, ShowLootDrop name padding
- squad/ui-consistency-fixes (PR #600): Full Unicode icon audit — all icons standardized to narrow symbols (EAW=N); IL() helper replaces EL() since all icons are 1-column
- Symbol standards: ⚔ Weapon, ⛨ Chest/Armor/Off-Hand, ⛑ Head, ◈ Shoulders, ☞ Hands, ≡ Legs, ⤓ Feet, ↩ Back, ✦ Accessory/Combo, ★ Level, ⚗ Consumable, ✶ CraftingMaterial












**Key Technical Patterns Established:**
- `AsciiArt` on Enemy is `string[]` (not string) — use `string.Join("\n", enemy.AsciiArt)`
- Enemy model has no Description property on base class

---

## Learnings

## Learnings — Deep Code Review (2026-02-27)

**Task:** Full review of Display layer and Engine/GameLoop for bugs affecting player experience.

**Files reviewed:** `Display/DisplayService.cs`, `Display/ConsoleMenuNavigator.cs`, `Engine/GameLoop.cs`, `Engine/CommandParser.cs`, `Engine/IntroSequence.cs`, `Program.cs`.

**Issues filed:**

| Issue | Title | Severity |
|-------|-------|----------|
| #604 | `ShowLootDrop` namePad uses `icon.Length` not visual width | HIGH |
| #605 | `HandleUse` turn consumed when consumable has no recognized effect | HIGH |
| #606 | `HandleLoad` does not reset `RunStats` — pre-load stats bleed into loaded run | HIGH |
| #607 | `SelectFromMenu` cursor not restored on exception (no try/finally) | MEDIUM |
| #608 | `ConsoleMenuNavigator.Select` never hides cursor during navigation | MEDIUM |
| #609 | Arrow-key menus corrupt rendering when option count ≥ terminal height | MEDIUM |
| #610 | `ShowPrestigeInfo` box misaligned — ⭐ (U+2B50) counts as 1 char but is 2 visual cols | LOW |

**Key things confirmed clean:** CommandParser is fully null-safe; color-reset discipline is consistent across all display methods; cursor-up formula in menu renderers (`options.Count - 1`) is correct for the no-trailing-newline pattern; 1-item menus work correctly; `HandleGo`/`HandleTake`/`HandleExamine` all set `_turnConsumed = false` on every rejection path.

**Pattern reinforced:** `PadRightVisible` should be used everywhere icon+name strings are constructed in box rows — raw `.Length` on icons is unreliable.

## Learnings — #674, #679 (Difficulty Balance Overhaul — Phase 1)

### Branch: squad/674-679-difficulty-settings-expanded | PR: TBD

**Files changed:**
- `Models/Difficulty.cs` — Added 9 new properties to `DifficultySettings`: `PlayerDamageMultiplier`, `EnemyDamageMultiplier`, `HealingMultiplier`, `MerchantPriceMultiplier`, `XPMultiplier`, `StartingGold`, `StartingPotions`, `ShrineSpawnMultiplier`, `MerchantSpawnMultiplier`. Updated `For()` method to return fully populated object initializers for all three difficulties (Casual, Normal, Hard) with explicit values for all properties.
- `Engine/IntroSequence.cs` — Modified `BuildPlayer()` to accept `DifficultySettings settings` parameter and apply `StartingGold` and `StartingPotions` to the new player. Updated `Run()` to call `DifficultySettings.For(difficulty)` and pass settings to `BuildPlayer()`.

**Why this matters:**
- Previous `DifficultySettings` only had 4 properties (EnemyStatMultiplier, LootDropMultiplier, GoldMultiplier, Permadeath). `LootDropMultiplier` and `GoldMultiplier` were dead code — no systems were reading them.
- Phase 1 (this change): Expand the model and wire up starting conditions. Phase 2 (future): Wire up the multipliers in CombatEngine, LootManager, MerchantManager, etc.
- The new properties provide fine-grained control over difficulty balance: player damage vs enemy damage, healing effectiveness, merchant prices, XP gains, shrine/merchant spawn rates, and starting resources.

**Design patterns used:**
- Starting potions are added to `player.Inventory` as `Item` objects with `Type = ItemType.Consumable`, `Name = "Health Potion"`, `HealAmount = 20`, `Tier = ItemTier.Common`.
- The `For()` method now uses explicit multi-line object initializers for all three difficulty cases instead of inline initializers, making the values easy to read and modify.
- All properties are set explicitly in all three cases — no reliance on default values in the `For()` return.

**Difficulty values chosen:**

| Property                  | Casual | Normal | Hard   |
|---------------------------|--------|--------|--------|
| EnemyStatMultiplier       | 0.65f  | 1.00f  | 1.35f  |
| EnemyDamageMultiplier     | 0.70f  | 1.00f  | 1.25f  |
| PlayerDamageMultiplier    | 1.20f  | 1.00f  | 0.90f  |
| LootDropMultiplier        | 1.60f  | 1.00f  | 0.65f  |
| GoldMultiplier            | 1.80f  | 1.00f  | 0.60f  |
| HealingMultiplier         | 1.50f  | 1.00f  | 0.75f  |
| MerchantPriceMultiplier   | 0.65f  | 1.00f  | 1.40f  |
| XPMultiplier              | 1.40f  | 1.00f  | 0.80f  |
| StartingGold              | 50     | 15     | 0      |
| StartingPotions           | 3      | 1      | 0      |
| ShrineSpawnMultiplier     | 1.50f  | 1.00f  | 0.70f  |
| MerchantSpawnMultiplier   | 1.40f  | 1.00f  | 0.70f  |
| Permadeath                | false  | false  | true   |

**Key insight:** Normal mode values are all 1.0f (or neutral defaults) — this is the baseline. Casual makes the game easier across all dimensions (cheaper items, more healing, more XP, more resources). Hard makes the game harder across all dimensions (tougher enemies, less healing, less XP, fewer resources, permadeath).

## Learnings — #701 (GoblinWarchief JSON Serialization Fix)

**Pattern: Registering new Enemy subclasses for JSON polymorphic serialization**

When adding a new `Enemy` subclass — especially a subclass of an existing subclass like `DungeonBoss` — it must be registered in the `[JsonDerivedType]` attribute list on the `Enemy` base class in `Models/Enemy.cs`. Failure to do so causes a `System.NotSupportedException` at runtime when saving game state:

```
System.NotSupportedException: Runtime type 'Dungnz.Systems.Enemies.GoblinWarchief' is not supported by polymorphic type 'Dungnz.Models.Enemy'.
```

**Rule:** Every concrete `Enemy` subclass (including subclasses of `DungeonBoss`, `DungeonElite`, etc.) needs its own `[JsonDerivedType(typeof(ClassName), "discriminator")]` line on the `Enemy` base class. The discriminator string should be the class name in lowercase (e.g., `"goblinwarchief"`). This is easy to miss when the new class extends an intermediate abstract class rather than `Enemy` directly.

## Learnings — #736 (Modernize GEAR Display with Spectre.Console)

- The GEAR display was migrated from EquipmentManager manual ASCII rendering to SpectreDisplayService.ShowEquipment()
- ShowEquipment(Player) added to IDisplayService, implemented with Table (rounded, gold border, 3 columns: Slot/Item/Stats)
- TierColor() helper already existed in SpectreDisplayService — reused for item name coloring
- SetBonusManager.GetActiveBonusDescription() used for set bonus footer (rendered in a Panel when non-empty)
- EquipmentManager.ShowEquipment() is now a one-liner delegating to _display.ShowEquipment(player); private helpers ShowArmorSlot, PadRightVisible, ColorizeItemName removed
- DisplayService.cs gets a minimal legacy stub (Console.WriteLine("[EQUIPMENT]"))
- FakeDisplayService and TestDisplayService both get AllOutput.Add("show_equipment") stubs

### 2026-03-01 — Serialization Fix: Room State Fields Wiring (#739, #746, #747, PR #749)

**Branch:** `squad/739-serialization-fixes`

## Learnings

### How SaveSystem.cs structures its save and load paths

**Save path** — inside `SaveGame()`, a BFS (`CollectRooms`) walks the room graph from the current room, collecting all reachable `Room` objects. Each `Room` is projected into a `RoomSaveData` init-only record using a LINQ `.Select()`. Exits are dehydrated to a `Dictionary<Direction, Guid>` (IDs only, no object references) to avoid circular-reference serialization. The whole `SaveData` (Player + Rooms + metadata) is serialized to JSON with a tmp-file swap pattern for atomic writes.

**Load path** — inside `LoadGame()`, a two-pass hydration strategy is used:
1. **Pass 1:** Iterate `saveData.Rooms`, construct each `Room` from flat fields (no exits yet), store in `Dictionary<Guid, Room>`.
2. **Pass 2:** Iterate `saveData.Rooms` again, look up each room in the dict, then wire `room.Exits[direction] = roomDict[exitId]` to restore the full bidirectional graph.

**The gap that caused PR #749 to fail:** Four fields (`SpecialRoomUsed`, `BlessedHealApplied`, `EnvironmentalHazard`, `Trap`) had been added to `RoomSaveData` but were never populated in the save LINQ projection or assigned back during the load pass. Fix was 9 lines: 4 in the save path, 4 in the load path, 1 trailing-comma fix.

## Learnings — #813 (Chest Slot Label Alignment in GEAR Display)

- The 🛡 (U+1F6E1), ⚔ (U+2694), and ⛨ (U+26E8) symbols are all 1-column-wide in terminals
- They all need 2 spaces after them in slot labels to align with full-width emoji entries in the GEAR table
- The Chest slot had only 1 space, causing its label to appear shifted relative to other equipment slots
- All item display contexts (inventory, pickup, shop, examine, equip menu) use `ItemTypeIcon()` in `Display/SpectreDisplayService.cs`. For armor slot-awareness, the pattern is `ItemIcon(Item)` which delegates to `SlotIcon(ArmorSlot)`.
- `ArmorSlot` enum is in `Models/ArmorSlot.cs` — values: None, Head, Shoulders, Chest, Hands, Legs, Feet, Back, OffHand

### 2025 — Emoji Label Audit (#820, #821, #822)

**Issues Closed:** #820, #821, #822
**File Modified:** `Display/SpectreDisplayService.cs`

## Learnings

**What was found:**
- Line 233: `table.AddRow("⚡ Combo", ...)` — ⚡ is in `NarrowEmoji` but was using raw string (1 space instead of 2)
- Line 766: `table.AddRow("⭐ Level", ...)` — ⭐ is NOT in `NarrowEmoji` (wide emoji, 1 space is correct), but was using raw string instead of `EL()`
- All other emoji+text labels in table rows and menus were already using `EL()` (equipment slots, combat actions)

**What was fixed:**
- Line 233: Updated to `EL("⚡", "Combo")` — now correctly gets 2 spaces (narrow emoji)
- Line 766: Updated to `EL("⭐", "Level")` — gets 1 space (wide emoji, correct behavior)

**Key decision:** ⭐ (U+2B50) is a wide emoji and was NOT added to `NarrowEmoji`. EL() gives it 1 space, which is correct for terminal rendering.

**Build:** `dotnet build` passes with 0 errors (3 pre-existing XML doc warnings, unrelated).

## Learnings — Mini-Map Phase 1

**What I implemented:**
- Fog of war: after BFS, built `knownSet` (visited + current rooms), then expanded it one hop to include unvisited neighbours. These show as `[grey][[?]][/]`. Connectors between visited and fog rooms appear automatically because fog rooms enter the grid.
- Room type symbols: added 8 new symbols in `GetMapRoomSymbol()` — `[M]` Merchant, `[T]` Trap, `[A]` Armory, `[L]` Library, `[F]` ForgottenShrine, `[~]` Hazard, `[*]` BlessedClearing, `[D]` Dark. Priority: BlessedClearing before generic hazard check.
- Legend split into two lines to fit all symbols.
- `ShowMap(Room, int floor = 1)` signature propagated to interface, legacy DisplayService, GameLoop (passes `_currentFloor`), and both test helpers.

**Key design decision:** Default parameter `floor = 1` keeps all existing callers valid without changes.

## Learnings — Mini-Map Phase 2

**Issues Closed:** #826, #827
**File Modified:** `Display/SpectreDisplayService.cs`

### Dynamic Legend (#826)

After building `grid`, iterate all rooms (skipping `currentRoom`) and set boolean flags for each room-type/state category, mirroring the exact priority order in `GetMapRoomSymbol`. Build `legendEntries` list starting with `[@] You` (always), then append each entry only if its flag was set. Format into 1 line (≤7 symbols) or 2 lines (>7 symbols) using `string.Join("   ", ...)` with `legHalf = (count + 1) / 2` split.

Key detail: variable names like `showBoss`, `showExit`, etc. must not conflict with loop-scoped variables `hasConnector`/`hasSouth` already in the method — they live in nested for-loop scopes so names are safe.

### Box-Drawing Connectors + Compass Rose (#827)

- Horizontal connector: `"-"` → `"─"` (U+2500)
- Vertical connector: `" | "` → `" │ "` (U+2502)
- Compass rose: replaced 2-line `═══ MAP ═══   N` + `↑` header with 4-line block: `═══ MAP ═══`, then `      N` / `    W ✦ E` / `      S` (✦ U+2726 as center marker)

## Learnings — Icon Standardization (#829)

**Issue Closed:** #829
**PR:** https://github.com/AnthonyMFuller/Dungnz/pull/830
**File Modified:** `Display/SpectreDisplayService.cs`

### Problem
Mixed-width emoji (wide emoji U+1F300+ vs narrow symbols U+2600-U+27BF) caused terminal alignment issues. Chest slot using 🛡 (U+1F6E1, EAW=N but not in `NarrowEmoji` set) got 1 space instead of 2, misaligning with other slots.

### Solution
Executive directive: ALL icons must use the same character set. Replaced all emoji with narrow Unicode symbols exclusively from Miscellaneous Symbols (U+2600–U+26FF) and Dingbats (U+2700–U+27BF) blocks. All are EAW=N (1 terminal column) — no ambiguity.

### Symbol Choices
- **Equipment slots:** ⚔ Weapon (U+2694), ✦ Accessory (U+2726), ⛑ Head (U+26D1 helmet), ◈ Shoulders (U+25C8), ⛨ Chest (U+26E8), ☞ Hands (U+261E pointing finger), ≡ Legs (U+2261 triple bar), ⤓ Feet (U+2913 downward arrow), ↩ Back (U+21A9 cloak-like), ⛨ Off-Hand (U+26E8)
- **Player stats:** ★ Level (U+2605 filled star), ✦ Combo (U+2726 sparkle)
- **Combat menu:** ⚔ Attack (U+2694), ✦ Ability (U+2726), ↗ Flee (U+2197 diagonal arrow), ⚗ Use Item (U+2697 alembic)
- **Item types:** ⚔ Weapon, ⛨ Armor, ⚗ Consumable, ✦ Accessory, ✶ CraftingMaterial (U+2736 six-pointed star)

**Why these symbols:** Selected for visual metaphor (⛑ is literally helmet, ⚗ is alchemical, ⚔ is swords) while staying strictly in narrow Unicode ranges. Verified all are EAW=N or EAW=A (ambiguous-narrow).

### EL() Simplification
Since ALL icons are now 1-column wide, replaced `EL(emoji, text)` helper with `IL(icon, text)` (Icon Label) that always adds 2 spaces: `$"{icon}  {text}"`. Deleted the `NarrowEmoji` HashSet entirely — no longer needed.

**Pattern:** icon(1 col) + 2 spaces = text starts at visual column 3, consistent across all UI.

### Build
`dotnet build Dungnz.csproj` passes with 0 errors (4 pre-existing XML doc warnings, unrelated).

## Learnings — Inventory Inspect & Compare Features (#844, #845, #846)

**Issues Closed:** #844 (COMPARE command), #845 (Enhanced EXAMINE), #846 (Interactive INVENTORY)
**PR:** #847 — `feat: add COMPARE command, enhanced EXAMINE, and interactive inventory`
**Branch:** `squad/844-845-846-inspect-compare`
**Design Spec:** `.ai-team/decisions/inbox/coulson-inspect-compare-design.md`

### What Was Implemented

**1. COMPARE Command (#844)**
- Added `Compare` to `CommandType` enum in `Engine/CommandParser.cs` (after `Leaderboard`, before `Unknown`)
- Added `"compare" or "comp"` switch case in parser with fuzzy-match support
- Implemented `HandleCompare(string itemName)` in `Engine/GameLoop.cs`:
  - No argument: shows interactive menu with equippable items only via `ShowEquipMenuAndSelect`
  - With argument: finds item by case-insensitive contains match, validates equippable, then shows comparison
  - Error cases: no equippable items, item not found, item not equippable
  - Never consumes a turn (`_turnConsumed = false` on all paths)

**2. Enhanced EXAMINE (#845)**
- Modified `HandleExamine` in `Engine/GameLoop.cs` to auto-show comparison after detail card for equippable inventory items
- Slot resolution uses new `GetCurrentlyEquippedForItem(Item)` helper
- Non-breaking: only affects inventory items (room items and enemies unchanged)

**3. Interactive INVENTORY (#846)**
- Changed `case CommandType.Inventory:` dispatcher to call `ShowInventoryAndSelect` instead of `ShowInventory`
- After selection, shows `ShowItemDetail` + auto-comparison if equippable
- Cancelling selection returns gracefully without error
- Never consumes a turn (`_turnConsumed = false`)

**4. Display Service Methods**
- Added `ShowInventoryAndSelect(Player)` to `IDisplayService.cs` interface
- **SpectreDisplayService:** Uses `SelectionPrompt<string>` with item names + `[grey]« Cancel »[/]` option
- **DisplayService (fallback):** Numbered text input with 'x' to cancel
- **Test helpers:** Added stubs to `FakeDisplayService` (with input reader support) and `TestDisplayService`

**5. Helper Method — GetCurrentlyEquippedForItem**
```csharp
private Item? GetCurrentlyEquippedForItem(Item item)
{
    return item.Type switch
    {
        ItemType.Weapon    => _player.EquippedWeapon,
        ItemType.Armor     => _player.GetArmorSlotItem(item.Slot == ArmorSlot.None ? ArmorSlot.Chest : item.Slot),
        ItemType.Accessory => _player.EquippedAccessory,
        _                  => null
    };
}
```
- Mirrors exact slot resolution logic from `EquipmentManager.DoEquip`
- `ArmorSlot.None` defaults to `Chest` (existing behavior)

**6. README Update**
Updated commands table to reflect new functionality:
- `examine <target>` — now mentions auto-comparison for equippable inventory items
- `inventory` — changed from "List carried items" to "Interactive item browser with arrow-key selection; displays details and comparison for selected equippable items"
- `compare <item>` — new row documenting COMPARE command with `comp` alias

### Key Design Decisions

**Reuse Over Reinvention:**
- COMPARE reuses existing `ShowEquipmentComparison` method (no new display code needed)
- Interactive selection reuses existing `ShowEquipMenuAndSelect` pattern
- Slot resolution mirrors `EquipmentManager.DoEquip` exactly (no divergence)

**Non-Breaking Behavior:**
- COMPARE/INVENTORY never consume a turn (info-only commands)
- EXAMINE only shows comparison for *inventory* items (room items/enemies unchanged)
- All interactive menus support cancellation without error

**Progressive Disclosure:**
- EXAMINE shows detail card first, then comparison (two separate calls)
- INVENTORY shows full list, then selection prompt (not hidden behind command)
- COMPARE validates equippable status before showing comparison

### Build & Verification

`dotnet build --no-restore` passes with 0 errors (5 pre-existing XML doc warnings, unrelated).

Pre-push hook required README update due to Engine/ changes — addressed by updating commands table.

### Files Modified
1. `Engine/CommandParser.cs` — enum, switch case, fuzzy-match array
2. `Engine/GameLoop.cs` — dispatcher, HandleExamine, HandleCompare, GetCurrentlyEquippedForItem, Inventory case
3. `Display/IDisplayService.cs` — ShowInventoryAndSelect signature
4. `Display/SpectreDisplayService.cs` — ShowInventoryAndSelect with SelectionPrompt
5. `Display/DisplayService.cs` — ShowInventoryAndSelect fallback
6. `Dungnz.Tests/Helpers/FakeDisplayService.cs` — ShowInventoryAndSelect stub
7. `Dungnz.Tests/Helpers/TestDisplayService.cs` — ShowInventoryAndSelect stub
8. `README.md` — commands table updates

### Coordination Note

Romanoff already implemented unit tests on branch `squad/846-inspect-compare-tests` (commit `9759491`). This implementation branch (`squad/844-845-846-inspect-compare`) contains only production code. Tests will merge separately to avoid conflicts.

---

# P1 Reliability Bug Sprint — 2026-03-04

**Issues:** #932, #937, #939, #941, #964
**PRs Opened:** #973 (AddXP overflow), #974 (empty room pool), #976 (LootTable typo)

## Summary

Worked five P1 structural/reliability issues in order. Two were already fixed in the codebase; three required minimal targeted fixes.

## Issue #932 — FirstOrDefault unchecked (ALREADY FIXED)

Audited all five command handlers (Compare, Craft, Examine, Use, Take) plus InventoryManager, EquipmentManager, CombatEngine, and StatusEffectManager. Every `FirstOrDefault` result was already followed by a null check before use. Closed with comment.

## Issue #937 — DungeonGenerator bounds check (ALREADY FIXED)

DungeonGenerator.cs lines 195–205 already have `specialIdx < eligibleRooms.Count` guards on every special-room assignment. The fix was in place before this sprint. Closed with comment.

## Issue #939 — AddXP int overflow risk → PR #973

**Branch:** `squad/939-addxp-overflow-guard`
**File:** `Models/PlayerStats.cs`

`XP += amount` with no overflow cap. On a very long run, XP near `int.MaxValue` would silently wrap to a negative value, breaking level-up comparisons.

**Fix:** `XP = (int)Math.Min((long)XP + amount, int.MaxValue);` — widened to long for the addition, then clamped and cast back. XML doc updated.

## Issue #941 — Empty room description pool → PR #974

**Branch:** `squad/941-empty-room-description-guard`
**File:** `Engine/DungeonGenerator.cs`

`roomPool[_rng.Next(roomPool.Length)]` throws `ArgumentOutOfRangeException` when `roomPool.Length == 0` (e.g. unrecognised floor).

**Fix:** `roomPool.Length > 0 ? roomPool[_rng.Next(roomPool.Length)] : string.Empty`

## Issue #964 — LootTable parameter typo → PR #976

**Branch:** `squad/964-loot-table-typo-fix`
**Files:** `Models/LootTable.cs`, `Dungnz.Tests/LootTableAdditionalTests.cs`

Parameter was spelled `dungeoonFloor` (double 'o') in signature, XML doc, and method body. Tests used the named argument so also needed updating.

**Fix:** Renamed parameter to `dungeonFloor` in signature, XML doc `<param>`, three body references, and five test named-argument call sites.

## Build & Test

`dotnet build --nologo` — 0 errors on all branches.
`dotnet test --nologo -q` — 1430/1430 passed on all branches.

### 2026-03-05 — Deep Code Audit (Engine + Models)

**Task:** Systematic audit of all Engine/ and Models/ files for bugs, data integrity, resource issues, edge cases, code quality, and serialization.

**Scope:** GameLoop.cs, CombatEngine.cs, DungeonGenerator.cs, CommandParser.cs, EnemyFactory.cs, all AI files, all command handlers, all model files (Player*.cs, Enemy.cs, Item.cs, Room.cs, LootTable.cs, etc.), SaveSystem.cs, Program.cs.

**Findings (13 new issues identified):**

| # | Severity | Category | File | Summary |
|---|----------|----------|------|---------|
| 1 | P1 | bug | Program.cs:65 | Loaded game always uses Normal difficulty |
| 2 | P1 | bug | DescendCommandHandler.cs:44 | playerLevel not passed to DungeonGenerator |
| 3 | P1 | bug | SaveSystem.cs (SaveData) | Difficulty not persisted in save file |
| 4 | P1 | bug | SaveSystem.cs (RoomSaveData) | Room.State not saved/loaded |
| 5 | P1 | bug | CombatEngine.cs:1322-1325 | ManaLeech drains mana via direct mutation bypassing SpendMana |
| 6 | P2 | bug | DungeonGenerator.cs:259 | CreateRandomItem creates LINQ pool per call — allocates on every room |
| 7 | P2 | tech-debt | CombatEngine.cs:25 | _turnLog grows unbounded across combats (never shrunk) |
| 8 | P2 | design-smell | EnemyFactory.cs:15-16 | Static mutable state not thread-safe, no Initialize guard |
| 9 | P2 | tech-debt | CombatEngine.cs:891-1344 | PerformEnemyTurn is 450+ lines with deeply nested branches |
| 10 | P2 | bug | GoblinShamanAI.cs | AI class exists but CombatEngine has inline shaman logic that shadows it |
| 11 | P2 | design-smell | Room.cs:101 | Items is mutable public List<Item> |
| 12 | P3 | code-smell | CombatEngine.cs:908-909 | Duplicate shaman heal cooldown tracked in both AI class and engine field |
| 13 | P3 | code-smell | LichAI.cs + LichKingAI.cs | Identical classes — should share a base or be unified |

**Key patterns:**
- Save system does not preserve difficulty or room narrative state
- Loaded games silently downgrade to Normal difficulty
- DungeonGenerator.Generate() defaults playerLevel=1 and both callers omit it
- CombatEngine has grown to 1709 lines with inline enemy AI that duplicates dedicated AI classes
- EnemyFactory relies on static mutable state with no re-entrance or initialization guard

### 2026-03-03 — Batch Bug Fixes (#940, #942, #938, #958, #959, #936)

**PR:** #1008 — `fix: Batch Hill fixes (#940, #942, #938, #958, #959, #936)`
**Branch:** `squad/batch-hill-fixes`

**Issues addressed:**

#### #940 — SaveGame null validation missing on restore
- `SaveSystem.LoadGame()` lacked null guards for `Player`, `Rooms`, individual room entries, `ExitIds`, `UnlockedSkills`, `StatusEffects`
- `CurrentRoomId` lookup used direct dictionary indexing (throws `KeyNotFoundException` on corrupt data)
- **Fix:** Added null/empty checks for all critical fields; used `TryGetValue` for CurrentRoomId; null-coalesced `Items` and `Description` in room reconstruction

#### #942 — Missing handler warning if CommandType has no handler
- `GameLoop.RunLoop()` treated unregistered `CommandType` values the same as `CommandType.Unknown`
- **Fix:** Split the `else` branch: `Unknown` shows user-facing error; registered enum values without a handler log a warning via `ILogger`

#### #938 — Enemy.LootTable default not enforced
- `Enemy.LootTable` was a simple auto-property with `= new LootTable()` default, but nothing prevented assigning null
- **Fix:** Changed to backing-field property with null-coalescing setter (`value ?? new LootTable()`)

#### #958 — Hardcoded dungeon grid dimensions
- `DungeonGenerator.Generate()` had magic numbers `width=5, height=4` as parameter defaults
- **Fix:** Extracted to `DungeonGenerator.DefaultWidth` and `DungeonGenerator.DefaultHeight` public constants

#### #959 — Hardcoded FinalFloor=8 duplicated
- `private const int FinalFloor = 8` was duplicated in GameLoop.cs, GoCommandHandler.cs, StatsCommandHandler.cs, DescendCommandHandler.cs
- **Fix:** Defined `DungeonGenerator.FinalFloor` as canonical source; all consumers now reference it

#### #936 — Event handler memory leak risk
- `GameEventBus` had `Subscribe<T>()` and `Clear()` but no way to remove individual handlers
- `GameEvents` (standard C# events) had no cleanup method
- `SoulHarvestPassive` registered on `GameEventBus` with no way to unregister
- **Fix:** Added `GameEventBus.Unsubscribe<T>()`, `SoulHarvestPassive.Unregister()`, and `GameEvents.ClearAll()`

**Key file paths:**
- `Systems/SaveSystem.cs` — save/load with migration pipeline
- `Engine/GameLoop.cs` — command dispatch via `_handlers` dictionary, `FinalFloor` constant
- `Engine/DungeonGenerator.cs` — grid generation constants (`DefaultWidth`, `DefaultHeight`, `FinalFloor`)
- `Engine/Commands/GoCommandHandler.cs` — room navigation + win condition check
- `Engine/Commands/DescendCommandHandler.cs` — floor descent + dungeon regeneration
- `Engine/Commands/StatsCommandHandler.cs` — floor progress display
- `Models/Enemy.cs` — abstract base with `LootTable` property
- `Systems/GameEventBus.cs` — generic pub/sub with Subscribe/Unsubscribe/Clear
- `Systems/GameEvents.cs` — standard C# event hub with ClearAll cleanup
- `Systems/SoulHarvestPassive.cs` — event bus consumer with Register/Unregister pattern

### 2026-03-06 — Terminal.Gui TUI Core Infrastructure (PR #1030)

**Task:** Implement Phase 1 TUI core infrastructure for Terminal.Gui migration (Issues #1017-#1021).

**Branch:** `squad/1017-1021-tui-core`

**Implementation:**

Created complete Terminal.Gui TUI foundation in `Display/Tui/` directory:

1. **TuiLayout.cs** — Split-screen layout with 5 panels:
   - Map Panel (top-left, 60% width × 30% height) — dungeon map display
   - Stats Panel (top-right, 40% width × 30% height) — player HP/MP/stats/equipment
   - Content Panel (middle, 100% width × 50% height) — room descriptions, combat text, menus
   - Message Log Panel (lower, 100% width × 15% height) — scrollable message history
   - Command Input (bottom, 100% width × 5% height) — text field for player commands

2. **GameThreadBridge.cs** — Dual-thread coordination:
   - Terminal.Gui runs on main thread via `Application.Run()`
   - GameLoop runs on background thread
   - `Application.MainLoop.Invoke()` marshals UI updates from game thread
   - `BlockingCollection<string>` queues commands from UI to game thread
   - `TaskCompletionSource<T>` pattern for synchronous input methods

3. **TerminalGuiInputReader.cs** — IInputReader implementation:
   - `ReadLine()` blocks on `_bridge.WaitForCommand()` until user types in TUI
   - `ReadKey()` returns null (TUI uses modal dialogs, not Console.ReadKey)
   - `IsInteractive` returns false (TUI controls its own focus)

4. **TuiMenuDialog.cs** — Reusable modal dialog:
   - Generic `TuiMenuDialog<T>` for type-safe option selection
   - Terminal.Gui `Dialog` + `ListView` for arrow-key navigation
   - Helper methods: `Show()` for strings, `ShowIndexed()` for 1-based indices, `ShowConfirm()` for Yes/No

5. **TerminalGuiDisplayService.cs** — Full IDisplayService implementation:
   - All 73 methods implemented
   - Pure output methods use `GameThreadBridge.InvokeOnUiThread()` to update panels
   - 19 input-coupled methods use `GameThreadBridge.InvokeOnUiThreadAndWait()` + `TuiMenuDialog`
   - Simplified map rendering (full BFS-based map deferred to later phase)
   - Simplified ShowSkillTreeMenu (complex skill UI deferred to later phase)

## Learnings

### Terminal.Gui v1.x Architecture Patterns

**Thread-safe UI updates:**
- Terminal.Gui v1.x uses `Application.MainLoop.Invoke(Action)` (not `Application.Invoke()` as in v2 docs)
- All UI updates from non-UI threads MUST be marshaled via `MainLoop.Invoke()`
- `TaskCompletionSource` with `TaskCreationOptions.RunContinuationsAsynchronously` prevents deadlocks

**Event handlers and return values:**
- `Button.Clicked` event expects `Action` (void return), not `Func<T>`
- Cannot use `return value` inside event lambda — must capture result in outer variable
- Pattern: `int? result = null; okButton.Clicked += () => { result = Parse(...); RequestStop(); }; return result;`

**Dialog lifecycle:**
- `Application.Run(dialog)` blocks until `Application.RequestStop()` is called
- Dialog must call `RequestStop()` in button handlers to unblock
- Result variables captured before `Run()` are available after it returns

### Model Property Mapping

**Player properties:**
- `player.Class` (enum) → `.ToString()` for display, NOT `player.ClassName`
- XP to next level: `100 * player.Level` (calculated, not a property)
- Equipment slots: `EquippedHead`, `EquippedHands`, `EquippedFeet` (not Helm/Gloves/Boots)
- Inventory max: `Player.MaxInventorySize` (const, not instance property)
- Skills: `player.Skills.IsUnlocked(id)` (SkillTree, not `player.UnlockedSkills`)

**Enemy properties:**
- `AsciiArt` is `string[]`, not `string` — use `string.Join("\n", enemy.AsciiArt)`
- No `Description` property on Enemy base class

**RunStats properties:**
- `GoldCollected` and `ItemsFound` (not GoldEarned/ItemsCollected)

**ItemType enum:**
- Values: `Weapon`, `Armor`, `Accessory`, `Consumable`, `CraftingMaterial`, `Gold`
- No `Chest`, `Helm`, `Gloves`, `Boots` (those are ArmorSlot, not ItemType)

### Nullable Reference Type Patterns

**Generic dialog options:**
- `TuiMenuDialog<Item?>` options must be typed as `(string Label, Item? Value)`
- LINQ: `.Select(i => (i.Name, (Item?)i))` casts to nullable explicitly
- Null-coalescing on structs: `Difficulty` is struct, `?? Difficulty.Normal` illegal

**Enum return defaults:**
- `StartupMenuOption` and `Difficulty` are non-nullable value types
- Cannot use `?? default` — just return the result directly

### Display/Tui Architecture Decisions

**Simplified implementations:**
- Map rendering: Shows current room position + exits only (full BFS map requires dungeon registry access)
- ShowSkillTreeMenu: Returns null (complex skill UI deferred to panel implementation phase)
- These are marked for enhancement in later PRs

**Additive-only changes:**
- All TUI code in `Display/Tui/` — NO changes to existing Display/ files
- SpectreDisplayService, IDisplayService, IInputReader remain untouched
- Zero regression risk: default Spectre.Console path unchanged

**Build/test metrics:**
- 1796 lines of new code (5 files)
- 0 errors, 0 warnings
- All 1641 tests pass
- Clean build in 3.68s


---

### 2026-03-04 — TUI Display Quality Fixes (#1048–#1054)

**PR:** #1056 — `fix: TUI stats panel refresh + display quality fixes`
**Branch:** `squad/1048-tui-display-fixes`
**Issues:** #1048, #1049, #1051, #1052, #1053, #1054

**Goal:** Fix 6 TUI display bugs affecting stats panel staleness, content growth, and visual clarity.

**Changes made:**

1. **Issue #1048 — Stats panel stale after equip/unequip**
   - `Systems/EquipmentManager.cs`: Added `ShowPlayerStats()` after equip and unequip operations
   
2. **Issue #1049 — Stats panel stale after shrine/combat/level-up**
   - `Engine/GameLoop.cs`: Added `ShowPlayerStats()` after:
     - All shrine interactions (heal, bless, fortify, meditate, sacred ground auto-heal)
     - Forgotten shrine attack buff
     - Room hazards (lava, corrupted ground, blessed clearing)
     - Library XP/MaxHP bonuses
   - `Engine/Commands/SellCommandHandler.cs`: Added after selling items
   - `Engine/Commands/ShopCommandHandler.cs`: Added after buying items
   - `Engine/Commands/CraftCommandHandler.cs`: Added after successful crafts

3. **Issue #1051 — Content panel grows unbounded**
   - `Display/Tui/TuiLayout.cs`: Capped `AppendContent()` at 500 lines
   - Uses `lines.Skip(lines.Length - 500)` pattern from message log

4. **Issue #1052 — ShowEquipment missing item stats**
   - `Display/Tui/TerminalGuiDisplayService.cs`: Enhanced `ShowEquipment()` to show stat bonuses
   - Format: `Weapon:    Iron Sword       (+5 ATK)`
   - Uses existing `GetPrimaryStatLabel()` helper

5. **Issue #1053 — ShowColoredStat ignores color**
   - `Display/Tui/TerminalGuiDisplayService.cs`: Routed to `AppendLog()` with type mapping
   - Red/BrightRed → "error", Green/BrightGreen → "loot", default → "info"

6. **Issue #1054 — Missing Application.Refresh() after panel updates**
   - `Display/Tui/TuiLayout.cs`: Added `Application.Refresh()` to:
     - `SetMap()`, `SetStats()`, `AppendContent()`, `AppendLog()`
   - Guarded with `if (Application.Driver is not null)` for test compatibility

**Key learnings:**
- ShowPlayerStats must be called after ANY player stat change (HP, gold, XP, level, ATK, DEF, MaxHP, MaxMana)
- GameLoop has many stat-changing code paths: shrines, hazards, special rooms, merchants, crafting
- ShowRoom() already calls ShowPlayerStats internally, so don't duplicate after ShowRoom()
- Application.Refresh() forces Terminal.Gui immediate repaint; critical for background-thread updates
- Content panel line cap prevents memory growth; mirrors message log's 100-line cap pattern

**Files changed:** 7 files, +56 lines
**Tests:** All 1988 tests pass

### 2026-03-05 — TUI Library Research and Analysis

**Requested by:** Anthony
**Objective:** Assess options to improve or replace the TUI implementation — purely technical research from a C# dev perspective

**Current State Analysis:**

**Dependencies (from Dungnz.csproj):**
- `Terminal.Gui` v1.19.0 (current TUI framework)
- `Spectre.Console` v0.54.0 (already in project, used for SpectreDisplayService)
- Target: `.NET 10.0`
- TUI implementation: ~2,370 LOC across 6 files in Display/Tui/
- Interface contract: `IDisplayService` with 35 methods (many input-coupled)

**Known Limitations of Current Terminal.Gui v1.19 Implementation:**
1. No per-character color control — entire TextView has one ColorScheme
2. ANSI escape sequences stripped/ignored in TextViews
3. ShowColoredMessage/ShowColoredCombatMessage/ShowColoredStat route to message log with icon prefixes, not inline color
4. TuiColorMapper exists but only partially used (log type mapping, not inline text)
5. Panel-level color contexts work (combat=red, shop=yellow, loot=green, gear=cyan)

---

#### Option 1: Terminal.Gui v2 Upgrade

**Status:** Terminal.Gui v2 is in active development (as of 2024-2025 timeframe)

**NuGet Package Health:**
- Package: `Terminal.Gui`
- v1.19 released ~2024 Q2, stable
- v2 pre-release track exists (2.x alpha/beta builds on NuGet)
- Maintenance: Active — Microsoft-backed project, Miguel de Icaza as primary contributor
- License: MIT (permissive)

**v2 Key Improvements:**
- True Attributed Text support — `AttributedString` allows per-character foreground/background color
- ANSI/VT100 sequence parsing — can render ANSI-colored strings directly
- Improved layout engine — constraint-based positioning, better resize handling
- More widgets: Tabs, Menus, improved ListView with data binding
- Better keyboard navigation and focus management
- Breaking changes: API surface differs significantly from v1

**.NET 10 Compatibility:**
- v2 targets .NET 6+ (LTS), should be compatible with .NET 10
- No known blockers for .NET 10

**Migration Cost Estimate:**
- **HIGH** — v1 → v2 is a breaking change migration
- TuiLayout.cs: ~30% rewrite (ColorScheme API changed, new Attribute model)
- TerminalGuiDisplayService.cs: ~40% rewrite (AttributedString for inline color)
- TuiMenuDialog.cs: ~20% rewrite (menu/list widget API changes)
- GameThreadBridge.cs: Likely unchanged (threading model same)
- **Estimate: 600-800 LOC changes, 2-3 days full-time**

**Gains:**
- ✅ Fixes inline color limitation — ShowColoredMessage can render actual colored text
- ✅ Better ANSI support — existing Spectre markup could be partially reused
- ✅ Modern widget set — richer UI components available
- ❌ Still a TUI framework, not graphical — doesn't fundamentally change the "feel"

**Risk:**
- v2 still in beta/RC — API may change before stable release
- Documentation less mature than v1
- Requires re-testing all 35 IDisplayService methods in TUI mode

---

#### Option 2: Spectre.Console Live Rendering (Pure Spectre, No Terminal.Gui)

**Status:** Already a dependency (v0.54.0)

**NuGet Package Health:**
- Package: `Spectre.Console`
- v0.54 released Q4 2024, actively maintained
- Patrik Svensson (GitHub: spectreconsole) — excellent community support
- License: MIT
- .NET 10 compatible: Yes — targets .NET 6+

**Spectre.Console TUI-Like Features:**
- `Live` class — live-updating panels that redraw on a background thread
- `Layout` — split screen into rows/columns with `Panel` widgets
- `Table`, `Tree`, `BarChart` — rich structured display
- Full ANSI color support — `[red]text[/]` markup
- `Prompt<T>`, `SelectionPrompt<T>`, `MultiSelectionPrompt<T>` — interactive menus

**Can It Replace Terminal.Gui?**
- **Partially** — Spectre.Console Live + Layout can mimic a split-screen TUI
- Example: `Layout` with 5 rows (Map, Stats, Content, Log, Input)
- Update via `Live.Refresh()` or `ctx.Refresh()` in a background task

**Key Limitations:**
- No native "application" abstraction — no Toplevel/Window/Focus
- Input handling is synchronous prompt-based — no async event model like Terminal.Gui
- No persistent command input field — would need custom Console.ReadLine loop
- Cursor positioning is less fine-grained than Terminal.Gui views

**Migration Cost Estimate:**
- **MEDIUM-HIGH** — Not a 1:1 replacement, requires architectural rethink
- Abandon TuiLayout.cs, replace with Spectre `Layout` + `Live` (~200 LOC)
- TerminalGuiDisplayService.cs → SpectreDisplayService refactor (~500 LOC changes)
- Input model: Replace TerminalGuiInputReader with async `Console.ReadLine` + `BlockingCollection` (~100 LOC)
- GameThreadBridge: Simplify or remove (Spectre `Live` handles thread marshalling)
- TuiMenuDialog: Replace with `SelectionPrompt<T>` (~150 LOC)
- **Estimate: 950 LOC, 3-4 days full-time**

**Gains:**
- ✅ Unify on one UI library (remove Terminal.Gui dependency)
- ✅ Full ANSI color support — ShowColoredMessage works inline
- ✅ Simpler threading model — `Live.Start()` handles background updates
- ✅ Already used in SpectreDisplayService — team familiar with API
- ❌ Lose some Terminal.Gui widgets (FrameView border styles, TextField focus)

**Risk:**
- Input model is less "application-like" — closer to a console REPL than a TUI app
- May feel like a regression from Terminal.Gui's widget model
- Resize handling less robust than Terminal.Gui

---

#### Option 3: Consolonia (Avalonia-based TUI)

**Status:** Relatively new (2021+), niche

**NuGet Package Health:**
- Package: `Consolonia` + `Consolonia.Themes`
- Latest: v0.4.x (as of late 2024)
- Maintenance: Active but small team (~2-3 core contributors)
- GitHub: github.com/jinek/Consolonia (~800 stars as of 2025)
- License: MIT
- .NET 10 compatible: Targets .NET 6+, should work with .NET 10

**What Is It:**
- TUI framework built on Avalonia UI (desktop GUI framework)
- Uses Avalonia XAML + MVVM patterns for TUI layouts
- Renders to console via ANSI/VT100
- Declarative UI: define panels/grids/controls in XAML or C# builders

**Pros:**
- Modern declarative UI — more maintainable than imperative Terminal.Gui code
- Full Avalonia control library available (Button, TextBox, ListBox, DataGrid, etc.)
- Strong data binding — can bind game state directly to UI controls
- Designer support (Avalonia Previewer works for Consolonia)

**Cons:**
- ❌ **Maturity risk** — v0.4.x is not production-grade stable
- ❌ Small community — StackOverflow/Discord support limited
- ❌ Avalonia dependency — heavy framework (10+ MB) for a text game
- ❌ XAML learning curve — none of the team has Avalonia experience (per history)
- ❌ Overkill — we don't need desktop GUI features, just better TUI

**Migration Cost Estimate:**
- **VERY HIGH** — Complete rewrite of Display layer
- Learn Avalonia XAML/MVVM patterns (~1 week ramp-up)
- Rewrite TuiLayout as XAML or Avalonia C# builders (~400 LOC)
- Refactor TerminalGuiDisplayService to Avalonia view models (~800 LOC)
- New input handling via Avalonia event model (~200 LOC)
- **Estimate: 1,400 LOC, 1-2 weeks full-time**

**Gains:**
- ✅ Best-in-class declarative UI (if we want to invest in TUI long-term)
- ✅ Full color and theming support
- ✅ Future-proof: could pivot to actual GUI (WPF/macOS) using same XAML
- ❌ Huge investment for marginal TUI improvement

**Risk:**
- Pre-1.0 software — breaking changes likely
- Documentation sparse — limited production usage examples
- Dependency bloat — Avalonia is 10+ NuGet packages

---

#### Option 4: gui.cs (Terminal.Gui Fork Analysis)

**Status:** `gui.cs` WAS the original name of Terminal.Gui pre-v1

**Historical Context:**
- Miguel de Icaza's `gui.cs` project was renamed to `Terminal.Gui` around 2019
- NuGet package `gui.cs` does not exist as a separate maintained fork
- Some forks exist on GitHub but none are NuGet-published or actively maintained

**Verdict:**
- ❌ Not a viable option — `Terminal.Gui` is the canonical package
- No active fork to migrate to

---

#### Option 5: Raw ANSI/VT100 Custom Renderer

**What:** Write a minimal split-screen TUI using raw `Console.SetCursorPosition`, ANSI escape codes, and manual buffer management

**Pros:**
- ✅ Zero external dependencies (besides System.Console)
- ✅ Full control over rendering — no framework limitations
- ✅ Tiny implementation — ~300-400 LOC for basic layout
- ✅ Educational value — team learns low-level console I/O

**Cons:**
- ❌ **Massive engineering cost** for production quality:
  - Resize detection and handling (~100 LOC)
  - Scrollback buffer management (~150 LOC)
  - Flicker-free double buffering (~100 LOC)
  - Cross-platform cursor/color support (Windows vs Linux/macOS) (~200 LOC)
  - Keyboard input handling (arrow keys, Ctrl combinations, etc.) (~150 LOC)
- ❌ Reinventing the wheel — Terminal.Gui already does this
- ❌ Testing complexity — manual mocking of Console I/O

**Migration Cost Estimate:**
- **EXTREME** — Not recommended unless Terminal.Gui is abandoned
- TuiLayout.cs: Rewrite as raw ANSI layout manager (~400 LOC)
- TerminalGuiDisplayService.cs: Rewrite as raw Console I/O (~600 LOC)
- Input handling: Custom ConsoleKeyInfo processing (~200 LOC)
- **Estimate: 1,200 LOC, 2 weeks full-time + 1 week debugging edge cases**

**Gains:**
- ✅ No external TUI dependency
- ✅ Full control over rendering behavior
- ❌ High maintenance burden — we own all bugs

**Risk:**
- Terminal quirks (cmd.exe vs PowerShell vs bash vs zsh)
- Unicode rendering inconsistencies
- Platform-specific ANSI sequence support
- Not a "quick win" — this is a multi-week R&D project

---

#### Option 6: Hybrid — Keep Terminal.Gui, Enhance with Spectre.Console Panels

**What:** Use Terminal.Gui for layout/input, but render content panels using Spectre.Console markup

**Strategy:**
- Keep TuiLayout.cs as-is (FrameView, TextView widgets)
- Replace ContentPanel.Text setter with Spectre `Panel` rendering to `StringBuilder`
- Use Spectre ANSI markup → convert to plain text + inject into Terminal.Gui TextView
- Spectre for menus: Replace TuiMenuDialog with Spectre `SelectionPrompt` (blocks UI thread, renders in ContentPanel)

**Pros:**
- ✅ Low risk — additive changes only
- ✅ Fixes inline color limitation (Spectre markup → plain text with color preservation if Terminal.Gui v2, or just rich plain text)
- ✅ Leverages existing Spectre dependency

**Cons:**
- ❌ Still constrained by Terminal.Gui v1 color limitations (unless upgrading to v2)
- ❌ Hybrid complexity — two rendering models in one codebase
- ❌ Spectre SelectionPrompt blocks UI thread — breaks async input model

**Migration Cost Estimate:**
- **LOW-MEDIUM** — Incremental changes
- Add Spectre Panel rendering helper (~50 LOC)
- Refactor ShowColoredMessage to use Spectre markup (~30 LOC)
- **Estimate: 80 LOC, 1 day**

**Gains:**
- ✅ Immediate color improvements in content panel
- ✅ Keeps existing Terminal.Gui layout structure
- ❌ Doesn't fundamentally solve Terminal.Gui limitations

---

### Integration Cost Assessment: IDisplayService Abstraction

**Current State:**
- `IDisplayService` has 35 methods
- 19 are input-coupled (e.g., `ShowInventoryAndSelect`, `ShowCombatMenuAndSelect`)
- Implementations:
  - `SpectreDisplayService` (~2,100 LOC) — fully functional
  - `TerminalGuiDisplayService` (~1,580 LOC) — fully functional (post-#1045)
  - `FakeDisplayService` (test stub) (~50 LOC)

**How Hard Is It to Swap?**
- ✅ **Abstraction works well** — proven by dual Spectre + Terminal.Gui implementations
- ✅ Program.cs has clean `--tui` flag switch (10 LOC)
- ✅ Zero GameLoop/CombatEngine changes required (validated during Terminal.Gui migration)

**Risk Surface for New Implementation:**
1. **Input-coupled methods** (19 methods) — highest risk
   - Must block and return user choice (int, string, Item?, Ability?, etc.)
   - Framework must support modal dialogs or prompt patterns
2. **Color methods** (4 methods) — medium risk
   - ShowColoredMessage, ShowColoredCombatMessage, ShowColoredStat, ShowEquipmentComparison
   - Framework must support inline or styled color
3. **Live-updating panels** (ShowRoom, ShowPlayerStats, ShowMap) — medium risk
   - TUI implementations need persistent panels; console implementations render-and-forget
4. **Async rendering** — low risk if using background thread + marshalling pattern (GameThreadBridge)

**Estimated Effort for New IDisplayService Implementation:**
- Minimal viable (all 35 methods stubbed): 200 LOC
- Functional (input-coupled methods work, color basic): 800 LOC
- Full-featured (parity with current TUI): 1,500 LOC

---

### Recommendations (Hill's C# Dev Perspective)

#### Short-Term (1-2 days effort):
1. **Upgrade Terminal.Gui v1.19 → v2.x (when stable)**
   - Fixes inline color limitation
   - Low risk: v2 API is evolutionary, not revolutionary
   - Wait for v2 stable release (check NuGet, likely Q2-Q3 2025)

#### Medium-Term (3-5 days effort):
2. **Pure Spectre.Console TUI using Live + Layout**
   - Remove Terminal.Gui dependency entirely
   - Simpler threading model
   - Team already familiar with Spectre API
   - Trade-off: Lose some Terminal.Gui widget polish

#### Long-Term (1-2 weeks effort):
3. **Consolonia (Avalonia TUI)**
   - Only if TUI is a long-term strategic investment
   - Declarative UI is cleaner for complex layouts
   - High initial cost, better maintainability long-term

#### Not Recommended:
- ❌ Raw ANSI/VT100 — reinventing the wheel, high maintenance burden
- ❌ gui.cs fork — does not exist as a maintained package
- ❌ Hybrid Terminal.Gui + Spectre — adds complexity without solving root issues

---

### .NET 10 Compatibility Summary

All evaluated libraries are .NET 10 compatible:
- ✅ Terminal.Gui v1.19 and v2.x: Targets .NET 6+
- ✅ Spectre.Console v0.54: Targets .NET 6+
- ✅ Consolonia v0.4.x: Targets .NET 6+

No blocking issues for .NET 10 (current target in Dungnz.csproj).

---

### Lines-of-Code Estimates

| Approach | New LOC | Changed LOC | Total Effort | Risk |
|----------|---------|-------------|--------------|------|
| Terminal.Gui v1 → v2 | 200 | 600 | ~800 | Medium |
| Pure Spectre.Console | 300 | 650 | ~950 | Medium |
| Consolonia | 500 | 900 | ~1,400 | High |
| Raw ANSI | 800 | 400 | ~1,200 | Very High |
| Hybrid (Spectre panels) | 50 | 30 | ~80 | Low |

**Current TUI Implementation:** 2,370 LOC (6 files)

---

### Anthony's Question: "Vastly Improve or Replace?"

**Hill's Answer:**

**To "Vastly Improve" (keep Terminal.Gui):**
- Wait for Terminal.Gui v2 stable release, then upgrade
- Gain: Inline color support, better rendering
- Cost: 2-3 days migration
- Risk: Medium (API changes)

**To "Replace" (better UI implementation):**
- **Best ROI:** Pure Spectre.Console using Live + Layout
  - Why: Already a dependency, team familiar, simpler threading
  - Cost: 3-4 days full rewrite
  - Risk: Medium (input model less polished than Terminal.Gui)
- **If long-term TUI investment:** Consolonia
  - Why: Declarative UI, future-proof, best maintainability
  - Cost: 1-2 weeks
  - Risk: High (pre-1.0 software, learning curve)

**My recommendation as C# dev:** 
1. Try **Spectre.Console Live + Layout** first (proof-of-concept: 1 day, full implementation: 3-4 days)
2. If Spectre doesn't feel right, **wait for Terminal.Gui v2 stable** and upgrade
3. Avoid Consolonia unless TUI is a multi-year strategic focus

---

**Research complete.** All findings based on public NuGet metadata, GitHub activity, and .NET ecosystem knowledge as of March 2026. No packages installed, no code changes made.


### 2026-03-05 — Option E Technical Assessment (Spectre.Console Live+Layout)

**Context:** Deep-dive on Option E for UI — replacing Terminal.Gui with Spectre.Console Live + Layout hybrid implementation.

**Deliverable:** Technical feasibility assessment, implementation estimates, gut check for squad greenlight decision.

---

## Session: 2026-03-05 — Spectre.Console Live+Layout Technical Research

**Task:** Deep technical analysis of Spectre.Console Live+Layout API as Terminal.Gui replacement.

### Learnings

**Spectre.Console Live API Findings:**

1. **Live+Layout Works:** `AnsiConsole.Live(layout).Start(ctx => {...})` successfully creates persistent multi-panel layouts. Can replicate TuiLayout's 5-panel structure (map, stats, content, log, input) using nested Layout.SplitRows/SplitColumns.

2. **Thread Safety — Major Win:** `ctx.Refresh()` is thread-safe. Can call from background threads without marshalling. This eliminates the need for GameThreadBridge entirely — much simpler than Terminal.Gui's MainLoop.Invoke() requirement.

3. **Input Conflict — Critical Problem:** Console.ReadLine() technically works inside Live.Start(), but input appears *below* the rendered layout, not *inside* the input panel. This breaks the persistent 5-panel TUI design.

4. **Modal Dialog Challenge:** SelectionPrompt cannot be used inside Live.Start() (rendering conflict). Options:
   - Exit Live, show SelectionPrompt, re-enter Live (loses persistent panels during menu)
   - Custom Console.ReadKey loop inside Live (50-100 LOC per menu type, reinventing arrow-key navigation)
   - Hybrid: simple menus in Live, complex menus exit to SelectionPrompt

5. **LOC Estimate:** 1,200-1,500 LOC for full IDisplayService implementation (vs 2,095 LOC current TUI). Smaller codebase, but input handling adds complexity.

6. **Input-Coupled Methods:** 24 of 54 methods (44%) require user input. Spectre.Console Live was designed for live dashboards (display updates), not interactive TUI (modal dialogs). Input is the weak point.

**Architectural Trade-offs:**

**Gain:**
- Thread-safe rendering (no GameThreadBridge)
- Persistent panels during narration/combat
- One less dependency
- ~40% less code

**Lose:**
- Input panel not truly part of layout (appears below, not inside)
- Menus break out of layout (or require custom input loops)
- No built-in arrow-key widgets

**Recommendation:** Build 1-day proof-of-concept to show Anthony the visual experience. If acceptable, full implementation is 3-4 days. If not acceptable, wait for Terminal.Gui v2 stable.

**Research artifacts:**
- Test project: scripts/spectre_test.csproj
- Full analysis: scripts/spectre_api_research.md
- 5 API tests executed, results documented

**No code changes made to Dungnz codebase.** Research only.


---

## Session: 2026-03-06 — Implement SpectreLayout + Display Methods (#1063, #1065, #1066)

**Task:** Implement the 5-panel SpectreLayout, thread-safe context, all display-only methods, and HP/MP urgency bars.

### Learnings

**Files Created/Modified:**

- `Display/Spectre/SpectreLayout.cs` — Created by Coulson (scaffold, complete). 5-panel Layout: TopRow→Map/Stats (30% height, 3:2 ratio), Content (50%), BottomRow→Log/Input (20%, 7:3 ratio). Panel name constants in `SpectreLayout.Panels`.
- `Display/Spectre/SpectreLayoutContext.cs` — Created by Coulson (scaffold, complete). Thread-safe wrapper with `UpdatePanel(string, IRenderable)` + `Refresh()`, `IsLiveActive`, lock-based thread safety.
- `Display/Spectre/SpectreLayoutDisplayService.cs` — Modified (Hill). Changed `sealed` → `partial`, added `StartAsync()`, implemented all ~30 display-only methods.
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs` — Already created by Barton (not modified by Hill). Contains all input-coupled methods + `TierColor`, `PrimaryStatLabel`, `GetRoomDisplayName` helpers.

**Key Implementation Patterns:**

1. **Content buffer pattern:** `_contentLines: List<string>` stores markup strings. `SetContent()` replaces, `AppendContent()` appends. `RefreshContentPanel()` joins and pushes to panel.

2. **Log panel pattern:** `_logHistory: List<string>` stores markup-formatted entries (`[grey]HH:mm[/] icon [color]message[/]`). `AppendLog(plain, type)` adds with timestamp and type-based coloring.

3. **Auto-refresh on room entry:** `ShowRoom()` caches `_cachedRoom`, then calls `RenderMapPanel()` and `RenderStatsPanel(_cachedPlayer)`.

4. **HP/MP urgency bars (issue #1066):**
   - HP: green >50%, yellow 25–50%, red <25% — `BuildHpBar(current, max, width=10)`
   - MP: blue >50%, mediumpurple1 25–50%, darkviolet <25% — `BuildMpBar(current, max, width=10)`
   - Format: `[color]████████░░[/]`

5. **Barton/Hill partial class split:** Hill owns all display-only methods + `ItemTypeIcon`, `SlotIcon`, `ItemIcon`, `EffectIcon`, `MapAnsiToSpectre`, `StripAnsiCodes`. Barton owns input methods + `TierColor`, `PrimaryStatLabel`, `GetRoomDisplayName`, `InputTierColor`, etc. Important: don't duplicate helpers across partial files.

6. **BFS map:** Same algorithm as SpectreDisplayService. Legend is dynamically built from rooms actually visible in the grid.

7. **StartAsync() pattern:** `public Task StartAsync() => Task.Run(StartLive);` — starts Live loop on background Task, game loop runs on main thread.

8. **Panel `Text` vs `Markup`:** Use `new Panel(new Markup(content))` with `Markup.Escape(userText)` for safe rendering of user-provided content.

**PR:** #1071 (co-committed with Barton's input methods on `squad/1067-input-methods`)
**Issues closed:** #1063, #1065, #1066



### 2026-03-06 — Display-Layer Bug Fixes (#1075, #1076, #1077, #1083, #1085, #1086, #1087)

**Commit:** 5340855 (committed alongside #1078-1088 fixes in same session)
**Branch:** `scribe/log-2026-03-06-tui-bug-hunt`

Fixed 7 display-layer bugs in `SpectreLayoutDisplayService` that caused blank screens, stale state, deadlocks, and inconsistent UI rendering.

**Files modified:**
- `Display/Spectre/SpectreLayoutDisplayService.cs` (main class)
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs` (partial class)
- `Engine/GameLoop.cs` (added Reset() calls)

**#1075 — ShowIntroNarrative and ShowPrestigeInfo render blank before Live starts:**
- Added `AnsiConsole.Write()` fallback branch to both methods when `!_ctx.IsLiveActive`
- Pattern matches `ShowTitle()`: if Live active, use `SetContent()`, else write directly to console
- Fixes: Lore and prestige info now visible during character creation (before Live loop starts)

**#1076 — Stale cached player/room state persists between game runs:**
- Added `public void Reset()` method that clears `_cachedPlayer`, `_cachedRoom`, `_contentLines`, `_logHistory`, `_currentFloor`, and resets headers/colors
- Called at start of both `GameLoop.Run(Player, Room)` and `GameLoop.Run(GameState)` methods
- Fixes: Display state is clean for each new game run (important for loaded saves)

**#1077 — Nested PauseAndRun deadlock risk in combat ability submenu:**
- Added `private int _pauseDepth = 0;` field (in main class file, not partial)
- Modified `PauseAndRun<T>()` in `Input.cs` to track nesting depth with `Interlocked.Increment/Decrement`
- Only pause/resume Live events at top level (depth == 1 / depth == 0)
- Fixes: Combat menu → Ability submenu works without corrupting pause state

**#1083 — Content panel cleared on combat start — room context lost:**
- Changed `ShowCombatStart()` to append instead of clear: removed `_contentLines.Clear()`
- Prepends blank line separator, then combat header, then enemy name
- Room description remains visible above combat log during fight
- Fixes: Player can still see room description and items while in combat

**#1085 — Dead code: RunPrompt and RunNullablePrompt never called:**
- Deleted `RunPrompt<T>` and `RunNullablePrompt<T>` methods from main class file
- The correct method `PauseAndRun<T>` in `Input.cs` is the one actually used by all input methods
- Fixes: Removed ~50 lines of confusing dead code

**#1086 — Map legend shows room types not visible on map:**
- In `BuildMapMarkup()` legend building loop, added `bool isCleared = rL.Visited && rL.Enemy?.HP <= 0`
- Skip cleared rooms when setting type flags (Dark, Armory, Library, FShrine, Hazard)
- Legend now only shows symbols that are actually rendered on the map
- Fixes: No `[[D]] Dark` in legend when all Dark rooms are cleared and render as `[+]`

**#1087 — MaxContentLines(100) vs TakeLast(50) inconsistency:**
- Changed `private const int MaxContentLines = 100;` to `50`
- Matches `RefreshContentPanel()` which calls `_contentLines.TakeLast(50)`
- Fixes: Content buffer and display are now consistent; no dead memory

**Tests:** All 1674 tests pass.

## Learnings

**AnsiConsole.Write() fallback pattern:**
- Display methods that render before Live starts (title, lore, prestige) MUST check `_ctx.IsLiveActive`
- If Live is active, use `SetContent()` to update panel; otherwise use `AnsiConsole.Write(new Markup(...))` directly
- This pattern ensures content is visible during startup sequence (before Live loop begins)

**Display state management between runs:**
- Cached state (`_cachedPlayer`, `_cachedRoom`, content/log history) must be cleared on each game start
- `Reset()` method provides centralized cleanup called by `GameLoop.Run()` methods
- Important for loaded saves and consecutive game runs in same process

**PauseAndRun nesting:**
- Nested SelectionPrompts (menu → submenu) require depth tracking
- Use `Interlocked` for thread-safe counter; only signal events at top level (depth 0/1)
- Prevents pause/resume event corruption when submenus call PauseAndRun recursively

**Content panel persistence in combat:**
- Never call `_contentLines.Clear()` during combat — room context must remain visible
- Use `AppendContent("")` separator + new content instead of replacing
- Player needs to see room description, items, and combat log simultaneously

**Map legend accuracy:**
- Legend should only show symbols that are ACTUALLY RENDERED on the current map
- Cleared rooms render as `[+]`, not their original type symbol (`[D]`, `[A]`, etc.)
- Check `isCleared` flag before setting type flags in legend builder loop

**Memory/display consistency:**
- Buffer size constants (MaxContentLines) must match usage patterns (TakeLast N)
- If buffer holds 100 but only displays 50, you're wasting memory and confusing future maintainers
- Keep buffer size = display size unless there's a documented reason to differ

### 2026-03-06 — Gear Panel + Layout Restructure (#1103, #1104)

**PR:** #1105 — `feat: Add Gear panel and vertical Log/Command stack`
**Branch:** `squad/1103-gear-panel-and-layout-improvements`

**Issues addressed:**

#### #1103 — Add dedicated Gear panel showing all 10 equipment slots
- `SpectreLayout.cs`: Added `Panels.Gear = "Gear"` constant; restructured layout to 6 panels
- Layout is now: TopRow(20%) = Map|Stats; MiddleRow(50%) = Content|Gear; BottomRow(30%) = Log/Input stacked
- Added Gear panel placeholder with gold border and `[bold yellow]⚔  Gear[/]` header
- `SpectreLayoutDisplayService.cs`: Added private `RenderGearPanel(Player player)` method
  - Uses inner `AddSlot(icon, slotName, item)` lambda pattern (consistent with `ShowEquipment`)
  - Shows all 10 slots: Weapon, Accessory, Head, Shoulders, Chest, Hands, Legs, Feet, Back, Off-Hand
  - Reuses existing `TierColor()` and `PrimaryStatLabel()` helpers from `SpectreLayoutDisplayService.Input.cs`
  - Updates panel via `_ctx.UpdatePanel(SpectreLayout.Panels.Gear, panel)`
- Removed 2-slot gear summary (Weapon + Chest) from `RenderStatsPanel` — Stats panel is now stats-only
- `ShowPlayerStats(player)` now calls both `RenderStatsPanel` and `RenderGearPanel`

#### #1104 — Stack Message Log and Command vertically; widen Command
- `SpectreLayout.cs`: Changed `BottomRow` from `SplitColumns` to `SplitRows`
- Log takes 70% of bottom row height, Input takes 30% — both span full terminal width

**Key file paths:**
- `Display/Spectre/SpectreLayout.cs` — layout tree and panel constants (~100 lines, easy to read entirely)
- `Display/Spectre/SpectreLayoutDisplayService.cs` — main display service; `RenderStatsPanel` ~line 403, `RenderGearPanel` added after it, `ShowPlayerStats` ~line 627
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs` — `TierColor()` at line 591, `PrimaryStatLabel()` at line 602

**Patterns and conventions established:**
- Inner `AddSlot(icon, slotName, item)` local function is the standard pattern for slot-list rendering in both `ShowEquipment` (content panel) and `RenderGearPanel` (gear panel)
- Gear panel uses `_cachedPlayer` indirectly — `ShowPlayerStats` updates `_cachedPlayer` and calls `RenderGearPanel`; no separate cached gear player needed
- `[ExcludeFromCodeCoverage]` on the class means no tests needed for display methods
- Architecture test `Display_Should_Not_Depend_On_System_Console` — never use `Console.*` directly in Display namespace

**Build & Test Status:**
- ✅ `dotnet test --nologo -v q` — 1674/1674 passing

---

### 2026-03-06 — All 18 Display Bugs Fixed (#1107–#1124)

**PR:** #1125 — `fix: resolve all 18 display bugs from deep UI audit`
**Branch:** `squad/1107-fix-all-display-bugs`

**Issues addressed (all 18 — triaged by Coulson/Romanoff audit):**

#### BUG-1 (#1107) — All in-game menus crash (InvalidOperationException)
- Root cause: `AnsiConsole.Live().Start()` holds `DefaultExclusivityMode` lock for its entire callback. `PauseAndRun` + `AnsiConsole.Prompt(SelectionPrompt)` throws because the lock is still held while Live loop is blocked.
- Fix: Added `ContentPanelMenu<T>` and `ContentPanelMenuNullable<T>` private methods in `SpectreLayoutDisplayService.Input.cs`
- These render menu items to the content panel via `SetContent` (▶ for selected) and use `AnsiConsole.Console.Input.ReadKey(intercept: true)` for navigation — same pattern as `ReadCommandInput`, no exclusivity needed
- `SelectionPromptValue` and `NullableSelectionPrompt` now dispatch to content-panel menu when `_ctx.IsLiveActive`, fall back to `AnsiConsole.Prompt` for startup (pre-Live) menus
- `ShowSkillTreeMenu`: `Skill?` is a nullable value type (not `class`), so `ContentPanelMenuNullable<T> where T : class` can't be used. Inlined the navigation loop directly in `ShowSkillTreeMenu` for the Live-active path.

#### BUG-4 (#1111) — After take, content panel stuck on Pickup view
- `TakeSingleItem` calls `context.Display.ShowRoom(context.CurrentRoom)` after pickup.

#### BUG-5 (#1112) — After combat, map still shows enemy [!]; content stale
- `GoCommandHandler` calls `context.Display.ShowRoom(context.CurrentRoom)` after `CombatResult.Won` block.

#### BUG-6 (#1113) — ShowRoom spams log "Entered room" every hazard tick
- `ShowRoom` now checks `isNewRoom = _cachedRoom?.Id != room.Id` before setting `_cachedRoom`. Only logs "Entered <room>" on new-room transitions.

#### BUG-7 (#1114) — Hazard damage message instantly erased by RefreshDisplay
- `GameLoop.ApplyRoomHazard`: replaced `_display.RefreshDisplay(...)` with `_display.ShowPlayerStats(_player)` in each hazard case. Hazard messages now stay visible.

#### BUG-8 (#1115) — ShowCombatStart doesn't clear content
- `ShowCombatStart` now calls `_contentLines.Clear()` and sets `_contentHeader = "⚔  Combat"` before appending. Consistent with `ShowCombatStatus`'s check of `_contentHeader == "⚔  Combat"`.

#### BUG-9 (#1116) — After equip/unequip, content panel not restored to room view
- `EquipCommandHandler.Handle` calls `context.Display.ShowRoom(context.CurrentRoom)` after `HandleEquip`.
- `UnequipCommandHandler.Handle` calls `context.Display.ShowRoom(context.CurrentRoom)` after `HandleUnequip`.

#### BUG-10 (#1117) — ShowEquipmentComparison bypasses _contentLines
- When `_ctx.IsLiveActive`, `ShowEquipmentComparison` now clears `_contentLines`, sets `_contentHeader = "⚔  ITEM DROP"` and `_contentBorderColor = Color.Yellow` before calling `_ctx.UpdatePanel`. Keeps internal buffer in sync with what's displayed.

#### BUG-11 (#1118) + BUG-18 (#1124) — RefreshDisplay double-renders; stale floor number
- `RefreshDisplay` now sets `_currentFloor = floor` FIRST, then calls `ShowPlayerStats(player)`, then `ShowRoom(room)`. Removed redundant `ShowMap(room, floor)` call since `ShowRoom` already calls `RenderMapPanel`.

#### BUG-12 (#1119) — Map ignores SpecialRoomUsed for special rooms
- `GetMapRoomSymbol` now checks `!r.SpecialRoomUsed` for ContestedArmory, PetrifiedLibrary, and ForgottenShrine. Used rooms fall through to `[+]` Cleared symbol.

#### BUG-13 (#1120) — ShowCombatStatus wipes accumulated combat messages
- `ShowCombatStatus`: if `_contentHeader == "⚔  Combat"` (combat view already established), appends a `[grey]──[/]` separator then the HP bars line-by-line via `AppendContent`. Only calls `SetContent` on first call to establish the view.

#### BUG-14 (#1121) — ShowFloorBanner doesn't update map panel header
- `ShowFloorBanner` calls `if (_cachedRoom != null) RenderMapPanel(_cachedRoom)` after setting `_currentFloor`. Map header immediately shows the new floor number.

#### BUG-15 (#1122) — TakeAllItems flickers through N content views
- `TakeAllItems` no longer calls `ShowItemPickup` per item (which triggers N `SetContent` flashes). Instead appends `ShowMessage("📦 Picked up: {item.Name}")` per item, then calls `ShowRoom` once after the loop.

#### BUG-16 (#1123) — GetRoomDisplayName returns "Room" for most types
- `GetRoomDisplayName` now has explicit cases for all `RoomType` values: Standard → "Dungeon Room", TrapRoom → "Trap Room", default → "Dungeon Room".

**Key Learnings:**
- `AnsiConsole.Live().Start()` holds `DefaultExclusivityMode` for the entire callback duration — ANY `AnsiConsole.Prompt()` call while Live is running will throw, even when the loop is blocked/sleeping. `ReadKey(intercept: true)` is the ONLY safe input method while Live is active.
- `ContentPanelMenu<T>` approach (render to content panel + ReadKey navigation) is the correct pattern for all in-game menus in the Spectre Live display. The `PauseAndRun` mechanism works for non-`SelectionPrompt` usage (e.g., `TextPrompt` via `ReadPlayerName`) only because `ReadPlayerName` is called pre-Live.
- Nullable value types (e.g., `Skill?` = `Nullable<Skill>`) don't satisfy `where T : class`. When a method needs to return a nullable value type, inline the navigation loop rather than using a generic helper with `where T : class`.
- `ShowCombatStart` must set `_contentHeader = "⚔  Combat"` (not `"Combat"`) to be consistent with the `ShowCombatStatus` check. String matching between methods is a fragile pattern — worth noting for future refactoring.
- The `RefreshDisplay` → `ShowRoom` → `RenderMapPanel` chain means `_currentFloor` MUST be set before `ShowRoom` is called, otherwise the map header shows the old floor.

**Build & Test Status:**
- ✅ `dotnet test --nologo -v q` — 1674/1674 passing
- ✅ Architecture test `Display_Should_Not_Depend_On_System_Console` — no `System.Console` refs in Display namespace
- ✅ Build: 0 errors, 0 warnings

#### BUG-17 (#1127) — Gear panel shows simplified stats, doesn't match gear command
- `RenderGearPanel` inner `AddSlot` replaced with the identical slot-aware logic from `ShowEquipment`: slot-type flags (`isWeapon`, `isAccessory`) drive colored stat parts (ATK red, DEF cyan, dodge yellow, mana blue, HP green), tier-colored item name, and set bonus appended at the bottom.
- Fix pattern: both `RenderGearPanel` and `ShowEquipment` now share the same `AddSlot` structure. If you need to change stat display logic, update **both** locations or extract to a shared helper.
- Key file: `Display/Spectre/SpectreLayoutDisplayService.cs` — `RenderGearPanel` (~line 435) and `ShowEquipment` (~line 864).

---

### 2026-03-05 — Deep Menu/Input Bug Analysis

**Task:** Investigate player-reported issue: opened inventory menu, could not cancel, Input panel became unresponsive.

**Analysis scope:** Read `SpectreLayoutDisplayService.Input.cs` line-by-line (739 lines) plus related files to trace full menu/input lifecycle.

**Files analyzed:**
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs` (739 lines)
- `Display/Spectre/SpectreLayoutDisplayService.cs` (main display service)
- `Display/Spectre/SpectreLayout.cs` (layout structure)
- `Engine/GameLoop.cs` (command loop)
- `Engine/Commands/InventoryCommandHandler.cs` (inventory command flow)

**Key findings — 13 bugs documented in `.ai-team/decisions/inbox/hill-menu-bug-analysis.md`:**

#### Critical Bugs (Break Menu Navigation)

**BUG-MENU-1: No Escape key handling in ContentPanelMenuNullable**
- Location: `ContentPanelMenuNullable<T>`, lines 599-612
- While loop only handles Up/Down/Enter. Escape and Q keys ignored.
- Loop never exits → subsequent keypresses consumed → Input panel never receives control.
- Fix: Add `case System.ConsoleKey.Escape: return null;`

**BUG-MENU-2: No Escape key handling in ContentPanelMenu**
- Location: `ContentPanelMenu<T>`, lines 562-575
- Same issue, affects non-nullable menus (difficulty, class, combat, shop).
- Player cannot cancel these menus via Escape, must select an option.

**BUG-MENU-3: No Escape key handling in ShowSkillTreeMenu**
- Location: `ShowSkillTreeMenu`, lines 438-446
- Inline key handling, also missing Escape case.

#### Content Panel State Bugs (UI Shows Wrong Content)

**BUG-MENU-4: Content panel not restored after menu exits**
- All menu methods call `SetContent()` which clears `_contentLines`.
- After menu exits, Content panel shows stale menu text instead of room narration.
- Fix: Save/restore `_contentLines`, `_contentHeader`, `_contentBorderColor` around menu calls.

**BUG-MENU-5: No explicit content refresh after menu methods**
- Command handlers call menu methods → menu mutates Content panel → control returns to RunLoop → Content never restored.
- Fix: Command handlers must call `ShowRoom()` or similar after menu-opening commands.

**BUG-MENU-11: Content not restored when Escape added**
- Once Escape is fixed, menus exit immediately but Content panel shows partial menu render.
- Fix: Same as BUG-MENU-4 — save/restore in finally block.

**BUG-MENU-12: Nested menu calls corrupt content state**
- Example: `ShowInventoryAndSelect` → `ShowItemDetail` → `ShowEquipmentComparison` — each calls `SetContent`.
- Fix: Same save/restore pattern, or use a menu-specific panel instead of Content.

**BUG-MENU-13: ReadCommandInput doesn't re-render after menu exits**
- `RunLoop` calls `ShowCommandPrompt()` then `ReadCommandInput()`. If a menu mutated Content panel, it's never restored.
- Fix: Add content refresh before or after `ReadCommandInput()` in RunLoop.

#### Minor/Edge-Case Bugs

**BUG-MENU-6: ReadCommandInput doesn't verify Live state**
- Directly calls `ReadKey(intercept: true)` without checking `_ctx.IsLiveActive`.
- If Live is stopped/paused unexpectedly, panel updates may fail.

**BUG-MENU-7: PauseAndRun can leave Live paused on exception**
- If exception occurs before try/finally, pause/resume events may be inconsistent.
- Current code looks correct but add logging/guards.

**BUG-MENU-8: No Q key handling for quick cancel**
- Many games support Q to cancel. Not implemented.
- Fix: Add `case System.ConsoleKey.Q: return null;` alongside Escape.

**BUG-MENU-9: Input panel cleared redundantly**
- `RunLoop` calls `ShowCommandPrompt()` then `ReadCommandInput()` which calls `UpdateCommandInputPanel("")`.
- Two panel updates for same purpose → potential flicker.

**BUG-MENU-10: No distinction between Escape vs Cancel option selection**
- Both return null, so analytics/logging can't distinguish quick cancel from navigated cancel.
- Fix: Return discriminated union or log before returning.

**Root Causes (3 categories):**
1. **Missing Escape/Q key handling** in all ContentPanelMenu* methods and ShowSkillTreeMenu.
2. **Content panel state not saved/restored** around menu operations.
3. **No explicit content refresh** after command handlers that open menus.

**Pattern insights:**
- `ContentPanelMenu` approach is correct for Live-active gameplay (avoids Spectre exclusivity lock), but implementation is incomplete.
- The while-true + ReadKey pattern requires explicit handling for ALL expected inputs, not just navigation keys.
- `SetContent()` is destructive (clears buffer) — any caller that needs to restore previous state must explicitly save it.
- The game loop expects `ShowCommandPrompt()` → `ReadCommandInput()` → command handler → loop — but menu commands break this by leaving Content panel in a mutated state.

**Recommended fix priorities:**
1. Add Escape/Q handling to all menu methods (BUG-MENU-1/2/3/8) — unblocks player.
2. Add content save/restore wrapper around menu calls (BUG-MENU-4/11) — fixes UI corruption.
3. Command handlers call `ShowRoom()` after menu-opening commands (BUG-MENU-5/13) — ensures content is current.

**Key file paths:**
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs` — all menu methods, ReadCommandInput, PauseAndRun
- `Engine/GameLoop.cs` — RunLoop (line 246), ReadCommandInput call (line 251)
- `Engine/Commands/InventoryCommandHandler.cs` — example of command handler that opens menu (line 7)

**Lessons for future menu implementation:**
- Always handle Escape and Q keys in any ReadKey navigation loop.
- Always save/restore display state (content, header, border) around destructive display operations.
- Menu methods should either: (1) restore state before returning, or (2) document that caller must refresh.
- Use finally blocks to ensure restoration even on exception/cancel.

### 2026-04-08 — UI Layout Bugs Fixed (#1143, #1144)

**Commit:** fc26b10 — `fix: limit Message Log to 12 displayed messages, enforce Command panel min size`

**Issues addressed:**

#### #1144 — Message Log shows too many messages
- `Display/Spectre/SpectreLayoutDisplayService.cs`:
  - Buffer size `MaxLogHistory = 50` controls how many messages are kept in memory
  - Display method `UpdateLogPanel()` was showing all 50 via `TakeLast(MaxLogHistory)`
- **Fix:** Added `MaxDisplayedLog = 12` constant; changed `UpdateLogPanel()` to use `TakeLast(MaxDisplayedLog)` instead
  - Buffer still retains 50 messages (for potential scrollback or debugging)
  - Panel now renders only the 12 most recent messages

#### #1143 — Command panel can collapse to unusable size
- `Display/Spectre/SpectreLayout.cs` line 60:
  - Input layout node: `new Layout(Panels.Input).Ratio(3)` allocates 30% of bottom row height
  - On small terminals, 30% could collapse to < 3 rows, making the Command panel unusable
- **Fix:** Added `.MinimumSize(4)` to Input layout: `new Layout(Panels.Input).Ratio(3).MinimumSize(4)`
  - In a `SplitRows()` context, `MinimumSize` controls height (rows), not width
  - Command panel is now always at least 4 rows tall, regardless of terminal size

**Key Learnings:**
- `MaxLogHistory` controls buffer size (memory), `MaxDisplayedLog` controls display size (UI) — separating these allows for large buffers without overwhelming the UI
- `MinimumSize()` in `SplitRows()` context controls row height (vertical), in `SplitColumns()` it would control column width (horizontal)
- Ratio-based layouts can collapse too small on tiny terminals — use `MinimumSize()` to enforce usability minimums


### 2026-06-14 — Merchant Menu Bugs Fixed (#1156, #1157, #1158, #1159)

**Commit:** c17cd7e — `fix: restore room view after sell/shop and fix escape key in menus`
**PR:** #1160

**Issues addressed:**

#### #1157 — Sell confirm menu persists after successful sell
- **Root cause:** `SellCommandHandler` didn't call `ShowRoom()` after sale/cancel, leaving confirm menu in content panel
- **Fix:** Added `ShowRoom(context.CurrentRoom)` at end of handler (after while loop exits)
  - Called on all normal exit paths: cancel at menu, no items to sell, successful sales
  - NOT called on error path (no merchant) — error paths preserve current display state

#### #1158 — SellCommandHandler should allow selling multiple items
- **Root cause:** Handler returned after first sale, forcing player to re-invoke `sell` command
- **Fix:** Wrapped entire flow in `while (true)` loop (matches `ShopCommandHandler` pattern)
  - Re-calculates `sellable` each iteration (inventory changes as items are sold)
  - Loop continues until: player selects Cancel (idx == 0) OR no sellable items remain
  - "Changed your mind" (cancel confirm) uses `continue` to re-show menu, not break

#### #1156 — ShopCommandHandler doesn't restore room view on Leave
- **Root cause:** When `shopChoice == 0` (Leave), handler returned without calling `ShowRoom()`
- **Fix:** Added `ShowRoom(context.CurrentRoom)` before return on Leave path (line 30)
  - NOT added to "no merchant" error path (same pattern as SellCommandHandler)

#### #1159 — ContentPanelMenu Escape returns selected item instead of cancel
- **Root cause:** `SpectreLayoutDisplayService.Input.cs` line 585: `case ConsoleKey.Escape: return items[selected].Value;`
- **Codebase convention:** Last item in every menu is always the cancel/leave/no option
- **Fix:** Changed to `return items[items.Count - 1].Value;` (returns last item, which is cancel)

**Test infrastructure added:**
- 8 new tests in `SellSystemTests.cs`:
  - `Sell_Success_CallsShowRoom` — validates ShowRoom called after sale
  - `Sell_Cancel_CallsShowRoom` — validates ShowRoom called when player cancels at menu (idx == 0)
  - `Sell_NoItems_CallsShowRoom` — validates ShowRoom called when inventory has no sellable items
  - `Sell_NoMerchant_DoesNotCallShowRoom` — validates ShowRoom NOT called on error path (no merchant)
  - `Sell_CanSellMultipleItems_InOneSession` — validates while loop allows selling 2+ items without re-invoking command
  - `Sell_AfterCancelConfirm_ContinuesLoop` — validates "Changed your mind" (No confirm) re-shows sell menu
  - `Shop_Leave_CallsShowRoom` — validates ShowRoom called when player selects Leave (shopChoice == 0)
  - `Shop_NoMerchant_Error_NoShowRoom` — validates ShowRoom NOT called on error path (no merchant)

**Test infrastructure updates:**
- `FakeDisplayService.cs`:
  - Added `ShowRoomCallCount` property to track ShowRoom() call frequency
  - Added `SellMenuSelectResponses`, `ConfirmMenuResponses`, `ShopMenuSelectResponses` queues for deterministic menu navigation in tests
  - `ShowSellMenuAndSelect` now dequeues from `SellMenuSelectResponses` if configured (was always reading from `_input`)

**Test bug fix:**
- `Sell_NoMerchant_DoesNotCallShowRoom` and `Shop_NoMerchant_Error_NoShowRoom` initially failed because they expected `ShowRoomCallCount == initialCount`
- **Issue:** `GameLoop.Run()` calls `RefreshDisplay()` at startup (line 170), which calls `ShowRoom()` once before command processing
- **Fix:** Changed assertions from `.Should().Be(initialShowRoomCount)` to `.Should().Be(initialShowRoomCount + 1)` to account for RefreshDisplay()'s ShowRoom call
- **Rationale:** Tests validate that *command handlers* don't call ShowRoom on error paths, but GameLoop's RefreshDisplay call is unavoidable and expected

## Learnings

**Convention: ALWAYS call ShowRoom() at end of every command handler to restore content panel**
- Command handlers that open menus (sell, shop, inventory, skills, equip, compare, etc.) mutate the content panel
- `ShowRoom()` restores the room view, ensuring content panel shows current state
- **Exception:** Error paths (no merchant, invalid input, etc.) should NOT call ShowRoom() — preserve current display state and show error

**ContentPanelMenu Escape/Q should return last item (cancel sentinel), not current selection**
- Codebase convention: Last menu item is ALWAYS the cancel/leave/no option
- Pressing Escape/Q should select cancel, regardless of current highlighted item
- Pattern applies to: sell menu (0 = Cancel), shop menu (0 = Leave), confirm menu (false = No), etc.

**SellCommandHandler now has a while(true) sell loop (like ShopCommandHandler)**
- Allows selling multiple items per `sell` command invocation (UX improvement)
- Loop exits when: player cancels (idx == 0) OR inventory has no sellable items
- "Changed your mind" (No at confirm) continues loop (re-shows sell menu) — doesn't break

**Test pattern: RefreshDisplay() call in GameLoop.Run() affects ShowRoomCallCount**
- `GameLoop.Run()` calls `RefreshDisplay()` at line 170 (before command loop starts)
- `RefreshDisplay()` calls `ShowRoom()`, incrementing `ShowRoomCallCount` by 1
- Tests that measure ShowRoom frequency must account for this initial call
- Pattern: `initialCount = display.ShowRoomCallCount; loop.Run(); display.ShowRoomCallCount.Should().Be(initialCount + 1);` for "no extra ShowRoom" assertions

**Key file paths:**
- `Engine/Commands/SellCommandHandler.cs` — sell command with multi-sell loop
- `Engine/Commands/ShopCommandHandler.cs` — shop command (calls SellCommandHandler on shopChoice == -1)
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs` — ContentPanelMenu (Escape/Q handling)
- `Dungnz.Tests/SellSystemTests.cs` — sell/shop command tests
- `Dungnz.Tests/Helpers/FakeDisplayService.cs` — test double for IDisplayService

### 2026-04-08 — Menu Input Bug Fixes (#1129–#1135)

**PR:** #1142 — `fix: Escape key handling, remove Console.ReadLine fallback, null key guard`
**Branch:** `squad/1129-1130-1133-1135-menu-fixes`

**Issues addressed:**

#### #1129 — ReadCommandInput null falls through to Console.ReadLine
- `GameLoop.cs` line 251: When `ReadCommandInput()` returned null (empty Enter), it fell through to `_input.ReadLine()` which called `Console.ReadLine()` OUTSIDE the Spectre Live layout, corrupting the terminal
- **Fix:** Changed `var input = _display.ReadCommandInput() ?? _input.ReadLine() ?? string.Empty;` to `var input = _display.ReadCommandInput() ?? string.Empty;` — if ReadCommandInput returns null, treat it as empty string, no fallback

#### #1130 — No Escape key handling in ContentPanelMenu methods
- `SpectreLayoutDisplayService.Input.cs`: Three menu methods had no Escape key handling — pressing Escape in a menu consumed keys but loop never exited
- **Fixes:**
  - `ContentPanelMenuNullable<T>` (line 599): Added `case ConsoleKey.Escape:` and `case ConsoleKey.Q:` returning `null`
  - `ContentPanelMenu<T>` (line 564): Added `case ConsoleKey.Escape:` and `case ConsoleKey.Q:` returning `items[selected].Value` (non-nullable, so return current item)
  - `ShowSkillTreeMenu` (line 438): Added `case ConsoleKey.Escape:` and `case ConsoleKey.Q:` returning `null`

#### #1133 — PauseAndRun uses Thread.Sleep(100) instead of synchronization
- `SpectreLayoutDisplayService.Input.cs` line 531: Used `Thread.Sleep(100)` as a brief timing buffer after signaling pause event
- **Assessment:** This is a timing buffer, not a correctness mechanism — the pause signal is event-based via `_pauseLiveEvent.Set()`
- **Fix:** Documented purpose with comment: "Brief wait to allow Live loop to observe pause signal (#1133) / Thread.Sleep is acceptable here as it's a timing buffer, not correctness"

#### #1135 — ContentPanelMenu returns first item on null ReadKey
- `SpectreLayoutDisplayService.Input.cs` line 600 / 564: If `AnsiConsole.Console.Input.ReadKey()` returned null (can happen in some terminals), `if (key == null) return items[selected].Value;` would return first item instead of handling gracefully
- **Fix:** Changed to `if (key == null) continue;` in both `ContentPanelMenu<T>` and `ContentPanelMenuNullable<T>` — skip null keys, keep loop running

**Build & Test Status:**
- ✅ `dotnet build --nologo` — 0 errors, 0 warnings
- ✅ `dotnet test --nologo -v q` — 1406 passed, 0 failed

**Key Learnings:**
- `Console.ReadLine()` breaks Spectre.Console's Live display by writing/reading outside the managed terminal context
- Escape key (and Q for terminals that don't reliably send Escape) should always be handled in menu loops as a cancel/exit option
- Nullable vs. non-nullable menu methods: nullable can return null on Escape, non-nullable should return current selection
- `ReadKey()` can return null in non-interactive or redirected input scenarios — always guard with null check before accessing `.Key`

---

### 2026-03-05 — TUI Usability Fixes (#1036–#1044)

**PR:** #1045 — `fix: TUI usability — contrast, auto-populating panels, color system, skill tree`
**Branch:** `squad/1036-tui-usability-fixes`

**Issues addressed (all 9 — triaged by Coulson):**

#### #1036 — No ColorScheme on any TUI panel
- `TuiLayout.cs`: Defined 5 high-contrast `ColorScheme` objects (normal/map/stats/log/input)
- Applied to all panels: bright green on black for Map, bright cyan on black for Stats, white on blue for content, bright yellow on black for command input
- Used `Terminal.Gui.Color` enum values — no `BrightWhite` exists in v1.19, used `Color.White` instead
- Added `MakeAttr()` private helper with null-guard on `Application.Driver` so tests (which don't call `Application.Init()`) don't NullReferenceException

#### #1042 — SetMap/SetStats destroy and recreate child views
- `TuiLayout.cs`: Added private `_mapView` and `_statsView` TextViews created once in constructor
- `SetMap()` and `SetStats()` now just update `.Text` property — no `RemoveAll()` + `new TextView` churn

#### #1038 + #1039 — Map and Stats panels blank on room entry
- `TerminalGuiDisplayService.cs`: Added `_player`, `_currentRoom`, `_currentFloor` fields
- `ShowPlayerStats(player)` caches `_player`; `ShowMap(room, floor)` caches `_currentRoom` / `_currentFloor`
- `ShowRoom(room)` now calls `BuildAsciiMap` and `_layout.SetMap()` automatically after rendering the room description; also calls `BuildStatsText(_player)` and `_layout.SetStats()` if player is cached
- Extracted `BuildStatsText(Player)` as a private static helper (reused by both `ShowPlayerStats` and the auto-refresh in `ShowRoom`)

#### #1037 — TuiColorMapper never called, ShowColored* ignores color
- `ShowColoredMessage(message, color)`: Now calls `TuiColorMapper.MapAnsiToTuiColor(color)` and maps the result to a log type (error/loot/info) — message appears in the log with appropriate prefix icon
- `ShowColoredCombatMessage(message, color)`: Routes to log with type `"combat"` so it gets the ⚔ prefix
- Terminal.Gui TextViews still don't support inline ANSI; color distinction is via log message type

#### #1041 — BuildColoredHpBar/MpBar dead code (barChar computed but unused)
- Fixed `BuildColoredHpBar`: `barChar` is now a `char` and `new string(barChar, filled)` uses it properly
- Fixed `BuildColoredMpBar`: same pattern — bar density reflects mana percentage (`█`/`▓`/`▒`)

#### #1040 — ShowSkillTreeMenu returns null unconditionally
- Implemented using `TuiMenuDialog<Skill?>`: lists all `Skill` enum values not yet unlocked by the player, plus a Cancel option. Returns selected skill or null.

#### #1043 — Race condition: InvokeOnUiThread drops early calls
- `GameThreadBridge.cs`: Added `static ManualResetEventSlim _uiReady`
- Added `static SetUiReady()` method that sets the event
- `InvokeOnUiThread()` now waits up to 5 s for `_uiReady` when `MainLoop` is null before falling through
- `Program.cs`: `layout.MainWindow.Loaded += () => GameThreadBridge.SetUiReady()` — fires after first Application.Run tick

#### #1044 — TUI-ARCHITECTURE.md describes non-existent API
- Rewrote `docs/TUI-ARCHITECTURE.md` to match actual implementation:
  - Replaced `ConcurrentQueue`/`FlushMessages`/`EnqueueCommand` fiction with `BlockingCollection`, `InvokeOnUiThread`, `Application.MainLoop.Invoke()`
  - Added `ManualResetEventSlim` / `SetUiReady` documentation
  - Added panel color table and auto-population notes
  - Corrected initialization sequence (5 steps → 9 steps)

**Build & Test Status:**
- ✅ `dotnet build --nologo -v q` — 0 errors, 0 warnings
- ✅ `dotnet test --nologo` — 1785/1785 passing

**Key Learnings:**
- Terminal.Gui v1.19 `Color` enum: no `BrightWhite` — use `Color.White`. Available bright variants: BrightBlue, BrightCyan, BrightGreen, BrightMagenta, BrightRed, BrightYellow
- `Application.Driver` is null before `Application.Init()` — guard with null-check when used in constructors that tests instantiate directly
- `new string(char, count)` not `new string(string, count)` — C# string repeat takes a `char`, not a `string`
- `Terminal.Gui.Attribute` conflicts with `System.Attribute` — use fully qualified name `Terminal.Gui.Attribute` when both namespaces are in scope



**PRs:** #965, #966, #967

**Issues addressed:**

#### #928 — GameLoop null! field initialization risk (PR #965)
**Branch:** `squad/928-gameloop-null-safety`
- `_player`, `_currentRoom`, `_stats`, `_context` declared with `null!` and only set in `Run()`
- Constructor accepted `display` and `combat` without null-checking despite non-nullable type
- `ExitRun()` compared `_context != null!` — syntactically confusing (null-forgiving in comparison)
- **Fix:** Added `ArgumentNullException.ThrowIfNull()` for `display`/`combat` in constructor; added same for `state.Player`/`state.CurrentRoom` in `Run(GameState)`; replaced `null!` comparison with `is not null`

#### #929 — Silent exception swallowing in PrestigeSystem (PR #966)
**Branch:** `squad/929-fix-silent-exceptions`
- `PrestigeSystem.Load()` used bare `catch { return new PrestigeData(); }` — no trace, no log
- `PrestigeSystem.Save()` used `catch { /* silently fail */ }` — prestige data loss with zero feedback
- `SaveSystem.SaveGame()` was already correct (re-throws after cleanup); `LoadGame()` already wraps as `InvalidDataException`
- **Fix:** Both catch blocks now capture `Exception ex` and call `Trace.TraceError()` with context+message. Non-crashing by design, but now observable via any configured trace listener

#### #930 — Console.WriteLine in Systems layer (PR #967)
**Branch:** `squad/930-remove-console-in-systems`
- `PrestigeSystem.Load()` called `Console.WriteLine()` for a version mismatch warning — the only offending Console.* call in Engine/ and Systems/
- **Fix:** Replaced with `Trace.TraceWarning()`. PrestigeSystem is static with no DI, so Trace is the right diagnostic channel

**Key Learnings:**
- Static systems without DI should use `System.Diagnostics.Trace` for diagnostics, not `Console.*`
- `null!` (null-forgiving) is for suppressing nullable warnings — never use it in comparisons; use `is not null` instead
- Bare `catch { }` is always wrong unless intentional; always capture `Exception ex` and trace/log it

### 2026-03-04 — Bug and Quality Scan (#868)

**Task:** Thorough scan of Engine/, Models/, and Program.cs for bugs and quality risks.

**Findings (20 issues identified):**

| Severity | Count | Key Issues |
|----------|-------|-----------|
| HIGH | 2 | Unvalidated fuzzy-match argument; Duplicated flee-state reset code |
| MED | 7 | Null checks, edge cases, parameter typo, hardcoded dimensions, bounds checks |
| LOW | 11 | Resource cleanup, magic numbers, type-system confidence, event handler leaks |

**Top Patterns to Address:**

1. **Duplicate flee-state reset** (CombatEngine.cs lines 436–490)
   - Nearly identical 50-line code blocks; prone to divergence
   - Fix: Extract `ResetFleeState(Player, Enemy)` helper

2. **Hardcoded magic numbers** (DungeonGenerator.cs, GameLoop.cs)
   - `width = 5, height = 4` and `FinalFloor = 8` scattered across logic
   - Fix: Extract to `const` fields; centralize floor-scaling rules

3. **Missing bounds checks** (DungeonGenerator.cs lines 193, 287)
   - `eligibleRooms[specialIdx++]` without guard; room description pool access
   - Fix: Guard before indexing; fallback descriptions

4. **Mutable collection exposure** (Room.cs line 101)
   - `Items` is public List; external code can mutate during iteration
   - Fix: Return `IReadOnlyList<Item>` or expose copy

5. **Event handler memory leak vector**
   - `OnHealthChanged?.Invoke()` never unsubscribed
   - Fix: Document event lifetime; consider weak-event pattern

**Files to Review for Fixes:**
- Engine/CombatEngine.cs (duplicate code, event leaks)
- Engine/DungeonGenerator.cs (magic numbers, bounds checks)
- Models/Room.cs (collection exposure)
- Models/PlayerStats.cs (event cleanup)
- Engine/GameLoop.cs (exit path cleanup, hardcoded constants)

**Quality Assessment:** Code is defensive and well-structured overall. Most issues are maintainability debt (hardcoded values, duplicate code) or edge-case risks (bounds checks, null guards). No critical runtime bugs detected, but the patterns compound risk as codebase grows.

---

## Learnings

### 2026-03-03 — GameLoop Decomposition to ICommandHandler Pattern (#868)

**PR:** #889 — `refactor: decompose GameLoop into ICommandHandler pattern`  
**Branch:** `squad/868-gameloop-decomposition`

**Problem:**
- GameLoop.cs had grown to 1,635 lines, difficult to maintain and extend
- Multiple command handling logic mixed together
- Hard to add new command types or modify existing ones
- Violated single responsibility principle

**Solution:**
- Decomposed GameLoop into ICommandHandler pattern
- Created `Engine/Commands/` directory with 23 handler classes:
  - Each command type has its own handler (e.g., AttackHandler, HealHandler, UseItemHandler)
- Created CommandContext class to hold mutable run state:
  - Player current HP/MP/position
  - Combat state flags
  - Inventory state
- GameLoop.cs reduced to 741 lines (45% reduction)
- Each handler implements ICommandHandler interface with Execute(CommandContext) method
- Handlers are registered in a CommandFactory/Registry pattern

**Architecture:**
- CommandContext holds all mutable run state (replaces scattered local variables)
- Each handler focuses on single command execution
- Easy to add new commands without modifying GameLoop
- Testable: handlers can be unit tested independently with CommandContext

**Testing:**
- ✅ All 1,422 tests passing
- ✅ Game starts and plays normally
- ✅ All command types still work identically

**Key Learning:**
- ICommandHandler pattern scales better than monolithic Game/GameLoop classes
- CommandContext makes state explicit and testable
- 23 focused handlers easier to maintain than one 1,635-line method

---

### 2026-03-03 — Schema Validation Fix (#849)

**PR:** #850 — `fix: repair invalid items in item-stats.json`  
**Branch:** `squad/849-fix-item-stats-schema`  
**File:** `Data/schemas/item-stats.schema.json` only

**Problem:**
- Game crashed on startup with schema validation error
- Error: `System.IO.InvalidDataException: Schema validation failed for Data/item-stats.json`
- Affected items at indices: 50, 77, 78, 79, 80, 81, 82, 83, 97 (all crafting materials)
- Validation reported: `ArrayItemNotValid` for each of these items

**Root Cause:**
- The JSON schema was missing property definitions for 4 fields that exist in all items:
  - `StatModifier` (integer)
  - `Description` (string)  
  - `Weight` (number)
  - `SellPrice` (integer)
- JSON Schema validation by default rejects properties not defined in the schema
- All items in item-stats.json have these properties, but the schema didn't declare them
- This caused validation to fail when StartupValidator ran its schema checks

**Fix:**
- Added missing property definitions to `Data/schemas/item-stats.schema.json`:
  - `"StatModifier": { "type": "integer" }`
  - `"Description": { "type": "string" }`
  - `"Weight": { "type": "number", "minimum": 0 }`
  - `"SellPrice": { "type": "integer", "minimum": 0 }`
- No changes to item-stats.json data file needed — it was already correct
- Schema now matches the actual structure of items in the data file

**Testing:**
- ✅ `dotnet build` succeeds
- ✅ Game starts without validation errors
- ✅ StartupValidator.ValidateOrThrow() passes
- Confirmed by running game — title screen appears (previously crashed immediately)

**Key Learning:**
- StartupValidator in `Systems/StartupValidator.cs` validates all data files against schemas at startup
- Schema validation is strict by default — all properties must be declared
- When schema validation fails, error messages show indices (0-based) and error kind
- Use `jq '.Items[N]'` to inspect specific items by index in large JSON files
- Always test both build AND runtime startup after schema changes

---

### 2026-03-02 — Emoji Restoration (#832)

**PR:** #833 — `fix: restore visual emojis, replace 🛡 with 🦺 for Chest alignment`  
**Branch:** `squad/832-restore-visual-emojis`  
**File:** `Display/SpectreDisplayService.cs` only

**What:**
- PR #830 replaced all emojis with narrow Unicode symbols to fix an alignment bug. The only ACTUALLY broken emoji was 🛡 (U+1F6E1, SHIELD) — EAW=N but not in NarrowEmoji, so it got 1 space instead of 2.
- Restored all original wide emojis (💍🪖🥋🧤👖👟🧥⭐✨🏃🧪).
- Replaced 🛡 with 🦺 (safety vest, U+1F9BA, EAW=W) for Chest and Armor icon — this is the real fix.
- Replaced `IL()` helper with `EL()` that uses a `NarrowEmoji` HashSet to decide spacing: narrow symbols get 2 spaces, wide emojis get 1 space.

**Key learning — EAW and terminal alignment:**
- EAW=W (wide) emojis occupy 2 terminal columns → use 1 space after = 3 columns total
- EAW=N (narrow) symbols occupy 1 terminal column → use 2 spaces after = 3 columns total
- The NarrowEmoji set: `["⚔", "⛨", "⚗", "☠", "★", "↩", "•"]`
- ✦ (U+2736) is narrow but only used in Combo row (not an equipment slot) — acceptable
- Never add 🛡 to the emoji set; 🦺 is the permanent replacement

**Build note:** `dotnet build Dungnz.csproj` (without `-q`, without `--no-restore`) works when the incremental build cache is in a bad state. `dotnet build -q --no-restore` may fail with MSB3492/GenerateTargetFrameworkMonikerAttribute — this is a pre-existing SDK quirk, not a code error.

---

## 2026-03-06: Fixed missing ShowRoom() calls in command handlers

**Issue:** Deep menu audit found that many command handlers display content in the panel but never call `ShowRoom()` afterward, leaving stale content visible until the player types `LOOK`.

**Fixed handlers:**
- `InventoryCommandHandler` — add `ShowRoom()` after item selection (item detail + comparison path)
- `UseCommandHandler` — add `ShowRoom()` at end when turn consumed (after using item from menu)
- `CompareCommandHandler` — add `ShowRoom()` after showing comparison
- `ExamineCommandHandler` — add `ShowRoom()` after item detail/comparison (both room item and inventory item paths)
- `StatsCommandHandler` — add `ShowRoom()` after stats display
- `MapCommandHandler` — add `ShowRoom()` after map update
- `HelpCommandHandler` — add `ShowRoom()` after help display
- `EquipmentCommandHandler` — add `ShowRoom()` after equipment display

**Pattern established:**
Every handler that sets content panel content MUST call `ShowRoom()` before returning (unless already called on cancel path). This is now the established convention — future handlers must follow this pattern.

**Testing:** All 1695 tests pass.
**Commit:** c72dbe8 on branch `scribe/log-merchant-menu-2026-03-06`
**Issues closed:** #1168, #1169, #1170, #1171, #1172, #1175

## 2026-03-10: WI-B + WI-E — MomentumResource model layer + display (#1274)

**Branch:** `squad/1274-momentum-model-display` | **PR:** #1293

### Changes

**WI-B — Model layer:**
- Created `Dungnz.Models/MomentumResource.cs` — sealed class with `Current`, `Maximum`, `IsCharged`, `Add(int)`, `Reset()`. Validates `maximum > 0` with `ArgumentOutOfRangeException`.
- Added `public MomentumResource? Momentum { get; set; }` to `Dungnz.Models/Player.cs` (in the passives section alongside `BattleHardenedStacks`). Nullable — Rogue stays on ComboPoints, null for unsupported classes.
- Added `Momentum?.Reset()` to `ResetCombatPassives()` in `Player.cs`.
- **Initialization deferred to CombatEngine (Barton)** — consistent with how `BattleHardenedStacks` works: no constructor needed, `Momentum` starts null and CombatEngine sets `new MomentumResource(max)` per class on combat start.

**WI-E — Display layer:**
- Updated `RenderStatsPanel()` in `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs` (~line 431).
- Added block below existing Rogue combo display: checks `player.Momentum is { } momentum`.
- Label switch: Warrior="Fury", Mage="Charge", Paladin="Devotion", Ranger="Focus", _="Momentum".
- Dot bar: `new string('●', current) + new string('○', max - current)`.
- Charged suffix: `" [bold cyan][CHARGED][/]"` when `IsCharged`.

### Learnings

**MomentumResource initialization pattern:**
- Follow BattleHardenedStacks pattern: property starts null/0 on model, CombatEngine owns per-class initialization at combat start. Do NOT add a Player constructor or per-class initialization in the model layer.
- Per-class max values: Warrior=5, Mage=3, Paladin=4, Ranger=3, Rogue=null (ComboPoints).

**Display pattern for class resource bars:**
- Template established: `[yellow]✦ {Label}[/] {dots}{chargedSuffix}` in `RenderStatsPanel()`.
- Rogue combo is rendered only when `ComboPoints > 0`; Momentum is rendered whenever `Momentum is not null` (even at 0 so player sees the bar immediately on entering combat).
- `[bold cyan][CHARGED][/]` is the charged color convention.

**Key files:**
- `Dungnz.Models/MomentumResource.cs` — new sealed class
- `Dungnz.Models/Player.cs` — Momentum property + ResetCombatPassives()
- `Dungnz.Display/Spectre/SpectreLayoutDisplayService.cs` — RenderStatsPanel() momentum display block
