# Avalonia UI Migration Spec

**Author:** Coulson (Lead Architect)  
**Date:** 2026-03-13  
**Status:** Approved by Anthony  
**Supersedes:** Terminal.Gui TUI experiment (deleted)

---

## Section 1: IDisplayService Audit

The current `IDisplayService` has **58 methods**. Grouped by concern:

### Output-Only (void, no user input)

These methods push content to the display and return nothing meaningful. They are pure fire-and-forget from the game thread's perspective.

| # | Method | What it renders |
|---|--------|----------------|
| 1 | `ShowTitle()` | ASCII title banner |
| 2 | `ShowRoom(Room)` | Room description, exits, enemies, items |
| 3 | `ShowCombat(string)` | Combat headline ("A Goblin attacks!") |
| 4 | `ShowCombatStatus(Player, Enemy, effects, effects)` | HP bars for both combatants |
| 5 | `ShowCombatMessage(string)` | Single combat narrative line |
| 6 | `ShowPlayerStats(Player)` | Full player stat card |
| 7 | `ShowInventory(Player)` | Inventory item list |
| 8 | `ShowLootDrop(Item, Player, bool)` | Loot drop card |
| 9 | `ShowGoldPickup(int, int)` | Gold notification |
| 10 | `ShowItemPickup(Item, int, int, int, int)` | Item pickup confirmation |
| 11 | `ShowItemDetail(Item)` | Full item stat card (EXAMINE) |
| 12 | `ShowMessage(string)` | General informational text |
| 13 | `ShowError(string)` | Error/warning message |
| 14 | `ShowHelp()` | Command reference |
| 15 | `ShowCommandPrompt(Player?)` | Input prompt symbol |
| 16 | `ShowMap(Room, int)` | BFS ASCII mini-map |
| 17 | `ShowColoredMessage(string, string)` | Color-coded message |
| 18 | `ShowColoredCombatMessage(string, string)` | Color-coded combat line |
| 19 | `ShowColoredStat(string, string, string)` | Color-coded stat pair |
| 20 | `ShowEquipmentComparison(Player, Item?, Item)` | Before/after stat delta |
| 21 | `ShowEquipment(Player)` | Equipped items table |
| 22 | `ShowEnhancedTitle()` | Enhanced ASCII title with colors |
| 23 | `ShowPrestigeInfo(PrestigeData)` | Prestige level card |
| 24 | `ShowShop(stock, int)` | Shop item cards |
| 25 | `ShowSellMenu(items, int)` | Sell menu display |
| 26 | `ShowCraftRecipe(string, Item, ingredients)` | Recipe card |
| 27 | `ShowCombatStart(Enemy)` | Combat start banner |
| 28 | `ShowCombatEntryFlags(Enemy)` | Elite/special flags |
| 29 | `ShowLevelUpChoice(Player)` | Level-up stat options (display only) |
| 30 | `ShowFloorBanner(int, int, DungeonVariant)` | Floor transition banner |
| 31 | `ShowEnemyDetail(Enemy)` | Enemy stat card |
| 32 | `ShowVictory(Player, int, RunStats)` | Victory screen |
| 33 | `ShowGameOver(Player, string?, RunStats)` | Death screen |
| 34 | `ShowEnemyArt(Enemy)` | ASCII art box |
| 35 | `ShowCombatHistory()` | Full combat log dump |
| 36 | `RefreshDisplay(Player, Room, int)` | Atomic panel refresh |
| 37 | `UpdateCooldownDisplay(cooldowns)` | Cooldown HUD update |

**Count: 37 output-only methods.**

### Input-Coupled (must pause game thread, collect input, return result)

These methods block the calling (game) thread until the player makes a choice.

