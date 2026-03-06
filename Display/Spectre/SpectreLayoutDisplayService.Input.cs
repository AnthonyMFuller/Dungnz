using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Spectre.Console;

namespace Dungnz.Display.Spectre;

/// <summary>
/// Input-coupled methods for <see cref="SpectreLayoutDisplayService"/>.
/// All methods that require user selection pause the Live render loop via
/// <see cref="PauseAndRun{T}"/>, run a <see cref="SelectionPrompt{T}"/> or
/// <see cref="TextPrompt{T}"/>, then resume Live.  Acceptable for a turn-based
/// game per Anthony's decision (Option E).
/// </summary>
public partial class SpectreLayoutDisplayService
{
    // ══════════════════════════════════════════════════════════════════════════
    // Issue #1068 — Equipment comparison with Spectre Table
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem)
    {
        var tc = InputTierColor(newItem.Tier);

        var newHeader = $"[{tc} bold]{Markup.Escape(newItem.Name)}[/] [dim](new)[/]";
        var oldHeader = oldItem != null
            ? $"[dim]{Markup.Escape(oldItem.Name)}[/] [dim](equipped)[/]"
            : "[dim](nothing equipped)[/]";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn(new TableColumn(newHeader).Centered())
            .AddColumn(new TableColumn(oldHeader).Centered());

        AddIntCompareRow(table, "ATK",       newItem.AttackBonus,      oldItem?.AttackBonus      ?? 0);
        AddIntCompareRow(table, "DEF",       newItem.DefenseBonus,     oldItem?.DefenseBonus     ?? 0);
        AddIntCompareRow(table, "Max MP",    newItem.MaxManaBonus,     oldItem?.MaxManaBonus     ?? 0);
        AddIntCompareRow(table, "HP/hit",    newItem.HPOnHit,          oldItem?.HPOnHit          ?? 0);
        AddPctCompareRow(table, "Dodge",     newItem.DodgeBonus,       oldItem?.DodgeBonus       ?? 0f);
        AddPctCompareRow(table, "Crit",      newItem.CritChance,       oldItem?.CritChance       ?? 0f);
        AddPctCompareRow(table, "Block",     newItem.BlockChanceBonus, oldItem?.BlockChanceBonus ?? 0f);

        var panel = new Panel(table)
            .Header("[bold yellow]⚔  ITEM DROP[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);

        if (_ctx.IsLiveActive)
            _ctx.UpdatePanel(SpectreLayout.Panels.Content, panel);
        else
            AnsiConsole.Write(panel);
    }

    private static void AddIntCompareRow(Table t, string label, int newVal, int oldVal)
    {
        if (newVal == 0 && oldVal == 0) return;
        int delta = newVal - oldVal;
        var deltaMarkup = delta > 0 ? $"[green]+{delta}[/]" : delta < 0 ? $"[red]{delta}[/]" : "[dim]±0[/]";
        t.AddRow($"{label}: [bold]{newVal}[/]", $"{label}: {oldVal}  {deltaMarkup}");
    }

    private static void AddPctCompareRow(Table t, string label, float newVal, float oldVal)
    {
        if (newVal == 0f && oldVal == 0f) return;
        float delta = newVal - oldVal;
        var deltaMarkup = delta > 0.001f
            ? $"[green]+{delta:P0}[/]"
            : delta < -0.001f ? $"[red]{delta:P0}[/]"
            : "[dim]±0%[/]";
        t.AddRow($"{label}: [bold]{newVal:P0}[/]", $"{label}: {oldVal:P0}  {deltaMarkup}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Issue #1067 — Input-coupled methods
    // ══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public string ReadPlayerName()
    {
        return PauseAndRun(() =>
            AnsiConsole.Prompt(
                new TextPrompt<string>("[bold yellow]Enter your character's name:[/]")
                    .DefaultValue("Hero")
                    .Validate(n => !string.IsNullOrWhiteSpace(n)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Name cannot be empty.[/]"))));
    }

    /// <inheritdoc/>
    public Item? ShowInventoryAndSelect(Player player)
    {
        if (player.Inventory.Count == 0) return null;

        var opts = player.Inventory
            .Select(i => ($"{InputItemIcon(i)} {Markup.Escape(i.Name)} [grey]({Markup.Escape(InputPrimaryStatLabel(i))})[/]", (Item?)i))
            .Append(("← Cancel", (Item?)null));

        return NullableSelectionPrompt(
            $"[bold]📦 Inventory ({player.Inventory.Count}/{Player.MaxInventorySize})[/]",
            opts);
    }

    /// <inheritdoc/>
    public Difficulty SelectDifficulty()
    {
        var opts = new[]
        {
            ("[green]CASUAL[/]  — Weaker enemies · Cheap shops · Start with 50g + 3 potions",  Difficulty.Casual),
            ("[yellow]NORMAL[/] — Balanced challenge · The intended experience",                Difficulty.Normal),
            ("[red]HARD[/]    — Stronger enemies · Scarce rewards · ☠  Permadeath",            Difficulty.Hard),
        };
        return SelectionPromptValue("[bold yellow]Choose your difficulty[/]", opts);
    }

    /// <inheritdoc/>
    public PlayerClassDefinition SelectClass(PrestigeData? prestige)
    {
        var opts = PlayerClassDefinition.All.Select(def =>
        {
            var bonus = prestige?.PrestigeLevel > 0
                ? $" [dim](+{prestige.BonusStartHP}HP +{prestige.BonusStartAttack}ATK +{prestige.BonusStartDefense}DEF prestige)[/]"
                : "";
            var label = $"{InputClassIcon(def)} [bold]{Markup.Escape(def.Name)}[/] — {Markup.Escape(def.Description)}{bonus}";
            return (label, def);
        });
        return SelectionPromptValue("[bold yellow]Choose your class[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var list = stock.ToList();
        var opts = list.Select((s, i) =>
        {
            var tc = InputTierColor(s.item.Tier);
            var afford = s.price <= playerGold ? "[green]✓[/]" : "[red]✗[/]";
            return ($"{afford} [{tc}]{Markup.Escape(s.item.Name)}[/] — {s.item.Tier} — {Markup.Escape(InputPrimaryStatLabel(s.item))} — {s.price}g", i + 1);
        }).Append(("← Leave", 0));
        return SelectionPromptValue($"[bold]🏪 Merchant  (Gold: {playerGold}g)[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        var list = items.ToList();
        var opts = list.Select((s, i) =>
        {
            var tc = InputTierColor(s.item.Tier);
            return ($"[{tc}]{Markup.Escape(s.item.Name)}[/] — [yellow]{s.sellPrice}g[/]", i + 1);
        }).Append(("← Cancel", 0));
        return SelectionPromptValue($"[bold]💰 Sell Items  (Gold: {playerGold}g)[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowLevelUpChoiceAndSelect(Player player)
    {
        var opts = new[]
        {
            ("[green]+5 Max HP[/]",  1),
            ("[red]+2 Attack[/]",    2),
            ("[cyan]+2 Defense[/]",  3),
        };
        return SelectionPromptValue(
            $"[bold yellow]🎉 Level Up!  You are now level {player.Level} — choose a stat boost[/]",
            opts);
    }

    /// <inheritdoc/>
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
    {
        double hpPct = player.MaxHP > 0 ? (double)player.HP / player.MaxHP : 1.0;
        var hpColor = hpPct > 0.5 ? "green" : hpPct >= 0.25 ? "yellow" : "red";

        var opts = new[]
        {
            ("⚔   Attack",   "A"),
            ("✨  Ability",   "B"),
            ("🎒  Use Item",  "I"),
            ("🏃  Flee",      "F"),
        };
        return SelectionPromptValue(
            $"[bold]⚔ [red]{Markup.Escape(enemy.Name)}[/]   HP: [{hpColor}]{player.HP}/{player.MaxHP}[/][/]",
            opts);
    }

    /// <inheritdoc/>
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes)
    {
        var list = recipes.ToList();
        var opts = list.Select((r, i) =>
        {
            var label = r.canCraft
                ? $"[green]✓[/] {Markup.Escape(r.recipeName)}"
                : $"[dim]✗ {Markup.Escape(r.recipeName)}[/]";
            return (label, i + 1);
        }).Append(("← Cancel", 0));
        return SelectionPromptValue("[bold]⚗  Crafting[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75)
    {
        var opts = new[]
        {
            ($"[green]💚 Heal[/]        ({healCost}g)",      1),
            ($"[yellow]✨ Blessing[/]   ({blessCost}g)",     2),
            ($"[cyan]🛡 Fortify[/]     ({fortifyCost}g)",   3),
            ($"[blue]🧘 Meditate[/]   ({meditateCost}g)",   4),
            ("← Leave",                                        0),
        };
        return SelectionPromptValue($"[bold yellow]✨ Shrine  (Gold: {playerGold}g)[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var list = stock.ToList();
        var opts = new List<(string, int)>();
        for (int i = 0; i < list.Count; i++)
        {
            var (item, price) = list[i];
            var tc = InputTierColor(item.Tier);
            var afford = price <= playerGold ? "[green]✓[/]" : "[red]✗[/]";
            opts.Add(($"{afford} [{tc}]{Markup.Escape(item.Name)}[/] — {price}g", i + 1));
        }
        opts.Add(("💰 Sell Items", -1));
        opts.Add(("← Leave", 0));
        return SelectionPromptValue($"[bold]🏪 Merchant  (Gold: {playerGold}g)[/]", opts);
    }

    /// <inheritdoc/>
    public bool ShowConfirmMenu(string prompt)
    {
        var opts = new[]
        {
            ("[green]Yes[/]", true),
            ("[red]No[/]",    false),
        };
        return SelectionPromptValue($"[bold]{Markup.Escape(prompt)}[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2)
    {
        var opts = new[]
        {
            (Markup.Escape(option1), 1),
            (Markup.Escape(option2), 2),
            ("← Leave",              0),
        };
        return SelectionPromptValue($"[bold yellow]{Markup.Escape(header)}[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowForgottenShrineMenuAndSelect()
    {
        var opts = new[]
        {
            ("[green]💚 Prayer of Restoration[/]  — Full heal + cure all ailments", 1),
            ("[red]⚔  Prayer of Might[/]         — Permanent +3 ATK",              2),
            ("[cyan]🛡 Prayer of Resilience[/]    — Permanent +3 DEF",              3),
            ("← Leave",                                                               0),
        };
        return SelectionPromptValue("[bold yellow]✨ Forgotten Shrine[/]", opts);
    }

    /// <inheritdoc/>
    public int ShowContestedArmoryMenuAndSelect(int playerDefense)
    {
        int dmg = Math.Max(10 - playerDefense, 1);
        var opts = new[]
        {
            ($"[red]⚡ Rush in[/]           — Take {dmg} damage, get rare weapon",   1),
            ("[cyan]🛡 Careful approach[/]  — Take 3 damage, get common weapon",     2),
            ("← Leave",                                                                0),
        };
        return SelectionPromptValue("[bold yellow]⚔  Contested Armory[/]", opts);
    }

    /// <inheritdoc/>
    public Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities)
    {
        var opts = new List<(string, Ability?)>();

        foreach (var a in availableAbilities)
            opts.Add(($"[green]✓[/] [bold]{Markup.Escape(a.Name)}[/]  [grey](MP: {a.ManaCost})[/] — {Markup.Escape(a.Description)}", a));

        foreach (var (a, onCd, cdTurns, _) in unavailableAbilities)
        {
            var why = onCd ? $"[red]CD:{cdTurns}t[/]" : "[yellow]No mana[/]";
            opts.Add(($"[dim]✗ {Markup.Escape(a.Name)}  ({why})[/]", null));
        }

        opts.Add(("← Cancel", null));
        return NullableSelectionPrompt("[bold]✨ Abilities[/]", opts);
    }

    /// <inheritdoc/>
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables)
    {
        if (consumables.Count == 0) return null;
        var opts = consumables
            .Select(i => ($"🧪 {Markup.Escape(i.Name)}  [grey]({Markup.Escape(InputPrimaryStatLabel(i))})[/]", (Item?)i))
            .Append(("← Cancel", (Item?)null));
        return NullableSelectionPrompt("[bold]🎒 Use Item[/]", opts);
    }

    /// <inheritdoc/>
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
    {
        if (equippable.Count == 0) return null;
        var opts = equippable.Select(i =>
        {
            var tc = InputTierColor(i.Tier);
            return ($"{InputItemIcon(i)} [{tc}]{Markup.Escape(i.Name)}[/]  [grey]({Markup.Escape(InputPrimaryStatLabel(i))})[/]", (Item?)i);
        }).Append(("← Cancel", (Item?)null));
        return NullableSelectionPrompt("[bold]⚔  Equip Item[/]", opts);
    }

    /// <inheritdoc/>
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable)
    {
        if (usable.Count == 0) return null;
        var opts = usable
            .Select(i => ($"🧪 {Markup.Escape(i.Name)}  [grey]({Markup.Escape(InputPrimaryStatLabel(i))})[/]", (Item?)i))
            .Append(("← Cancel", (Item?)null));
        return NullableSelectionPrompt("[bold]🎒 Use Item[/]", opts);
    }

    /// <inheritdoc/>
    public TakeSelection? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems)
    {
        if (roomItems.Count == 0) return null;
        var opts = new List<(string, TakeSelection?)>
        {
            ("[bold]Take All[/]", new TakeSelection.All()),
        };
        foreach (var item in roomItems)
            opts.Add(($"{InputItemIcon(item)} {Markup.Escape(item.Name)}  [grey]({Markup.Escape(InputPrimaryStatLabel(item))})[/]", new TakeSelection.Single(item)));
        opts.Add(("← Cancel", null));
        return NullableSelectionPrompt("[bold]📦 Take Items[/]", opts);
    }

    /// <inheritdoc/>
    public StartupMenuOption ShowStartupMenu(bool hasSaves)
    {
        var opts = new List<(string, StartupMenuOption)>
        {
            ("🗡  New Game",            StartupMenuOption.NewGame),
        };
        if (hasSaves)
            opts.Add(("📂 Load Save",   StartupMenuOption.LoadSave));
        opts.Add(("🌱 New Game with Seed", StartupMenuOption.NewGameWithSeed));
        opts.Add(("🚪 Exit",            StartupMenuOption.Exit));
        return SelectionPromptValue("[bold yellow]⚔  D U N G N Z[/]", opts);
    }

    /// <inheritdoc/>
    public string? SelectSaveToLoad(string[] saveNames)
    {
        if (saveNames.Length == 0) return null;
        var opts = saveNames
            .Select(s => (s, (string?)s))
            .Append(("← Cancel", (string?)null));
        return NullableSelectionPrompt("[bold]📂 Load Save[/]", opts);
    }

    /// <inheritdoc/>
    public int? ReadSeed()
    {
        return PauseAndRun<int?>(() =>
        {
            var raw = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold yellow]Enter a 6-digit seed (100000–999999), or 'cancel':[/]")
                    .AllowEmpty()
                    .Validate(s =>
                    {
                        if (string.IsNullOrWhiteSpace(s) ||
                            s.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                            return ValidationResult.Success();
                        return int.TryParse(s, out int v) && v is >= 100000 and <= 999999
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Enter a number between 100000–999999, or 'cancel'.[/]");
                    }));

            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                return null;
            return int.Parse(raw);
        });
    }

    /// <inheritdoc/>
    public Skill? ShowSkillTreeMenu(Player player)
    {
        var opts = new List<(string Label, Skill? Value)>();

        foreach (var skill in Enum.GetValues<Skill>())
        {
            if (player.HasSkill(skill)) continue;

            var (minLevel, classRestriction) = SkillTree.GetSkillRequirements(skill);
            if (classRestriction.HasValue && player.Class != classRestriction.Value) continue;

            var desc = SkillTree.GetDescription(skill);
            if (player.Level >= minLevel)
                opts.Add(($"[green]✓[/] [bold]{skill}[/] [grey]— {Markup.Escape(desc)}[/]", (Skill?)skill));
            else
                opts.Add(($"[dim]✗ {skill}  (Req Lv.{minLevel}) — {Markup.Escape(desc)}[/]", null));
        }

        if (opts.Count == 0) return null;
        opts.Add(("← Cancel", null));

        return PauseAndRun(() =>
            AnsiConsole.Prompt(
                new SelectionPrompt<(string Label, Skill? Value)>()
                    .Title("[bold]✨ Skill Tree[/]")
                    .PageSize(15)
                    .UseConverter(o => o.Label)
                    .HighlightStyle(new Style(Color.Yellow))
                    .AddChoices(opts))
            .Value);
    }

    /// <inheritdoc/>
    public string? ReadCommandInput()
    {
        return PauseAndRun<string?>(() =>
            AnsiConsole.Prompt(
                new TextPrompt<string>("[grey]>[/]")
                    .AllowEmpty()));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Pause/resume helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pauses the Live render loop, executes <paramref name="action"/>, then resumes.
    /// Works with any return type including nullable value types (no constraints).
    /// </summary>
    private T PauseAndRun<T>(Func<T> action)
    {
        if (!_ctx.IsLiveActive) return action();
        
        bool isTopLevel = Interlocked.Increment(ref _pauseDepth) == 1;
        if (isTopLevel)
        {
            _pauseLiveEvent.Set();
            Thread.Sleep(100);
        }
        try { return action(); }
        finally 
        { 
            if (Interlocked.Decrement(ref _pauseDepth) == 0)
                _resumeLiveEvent.Set(); 
        }
    }

    // Generic selection prompt wrappers that use PauseAndRun

    private T SelectionPromptValue<T>(string title, IEnumerable<(string Label, T Value)> options)
        where T : notnull
    {
        var list = options.ToList();
        return PauseAndRun(() =>
            AnsiConsole.Prompt(
                new SelectionPrompt<(string Label, T Value)>()
                    .Title(title)
                    .PageSize(15)
                    .UseConverter(o => o.Label)
                    .HighlightStyle(new Style(Color.Yellow))
                    .AddChoices(list))
            .Value);
    }

    private T? NullableSelectionPrompt<T>(string title, IEnumerable<(string Label, T? Value)> options)
        where T : class
    {
        var list = options.ToList();
        return PauseAndRun(() =>
            AnsiConsole.Prompt(
                new SelectionPrompt<(string Label, T? Value)>()
                    .Title(title)
                    .PageSize(15)
                    .UseConverter(o => o.Label)
                    .HighlightStyle(new Style(Color.Yellow))
                    .AddChoices(list))
            .Value);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Display helpers (private, used only in this partial class)
    // ──────────────────────────────────────────────────────────────────────────

    private static string InputTierColor(ItemTier tier) => tier switch
    {
        ItemTier.Common    => "white",
        ItemTier.Uncommon  => "green",
        ItemTier.Rare      => "blue",
        ItemTier.Epic      => "purple",
        ItemTier.Legendary => "gold1",
        _                  => "grey",
    };

    private static string InputItemIcon(Item item) => item.Type switch
    {
        ItemType.Weapon           => "⚔",
        ItemType.Armor            => "🛡",
        ItemType.Accessory        => "💍",
        ItemType.Consumable       => "🧪",
        ItemType.CraftingMaterial => "⚗",
        _                         => "◆",
    };

    private static string InputPrimaryStatLabel(Item item)
    {
        if (item.AttackBonus  != 0) return $"+{item.AttackBonus} ATK";
        if (item.DefenseBonus != 0) return $"+{item.DefenseBonus} DEF";
        if (item.HealAmount   != 0) return $"+{item.HealAmount} HP";
        if (item.ManaRestore  != 0) return $"+{item.ManaRestore} MP";
        if (item.MaxManaBonus != 0) return $"+{item.MaxManaBonus} Max MP";
        return item.Type.ToString();
    }

    private static string InputClassIcon(PlayerClassDefinition def) => def.Name switch
    {
        "Warrior"     => "⚔",
        "Mage"        => "🔮",
        "Rogue"       => "🗡",
        "Paladin"     => "⛨",
        "Necromancer" => "☠",
        "Ranger"      => "🏹",
        _             => "◆",
    };

    // ──────────────────────────────────────────────────────────────────────────
    // Stubs for helpers referenced in Hill's display-only methods (main file).
    // Hill will replace these with full implementations; these unblock the build.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the Spectre markup color name for an item tier.</summary>
    private static string TierColor(ItemTier tier) => tier switch
    {
        ItemTier.Common    => "white",
        ItemTier.Uncommon  => "green",
        ItemTier.Rare      => "blue",
        ItemTier.Epic      => "purple",
        ItemTier.Legendary => "gold1",
        _                  => "grey",
    };

    /// <summary>Returns the primary-stat label string for an item (e.g. "+5 ATK").</summary>
    private static string PrimaryStatLabel(Item item)
    {
        if (item.AttackBonus  != 0) return $"Attack +{item.AttackBonus}";
        if (item.DefenseBonus != 0) return $"Defense +{item.DefenseBonus}";
        if (item.HealAmount   != 0) return $"Heals {item.HealAmount} HP";
        if (item.ManaRestore  != 0) return $"Mana +{item.ManaRestore}";
        if (item.MaxManaBonus != 0) return $"Max Mana +{item.MaxManaBonus}";
        return item.Type.ToString();
    }

    /// <summary>Returns a display name for the given room based on its type.</summary>
    private static string GetRoomDisplayName(Room room) => room.Type switch
    {
        RoomType.Dark             => "Dark Chamber",
        RoomType.Scorched         => "Scorched Hall",
        RoomType.Flooded          => "Flooded Passage",
        RoomType.Mossy            => "Mossy Alcove",
        RoomType.Ancient          => "Ancient Hall",
        RoomType.ForgottenShrine  => "Forgotten Shrine",
        RoomType.PetrifiedLibrary => "Petrified Library",
        RoomType.ContestedArmory  => "Contested Armory",
        _                         => "Room",
    };
}
