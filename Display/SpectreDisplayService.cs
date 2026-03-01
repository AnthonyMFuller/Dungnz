using Dungnz.Models;
using Dungnz.Systems;
using Spectre.Console;

namespace Dungnz.Display;

/// <summary>
/// Spectre.Console-backed implementation of IDisplayService.
/// Replaces hand-rolled ANSI with Spectre widgets.
/// </summary>
public sealed class SpectreDisplayService : IDisplayService
{
    /// <inheritdoc/>
    public void ShowTitle() =>
        throw new NotImplementedException("SpectreDisplayService.ShowTitle not yet implemented");

    /// <inheritdoc/>
    public void ShowRoom(Room room) =>
        throw new NotImplementedException("SpectreDisplayService.ShowRoom not yet implemented");

    /// <inheritdoc/>
    public void ShowCombat(string message) =>
        AnsiConsole.Write(new Rule($"[bold red]{Markup.Escape(message)}[/]"));

    /// <inheritdoc/>
    public void ShowCombatStatus(Player player, Enemy enemy,
        IReadOnlyList<ActiveEffect> playerEffects,
        IReadOnlyList<ActiveEffect> enemyEffects)
    {
        AnsiConsole.WriteLine();

        var table = new Table().NoBorder().Expand();
        table.AddColumn(new TableColumn("").NoWrap());
        table.AddColumn(new TableColumn("").NoWrap());

        // Player cell
        var playerCell = new System.Text.StringBuilder();
        playerCell.Append($"⚔  [bold]{Markup.Escape(player.Name)}[/]");
        playerCell.AppendLine();
        playerCell.Append($"HP: {BuildHpBar(player.HP, player.MaxHP)} {player.HP}/{player.MaxHP}");
        if (player.MaxMana > 0)
        {
            playerCell.AppendLine();
            playerCell.Append($"MP: [blue]{BuildBar(player.Mana, player.MaxMana)}[/] {player.Mana}/{player.MaxMana}");
        }
        if (playerEffects.Count > 0)
        {
            playerCell.AppendLine();
            foreach (var e in playerEffects)
            {
                var color = e.IsBuff ? "purple" : "red";
                playerCell.Append($"[{color}][{EffectIcon(e.Effect)}{Markup.Escape(e.Effect.ToString())} {e.RemainingTurns}t][/] ");
            }
        }

        // Enemy cell
        var enemyCell = new System.Text.StringBuilder();
        enemyCell.Append($"🐉 [bold]{Markup.Escape(enemy.Name)}[/]");
        enemyCell.AppendLine();
        enemyCell.Append($"HP: {BuildHpBar(enemy.HP, enemy.MaxHP)} {enemy.HP}/{enemy.MaxHP}");
        if (enemyEffects.Count > 0)
        {
            enemyCell.AppendLine();
            foreach (var e in enemyEffects)
            {
                var color = e.IsBuff ? "purple" : "red";
                enemyCell.Append($"[{color}][{EffectIcon(e.Effect)}{Markup.Escape(e.Effect.ToString())} {e.RemainingTurns}t][/] ");
            }
        }

        table.AddRow(new Markup(playerCell.ToString()), new Markup(enemyCell.ToString()));
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowCombatMessage(string message) =>
        AnsiConsole.MarkupLine($"  [white]{Markup.Escape(message)}[/]");

    /// <inheritdoc/>
    public void ShowPlayerStats(Player player) =>
        throw new NotImplementedException("SpectreDisplayService.ShowPlayerStats not yet implemented");

    /// <inheritdoc/>
    public void ShowInventory(Player player) =>
        throw new NotImplementedException("SpectreDisplayService.ShowInventory not yet implemented");

    /// <inheritdoc/>
    public void ShowLootDrop(Item item, Player player, bool isElite = false) =>
        throw new NotImplementedException("SpectreDisplayService.ShowLootDrop not yet implemented");

    /// <inheritdoc/>
    public void ShowGoldPickup(int amount, int newTotal) =>
        throw new NotImplementedException("SpectreDisplayService.ShowGoldPickup not yet implemented");

    /// <inheritdoc/>
    public void ShowItemPickup(Item item, int slotsCurrent, int slotsMax, int weightCurrent, int weightMax) =>
        throw new NotImplementedException("SpectreDisplayService.ShowItemPickup not yet implemented");

    /// <inheritdoc/>
    public void ShowItemDetail(Item item) =>
        throw new NotImplementedException("SpectreDisplayService.ShowItemDetail not yet implemented");

    /// <inheritdoc/>
    public void ShowMessage(string message) =>
        throw new NotImplementedException("SpectreDisplayService.ShowMessage not yet implemented");

    /// <inheritdoc/>
    public void ShowError(string message) =>
        throw new NotImplementedException("SpectreDisplayService.ShowError not yet implemented");

    /// <inheritdoc/>
    public void ShowHelp() =>
        throw new NotImplementedException("SpectreDisplayService.ShowHelp not yet implemented");

    /// <inheritdoc/>
    public void ShowCommandPrompt(Player? player = null) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCommandPrompt not yet implemented");

    /// <inheritdoc/>
    public void ShowMap(Room currentRoom) =>
        throw new NotImplementedException("SpectreDisplayService.ShowMap not yet implemented");

    /// <inheritdoc/>
    public string ReadPlayerName() =>
        throw new NotImplementedException("SpectreDisplayService.ReadPlayerName not yet implemented");

    /// <inheritdoc/>
    public void ShowColoredMessage(string message, string color) =>
        throw new NotImplementedException("SpectreDisplayService.ShowColoredMessage not yet implemented");

    /// <inheritdoc/>
    public void ShowColoredCombatMessage(string message, string color) =>
        throw new NotImplementedException("SpectreDisplayService.ShowColoredCombatMessage not yet implemented");

    /// <inheritdoc/>
    public void ShowColoredStat(string label, string value, string valueColor) =>
        throw new NotImplementedException("SpectreDisplayService.ShowColoredStat not yet implemented");

    /// <inheritdoc/>
    public void ShowEquipmentComparison(Player player, Item? oldItem, Item newItem) =>
        throw new NotImplementedException("SpectreDisplayService.ShowEquipmentComparison not yet implemented");

    /// <inheritdoc/>
    public void ShowEnhancedTitle() =>
        throw new NotImplementedException("SpectreDisplayService.ShowEnhancedTitle not yet implemented");

    /// <inheritdoc/>
    public bool ShowIntroNarrative() =>
        throw new NotImplementedException("SpectreDisplayService.ShowIntroNarrative not yet implemented");

    /// <inheritdoc/>
    public void ShowPrestigeInfo(PrestigeData prestige) =>
        throw new NotImplementedException("SpectreDisplayService.ShowPrestigeInfo not yet implemented");

    /// <inheritdoc/>
    public Difficulty SelectDifficulty() =>
        PromptFromMenu("[bold yellow]Choose your difficulty:[/]",
            new (string, Difficulty)[]
            {
                ("[green]CASUAL[/]     Weaker enemies · Cheap shops · Start with 50g + 3 potions", Difficulty.Casual),
                ("[yellow]NORMAL[/]     Balanced challenge · The intended experience · Start with 15g + 1 potion", Difficulty.Normal),
                ("[red]HARD[/]       Stronger enemies · Scarce rewards · No starting supplies · ☠ Permadeath", Difficulty.Hard),
            });

    /// <inheritdoc/>
    public PlayerClassDefinition SelectClass(PrestigeData? prestige)
    {
        var choices = PlayerClassDefinition.All.Select(def =>
        {
            var presBonus = prestige != null && prestige.PrestigeLevel > 0
                ? $" [yellow](+{prestige.BonusStartHP} HP, +{prestige.BonusStartAttack} ATK, +{prestige.BonusStartDefense} DEF prestige)[/]"
                : "";
            var label = $"{ClassIcon(def)} [bold]{Markup.Escape(def.Name),-12}[/] — {Markup.Escape(def.Description)}{presBonus}";
            return (label, def);
        });
        return PromptFromMenu("[bold yellow]Choose your class:[/]", choices);
    }

    /// <inheritdoc/>
    public void ShowShop(IEnumerable<(Item item, int price)> stock, int playerGold) =>
        throw new NotImplementedException("SpectreDisplayService.ShowShop not yet implemented");

    /// <inheritdoc/>
    public int ShowShopAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        var options = stockList
            .Select((s, i) => (
                $"{ItemTypeIcon(s.item.Type)} {Markup.Escape(s.item.Name)}  [grey]{Markup.Escape(PrimaryStatLabel(s.item))}[/]  [yellow]{s.price}g[/]",
                i + 1))
            .Append(("[grey]Cancel[/]", 0));
        return PromptFromMenu("[bold yellow]Buy an item:[/]", options);
    }

