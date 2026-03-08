namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Handles skill and ability usage during combat, including presenting the ability
/// and item menus to the player and executing the selected action.
/// Migrated from <see cref="CombatEngine"/> as part of the decomposition task (#1205).
/// </summary>
public class AbilityProcessor : IAbilityProcessor
{
    private readonly IDisplayService _display;
    private readonly AbilityManager _abilities;
    private readonly StatusEffectManager _statusEffects;
    private readonly InventoryManager _inventoryManager;
    private RunStats _stats = new();

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
    public void SetStats(RunStats stats) => _stats = stats;

    /// <inheritdoc/>
    public AbilityMenuResult HandleAbilityMenu(Player player, Enemy enemy)
    {
        if (_statusEffects.HasEffect(player, StatusEffect.Silence))
        {
            _display.ShowCombatMessage("You are silenced and cannot use abilities!");
            return AbilityMenuResult.Cancel;
        }

        var unlocked = _abilities.GetUnlockedAbilities(player);
        if (!unlocked.Any())
        {
            _display.ShowMessage("You haven't unlocked any abilities yet!");
            return AbilityMenuResult.Cancel;
        }

        // Classify abilities into available and unavailable
        var unavailable = new List<(Ability, bool, int, bool)>();
        var available = new List<Ability>();
        foreach (var ability in unlocked)
        {
            if (_abilities.IsOnCooldown(ability.Type))
                unavailable.Add((ability, true, _abilities.GetCooldown(ability.Type), false));
            else if (player.Mana < ability.ManaCost)
                unavailable.Add((ability, false, 0, true));
            else
                available.Add(ability);
        }

        var selectedAbility = _display.ShowAbilityMenuAndSelect(unavailable, available);
        if (selectedAbility == null)
            return AbilityMenuResult.Cancel;

        // Execute the selected ability
        var hpBeforeAbility = enemy.HP;
        var result = _abilities.UseAbility(player, enemy, selectedAbility.Type, _statusEffects, _display);

        if (result == UseAbilityResult.Success)
        {
            _display.ShowMessage($"{ColorCodes.Bold}{ColorCodes.Yellow}[{selectedAbility.Name} activated — {selectedAbility.Description}]{ColorCodes.Reset}");
            // Bug #111: track ability damage in run stats
            if (enemy.HP < hpBeforeAbility)
                _stats.DamageDealt += hpBeforeAbility - enemy.HP;
            _stats.AbilitiesUsed++;
            return AbilityMenuResult.Used;
        }

        _display.ShowMessage($"Cannot use ability: {result}");
        return AbilityMenuResult.Cancel;
    }

    /// <inheritdoc/>
    public AbilityMenuResult HandleItemMenu(Player player, Enemy enemy)
    {
        var consumables = player.Inventory
            .Where(i => i.Type == ItemType.Consumable)
            .ToList();

        if (consumables.Count == 0)
        {
            _display.ShowMessage("You have no usable items!");
            return AbilityMenuResult.Cancel;
        }

        var selected = _display.ShowCombatItemMenuAndSelect(consumables);
        if (selected == null) return AbilityMenuResult.Cancel;

        _inventoryManager.UseItem(player, selected.Name);
        return AbilityMenuResult.Used;
    }
}
