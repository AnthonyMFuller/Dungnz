namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Systems;

/// <summary>
/// Handles skill and ability usage during combat, including presenting the ability
/// and item menus to the player and executing the selected action.
/// </summary>
public interface IAbilityProcessor
{
    /// <summary>Presents the ability selection menu and executes the chosen ability.</summary>
    AbilityMenuResult HandleAbilityMenu(Player player, Enemy enemy);

    /// <summary>Presents the in-combat item menu and uses the selected consumable.</summary>
    AbilityMenuResult HandleItemMenu(Player player, Enemy enemy);
}
