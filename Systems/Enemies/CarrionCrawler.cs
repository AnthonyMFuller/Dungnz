namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A worm-like scavenger that regenerates 5 HP each enemy turn.
/// </summary>
public class CarrionCrawler : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private CarrionCrawler() { }

    /// <summary>Creates a Carrion Crawler with optional data-driven stats.</summary>
    public CarrionCrawler(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
    {
        if (stats != null)
        {
            Name = stats.Name;
            HP = MaxHP = stats.MaxHP;
            Attack = stats.Attack;
            Defense = stats.Defense;
            XPValue = stats.XPValue;
            IsUndead = stats.IsUndead;
            LootTable = new LootTable(minGold: stats.MinGold, maxGold: stats.MaxGold);
            AsciiArt = stats.AsciiArt;
        }
        else
        {
            Name = "Carrion Crawler";
            HP = MaxHP = 35;
            Attack = 12;
            Defense = 4;
            XPValue = 30;
            LootTable = new LootTable(minGold: 5, maxGold: 14);
        }

        RegenPerTurn = 5;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.20);
    }
}