| # | Method | Returns | Input type |
|---|--------|---------|------------|
| 1 | `ReadCommandInput()` | `string?` | Free text entry |
| 2 | `ReadPlayerName()` | `string` | Free text entry |
| 3 | `ReadSeed()` | `int?` | Numeric text entry |
| 4 | `SelectDifficulty()` | `Difficulty` | Menu selection (3-4 options) |
| 5 | `SelectClass(PrestigeData?)` | `PlayerClassDefinition` | Menu selection (6 classes) |
| 6 | `SelectSaveToLoad(string[])` | `string?` | Menu selection (N saves + cancel) |
| 7 | `ShowStartupMenu(bool)` | `StartupMenuOption` | Menu selection (2-3 options) |
| 8 | `ShowInventoryAndSelect(Player)` | `Item?` | Menu selection over inventory |
| 9 | `ShowShopAndSelect(stock, int)` | `int` | Menu selection (1-based index) |
| 10 | `ShowSellMenuAndSelect(items, int)` | `int` | Menu selection (1-based index) |
| 11 | `ShowShopWithSellAndSelect(stock, int)` | `int` | Menu selection (buy/sell/leave) |
| 12 | `ShowCombatMenuAndSelect(Player, Enemy)` | `string` | Menu selection (A/B/I/F) |
| 13 | `ShowCraftMenuAndSelect(recipes)` | `int` | Menu selection (1-based index) |
| 14 | `ShowShrineMenuAndSelect(int, ...)` | `int` | Menu selection (4 blessings + leave) |
| 15 | `ShowConfirmMenu(string)` | `bool` | Yes/No selection |
| 16 | `ShowTrapChoiceAndSelect(string, string, string)` | `int` | 2-option menu |
| 17 | `ShowForgottenShrineMenuAndSelect()` | `int` | 3-option menu |
| 18 | `ShowContestedArmoryMenuAndSelect(int)` | `int` | 2-option menu |
| 19 | `ShowAbilityMenuAndSelect(unavailable, available)` | `Ability?` | Menu selection + info lines |
| 20 | `ShowCombatItemMenuAndSelect(consumables)` | `Item?` | Menu selection |
| 21 | `ShowEquipMenuAndSelect(equippable)` | `Item?` | Menu selection |
| 22 | `ShowUseMenuAndSelect(usable)` | `Item?` | Menu selection |
| 23 | `ShowTakeMenuAndSelect(roomItems)` | `TakeSelection?` | Menu selection |
| 24 | `ShowLevelUpChoiceAndSelect(Player)` | `int` | Menu selection (3 stats) |
| 25 | `ShowSkillTreeMenu(Player)` | `Skill?` | Menu selection (skill tree) |
| 26 | `ShowIntroNarrative()` | `bool` | Confirmation (continue/skip) |

**Count: 26 input-coupled methods.**

Input breakdown:
- **Menu selection** (pick from list): 22 methods
- **Free text entry**: 2 methods (`ReadCommandInput`, `ReadPlayerName`)
- **Numeric entry**: 1 method (`ReadSeed`)
- **Confirmation**: 1 method (`ShowIntroNarrative`)

### Methods That Mix Output + Input

Several `*AndSelect` methods are already decomposed in the interface — there's a display-only `ShowShop()` paired with `ShowShopAndSelect()`, `ShowLevelUpChoice()` paired with `ShowLevelUpChoiceAndSelect()`, etc. However, the `*AndSelect` methods still render their own content before collecting input. In Avalonia, this is natural — the same control displays options and handles clicks.

Methods that are **not** decomposable (the display IS the input):
- `ShowCombatMenuAndSelect` — the combat action menu IS the selection UI
- `ShowConfirmMenu` — the Yes/No prompt IS the UI
- `ContentPanelMenu<T>` — the generic pattern renders options + handles arrow keys atomically

This is fine. In Avalonia, each becomes a ViewModel that exposes options + a `TaskCompletionSource<T>` for the result.

---

## Section 2: The Split

### Proposed Interfaces

Split `IDisplayService` into two interfaces:

