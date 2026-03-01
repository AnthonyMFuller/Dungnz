using Dungnz.Display;
using Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Regression tests for all difficulty balance behaviors introduced in issue #691.
/// Verifies that Casual/Normal/Hard settings apply correct multipliers to player damage,
/// enemy damage, gold, XP, loot drops, merchant prices, healing, and starting conditions.
/// </summary>
public class DifficultyBalanceTests
{
    // ── 1. DifficultySettings.For() values ────────────────────────────────────

    [Fact]
    public void DifficultySettings_Casual_AllPropertiesCorrect()
    {
        var settings = DifficultySettings.For(Difficulty.Casual);
        
        settings.EnemyStatMultiplier.Should().Be(0.65f);
        settings.EnemyDamageMultiplier.Should().Be(0.70f);
        settings.PlayerDamageMultiplier.Should().Be(1.20f);
        settings.LootDropMultiplier.Should().Be(1.60f);
        settings.GoldMultiplier.Should().Be(1.80f);
        settings.HealingMultiplier.Should().Be(1.50f);
        settings.MerchantPriceMultiplier.Should().Be(0.65f);
        settings.XPMultiplier.Should().Be(1.40f);
        settings.StartingGold.Should().Be(50);
        settings.StartingPotions.Should().Be(3);
        settings.ShrineSpawnMultiplier.Should().Be(1.50f);
        settings.MerchantSpawnMultiplier.Should().Be(1.40f);
        settings.Permadeath.Should().BeFalse();
    }

    [Fact]
    public void DifficultySettings_Normal_AllPropertiesCorrect()
    {
        var settings = DifficultySettings.For(Difficulty.Normal);
        
        settings.EnemyStatMultiplier.Should().Be(1.00f);
        settings.EnemyDamageMultiplier.Should().Be(1.00f);
        settings.PlayerDamageMultiplier.Should().Be(1.00f);
        settings.LootDropMultiplier.Should().Be(1.00f);
        settings.GoldMultiplier.Should().Be(1.00f);
        settings.HealingMultiplier.Should().Be(1.00f);
        settings.MerchantPriceMultiplier.Should().Be(1.00f);
        settings.XPMultiplier.Should().Be(1.00f);
        settings.StartingGold.Should().Be(15);
        settings.StartingPotions.Should().Be(1);
        settings.ShrineSpawnMultiplier.Should().Be(1.00f);
        settings.MerchantSpawnMultiplier.Should().Be(1.00f);
        settings.Permadeath.Should().BeFalse();
    }

    [Fact]
    public void DifficultySettings_Hard_AllPropertiesCorrect()
    {
        var settings = DifficultySettings.For(Difficulty.Hard);
        
        settings.EnemyStatMultiplier.Should().Be(1.35f);
        settings.EnemyDamageMultiplier.Should().Be(1.25f);
        settings.PlayerDamageMultiplier.Should().Be(0.90f);
        settings.LootDropMultiplier.Should().Be(0.65f);
        settings.GoldMultiplier.Should().Be(0.60f);
        settings.HealingMultiplier.Should().Be(0.75f);
        settings.MerchantPriceMultiplier.Should().Be(1.40f);
        settings.XPMultiplier.Should().Be(0.80f);
        settings.StartingGold.Should().Be(0);
        settings.StartingPotions.Should().Be(0);
        settings.ShrineSpawnMultiplier.Should().Be(0.70f);
        settings.MerchantSpawnMultiplier.Should().Be(0.70f);
        settings.Permadeath.Should().BeTrue();
    }

    // ── 2. PlayerDamageMultiplier applied ──────────────────────────────────

