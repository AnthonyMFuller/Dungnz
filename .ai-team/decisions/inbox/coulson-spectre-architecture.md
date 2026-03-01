### 2025-03-01: Spectre.Console migration architecture
**By:** Coulson
**What:** Side-by-side SpectreDisplayService implementation swappable via constructor DI; IDisplayService remains the stable contract for future Blazor compatibility.
**Why:** Boss approved Option 1 Spectre.Console upgrade; future Option 3 (Blazor) must remain viable.

---

## Section 1: Architecture Decisions

1. **Side-by-side implementation, not direct replacement**: Create `SpectreDisplayService : IDisplayService` alongside existing `ConsoleDisplayService`. DI registration in `Program.cs` determines which implementation is used. This allows rollback and A/B testing.

2. **IDisplayService is the seam — no Spectre leakage**: All Spectre.Console calls stay inside `SpectreDisplayService`. Game logic (GameLoop, CombatEngine, etc.) continues to depend only on `IDisplayService`. This preserves Blazor viability.

3. **IMenuNavigator replaced by Spectre SelectionPrompt internally**: `SpectreDisplayService` does NOT use `ConsoleMenuNavigator`. Instead, interactive menus (`Show*AndSelect` methods) delegate to `SelectionPrompt<T>`. The `IMenuNavigator` abstraction can be phased out once migration is complete.

4. **NuGet reference added to main csproj only**: Add `Spectre.Console` (latest stable, currently 0.50.0) to `Dungnz.csproj`. No separate Display project needed — the csproj already contains the Display layer.

5. **Feature flags for gradual rollout**: Add `bool UseSpectreConsole` app setting (default `false` initially) to swap implementations without code changes. Can be deleted post-migration.

---

## Section 2: Migration Strategy

### Approach: Incremental method-by-method migration

1. **Create SpectreDisplayService skeleton** implementing `IDisplayService` with all methods throwing `NotImplementedException`.

2. **Migrate methods in visual-category batches** — each batch is one issue, each results in a testable deliverable:
   - Batch A: Title/Intro screens (low risk, high visibility)
   - Batch B: Menus/Selection prompts (high value — replaces ConsoleMenuNavigator)
   - Batch C: Combat UI (stat bars, HP displays)
   - Batch D: Room/Map navigation
   - Batch E: Inventory/Shop/Loot cards
   - Batch F: Endgame screens (Victory/GameOver)

3. **Each method implementation**:
   - Use Spectre primitives (`Table`, `Panel`, `Rule`, `Markup`, `BarChart`, `SelectionPrompt`)
   - Preserve visual intent (colors, layout, box-drawing feel)
   - Run manual smoke test after each batch

4. **Swap default implementation** once all methods pass smoke tests.

5. **Deprecate ConsoleDisplayService** (keep for reference; mark obsolete).

---

## Section 3: GitHub Issue Breakdown

### Issue 1: Add Spectre.Console NuGet reference and SpectreDisplayService skeleton
**Assigned:** Hill
**Labels:** `enhancement`, `display`, `spectre-migration`

```markdown
## Summary
Add the Spectre.Console NuGet package and create the initial `SpectreDisplayService` skeleton.

## Tasks
- [ ] Add `Spectre.Console` (latest stable) to `Dungnz.csproj`
- [ ] Create `Display/SpectreDisplayService.cs` implementing `IDisplayService`
- [ ] All methods throw `NotImplementedException` initially
- [ ] Add DI registration in `Program.cs` with feature-flag switch (`--use-spectre` CLI arg or env var)
- [ ] Ensure project builds with no warnings

## Acceptance Criteria
- `dotnet build` succeeds
- Running with `--use-spectre` instantiates `SpectreDisplayService`
- Running without flag uses existing `ConsoleDisplayService`
```

---

### Issue 2: Migrate Title/Intro screens to Spectre
**Assigned:** Hill
**Labels:** `enhancement`, `display`, `spectre-migration`

```markdown
## Summary
Implement Spectre versions of title and introduction display methods.

## Methods to migrate
- `ShowTitle()`
- `ShowEnhancedTitle()`
- `ShowIntroNarrative()`
- `ShowPrestigeInfo(PrestigeData)`

## Implementation notes
- Use `FigletText` for title banner
- Use `Panel` with border styles for prestige info box
- Use `Markup` for colored narrative text

## Acceptance Criteria
- All four methods render without exception
- Visual appearance matches or improves on current output
- No ANSI escape codes in SpectreDisplayService (use Spectre markup only)
```

---

### Issue 3: Migrate selection menus to Spectre SelectionPrompt
**Assigned:** Hill
**Labels:** `enhancement`, `display`, `spectre-migration`, `priority:high`

```markdown
## Summary
Replace arrow-key menu logic with Spectre's `SelectionPrompt<T>`. This is the highest-value migration item.

## Methods to migrate
- `SelectDifficulty()`
- `SelectClass(PrestigeData?)`
- `ShowConfirmMenu(string)`
- `ShowLevelUpChoiceAndSelect(Player)`
- `ShowCombatMenuAndSelect(Player, Enemy)`
- `ShowCraftMenuAndSelect(IEnumerable<...>)`
- `ShowShrineMenuAndSelect(...)`
- `ShowForgottenShrineMenuAndSelect()`
- `ShowContestedArmoryMenuAndSelect(int)`
- `ShowTrapChoiceAndSelect(...)`
- `ShowAbilityMenuAndSelect(...)`

## Implementation notes
- Use `SelectionPrompt<T>` with custom formatters
- Preserve color coding in selection labels using Spectre markup `[red]...[/]`
- Handle fallback for redirected stdin (Spectre handles this natively)

## Acceptance Criteria
- All menu methods work with arrow-key navigation
- Menu renders with proper colors and labels
- Enter confirms, Escape/Ctrl+C cancels where applicable
```

