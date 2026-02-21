namespace Dungnz.Engine;
using Dungnz.Models;
using Dungnz.Display;
using Dungnz.Systems;
using Dungnz.Systems.Enemies;

/// <summary>
/// Full turn-based combat engine that drives fight encounters between the player and
/// an enemy, handling player input, ability usage, status effects, boss mechanics
/// (enrage, telegraphed charge, ambush), loot distribution, and XP/level-up logic.
/// </summary>
public class CombatEngine : ICombatEngine
{
    private readonly IDisplayService _display;
    private readonly IInputReader _input;
    private readonly Random _rng;
    private readonly GameEvents? _events;
    private readonly StatusEffectManager _statusEffects;
    private readonly AbilityManager _abilities;
    private readonly List<CombatTurn> _turnLog = new();

    /// <summary>
    /// Initialises a new <see cref="CombatEngine"/> with the required display and input
    /// services, optional event bus, and optional pre-seeded subsystem instances for
    /// deterministic testing.
    /// </summary>
    /// <param name="display">The display service used to render all combat output.</param>
    /// <param name="input">
    /// The input reader used to receive player choices during combat.
    /// Defaults to <see cref="ConsoleInputReader"/> when <see langword="null"/>.
    /// </param>
    /// <param name="rng">
    /// The random-number generator used for hit/dodge/crit rolls.
    /// A new instance is created when <see langword="null"/>.
    /// </param>
    /// <param name="events">
    /// Optional event bus for broadcasting game-wide events such as combat end.
    /// </param>
    /// <param name="statusEffects">
    /// Optional pre-configured status-effect manager; a default instance is created
    /// when <see langword="null"/>.
    /// </param>
    /// <param name="abilities">
    /// Optional pre-configured ability manager; a default instance is created when
    /// <see langword="null"/>.
    /// </param>
    public CombatEngine(IDisplayService display, IInputReader? input = null, Random? rng = null, GameEvents? events = null, StatusEffectManager? statusEffects = null, AbilityManager? abilities = null)
    {
        _display = display;
        _input = input ?? new ConsoleInputReader();
        _rng = rng ?? new Random();
        _events = events;
        _statusEffects = statusEffects ?? new StatusEffectManager(display);
        _abilities = abilities ?? new AbilityManager();
    }

