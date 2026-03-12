using Dungnz.Models;

namespace Dungnz.Engine;

/// <summary>
/// Defines the contract for enemy AI decision-making during combat.
/// Each enemy type can implement its own ChooseAction logic for composable,
/// testable AI behaviors.
/// </summary>
public interface IEnemyAI
{
    /// <summary>
    /// Chooses the enemy's action for this turn based on combat state.
    /// </summary>
    /// <param name="self">The enemy performing the action.</param>
    /// <param name="player">The player being targeted.</param>
    /// <param name="context">Combat context with round number, player HP%, and floor info.</param>
    /// <returns>The action the enemy will attempt to perform.</returns>
    EnemyAction ChooseAction(Enemy self, Player player, CombatContext context);
}

/// <summary>
/// Provides contextual information about the current combat state,
/// used by <see cref="IEnemyAI"/> implementations to make tactical decisions.
/// </summary>
/// <param name="RoundNumber">The current combat round (1-based).</param>
/// <param name="PlayerHPPercent">The player's current HP as a fraction of MaxHP (0.0–1.0).</param>
/// <param name="CurrentFloor">The dungeon floor the combat is occurring on.</param>
public record CombatContext(int RoundNumber, double PlayerHPPercent, int CurrentFloor);

/// <summary>
/// Represents an action that an enemy can perform during combat.
/// </summary>
/// <param name="Type">The type of action to perform.</param>
/// <param name="Modifier">Optional modifier for the action (e.g., damage multiplier, accuracy reduction).</param>
public record EnemyAction(EnemyActionType Type, double Modifier = 1.0);

/// <summary>
/// Defines the types of actions an enemy can perform during combat.
/// </summary>
public enum EnemyActionType
{
    /// <summary>Standard melee attack.</summary>
    Attack,
    
    /// <summary>Aggressive double-damage attack.</summary>
    AggressiveAttack,
    
    /// <summary>Armor-piercing attack that ignores defense.</summary>
    ArmorPiercingAttack,
    
    /// <summary>Special ability that reduces player accuracy.</summary>
    BoneRattle,
    
    /// <summary>Attempt to flee from combat.</summary>
    Flee,
    
    /// <summary>Cower in fear, doing nothing.</summary>
    Cower,

    /// <summary>Enemy heals itself (Modifier * MaxHP HP restored). Skips attack.</summary>
    SelfHeal,

    /// <summary>Drains player mana then deals armor-piercing damage.</summary>
    DrainAttack
}
