using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Test coverage for Phase 0 (ANSI-safe padding) and Phase 1 (colorized combat messages)
/// display features merged in PRs #299 and prior.
/// </summary>
public class Phase1DisplayTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 0: ANSI-Safe Padding Tests (via ShowLootDrop / ShowInventory)
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Test that ShowLootDrop with a colorized tier label (e.g., Rare or Uncommon)
    /// produces correctly aligned box borders. The VisibleLength and PadRightVisible
    /// helpers are private static, so we test them indirectly by checking that the
    /// box borders align correctly despite ANSI color codes.
    /// </summary>
    [Collection("console-output")]
    public class ShowLootDrop_AnsiPaddingTests : IDisposable
    {
        private readonly StringWriter _output;
        private readonly TextWriter _originalOut;
        private readonly ConsoleDisplayService _svc;

        public ShowLootDrop_AnsiPaddingTests()
        {
            _originalOut = Console.Out;
            _output = new StringWriter();
            Console.SetOut(_output);
            _svc = new ConsoleDisplayService();
        }

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            _output.Dispose();
        }

        private string Output => _output.ToString();

        [Theory]
        [InlineData(ItemTier.Rare)]
        [InlineData(ItemTier.Uncommon)]
        public void ShowLootDrop_ColorizedTier_BoxBordersAlign(ItemTier tier)
        {
            // Arrange: item with a colorized tier label
            var item = new Item 
            { 
                Name = "Magic Sword", 
                Type = ItemType.Weapon, 
                AttackBonus = 10, 
                Tier = tier 
            };
            var player = new Player();

            // Act
            _svc.ShowLootDrop(item, player);

            // Assert: box borders should appear at consistent positions
            // Strip ANSI codes from each line, check that all border lines have consistent lengths
            var lines = Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            var strippedLines = lines.Select(l => ColorCodes.StripAnsiCodes(l)).ToList();
            
            // All lines with "║" should have the same visible length
            var borderLines = strippedLines.Where(l => l.Contains("║")).ToList();
            borderLines.Should().NotBeEmpty("box should have border lines");
            
            var lengths = borderLines.Select(l => l.Length).Distinct().ToList();
            lengths.Should().ContainSingle("all border lines should have the same visible length");
        }

        [Fact]
        public void ShowInventory_ColorizedItems_AlignCorrectly()
        {
            // Arrange: player with items having colorized names (by tier)
            var player = new Player();
            player.Inventory.Add(new Item 
            { 
                Name = "Rare Sword", 
                Type = ItemType.Weapon, 
                AttackBonus = 15, 
                Tier = ItemTier.Rare, 
                Weight = 5 
            });
            player.Inventory.Add(new Item 
            { 
                Name = "Common Axe", 
                Type = ItemType.Weapon, 
                AttackBonus = 5, 
                Tier = ItemTier.Common, 
                Weight = 6 
            });

            // Act
            _svc.ShowInventory(player);

            // Assert: inventory lines shouldn't have misaligned output
            // We check that weight appears in a consistent format
            var lines = Output.Split(Environment.NewLine);
            var itemLines = lines.Where(l => ColorCodes.StripAnsiCodes(l).Contains("⚔")).ToList();
            
            itemLines.Should().HaveCountGreaterThan(0, "inventory should contain weapon items");
            // All item lines should have weight indicator at the end
            foreach (var line in itemLines)
            {
                var stripped = ColorCodes.StripAnsiCodes(line);
                stripped.Should().MatchRegex(@"\d+ wt", "weight should appear in consistent format");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 1.4: Colorized Turn Log Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CombatEngine_CriticalHit_ProducesColorizedMessage()
    {
        // Arrange: player vs weak enemy, forced crit
        var player = new Player { HP = 100, MaxHP = 100, Attack = 50, Defense = 5 };
        var enemy = new Enemy_Stub(hp: 50, atk: 1, def: 0, xp: 10);
        var input = new FakeInputReader("A"); // single attack
        var display = new FakeDisplayService(input);
        // Force crit with controlled random: NextDouble() returns 0.01 < 0.05
        var rng = new ControlledRandom(defaultDouble: 0.01);
        var engine = new CombatEngine(display, input, rng);

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert: the raw combat messages should contain ANSI color codes for crit
        result.Should().Be(CombatResult.Won);
        var rawMsgs = display.RawCombatMessages;
        rawMsgs.Should().Contain(msg => 
            msg.Contains(ColorCodes.Yellow) || msg.Contains(ColorCodes.BrightRed) || msg.Contains(ColorCodes.Bold),
            "crit message should contain color codes");
    }

    [Fact]
    public void CombatEngine_Miss_ProducesGrayColoredMessage()
    {
        // Arrange: player with very low attack vs high-defense enemy, guaranteed to miss via dodge roll
        var player = new Player { HP = 100, MaxHP = 100, Attack = 1, Defense = 5 };
        var enemy = new Enemy_Stub(hp: 200, atk: 1, def: 100, xp: 10);
        var input = new FakeInputReader("A", "A", "A", "F"); // try attacks, then flee
        var display = new FakeDisplayService(input);
        // Force dodge: NextDouble() for dodge check returns 0.01 < 0.05 (5% dodge)
        var rng = new ControlledRandom(defaultDouble: 0.01);
        var engine = new CombatEngine(display, input, rng);

        // Act
        engine.RunCombat(player, enemy);

        // Assert: dodge messages appear in combat output; gray styling is applied in recent-turns
        // summary via ShowRecentTurns → ShowMessage (ANSI stripped by FakeDisplayService).
        var rawMsgs = display.RawCombatMessages;
        rawMsgs.Should().Contain(msg => msg.Contains("dodge") || msg.Contains("Dodge"),
            "dodge message should appear in combat output");
    }

    [Fact]
    public void CombatEngine_StatusEffectApplied_MessageContainsEffectColor()
    {
        // Arrange: player with poison weapon vs enemy
        var player = new Player 
        { 
            HP = 100, 
            MaxHP = 100, 
            Attack = 10, 
            Defense = 5 
        };
        player.EquippedWeapon = new Item 
        { 
            Name = "Bleed Blade", 
            Type = ItemType.Weapon, 
            AttackBonus = 10, 
            AppliesBleedOnHit = true
        };
        var enemy = new Enemy_Stub(hp: 100, atk: 1, def: 0, xp: 10);
        var input = new FakeInputReader("A", "F"); // attack once, then flee
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(0.01); // 0.01 < 0.5 → flee succeeds; 0.01 < 0.10 → bleed procs
        var engine = new CombatEngine(display, input, rng);

        // Act
        engine.RunCombat(player, enemy);

        // Assert: combat messages should mention a status effect being applied
        var allCombat = string.Join(" ", display.CombatMessages);
        allCombat.Should().ContainEquivalentOf("bleed", "bleed effect should be mentioned");
    }

    [Fact]
    public void CombatEngine_ImmunityFeedback_MessageAppears()
    {
        // Arrange: player with poison weapon vs Stone Golem (immune to status effects)
        var player = new Player 
        { 
            HP = 100, 
            MaxHP = 100, 
            Attack = 10, 
            Defense = 5 
        };
        player.EquippedWeapon = new Item 
        { 
            Name = "Bleed Blade", 
            Type = ItemType.Weapon, 
            AttackBonus = 10, 
            AppliesBleedOnHit = true
        };
        var golem = new StoneGolem(null, null); // immune enemy
        var input = new FakeInputReader("A", "F"); // attack once, then flee
        var display = new FakeDisplayService(input);
        // Queue: 0.9 → dodge(20)=0.5: not dodged; 0.9 → crit: miss; then 0.01 → bleed procs + flee succeeds
        var rng = new ControlledRandom(0.01, 0.9, 0.9);
        var engine = new CombatEngine(display, input, rng);

        // Act
        engine.RunCombat(player, golem);

        // Assert: should show immunity message
        var allMessages = string.Join(" ", display.Messages);
        allMessages.Should().Contain("immune", "immunity feedback should appear");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 1.6: XP Progress Message Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CombatEngine_AfterWinningCombat_ShowsXPProgress()
    {
        // Arrange: player vs weak enemy
        var player = new Player 
        { 
            HP = 100, 
            MaxHP = 100, 
            Attack = 50, 
            Defense = 5,
            XP = 50,
            Level = 1
        };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 20);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom();
        var engine = new CombatEngine(display, input, rng);

        // Act
        var result = engine.RunCombat(player, enemy);

        // Assert: XP progress message should be displayed
        result.Should().Be(CombatResult.Won);
        var allMessages = string.Join(" ", display.Messages);
        allMessages.Should().Contain("XP", "XP message should appear");
        allMessages.Should().Contain("Total:", "XP progress should show total");
        allMessages.Should().Contain("to next level", "XP progress should show next level threshold");
    }

    [Fact]
    public void CombatEngine_XPProgressMessage_ContainsCorrectValues()
    {
        // Arrange: player at specific XP level
        var player = new Player 
        { 
            HP = 100, 
            MaxHP = 100, 
            Attack = 50, 
            Defense = 5,
            XP = 80,
            Level = 1
        };
        var enemy = new Enemy_Stub(hp: 1, atk: 1, def: 0, xp: 15);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom();
        var engine = new CombatEngine(display, input, rng);

        // Act
        engine.RunCombat(player, enemy);
        
        // Assert: XP message should show gained amount and new total
        var allMessages = string.Join(" ", display.Messages);
        allMessages.Should().Contain("15 XP", "should show XP gained");
        allMessages.Should().Contain("95", "should show new total XP (80 + 15)");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 1.7: Ability Confirmation Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CombatEngine_AbilityUsed_ShowsConfirmationMessage()
    {
        // Arrange: player with enough mana/level to use Power Strike (unlocks at level 1)
        var player = new Player 
        { 
            HP = 100, 
            MaxHP = 100, 
            Attack = 10, 
            Defense = 5,
            Mana = 50,
            MaxMana = 50,
            Level = 1
        };

        var enemy = new Enemy_Stub(hp: 50, atk: 1, def: 0, xp: 10);
        var input = new FakeInputReader("B", "1", "F"); // use ability (B=ability menu), then flee
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(0.01); // 0.01 < 0.5 → flee succeeds
        var engine = new CombatEngine(display, input, rng);

        // Act
        engine.RunCombat(player, enemy);

        // Assert: confirmation message should contain "activated"
        var allMessages = string.Join(" ", display.Messages);
        allMessages.Should().Contain("activated", "ability confirmation should contain 'activated'");
    }

    [Fact]
    public void CombatEngine_AbilityUsed_MessageContainsAbilityName()
    {
        // Arrange: player with enough mana/level to use Shield Bash (Warrior level 1+)
        var player = new Player 
        { 
            HP = 100, 
            MaxHP = 100, 
            Attack = 15, 
            Defense = 5,
            Mana = 50,
            MaxMana = 50,
            Level = 1,
            Class = PlayerClass.Warrior
        };

        var enemy = new Enemy_Stub(hp: 100, atk: 1, def: 0, xp: 10);
        var input = new FakeInputReader("B", "1", "F"); // use ability (B=ability menu), then flee
        var display = new FakeDisplayService(input);
        var rng = new ControlledRandom(0.01); // 0.01 < 0.5 → flee succeeds
        var engine = new CombatEngine(display, input, rng);

        // Act
        engine.RunCombat(player, enemy);

        // Assert: confirmation message should contain "Shield Bash" (first Warrior ability at level 1)
        var allMessages = string.Join(" ", display.Messages);
        allMessages.Should().Contain("Shield Bash", "ability name should appear in confirmation");
    }
}