```csharp
// Dungnz.Models/IGameDisplay.cs
public interface IGameDisplay
{
    // ── Title / Narrative ──
    void ShowTitle();
    void ShowEnhancedTitle();
    bool ShowIntroNarrative(); // NOTE: returns bool but it's "press any key" — output-side
    void ShowPrestigeInfo(PrestigeData prestige);
    void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant);

    // ── Room / Map ──
    void ShowRoom(Room room);
    void ShowMap(Room currentRoom, int floor = 1);

    // ── Combat Output ──
    void ShowCombat(string message);
    void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects);
    void ShowCombatMessage(string message);
    void ShowColoredCombatMessage(string message, string color);
    void ShowCombatStart(Enemy enemy);
    void ShowCombatEntryFlags(Enemy enemy);
    void ShowEnemyArt(Enemy enemy);
    void ShowEnemyDetail(Enemy enemy);
    void ShowCombatHistory();

    // ── Player / Item Display ──
    void ShowPlayerStats(Player player);
    void ShowInventory(Player player);
    void ShowEquipment(Player player);
    void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem);
    void ShowItemDetail(Item item);
    void ShowLootDrop(Item item, Player player, bool isElite = false);
    void ShowGoldPickup(int amount, int newTotal);
    void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax);

    // ── Shop / Craft Display ──
    void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold);
    void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold);
    void ShowCraftRecipe(string recipeName, Item result,
        List<(string ingredient, bool playerHasIt)> ingredients);

    // ── General Output ──
    void ShowMessage(string message);
    void ShowError(string message);
    void ShowHelp();
    void ShowCommandPrompt(Player? player = null);
    void ShowColoredMessage(string message, string color);
    void ShowColoredStat(string label, string value, string valueColor);
    void ShowLevelUpChoice(Player player);

    // ── End Screens ──
    void ShowVictory(Player player, int floorsCleared, RunStats stats);
    void ShowGameOver(Player player, string? killedBy, RunStats stats);

    // ── Refresh / HUD ──
    void RefreshDisplay(Player player, Room room, int floor);
    void UpdateCooldownDisplay(IReadOnlyList<(string name, int turnsRemaining)> cooldowns) { }
}
```

```csharp
// Dungnz.Models/IGameInput.cs
public interface IGameInput
{
    // ── Text Entry ──
    string? ReadCommandInput();
    string ReadPlayerName();
    int? ReadSeed();

    // ── Startup Flow ──
    StartupMenuOption ShowStartupMenu(bool hasSaves);
    Difficulty SelectDifficulty();
    PlayerClassDefinition SelectClass(PrestigeData? prestige);
    string? SelectSaveToLoad(string[] saveNames);

    // ── Combat Menus ──
    string ShowCombatMenuAndSelect(Player player, Enemy enemy);
    Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities);
    Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables);
    int ShowLevelUpChoiceAndSelect(Player player);

    // ── Inventory / Equipment Menus ──
    Item? ShowInventoryAndSelect(Player player);
    Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable);
    Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable);
    TakeSelection? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems);

    // ── Shop / Craft Menus ──
    int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold);
    int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold);
    int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold);
    int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes);

    // ── Special Room Menus ──
    int ShowShrineMenuAndSelect(int playerGold, int healCost = 30,
        int blessCost = 50, int fortifyCost = 75, int meditateCost = 75);
    bool ShowConfirmMenu(string prompt);
    int ShowTrapChoiceAndSelect(string header, string option1, string option2);
    int ShowForgottenShrineMenuAndSelect();
    int ShowContestedArmoryMenuAndSelect(int playerDefense);

    // ── Skill Tree ──
    Skill? ShowSkillTreeMenu(Player player);
}
```

### Design Notes

**`ShowIntroNarrative()` → IGameDisplay.** It returns `bool` but the current implementation always returns `false`. The return is reserved for a future "skip" button. In Avalonia, this becomes a "press any key to continue" overlay — fundamentally output with a trivial input gate. If skip becomes real, it moves to `IGameInput`.

**`IDisplayService` becomes a facade.** To avoid a flag-day rewrite of all call sites:

```csharp
// Dungnz.Models/IDisplayService.cs (backward compat)
public interface IDisplayService : IGameDisplay, IGameInput { }
```

All existing code (`GameLoop`, `CombatEngine`, `StartupOrchestrator`) continues to accept `IDisplayService`. New code can depend on the narrower interface it actually needs.

**ConsoleDisplayService** implements `IDisplayService` (both interfaces) — no change needed, it already implements everything. The partial split is purely for new implementations to pick their targets.

**SpectreLayoutDisplayService** implements `IDisplayService` — same, no breaking change.

**AvaloniaDisplayService** will implement `IDisplayService` to maintain full compatibility, but internally it will be structured as two collaborators: a display renderer and an input handler.

---

## Section 3: Avalonia Project Structure

### Solution Layout