    /// <summary>
    /// Runs a complete combat encounter between <paramref name="player"/> and
    /// <paramref name="enemy"/>, looping through player/enemy turns until one side
    /// is defeated or the player successfully flees. Handles ambush enemies, boss
    /// phase-two enrage and charged attacks, status-effect tick processing, mana
    /// regeneration, ability menus, loot drops, and XP/level-up on victory.
    /// </summary>
    /// <param name="player">The player character participating in the fight.</param>
    /// <param name="enemy">The enemy the player is fighting.</param>
    /// <returns>
    /// <see cref="CombatResult.Won"/> if the enemy was defeated,
    /// <see cref="CombatResult.Fled"/> if the player escaped, or
    /// <see cref="CombatResult.PlayerDied"/> if the player's HP reached zero.
    /// </returns>
    public CombatResult RunCombat(Player player, Enemy enemy)
    {
        _display.ShowCombat($"A {enemy.Name} attacks!");
        _turnLog.Clear();

        // Ambush: Mimic gets a free first strike before the player can act
        if (enemy.IsAmbush)
        {
            _display.ShowCombatMessage($"It's a {enemy.Name}! You've been ambushed!");
            PerformEnemyTurn(player, enemy);
            if (player.HP <= 0) return CombatResult.PlayerDied;
        }
        
        while (true)
        {
            _statusEffects.ProcessTurnStart(player);
            _statusEffects.ProcessTurnStart(enemy);
            
            player.RestoreMana(10);
            _abilities.TickCooldowns();

            // Boss Phase 2: check enrage
            if (enemy is DungeonBoss boss)
            {
                var wasEnraged = boss.IsEnraged;
                boss.CheckEnrage();
                if (!wasEnraged && boss.IsEnraged)
                    _display.ShowCombatMessage("⚠ The boss ENRAGES! Its attack has increased by 50%!");
            }
            
            if (enemy.HP <= 0)
            {
                _display.ShowCombat($"You defeated the {enemy.Name}!");
                HandleLootAndXP(player, enemy);
                return CombatResult.Won;
            }
            
            if (player.HP <= 0) return CombatResult.PlayerDied;
            
            if (_statusEffects.HasEffect(player, StatusEffect.Stun))
            {
                _display.ShowCombatMessage("You are stunned and cannot act this turn!");
                PerformEnemyTurn(player, enemy);
                if (player.HP <= 0) return CombatResult.PlayerDied;
                continue;
            }
            
            _display.ShowCombatStatus(player, enemy);
            ShowRecentTurns();
            ShowCombatMenu(player);
            var choice = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
            
            if (choice == "F" || choice == "FLEE")
            {
                if (_rng.NextDouble() < 0.5)
                {
                    _display.ShowMessage("You fled successfully!");
                    _statusEffects.Clear(player);
                    _statusEffects.Clear(enemy);
                    return CombatResult.Fled;
                }
                else
                {
                    _display.ShowMessage("You failed to flee!");
                    PerformEnemyTurn(player, enemy);
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                    continue;
                }
            }
            
            if (choice == "B" || choice == "ABILITY")
            {
                var abilityResult = HandleAbilityMenu(player, enemy);
                if (abilityResult == AbilityMenuResult.Cancel)
                    continue;
                if (abilityResult == AbilityMenuResult.Used)
                {
                    if (enemy.HP <= 0)
                    {
                        _display.ShowCombat($"You defeated the {enemy.Name}!");
                        HandleLootAndXP(player, enemy);
                        return CombatResult.Won;
                    }
                    PerformEnemyTurn(player, enemy);
                    if (player.HP <= 0) return CombatResult.PlayerDied;
                    continue;
                }
            }
            
            if (choice == "A" || choice == "ATTACK")
            {
                PerformPlayerAttack(player, enemy);
                
                if (enemy.HP <= 0)
                {
                    _display.ShowCombat($"You defeated the {enemy.Name}!");
                    HandleLootAndXP(player, enemy);
                    return CombatResult.Won;
                }
                
                PerformEnemyTurn(player, enemy);
                if (player.HP <= 0) return CombatResult.PlayerDied;
            }
            else
            {
                _display.ShowError("Invalid choice. [A]ttack, [B]ability, or [F]lee.");
                PerformEnemyTurn(player, enemy);
                if (player.HP <= 0) return CombatResult.PlayerDied;
            }
        }
    }
    
    private void ShowCombatMenu(Player player)
    {
        _display.ShowMessage("[A]ttack [B]ability [F]lee");
        var unlockedAbilities = _abilities.GetUnlockedAbilities(player);
        if (unlockedAbilities.Any())
        {
            _display.ShowMessage($"Mana: {player.Mana}/{player.MaxMana}");
        }
    }

    /// <summary>
    /// Displays the last three entries from the combat turn log, giving the player a
    /// concise summary of recent exchanges before they choose their next action.
    /// </summary>
    private void ShowRecentTurns()
    {
        var recent = _turnLog.Count > 3 ? _turnLog.Skip(_turnLog.Count - 3).ToList() : _turnLog;
        if (recent.Count == 0) return;

        _display.ShowMessage("─── Recent turns ───");
        foreach (var turn in recent)
        {
            string line;
            if (turn.IsDodge)
                line = $"  {turn.Actor}: {turn.Action} → dodged";
            else if (turn.IsCrit)
                line = $"  {turn.Actor}: {turn.Action} → CRIT {turn.Damage} dmg";
            else
                line = $"  {turn.Actor}: {turn.Action} → {turn.Damage} dmg";

            if (turn.StatusApplied != null)
                line += $" [{turn.StatusApplied}]";

            _display.ShowMessage(line);
        }
    }
    
