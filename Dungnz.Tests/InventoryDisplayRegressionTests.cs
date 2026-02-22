using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// UI regression tests for a 20-item (full-capacity) mixed inventory.
/// Exercises ShowInventory, ShowLootDrop, ShowItemDetail, and equip/unequip flows
/// across all ItemType values (Weapon, Armor, Accessory, Consumable) and all
/// ItemTier values (Common, Uncommon, Rare).
///
/// Covers:
///  — No exceptions thrown for any display path with a full, mixed inventory
///  — Correct tier color codes (Green=Uncommon, BrightCyan=Rare) appear in output
///  — Common items are NOT wrapped in Uncommon/Rare color codes
///  — Long item names (>30 chars) are truncated to 27+"..." — no double truncation
///  — Equip and unequip cycles produce no null-ref exceptions
/// </summary>
[Collection("console-output")]
public class InventoryDisplayRegressionTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly TextWriter _originalOut;
    private readonly ConsoleDisplayService _svc;

    private const string BrightCyan = "\u001b[96m";
    private const string Green      = "\u001b[32m";
    private const string BrightWhite = "\u001b[97m";

    public InventoryDisplayRegressionTests()
    {
        _originalOut = Console.Out;
        _output      = new StringWriter();
        Console.SetOut(_output);
        _svc         = new ConsoleDisplayService();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _output.Dispose();
    }

    private string Output => _output.ToString();

    // ─────────────────────────────────────────────────────────────────────────
    // Shared test data
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns exactly 20 items (the max inventory capacity) that span every
    /// ItemType and every ItemTier, including items with long names designed to
    /// trigger the TruncateName(name, 30) path in DisplayService.
    /// </summary>
    private static List<Item> BuildFullInventory() => new()
    {
        // ── Weapons — Common ─────────────────────────────────────────────────
        new Item { Name = "Rusty Dagger",              Type = ItemType.Weapon,     Tier = ItemTier.Common,   AttackBonus = 2,  IsEquippable = true,  Weight = 1 },
        new Item { Name = "Cracked Shortsword",        Type = ItemType.Weapon,     Tier = ItemTier.Common,   AttackBonus = 4,  IsEquippable = true,  Weight = 2 },
        // ── Weapons — Uncommon ───────────────────────────────────────────────
        new Item { Name = "Steel Longsword",           Type = ItemType.Weapon,     Tier = ItemTier.Uncommon, AttackBonus = 9,  IsEquippable = true,  Weight = 2, Description = "A finely balanced blade." },
        new Item { Name = "Elven Recurve Bow",         Type = ItemType.Weapon,     Tier = ItemTier.Uncommon, AttackBonus = 11, IsEquippable = true,  Weight = 2 },
        // ── Weapons — Rare ───────────────────────────────────────────────────
        new Item { Name = "Void Blade",                Type = ItemType.Weapon,     Tier = ItemTier.Rare,     AttackBonus = 18, IsEquippable = true,  Weight = 3, AppliesBleedOnHit = true },
        new Item { Name = "Ancient Runic Greatsword",  Type = ItemType.Weapon,     Tier = ItemTier.Rare,     AttackBonus = 20, IsEquippable = true,  Weight = 4, Description = "Whispers fill the air when drawn." },

        // ── Armor — Common ───────────────────────────────────────────────────
        new Item { Name = "Tattered Robe",             Type = ItemType.Armor,      Tier = ItemTier.Common,   DefenseBonus = 1, IsEquippable = true,  Weight = 1 },
        new Item { Name = "Worn Leather Vest",         Type = ItemType.Armor,      Tier = ItemTier.Common,   DefenseBonus = 3, IsEquippable = true,  Weight = 2 },
        // ── Armor — Uncommon ─────────────────────────────────────────────────
        new Item { Name = "Chainmail Hauberk",         Type = ItemType.Armor,      Tier = ItemTier.Uncommon, DefenseBonus = 7, IsEquippable = true,  Weight = 3 },
        new Item { Name = "Shadow Cloak",              Type = ItemType.Armor,      Tier = ItemTier.Uncommon, DefenseBonus = 6, IsEquippable = true,  Weight = 2, DodgeBonus = 0.05f },
        // ── Armor — Rare ─────────────────────────────────────────────────────
        new Item { Name = "Dragonscale Plate Armor",   Type = ItemType.Armor,      Tier = ItemTier.Rare,     DefenseBonus = 15, IsEquippable = true, Weight = 5, PoisonImmunity = true },

        // ── Accessories — Common ─────────────────────────────────────────────
        new Item { Name = "Copper Ring",               Type = ItemType.Accessory,  Tier = ItemTier.Common,   StatModifier = 0, IsEquippable = true,  Weight = 1 },
        // ── Accessories — Uncommon ───────────────────────────────────────────
        new Item { Name = "Silver Pendant of Warding", Type = ItemType.Accessory,  Tier = ItemTier.Uncommon, MaxManaBonus = 10, IsEquippable = true, Weight = 1, Description = "Faint blue glow." },
        // ── Accessories — Rare ───────────────────────────────────────────────
        new Item { Name = "Amulet of Eternal Fortitude", Type = ItemType.Accessory, Tier = ItemTier.Rare,   StatModifier = 5, IsEquippable = true,  Weight = 1, DodgeBonus = 0.10f, MaxManaBonus = 20 },

        // ── Consumables — Common ─────────────────────────────────────────────
        new Item { Name = "Health Potion",             Type = ItemType.Consumable, Tier = ItemTier.Common,   HealAmount = 25,  Weight = 1 },
        new Item { Name = "Weak Antidote",             Type = ItemType.Consumable, Tier = ItemTier.Common,   HealAmount = 5,   Weight = 1 },
        // ── Consumables — Uncommon ───────────────────────────────────────────
        new Item { Name = "Greater Healing Draught",   Type = ItemType.Consumable, Tier = ItemTier.Uncommon, HealAmount = 60,  Weight = 1, ManaRestore = 10 },
        // ── Consumables — Rare ───────────────────────────────────────────────
        new Item { Name = "Elixir of Unstoppable Might", Type = ItemType.Consumable, Tier = ItemTier.Rare,  HealAmount = 100, Weight = 1, ManaRestore = 50 },

        // ── Long-name edge cases (trigger TruncateName at ≥30 chars) ─────────
        new Item { Name = "The Sword of Endless Shadow Despair", Type = ItemType.Weapon, Tier = ItemTier.Common,   AttackBonus = 3,  IsEquippable = true, Weight = 2 },
        new Item { Name = "Obsidian Tower Shield of Ages",       Type = ItemType.Armor,  Tier = ItemTier.Uncommon, DefenseBonus = 8, IsEquippable = true, Weight = 4 },
    };

    private static Player BuildPlayerWithFullInventory()
    {
        var player = new Player { Name = "Romanoff", HP = 100, MaxHP = 100 };
        foreach (var item in BuildFullInventory())
            player.Inventory.Add(item);
        return player;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowInventory — no-throw & presence of all tier colors
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowInventory_FullMixedInventory_DoesNotThrow()
    {
        // Arrange
        var player = BuildPlayerWithFullInventory();

        // Act / Assert
        var act = () => _svc.ShowInventory(player);
        act.Should().NotThrow("ShowInventory must handle a full mixed-tier inventory without exceptions");
    }

    [Fact]
    public void ShowInventory_FullInventory_ContainsBrightCyanForRareItems()
    {
        // Arrange
        var player = BuildPlayerWithFullInventory();

        // Act
        _svc.ShowInventory(player);

        // Assert — at least one Rare item must produce BrightCyan in the output
        Output.Should().Contain(BrightCyan,
            because: "Rare items in a full inventory must be displayed with BrightCyan color code");
    }

    [Fact]
    public void ShowInventory_FullInventory_ContainsGreenForUncommonItems()
    {
        // Arrange
        var player = BuildPlayerWithFullInventory();

        // Act
        _svc.ShowInventory(player);

        // Assert — at least one Uncommon item must produce Green on its name
        Output.Should().Contain($"{Green}Steel Longsword",
            because: "Uncommon items must have their names wrapped in Green in the inventory list");
    }

    [Fact]
    public void ShowInventory_FullInventory_CommonItemsNotWrappedInRareColor()
    {
        // Arrange
        var player = BuildPlayerWithFullInventory();

        // Act
        _svc.ShowInventory(player);

        // Assert — Common item names must NOT be immediately preceded by BrightCyan
        Output.Should().NotContain($"{BrightCyan}Rusty Dagger",
            because: "Common items must not use the Rare BrightCyan color on their name");
        Output.Should().NotContain($"{BrightCyan}Health Potion",
            because: "Common consumables must not use the Rare BrightCyan color on their name");
    }

    [Fact]
    public void ShowInventory_FullInventory_CommonItemsNotWrappedInUncommonGreen()
    {
        // Arrange
        var player = BuildPlayerWithFullInventory();

        // Act
        _svc.ShowInventory(player);

        // Assert — Common item names must NOT be immediately preceded by the Uncommon Green code
        Output.Should().NotContain($"{Green}Rusty Dagger",
            because: "Common items must not be colored with the Uncommon Green code");
        Output.Should().NotContain($"{Green}Health Potion",
            because: "Common consumables must not be colored with the Uncommon Green code");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowInventory — name truncation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowInventory_ItemNameExceeds30Chars_OutputContainsEllipsis()
    {
        // Arrange — "The Sword of Endless Shadow Despair" (36 chars) must be truncated
        var player = BuildPlayerWithFullInventory();

        // Act
        _svc.ShowInventory(player);

        // Assert — truncated name must appear with ellipsis marker
        Output.Should().Contain("The Sword of Endless Shadow...",
            because: "item names longer than 30 chars must be truncated to 27 chars + '...'");
    }

    [Fact]
    public void ShowInventory_LongItemName_NoDoubleTruncationArtifact()
    {
        // Arrange
        var player = BuildPlayerWithFullInventory();

        // Act
        _svc.ShowInventory(player);

        // Assert — TruncateName must not be applied twice, producing "......""
        Output.Should().NotContain("......",
            because: "TruncateName must be idempotent — applying it twice must not create '......' artifacts");
    }

    [Fact]
    public void ShowInventory_LongItemName_FullNameNotInOutput()
    {
        // Arrange — full name is 36 chars
        const string fullName = "The Sword of Endless Shadow Despair";
        var player = BuildPlayerWithFullInventory();

        // Act
        _svc.ShowInventory(player);

        // Assert — the full untruncated name must not appear (it was truncated)
        Output.Should().NotContain(fullName,
            because: "item names >30 chars must be truncated in inventory display");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowItemDetail — all types and tiers
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Rusty Dagger",              ItemType.Weapon,     ItemTier.Common,   2,  0,  0)]
    [InlineData("Steel Longsword",           ItemType.Weapon,     ItemTier.Uncommon, 9,  0,  0)]
    [InlineData("Void Blade",               ItemType.Weapon,     ItemTier.Rare,     18, 0,  0)]
    [InlineData("Worn Leather Vest",         ItemType.Armor,      ItemTier.Common,   0,  3,  0)]
    [InlineData("Chainmail Hauberk",         ItemType.Armor,      ItemTier.Uncommon, 0,  7,  0)]
    [InlineData("Dragonscale Plate Armor",   ItemType.Armor,      ItemTier.Rare,     0,  15, 0)]
    [InlineData("Copper Ring",               ItemType.Accessory,  ItemTier.Common,   0,  0,  0)]
    [InlineData("Silver Pendant of Warding", ItemType.Accessory,  ItemTier.Uncommon, 0,  0,  10)]
    [InlineData("Amulet of Eternal Fortitude", ItemType.Accessory, ItemTier.Rare,    0,  0,  20)]
    [InlineData("Health Potion",             ItemType.Consumable, ItemTier.Common,   0,  0,  0)]
    [InlineData("Greater Healing Draught",   ItemType.Consumable, ItemTier.Uncommon, 0,  0,  0)]
    [InlineData("Elixir of Unstoppable Might", ItemType.Consumable, ItemTier.Rare,   0,  0,  0)]
    public void ShowItemDetail_AllTypesAndTiers_DoNotThrow(
        string name, ItemType type, ItemTier tier, int atk, int def, int mana)
    {
        // Arrange
        var item = new Item
        {
            Name         = name,
            Type         = type,
            Tier         = tier,
            AttackBonus  = atk,
            DefenseBonus = def,
            MaxManaBonus = mana
        };

        // Act / Assert
        var act = () => _svc.ShowItemDetail(item);
        act.Should().NotThrow(
            because: $"ShowItemDetail must handle a {tier} {type} '{name}' without exceptions");
    }

    [Fact]
    public void ShowItemDetail_RareItem_OutputContainsBrightCyan()
    {
        // Arrange
        var item = new Item { Name = "Void Blade", Type = ItemType.Weapon, Tier = ItemTier.Rare, AttackBonus = 18 };

        // Act
        _svc.ShowItemDetail(item);

        // Assert — Rare tier must color the title with BrightCyan
        Output.Should().Contain(BrightCyan,
            because: "ShowItemDetail must render Rare item titles in BrightCyan");
    }

    [Fact]
    public void ShowItemDetail_UncommonItem_OutputContainsGreen()
    {
        // Arrange
        var item = new Item { Name = "Chainmail Hauberk", Type = ItemType.Armor, Tier = ItemTier.Uncommon, DefenseBonus = 7 };

        // Act
        _svc.ShowItemDetail(item);

        // Assert
        Output.Should().Contain(Green,
            because: "ShowItemDetail must render Uncommon item titles in Green");
    }

    [Fact]
    public void ShowItemDetail_LongItemName_TitleTruncatedWithEllipsis()
    {
        // Arrange — 36-char name must be truncated in the title box
        var item = new Item
        {
            Name = "The Sword of Endless Shadow Despair",
            Type = ItemType.Weapon,
            Tier = ItemTier.Common,
            AttackBonus = 3
        };

        // Act
        _svc.ShowItemDetail(item);

        // Assert — title must contain truncated form
        Output.Should().Contain("THE SWORD OF ENDLESS SHADOW...",
            because: "ShowItemDetail must apply TruncateName to the title to prevent box overflow");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowLootDrop — all types and tiers
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ItemTier.Common)]
    [InlineData(ItemTier.Uncommon)]
    [InlineData(ItemTier.Rare)]
    public void ShowLootDrop_AllTiers_Weapon_DoNotThrow(ItemTier tier)
    {
        // Arrange
        var item = new Item { Name = "Test Weapon", Type = ItemType.Weapon, Tier = tier, AttackBonus = 5 };

        // Act / Assert
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow($"ShowLootDrop must not throw for a {tier} Weapon");
    }

    [Theory]
    [InlineData(ItemTier.Common)]
    [InlineData(ItemTier.Uncommon)]
    [InlineData(ItemTier.Rare)]
    public void ShowLootDrop_AllTiers_Armor_DoNotThrow(ItemTier tier)
    {
        // Arrange
        var item = new Item { Name = "Test Armor", Type = ItemType.Armor, Tier = tier, DefenseBonus = 5 };

        // Act / Assert
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow($"ShowLootDrop must not throw for a {tier} Armor");
    }

    [Theory]
    [InlineData(ItemTier.Common)]
    [InlineData(ItemTier.Uncommon)]
    [InlineData(ItemTier.Rare)]
    public void ShowLootDrop_AllTiers_Accessory_DoNotThrow(ItemTier tier)
    {
        // Arrange
        var item = new Item { Name = "Test Accessory", Type = ItemType.Accessory, Tier = tier };

        // Act / Assert
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow($"ShowLootDrop must not throw for a {tier} Accessory");
    }

    [Theory]
    [InlineData(ItemTier.Common)]
    [InlineData(ItemTier.Uncommon)]
    [InlineData(ItemTier.Rare)]
    public void ShowLootDrop_AllTiers_Consumable_DoNotThrow(ItemTier tier)
    {
        // Arrange
        var item = new Item { Name = "Test Potion", Type = ItemType.Consumable, Tier = tier, HealAmount = 30 };

        // Act / Assert
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow($"ShowLootDrop must not throw for a {tier} Consumable");
    }

    [Fact]
    public void ShowLootDrop_RareItem_TierLabelContainsBrightCyan()
    {
        // Arrange
        var item = new Item { Name = "Void Blade", Type = ItemType.Weapon, Tier = ItemTier.Rare, AttackBonus = 18 };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — the [Rare] tier badge must use BrightCyan
        Output.Should().Contain(BrightCyan,
            because: "ShowLootDrop must render the Rare tier badge and item name in BrightCyan");
    }

    [Fact]
    public void ShowLootDrop_UncommonItem_TierLabelContainsGreen()
    {
        // Arrange
        var item = new Item { Name = "Steel Longsword", Type = ItemType.Weapon, Tier = ItemTier.Uncommon, AttackBonus = 9 };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — the [Uncommon] tier badge must use Green
        Output.Should().Contain(Green,
            because: "ShowLootDrop must render the Uncommon tier badge in Green");
    }

    [Fact]
    public void ShowLootDrop_CommonItem_NoBrightCyanInOutput()
    {
        // Arrange
        var item = new Item { Name = "Rusty Dagger", Type = ItemType.Weapon, Tier = ItemTier.Common, AttackBonus = 2 };

        // Act
        _svc.ShowLootDrop(item, new Player());

        // Assert — Common loot cards must never use the Rare BrightCyan code
        Output.Should().NotContain(BrightCyan,
            because: "ShowLootDrop for a Common item must not emit BrightCyan");
    }

    [Fact]
    public void ShowLootDrop_LongItemName_DoesNotThrowAndTruncates()
    {
        // Arrange — 36-char name exceeds TruncateName limit of 30
        var item = new Item
        {
            Name        = "The Sword of Endless Shadow Despair",
            Type        = ItemType.Weapon,
            Tier        = ItemTier.Common,
            AttackBonus = 3
        };

        // Act / Assert — no exception
        var act = () => _svc.ShowLootDrop(item, new Player());
        act.Should().NotThrow("a long item name must not crash ShowLootDrop");

        // Assert — truncated form appears, full form does not
        Output.Should().Contain("The Sword of Endless Shadow...",
            because: "names >30 chars must be truncated in the loot drop card");
        Output.Should().NotContain("The Sword of Endless Shadow Despair",
            because: "the full untruncated 36-char name must not appear in the loot drop card");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Equip / Unequip flows via EquipmentManager
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void HandleEquip_ThenHandleUnequip_AllTierWeapons_NoExceptions()
    {
        // Arrange
        var fake    = new FakeDisplayService();
        var manager = new EquipmentManager(fake);
        var weapons = BuildFullInventory()
            .Where(i => i.Type == ItemType.Weapon && i.IsEquippable)
            .ToList();

        var player = new Player { Name = "Romanoff", HP = 100, MaxHP = 100 };
        foreach (var w in weapons)
            player.Inventory.Add(w);

        // Act — equip each weapon in turn (tests the equip→swap path as well)
        var equipAct = () =>
        {
            foreach (var w in weapons)
                manager.HandleEquip(player, w.Name);
        };
        equipAct.Should().NotThrow("equipping weapons of every tier must not throw");

        // Final equipped weapon should be the last one we equipped
        player.EquippedWeapon.Should().NotBeNull();

        // Unequip
        var unequipAct = () => manager.HandleUnequip(player, "weapon");
        unequipAct.Should().NotThrow("unequipping the weapon slot must not throw");

        player.EquippedWeapon.Should().BeNull(
            because: "unequip must clear the weapon slot");
    }

    [Fact]
    public void HandleEquip_ThenHandleUnequip_AllTierArmors_NoExceptions()
    {
        // Arrange
        var fake    = new FakeDisplayService();
        var manager = new EquipmentManager(fake);
        var armors  = BuildFullInventory()
            .Where(i => i.Type == ItemType.Armor && i.IsEquippable)
            .ToList();

        var player = new Player { Name = "Romanoff", HP = 100, MaxHP = 100 };
        foreach (var a in armors)
            player.Inventory.Add(a);

        // Act
        var equipAct = () =>
        {
            foreach (var a in armors)
                manager.HandleEquip(player, a.Name);
        };
        equipAct.Should().NotThrow("equipping armors of every tier must not throw");
        player.EquippedArmor.Should().NotBeNull();

        var unequipAct = () => manager.HandleUnequip(player, "armor");
        unequipAct.Should().NotThrow("unequipping the armor slot must not throw");
        player.EquippedArmor.Should().BeNull();
    }

    [Fact]
    public void HandleEquip_ThenHandleUnequip_AllTierAccessories_NoExceptions()
    {
        // Arrange
        var fake       = new FakeDisplayService();
        var manager    = new EquipmentManager(fake);
        var accessories = BuildFullInventory()
            .Where(i => i.Type == ItemType.Accessory && i.IsEquippable)
            .ToList();

        var player = new Player { Name = "Romanoff", HP = 100, MaxHP = 100 };
        foreach (var a in accessories)
            player.Inventory.Add(a);

        // Act
        var equipAct = () =>
        {
            foreach (var a in accessories)
                manager.HandleEquip(player, a.Name);
        };
        equipAct.Should().NotThrow("equipping accessories of every tier must not throw");
        player.EquippedAccessory.Should().NotBeNull();

        var unequipAct = () => manager.HandleUnequip(player, "accessory");
        unequipAct.Should().NotThrow("unequipping the accessory slot must not throw");
        player.EquippedAccessory.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ShowInventory after equip — [E] tag and color code both appear
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowInventory_RareWeaponMarkedEquipped_ShowsEquippedTagAndBrightCyan()
    {
        // Arrange — set the equipped slot directly so the item stays in Inventory,
        // which is the code path that produces the [E] tag in ShowInventory.
        var player = new Player { Name = "Romanoff", HP = 100, MaxHP = 100 };
        var rare   = new Item { Name = "Void Blade", Type = ItemType.Weapon, Tier = ItemTier.Rare, AttackBonus = 18, IsEquippable = true, Weight = 3 };
        var common = new Item { Name = "Rusty Dagger", Type = ItemType.Weapon, Tier = ItemTier.Common, AttackBonus = 2, IsEquippable = true, Weight = 1 };
        player.Inventory.Add(rare);
        player.Inventory.Add(common);
        player.EquippedWeapon = rare; // keep item in Inventory to exercise [E] tag path

        // Act
        _svc.ShowInventory(player);

        // Assert — Rare equipped item: BrightCyan on name AND [E] tag both present
        Output.Should().Contain(BrightCyan,
            because: "Rare equipped items must display their name in BrightCyan");
        Output.Should().Contain("[E]",
            because: "the equipped tag must appear next to the equipped Rare item");
    }

    [Fact]
    public void ShowInventory_UncommonArmorMarkedEquipped_ShowsEquippedTagAndGreen()
    {
        // Arrange — set the equipped slot directly so the item stays in Inventory.
        var player   = new Player { Name = "Romanoff", HP = 100, MaxHP = 100 };
        var uncommon = new Item { Name = "Shadow Cloak", Type = ItemType.Armor, Tier = ItemTier.Uncommon, DefenseBonus = 6, IsEquippable = true, Weight = 2 };
        player.Inventory.Add(uncommon);
        player.EquippedArmor = uncommon;

        // Act
        _svc.ShowInventory(player);

        // Assert
        Output.Should().Contain($"{Green}Shadow Cloak",
            because: "Uncommon equipped items must have their name in Green");
        Output.Should().Contain("[E]",
            because: "the equipped tag must appear next to the equipped Uncommon item");
    }
}
