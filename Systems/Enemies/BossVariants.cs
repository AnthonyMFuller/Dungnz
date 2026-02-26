namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>The Lich King — a powerful undead mage boss.</summary>
public class LichKing : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public LichKing() : base(null, null) { Name = "Lich King"; HP = MaxHP = 120; Attack = 18; Defense = 5; XPValue = 100; AppliesPoisonOnHit = true; IsUndead = true; }

    /// <summary>Creates a LichKing with optional data-driven stats.</summary>
    public LichKing(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Lich King";
        HP = MaxHP = 120; Attack = 18; Defense = 5; XPValue = 100;
        AppliesPoisonOnHit = true;
        IsUndead = true;
    }
}
/// <summary>The Stone Titan — a massive golem boss.</summary>
public class StoneTitan : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public StoneTitan() : base(null, null) { Name = "Stone Titan"; HP = MaxHP = 200; Attack = 22; Defense = 15; XPValue = 100; }

    /// <summary>Creates a StoneTitan with optional data-driven stats.</summary>
    public StoneTitan(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Stone Titan";
        HP = MaxHP = 200; Attack = 22; Defense = 15; XPValue = 100;
    }
}
/// <summary>The Shadow Wraith — a speedy, evasive boss.</summary>
public class ShadowWraith : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public ShadowWraith() : base(null, null) { Name = "Shadow Wraith"; FlatDodgeChance = 0.25f; HP = MaxHP = 90; Attack = 25; Defense = 3; XPValue = 100; }

    /// <summary>Creates a ShadowWraith with optional data-driven stats.</summary>
    public ShadowWraith(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Shadow Wraith";
        FlatDodgeChance = 0.25f;
        HP = MaxHP = 90; Attack = 25; Defense = 3; XPValue = 100;
    }
}
/// <summary>The Vampire Lord — a lifesteal boss.</summary>
public class VampireBoss : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public VampireBoss() : base(null, null) { Name = "Vampire Lord"; LifestealPercent = 0.30f; HP = MaxHP = 110; Attack = 20; Defense = 8; XPValue = 100; }

    /// <summary>Creates a VampireBoss with optional data-driven stats.</summary>
    public VampireBoss(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Vampire Lord";
        LifestealPercent = 0.30f;
        HP = MaxHP = 110; Attack = 20; Defense = 8; XPValue = 100;
    }
}

/// <summary>The Archlich Sovereign — an ancient undead overlord of the Void Antechamber (Floor 6).</summary>
public class ArchlichSovereign : DungeonBoss
{
    /// <summary>Whether phase 2 has been activated (adds summoned at least once).</summary>
    public bool Phase2Triggered { get; set; }

    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public ArchlichSovereign() : base(null, null) { Name = "Archlich Sovereign"; HP = MaxHP = 180; Attack = 28; Defense = 14; XPValue = 150; IsUndead = true; }

    /// <summary>Creates an ArchlichSovereign with optional data-driven stats.</summary>
    public ArchlichSovereign(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Archlich Sovereign";
        HP = MaxHP = 180; Attack = 28; Defense = 14; XPValue = 150;
        IsUndead = true;
        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var crown = itemConfig?.FirstOrDefault(i => i.Name == "LichsCrown");
        if (crown != null) LootTable.AddDrop(ItemConfig.CreateItem(crown), 0.50);
        var soul = itemConfig?.FirstOrDefault(i => i.Name == "SoulGem") ?? itemConfig?.FirstOrDefault(i => i.Name == "Soul Gem");
        if (soul != null) LootTable.AddDrop(ItemConfig.CreateItem(soul), 0.30);
    }
}

/// <summary>The Abyssal Leviathan — a vast aquatic horror haunting the Bone Palace (Floor 7).</summary>
public class AbyssalLeviathan : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public AbyssalLeviathan() : base(null, null) { Name = "Abyssal Leviathan"; HP = MaxHP = 220; Attack = 32; Defense = 12; XPValue = 180; }

    /// <summary>Creates an AbyssalLeviathan with optional data-driven stats.</summary>
    public AbyssalLeviathan(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Abyssal Leviathan";
        HP = MaxHP = 220; Attack = 32; Defense = 12; XPValue = 180;
        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var scale = itemConfig?.FirstOrDefault(i => i.Name == "DragonScale") ?? itemConfig?.FirstOrDefault(i => i.Name == "Dragon Scale");
        if (scale != null) LootTable.AddDrop(ItemConfig.CreateItem(scale), 0.60);
        var soul = itemConfig?.FirstOrDefault(i => i.Name == "SoulGem") ?? itemConfig?.FirstOrDefault(i => i.Name == "Soul Gem");
        if (soul != null) LootTable.AddDrop(ItemConfig.CreateItem(soul), 0.25);
    }
}

/// <summary>The Infernal Dragon — the final boss of the Final Sanctum (Floor 8).</summary>
public class InfernalDragon : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public InfernalDragon() : base(null, null) { Name = "Infernal Dragon"; HP = MaxHP = 250; Attack = 36; Defense = 16; XPValue = 220; FlameBreathCooldown = 1; }

    /// <summary>Creates an InfernalDragon with optional data-driven stats.</summary>
    public InfernalDragon(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Infernal Dragon";
        HP = MaxHP = 250; Attack = 36; Defense = 16; XPValue = 220;
        FlameBreathCooldown = 1;
        AddLoot(itemConfig);
    }

    private void AddLoot(List<ItemStats>? itemConfig)
    {
        var sword = itemConfig?.FirstOrDefault(i => i.Name == "InfernalGreatsword");
        if (sword != null) LootTable.AddDrop(ItemConfig.CreateItem(sword), 0.40);
        var scale = itemConfig?.FirstOrDefault(i => i.Name == "DragonScale") ?? itemConfig?.FirstOrDefault(i => i.Name == "Dragon Scale");
        if (scale != null) LootTable.AddDrop(ItemConfig.CreateItem(scale), 0.60);
    }
}