    private AbilityMenuResult HandleAbilityMenu(Player player, Enemy enemy)
    {
        var unlocked = _abilities.GetUnlockedAbilities(player);
        if (!unlocked.Any())
        {
            _display.ShowMessage("You haven't unlocked any abilities yet!");
            return AbilityMenuResult.Cancel;
        }
        
        _display.ShowMessage("\n=== Abilities ===");
        int index = 1;
        foreach (var ability in unlocked)
        {
            var status = "";
            if (_abilities.IsOnCooldown(ability.Type))
            {
                status = $" (Cooldown: {_abilities.GetCooldown(ability.Type)} turns)";
            }
            else if (player.Mana < ability.ManaCost)
            {
                status = $" (Need {ability.ManaCost} mana)";
            }
            else
            {
                status = $" [{index}]";
            }
            _display.ShowMessage($"{status} {ability.Name} - {ability.Description} (Cost: {ability.ManaCost} MP, CD: {ability.CooldownTurns} turns)");
            index++;
        }
        _display.ShowMessage("[C]ancel");
        
        var choice = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
        if (choice == "C" || choice == "CANCEL")
            return AbilityMenuResult.Cancel;
        
        if (int.TryParse(choice, out int abilityIndex) && abilityIndex >= 1 && abilityIndex <= unlocked.Count)
        {
            var selectedAbility = unlocked[abilityIndex - 1];
            var result = _abilities.UseAbility(player, enemy, selectedAbility.Type, _statusEffects, _display);
            
            if (result == UseAbilityResult.Success)
                return AbilityMenuResult.Used;
            
            _display.ShowMessage($"Cannot use ability: {result}");
            return AbilityMenuResult.Cancel;
        }
        
        _display.ShowMessage("Invalid choice!");
        return AbilityMenuResult.Cancel;
    }
    
    private void PerformPlayerAttack(Player player, Enemy enemy)
    {
        // Use flat dodge chance for enemies like Wraith, otherwise DEF-based
        bool dodged = enemy.FlatDodgeChance >= 0
            ? _rng.NextDouble() < enemy.FlatDodgeChance
            : RollDodge(enemy.Defense);

        if (dodged)
        {
            _display.ShowCombatMessage($"{enemy.Name} dodged your attack!");
            _turnLog.Add(new CombatTurn("You", "Attack", 0, false, true, null));
        }
        else
        {
            var playerDmg = Math.Max(1, player.Attack - enemy.Defense);
            var isCrit = RollCrit();
            if (isCrit)
            {
                playerDmg *= 2;
                _display.ShowCombatMessage("Critical hit!");
            }
            enemy.HP -= playerDmg;
            _display.ShowCombatMessage($"You hit {enemy.Name} for {playerDmg} damage!");
            _turnLog.Add(new CombatTurn("You", "Attack", playerDmg, isCrit, false, null));
        }
    }
    
