namespace Dungnz.Models;

public abstract class Enemy
{
    public string Name { get; set; } = string.Empty;
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int XPValue { get; set; }
    public LootTable LootTable { get; set; } = null!;

    // Special mechanic flags
    public bool IsImmuneToEffects { get; protected set; }
    public float FlatDodgeChance { get; protected set; } = -1f; // -1 = use DEF-based formula
    public float LifestealPercent { get; protected set; }
    public bool AppliesPoisonOnHit { get; protected set; }
    public bool IsAmbush { get; set; } // first-turn surprise
}
