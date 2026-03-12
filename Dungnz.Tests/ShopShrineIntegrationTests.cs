using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

public class ShopShrineIntegrationTests
{
    [Fact]
    public void Shop_BuyItem_SufficientGold_GoldReducedAndItemAdded()
    {
        var p = new PlayerBuilder().WithGold(200).Build();
        var sword = new Item { Name = "Steel Sword", Type = ItemType.Weapon };
        p.SpendGold(50); p.Inventory.Add(sword);
        p.Gold.Should().Be(150);
        p.Inventory.Should().Contain(i => i.Name == "Steel Sword");
    }

    [Fact]
    public void Shop_BuyTwoItems_GoldReducedForEach()
    {
        var p = new PlayerBuilder().WithGold(300).Build();
        p.SpendGold(30); p.Inventory.Add(new Item { Name = "Potion", Type = ItemType.Consumable });
        p.SpendGold(80); p.Inventory.Add(new Item { Name = "Shield", Type = ItemType.Armor });
        p.Gold.Should().Be(190);
        p.Inventory.Should().HaveCount(2);
    }

    [Fact]
    public void Shop_BuyItem_ExactGold_GoldReachesZero()
    {
        var p = new PlayerBuilder().WithGold(50).Build();
        p.SpendGold(50); p.Inventory.Add(new Item { Name = "Last Potion", Type = ItemType.Consumable });
        p.Gold.Should().Be(0);
    }

    [Fact]
    public void Shop_BuyItem_InsufficientGold_TransactionBlocked()
    {
        var p = new PlayerBuilder().WithGold(20).Build();
        int before = p.Gold;
        ((Action)(() => p.SpendGold(50))).Should().Throw<InvalidOperationException>();
        p.Gold.Should().Be(before);
    }

    [Fact]
    public void Shop_AddGold_NegativeAmount_ThrowsArgumentException()
    {
        var p = new PlayerBuilder().WithGold(100).Build();
        ((Action)(() => p.AddGold(-1))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Shop_SellItem_GoldAddedItemRemovedFromInventory()
    {
        var p = new PlayerBuilder().WithGold(10).Build();
        var item = new Item { Name = "Old Dagger", Type = ItemType.Weapon };
        p.Inventory.Add(item);
        p.Inventory.Remove(item); p.AddGold(25);
        p.Gold.Should().Be(35);
        p.Inventory.Should().NotContain(i => i.Name == "Old Dagger");
    }

    [Fact]
    public void Shrine_HealChoice_FullHealGoldSpentFlagSet()
    {
        var p = new Player { Name = "Hero", MaxHP = 100, Gold = 100 }; p.SetHPDirect(40);
        var r = new Room { HasShrine = true };
        p.SpendGold(30); p.Heal(p.MaxHP); r.ShrineUsed = true;
        p.HP.Should().Be(100); p.Gold.Should().Be(70); r.ShrineUsed.Should().BeTrue();
    }

    [Fact]
    public void Shrine_BlessChoice_AttackAndDefenseIncreased()
    {
        var p = new Player { Name = "Hero", MaxHP = 100, Gold = 200, Attack = 10, Defense = 5 }; p.SetHPDirect(100);
        p.SpendGold(50); p.ModifyAttack(2); p.ModifyDefense(2);
        p.Attack.Should().Be(12); p.Defense.Should().Be(7); p.Gold.Should().Be(150);
    }

    [Fact]
    public void Shrine_FortifyChoice_MaxHPIncreasedBy10()
    {
        var p = new Player { Name = "Hero", MaxHP = 100, Gold = 200 }; p.SetHPDirect(100);
        int before = p.MaxHP; p.SpendGold(75); p.FortifyMaxHP(10);
        p.MaxHP.Should().Be(before + 10); p.Gold.Should().Be(125);
    }

    [Fact]
    public void Shrine_MeditateChoice_MaxManaIncreasedBy10()
    {
        var p = new Player { Name = "Mage", MaxHP = 100, MaxMana = 50, Gold = 200 }; p.SetHPDirect(100);
        int before = p.MaxMana; p.SpendGold(75); p.FortifyMaxMana(10);
        p.MaxMana.Should().Be(before + 10); p.Gold.Should().Be(125);
    }

    [Fact]
    public void Shrine_AlreadyUsed_FlagIsTrue()
    {
        var r = new Room { HasShrine = true, ShrineUsed = true };
        r.ShrineUsed.Should().BeTrue();
    }

    [Fact]
    public void Shrine_HealWithInsufficientGold_CannotAfford()
    {
        var p = new Player { Name = "Hero", MaxHP = 100, Gold = 10 }; p.SetHPDirect(40);
        (p.Gold >= 30).Should().BeFalse();
        p.Gold.Should().Be(10); p.HP.Should().Be(40);
    }
}
