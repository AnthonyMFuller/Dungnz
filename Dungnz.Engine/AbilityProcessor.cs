namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Stub implementation of <see cref="IAbilityProcessor"/>. Logic will be migrated from
/// <see cref="CombatEngine"/> in a follow-up decomposition task.
/// </summary>
public class AbilityProcessor : IAbilityProcessor
{
    private readonly IDisplayService _display;
    private readonly AbilityManager _abilities;
    private readonly StatusEffectManager _statusEffects;
    private readonly InventoryManager _inventoryManager;

    /// <summary>Initialises a new <see cref="AbilityProcessor"/> with the required dependencies.</summary>
    public AbilityProcessor(
        IDisplayService display,
        AbilityManager abilities,
        StatusEffectManager statusEffects,
        InventoryManager inventoryManager)
    {
        _display = display;
        _abilities = abilities;
        _statusEffects = statusEffects;
        _inventoryManager = inventoryManager;
    }

    /// <inheritdoc/>
    public AbilityMenuResult HandleAbilityMenu(Player player, Enemy enemy) => AbilityMenuResult.Cancel;

    /// <inheritdoc/>
    public AbilityMenuResult HandleItemMenu(Player player, Enemy enemy) => AbilityMenuResult.Cancel;
}
