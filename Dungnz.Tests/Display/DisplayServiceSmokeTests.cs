using Dungnz.Display;
using Dungnz.Models;
using Dungnz.Systems.Enemies;
using FluentAssertions;
using Spectre.Console;
using Xunit;

namespace Dungnz.Tests.Display;

/// <summary>
/// Smoke tests for issue #875: verifies that the five primary display methods in
/// <see cref="SpectreDisplayService"/> render without throwing a
/// <see cref="Spectre.Console.MarkupException"/> (malformed markup / unescaped brackets)
/// and produce non-empty output.
///
/// Uses the AnsiConsole output-capture pattern established for #870:
/// swap <see cref="AnsiConsole.Console"/> with a non-interactive no-colour writer
/// before each test and restore it in <see cref="Dispose"/>.
/// </summary>
[Collection("console-output")]
public sealed class DisplayServiceSmokeTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole;
    private readonly StringWriter _writer;
    private readonly SpectreDisplayService _svc;

    public DisplayServiceSmokeTests()
    {
        _originalConsole = AnsiConsole.Console;
        _writer = new StringWriter();
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi        = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out         = new AnsiConsoleOutput(_writer),
            Interactive = InteractionSupport.No,
        });
        _svc = new SpectreDisplayService();
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _writer.Dispose();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal valid player. Level 2 is intentional: all learnable skills require
    /// level 3+, so <see cref="SpectreDisplayService.ShowSkillTreeMenu"/> will
    /// return early without triggering an interactive <see cref="AnsiConsole.Prompt"/>.
    /// </summary>
    private static Player BuildPlayer() => new()
    {
        Name    = "TestHero",
        HP      = 80,
        MaxHP   = 100,
        Mana    = 20,
        MaxMana = 30,
        Attack  = 12,
        Defense = 5,
        Level   = 2,
        Gold    = 50,
        Class   = PlayerClass.Warrior,
    };

    // ── ShowInventory ────────────────────────────────────────────────────────

    /// <summary>
    /// ShowInventory with a populated inventory must not throw and must produce output.
    /// Exercises the item-table rendering path including tier colour markup.
    /// </summary>
    [Fact]
    public void ShowInventory_WithItems_DoesNotThrow()
    {
        var player = BuildPlayer();
        player.Inventory.Add(new Item { Name = "Iron Sword",    Type = ItemType.Weapon,      Tier = ItemTier.Common,   Weight = 3, AttackBonus = 5, IsEquippable = true });
        player.Inventory.Add(new Item { Name = "Health Potion", Type = ItemType.Consumable,  Tier = ItemTier.Common,   Weight = 1, HealAmount  = 30 });
        player.Inventory.Add(new Item { Name = "Chain Chestplate", Type = ItemType.Armor,    Tier = ItemTier.Uncommon, Weight = 5, DefenseBonus = 4, IsEquippable = true, Slot = ArmorSlot.Chest });

        var act = () => _svc.ShowInventory(player);

        act.Should().NotThrow();
        _writer.ToString().Should().NotBeEmpty();
    }

    /// <summary>
    /// ShowInventory with an empty inventory must render the empty-state message
    /// without throwing.
    /// </summary>
    [Fact]
    public void ShowInventory_WhenEmpty_DoesNotThrow()
    {
        var act = () => _svc.ShowInventory(BuildPlayer());

        act.Should().NotThrow();
        _writer.ToString().Should().Contain("empty");
    }

    // ── ShowEquipment ────────────────────────────────────────────────────────

    /// <summary>
    /// ShowEquipment with gear in multiple slots must not throw and must render
    /// the equipment table with item names visible in output.
    /// </summary>
    [Fact]
    public void ShowEquipment_WithGear_DoesNotThrow()
    {
        var player  = BuildPlayer();
        var weapon  = new Item { Name = "Iron Sword",      Type = ItemType.Weapon, Tier = ItemTier.Common,   Weight = 3, AttackBonus  = 5, IsEquippable = true };
        var chest   = new Item { Name = "Leather Armour",  Type = ItemType.Armor,  Tier = ItemTier.Common,   Weight = 5, DefenseBonus = 3, IsEquippable = true, Slot = ArmorSlot.Chest };

        player.EquippedWeapon = weapon;
        player.EquippedChest  = chest;

        var act = () => _svc.ShowEquipment(player);

        act.Should().NotThrow();
        var output = _writer.ToString();
        output.Should().NotBeEmpty();
        output.Should().Contain("Iron Sword");
        output.Should().Contain("Leather Armour");
    }

    /// <summary>
    /// ShowEquipment with all slots empty must render "(empty)" placeholders without
    /// throwing — exercises the null-item branch in AddSlot.
    /// </summary>
    [Fact]
    public void ShowEquipment_AllSlotsEmpty_DoesNotThrow()
    {
        var act = () => _svc.ShowEquipment(BuildPlayer());

        act.Should().NotThrow();
        _writer.ToString().Should().Contain("empty");
    }

    // ── ShowSkillTree ────────────────────────────────────────────────────────

    /// <summary>
    /// ShowSkillTreeMenu must render the overview table and return without prompting
    /// when no skills are learnable (level 2, min skill level is 3).
    /// </summary>
    [Fact]
    public void ShowSkillTreeMenu_NoLearnableSkills_DoesNotThrow()
    {
        var act = () => _svc.ShowSkillTreeMenu(BuildPlayer());

        act.Should().NotThrow();
        _writer.ToString().Should().NotBeEmpty();
    }

    // ── ShowHelp ─────────────────────────────────────────────────────────────

    /// <summary>
    /// ShowHelp must render the command reference table without throwing.
    /// Regression guard for #870 (square-bracket sequences in command syntax that
    /// were previously unescaped and triggered InvalidOperationException).
    /// </summary>
    [Fact]
    public void ShowHelp_DoesNotThrow()
    {
        var act = () => _svc.ShowHelp();

        act.Should().NotThrow();
        _writer.ToString().Should().NotBeEmpty();
    }

    // ── ShowCombatStatus ─────────────────────────────────────────────────────

    /// <summary>
    /// ShowCombatStatus with no active effects must render the two-column HP table
    /// without throwing.
    /// </summary>
    [Fact]
    public void ShowCombatStatus_NoEffects_DoesNotThrow()
    {
        var player = BuildPlayer();
        var enemy  = new Goblin();

        var act = () => _svc.ShowCombatStatus(player, enemy,
            Array.Empty<ActiveEffect>(),
            Array.Empty<ActiveEffect>());

        act.Should().NotThrow();
        var output = _writer.ToString();
        output.Should().NotBeEmpty();
        output.Should().Contain("TestHero");
    }

    /// <summary>
    /// ShowCombatStatus with active status effects on both sides exercises the
    /// effect-badge markup path — ensures effect names aren't left unescaped.
    /// </summary>
    [Fact]
    public void ShowCombatStatus_WithActiveEffects_DoesNotThrow()
    {
        var player = BuildPlayer();
        var enemy  = new Goblin();

        var playerEffects = new List<ActiveEffect>
        {
            new(StatusEffect.Poison, duration: 2),
            new(StatusEffect.Bleed,  duration: 3),
        };
        var enemyEffects = new List<ActiveEffect>
        {
            new(StatusEffect.Burn, duration: 1),
        };

        var act = () => _svc.ShowCombatStatus(player, enemy, playerEffects, enemyEffects);

        act.Should().NotThrow();
        _writer.ToString().Should().NotBeEmpty();
    }
}