---

### Issue 4: Migrate Combat UI to Spectre
**Assigned:** Hill
**Labels:** `enhancement`, `display`, `spectre-migration`

```markdown
## Summary
Implement combat display methods using Spectre Tables and Markup.

## Methods to migrate
- `ShowCombat(string)`
- `ShowCombatStatus(Player, Enemy, ...)`
- `ShowCombatMessage(string)`
- `ShowColoredCombatMessage(string, string)`
- `ShowCombatStart(Enemy)`
- `ShowCombatEntryFlags(Enemy)`
- `ShowEnemyArt(Enemy)`
- `ShowEnemyDetail(Enemy)`

## Implementation notes
- Use `Table` for side-by-side HP bars
- Use `Panel` for enemy art display
- Use `BarChart` or custom progress bar for HP visualization
- Use `Rule` for combat start divider

## Acceptance Criteria
- Combat sequence renders correctly
- HP bars update visually
- Enemy art displays in bordered panel
```

---

### Issue 5: Migrate Room/Map/Navigation UI to Spectre
**Assigned:** Hill
**Labels:** `enhancement`, `display`, `spectre-migration`

```markdown
## Summary
Implement room description and map display using Spectre primitives.

## Methods to migrate
- `ShowRoom(Room)`
- `ShowMap(Room)`
- `ShowFloorBanner(int, int, DungeonVariant)`
- `ShowCommandPrompt(Player?)`
- `ShowMessage(string)`
- `ShowError(string)`
- `ShowColoredMessage(string, string)`
- `ShowHelp()`
- `ReadPlayerName()`

## Implementation notes
- Use `TextPrompt<string>` for name input
- Use `Markup` for room descriptions
- Use `Table` or `Canvas` for ASCII map (preserve existing BFS logic)
- Use `Panel` for floor banner

## Acceptance Criteria
- Room navigation works end-to-end
- Map renders correctly with fog-of-war
- Error messages display in red
```

---

### Issue 6: Migrate Inventory/Shop/Loot UI to Spectre
**Assigned:** Hill
**Labels:** `enhancement`, `display`, `spectre-migration`

```markdown
## Summary
Implement inventory, shop, and loot displays using Spectre Tables and Panels.

## Methods to migrate
- `ShowPlayerStats(Player)`
- `ShowInventory(Player)`
- `ShowLootDrop(Item, Player, bool)`
- `ShowGoldPickup(int, int)`
- `ShowItemPickup(Item, ...)`
- `ShowItemDetail(Item)`
- `ShowEquipmentComparison(Player, Item?, Item)`
- `ShowColoredStat(string, string, string)`
- `ShowShop(IEnumerable<...>, int)`
- `ShowShopAndSelect(...)`
- `ShowShopWithSellAndSelect(...)`
- `ShowSellMenu(...)`
- `ShowSellMenuAndSelect(...)`
- `ShowCraftRecipe(...)`
- `ShowCombatItemMenuAndSelect(...)`
- `ShowEquipMenuAndSelect(...)`
- `ShowUseMenuAndSelect(...)`
- `ShowTakeMenuAndSelect(...)`

## Implementation notes
- Use `Table` for inventory grid
- Use `Panel` for item cards
- Use `SelectionPrompt<Item>` for item selection menus
- Color-code prices (green = affordable, red = too expensive)

## Acceptance Criteria
- Inventory displays correctly
- Shop buying/selling works
- Loot drops render with tier colors
```

---

### Issue 7: Migrate Endgame screens and finalize swap
**Assigned:** Hill
**Labels:** `enhancement`, `display`, `spectre-migration`

```markdown
## Summary
Implement victory/game-over screens and make SpectreDisplayService the default.

## Methods to migrate
- `ShowVictory(Player, int, RunStats)`
- `ShowGameOver(Player, string?, RunStats)`
- `ShowLevelUpChoice(Player)` (display-only version)

## Final tasks
- [ ] Remove all `NotImplementedException` calls
- [ ] Set `--use-spectre` as default (or remove flag)
- [ ] Mark `ConsoleDisplayService` as `[Obsolete]`
- [ ] Update README with Spectre.Console dependency note

## Acceptance Criteria
- Full game playable with SpectreDisplayService
- No regressions in visual output
- ConsoleDisplayService still available for fallback
```

---

## Section 4: Architectural Notes for decisions.md

### New decisions to capture:

1. **D-SPECTRE-001: SpectreDisplayService is the Spectre.Console adapter** — All Spectre.Console usage is encapsulated in `SpectreDisplayService`. No other class may reference Spectre directly.

2. **D-SPECTRE-002: IDisplayService is version-stable** — The interface signature is frozen during migration. Any new display needs must be additive (new methods), not breaking changes.

3. **D-SPECTRE-003: Blazor compatibility preserved** — A future `BlazorDisplayService : IDisplayService` remains viable. The interface uses only CLR primitives and model types — no Console or Spectre types in signatures.

4. **D-SPECTRE-004: ConsoleMenuNavigator deprecation** — Once SpectreDisplayService is default, `IMenuNavigator` and `ConsoleMenuNavigator` are obsolete. Menu navigation is an internal implementation detail of each display service.