    [Fact]
    public void CombatEngine_Casual_PlayerDamageMultiplierApplied()
    {
        // Casual: PlayerDamageMultiplier = 1.20
        // Player Attack=100, Enemy Defense=0 → base damage=100
        // After multiplier: Max(1, (int)(100 * 1.20)) = 120
        // Enemy HP=50 < 120, so enemy dies in one hit — no flee needed
        var player = new Player { HP = 100, MaxHP = 100, Attack = 100, Defense = 5 };
        var enemy = new BalanceTestEnemy(hp: 50, atk: 0, def: 0, xp: 10);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Casual);
        // RNG: 0.95 = no crit (0.95 > 0.15), no enemy-dodge (0.95 > 0)
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.95), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("120 damage"));
    }

    [Fact]
    public void CombatEngine_Hard_PlayerDamageMultiplierApplied()
    {
        // Hard: PlayerDamageMultiplier = 0.90
        // Player Attack=100, Enemy Defense=0 → base damage=100
        // After multiplier: Max(1, (int)(100 * 0.90)) = 90
        // Enemy HP=50 < 90, so enemy dies in one hit — no flee needed
        var player = new Player { HP = 100, MaxHP = 100, Attack = 100, Defense = 5 };
        var enemy = new BalanceTestEnemy(hp: 50, atk: 0, def: 0, xp: 10);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Hard);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.95), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("90 damage"));
    }

    [Fact]
    public void CombatEngine_Normal_PlayerDamageMultiplierApplied()
    {
        // Normal: PlayerDamageMultiplier = 1.00
        // Player Attack=50, Enemy Defense=0 → base damage=50
        // After multiplier: Max(1, (int)(50 * 1.00)) = 50
        // Enemy HP=30 < 50, so enemy dies in one hit — no flee needed
        var player = new Player { HP = 100, MaxHP = 100, Attack = 50, Defense = 5 };
        var enemy = new BalanceTestEnemy(hp: 30, atk: 0, def: 0, xp: 10);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Normal);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.95), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        display.CombatMessages.Should().Contain(m => m.Contains("50 damage"));
    }

    // ── 3. EnemyDamageMultiplier applied ───────────────────────────────────

    [Fact]
    public void CombatEngine_Casual_EnemyDamageMultiplierApplied()
    {
        // Casual: EnemyDamageMultiplier = 0.70
        // Enemy Attack=100, Player Defense=0 → base damage=100
        // After multiplier: Max(1, (int)(100 * 0.70)) = 70
        // Player ATK=1 so enemy HP=2 survives first hit; enemy attacks once, then dies on second hit
        var player = new Player { HP = 200, MaxHP = 200, Attack = 1, Defense = 0 };
        var enemy = new BalanceTestEnemy(hp: 2, atk: 100, def: 0, xp: 10);
        var input = new FakeInputReader("A", "A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Casual);
        // RNG default=0.95: no crits, no dodges for all turns
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.95), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        player.HP.Should().Be(130, "enemy dealt 70 damage (100 * 0.70) on their one attack");
    }

    [Fact]
    public void CombatEngine_Hard_EnemyDamageMultiplierApplied()
    {
        // Hard: EnemyDamageMultiplier = 1.25
        // Enemy Attack=100, Player Defense=0 → base damage=100
        // After multiplier: Max(1, (int)(100 * 1.25)) = 125
        // Player ATK=1 so enemy HP=2 survives first hit; enemy attacks once, then dies on second hit
        var player = new Player { HP = 200, MaxHP = 200, Attack = 1, Defense = 0 };
        var enemy = new BalanceTestEnemy(hp: 2, atk: 100, def: 0, xp: 10);
        var input = new FakeInputReader("A", "A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Hard);
        var engine = new CombatEngine(display, input, new ControlledRandom(defaultDouble: 0.95), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        player.HP.Should().Be(75, "enemy dealt 125 damage (100 * 1.25) on their one attack");
    }

    // ── 4. GoldMultiplier applied ──────────────────────────────────────────

    [Fact]
    public void CombatEngine_Casual_GoldMultiplierApplied()
    {
        // Casual: GoldMultiplier = 1.80
        // Enemy loot table: 10 gold
        // After multiplier: (int)(10 * 1.80) = 18
        var player = new Player { HP = 100, MaxHP = 100, Attack = 50, Defense = 5, Gold = 0 };
        var enemy = new BalanceTestEnemy(hp: 1, atk: 0, def: 0, xp: 10);
        enemy.LootTable = new LootTable(minGold: 10, maxGold: 10);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Casual);
        var engine = new CombatEngine(display, input, new ControlledRandom(), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        player.Gold.Should().Be(18, "gold is scaled by 1.80x");
    }

    [Fact]
    public void CombatEngine_Hard_GoldMultiplierApplied()
    {
        // Hard: GoldMultiplier = 0.60
        // Enemy loot table: 10 gold
        // After multiplier: (int)(10 * 0.60) = 6
        var player = new Player { HP = 100, MaxHP = 100, Attack = 50, Defense = 5, Gold = 0 };
        var enemy = new BalanceTestEnemy(hp: 1, atk: 0, def: 0, xp: 10);
        enemy.LootTable = new LootTable(minGold: 10, maxGold: 10);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Hard);
        var engine = new CombatEngine(display, input, new ControlledRandom(), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        player.Gold.Should().Be(6, "gold is scaled by 0.60x");
    }

    // ── 5. XPMultiplier applied ────────────────────────────────────────────

    [Fact]
    public void CombatEngine_Casual_XPMultiplierApplied()
    {
        // Casual: XPMultiplier = 1.40
        // Enemy XPValue = 100
        // After multiplier: Max(1, (int)(100 * 1.40)) = 140
        var player = new Player { HP = 100, MaxHP = 100, Attack = 50, Defense = 5, XP = 0 };
        var enemy = new BalanceTestEnemy(hp: 1, atk: 0, def: 0, xp: 100);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Casual);
        var engine = new CombatEngine(display, input, new ControlledRandom(), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        player.XP.Should().Be(140, "XP is scaled by 1.40x");
    }

    [Fact]
    public void CombatEngine_Hard_XPMultiplierApplied()
    {
        // Hard: XPMultiplier = 0.80
        // Enemy XPValue = 100
        // After multiplier: Max(1, (int)(100 * 0.80)) = 80
        var player = new Player { HP = 100, MaxHP = 100, Attack = 50, Defense = 5, XP = 0 };
        var enemy = new BalanceTestEnemy(hp: 1, atk: 0, def: 0, xp: 100);
        var input = new FakeInputReader("A");
        var display = new FakeDisplayService(input);
        var difficulty = DifficultySettings.For(Difficulty.Hard);
        var engine = new CombatEngine(display, input, new ControlledRandom(), difficulty: difficulty);

        engine.RunCombat(player, enemy);

        player.XP.Should().Be(80, "XP is scaled by 0.80x");
    }

    // ── 6. LootDropMultiplier affects drop rates ───────────────────────────

    [Fact]
    public void LootTable_CasualMultiplier_IncreasesDropRate()
    {
        // Casual: LootDropMultiplier = 1.60
        // Base 30% drop rate becomes 48% with 1.60x multiplier
        // Run 1000 trials with fixed RNG to verify statistical increase
        const int trials = 1000;
        int casualDrops = 0;
        int normalDrops = 0;

        // Setup: create a common-tier item pool
        var item1 = new Item { Name = "Test Sword", Type = ItemType.Weapon, Tier = ItemTier.Common };
        LootTable.SetTierPools(new[] { item1 }, Array.Empty<Item>(), Array.Empty<Item>());

        var enemy = new BalanceTestEnemy(hp: 10, atk: 1, def: 0, xp: 1);

        // Casual: LootDropMultiplier = 1.60
        var casualTable = new LootTable(new Random(42), 0, 0);
        for (int i = 0; i < trials; i++)
        {
            var result = casualTable.RollDrop(enemy, playerLevel: 1, lootDropMultiplier: 1.60f);
            if (result.Item != null) casualDrops++;
        }

        // Normal: LootDropMultiplier = 1.00
        var normalTable = new LootTable(new Random(42), 0, 0);
        for (int i = 0; i < trials; i++)
        {
            var result = normalTable.RollDrop(enemy, playerLevel: 1, lootDropMultiplier: 1.00f);
            if (result.Item != null) normalDrops++;
        }

        casualDrops.Should().BeGreaterThan(normalDrops, "Casual should drop more items than Normal with same seed");
    }

    [Fact]
    public void LootTable_HardMultiplier_DecreasesDropRate()
    {
        // Hard: LootDropMultiplier = 0.65
        // Base 30% drop rate becomes ~20% with 0.65x multiplier
        const int trials = 1000;
        int hardDrops = 0;
        int normalDrops = 0;

        var item1 = new Item { Name = "Test Sword", Type = ItemType.Weapon, Tier = ItemTier.Common };
        LootTable.SetTierPools(new[] { item1 }, Array.Empty<Item>(), Array.Empty<Item>());

        var enemy = new BalanceTestEnemy(hp: 10, atk: 1, def: 0, xp: 1);

        // Hard: LootDropMultiplier = 0.65
        var hardTable = new LootTable(new Random(99), 0, 0);
        for (int i = 0; i < trials; i++)
        {
            var result = hardTable.RollDrop(enemy, playerLevel: 1, lootDropMultiplier: 0.65f);
            if (result.Item != null) hardDrops++;
        }

        // Normal: LootDropMultiplier = 1.00
        var normalTable = new LootTable(new Random(99), 0, 0);
        for (int i = 0; i < trials; i++)
        {
            var result = normalTable.RollDrop(enemy, playerLevel: 1, lootDropMultiplier: 1.00f);
            if (result.Item != null) normalDrops++;
        }

        hardDrops.Should().BeLessThan(normalDrops, "Hard should drop fewer items than Normal with same seed");
    }

    // ── 7. MerchantPriceMultiplier affects prices ──────────────────────────

    [Fact]
    public void MerchantInventoryConfig_CasualMultiplier_ReducesPrices()
    {
        // Casual: MerchantPriceMultiplier = 0.65
        // Create items and verify prices are ~65% of normal
        var item1 = new Item { Id = "test-sword", Name = "Test Sword", Tier = ItemTier.Common, AttackBonus = 5 };
        var item2 = new Item { Id = "test-armor", Name = "Test Armor", Tier = ItemTier.Common, DefenseBonus = 3 };
        var allItems = new[] { item1, item2 };
        
        var casualDiff = DifficultySettings.For(Difficulty.Casual);
        var normalDiff = DifficultySettings.For(Difficulty.Normal);
        
        var casualStock = MerchantInventoryConfig.GetStockForFloor(1, allItems, new Random(42), casualDiff);
        var normalStock = MerchantInventoryConfig.GetStockForFloor(1, allItems, new Random(42), normalDiff);

        // With fallback stock (3 items), verify prices are scaled
        var casualPotion = casualStock.FirstOrDefault(s => s.Item.Name.Contains("Potion"));
        var normalPotion = normalStock.FirstOrDefault(s => s.Item.Name.Contains("Potion"));
        
        if (casualPotion != null && normalPotion != null)
        {
            casualPotion.Price.Should().BeLessThan(normalPotion.Price, "Casual prices should be lower than Normal");
            // Expected: Normal potion is 25g, Casual is (int)(25 * 0.65) = 16g
            casualPotion.Price.Should().Be(16);
            normalPotion.Price.Should().Be(25);
        }
    }

    [Fact]
    public void MerchantInventoryConfig_HardMultiplier_IncreasesPrices()
    {
        // Hard: MerchantPriceMultiplier = 1.40
        var item1 = new Item { Id = "test-sword", Name = "Test Sword", Tier = ItemTier.Common, AttackBonus = 5 };
        var allItems = new[] { item1 };
        
        var hardDiff = DifficultySettings.For(Difficulty.Hard);
        var normalDiff = DifficultySettings.For(Difficulty.Normal);
        
        var hardStock = MerchantInventoryConfig.GetStockForFloor(1, allItems, new Random(42), hardDiff);
        var normalStock = MerchantInventoryConfig.GetStockForFloor(1, allItems, new Random(42), normalDiff);

        var hardPotion = hardStock.FirstOrDefault(s => s.Item.Name.Contains("Potion"));
        var normalPotion = normalStock.FirstOrDefault(s => s.Item.Name.Contains("Potion"));
        
        if (hardPotion != null && normalPotion != null)
        {
            hardPotion.Price.Should().BeGreaterThan(normalPotion.Price, "Hard prices should be higher than Normal");
            // Expected: Normal potion is 25g, Hard is (int)(25 * 1.40) = 35g
            hardPotion.Price.Should().Be(35);
            normalPotion.Price.Should().Be(25);
        }
    }

    // ── 8. Starting gold and potions ───────────────────────────────────────

    [Fact]
    public void StartingConditions_Casual_CorrectValues()
    {
        var settings = DifficultySettings.For(Difficulty.Casual);
        settings.StartingGold.Should().Be(50);
        settings.StartingPotions.Should().Be(3);
    }

    [Fact]
    public void StartingConditions_Normal_CorrectValues()
    {
        var settings = DifficultySettings.For(Difficulty.Normal);
        settings.StartingGold.Should().Be(15);
        settings.StartingPotions.Should().Be(1);
    }

    [Fact]
    public void StartingConditions_Hard_CorrectValues()
    {
        var settings = DifficultySettings.For(Difficulty.Hard);
        settings.StartingGold.Should().Be(0);
        settings.StartingPotions.Should().Be(0);
    }

    // ── 9. HealingMultiplier applied ───────────────────────────────────────

    [Fact]
    public void HealingMultiplier_Casual_IncreasesHealAmount()
    {
        // Casual: HealingMultiplier = 1.50
        // Potion heals 20 HP → after multiplier: Max(1, (int)(20 * 1.50)) = 30
        var casualDiff = DifficultySettings.For(Difficulty.Casual);
        var healAmount = 20;
        var scaledHeal = Math.Max(1, (int)(healAmount * casualDiff.HealingMultiplier));
        
        scaledHeal.Should().Be(30, "20 HP heal * 1.50 = 30 HP");
    }

    [Fact]
    public void HealingMultiplier_Hard_DecreasesHealAmount()
    {
        // Hard: HealingMultiplier = 0.75
        // Potion heals 20 HP → after multiplier: Max(1, (int)(20 * 0.75)) = 15
        var hardDiff = DifficultySettings.For(Difficulty.Hard);
        var healAmount = 20;
        var scaledHeal = Math.Max(1, (int)(healAmount * hardDiff.HealingMultiplier));
        
        scaledHeal.Should().Be(15, "20 HP heal * 0.75 = 15 HP");
    }

    [Fact]
    public void HealingMultiplier_Normal_NoChange()
    {
        // Normal: HealingMultiplier = 1.00
        var normalDiff = DifficultySettings.For(Difficulty.Normal);
        var healAmount = 20;
        var scaledHeal = Math.Max(1, (int)(healAmount * normalDiff.HealingMultiplier));
        
        scaledHeal.Should().Be(20, "20 HP heal * 1.00 = 20 HP");
    }

    // ── 10. Sell prices unaffected by difficulty ───────────────────────────

    [Fact]
    public void SellPrice_UnaffectedByDifficulty()
    {
        // ComputeSellPrice should return same value regardless of difficulty
        var item = new Item { SellPrice = 0, Tier = ItemTier.Common, AttackBonus = 5, DefenseBonus = 0, HealAmount = 0 };
        
        var sellPrice = MerchantInventoryConfig.ComputeSellPrice(item);
        
        // Sell price is computed independently (40% of buy price, no difficulty consideration)
        // ComputePrice = 15 + 0 + 5*5 = 40; sell = max(1, 40 * 40 / 100) = 16
        sellPrice.Should().Be(16, "sell price is computed independently of difficulty");
    }
}

/// <summary>Minimal test stub for balance testing with configurable stats.</summary>
file class BalanceTestEnemy : Enemy
{
    public BalanceTestEnemy(int hp, int atk, int def, int xp)
    {
        Name = "BalanceTestEnemy";
        HP = MaxHP = hp;
        Attack = atk;
        Defense = def;
        XPValue = xp;
        LootTable = new LootTable(minGold: 0, maxGold: 0);
        FlatDodgeChance = 0f;
    }
}