```
Dungnz.slnx
├── Dungnz.csproj                    (entry point — references Engine, Display.Avalonia)
├── Dungnz.Models/                   (IDisplayService, IGameDisplay, IGameInput, domain types)
├── Dungnz.Data/
├── Dungnz.Systems/
├── Dungnz.Engine/                   (references Models, Data, Systems, Display)
├── Dungnz.Display/                  (Spectre impl + ConsoleDisplayService — unchanged)
├── Dungnz.Display.Avalonia/         (NEW — Avalonia impl)
│   ├── Dungnz.Display.Avalonia.csproj
│   ├── AvaloniaDisplayService.cs    (IDisplayService implementation)
│   ├── ViewModels/
│   │   ├── MainWindowViewModel.cs
│   │   ├── MapPanelViewModel.cs
│   │   ├── StatsPanelViewModel.cs
│   │   ├── ContentPanelViewModel.cs
│   │   ├── GearPanelViewModel.cs
│   │   ├── LogPanelViewModel.cs
│   │   └── InputPanelViewModel.cs
│   ├── Views/
│   │   ├── MainWindow.axaml(.cs)
│   │   └── Panels/
│   │       ├── MapPanel.axaml(.cs)
│   │       ├── StatsPanel.axaml(.cs)
│   │       ├── ContentPanel.axaml(.cs)
│   │       ├── GearPanel.axaml(.cs)
│   │       ├── LogPanel.axaml(.cs)
│   │       └── InputPanel.axaml(.cs)
│   ├── Controls/
│   │   └── AsciiMapControl.cs       (custom control for map rendering)
│   ├── Converters/
│   │   └── TierColorConverter.cs
│   └── App.axaml(.cs)
└── Dungnz.Tests/
```

### New csproj

```xml
<!-- Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors>CS1591;CS1573;CS1712</WarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Dungnz.Models/Dungnz.Models.csproj" />
    <ProjectReference Include="../Dungnz.Systems/Dungnz.Systems.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Avalonia 11.x — stable release line -->
    <PackageReference Include="Avalonia" Version="11.3.2" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.2" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.2" />
    <!-- CommunityToolkit.Mvvm for ObservableObject + source generators -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
  </ItemGroup>

  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Dungnz.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Dungnz</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

</Project>
```

**Key package choices:**
- `Avalonia` 11.3.x — stable, cross-platform, GPU-accelerated
- `Avalonia.Desktop` — desktop windowing (Skia/OpenGL backend)
- `Avalonia.Themes.Fluent` — built-in theme, dark mode ready
- `CommunityToolkit.Mvvm` — lightweight MVVM (no ReactiveUI complexity)

**No ReactiveUI.** CommunityToolkit.Mvvm provides `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` via source generators. Simpler learning curve, zero reactive pipeline complexity. If we need reactive streams later, it's additive.

### Program.cs Wiring

```csharp
// Program.cs — updated entry point
var useAvalonia = args.Contains("--avalonia");

if (useAvalonia)
{
    // Avalonia takes over the main thread.
    // Game runs on background thread, display on UI thread.
    var app = AvaloniaAppBuilder.Configure(args);
    app.RunGame(prestige); // Internally: starts Avalonia, creates window, runs game on Task.Run
}
else if (inputReader.IsInteractive)
{
    spectreService = new SpectreLayoutDisplayService();
    display = spectreService;
    // ... existing Spectre flow unchanged ...
}
else
{
    display = new ConsoleDisplayService(inputReader);
    // ... existing headless flow unchanged ...
}
```

The `--avalonia` flag mirrors the pattern used by the deleted `--tui` flag. ConsoleDisplayService and SpectreLayoutDisplayService remain untouched — full rollback = remove flag + delete directory.

### Dungnz.csproj Reference Update

```xml
<!-- Add to Dungnz.csproj ItemGroup -->
<ProjectReference Include="Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj" />
```

### Dungnz.slnx Update

```xml
<Project Path="Dungnz.Display.Avalonia/Dungnz.Display.Avalonia.csproj" />
```

---

## Section 4: AvaloniaDisplayService Architecture

### 1. Thread Model

Avalonia runs its UI on the main thread via `Dispatcher.UIThread`. The game engine runs on a background thread (same as today). All display method calls from the game thread must be marshalled to the UI thread.

