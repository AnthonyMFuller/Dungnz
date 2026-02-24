namespace Dungnz.Systems;
using Dungnz.Models;
using Dungnz.Display;

/// <summary>
/// Manages the player's combat abilities: tracking unlock eligibility by level, enforcing
/// cooldown turns, verifying mana costs, and executing ability effects during combat.
/// </summary>
public class AbilityManager
{
    private readonly Dictionary<AbilityType, int> _cooldowns = new();
    private readonly List<Ability> _abilities;

    /// <summary>Flavor text displayed immediately before each ability activates.</summary>
    private static readonly Dictionary<string, string> _abilityFlavor = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PowerStrike"]     = "You focus every ounce of strength into a single devastating blow!",
        ["DefensiveStance"] = "You lower your center of gravity and raise your guard, ready for anything.",
        ["PoisonDart"]      = "You notch a venomed bolt and loose it with deadly precision.",
        ["SecondWind"]      = "Warm golden light flows through you, knitting wounds closed.",
    };
    
    /// <summary>
    /// Initialises the manager and registers all available abilities with their names,
    /// descriptions, mana costs, cooldown turns, unlock levels, and ability types.
    /// </summary>
    public AbilityManager()
    {
        _abilities = new List<Ability>
        {
            // Warrior abilities
            new Ability("Shield Bash", "Strike with shield, stunning the enemy", 8, 2, 1, AbilityType.ShieldBash) 
                { ClassRestriction = PlayerClass.Warrior },
            new Ability("Battle Cry", "Rallying cry boosting attack power", 10, 4, 2, AbilityType.BattleCry) 
                { ClassRestriction = PlayerClass.Warrior },
            new Ability("Fortify", "Fortify defenses, reducing incoming damage", 12, 3, 3, AbilityType.Fortify) 
                { ClassRestriction = PlayerClass.Warrior },
            new Ability("Reckless Blow", "Massive damage but reduces defense", 15, 3, 5, AbilityType.RecklessBlow) 
                { ClassRestriction = PlayerClass.Warrior },
            new Ability("Last Stand", "Desperate stand at low HP", 20, 6, 7, AbilityType.LastStand) 
                { ClassRestriction = PlayerClass.Warrior },

            // Mage abilities
            new Ability("Arcane Bolt", "Launch arcane energy dealing magical damage", 8, 0, 1, AbilityType.ArcaneBolt) 
                { ClassRestriction = PlayerClass.Mage },
            new Ability("Frost Nova", "Freezing blast that slows the enemy", 14, 3, 2, AbilityType.FrostNova) 
                { ClassRestriction = PlayerClass.Mage },
            new Ability("Mana Shield", "Absorb damage with mana", 0, 5, 4, AbilityType.ManaShield) 
                { ClassRestriction = PlayerClass.Mage },
            new Ability("Arcane Sacrifice", "Sacrifice HP to restore mana", 0, 3, 5, AbilityType.ArcaneSacrifice) 
                { ClassRestriction = PlayerClass.Mage },
            new Ability("Meteor", "Call down a devastating meteor", 35, 5, 7, AbilityType.Meteor) 
                { ClassRestriction = PlayerClass.Mage },

            // Rogue abilities
            new Ability("Quick Strike", "Fast strike generating combo points", 5, 0, 1, AbilityType.QuickStrike) 
                { ClassRestriction = PlayerClass.Rogue },
            new Ability("Backstab", "Strike from behind with bonus damage", 10, 2, 2, AbilityType.Backstab) 
                { ClassRestriction = PlayerClass.Rogue },
            new Ability("Evade", "Dodge the next enemy attack", 12, 4, 3, AbilityType.Evade) 
                { ClassRestriction = PlayerClass.Rogue },
            new Ability("Flurry", "Rapid succession of attacks", 15, 3, 5, AbilityType.Flurry) 
                { ClassRestriction = PlayerClass.Rogue },
            new Ability("Assassinate", "Execute enemy with massive damage", 25, 6, 7, AbilityType.Assassinate) 
                { ClassRestriction = PlayerClass.Rogue }
        };
    }
    
    /// <summary>
    /// Returns all abilities whose required unlock level is less than or equal to the player's
    /// current level and match the player's class (or have no class restriction), regardless of cooldown or mana.
    /// </summary>
    /// <param name="player">The player whose level and class determine which abilities are accessible.</param>
    /// <returns>A list of abilities the player has unlocked.</returns>
    public List<Ability> GetUnlockedAbilities(Player player)
    {
        return _abilities
            .Where(a => a.UnlockLevel <= player.Level)
            .Where(a => a.ClassRestriction == null || a.ClassRestriction == player.Class)
            .ToList();
    }
    
    /// <summary>
    /// Returns the subset of unlocked abilities that are not currently on cooldown and whose
    /// mana cost the player can afford right now.
    /// </summary>
    /// <param name="player">The player whose level and current mana are checked.</param>
    /// <returns>A list of abilities ready to use this turn.</returns>
    public List<Ability> GetAvailableAbilities(Player player)
    {
        return GetUnlockedAbilities(player)
            .Where(a => !IsOnCooldown(a.Type) && player.Mana >= a.ManaCost)
            .ToList();
    }
    
    /// <summary>
    /// Returns <see langword="true"/> if the specified ability type has remaining cooldown turns and cannot be used.
    /// </summary>
    /// <param name="type">The ability type to check.</param>
    /// <returns><see langword="true"/> if the ability is on cooldown; otherwise <see langword="false"/>.</returns>
    public bool IsOnCooldown(AbilityType type)
    {
        return _cooldowns.ContainsKey(type) && _cooldowns[type] > 0;
    }
    
    /// <summary>
    /// Returns the number of turns remaining before the specified ability can be used again.
    /// Returns 0 if the ability is not on cooldown.
    /// </summary>
    /// <param name="type">The ability type to query.</param>
    /// <returns>Remaining cooldown turns, or 0 if ready.</returns>
    public int GetCooldown(AbilityType type)
    {
        return _cooldowns.ContainsKey(type) ? _cooldowns[type] : 0;
    }
    
    /// <summary>
    /// Registers the specified ability as being on cooldown for the given number of turns,
    /// preventing its use until <see cref="TickCooldowns"/> has been called that many times.
    /// Applies cooldown reduction from the Relentless passive (reduces Flurry/Assassinate by 1).
    /// </summary>
    /// <param name="type">The ability type to put on cooldown.</param>
    /// <param name="turns">The number of turns before the ability becomes available again.</param>
    /// <param name="player">The player using the ability (optional, for cooldown reduction).</param>
    public void PutOnCooldown(AbilityType type, int turns, Player? player = null)
    {
        if (player != null)
        {
            var reduction = player.GetCooldownReduction(type);
            turns = Math.Max(0, turns - reduction);
        }
        _cooldowns[type] = turns;
    }
    
    /// <summary>
    /// Clears all active cooldowns. Should be called at the start of each new combat so
    /// cooldowns from a previous fight do not carry over.
    /// </summary>
    public void ResetCooldowns()
    {
        _cooldowns.Clear();
    }

    /// <summary>
    /// Decrements the remaining cooldown for every ability currently on cooldown by one turn.
    /// Should be called once at the end of each player turn.
    /// </summary>
    public void TickCooldowns()
    {
        var keys = _cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            if (_cooldowns[key] > 0)
                _cooldowns[key]--;
        }
    }
    
    /// <summary>
    /// Looks up and returns the <see cref="Ability"/> definition for the given type,
    /// or <see langword="null"/> if no matching ability is registered.
    /// </summary>
    /// <param name="type">The ability type to retrieve.</param>
    /// <returns>The matching <see cref="Ability"/>, or <see langword="null"/> if not found.</returns>
    public Ability? GetAbility(AbilityType type)
    {
        return _abilities.FirstOrDefault(a => a.Type == type);
    }
    
    /// <summary>
    /// Attempts to use the specified ability in the current combat encounter. Validates level,
    /// cooldown, and mana requirements before spending mana, applying the cooldown, and executing
    /// the ability's combat effect (damage, buff, debuff, or healing).
    /// </summary>
    /// <param name="player">The player attempting to use the ability.</param>
    /// <param name="enemy">The current enemy target for offensive abilities.</param>
    /// <param name="type">The ability type to execute.</param>
    /// <param name="statusEffects">The active status effect manager for applying buffs and debuffs.</param>
    /// <param name="display">The display service for emitting combat feedback messages.</param>
    /// <returns>A <see cref="UseAbilityResult"/> indicating whether the ability was used or why it failed.</returns>
    public UseAbilityResult UseAbility(Player player, Enemy enemy, AbilityType type, StatusEffectManager statusEffects, IDisplayService display)
    {
        var ability = GetAbility(type);
        if (ability == null)
            return UseAbilityResult.InvalidAbility;
        
        if (ability.UnlockLevel > player.Level)
            return UseAbilityResult.NotUnlocked;
        
        if (IsOnCooldown(type))
            return UseAbilityResult.OnCooldown;
        
        // Calculate effective mana cost with Spell Weaver passive
        int effectiveCost = ability.ManaCost;
        if (player.Class == PlayerClass.Mage && ability.ClassRestriction == PlayerClass.Mage)
        {
            effectiveCost = Math.Max(1, (int)(ability.ManaCost * player.GetSpellCostMultiplier()));
        }
        
        if (player.Mana < effectiveCost)
            return UseAbilityResult.InsufficientMana;
        
        player.SpendMana(effectiveCost);
        PutOnCooldown(type, ability.CooldownTurns, player);

        if (_abilityFlavor.TryGetValue(type.ToString(), out var flavorText))
            display.ShowCombatMessage(flavorText);

        switch (type)
        {
            // Old shared abilities - removed in Phase 2
            case AbilityType.PowerStrike:
                var damage = Math.Max(1, player.Attack * 2 - enemy.Defense);
                enemy.HP -= damage;
                display.ShowCombatMessage($"Power Strike! You deal {damage} damage to {enemy.Name}!");
                break;
                
            case AbilityType.DefensiveStance:
                statusEffects.Apply(player, StatusEffect.Fortified, 2);
                display.ShowCombatMessage("Defensive Stance activated! Your defense is boosted!");
                break;
                
            case AbilityType.PoisonDart:
                statusEffects.Apply(enemy, StatusEffect.Poison, 3);
                display.ShowCombatMessage($"Poison Dart! {enemy.Name} is poisoned!");
                break;
                
            case AbilityType.SecondWind:
                var healAmount = (int)(player.MaxHP * 0.3);
                player.Heal(healAmount);
                display.ShowCombatMessage($"Second Wind! You heal {healAmount} HP!");
                break;

            // Warrior abilities - Phase 3 implementation (Issue #362)
            case AbilityType.ShieldBash:
                {
                    var bashDamage = Math.Max(1, (int)(player.Attack * 1.2) - enemy.Defense);
                    enemy.HP -= bashDamage;
                    display.ShowCombatMessage($"You slam your shield into the enemy's skull! ({bashDamage} damage)");
                    if (new Random().NextDouble() < 0.5)
                    {
                        statusEffects.Apply(enemy, StatusEffect.Stun, 1);
                        display.ShowCombatMessage($"{enemy.Name} is stunned!");
                    }
                }
                break;
            
            case AbilityType.BattleCry:
                {
                    statusEffects.RemoveDebuffs(player);
                    statusEffects.Apply(player, StatusEffect.BattleCry, 3);
                    display.ShowCombatMessage("A primal roar tears from your throat — you will not fall!");
                }
                break;
            
            case AbilityType.Fortify:
                {
                    statusEffects.Apply(player, StatusEffect.Fortified, 3);
                    if (player.HP <= player.MaxHP * 0.5)
                    {
                        var fortifyHeal = (int)(player.MaxHP * 0.15);
                        player.Heal(fortifyHeal);
                        display.ShowCombatMessage($"You plant your feet and brace for the onslaught. (Healed {fortifyHeal} HP!)");
                    }
                    else
                    {
                        display.ShowCombatMessage("You plant your feet and brace for the onslaught.");
                    }
                }
                break;
            
            case AbilityType.RecklessBlow:
                {
                    var effectiveEnemyDef = enemy.Defense / 2;
                    var recklessDamage = Math.Max(1, (int)(player.Attack * 2.5) - effectiveEnemyDef);
                    enemy.HP -= recklessDamage;
                    
                    var selfDamage = Math.Max(1, (int)(player.MaxHP * 0.1));
                    if (player.HP - selfDamage < 1)
                        selfDamage = player.HP - 1;
                    player.TakeDamage(selfDamage);
                    
                    display.ShowCombatMessage($"You throw caution aside and swing with everything! ({recklessDamage} damage, {selfDamage} self-damage)");
                }
                break;
            
            case AbilityType.LastStand:
                {
                    var threshold = player.GetLastStandThreshold();
                    if (player.HP > player.MaxHP * threshold)
                    {
                        display.ShowCombatMessage("You must be gravely wounded to use Last Stand!");
                        player.RestoreMana(effectiveCost); // refund mana
                        return UseAbilityResult.InsufficientMana; // reuse this as generic failure
                    }
                    player.LastStandTurns = 2;
                    display.ShowCombatMessage("Your vision narrows. Everything slows. This ends now.");
                }
                break;

            // Mage abilities - Phase 3 implementation (Issue #362)
            case AbilityType.ArcaneBolt:
                {
                    var baseDmg = (int)((player.Attack * 1.5) + (player.Mana / 10));
                    if (player.IsOverchargeActive())
                        baseDmg = (int)(baseDmg * 1.25);
                    // Magic damage bypasses defense (or reduces it significantly)
                    var arcaneDamage = Math.Max(1, baseDmg - (enemy.Defense / 4));
                    enemy.HP -= arcaneDamage;
                    display.ShowCombatMessage($"A crackling bolt of raw energy leaps from your fingertips! ({arcaneDamage} damage)");
                }
                break;
            
            case AbilityType.FrostNova:
                {
                    var baseDmg = (int)(player.Attack * 1.2);
                    if (player.IsOverchargeActive())
                        baseDmg = (int)(baseDmg * 1.25);
                    var frostDamage = Math.Max(1, baseDmg - (enemy.Defense / 4));
                    enemy.HP -= frostDamage;
                    statusEffects.Apply(enemy, StatusEffect.Slow, 2);
                    display.ShowCombatMessage($"A wave of bitter cold explodes outward! ({frostDamage} damage, enemy slowed)");
                }
                break;
            
            case AbilityType.ManaShield:
                {
                    player.IsManaShieldActive = !player.IsManaShieldActive;
                    if (player.IsManaShieldActive)
                        display.ShowCombatMessage("You wrap yourself in a lattice of pure arcane energy.");
                    else
                        display.ShowCombatMessage("The arcane barrier dissolves.");
                }
                break;
            
            case AbilityType.ArcaneSacrifice:
                {
                    var sacrificeDamage = Math.Max(1, (int)(player.MaxHP * 0.15));
                    if (player.HP - sacrificeDamage < 1)
                        sacrificeDamage = player.HP - 1;
                    player.TakeDamage(sacrificeDamage);
                    
                    var manaRestore = (int)(player.MaxMana * 0.30);
                    player.RestoreMana(manaRestore);
                    
                    display.ShowCombatMessage($"You draw power from your own essence — dangerous, but effective. ({manaRestore} mana restored)");
                }
                break;
            
            case AbilityType.Meteor:
                {
                    var baseDmg = (player.Attack * 3) + 20;
                    if (player.IsOverchargeActive())
                        baseDmg = (int)(baseDmg * 1.25);
                    var meteorDamage = Math.Max(1, baseDmg);
                    enemy.HP -= meteorDamage;
                    
                    // Check for execute
                    if (enemy.HP <= enemy.MaxHP * 0.20 && !enemy.IsImmuneToEffects)
                    {
                        enemy.HP = 0;
                        display.ShowCombatMessage("The ceiling cracks. A fragment of the heavens descends. The creature collapses, obliterated.");
                    }
                    else
                    {
                        display.ShowCombatMessage($"The ceiling cracks. A fragment of the heavens descends! ({meteorDamage} damage)");
                    }
                }
                break;

            // Rogue abilities - Phase 3 implementation (Issue #362)
            case AbilityType.QuickStrike:
                {
                    var quickDamage = Math.Max(1, player.Attack - enemy.Defense);
                    enemy.HP -= quickDamage;
                    player.AddComboPoints(1);
                    display.ShowCombatMessage($"A lightning-fast jab — you're already setting up the next hit. ({quickDamage} damage, Combo: {player.ComboPoints})");
                }
                break;
            
            case AbilityType.Backstab:
                {
                    bool hasCondition = player.ShouldTriggerBackstabBonus(enemy, statusEffects.HasEffect);
                    int backstabDamage;
                    if (hasCondition)
                    {
                        backstabDamage = Math.Max(1, (int)(player.Attack * 2.5) - enemy.Defense);
                        display.ShowCombatMessage($"You find the opening and drive your blade home! Critical backstab! ({backstabDamage} damage)");
                    }
                    else
                    {
                        backstabDamage = Math.Max(1, (int)(player.Attack * 1.5) - enemy.Defense);
                        display.ShowCombatMessage($"You find the opening and drive your blade home. ({backstabDamage} damage)");
                    }
                    enemy.HP -= backstabDamage;
                }
                break;
            
            case AbilityType.Evade:
                {
                    player.EvadeNextAttack = true;
                    var comboGrant = player.GetEvadeComboPointGrant();
                    player.AddComboPoints(comboGrant);
                    display.ShowCombatMessage($"You melt into the shadows — the blow finds only air. (Combo: {player.ComboPoints})");
                }
                break;
            
            case AbilityType.Flurry:
                {
                    if (player.ComboPoints < 1)
                    {
                        display.ShowCombatMessage("You need at least 1 Combo Point!");
                        player.RestoreMana(effectiveCost);
                        return UseAbilityResult.InsufficientMana;
                    }
                    var pts = player.SpendComboPoints();
                    var flurryDamage = Math.Max(1, (int)((0.6 * pts) * player.Attack) - enemy.Defense);
                    enemy.HP -= flurryDamage;
                    
                    // Each "hit" has 30% chance to bleed
                    var rng = new Random();
                    for (int i = 0; i < pts; i++)
                    {
                        if (rng.NextDouble() < 0.30)
                        {
                            statusEffects.Apply(enemy, StatusEffect.Bleed, 3);
                            break; // Only apply once
                        }
                    }
                    display.ShowCombatMessage($"A blur of steel — {pts} combo strikes! ({flurryDamage} damage, 30% Bleed chance per hit)");
                }
                break;
            
            case AbilityType.Assassinate:
                {
                    if (player.ComboPoints < 3)
                    {
                        display.ShowCombatMessage("Assassinate requires 3+ Combo Points!");
                        player.RestoreMana(effectiveCost);
                        return UseAbilityResult.InsufficientMana;
                    }
                    var pts = player.SpendComboPoints();
                    var assassinateDamage = Math.Max(1, (int)((pts * 0.8) * player.Attack) - enemy.Defense);
                    enemy.HP -= assassinateDamage;
                    
                    // Check for execute
                    if (enemy.HP <= enemy.MaxHP * 0.30 && !enemy.IsImmuneToEffects)
                    {
                        enemy.HP = 0;
                        display.ShowCombatMessage("One clean strike. They never see it coming.");
                    }
                    else
                    {
                        display.ShowCombatMessage($"One clean strike for {assassinateDamage} damage!");
                    }
                }
                break;
        }
        
        return UseAbilityResult.Success;
    }
}

/// <summary>Represents the outcome of a <see cref="AbilityManager.UseAbility"/> call.</summary>
public enum UseAbilityResult
{
    /// <summary>The ability was used successfully and its effects have been applied.</summary>
    Success,

    /// <summary>No ability with the requested type exists in the manager's registry.</summary>
    InvalidAbility,

    /// <summary>The player has not yet reached the level required to unlock this ability.</summary>
    NotUnlocked,

    /// <summary>The ability was used recently and must wait for its cooldown to expire.</summary>
    OnCooldown,

    /// <summary>The player does not have enough mana to activate the ability.</summary>
    InsufficientMana
}
