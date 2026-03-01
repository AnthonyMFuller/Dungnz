using System.Text.Json;
using System.Text.Json.Serialization;
using Dungnz.Models;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;
using VerifyXunit;
using Xunit;

namespace Dungnz.Tests.Snapshots;

/// <summary>
/// Snapshot tests using Verify.Xunit to detect unintended serialization format changes.
/// Verified snapshots are committed alongside tests as *.verified.txt files.
/// </summary>
public class SerializationSnapshotTests
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public Task GameState_Serialization_MatchesSnapshot()
    {
        var player = new Player
        {
            Name = "SnapshotHero",
            MaxHP = 120,
            Attack = 15,
            Defense = 8,
            Level = 3,
            Gold = 50,
            XP = 150,
            Mana = 25,
            MaxMana = 30,
            Class = PlayerClass.Warrior
        };
        player.SetHPDirect(100);
        player.Inventory.Add(new Item
        {
            Id = "steel-sword",
            Name = "Steel Sword",
            Type = ItemType.Weapon,
            AttackBonus = 5,
            IsEquippable = true,
            Tier = ItemTier.Uncommon,
            Description = "A quality weapon."
        });

        var room = new Room
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Description = "A dimly lit chamber.",
            Type = RoomType.Dark,
            Visited = true
        };

        var state = new GameState(player, room, currentFloor: 2, seed: 42);

        var json = JsonSerializer.Serialize(new
        {
            state.Player.Name,
            state.Player.HP,
            state.Player.MaxHP,
            state.Player.Attack,
            state.Player.Defense,
            state.Player.Level,
            state.Player.Gold,
            state.Player.XP,
            state.Player.Class,
            InventoryCount = state.Player.Inventory.Count,
            FirstItem = state.Player.Inventory.FirstOrDefault()?.Name,
            state.CurrentFloor,
            state.Seed,
            RoomDescription = state.CurrentRoom.Description,
            RoomType = state.CurrentRoom.Type.ToString()
        }, SnapshotJsonOptions);

        return Verifier.Verify(json);
    }

    [Fact]
    public Task Enemy_Serialization_MatchesSnapshot()
    {
        var goblin = new Goblin();
        var json = JsonSerializer.Serialize<Enemy>(goblin, SnapshotJsonOptions);
        return Verifier.Verify(json);
    }

    [Fact]
    public Task CombatRoundResult_Format_MatchesSnapshot()
    {
        // Simulate a combat round result structure
        var result = new
        {
            PlayerName = "TestHero",
            PlayerHP = 85,
            PlayerMaxHP = 100,
            EnemyName = "Goblin",
            EnemyHP = 0,
            EnemyMaxHP = 20,
            DamageDealt = 8,
            DamageTaken = 3,
            Outcome = CombatResult.Won.ToString(),
            XPEarned = 15,
            GoldEarned = 5,
            LootDropped = "Short Sword"
        };

        var json = JsonSerializer.Serialize(result, SnapshotJsonOptions);
        return Verifier.Verify(json);
    }
}
