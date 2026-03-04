using Dungnz.Display;
using Dungnz.Models;

namespace Dungnz.Systems;

/// <summary>
/// Necromancer class passive: heals 5 HP on each enemy defeat via the
/// GameEventBus. Subscribes to OnEnemyKilled events and fires only when
/// the player class is Necromancer.
/// </summary>
public class SoulHarvestPassive
{
    private readonly IDisplayService? _display;

    /// <summary>Total HP healed by Soul Harvest during this session.</summary>
    public int TotalHealed { get; private set; }

    /// <summary>Number of times Soul Harvest has triggered.</summary>
    public int TriggerCount { get; private set; }

    /// <summary>Creates a new Soul Harvest passive handler.</summary>
    public SoulHarvestPassive(IDisplayService? display = null)
    {
        _display = display;
    }

    /// <summary>Registers this passive on the given event bus.</summary>
    public void Register(GameEventBus bus)
    {
        bus.Subscribe<OnEnemyKilled>(HandleEnemyKilled);
    }

    /// <summary>Unregisters this passive from the given event bus to prevent memory leaks.</summary>
    public void Unregister(GameEventBus bus)
    {
        bus.Unsubscribe<OnEnemyKilled>(HandleEnemyKilled);
    }

    private void HandleEnemyKilled(OnEnemyKilled evt)
    {
        if (evt.Player.Class != PlayerClass.Necromancer) return;

        evt.Player.Heal(5);
        TotalHealed += 5;
        TriggerCount++;
        _display?.ShowCombatMessage("[Soul Harvest] You absorb the fallen essence. +5 HP");
    }
}