**Pattern: `Dispatcher.UIThread.InvokeAsync`**

```csharp
public class AvaloniaDisplayService : IDisplayService
{
    private readonly MainWindowViewModel _vm;

    // ── Output methods: fire-and-forget marshal to UI thread ──
    public void ShowMessage(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() => _vm.Content.AppendMessage(message));
    }

    public void ShowPlayerStats(Player player)
    {
        Dispatcher.UIThread.InvokeAsync(() => _vm.Stats.Update(player));
    }

    public void ShowMap(Room currentRoom, int floor = 1)
    {
        Dispatcher.UIThread.InvokeAsync(() => _vm.Map.Update(currentRoom, floor));
    }

    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
            _vm.Stats.UpdateCombat(player, enemy, playerEffects, enemyEffects));
    }

    public void RefreshDisplay(Player player, Room room, int floor)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.Stats.Update(player);
            _vm.Content.ShowRoom(room);
            _vm.Map.Update(room, floor);
            _vm.Gear.Update(player);
        });
    }
    
    // ... 37 output methods follow this same pattern ...
}
```

**Why InvokeAsync (not Post)?** InvokeAsync returns a `Task` — we can await it if we ever need confirmation that the UI updated. For pure output methods we fire-and-forget. For input methods we await.

### 2. The 6-Panel Layout in AXAML

Direct mapping from `SpectreLayout.Panels`:

```xml
<!-- Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:panels="using:Dungnz.Display.Avalonia.Views.Panels"
        Title="D U N G N Z" Width="1280" Height="800"
        Background="#1a1a2e">

  <Grid RowDefinitions="2*,5*,3*" ColumnDefinitions="*">

    <!-- Top Row: Map | Stats (20% height) -->
    <Grid Grid.Row="0" ColumnDefinitions="6*,4*">
      <panels:MapPanel   Grid.Column="0" DataContext="{Binding Map}" />
      <panels:StatsPanel Grid.Column="1" DataContext="{Binding Stats}" />
    </Grid>

    <!-- Middle Row: Content | Gear (50% height) -->
    <Grid Grid.Row="1" ColumnDefinitions="7*,3*">
      <panels:ContentPanel Grid.Column="0" DataContext="{Binding Content}" />
      <panels:GearPanel    Grid.Column="1" DataContext="{Binding Gear}" />
    </Grid>

    <!-- Bottom Row: Log / Input (30% height) -->
    <Grid Grid.Row="2" RowDefinitions="7*,3*">
      <panels:LogPanel   Grid.Row="0" DataContext="{Binding Log}" />
      <panels:InputPanel Grid.Row="1" DataContext="{Binding Input}" />
    </Grid>

  </Grid>
</Window>
```

Panel name mapping from Spectre:
| Spectre Panel | Avalonia Panel | Grid Position |
|---------------|---------------|---------------|
| `Map` | `MapPanel` | Row 0, Col 0 |
| `Stats` | `StatsPanel` | Row 0, Col 1 |
| `Content` | `ContentPanel` | Row 1, Col 0 |
| `Gear` | `GearPanel` | Row 1, Col 1 |
| `Log` | `LogPanel` | Row 2, Row 0 |
| `Input` | `InputPanel` | Row 2, Row 1 |

Ratios match exactly: `2*:5*:3*` rows = 20%:50%:30%, `6*:4*` = 60%:40%, `7*:3*` = 70%:30%.

### 3. Input Handling — The TaskCompletionSource Pattern

For input-coupled methods, the game thread must block until the player makes a choice in the Avalonia UI. The pattern:

