using Dungnz.Models;
using Dungnz.Tests.Builders;
using FluentAssertions;
using Xunit;

namespace Dungnz.Tests;

/// <summary>
/// Validates that the test builder helpers produce correctly configured objects.
/// </summary>
public class BuilderTests
{
    [Fact]
    public void PlayerBuilder_CreatesPlayerWithCustomStats()
    {
        var player = new PlayerBuilder()
            .Named("Hero")
            .WithHP(50)
            .WithMaxHP(80)
            .WithAttack(15)
            .WithDefense(10)
            .WithClass(PlayerClass.Mage)
            .WithGold(100)
            .Build();

        player.Name.Should().Be("Hero");
        player.HP.Should().Be(50);
        player.MaxHP.Should().Be(80);
        player.Attack.Should().Be(15);
        player.Defense.Should().Be(10);
        player.Class.Should().Be(PlayerClass.Mage);
        player.Gold.Should().Be(100);
    }

    [Fact]
    public void EnemyBuilder_CreatesEnemyWithCustomStats()
    {
        var enemy = new EnemyBuilder()
            .Named("Test Goblin")
            .WithHP(30)
            .WithAttack(5)
            .WithDefense(1)
            .Build();

        enemy.Name.Should().Be("Test Goblin");
        enemy.HP.Should().Be(30);
        enemy.MaxHP.Should().Be(30);
        enemy.Attack.Should().Be(5);
        enemy.Defense.Should().Be(1);
    }

    [Fact]
    public void RoomBuilder_CreatesRoomWithEnemyAndLoot()
    {
        var goblin = new EnemyBuilder().Named("Goblin").WithHP(20).Build();
        var sword = new ItemBuilder().Named("Rusty Sword").WithDamage(5).Build();

        var room = new RoomBuilder()
            .Named("Test Chamber")
            .WithEnemy(goblin)
            .WithLoot(sword)
            .Build();

        room.Description.Should().Be("Test Chamber");
        room.Enemy.Should().NotBeNull();
        room.Enemy!.Name.Should().Be("Goblin");
        room.Items.Should().ContainSingle().Which.Name.Should().Be("Rusty Sword");
    }

    [Fact]
    public void ItemBuilder_CreatesWeaponWithDamage()
    {
        var sword = new ItemBuilder()
            .Named("Rusty Sword")
            .WithDamage(5)
            .WithTier(ItemTier.Common)
            .Build();

        sword.Name.Should().Be("Rusty Sword");
        sword.AttackBonus.Should().Be(5);
        sword.Type.Should().Be(ItemType.Weapon);
        sword.Tier.Should().Be(ItemTier.Common);
        sword.IsEquippable.Should().BeTrue();
    }

    [Fact]
    public void ItemBuilder_CreatesConsumableWithHeal()
    {
        var potion = new ItemBuilder()
            .Named("Health Potion")
            .WithHeal(25)
            .Build();

        potion.Name.Should().Be("Health Potion");
        potion.HealAmount.Should().Be(25);
        potion.Type.Should().Be(ItemType.Consumable);
        potion.IsEquippable.Should().BeFalse();
    }

    [Fact]
    public void PlayerBuilder_WithItem_AddsToInventory()
    {
        var sword = new ItemBuilder().Named("Sword").WithDamage(10).Build();
        var potion = new ItemBuilder().Named("Potion").WithHeal(20).Build();

        var player = new PlayerBuilder()
            .WithItem(sword)
            .WithItem(potion)
            .Build();

        player.Inventory.Should().HaveCount(2);
        player.Inventory.Select(i => i.Name).Should().Contain("Sword").And.Contain("Potion");
    }
}
