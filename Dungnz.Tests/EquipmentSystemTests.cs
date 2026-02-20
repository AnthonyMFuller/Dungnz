namespace Dungnz.Tests;

using Dungnz.Models;
using Xunit;

public class EquipmentSystemTests
{
    [Fact]
    public void EquipWeapon_AppliesAttackBonus()
    {
        var player = new Player { Name = "Test" };
        var sword = new Item
        {
            Name = "Iron Sword",
            Type = ItemType.Weapon,
            AttackBonus = 5,
            DefenseBonus = 0
        };
        player.Inventory.Add(sword);

        var initialAttack = player.Attack;
        player.EquipItem(sword);

        Assert.Equal(initialAttack + 5, player.Attack);
        Assert.Equal(sword, player.EquippedWeapon);
        Assert.DoesNotContain(sword, player.Inventory);
    }

    [Fact]
    public void EquipArmor_AppliesDefenseBonus()
    {
        var player = new Player { Name = "Test" };
        var armor = new Item
        {
            Name = "Leather Armor",
            Type = ItemType.Armor,
            AttackBonus = 0,
            DefenseBonus = 3
        };
        player.Inventory.Add(armor);

        var initialDefense = player.Defense;
        player.EquipItem(armor);

        Assert.Equal(initialDefense + 3, player.Defense);
        Assert.Equal(armor, player.EquippedArmor);
        Assert.DoesNotContain(armor, player.Inventory);
    }

    [Fact]
    public void EquipAccessory_AppliesStatModifier()
    {
        var player = new Player { Name = "Test" };
        var ring = new Item
        {
            Name = "Health Ring",
            Type = ItemType.Accessory,
            StatModifier = 20
        };
        player.Inventory.Add(ring);

        var initialMaxHP = player.MaxHP;
        player.EquipItem(ring);

        Assert.Equal(initialMaxHP + 20, player.MaxHP);
        Assert.Equal(ring, player.EquippedAccessory);
        Assert.DoesNotContain(ring, player.Inventory);
    }

    [Fact]
    public void EquipItem_SwapsOccupiedSlot()
    {
        var player = new Player { Name = "Test" };
        var sword1 = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        var sword2 = new Item { Name = "Steel Sword", Type = ItemType.Weapon, AttackBonus = 8 };
        player.Inventory.Add(sword1);
        player.Inventory.Add(sword2);

        player.EquipItem(sword1);
        var attackAfterFirst = player.Attack;

        player.EquipItem(sword2);

        Assert.Equal(sword2, player.EquippedWeapon);
        Assert.Contains(sword1, player.Inventory);
        Assert.DoesNotContain(sword2, player.Inventory);
        Assert.Equal(attackAfterFirst - 5 + 8, player.Attack);
    }

    [Fact]
    public void UnequipWeapon_RemovesStatBonus()
    {
        var player = new Player { Name = "Test" };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        player.Inventory.Add(sword);
        player.EquipItem(sword);

        var attackWithWeapon = player.Attack;
        player.UnequipItem("weapon");

        Assert.Equal(attackWithWeapon - 5, player.Attack);
        Assert.Null(player.EquippedWeapon);
        Assert.Contains(sword, player.Inventory);
    }

    [Fact]
    public void UnequipArmor_RemovesStatBonus()
    {
        var player = new Player { Name = "Test" };
        var armor = new Item { Name = "Leather Armor", Type = ItemType.Armor, DefenseBonus = 3 };
        player.Inventory.Add(armor);
        player.EquipItem(armor);

        var defenseWithArmor = player.Defense;
        player.UnequipItem("armor");

        Assert.Equal(defenseWithArmor - 3, player.Defense);
        Assert.Null(player.EquippedArmor);
        Assert.Contains(armor, player.Inventory);
    }

    [Fact]
    public void UnequipAccessory_RemovesStatBonus()
    {
        var player = new Player { Name = "Test" };
        var ring = new Item { Name = "Health Ring", Type = ItemType.Accessory, StatModifier = 20 };
        player.Inventory.Add(ring);
        player.EquipItem(ring);

        var maxHPWithRing = player.MaxHP;
        player.UnequipItem("accessory");

        Assert.Equal(maxHPWithRing - 20, player.MaxHP);
        Assert.Null(player.EquippedAccessory);
        Assert.Contains(ring, player.Inventory);
    }

