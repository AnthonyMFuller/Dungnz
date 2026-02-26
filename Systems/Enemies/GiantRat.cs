namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// A fast but weak rat that gains ATK bonuses in packs.
/// PackCount (1–3) is randomised on creation; each additional rat adds +2 ATK.
/// </summary>
public class GiantRat : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private GiantRat() { }

    /// <summary>Creates a Giant Rat with optional data-driven stats and a random pack size.</summary>
    public GiantRat(EnemyStats? stats = null, List<ItemStats>? itemConfig = null, Random? rng = null)
    {
        rng ??= new Random();
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
            Name = "Giant Rat";
            HP = MaxHP = 15;
            Attack = 7;
            Defense = 1;
            XPValue = 12;
            LootTable = new LootTable(minGold: 1, maxGold: 5);
        }

        // Pack bonus: 1–3 rats; each extra rat adds +2 ATK (max +4 for 3 rats)
        PackCount = rng.Next(1, 4);
        if (PackCount > 1)
        {
            Attack += 2 * (PackCount - 1);
        }

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var rodent = itemConfig?.FirstOrDefault(i => i.Name == "Rodent Pelt");
        if (rodent != null) LootTable.AddDrop(ItemConfig.CreateItem(rodent), 0.40);
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.20);
    }
}
