namespace Dungnz.Models;

/// <summary>
/// Represents the player character, tracking combat stats, inventory, equipment, mana, and
/// progression throughout the dungeon crawl. Exposes methods for taking damage, healing,
/// managing gold and XP, equipping items, and levelling up.
/// </summary>
public partial class Player
{
    /// <summary>Gets or sets the player's display name shown in UI and combat messages.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of status effects currently active on the player. Used for save/load
    /// persistence and is managed directly by <see cref="Dungnz.Systems.StatusEffectManager"/>.
    /// </summary>
    public List<ActiveEffect> ActiveEffects { get; } = new();

    /// <summary>Gets the list of minions currently summoned to fight alongside the player.</summary>
    public List<Minion> ActiveMinions { get; set; } = new();

    /// <summary>Tracks the MaxHP of the last enemy killed, used by Necromancer Raise Dead.</summary>
    public int LastKilledEnemyHp { get; set; }

    /// <summary>Gets the list of traps the player has placed that have not yet triggered.</summary>
    public List<Trap> ActiveTraps { get; set; } = new();

    /// <summary>Gets or sets whether a trap has already triggered during the current combat.</summary>
    public bool TrapTriggeredThisCombat { get; set; } = false;
}