    /// <inheritdoc/>
    public void ShowSellMenu(IEnumerable<(Item item, int sellPrice)> items, int playerGold) =>
        throw new NotImplementedException("SpectreDisplayService.ShowSellMenu not yet implemented");

    /// <inheritdoc/>
    public int ShowSellMenuAndSelect(IEnumerable<(Item item, int sellPrice)> items, int playerGold)
    {
        var itemList = items.ToList();
        var options = itemList
            .Select((s, i) => (
                $"{ItemTypeIcon(s.item.Type)} {Markup.Escape(s.item.Name)}  [grey]{Markup.Escape(PrimaryStatLabel(s.item))}[/]  [green]+{s.sellPrice}g[/]",
                i + 1))
            .Append(("[grey]Cancel[/]", 0));
        return PromptFromMenu("[bold yellow]Sell an item:[/]", options);
    }

    /// <inheritdoc/>
    public void ShowCraftRecipe(string recipeName, Item result, List<(string ingredient, bool playerHasIt)> ingredients) =>
        throw new NotImplementedException("SpectreDisplayService.ShowCraftRecipe not yet implemented");

    /// <inheritdoc/>
    public void ShowCombatStart(Enemy enemy)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule($"[bold red]⚔  COMBAT BEGINS  ⚔[/]");
        rule.Style = Style.Parse("red");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine($"[bold red]  {Markup.Escape(enemy.Name)}[/]");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void ShowCombatEntryFlags(Enemy enemy)
    {
        if (enemy.IsElite)
            AnsiConsole.MarkupLine("  [yellow]⭐ ELITE — enhanced stats and loot[/]");
        if (enemy is Dungnz.Systems.Enemies.DungeonBoss boss && boss.IsEnraged)
            AnsiConsole.MarkupLine("  [bold red]⚡ ENRAGED[/]");
    }