    private void PerformEnemyTurn(Player player, Enemy enemy)
    {
        if (_statusEffects.HasEffect(enemy, StatusEffect.Stun))
        {
            _display.ShowCombatMessage($"{enemy.Name} is stunned and cannot act!");
            return;
        }

        // Goblin Shaman: try to heal when below 50% HP
        if (enemy is GoblinShaman shaman && shaman.HP < shaman.MaxHP / 2)
        {
            int heal = (int)(shaman.MaxHP * 0.20);
            shaman.HP = Math.Min(shaman.MaxHP, shaman.HP + heal);
            _display.ShowCombatMessage($"The {shaman.Name} channels dark magic and heals for {heal}!");
            _display.ShowCombatMessage($"({shaman.Name} HP: {shaman.HP}/{shaman.MaxHP})");
            return; // skip normal attack this turn
        }
        // Troll: regenerates 5% max HP each turn
        if (enemy is Troll troll)
        {
            int regen = Math.Max(1, (int)(troll.MaxHP * 0.05));
            troll.HP = Math.Min(troll.MaxHP, troll.HP + regen);
            _display.ShowCombatMessage($"The Troll regenerates {regen} HP!");
        }

        // Boss telegraphed charge: resolve previous charge or set new one
        if (enemy is DungeonBoss boss)
        {
            if (boss.IsCharging)
            {
                boss.IsCharging = false;
                boss.ChargeActive = true;
            }
            else if (_rng.Next(100) < 30)
            {
                boss.IsCharging = true;
                _display.ShowCombatMessage($"⚠ {enemy.Name} is charging a powerful attack! Prepare to defend!");
                return; // warn turn — no damage this turn
            }
        }
        
        if (RollDodge(player.Defense))
        {
            _display.ShowCombatMessage("You dodged the attack!");
            _turnLog.Add(new CombatTurn(enemy.Name, "Attack", 0, false, true, null));
        }
        else
        {
            var enemyDmg = Math.Max(1, enemy.Attack - player.Defense);

            // Apply charge multiplier (3x)
            if (enemy is DungeonBoss chargeBoss && chargeBoss.ChargeActive)
            {
                chargeBoss.ChargeActive = false;
                enemyDmg *= 3;
                _display.ShowCombatMessage($"⚡ {enemy.Name} unleashes the charged attack!");
            }

            var isCrit = RollCrit();
            if (isCrit)
            {
                enemyDmg *= 2;
                _display.ShowCombatMessage("Critical hit!");
            }
            player.TakeDamage(enemyDmg);
            _display.ShowCombatMessage($"{enemy.Name} hits you for {enemyDmg} damage!");

            string? statusApplied = null;

            // Poison on hit (e.g. Goblin Shaman poisons the player when it lands a hit)
            if (enemy.AppliesPoisonOnHit)
            {
                _statusEffects.Apply(player, StatusEffect.Poison, 3);
                statusApplied = "Poison";
            }

            // Lifesteal (e.g. Vampire Lord)
            if (enemy.LifestealPercent > 0)
            {
                var heal = (int)(enemyDmg * enemy.LifestealPercent);
                if (heal > 0)
                {
                    enemy.HP = Math.Min(enemy.MaxHP, enemy.HP + heal);
                    _display.ShowCombatMessage($"{enemy.Name} drains {heal} HP!");
                }
            }

            _turnLog.Add(new CombatTurn(enemy.Name, "Attack", enemyDmg, isCrit, false, statusApplied));
        }
    }
    
    private void HandleLootAndXP(Player player, Enemy enemy)
    {
        var loot = enemy.LootTable.RollDrop(enemy, player.Level);
        if (loot.Gold > 0)
        {
            player.AddGold(loot.Gold);
            _display.ShowMessage($"You found {loot.Gold} gold!");
        }
        if (loot.Item != null)
        {
            player.Inventory.Add(loot.Item);
            _display.ShowLootDrop(loot.Item);
        }
        
        player.AddXP(enemy.XPValue);
        _display.ShowMessage($"You gained {enemy.XPValue} XP. (Total: {player.XP})");
        CheckLevelUp(player);
        _events?.RaiseCombatEnded(player, enemy, CombatResult.Won);
        _statusEffects.Clear(player);
        _statusEffects.Clear(enemy);
    }
    
    private void CheckLevelUp(Player player)
    {
        while (player.XP / 100 + 1 > player.Level)
        {
            player.LevelUp();
            _display.ShowMessage($"LEVEL UP! You are now level {player.Level}!");
        }
    }
    
    private bool RollDodge(int defense)
    {
        var dodgeChance = defense / (double)(defense + 20);
        return _rng.NextDouble() < dodgeChance;
    }
    
    private bool RollCrit()
    {
        return _rng.NextDouble() < 0.15;
    }
}

/// <summary>
/// Indicates the outcome of the player's interaction with the in-combat ability
/// selection menu, so the combat loop knows whether to advance to the enemy's
/// turn or to re-display the main combat prompt.
/// </summary>
public enum AbilityMenuResult
{
    /// <summary>
    /// The player dismissed the menu without using an ability, either by choosing
    /// "Cancel" explicitly or by providing an invalid selection.
    /// </summary>
    Cancel,

    /// <summary>
    /// The player successfully activated an ability, consuming the required mana
    /// and triggering its effect; the enemy turn should now follow.
    /// </summary>
    Used
}