```csharp
// ── Input methods: block game thread via TaskCompletionSource ──

public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
{
    // Create a TCS that the UI will complete when user picks
    var tcs = new TaskCompletionSource<string>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    Dispatcher.UIThread.InvokeAsync(() =>
    {
        var options = new List<(string Label, string Value)>
        {
            ("⚔  Attack", "A"),
            ("✨ Ability", "B"),
            ("🧪 Use Item", "I"),
            ("🏃 Flee", "F")
        };
        _vm.Content.ShowMenu("Combat Action", options, tcs);
    });

    // Block game thread until user selects (this is the key pattern)
    return tcs.Task.GetAwaiter().GetResult();
}

public bool ShowConfirmMenu(string prompt)
{
    var tcs = new TaskCompletionSource<bool>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    Dispatcher.UIThread.InvokeAsync(() =>
    {
        _vm.Content.ShowConfirm(prompt, tcs);
    });

    return tcs.Task.GetAwaiter().GetResult();
}

public Item? ShowInventoryAndSelect(Player player)
{
    var tcs = new TaskCompletionSource<Item?>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    Dispatcher.UIThread.InvokeAsync(() =>
    {
        var items = player.Inventory
            .Select(i => (Label: $"{ItemIcon(i)} {i.Name}", Value: (Item?)i))
            .Append(("← Cancel", (Item?)null))
            .ToList();
        _vm.Content.ShowMenu("Inventory", items, tcs);
    });

    return tcs.Task.GetAwaiter().GetResult();
}
```

**On the UI side** (ContentPanelViewModel):

```csharp
public partial class ContentPanelViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<MenuItemViewModel> _menuItems = new();
    [ObservableProperty] private bool _isMenuVisible;
    [ObservableProperty] private string _menuTitle = "";

    private TaskCompletionSource<object?>? _pendingSelection;

    public void ShowMenu<T>(string title, List<(string Label, T Value)> options,
        TaskCompletionSource<T> tcs)
    {
        MenuTitle = title;
        MenuItems.Clear();
        foreach (var (label, value) in options)
        {
            MenuItems.Add(new MenuItemViewModel(label, value,
                () => { tcs.TrySetResult(value); IsMenuVisible = false; }));
        }
        IsMenuVisible = true;
        // TCS bridges back to game thread when user clicks/keys Enter
    }
}
```

**Why `GetAwaiter().GetResult()` and not `.Result`?** Same blocking behavior, but `.GetResult()` doesn't wrap exceptions in `AggregateException`. The game thread is a background thread — blocking it is fine. The UI thread is never blocked.

**Why `RunContinuationsAsynchronously`?** Prevents the game thread continuation from running on the UI thread when `TrySetResult` is called from a UI event handler.

**Keyboard support:** `InputPanel` handles arrow keys and Enter via `KeyDown` event, forwarding to `ContentPanelViewModel` to navigate the active menu — identical UX to current `ContentPanelMenu<T>`.

### 4. Map Panel Approach

The current map is ASCII characters rendered into a Spectre Panel via `BuildAsciiMap()`. The map uses ~15 distinct symbols (`[@]`, `[?]`, `[B]`, `[E]`, etc.) with positional grid layout.

**Recommended approach: `TextBlock` with monospace font.**

```csharp
// MapPanelViewModel.cs
public partial class MapPanelViewModel : ObservableObject
{
    [ObservableProperty] private string _mapText = "";
    [ObservableProperty] private string _legendText = "";

    public void Update(Room currentRoom, int floor)
    {
        // Reuse the existing BuildAsciiMap logic from SpectreLayoutDisplayService
        // Strip Spectre markup, output plain text
        MapText = MapRenderer.BuildPlainTextMap(currentRoom, floor);
        LegendText = MapRenderer.BuildLegend();
    }
}
```

```xml
<!-- Panels/MapPanel.axaml -->
<Border BorderBrush="Green" BorderThickness="1" CornerRadius="4" Padding="4">
  <StackPanel>
    <TextBlock Text="Dungeon Map" FontWeight="Bold" Foreground="Green" />
    <TextBlock Text="{Binding MapText}"
               FontFamily="Cascadia Mono,Consolas,Courier New"
               FontSize="12" Foreground="White" />
    <TextBlock Text="{Binding LegendText}"
               FontFamily="Cascadia Mono,Consolas,Courier New"
               FontSize="10" Foreground="Gray" />
  </StackPanel>
</Border>
```

**Why not Canvas/DrawingContext?** The map is 15-20 characters wide by 10-15 tall. A `TextBlock` with a monospace font renders this perfectly with zero custom drawing code. The symbols are Unicode — they render natively. Color per-character can be added later via `InlineCollection` with `Run` elements if needed, or via a custom `FormattedTextBlock` control. Start simple.

