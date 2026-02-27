namespace Dungnz.Systems.Enemies;
using Dungnz.Models;
using Dungnz.Systems;

// ─── Floor 1–5 Named Bosses ──────────────────────────────────────────────────

/// <summary>
/// Goblin Warchief — Floor 1 boss. A hulking goblin draped in looted armor, more trophies
/// than defense. His yellowed eyes track you like you're already dead. The warband answers
/// his call even as his blood hits the stone.
/// </summary>
public class GoblinWarchief : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public GoblinWarchief() : base(null, null)
    {
        Name = "Goblin Warchief"; HP = MaxHP = 60; Attack = 10; Defense = 3; XPValue = 60;
        FloorNumber = 1;
        SpecialAbilityDescription = "Calls minion reinforcements at 50% HP.";
    }
    /// <summary>Creates a GoblinWarchief with optional data-driven stats.</summary>
    public GoblinWarchief(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Goblin Warchief"; HP = MaxHP = 60; Attack = 10; Defense = 3; XPValue = 60;
        FloorNumber = 1;
        SpecialAbilityDescription = "Calls minion reinforcements at 50% HP.";
    }
}

/// <summary>
/// Plague Hound Alpha — Floor 2 boss. A disease-bloated hound whose matted fur weeps
/// infection with every shuddering breath. It doesn't hunt for hunger — it hunts to spread.
/// When cornered and bleeding, it becomes something far worse.
/// </summary>
public class PlagueHoundAlpha : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public PlagueHoundAlpha() : base(null, null)
    {
        Name = "Plague Hound Alpha"; HP = MaxHP = 80; Attack = 13; Defense = 4; XPValue = 75;
        AppliesPoisonOnHit = true;
        FloorNumber = 2;
        SpecialAbilityDescription = "Poisons every hit; enters a frenzy at 40% HP (+5 ATK).";
    }
    /// <summary>Creates a PlagueHoundAlpha with optional data-driven stats.</summary>
    public PlagueHoundAlpha(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Plague Hound Alpha"; HP = MaxHP = 80; Attack = 13; Defense = 4; XPValue = 75;
        AppliesPoisonOnHit = true;
        FloorNumber = 2;
        SpecialAbilityDescription = "Poisons every hit; enters a frenzy at 40% HP (+5 ATK).";
    }
}

/// <summary>
/// Iron Sentinel — Floor 3 boss. An ancient automaton whose joints have fused solid from
/// centuries of motionless vigil. It doesn't speak, it doesn't feel — it simply enforces.
/// Its plating absorbs punishment that would shatter lesser things, and a single grip
/// can leave a warrior unable to move.
/// </summary>
public class IronSentinel : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public IronSentinel() : base(null, null)
    {
        Name = "Iron Sentinel"; HP = MaxHP = 110; Attack = 14; Defense = 10; XPValue = 90;
        FloorNumber = 3;
        SpecialAbilityDescription = "50% damage reduction from plating; stuns the player at 60% HP.";
    }
    /// <summary>Creates an IronSentinel with optional data-driven stats.</summary>
    public IronSentinel(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Iron Sentinel"; HP = MaxHP = 110; Attack = 14; Defense = 10; XPValue = 90;
        FloorNumber = 3;
        SpecialAbilityDescription = "50% damage reduction from plating; stuns the player at 60% HP.";
    }
}

/// <summary>
/// Bone Archon — Floor 4 boss. A towering skeleton in the robes of a high priest, its
/// eye sockets burning with cold violet flame. It has commanded the dead for so long it
/// no longer distinguishes ally from enemy — only servants and future servants.
/// </summary>
public class BoneArchon : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public BoneArchon() : base(null, null)
    {
        Name = "Bone Archon"; HP = MaxHP = 130; Attack = 16; Defense = 6; XPValue = 110;
        IsUndead = true;
        FloorNumber = 4;
        SpecialAbilityDescription = "Weakened on every 3rd hit received; raises a skeleton when it kills.";
    }
    /// <summary>Creates a BoneArchon with optional data-driven stats.</summary>
    public BoneArchon(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Bone Archon"; HP = MaxHP = 130; Attack = 16; Defense = 6; XPValue = 110;
        IsUndead = true;
        FloorNumber = 4;
        SpecialAbilityDescription = "Weakened on every 3rd hit received; raises a skeleton when it kills.";
    }
}

/// <summary>
/// Crimson Vampire — Floor 5 boss. She moves like smoke and strikes like a blade thrown
/// from shadow. Every wound she inflicts feeds her, and when her prey runs dry of power
/// she draws on something darker still. She has not lost a hunt in three hundred years.
/// </summary>
public class CrimsonVampire : DungeonBoss
{
    /// <summary>Parameterless constructor used by the JSON deserializer.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public CrimsonVampire() : base(null, null)
    {
        Name = "Crimson Vampire"; HP = MaxHP = 150; Attack = 18; Defense = 7; XPValue = 130;
        LifestealPercent = 0.30f;
        FloorNumber = 5;
        SpecialAbilityDescription = "30% life steal on every hit; drains player MP at 25% HP.";
    }
    /// <summary>Creates a CrimsonVampire with optional data-driven stats.</summary>
    public CrimsonVampire(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Crimson Vampire"; HP = MaxHP = 150; Attack = 18; Defense = 7; XPValue = 130;
        LifestealPercent = 0.30f;
        FloorNumber = 5;
        SpecialAbilityDescription = "30% life steal on every hit; drains player MP at 25% HP.";
    }
}

// ─── Legacy random-pool bosses (kept for compatibility) ──────────────────────

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
    public ArchlichSovereign() : base(null, null) { Name = "Archlich Sovereign"; HP = MaxHP = 180; Attack = 42; Defense = 14; XPValue = 150; IsUndead = true; }

    /// <summary>Creates an ArchlichSovereign with optional data-driven stats.</summary>
    public ArchlichSovereign(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Archlich Sovereign";
        HP = MaxHP = 180; Attack = 42; Defense = 14; XPValue = 150;
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
    public AbyssalLeviathan() : base(null, null) { Name = "Abyssal Leviathan"; HP = MaxHP = 220; Attack = 48; Defense = 12; XPValue = 180; }

    /// <summary>Creates an AbyssalLeviathan with optional data-driven stats.</summary>
    public AbyssalLeviathan(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Abyssal Leviathan";
        HP = MaxHP = 220; Attack = 48; Defense = 12; XPValue = 180;
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
    public InfernalDragon() : base(null, null) { Name = "Infernal Dragon"; HP = MaxHP = 250; Attack = 54; Defense = 16; XPValue = 220; FlameBreathCooldown = 2; }

    /// <summary>Creates an InfernalDragon with optional data-driven stats.</summary>
    public InfernalDragon(EnemyStats? stats, List<ItemStats>? itemConfig) : base(stats, itemConfig)
    {
        Name = "Infernal Dragon";
        HP = MaxHP = 250; Attack = 54; Defense = 16; XPValue = 220;
        FlameBreathCooldown = 2; // first breath fires on turn 2 (decrement-first)
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
