namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// An undead ranged combatant. On the first attack only: 20% crit chance that
/// deals 3× damage (standard crit is 2×; +50% bonus = 3×).
/// </summary>
public class BoneArcher : Enemy
{
    [System.Text.Json.Serialization.JsonConstructor]
    private BoneArcher() { }

    /// <summary>Creates a Bone Archer with optional data-driven stats.</summary>
    public BoneArcher(EnemyStats? stats = null, List<ItemStats>? itemConfig = null)
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
            Name = "Bone Archer";
            HP = MaxHP = 38;
            Attack = 14;
            Defense = 5;
            XPValue = 33;
            IsUndead = true;
            LootTable = new LootTable(minGold: 8, maxGold: 18);
        }

        // First attack: 20% crit at 3× damage modelled as 1.5× multiplier on first hit
        // (standard crit doubles, so 1.5× on top = 3×; handled in CombatEngine via
        //  FirstAttackMultiplier combined with FirstAttackCritChance)
        FirstAttackMultiplier = 1.5f;
        FirstAttackCritChance = 0.20f;

        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var fragment = itemConfig?.FirstOrDefault(i => i.Name == "Bone Fragment");
        if (fragment != null) LootTable.AddDrop(ItemConfig.CreateItem(fragment), 0.40);
        var potion = itemConfig?.FirstOrDefault(i => i.Name == "Health Potion");
        if (potion != null) LootTable.AddDrop(ItemConfig.CreateItem(potion), 0.20);
    }
}
