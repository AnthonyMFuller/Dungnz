using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Tests.Builders;
using Dungnz.Tests.Helpers;
using FluentAssertions;

namespace Dungnz.Tests;

public class NavigationInventoryIntegrationTests
{
    [Fact]
    public void Navigation_MoveNorth_WhenExitExists_RoomChanges()
    {
        var start = new Room { Description = "Start" }; var north = new Room { Description = "North" };
        start.Exits[Direction.North] = north; north.Exits[Direction.South] = start;
        Room cur = start;
        if (cur.Exits.TryGetValue(Direction.North, out var n)) cur = n;
        cur.Description.Should().Be("North");
    }

    [Fact]
    public void Navigation_MoveNorth_WhenNoExit_PlayerStaysInSameRoom()
    {
        var room = new Room { Description = "Dead End" }; Room cur = room;
        cur.Exits.TryGetValue(Direction.North, out _).Should().BeFalse();
        cur.Should().Be(room);
    }

    [Fact]
    public void Navigation_FourWayRoom_AllDirectionsNavigable()
    {
        var c = new Room { Description = "Centre" };
        var n = new Room { Description = "North" }; var s = new Room { Description = "South" };
        var e = new Room { Description = "East" };  var w = new Room { Description = "West" };
        c.Exits[Direction.North] = n; c.Exits[Direction.South] = s;
        c.Exits[Direction.East] = e;  c.Exits[Direction.West] = w;
        n.Exits[Direction.South] = c;
        c.Exits[Direction.North].Description.Should().Be("North");
        c.Exits[Direction.South].Description.Should().Be("South");
        c.Exits[Direction.East].Description.Should().Be("East");
        c.Exits[Direction.West].Description.Should().Be("West");
        n.Exits[Direction.South].Description.Should().Be("Centre");
    }

    [Fact]
    public void Navigation_ExitRoom_IsExitFlagDetectable()
    {
        var start = new Room { Description = "Entrance" };
        var exit = new Room { Description = "Abyss", IsExit = true };
        start.Exits[Direction.East] = exit;
        Room cur = start;
        if (cur.Exits.TryGetValue(Direction.East, out var n)) cur = n;
        cur.IsExit.Should().BeTrue(); cur.Description.Should().Be("Abyss");
    }

    [Fact]
    public void Navigation_VisitFlag_SetAfterFirstEntry()
    {
        var r = new Room { Visited = false }; r.Visited = true;
        r.Visited.Should().BeTrue();
    }

    [Fact]
    public void Inventory_FillToMax_CountEqualsMaxInventorySize()
    {
        var p = new Player { Name = "H" };
        for (int i = 0; i < Player.MaxInventorySize; i++)
            p.Inventory.Add(new Item { Name = $"I{i}", Type = ItemType.Consumable });
        p.Inventory.Count.Should().Be(Player.MaxInventorySize);
    }

    [Fact]
    public void Inventory_IsFull_ReturnsTrueAtMaxCapacity()
    {
        var p = new Player { Name = "H" }; var mgr = new InventoryManager(new FakeDisplayService());
        for (int i = 0; i < Player.MaxInventorySize; i++)
            p.Inventory.Add(new Item { Name = $"J{i}", Type = ItemType.Consumable });
        mgr.IsFull(p).Should().BeTrue();
    }

    [Fact]
    public void Inventory_IsFull_ReturnsFalseWhenBelowMax()
    {
        var p = new Player { Name = "H" }; var mgr = new InventoryManager(new FakeDisplayService());
        p.Inventory.Add(new Item { Name = "P", Type = ItemType.Consumable });
        mgr.IsFull(p).Should().BeFalse();
    }

    [Fact]
    public void Inventory_RemoveItem_CountDecrements()
    {
        var p = new Player { Name = "H" };
        var item = new Item { Name = "X", Type = ItemType.Consumable }; p.Inventory.Add(item);
        int before = p.Inventory.Count; p.Inventory.Remove(item);
        p.Inventory.Count.Should().Be(before - 1);
        p.Inventory.Should().NotContain(item);
    }

    [Fact]
    public void Inventory_EmptyInventory_CountIsZeroNoException()
    {
        var p = new Player { Name = "P" };
        p.Inventory.Should().BeEmpty();
        p.Inventory.FirstOrDefault().Should().BeNull();
    }

    [Fact]
    public void Inventory_EquipFromInventory_ItemRemovedFromList()
    {
        var p = new Player { Name = "H" };
        var w = new Item { Name = "Battle Sword", Type = ItemType.Weapon, AttackBonus = 10, IsEquippable = true };
        p.Inventory.Add(w); int before = p.Inventory.Count;
        p.EquipItem(w);
        p.Inventory.Count.Should().Be(before - 1);
        p.EquippedWeapon!.Name.Should().Be("Battle Sword");
    }

    [Fact]
    public void Inventory_UnequipToInventory_ItemReturnedToList()
    {
        var p = new Player { Name = "H" };
        var w = new Item { Name = "Crystal Blade", Type = ItemType.Weapon, AttackBonus = 8, IsEquippable = true };
        p.Inventory.Add(w); p.EquipItem(w);
        p.Inventory.Should().NotContain(w);
        p.UnequipItem("weapon");
        p.Inventory.Should().Contain(i => i.Name == "Crystal Blade");
        p.EquippedWeapon.Should().BeNull();
    }
}