**Future upgrade path:** If we want colored map tiles, upgrade to a custom control that uses `DrawText` per character with different brushes. But TextBlock is the minimum viable approach and matches the current ASCII aesthetic.

---

## Section 5: Migration Phases

| Phase | What | Who | Dependencies | Risk | Notes |
|-------|------|-----|-------------|------|-------|
| **P0** | `IDisplayService` split → `IGameDisplay` + `IGameInput` + facade `IDisplayService : IGameDisplay, IGameInput` | Hill | None | **Low** | Pure additive. Zero call-site changes. Add two new files to Dungnz.Models. Existing interface inherits both. |
| **P1** | Extract `MapRenderer` static class from `SpectreLayoutDisplayService.ShowMap` | Hill | None | **Low** | Shared map logic for both Spectre and Avalonia. Strip Spectre markup → plain text variant. |
| **P2** | Scaffold `Dungnz.Display.Avalonia` project | Hill | P0 | **Low** | Empty project, csproj, App.axaml, MainWindow.axaml with 6-panel Grid. Builds but renders empty panels. Wire `--avalonia` flag in Program.cs. |
| **P3** | Output-only panels: Stats, Gear, Log, Content | Hill + Barton | P2 | **Medium** | Implement `IGameDisplay` output methods. Stats/Gear use data binding. Content/Log use `ObservableCollection<string>`. Map deferred. |
| **P4** | Map panel | Hill | P1, P3 | **Medium** | `MapPanelViewModel` + `TextBlock` monospace rendering. Reuses extracted `MapRenderer`. |
| **P5** | Input panel + `ReadCommandInput` | Hill | P3 | **Medium** | `TextBox` for command entry, Enter to submit, `TaskCompletionSource<string?>` pattern. Game thread can resume. |
| **P6** | Menu input: all `*AndSelect` methods | Hill + Barton | P3, P5 | **High** | 22 menu-selection methods via `ContentPanelViewModel.ShowMenu<T>`. Keyboard nav (arrow keys + Enter). This is the hardest phase — every menu needs testing. |
| **P7** | Text entry inputs: `ReadPlayerName`, `ReadSeed` | Hill | P5 | **Low** | Reuse `InputPanel` TextBox with validation. |
| **P8** | Startup flow: `SelectDifficulty`, `SelectClass`, `ShowStartupMenu` | Hill | P6 | **Medium** | Pre-game-loop menus. May need a dedicated startup view or reuse Content panel. |
| **P9** | ConsoleDisplayService compatibility verification | Romanoff | P0 | **Low** | Verify `ConsoleDisplayService` still implements `IDisplayService` after split. Run all existing tests. CI must stay green at every phase. |
| **P10** | Integration testing + polish | Romanoff + Hill | P6, P7, P8 | **Medium** | Play through full game in Avalonia mode. File bugs. Fix rendering edge cases. |
| **P11** | CI build update: add `Dungnz.Display.Avalonia` to build matrix | Fitz | P2 | **Low** | Ensure `dotnet build` and `dotnet test` include the new project. |

**Critical path:** P0 → P2 → P3 → P5 → P6 → P10

**Parallel tracks:**
- P1 (MapRenderer extraction) can run in parallel with P0-P2
- P9 (ConsoleDisplayService compat) can run in parallel with P3-P8
- P11 (CI) can run immediately after P2

**Game stays playable throughout.** Spectre mode is the default. `--avalonia` is opt-in. Every PR must leave `dotnet build && dotnet test` green.

---

## Section 6: Risks and Mitigations

### R1: Avalonia Learning Curve
**Risk:** Team has zero Avalonia experience. XAML, data binding, and Dispatcher patterns are new.  
**Likelihood:** High  
**Impact:** Medium (slower velocity, not blocked)  
**Mitigation:**
- Use `CommunityToolkit.Mvvm` instead of ReactiveUI — dramatically simpler. `[ObservableProperty]` generates INotifyPropertyChanged boilerplate via source generators. No reactive streams to learn.
- Code-behind is acceptable for P2-P3. Refactor to MVVM once patterns are proven.
- P2 (scaffold) is deliberately minimal — just get a window with 6 colored rectangles. Build confidence before complexity.