    /// <inheritdoc/>
    public void ShowLevelUpChoice(Player player) =>
        throw new NotImplementedException("SpectreDisplayService.ShowLevelUpChoice not yet implemented");

    /// <inheritdoc/>
    public void ShowFloorBanner(int floor, int maxFloor, DungeonVariant variant) =>
        throw new NotImplementedException("SpectreDisplayService.ShowFloorBanner not yet implemented");

    /// <inheritdoc/>
    public void ShowEnemyDetail(Enemy enemy) =>
        throw new NotImplementedException("SpectreDisplayService.ShowEnemyDetail not yet implemented");

    /// <inheritdoc/>
    public void ShowVictory(Player player, int floorsCleared, RunStats stats) =>
        throw new NotImplementedException("SpectreDisplayService.ShowVictory not yet implemented");

    /// <inheritdoc/>
    public void ShowGameOver(Player player, string? killedBy, RunStats stats) =>
        throw new NotImplementedException("SpectreDisplayService.ShowGameOver not yet implemented");

    /// <inheritdoc/>
    public void ShowEnemyArt(Enemy enemy)
    {
        if (enemy.AsciiArt == null || enemy.AsciiArt.Length == 0)
            return;

        var artText = string.Join("\n", enemy.AsciiArt.Select(l => Markup.Escape(l)));
        var artColor = enemy.IsElite ? "yellow" : "red";
        var panel = new Panel($"[{artColor}]{artText}[/]")
        {
            Header = new PanelHeader($"[bold red]{Markup.Escape(enemy.Name)}[/]"),
            Border = BoxBorder.Rounded,
        };
        AnsiConsole.Write(panel);
    }

