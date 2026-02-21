namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>The Lich King — a powerful undead mage boss.</summary>
public class LichKing : DungeonBoss
{
    public LichKing(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Lich King";
        if (stats == null) { HP = MaxHP = 120; Attack = 18; Defense = 5; XPValue = 100; }
        AppliesPoisonOnHit = true;
    }
}
/// <summary>The Stone Titan — a massive golem boss.</summary>
public class StoneTitan : DungeonBoss
{
    public StoneTitan(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Stone Titan";
        if (stats == null) { HP = MaxHP = 200; Attack = 22; Defense = 15; XPValue = 100; }
    }
}
/// <summary>The Shadow Wraith — a speedy, evasive boss.</summary>
public class ShadowWraith : DungeonBoss
{
    public ShadowWraith(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Shadow Wraith";
        FlatDodgeChance = 0.25f;
        if (stats == null) { HP = MaxHP = 90; Attack = 25; Defense = 3; XPValue = 100; }
    }
}
/// <summary>The Vampire Lord — a lifesteal boss.</summary>
public class VampireBoss : DungeonBoss
{
    public VampireBoss(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Vampire Lord";
        LifestealPercent = 0.30f;
        if (stats == null) { HP = MaxHP = 110; Attack = 20; Defense = 8; XPValue = 100; }
    }
}