### R2: Input Blocking Pattern Complexity
**Risk:** The `TaskCompletionSource` pattern for input-coupled methods is conceptually simple but has edge cases: what if the window closes mid-menu? What if two menus overlap? What if the game thread deadlocks waiting for input?  
**Likelihood:** Medium  
**Impact:** High (deadlocks = unrecoverable)  
**Mitigation:**
- **One menu at a time.** The game engine is single-threaded and sequential — it can only be waiting for one input at a time. No concurrent menu risk.
- **Window close → cancel.** If the window closes, all pending `TaskCompletionSource` instances get `TrySetCanceled()`. Game thread catches `OperationCanceledException` and exits gracefully.
- **Timeout guard.** Add a 5-minute timeout on `tcs.Task.Wait(TimeSpan)` as a deadlock safety net. Log and exit if hit.
- **Unit-testable pattern.** `TaskCompletionSource` can be completed programmatically in tests — same testing story as `FakeMenuNavigator`.

### R3: Map Rendering Approach Uncertainty
**Risk:** Monospace `TextBlock` may not handle all Unicode symbols correctly across platforms (Windows/macOS/Linux font differences). Emoji width may vary.  
**Likelihood:** Medium  
**Impact:** Low (visual glitch, not functional)  
**Mitigation:**
- Start with `TextBlock` + Cascadia Mono. This handles all current map symbols (`[@]`, `[B]`, `[?]`, corridors `─`, `│`).
- If emoji symbols (`🌑`, `🔥`) cause width issues, replace with ASCII-only map symbols in Avalonia mode. The map already has both emoji and ASCII legend entries.
- Upgrade path to custom `DrawText` control is straightforward if needed.

### R4: Test Coverage During Migration
**Risk:** Romanoff's concern — 2,154 tests exist for current behavior. New `AvaloniaDisplayService` has zero tests initially. Regression risk during interface split.  
**Likelihood:** Medium  
**Impact:** Medium  
**Mitigation:**
- **P0 (interface split) is additive.** `IDisplayService : IGameDisplay, IGameInput` — zero call-site changes. All 2,154 tests pass unchanged. If they don't, the split is wrong.
- **AvaloniaDisplayService gets its own test class** mirroring the `TestDisplayService` pattern — verify every method is callable, verify output reaches ViewModels, verify input TCS completes.
- **P9 is explicitly a Romanoff phase** — ConsoleDisplayService compatibility verification before and after.
- **ViewModels are independently testable** — no Avalonia UI thread needed. `MapPanelViewModel.Update(room, floor)` can be tested as pure C#.

### R5: Build Complexity Increase
**Risk:** Adding an Avalonia project increases build time, CI complexity, and NuGet dependency surface. Avalonia pulls in Skia, HarfBuzz, and platform-specific native libraries.  
**Likelihood:** High (it will happen)  
**Impact:** Low (manageable)  
**Mitigation:**
- **Conditional build.** If Avalonia packages slow CI significantly, gate the Avalonia project behind a build property: `dotnet build -p:IncludeAvalonia=true`. Default CI builds skip it; dedicated Avalonia CI job builds it.
- **Separate project isolation.** `Dungnz.Display.Avalonia` has zero reverse dependencies — nothing in Engine/Systems/Models knows about Avalonia. Removing it is delete-directory-and-revert-two-lines.
- Fitz handles CI integration in P11.

### R6: Spectre Deprecation Timeline
**Risk:** Maintaining two full `IDisplayService` implementations (Spectre + Avalonia) doubles the display maintenance burden long-term.  
**Likelihood:** High (if migration succeeds)  
**Impact:** Medium  
**Mitigation:**
- No deprecation until Avalonia achieves feature parity AND passes a full play-through.
- Target: Spectre becomes `--classic` fallback (same pattern as Terminal.Gui experiment).
- ConsoleDisplayService (headless/CI) is permanent — it's 100 lines and never changes.

---

## Open Questions (for Anthony)

1. **Dark theme only, or configurable?** Recommend dark-only initially (matches terminal aesthetic).
2. **Window resizable?** Recommend yes, with minimum 1024×600. Grid ratios scale naturally.
3. **Sound?** Out of scope for this spec. Can be added later via Avalonia audio APIs.