    /// <inheritdoc/>
    public int ShowLevelUpChoiceAndSelect(Player player) =>
        PromptFromMenu("[bold yellow]★ Choose a stat bonus:[/]",
            new (string, int)[]
            {
                ($"+5 Max HP     [grey]({player.MaxHP} → {player.MaxHP + 5})[/]", 1),
                ($"+2 Attack     [grey]({player.Attack} → {player.Attack + 2})[/]", 2),
                ($"+2 Defense    [grey]({player.Defense} → {player.Defense + 2})[/]", 3),
            });

    /// <inheritdoc/>
    public string ShowCombatMenuAndSelect(Player player, Enemy enemy)
    {
        var ctx = new System.Text.StringBuilder($"Mana: {player.Mana}/{player.MaxMana}");
        if (player.Class == PlayerClass.Rogue)
        {
            var dots = new string('●', player.ComboPoints) + new string('○', 5 - player.ComboPoints);
            ctx.Append($"  ⚡ Combo: {dots}");
        }
        if (player.Class == PlayerClass.Mage && player.IsManaShieldActive)
            ctx.Append(" [SHIELD ACTIVE]");
        if (player.Class == PlayerClass.Paladin && player.DivineShieldTurnsRemaining > 0)
            ctx.Append($" [DIVINE SHIELD: {player.DivineShieldTurnsRemaining}T]");
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(ctx.ToString())}[/]");
        return PromptFromMenu("[bold yellow]Choose your action:[/]",
            new (string, string)[]
            {
                ("⚔  Attack",  "A"),
                ("✨ Ability", "B"),
                ("🏃 Flee",    "F"),
                ("🧪 Use Item","I"),
            });
    }

    /// <inheritdoc/>
    public int ShowCraftMenuAndSelect(IEnumerable<(string recipeName, bool canCraft)> recipes)
    {
        var recipeList = recipes.ToList();
        var options = recipeList
            .Select((r, i) => (
                r.canCraft
                    ? $"[green]✅ {Markup.Escape(r.recipeName)}[/]"
                    : $"[red]❌ {Markup.Escape(r.recipeName)}[/]",
                i + 1))
            .Append(("[grey]↩  Cancel[/]", 0));
        return PromptFromMenu("[bold yellow]=== CRAFTING — Choose a recipe ===[/]", options);
    }

    /// <inheritdoc/>
    public int ShowShrineMenuAndSelect(int playerGold, int healCost = 30, int blessCost = 50, int fortifyCost = 75, int meditateCost = 75) =>
        PromptFromMenu("[bold yellow]✨ [[Shrine Menu]][/]",
            new (string, int)[]
            {
                ($"Heal fully        — [yellow]{healCost}g[/]  [grey](Your gold: {playerGold}g)[/]", 1),
                ($"Bless             — [yellow]{blessCost}g[/]  [grey](+2 ATK/DEF permanently)[/]", 2),
                ($"Fortify           — [yellow]{fortifyCost}g[/]  [grey](MaxHP +10, permanent)[/]", 3),
                ($"Meditate          — [yellow]{meditateCost}g[/]  [grey](MaxMana +10, permanent)[/]", 4),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public int ShowShopWithSellAndSelect(IEnumerable<(Item item, int price)> stock, int playerGold)
    {
        var stockList = stock.ToList();
        var options = stockList
            .Select((s, i) => (
                $"{ItemTypeIcon(s.item.Type)} {Markup.Escape(s.item.Name)}  [grey]{Markup.Escape(PrimaryStatLabel(s.item))}[/]  [yellow]{s.price}g[/]",
                i + 1))
            .Append(("[yellow]💰 Sell Items[/]", -1))
            .Append(("[grey]Leave[/]", 0));
        return PromptFromMenu("[bold yellow]Merchant — what would you like?[/]", options);
    }

    /// <inheritdoc/>
    public bool ShowConfirmMenu(string prompt) =>
        PromptFromMenu($"[bold yellow]{Markup.Escape(prompt)}[/]",
            new (string, bool)[]
            {
                ("[green]Yes[/]", true),
                ("[grey]No[/]", false),
            });

    /// <inheritdoc/>
    public int ShowTrapChoiceAndSelect(string header, string option1, string option2) =>
        PromptFromMenu($"[bold yellow]{Markup.Escape(header)}[/]",
            new (string, int)[]
            {
                (Markup.Escape(option1), 1),
                (Markup.Escape(option2), 2),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public int ShowForgottenShrineMenuAndSelect() =>
        PromptFromMenu("[bold yellow]🕯 [[Forgotten Shrine]] — choose a blessing:[/]",
            new (string, int)[]
            {
                ("Holy Strength   — [grey]+5 ATK (lasts until next floor)[/]", 1),
                ("Sacred Ground   — [grey]Auto-heal at shrines[/]", 2),
                ("Warding Veil    — [grey]20% chance to deflect enemy attacks this floor[/]", 3),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public int ShowContestedArmoryMenuAndSelect(int playerDefense) =>
        PromptFromMenu("[bold yellow]⚔ [[Contested Armory]] — how do you approach?[/]",
            new (string, int)[]
            {
                ($"Careful approach — [grey]disarm traps (requires DEF > 12, yours: {playerDefense})[/]", 1),
                ("Reckless grab   — [grey]take what you can (15-30 damage)[/]", 2),
                ("[grey]Leave[/]", 0),
            });

    /// <inheritdoc/>
    public Ability? ShowAbilityMenuAndSelect(
        IEnumerable<(Ability ability, bool onCooldown, int cooldownTurns, bool notEnoughMana)> unavailableAbilities,
        IEnumerable<Ability> availableAbilities)
    {
        foreach (var (ability, onCooldown, cooldownTurns, notEnoughMana) in unavailableAbilities)
        {
            if (onCooldown)
                AnsiConsole.MarkupLine($"  [grey]○ {Markup.Escape(ability.Name)} — Cooldown: {cooldownTurns} turns (Cost: {ability.ManaCost} MP)[/]");
            else if (notEnoughMana)
                AnsiConsole.MarkupLine($"  [red]○ {Markup.Escape(ability.Name)} — Need {ability.ManaCost} MP (Cost: {ability.ManaCost} MP)[/]");
        }
        var availList = availableAbilities.ToList();
        var options = availList
            .Select(a => (
                $"{Markup.Escape(a.Name)} — [grey]{Markup.Escape(a.Description)} (Cost: {a.ManaCost} MP)[/]",
                (Ability?)a))
            .Append(("[grey]Cancel[/]", (Ability?)null));
        return PromptFromMenu("[bold yellow]=== Abilities ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables)
    {
        var options = consumables
            .Select(item =>
            {
                var manaStr = item.ManaRestore > 0 ? $" [blue]+{item.ManaRestore} MP[/]" : "";
                return ($"🧪 {Markup.Escape(item.Name)} [green](+{item.HealAmount} HP)[/]{manaStr}", (Item?)item);
            })
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== USE ITEM — Choose a consumable ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowEquipMenuAndSelect(IReadOnlyList<Item> equippable)
    {
        var options = equippable
            .Select(item => (
                $"{ItemTypeIcon(item.Type)} {Markup.Escape(item.Name)}  [grey][[{Markup.Escape(PrimaryStatLabel(item))}]][/]",
                (Item?)item))
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== EQUIP — Choose an item ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowUseMenuAndSelect(IReadOnlyList<Item> usable)
    {
        var options = usable
            .Select(item => (
                $"{ItemTypeIcon(item.Type)} {Markup.Escape(item.Name)}  [grey][[{Markup.Escape(PrimaryStatLabel(item))}]][/]",
                (Item?)item))
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== Use which item? ===[/]", options);
    }

    /// <inheritdoc/>
    public Item? ShowTakeMenuAndSelect(IReadOnlyList<Item> roomItems)
    {
        var sentinel = new Item { Name = "__TAKE_ALL__" };
        var options = roomItems
            .Select(item => (
                $"{ItemTypeIcon(item.Type)} {Markup.Escape(item.Name)}  [grey][[{Markup.Escape(PrimaryStatLabel(item))}]][/]",
                (Item?)item))
            .Prepend(("[yellow]📦 Take All[/]", (Item?)sentinel))
            .Append(("[grey]↩  Cancel[/]", (Item?)null));
        return PromptFromMenu("[bold yellow]=== TAKE — Choose an item ===[/]", options);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static T PromptFromMenu<T>(string title, IEnumerable<(string Label, T Value)> options)
    {
        var optList = options.ToList();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<(string Label, T Value)>()
                .Title(title)
                .UseConverter(o => o.Label)
                .AddChoices(optList));
        return selected.Value;
    }

    private static string ItemTypeIcon(ItemType type) => type switch
    {
        ItemType.Weapon           => "⚔",
        ItemType.Armor            => "🛡",
        ItemType.Consumable       => "🧪",
        ItemType.Accessory        => "💍",
        ItemType.CraftingMaterial => "⚗",
        _                         => "•"
    };

    private static string PrimaryStatLabel(Item item)
    {
        if (item.AttackBonus  != 0) return $"Attack +{item.AttackBonus}";
        if (item.DefenseBonus != 0) return $"Defense +{item.DefenseBonus}";
        if (item.HealAmount   != 0) return $"Heals {item.HealAmount} HP";
        if (item.ManaRestore  != 0) return $"Mana +{item.ManaRestore}";
        if (item.MaxManaBonus != 0) return $"Max Mana +{item.MaxManaBonus}";
        if (item.DodgeBonus   >  0) return $"Dodge +{item.DodgeBonus:P0}";
        if (item.StatModifier != 0) return $"HP +{item.StatModifier}";
        return item.Type.ToString();
    }

    private static string ClassIcon(PlayerClassDefinition def) => def.Class switch
    {
        PlayerClass.Warrior     => "⚔",
        PlayerClass.Mage        => "🔮",
        PlayerClass.Rogue       => "🗡",
        PlayerClass.Paladin     => "🛡",
        PlayerClass.Necromancer => "💀",
        PlayerClass.Ranger      => "🏹",
        _                       => "•"
    };

    private static string BuildBar(int current, int max, int width = 10)
    {
        current = Math.Clamp(current, 0, Math.Max(max, 1));
        int filled = max > 0 ? (int)Math.Round((double)current / max * width) : 0;
        return new string('█', filled) + new string('░', width - filled);
    }

    private static string BuildHpBar(int current, int max, int width = 10)
    {
        var bar = BuildBar(current, max, width);
        double pct = max > 0 ? (double)current / max : 0;
        var color = pct > 0.5 ? "green" : pct >= 0.25 ? "yellow" : "red";
        return $"[{color}]{bar}[/]";
    }

    private static string EffectIcon(StatusEffect effect) => effect switch
    {
        StatusEffect.Poison    => "☠",
        StatusEffect.Bleed     => "🩸",
        StatusEffect.Stun      => "⚡",
        StatusEffect.Regen     => "✨",
        StatusEffect.Fortified => "🛡",
        StatusEffect.Weakened  => "💀",
        StatusEffect.Slow      => ">",
        StatusEffect.BattleCry => "!",
        StatusEffect.Burn      => "*",
        StatusEffect.Freeze    => "~",
        StatusEffect.Silence   => "X",
        StatusEffect.Curse     => "@",
        _                      => "●"
    };
}