    [Fact]
    public void UnequipEmptySlot_ThrowsException()
    {
        var player = new Player { Name = "Test" };

        Assert.Throws<InvalidOperationException>(() => player.UnequipItem("weapon"));
        Assert.Throws<InvalidOperationException>(() => player.UnequipItem("armor"));
        Assert.Throws<InvalidOperationException>(() => player.UnequipItem("accessory"));
    }

    [Fact]
    public void EquipItemNotInInventory_ThrowsException()
    {
        var player = new Player { Name = "Test" };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };

        Assert.Throws<ArgumentException>(() => player.EquipItem(sword));
    }

    [Fact]
    public void EquipNonEquippableItem_ThrowsException()
    {
        var player = new Player { Name = "Test" };
        var potion = new Item { Name = "Health Potion", Type = ItemType.Consumable, HealAmount = 30 };
        player.Inventory.Add(potion);

        Assert.Throws<ArgumentException>(() => player.EquipItem(potion));
    }

    [Fact]
    public void UnequipInvalidSlotName_ThrowsException()
    {
        var player = new Player { Name = "Test" };

        Assert.Throws<ArgumentException>(() => player.UnequipItem("invalid"));
        Assert.Throws<ArgumentException>(() => player.UnequipItem("helmet"));
    }

    [Fact]
    public void EquipWeaponToWrongSlot_PreventedByType()
    {
        var player = new Player { Name = "Test" };
        var sword = new Item { Name = "Iron Sword", Type = ItemType.Weapon, AttackBonus = 5 };
        player.Inventory.Add(sword);

        player.EquipItem(sword);

        Assert.Equal(sword, player.EquippedWeapon);
        Assert.Null(player.EquippedArmor);
        Assert.Null(player.EquippedAccessory);
    }

    [Fact]
    public void StatBonuses_ApplyAndRemoveCorrectly()
    {
        var player = new Player { Name = "Test" };
        var sword = new Item { Name = "Sword", Type = ItemType.Weapon, AttackBonus = 5, DefenseBonus = 1 };
        var armor = new Item { Name = "Armor", Type = ItemType.Armor, AttackBonus = 0, DefenseBonus = 3 };
        var ring = new Item { Name = "Ring", Type = ItemType.Accessory, AttackBonus = 2, DefenseBonus = 2, StatModifier = 10 };
        
        player.Inventory.Add(sword);
        player.Inventory.Add(armor);
        player.Inventory.Add(ring);

        var baseAttack = player.Attack;
        var baseDefense = player.Defense;
        var baseMaxHP = player.MaxHP;

        player.EquipItem(sword);
        player.EquipItem(armor);
        player.EquipItem(ring);

        Assert.Equal(baseAttack + 5 + 2, player.Attack);
        Assert.Equal(baseDefense + 1 + 3 + 2, player.Defense);
        Assert.Equal(baseMaxHP + 10, player.MaxHP);

        player.UnequipItem("weapon");
        player.UnequipItem("armor");
        player.UnequipItem("accessory");

        Assert.Equal(baseAttack, player.Attack);
        Assert.Equal(baseDefense, player.Defense);
        Assert.Equal(baseMaxHP, player.MaxHP);
    }

    [Fact]
    public void MaxHPIncrease_ProportionallyHealsPlayer()
    {
        var player = new Player { Name = "Test" };
        player.TakeDamage(50); // HP: 50/100
        
        var ring = new Item { Name = "Health Ring", Type = ItemType.Accessory, StatModifier = 20 };
        player.Inventory.Add(ring);
        player.EquipItem(ring);

        Assert.Equal(120, player.MaxHP);
        Assert.Equal(70, player.HP); // 50 + 20 proportional heal
    }

    [Fact]
    public void MaxHPDecrease_ClampsCurrentHP()
    {
        var player = new Player { Name = "Test" };
        var ring = new Item { Name = "Health Ring", Type = ItemType.Accessory, StatModifier = 20 };
        player.Inventory.Add(ring);
        player.EquipItem(ring);

        Assert.Equal(120, player.MaxHP);
        Assert.Equal(120, player.HP);

        player.UnequipItem("accessory");

        Assert.Equal(100, player.MaxHP);
        Assert.Equal(100, player.HP); // Clamped down
    }
}
